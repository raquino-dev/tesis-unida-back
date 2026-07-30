# HTTPS, proxy inverso y Data Protection

Esta guía describe la configuración implementada en la API y cómo utilizarla en desarrollo y producción.

## 1. Responsabilidades

En desarrollo Docker, Postman se conecta directamente a Kestrel:

```text
Postman ──HTTP──> localhost:8080 ──> API
```

En producción, la aplicación no publica Kestrel directamente. Cloudflare y Nginx forman el borde HTTPS:

```text
Cliente
  └─HTTPS─> Cloudflare
              └─HTTPS─> Nginx
                          └─HTTP privado─> API:8080
```

Nginx:

- redirige el puerto 80 a HTTPS;
- presenta el certificado TLS;
- agrega `X-Forwarded-For` con la IP de origen;
- agrega `X-Forwarded-Proto: https`;
- reenvía la solicitud a `api:8080` por la red privada de Docker.

La API:

- no ejecuta `UseHttpsRedirection`, porque no termina TLS;
- procesa encabezados reenviados antes de routing, autenticación y rate limiting;
- sólo acepta esos encabezados desde las IP configuradas en `ReverseProxy:KnownProxies`;
- rechaza el arranque si se habilita el proxy sin declarar al menos una IP confiable.

No se debe configurar `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`: esa alternativa confía en cualquier proxy y reduce la protección contra encabezados falsificados.

## 2. Configuración local

`backend/deploy/compose.yaml` configura:

```yaml
ASPNETCORE_ENVIRONMENT: Development
ReverseProxy__Enabled: "false"
DataProtection__KeysPath: /app/data-protection-keys
```

La imagen oficial de ASP.NET Core ya escucha en el puerto HTTP 8080. No se vuelve a establecer `ASPNETCORE_URLS`, evitando la advertencia de configuración de puertos duplicada.

La API se abre directamente en:

```text
http://localhost:8080
```

No debe aparecer la advertencia `Failed to determine the https port for redirect`, porque la API ya no intenta convertir esta conexión local en HTTPS.

El volumen `data-protection-keys` conserva el anillo de claves aunque el contenedor sea reemplazado:

```yaml
volumes:
  - data-protection-keys:/app/data-protection-keys
```

En desarrollo las claves persistidas no se cifran con certificado. ASP.NET Core puede advertirlo en el log; es intencional para el entorno local. En `Production` la API no permite esta configuración.

## 3. Configuración productiva del proxy

Se debe asignar a Nginx una IP fija dentro de una red Docker privada. Ejemplo:

```yaml
services:
  nginx:
    networks:
      frontend:
        ipv4_address: 172.30.0.10

  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ReverseProxy__Enabled: "true"
      ReverseProxy__KnownProxies__0: 172.30.0.10
    networks:
      - frontend

networks:
  frontend:
    ipam:
      config:
        - subnet: 172.30.0.0/24
```

`172.30.0.10` debe coincidir exactamente con la dirección desde la cual la API observa la conexión de Nginx. No se debe agregar la red completa ni limpiar la lista de proxies confiables.

La API registra internamente tanto la forma IPv4 como su representación IPv6 mapeada (`::ffff:172.30.0.10`). Esto mantiene la validación exacta cuando Kestrel utiliza sockets de modo dual.

La plantilla de Nginx está en:

```text
backend/deploy/nginx/api.conf.example
```

Antes de usarla se reemplaza `api.example.com` y se montan los certificados TLS en las rutas indicadas. El certificado TLS de Nginx y el certificado de Data Protection son elementos distintos.

## 4. Qué es Data Protection

Data Protection es el sistema criptográfico de ASP.NET Core para datos internos protegidos, por ejemplo cookies, tokens temporales y componentes del framework. No sustituye:

- la clave `Jwt:SigningKey`;
- el certificado TLS de Nginx;
- el cifrado de PostgreSQL o de los archivos.

El sistema usa un anillo de claves. Si las claves sólo viven dentro del contenedor, desaparecen al recrearlo y cualquier dato protegido con ellas deja de poder abrirse. Por eso se guardan en un volumen persistente.

En producción también deben cifrarse en reposo. La API exige:

```text
DataProtection__KeysPath=/app/data-protection-keys
DataProtection__CertificatePath=/run/secrets/data-protection.pfx
DataProtection__CertificatePassword=<secreto>
```

Si falta el certificado o su contraseña, el proceso termina al iniciar. Esto evita desplegar accidentalmente claves XML sin cifrar.

## 5. Crear el certificado de Data Protection

Desde PowerShell, en la raíz del repositorio:

```powershell
.\backend\deploy\New-DataProtectionCertificate.ps1
```

El script:

1. solicita una contraseña como `SecureString`;
2. genera una clave RSA de 3072 bits;
3. exporta un PFX con clave privada;
4. elimina la copia temporal del almacén personal de Windows;
5. deja el archivo en `backend/deploy/secrets/data-protection.pfx`.

La carpeta `secrets` ignora todos sus archivos salvo su propio `.gitignore`. El PFX y su contraseña nunca deben confirmarse en Git.

Para producción es preferible generar el certificado directamente en el servidor:

```powershell
.\New-DataProtectionCertificate.ps1 -OutputPath "C:\ruta-segura\data-protection.pfx"
```

En Linux también puede generarse un PFX mediante OpenSSL. Debe contener la clave privada.

## 6. Montar el certificado y el volumen

Fragmento para el futuro `compose.production.yaml`:

```yaml
services:
  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ReverseProxy__Enabled: "true"
      ReverseProxy__KnownProxies__0: 172.30.0.10
      DataProtection__ApplicationName: FinanzasInteligentes.Api
      DataProtection__KeysPath: /app/data-protection-keys
      DataProtection__CertificatePath: /run/secrets/data-protection.pfx
      DataProtection__CertificatePassword: ${DATA_PROTECTION_CERTIFICATE_PASSWORD}
    volumes:
      - data-protection-keys:/app/data-protection-keys
      - ./secrets/data-protection.pfx:/run/secrets/data-protection.pfx:ro

volumes:
  data-protection-keys:
```

El volumen de claves debe formar parte del backup. No se elimina durante un despliegue normal.

## 7. Flujo completo de una petición

1. El cliente solicita `https://api.example.com/api/v1/cuentas`.
2. Cloudflare valida y reenvía HTTPS a Nginx.
3. Nginx termina TLS y abre una conexión HTTP privada hacia `api:8080`.
4. Nginx conserva `Host` y agrega `X-Forwarded-For` y `X-Forwarded-Proto`.
5. `UseForwardedHeaders` comprueba que el emisor sea la IP de Nginx.
6. Si es confiable, ASP.NET Core establece el esquema original como `https` y recupera la IP del cliente.
7. Routing, JWT, rate limiting, logging y endpoints trabajan con esos valores corregidos.
8. La respuesta vuelve por Nginx y Cloudflare usando HTTPS.

Si alguien accede directamente a Kestrel y envía un `X-Forwarded-For` falso, la API no lo procesa porque esa conexión no proviene del proxy confiable.

## 8. Verificación local

Reconstruir la API:

```powershell
cd backend\deploy
docker compose up -d --build api
docker compose logs --tail 100 api
```

Probar salud:

```powershell
Invoke-RestMethod http://localhost:8080/salud/vivo
Invoke-RestMethod http://localhost:8080/salud/listo
```

Comprobar que el volumen existe:

```powershell
docker volume ls --filter name=finanzas-inteligentes_data-protection-keys
docker compose exec api sh -c 'ls -la /app/data-protection-keys'
```

Recrear sólo la API y verificar que la misma clave continúa:

```powershell
docker compose up -d --force-recreate api
docker compose exec api sh -c 'ls -la /app/data-protection-keys'
```

No debe aparecer la advertencia sobre un puerto HTTPS desconocido. En desarrollo sí puede aparecer la advertencia que indica que las claves XML no tienen un cifrador; el certificado obligatorio de producción resuelve ese caso.

## 9. Fallos seguros esperados

La API no arranca en los siguientes casos:

- `ReverseProxy:Enabled=true` sin `KnownProxies`;
- una dirección de proxy no es una IP válida;
- `DataProtection:KeysPath` está vacío;
- `Production` no tiene certificado de Data Protection;
- se configuró certificado sin contraseña;
- el PFX no existe o no contiene clave privada.

Estos fallos ocurren durante el arranque para evitar que una configuración insegura llegue a atender solicitudes.
