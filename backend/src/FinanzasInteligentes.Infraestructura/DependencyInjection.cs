using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Infraestructura.Archivos;
using FinanzasInteligentes.Infraestructura.Autenticacion;
using FinanzasInteligentes.Infraestructura.Correo;
using FinanzasInteligentes.Infraestructura.Persistencia;
using FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;
using FinanzasInteligentes.Infraestructura.Procesamiento.Analitica;
using FinanzasInteligentes.Infraestructura.Procesamiento.Outbox;
using FinanzasInteligentes.Infraestructura.Procesamiento.Recurrencias;
using FinanzasInteligentes.Infraestructura.Procesamiento.Suscripciones;
using FinanzasInteligentes.Infraestructura.Seguridad;
using FinanzasInteligentes.Infraestructura.Suscripciones;
using FinanzasInteligentes.Infraestructura.Notificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Infraestructura;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructura(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistencia(configuration);

        var jwtSigningKey = RequireSecret(configuration, "Jwt:SigningKey");
        EnsureDifferentSecrets(
            ("Jwt:SigningKey", jwtSigningKey),
            ("Archivos:SigningKey", RequireSecret(configuration, "Archivos:SigningKey")),
            ("Seguridad:TokenPushKey", RequireSecret(configuration, "Seguridad:TokenPushKey")));

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(x => Encoding.UTF8.GetByteCount(x.SigningKey) >= 32, "Jwt:SigningKey debe tener al menos 32 bytes.")
            .Validate(x => x.AccessTokenMinutes is >= 10 and <= 15, "El access token debe durar entre 10 y 15 minutos.")
            .ValidateOnStart();

        services.AddOptions<PoliticaContrasenaOptions>()
            .Bind(configuration.GetSection(PoliticaContrasenaOptions.SectionName))
            .Validate(x => x.LongitudMinima is >= 10 and <= 64, "Contrasenas:LongitudMinima debe estar entre 10 y 64.")
            .Validate(x => x.LongitudMaxima is >= 64 and <= 256, "Contrasenas:LongitudMaxima debe estar entre 64 y 256.")
            .Validate(x => x.LongitudMaxima >= x.LongitudMinima, "La longitud máxima no puede ser menor que la mínima.")
            .ValidateOnStart();
        services.AddOptions<SeguridadFlujosOptions>()
            .Bind(configuration.GetSection(SeguridadFlujosOptions.SectionName))
            .Validate(x => x.OtpMinutos is >= 2 and <= 15, "SeguridadFlujos:OtpMinutos fuera de rango.")
            .Validate(x => x.VerificacionOtpMinutos is >= 2 and <= 30, "SeguridadFlujos:VerificacionOtpMinutos fuera de rango.")
            .Validate(x => x.RecuperacionContrasenaMinutos is >= 5 and <= 60, "SeguridadFlujos:RecuperacionContrasenaMinutos fuera de rango.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IPoliticaContrasena, PoliticaContrasena>();
        services.AddSingleton<ISeguridadFlujosConfiguracion, SeguridadFlujosConfiguracion>();
        services.AddSingleton<IHasherTokenUnSoloUso, HasherTokenUnSoloUso>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }

    public static IServiceCollection AddPersistencia(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException(
                "Falta ConnectionStrings:PostgreSql. Configúrela mediante una variable de entorno o un proveedor de secretos.");

        var archivosSigningKey = RequireSecret(configuration, "Archivos:SigningKey");
        var tokenPushKey = RequireSecret(configuration, "Seguridad:TokenPushKey");
        EnsureDifferentSecrets(
            ("Archivos:SigningKey", archivosSigningKey),
            ("Seguridad:TokenPushKey", tokenPushKey));

        services.AddOptions<ArchivoStorageOptions>()
            .Bind(configuration.GetSection(ArchivoStorageOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Ruta), "Archivos:Ruta es obligatorio.")
            .Validate(
                x => Uri.TryCreate(x.PublicBaseUrl, UriKind.Absolute, out _),
                "Archivos:PublicBaseUrl debe ser una URL absoluta.")
            .Validate(
                x => Encoding.UTF8.GetByteCount(x.SigningKey) >= 32,
                "Archivos:SigningKey debe tener al menos 32 bytes.")
            .ValidateOnStart();
        services.AddOptions<DocumentoOptions>()
            .Bind(configuration.GetSection(DocumentoOptions.SectionName))
            .Validate(x => x.TamanoMaximoBytes is >= 1_048_576 and <= 52_428_800,
                "Documentos:TamanoMaximoBytes debe estar entre 1 y 50 MiB.")
            .Validate(x => x.CantidadMaximaPorUsuario is >= 10 and <= 10_000,
                "Documentos:CantidadMaximaPorUsuario debe estar entre 10 y 10000.")
            .ValidateOnStart();
        services.AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(S3StorageOptions.SectionName))
            .Validate(
                x => !x.Habilitado ||
                    !string.IsNullOrWhiteSpace(x.Bucket),
                "S3:Bucket es obligatorio cuando S3 está habilitado.")
            .Validate(
                x => !x.Habilitado ||
                    string.Equals(x.Region, "us-east-1",
                        StringComparison.OrdinalIgnoreCase),
                "El piloto centralizado requiere S3:Region=us-east-1.")
            .ValidateOnStart();
        services.AddOptions<TextractOptions>()
            .Bind(configuration.GetSection(TextractOptions.SectionName))
            .Validate(
                x => !x.Habilitado ||
                    string.Equals(x.Region, "us-east-1",
                        StringComparison.OrdinalIgnoreCase),
                "El piloto centralizado requiere Textract:Region=us-east-1.")
            .ValidateOnStart();

        services.AddOptions<SeguridadOptions>()
            .Bind(configuration.GetSection(SeguridadOptions.SectionName))
            .Validate(
                x => Encoding.UTF8.GetByteCount(x.TokenPushKey) >= 32,
                "Seguridad:TokenPushKey debe tener al menos 32 bytes.")
            .ValidateOnStart();

        services.AddOptions<CorreoOptions>()
            .Bind(configuration.GetSection(CorreoOptions.SectionName))
            .Validate(x => !x.Habilitado || !string.IsNullOrWhiteSpace(x.Remitente),
                "Correo:Remitente es obligatorio cuando SES está habilitado.")
            .Validate(x => !x.Habilitado || !string.IsNullOrWhiteSpace(x.Region),
                "Correo:Region es obligatoria cuando SES está habilitado.")
            .Validate(x => Uri.TryCreate(x.UrlAplicacion, UriKind.Absolute, out _),
                "Correo:UrlAplicacion debe ser una URL absoluta.")
            .Validate(x => x.MaximoEnviosPorDestinatarioHora is >= 1 and <= 100,
                "Correo:MaximoEnviosPorDestinatarioHora debe estar entre 1 y 100.")
            .ValidateOnStart();
        services.AddOptions<FirebaseOptions>()
            .Bind(configuration.GetSection(FirebaseOptions.SectionName))
            .Validate(x => !x.Habilitado || !string.IsNullOrWhiteSpace(x.ProjectId),
                "Firebase:ProjectId es obligatorio cuando Firebase está habilitado.")
            .ValidateOnStart();

        services.AddDbContext<FinanzasDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "infra")));

        services.AddScoped<IIdentidadRepository, IdentidadRepository>();
        services.AddScoped<IPilotoRepository, PilotoRepository>();
        services.AddScoped<IFinanzasRepository, FinanzasRepository>();
        services.AddScoped<IFamiliasRepository, FamiliasRepository>();
        services.AddScoped<ISuscripcionesRepository, SuscripcionesRepository>();
        services.AddOptions<GooglePlayOptions>()
            .Bind(configuration.GetSection(GooglePlayOptions.SectionName))
            .Validate(x => !x.Habilitado || !string.IsNullOrWhiteSpace(x.PackageName),
                "GooglePlay:PackageName es obligatorio cuando Google Play está habilitado.")
            .Validate(x => !x.Habilitado || x.Productos.Count > 0,
                "GooglePlay:Productos debe mapear los planes cuando Google Play está habilitado.")
            .Validate(x => !x.Habilitado || Uri.TryCreate(x.RtdnAudience, UriKind.Absolute, out _),
                "GooglePlay:RtdnAudience debe ser una URL absoluta cuando Google Play está habilitado.")
            .Validate(x => !x.Habilitado || !string.IsNullOrWhiteSpace(x.RtdnServiceAccountEmail),
                "GooglePlay:RtdnServiceAccountEmail es obligatorio cuando Google Play está habilitado.")
            .ValidateOnStart();
        services.AddSingleton<IValidadorComprobanteSuscripcion, ValidadorComprobanteSuscripcion>();
        services.AddSingleton<IValidadorNotificacionGooglePlay, ValidadorNotificacionGooglePlay>();
        services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        services.AddScoped<ISincronizacionRepository, SincronizacionRepository>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<IRecurrenciasProcessor, RecurrenciasProcessor>();
        services.AddScoped<IAnaliticaProcessor, AnaliticaProcessor>();
        services.AddScoped<ISuscripcionesProcessor, SuscripcionesProcessor>();
        services.AddScoped<IDocumentosRepository, DocumentosRepository>();
        services.AddSingleton<LocalArchivoStorage>();
        services.AddSingleton<S3ArchivoStorage>();
        services.AddSingleton<IArchivoStorage>(provider =>
            configuration.GetValue<bool>("S3:Habilitado")
                ? provider.GetRequiredService<S3ArchivoStorage>()
                : provider.GetRequiredService<LocalArchivoStorage>());
        services.AddSingleton<IProcesadorOcrDocumento, TextractProcesadorOcr>();
        services.AddSingleton<IValidadorDocumento, ValidadorDocumento>();
        services.AddScoped<ISeguridadRepository, SeguridadRepository>();
        services.AddSingleton<IProtectorTokenPush, ProtectorTokenPush>();
        services.AddSingleton<ICorreoSender, SesCorreoSender>();
        services.AddScoped<IPushNotificationSender, FirebasePushSender>();

        return services;
    }

    private static string RequireSecret(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) < 32)
        {
            throw new InvalidOperationException(
                $"{key} debe configurarse externamente y tener al menos 32 bytes.");
        }

        return value;
    }

    private static void EnsureDifferentSecrets(params (string Name, string Value)[] secrets)
    {
        for (var current = 0; current < secrets.Length; current++)
        {
            for (var other = current + 1; other < secrets.Length; other++)
            {
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(secrets[current].Value),
                    Encoding.UTF8.GetBytes(secrets[other].Value)))
                {
                    throw new InvalidOperationException(
                        $"{secrets[current].Name} y {secrets[other].Name} deben usar secretos diferentes.");
                }
            }
        }
    }
}
