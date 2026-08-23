using FinanzasInteligentes.Dominio.Analitica;
using FinanzasInteligentes.Dominio.Documentos;
using FinanzasInteligentes.Dominio.FinanzasFamiliares;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Identidad;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using FinanzasInteligentes.Dominio.Piloto;
using FinanzasInteligentes.Dominio.Seguridad;
using FinanzasInteligentes.Dominio.Suscripciones;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Persistencia;

public sealed class FinanzasDbContext(DbContextOptions<FinanzasDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<PreferenciasUsuario> Preferencias => Set<PreferenciasUsuario>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<DesafioOtp> DesafiosOtp => Set<DesafioOtp>();
    public DbSet<VerificacionOtp> VerificacionesOtp => Set<VerificacionOtp>();
    public DbSet<RecuperacionContrasena> RecuperacionesContrasena => Set<RecuperacionContrasena>();
    public DbSet<EliminacionPerfil> EliminacionesPerfil => Set<EliminacionPerfil>();
    public DbSet<PoliticaPrivacidad> PoliticasPrivacidad => Set<PoliticaPrivacidad>();
    public DbSet<ConsentimientoPrivacidad> ConsentimientosPrivacidad => Set<ConsentimientoPrivacidad>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<TarjetaCredito> TarjetasCredito => Set<TarjetaCredito>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<MovimientoRecurrente> MovimientosRecurrentes => Set<MovimientoRecurrente>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();
    public DbSet<AlertaFinanciera> AlertasFinancieras => Set<AlertaFinanciera>();
    public DbSet<DocumentoFinanciero> DocumentosFinancieros => Set<DocumentoFinanciero>();
    public DbSet<ProcesamientoDocumental> ProcesamientosDocumentales => Set<ProcesamientoDocumental>();
    public DbSet<Exportacion> Exportaciones => Set<Exportacion>();
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<EventoSeguridad> EventosSeguridad => Set<EventoSeguridad>();
    public DbSet<EventoAuditoria> EventosAuditoria => Set<EventoAuditoria>();
    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();
    public DbSet<MetaAhorro> MetasAhorro => Set<MetaAhorro>();
    public DbSet<AporteMeta> AportesMeta => Set<AporteMeta>();
    public DbSet<GrupoFamiliar> GruposFamiliares => Set<GrupoFamiliar>();
    public DbSet<IntegranteFamiliar> IntegrantesFamiliares => Set<IntegranteFamiliar>();
    public DbSet<InvitacionFamiliar> InvitacionesFamiliares => Set<InvitacionFamiliar>();
    public DbSet<CuentaCompartida> CuentasCompartidas => Set<CuentaCompartida>();
    public DbSet<CategoriaFamiliar> CategoriasFamiliares => Set<CategoriaFamiliar>();
    public DbSet<MovimientoFamiliar> MovimientosFamiliares => Set<MovimientoFamiliar>();
    public DbSet<CajaCompartida> CajasCompartidas => Set<CajaCompartida>();
    public DbSet<OperacionCaja> OperacionesCaja => Set<OperacionCaja>();
    public DbSet<PresupuestoFamiliar> PresupuestosFamiliares => Set<PresupuestoFamiliar>();
    public DbSet<EliminacionGrupoFamiliar> EliminacionesGrupos => Set<EliminacionGrupoFamiliar>();
    public DbSet<PlanSuscripcion> PlanesSuscripcion => Set<PlanSuscripcion>();
    public DbSet<Suscripcion> Suscripciones => Set<Suscripcion>();
    public DbSet<TransaccionSuscripcion> TransaccionesSuscripcion => Set<TransaccionSuscripcion>();
    public DbSet<AvisoSuscripcion> AvisosSuscripcion => Set<AvisoSuscripcion>();
    public DbSet<EventoOutbox> EventosOutbox => Set<EventoOutbox>();
    public DbSet<EntregaOutbox> EntregasOutbox => Set<EntregaOutbox>();
    public DbSet<Idempotencia> Idempotencias => Set<Idempotencia>();
    public DbSet<InstrumentoPiloto> InstrumentosPiloto => Set<InstrumentoPiloto>();
    public DbSet<PreguntaInstrumentoPiloto> PreguntasInstrumentoPiloto => Set<PreguntaInstrumentoPiloto>();
    public DbSet<RespuestaInstrumentoPiloto> RespuestasInstrumentoPiloto => Set<RespuestaInstrumentoPiloto>();
    public DbSet<DetalleRespuestaInstrumentoPiloto> DetallesRespuestaInstrumentoPiloto => Set<DetalleRespuestaInstrumentoPiloto>();
    public DbSet<CambioSincronizacion> CambiosSincronizacion => Set<CambioSincronizacion>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RegistrarCambiosSincronizacion();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        ConfigurarUsuario(modelBuilder);
        ConfigurarPrivacidad(modelBuilder);
        ConfigurarFinanzas(modelBuilder);
        ConfigurarAnalitica(modelBuilder);
        ConfigurarDocumentos(modelBuilder);
        ConfigurarSeguridadYAuditoria(modelBuilder);
        ConfigurarFamilias(modelBuilder);
        ConfigurarSuscripciones(modelBuilder);
        ConfigurarPiloto(modelBuilder);
        ConfigurarInfraestructura(modelBuilder);
        ConfigurarSincronizacion(modelBuilder);
    }

    private static void ConfigurarSincronizacion(ModelBuilder builder)
    {
        var cambio = builder.Entity<CambioSincronizacion>();
        cambio.ToTable("cambios", "sincronizacion");
        cambio.HasKey(x => x.Secuencia);
        cambio.Property(x => x.Secuencia).HasColumnName("secuencia").ValueGeneratedOnAdd();
        cambio.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        cambio.Property(x => x.TipoEntidad).HasColumnName("tipo_entidad").HasMaxLength(40);
        cambio.Property(x => x.EntidadId).HasColumnName("entidad_id");
        cambio.Property(x => x.Operacion).HasColumnName("operacion").HasMaxLength(20);
        cambio.Property(x => x.Version).HasColumnName("version");
        cambio.Property(x => x.OcurridoEn).HasColumnName("ocurrido_en");
        cambio.HasIndex(x => new { x.UsuarioId, x.Secuencia });
    }

    private void RegistrarCambiosSincronizacion()
    {
        var registrados = ChangeTracker.Entries<CambioSincronizacion>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => (x.Entity.UsuarioId, x.Entity.TipoEntidad, x.Entity.EntidadId))
            .ToHashSet();
        var cambios = ChangeTracker.Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified)
            .Select(x => DatosCambio(x.Entity))
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .Where(x => !registrados.Contains((x.UsuarioId, x.Tipo, x.EntidadId)))
            .Select(x => CambioSincronizacion.Crear(
                x.UsuarioId, x.Tipo, x.EntidadId, x.Operacion, x.Version))
            .ToArray();
        if (cambios.Length > 0) CambiosSincronizacion.AddRange(cambios);
    }

    private static (Guid UsuarioId, string Tipo, Guid EntidadId, string Operacion, long Version)?
        DatosCambio(object entity) => entity switch
        {
            Cuenta x => (x.UsuarioId, "cuenta", x.Id,
                x.EliminadoEn is null ? "actualizado" : "eliminado", x.Version),
            Categoria x when x.UsuarioId is not null => (x.UsuarioId.Value, "categoria", x.Id,
                x.EliminadoEn is null ? "actualizado" : "eliminado", x.Version),
            Movimiento x => (x.UsuarioId, "movimiento", x.Id,
                x.Estado == "anulado" ? "eliminado" : "actualizado", x.Version),
            Presupuesto x => (x.UsuarioId, "presupuesto", x.Id,
                x.EliminadoEn is null ? "actualizado" : "eliminado", x.Version),
            MetaAhorro x when x.UsuarioId is not null => (x.UsuarioId.Value, "meta_ahorro", x.Id,
                x.EliminadoEn is null ? "actualizado" : "eliminado", x.Version),
            MovimientoRecurrente x => (x.UsuarioId, "movimiento_recurrente", x.Id,
                x.EliminadoEn is null ? "actualizado" : "eliminado", x.Version),
            TarjetaCredito x => (x.UsuarioId, "tarjeta_credito", x.Id,
                x.EliminadoEn is null ? "actualizado" : "eliminado", x.Version),
            _ => null
        };

    private static void ConfigurarPiloto(ModelBuilder builder)
    {
        var instrumento = builder.Entity<InstrumentoPiloto>();
        instrumento.ToTable("instrumentos", "piloto");
        instrumento.HasKey(x => x.Id);
        instrumento.Property(x => x.Id).HasColumnName("id");
        instrumento.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(40);
        instrumento.Property(x => x.VersionInstrumento).HasColumnName("version_instrumento").HasMaxLength(30);
        instrumento.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(160);
        instrumento.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(1000);
        instrumento.Property(x => x.Activo).HasColumnName("activo");
        instrumento.Property(x => x.CreadoEn).HasColumnName("creado_en");
        instrumento.HasIndex(x => new { x.Codigo, x.VersionInstrumento }).IsUnique();
        instrumento.HasIndex(x => x.Codigo).IsUnique().HasFilter("activo = true");

        var pregunta = builder.Entity<PreguntaInstrumentoPiloto>();
        pregunta.ToTable("preguntas", "piloto");
        pregunta.HasKey(x => x.Id);
        pregunta.Property(x => x.Id).HasColumnName("id");
        pregunta.Property(x => x.InstrumentoId).HasColumnName("instrumento_id");
        pregunta.Property(x => x.Orden).HasColumnName("orden");
        pregunta.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20);
        pregunta.Property(x => x.Texto).HasColumnName("texto").HasMaxLength(1000);
        pregunta.Property(x => x.Requerida).HasColumnName("requerida");
        pregunta.Property(x => x.Minimo).HasColumnName("minimo");
        pregunta.Property(x => x.Maximo).HasColumnName("maximo");
        pregunta.HasIndex(x => new { x.InstrumentoId, x.Orden }).IsUnique();
        pregunta.HasOne<InstrumentoPiloto>().WithMany(x => x.Preguntas)
            .HasForeignKey(x => x.InstrumentoId).OnDelete(DeleteBehavior.Cascade);

        var respuesta = builder.Entity<RespuestaInstrumentoPiloto>();
        respuesta.ToTable("respuestas", "piloto");
        respuesta.HasKey(x => x.Id);
        respuesta.Property(x => x.Id).HasColumnName("id");
        respuesta.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        respuesta.Property(x => x.InstrumentoId).HasColumnName("instrumento_id");
        respuesta.Property(x => x.VersionInstrumento).HasColumnName("version_instrumento").HasMaxLength(30);
        respuesta.Property(x => x.RespondidoEn).HasColumnName("respondido_en");
        respuesta.HasIndex(x => new { x.UsuarioId, x.InstrumentoId }).IsUnique();
        respuesta.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        respuesta.HasOne<InstrumentoPiloto>().WithMany().HasForeignKey(x => x.InstrumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        var detalle = builder.Entity<DetalleRespuestaInstrumentoPiloto>();
        detalle.ToTable("respuestas_detalle", "piloto");
        detalle.HasKey(x => x.Id);
        detalle.Property(x => x.Id).HasColumnName("id");
        detalle.Property(x => x.RespuestaId).HasColumnName("respuesta_id");
        detalle.Property(x => x.PreguntaId).HasColumnName("pregunta_id");
        detalle.Property(x => x.ValorEscala).HasColumnName("valor_escala");
        detalle.Property(x => x.ValorTexto).HasColumnName("valor_texto").HasMaxLength(2000);
        detalle.HasIndex(x => new { x.RespuestaId, x.PreguntaId }).IsUnique();
        detalle.HasOne<RespuestaInstrumentoPiloto>().WithMany(x => x.Detalles)
            .HasForeignKey(x => x.RespuestaId).OnDelete(DeleteBehavior.Cascade);
        detalle.HasOne<PreguntaInstrumentoPiloto>().WithMany().HasForeignKey(x => x.PreguntaId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarUsuario(ModelBuilder builder)
    {
        var usuario = builder.Entity<Usuario>();
        usuario.ToTable("usuarios", "identidad");
        usuario.HasKey(x => x.Id);
        usuario.Property(x => x.Id).HasColumnName("id");
        usuario.Property(x => x.Correo).HasColumnName("correo").HasColumnType("citext").HasMaxLength(320);
        usuario.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
        usuario.Property(x => x.Alias).HasColumnName("alias").HasColumnType("citext").HasMaxLength(24);
        usuario.Property(x => x.HashContrasena).HasColumnName("hash_contrasena");
        usuario.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        usuario.Property(x => x.Idioma).HasColumnName("idioma").HasMaxLength(10);
        usuario.Property(x => x.Ubicacion).HasColumnName("ubicacion").HasMaxLength(160);
        usuario.Property(x => x.ZonaHoraria).HasColumnName("zona_horaria").HasMaxLength(80);
        usuario.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(30);
        usuario.Property(x => x.Rol).HasColumnName("rol").HasMaxLength(30);
        usuario.Property(x => x.AnonimizadoEn).HasColumnName("anonimizado_en");
        ConfigurarMutable(usuario);
        usuario.HasIndex(x => x.Correo).IsUnique().HasFilter("anonimizado_en IS NULL");
        usuario.HasIndex(x => x.Alias).IsUnique().HasFilter("anonimizado_en IS NULL");
        usuario.HasOne(x => x.Preferencias).WithOne().HasForeignKey<PreferenciasUsuario>(x => x.UsuarioId);

        var preferencias = builder.Entity<PreferenciasUsuario>();
        preferencias.ToTable("preferencias", "identidad");
        preferencias.HasKey(x => x.UsuarioId);
        preferencias.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        preferencias.Property(x => x.Tema).HasColumnName("tema").HasMaxLength(20);
        preferencias.Property(x => x.NotificacionesPush).HasColumnName("notificaciones_push");
        preferencias.Property(x => x.NotificacionesCorreo).HasColumnName("notificaciones_correo");
        preferencias.Property(x => x.ResumenSemanal).HasColumnName("resumen_semanal");
        preferencias.Property(x => x.CreadoEn).HasColumnName("creado_en");
        preferencias.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
        preferencias.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();

        var sesion = builder.Entity<Sesion>();
        sesion.ToTable("sesiones", "identidad");
        sesion.HasKey(x => x.Id);
        sesion.Property(x => x.Id).HasColumnName("id");
        sesion.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        sesion.Property(x => x.HashRefreshToken).HasColumnName("hash_refresh_token").HasMaxLength(64);
        sesion.Property(x => x.FamiliaToken).HasColumnName("familia_token");
        sesion.Property(x => x.ExpiraEn).HasColumnName("expira_en");
        sesion.Property(x => x.UsadoEn).HasColumnName("usado_en");
        sesion.Property(x => x.RevocadoEn).HasColumnName("revocado_en");
        sesion.Property(x => x.IdentificadorDispositivo).HasColumnName("identificador_dispositivo").HasMaxLength(200);
        sesion.Property(x => x.NombreDispositivo).HasColumnName("nombre_dispositivo").HasMaxLength(120);
        sesion.Property(x => x.PlataformaDispositivo).HasColumnName("plataforma_dispositivo").HasMaxLength(30);
        sesion.Property(x => x.CreadoEn).HasColumnName("creado_en");
        sesion.HasIndex(x => x.HashRefreshToken).IsUnique();
        sesion.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);

        ConfigurarSeguridad(builder);
    }

    private static void ConfigurarSeguridad(ModelBuilder builder)
    {
        var desafio = builder.Entity<DesafioOtp>();
        desafio.ToTable("desafios_otp", "identidad");
        desafio.HasKey(x => x.Id);
        desafio.Property(x => x.Id).HasColumnName("id");
        desafio.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        desafio.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(50);
        desafio.Property(x => x.Canal).HasColumnName("canal").HasMaxLength(20);
        desafio.Property(x => x.HashCodigo).HasColumnName("hash_codigo").HasMaxLength(64);
        desafio.Property(x => x.Destino).HasColumnName("destino").HasMaxLength(320);
        desafio.Property(x => x.ExpiraEn).HasColumnName("expira_en");
        desafio.Property(x => x.IntentosRestantes).HasColumnName("intentos_restantes");
        desafio.Property(x => x.VerificadoEn).HasColumnName("verificado_en");
        ConfigurarMutable(desafio);
        desafio.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);

        var verificacion = builder.Entity<VerificacionOtp>();
        verificacion.ToTable("verificaciones_otp", "identidad");
        verificacion.HasKey(x => x.Id);
        verificacion.Property(x => x.Id).HasColumnName("id");
        verificacion.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        verificacion.Property(x => x.DesafioId).HasColumnName("desafio_id");
        verificacion.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(50);
        verificacion.Property(x => x.ExpiraEn).HasColumnName("expira_en");
        verificacion.Property(x => x.ConsumidoEn).HasColumnName("consumido_en");
        verificacion.Property(x => x.CreadoEn).HasColumnName("creado_en");
        verificacion.HasIndex(x => x.DesafioId).IsUnique();
        verificacion.HasOne<DesafioOtp>().WithOne()
            .HasForeignKey<VerificacionOtp>(x => x.DesafioId)
            .OnDelete(DeleteBehavior.Cascade);
        verificacion.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        var recuperacion = builder.Entity<RecuperacionContrasena>();
        recuperacion.ToTable("recuperaciones_contrasena", "identidad");
        recuperacion.HasKey(x => x.Id);
        recuperacion.Property(x => x.Id).HasColumnName("id");
        recuperacion.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        recuperacion.Property(x => x.HashToken).HasColumnName("hash_token").HasMaxLength(64);
        recuperacion.Property(x => x.ExpiraEn).HasColumnName("expira_en");
        recuperacion.Property(x => x.ConsumidoEn).HasColumnName("consumido_en");
        recuperacion.Property(x => x.CreadoEn).HasColumnName("creado_en");
        recuperacion.HasIndex(x => x.HashToken).IsUnique();
        recuperacion.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        var eliminacion = builder.Entity<EliminacionPerfil>();
        eliminacion.ToTable("eliminaciones_perfil", "identidad");
        eliminacion.HasKey(x => x.Id);
        eliminacion.Property(x => x.Id).HasColumnName("id");
        eliminacion.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        eliminacion.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        eliminacion.Property(x => x.CompletadoEn).HasColumnName("completado_en");
        eliminacion.Property(x => x.ErrorCodigo).HasColumnName("error_codigo").HasMaxLength(80);
        ConfigurarMutable(eliminacion);
        eliminacion.HasIndex(x => x.UsuarioId)
            .IsUnique().HasFilter("estado = 'pendiente'");
        eliminacion.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarPrivacidad(ModelBuilder builder)
    {
        var politica = builder.Entity<PoliticaPrivacidad>();
        politica.ToTable("politicas_privacidad", "identidad");
        politica.HasKey(x => x.Id);
        politica.Property(x => x.Id).HasColumnName("id");
        politica.Property(x => x.VersionPolitica).HasColumnName("version_politica").HasMaxLength(30);
        politica.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(160);
        politica.Property(x => x.UrlDocumento).HasColumnName("url_documento").HasMaxLength(500);
        politica.Property(x => x.VigenteDesde).HasColumnName("vigente_desde");
        politica.Property(x => x.Activa).HasColumnName("activa");
        politica.Property(x => x.CreadoEn).HasColumnName("creado_en");
        politica.HasIndex(x => x.VersionPolitica).IsUnique();
        politica.HasData(new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000201"),
            VersionPolitica = "1.0",
            Titulo = "Política de privacidad",
            UrlDocumento = "https://example.invalid/privacidad/1.0",
            VigenteDesde = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            Activa = true,
            CreadoEn = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)
        });

        var consentimiento = builder.Entity<ConsentimientoPrivacidad>();
        consentimiento.ToTable("consentimientos_privacidad", "identidad");
        consentimiento.HasKey(x => x.Id);
        consentimiento.Property(x => x.Id).HasColumnName("id");
        consentimiento.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        consentimiento.Property(x => x.PoliticaId).HasColumnName("politica_id");
        consentimiento.Property(x => x.VersionPolitica).HasColumnName("version_politica").HasMaxLength(30);
        consentimiento.Property(x => x.Finalidad).HasColumnName("finalidad").HasMaxLength(120);
        consentimiento.Property(x => x.AceptadoEn).HasColumnName("aceptado_en");
        consentimiento.Property(x => x.RevocadoEn).HasColumnName("revocado_en");
        consentimiento.Property(x => x.CreadoEn).HasColumnName("creado_en");
        consentimiento.HasIndex(x => new { x.UsuarioId, x.PoliticaId, x.Finalidad })
            .IsUnique().HasFilter("revocado_en IS NULL");
        consentimiento.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
        consentimiento.HasOne<PoliticaPrivacidad>().WithMany().HasForeignKey(x => x.PoliticaId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarFinanzas(ModelBuilder builder)
    {
        var cuenta = builder.Entity<Cuenta>();
        cuenta.ToTable("cuentas", "finanzas", table =>
        {
            table.HasCheckConstraint("ck_cuentas_moneda", "moneda = 'PYG'");
        });
        cuenta.HasKey(x => x.Id);
        cuenta.Property(x => x.Id).HasColumnName("id");
        cuenta.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        cuenta.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
        cuenta.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(40);
        cuenta.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        cuenta.Property(x => x.SaldoActual).HasColumnName("saldo_actual");
        cuenta.Property(x => x.SaldoInicial).HasColumnName("saldo_inicial");
        cuenta.Property(x => x.Color).HasColumnName("color").HasMaxLength(7);
        cuenta.Property(x => x.Icono).HasColumnName("icono").HasMaxLength(50);
        cuenta.Property(x => x.IncluidaEnTotal).HasColumnName("incluida_en_total");
        cuenta.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(cuenta);
        cuenta.HasIndex(x => new { x.UsuarioId, x.Nombre }).IsUnique().HasFilter("eliminado_en IS NULL");

        var categoria = builder.Entity<Categoria>();
        categoria.ToTable("categorias", "finanzas");
        categoria.HasKey(x => x.Id);
        categoria.Property(x => x.Id).HasColumnName("id");
        categoria.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        categoria.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100);
        categoria.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20);
        categoria.Property(x => x.Icono).HasColumnName("icono").HasMaxLength(50);
        categoria.Property(x => x.Color).HasColumnName("color").HasMaxLength(7);
        categoria.Property(x => x.EsPredeterminada).HasColumnName("es_predeterminada");
        categoria.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(categoria);

        var tarjeta = builder.Entity<TarjetaCredito>();
        tarjeta.ToTable("tarjetas_credito", "finanzas", table =>
        {
            table.HasCheckConstraint("ck_tarjetas_credito_moneda", "moneda = 'PYG'");
            table.HasCheckConstraint(
                "ck_tarjetas_credito_dias",
                "dia_cierre BETWEEN 1 AND 31 AND dia_vencimiento BETWEEN 1 AND 31");
            table.HasCheckConstraint(
                "ck_tarjetas_credito_limites",
                "limite_credito >= 0 AND saldo_utilizado >= 0");
        });
        tarjeta.HasKey(x => x.Id);
        tarjeta.Property(x => x.Id).HasColumnName("id");
        tarjeta.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        tarjeta.Property(x => x.CuentaPagoId).HasColumnName("cuenta_pago_id");
        tarjeta.Property(x => x.Alias).HasColumnName("alias").HasMaxLength(120);
        tarjeta.Property(x => x.DiaCierre).HasColumnName("dia_cierre");
        tarjeta.Property(x => x.DiaVencimiento).HasColumnName("dia_vencimiento");
        tarjeta.Property(x => x.LimiteCredito).HasColumnName("limite_credito");
        tarjeta.Property(x => x.SaldoUtilizado).HasColumnName("saldo_utilizado");
        tarjeta.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        tarjeta.Property(x => x.Color).HasColumnName("color").HasMaxLength(7);
        tarjeta.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        tarjeta.Ignore(x => x.CreditoDisponible);
        ConfigurarMutable(tarjeta);
        tarjeta.HasIndex(x => new { x.UsuarioId, x.Alias })
            .IsUnique().HasFilter("eliminado_en IS NULL");
        tarjeta.HasOne<Cuenta>().WithMany()
            .HasForeignKey(x => x.CuentaPagoId)
            .OnDelete(DeleteBehavior.Restrict);

        var movimiento = builder.Entity<Movimiento>();
        movimiento.ToTable("movimientos", "finanzas", table =>
        {
            table.HasCheckConstraint("ck_movimientos_monto", "monto > 0");
            table.HasCheckConstraint("ck_movimientos_moneda", "moneda = 'PYG'");
        });
        movimiento.HasKey(x => x.Id);
        movimiento.Property(x => x.Id).HasColumnName("id");
        movimiento.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        movimiento.Property(x => x.CuentaId).HasColumnName("cuenta_id");
        movimiento.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(10);
        movimiento.Property(x => x.Monto).HasColumnName("monto");
        movimiento.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        movimiento.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300);
        movimiento.Property(x => x.Fecha).HasColumnName("fecha");
        movimiento.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        movimiento.Property(x => x.Origen).HasColumnName("origen").HasMaxLength(30);
        movimiento.Property(x => x.AnuladoEn).HasColumnName("anulado_en");
        movimiento.Property(x => x.MotivoAnulacion).HasColumnName("motivo_anulacion").HasMaxLength(300);
        movimiento.Property(x => x.RecurrenciaId).HasColumnName("recurrencia_id");
        movimiento.Property(x => x.PeriodoRecurrencia).HasColumnName("periodo_recurrencia");
        movimiento.Property(x => x.TransferenciaId).HasColumnName("transferencia_id");
        movimiento.Property(x => x.DocumentoId).HasColumnName("documento_id");
        movimiento.Property(x => x.CreadoEn).HasColumnName("creado_en");
        movimiento.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        movimiento.HasIndex(x => new { x.UsuarioId, x.Fecha, x.Id });
        movimiento.HasIndex(x => new { x.RecurrenciaId, x.PeriodoRecurrencia })
            .IsUnique().HasFilter("recurrencia_id IS NOT NULL");
        movimiento.HasOne<Cuenta>().WithMany().HasForeignKey(x => x.CuentaId).OnDelete(DeleteBehavior.Restrict);
        movimiento.HasMany(x => x.Categorias).WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "MovimientoCategoria",
                right => right.HasOne<Categoria>().WithMany()
                    .HasForeignKey("categoria_id").OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<Movimiento>().WithMany()
                    .HasForeignKey("movimiento_id").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("movimientos_categorias", "finanzas");
                    join.HasKey("movimiento_id", "categoria_id");
                });

        var recurrente = builder.Entity<MovimientoRecurrente>();
        recurrente.ToTable("recurrencias", "finanzas", table =>
        {
            table.HasCheckConstraint("ck_recurrencias_monto", "monto > 0");
            table.HasCheckConstraint("ck_recurrencias_moneda", "moneda = 'PYG'");
            table.HasCheckConstraint(
                "ck_recurrencias_frecuencia",
                "frecuencia IN ('diaria', 'semanal', 'quincenal', 'mensual', 'anual')");
            table.HasCheckConstraint(
                "ck_recurrencias_fechas",
                "fecha_fin IS NULL OR fecha_fin >= fecha_inicio");
        });
        recurrente.HasKey(x => x.Id);
        recurrente.Property(x => x.Id).HasColumnName("id");
        recurrente.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        recurrente.Property(x => x.CuentaId).HasColumnName("cuenta_id");
        recurrente.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(10);
        recurrente.Property(x => x.Monto).HasColumnName("monto");
        recurrente.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        recurrente.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300);
        recurrente.Property(x => x.FechaInicio).HasColumnName("fecha_inicio");
        recurrente.Property(x => x.FechaFin).HasColumnName("fecha_fin");
        recurrente.Property(x => x.Frecuencia).HasColumnName("frecuencia").HasMaxLength(20);
        recurrente.Property(x => x.CantidadOcurrencias).HasColumnName("cantidad_ocurrencias");
        recurrente.Property(x => x.OcurrenciasCompletadas).HasColumnName("ocurrencias_completadas");
        recurrente.Property(x => x.ProximaEjecucion).HasColumnName("proxima_ejecucion");
        recurrente.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        recurrente.Property(x => x.UltimaEjecucionEn).HasColumnName("ultima_ejecucion_en");
        recurrente.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(recurrente);
        recurrente.HasIndex(x => new { x.Estado, x.ProximaEjecucion });
        recurrente.HasIndex(x => new { x.UsuarioId, x.CuentaId, x.Descripcion })
            .IsUnique().HasFilter("estado = 'activa' AND eliminado_en IS NULL");
        recurrente.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        recurrente.HasOne<Cuenta>().WithMany()
            .HasForeignKey(x => x.CuentaId).OnDelete(DeleteBehavior.Restrict);
        recurrente.HasMany(x => x.Categorias).WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "RecurrenciaCategoria",
                right => right.HasOne<Categoria>().WithMany()
                    .HasForeignKey("categoria_id").OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<MovimientoRecurrente>().WithMany()
                    .HasForeignKey("recurrencia_id").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("recurrencia_categorias", "finanzas");
                    join.HasKey("recurrencia_id", "categoria_id");
                });
        movimiento.HasOne<MovimientoRecurrente>().WithMany()
            .HasForeignKey(x => x.RecurrenciaId).OnDelete(DeleteBehavior.Restrict);

        var transferencia = builder.Entity<Transferencia>();
        transferencia.ToTable("transferencias", "finanzas", table =>
        {
            table.HasCheckConstraint("ck_transferencias_monto", "monto > 0");
            table.HasCheckConstraint("ck_transferencias_cuentas", "cuenta_origen_id <> cuenta_destino_id");
            table.HasCheckConstraint("ck_transferencias_moneda", "moneda = 'PYG'");
            table.HasCheckConstraint(
                "ck_transferencias_movimientos",
                "(estado = 'procesando' AND movimiento_egreso_id IS NULL AND movimiento_ingreso_id IS NULL) OR " +
                "(estado IN ('confirmada', 'anulada') AND movimiento_egreso_id IS NOT NULL AND movimiento_ingreso_id IS NOT NULL)");
        });
        transferencia.HasKey(x => x.Id);
        transferencia.Property(x => x.Id).HasColumnName("id");
        transferencia.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        transferencia.Property(x => x.CuentaOrigenId).HasColumnName("cuenta_origen_id");
        transferencia.Property(x => x.CuentaDestinoId).HasColumnName("cuenta_destino_id");
        transferencia.Property(x => x.Monto).HasColumnName("monto");
        transferencia.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        transferencia.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300);
        transferencia.Property(x => x.Fecha).HasColumnName("fecha");
        transferencia.Property(x => x.MovimientoEgresoId).HasColumnName("movimiento_egreso_id");
        transferencia.Property(x => x.MovimientoIngresoId).HasColumnName("movimiento_ingreso_id");
        transferencia.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        transferencia.Property(x => x.AnuladaEn).HasColumnName("anulada_en");
        transferencia.Property(x => x.HashIdempotencia).HasColumnName("hash_idempotencia").HasMaxLength(64);
        ConfigurarMutable(transferencia);
        transferencia.HasIndex(x => new { x.UsuarioId, x.HashIdempotencia }).IsUnique();
        transferencia.HasIndex(x => new { x.UsuarioId, x.Fecha, x.Id });
        transferencia.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        transferencia.HasOne<Cuenta>().WithMany().HasForeignKey(x => x.CuentaOrigenId).OnDelete(DeleteBehavior.Restrict);
        transferencia.HasOne<Cuenta>().WithMany().HasForeignKey(x => x.CuentaDestinoId).OnDelete(DeleteBehavior.Restrict);
        movimiento.HasOne<Transferencia>().WithMany()
            .HasForeignKey(x => x.TransferenciaId).OnDelete(DeleteBehavior.Restrict);

        var presupuesto = builder.Entity<Presupuesto>();
        presupuesto.ToTable("presupuestos", "finanzas", table =>
        {
            table.HasCheckConstraint("ck_presupuestos_monto", "monto > 0");
            table.HasCheckConstraint("ck_presupuestos_moneda", "moneda = 'PYG'");
            table.HasCheckConstraint(
                "ck_presupuestos_periodo",
                "periodo IN ('semanal', 'mensual', 'anual')");
        });
        presupuesto.HasKey(x => x.Id);
        presupuesto.Property(x => x.Id).HasColumnName("id");
        presupuesto.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        presupuesto.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
        presupuesto.Property(x => x.Monto).HasColumnName("monto");
        presupuesto.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        presupuesto.Property(x => x.Periodo).HasColumnName("periodo").HasMaxLength(20);
        presupuesto.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        presupuesto.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(presupuesto);
        presupuesto.HasIndex(x => new { x.UsuarioId, x.Estado });
        presupuesto.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        presupuesto.HasMany(x => x.Categorias).WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "PresupuestoCategoria",
                right => right.HasOne<Categoria>().WithMany()
                    .HasForeignKey("categoria_id").OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<Presupuesto>().WithMany()
                    .HasForeignKey("presupuesto_id").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("presupuesto_categorias", "finanzas");
                    join.HasKey("presupuesto_id", "categoria_id");
                });

        var meta = builder.Entity<MetaAhorro>();
        meta.ToTable("metas_ahorro", "finanzas", table =>
        {
            table.HasCheckConstraint("ck_metas_ahorro_monto", "monto_objetivo > 0");
            table.HasCheckConstraint("ck_metas_ahorro_moneda", "moneda = 'PYG'");
            table.HasCheckConstraint(
                "ck_metas_ahorro_propietario",
                "(ambito = 'privado' AND usuario_id IS NOT NULL AND grupo_familiar_id IS NULL) OR " +
                "(ambito = 'familiar' AND usuario_id IS NULL AND grupo_familiar_id IS NOT NULL)");
        });
        meta.HasKey(x => x.Id);
        meta.Property(x => x.Id).HasColumnName("id");
        meta.Property(x => x.Ambito).HasColumnName("ambito").HasMaxLength(20);
        meta.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        meta.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        meta.Property(x => x.CreadoPor).HasColumnName("creado_por");
        meta.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
        meta.Property(x => x.MontoObjetivo).HasColumnName("monto_objetivo");
        meta.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        meta.Property(x => x.FechaObjetivo).HasColumnName("fecha_objetivo");
        meta.Property(x => x.CuentaId).HasColumnName("cuenta_id");
        meta.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        meta.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(meta);
        meta.HasIndex(x => new { x.UsuarioId, x.Estado });
        meta.HasIndex(x => new { x.GrupoFamiliarId, x.Estado });
        meta.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        meta.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.CreadoPor).OnDelete(DeleteBehavior.Restrict);
        meta.HasOne<GrupoFamiliar>().WithMany()
            .HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Cascade);
        meta.HasOne<Cuenta>().WithMany()
            .HasForeignKey(x => x.CuentaId).OnDelete(DeleteBehavior.Restrict);

        var aporte = builder.Entity<AporteMeta>();
        aporte.ToTable("aportes_meta", "finanzas", table =>
            table.HasCheckConstraint("ck_aportes_meta_monto", "monto > 0"));
        aporte.HasKey(x => x.Id);
        aporte.Property(x => x.Id).HasColumnName("id");
        aporte.Property(x => x.MetaAhorroId).HasColumnName("meta_id");
        aporte.Property(x => x.Monto).HasColumnName("monto");
        aporte.Property(x => x.CuentaOrigenId).HasColumnName("cuenta_origen_id");
        aporte.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300);
        aporte.Property(x => x.AportadoPor).HasColumnName("aportado_por");
        aporte.Property(x => x.Fecha).HasColumnName("fecha");
        aporte.Property(x => x.HashIdempotencia)
            .HasColumnName("hash_idempotencia").HasMaxLength(64);
        aporte.Property(x => x.CreadoEn).HasColumnName("creado_en");
        aporte.HasIndex(x => new { x.MetaAhorroId, x.Fecha, x.Id });
        aporte.HasIndex(x => new { x.MetaAhorroId, x.HashIdempotencia }).IsUnique();
        aporte.HasOne<MetaAhorro>().WithMany()
            .HasForeignKey(x => x.MetaAhorroId).OnDelete(DeleteBehavior.Restrict);
        aporte.HasOne<Cuenta>().WithMany()
            .HasForeignKey(x => x.CuentaOrigenId).OnDelete(DeleteBehavior.Restrict);
        aporte.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.AportadoPor).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarAnalitica(ModelBuilder builder)
    {
        var alerta = builder.Entity<AlertaFinanciera>();
        alerta.ToTable("alertas", "analitica");
        alerta.HasKey(x => x.Id);
        alerta.Property(x => x.Id).HasColumnName("id");
        alerta.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        alerta.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(50);
        alerta.Property(x => x.Nivel).HasColumnName("nivel").HasMaxLength(20);
        alerta.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(160);
        alerta.Property(x => x.Mensaje).HasColumnName("mensaje").HasMaxLength(500);
        alerta.Property(x => x.QueOcurrio).HasColumnName("que_ocurrio").HasMaxLength(500);
        alerta.Property(x => x.DatosUtilizados).HasColumnName("datos_utilizados").HasMaxLength(500);
        alerta.Property(x => x.Impacto).HasColumnName("impacto").HasMaxLength(500);
        alerta.Property(x => x.Recomendacion).HasColumnName("recomendacion").HasMaxLength(500);
        alerta.Property(x => x.ClaveDeduplicacion).HasColumnName("clave_deduplicacion").HasMaxLength(160);
        alerta.Property(x => x.Leida).HasColumnName("leida");
        alerta.Property(x => x.LeidaEn).HasColumnName("leida_en");
        alerta.Property(x => x.Archivada).HasColumnName("archivada");
        alerta.Property(x => x.ArchivadaEn).HasColumnName("archivada_en");
        ConfigurarMutable(alerta);
        alerta.HasIndex(x => new { x.UsuarioId, x.ClaveDeduplicacion }).IsUnique();
        alerta.HasIndex(x => new { x.UsuarioId, x.CreadoEn });
        alerta.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarDocumentos(ModelBuilder builder)
    {
        var documento = builder.Entity<DocumentoFinanciero>();
        documento.ToTable("archivos", "documentos", table =>
            table.HasCheckConstraint("ck_archivos_tamano", "tamano_bytes > 0 AND tamano_bytes <= 10485760"));
        documento.HasKey(x => x.Id);
        documento.Property(x => x.Id).HasColumnName("id");
        documento.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        documento.Property(x => x.Ambito).HasColumnName("ambito").HasMaxLength(20);
        documento.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        documento.Property(x => x.ClaveObjeto).HasColumnName("clave_objeto").HasMaxLength(300);
        documento.Property(x => x.NombreOriginal).HasColumnName("nombre_original").HasMaxLength(255);
        documento.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20);
        documento.Property(x => x.MimeType).HasColumnName("mime").HasMaxLength(100);
        documento.Property(x => x.TamanoBytes).HasColumnName("tamano_bytes");
        documento.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(64);
        documento.Property(x => x.HashIdempotencia).HasColumnName("hash_idempotencia").HasMaxLength(64);
        documento.Property(x => x.EstadoArchivo).HasColumnName("estado").HasMaxLength(20);
        documento.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(documento);
        documento.HasIndex(x => x.ClaveObjeto).IsUnique();
        documento.HasIndex(x => new { x.UsuarioId, x.Sha256 });
        documento.HasIndex(x => new { x.UsuarioId, x.HashIdempotencia }).IsUnique()
            .HasFilter("eliminado_en IS NULL");
        documento.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        documento.HasOne<GrupoFamiliar>().WithMany().HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Restrict);

        var proceso = builder.Entity<ProcesamientoDocumental>();
        proceso.ToTable("procesamientos", "documentos", table =>
            table.HasCheckConstraint("ck_procesamientos_confianza", "confianza IS NULL OR (confianza >= 0 AND confianza <= 1)"));
        proceso.HasKey(x => x.Id);
        proceso.Property(x => x.Id).HasColumnName("id");
        proceso.Property(x => x.DocumentoId).HasColumnName("archivo_id");
        proceso.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20);
        proceso.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        proceso.Property(x => x.Proveedor).HasColumnName("proveedor").HasMaxLength(50);
        proceso.Property(x => x.VersionModelo).HasColumnName("version_modelo").HasMaxLength(50);
        proceso.Property(x => x.Confianza).HasColumnName("confianza");
        proceso.Property(x => x.DatosDetectados).HasColumnName("resultado").HasColumnType("jsonb");
        proceso.Property(x => x.Advertencias).HasColumnName("advertencias").HasColumnType("text[]");
        proceso.Property(x => x.IniciadoEn).HasColumnName("iniciado_en");
        proceso.Property(x => x.FinalizadoEn).HasColumnName("completado_en");
        proceso.Property(x => x.HashIdempotencia).HasColumnName("hash_idempotencia").HasMaxLength(64);
        ConfigurarMutable(proceso);
        proceso.HasIndex(x => new { x.DocumentoId, x.HashIdempotencia }).IsUnique();
        proceso.HasIndex(x => new { x.Estado, x.CreadoEn });
        proceso.HasOne<DocumentoFinanciero>().WithMany().HasForeignKey(x => x.DocumentoId).OnDelete(DeleteBehavior.Cascade);

        var exportacion = builder.Entity<Exportacion>();
        exportacion.ToTable("exportaciones", "documentos");
        exportacion.HasKey(x => x.Id);
        exportacion.Property(x => x.Id).HasColumnName("id");
        exportacion.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        exportacion.Property(x => x.Ambito).HasColumnName("ambito").HasMaxLength(20);
        exportacion.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        exportacion.Property(x => x.Formato).HasColumnName("formato").HasMaxLength(10);
        exportacion.Property(x => x.Desde).HasColumnName("desde");
        exportacion.Property(x => x.Hasta).HasColumnName("hasta");
        exportacion.Property(x => x.TipoMovimiento).HasColumnName("tipo_movimiento").HasMaxLength(10);
        exportacion.Property(x => x.CategoriaId).HasColumnName("categoria_id");
        exportacion.Property(x => x.CuentaId).HasColumnName("cuenta_id");
        exportacion.Property(x => x.Documento).HasColumnName("documento").HasMaxLength(20);
        exportacion.Property(x => x.HashSolicitud).HasColumnName("hash_solicitud").HasMaxLength(64);
        exportacion.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        exportacion.Property(x => x.ClaveObjeto).HasColumnName("clave_objeto").HasMaxLength(300);
        exportacion.Property(x => x.FinalizadoEn).HasColumnName("finalizado_en");
        exportacion.Property(x => x.CantidadMovimientos).HasColumnName("cantidad_movimientos");
        exportacion.Property(x => x.TotalIngresos).HasColumnName("total_ingresos");
        exportacion.Property(x => x.TotalGastos).HasColumnName("total_gastos");
        exportacion.Property(x => x.TotalTransferido).HasColumnName("total_transferido");
        exportacion.Property(x => x.ExpiraEn).HasColumnName("expira_en");
        exportacion.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(exportacion);
        exportacion.HasIndex(x => new { x.UsuarioId, x.HashSolicitud }).IsUnique()
            .HasFilter("eliminado_en IS NULL");
        exportacion.HasIndex(x => new { x.Estado, x.CreadoEn });
        exportacion.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        exportacion.HasOne<GrupoFamiliar>().WithMany().HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Movimiento>().HasOne<DocumentoFinanciero>().WithMany()
            .HasForeignKey(x => x.DocumentoId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarSeguridadYAuditoria(ModelBuilder builder)
    {
        var dispositivo = builder.Entity<Dispositivo>();
        dispositivo.ToTable("dispositivos", "seguridad");
        dispositivo.HasKey(x => x.Id);
        dispositivo.Property(x => x.Id).HasColumnName("id");
        dispositivo.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        dispositivo.Property(x => x.IdentificadorInstalacion).HasColumnName("identificador_instalacion").HasMaxLength(200);
        dispositivo.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
        dispositivo.Property(x => x.Plataforma).HasColumnName("plataforma").HasMaxLength(20);
        dispositivo.Property(x => x.VersionSistema).HasColumnName("version_so").HasMaxLength(50);
        dispositivo.Property(x => x.VersionAplicacion).HasColumnName("version_app").HasMaxLength(50);
        dispositivo.Property(x => x.TokenPushProtegido).HasColumnName("push_token_cifrado").HasMaxLength(6000);
        dispositivo.Property(x => x.ZonaHoraria).HasColumnName("zona_horaria").HasMaxLength(80);
        dispositivo.Property(x => x.Confiable).HasColumnName("confiable");
        dispositivo.Property(x => x.UltimoAccesoEn).HasColumnName("ultimo_acceso_en");
        dispositivo.Property(x => x.RevocadoEn).HasColumnName("revocado_en");
        ConfigurarMutable(dispositivo);
        dispositivo.HasIndex(x => new { x.UsuarioId, x.IdentificadorInstalacion })
            .IsUnique().HasFilter("revocado_en IS NULL");
        dispositivo.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);

        var seguridad = builder.Entity<EventoSeguridad>();
        seguridad.ToTable("eventos", "seguridad");
        seguridad.HasKey(x => x.Id);
        seguridad.Property(x => x.Id).HasColumnName("id");
        seguridad.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        seguridad.Property(x => x.DispositivoId).HasColumnName("dispositivo_id");
        seguridad.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(80);
        seguridad.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
        seguridad.Property(x => x.Exitoso).HasColumnName("exitoso");
        seguridad.Property(x => x.OrigenAproximado).HasColumnName("origen_aproximado").HasMaxLength(120);
        seguridad.Property(x => x.Dispositivo).HasColumnName("dispositivo").HasMaxLength(120);
        seguridad.Property(x => x.OcurridoEn).HasColumnName("ocurrido_en");
        seguridad.Property(x => x.CreadoEn).HasColumnName("creado_en");
        seguridad.HasIndex(x => new { x.UsuarioId, x.OcurridoEn });
        seguridad.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        seguridad.HasOne<Dispositivo>().WithMany().HasForeignKey(x => x.DispositivoId).OnDelete(DeleteBehavior.SetNull);

        var auditoria = builder.Entity<EventoAuditoria>();
        auditoria.ToTable("eventos", "auditoria");
        auditoria.HasKey(x => x.Id);
        auditoria.Property(x => x.Id).HasColumnName("id");
        auditoria.Property(x => x.UsuarioId).HasColumnName("actor_usuario_id");
        auditoria.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        auditoria.Property(x => x.Accion).HasColumnName("accion").HasMaxLength(100);
        auditoria.Property(x => x.Recurso).HasColumnName("recurso_tipo").HasMaxLength(80);
        auditoria.Property(x => x.RecursoId).HasColumnName("recurso_id");
        auditoria.Property(x => x.DatosAnteriores).HasColumnName("datos_anteriores").HasColumnType("jsonb");
        auditoria.Property(x => x.DatosPosteriores).HasColumnName("datos_nuevos").HasColumnType("jsonb");
        auditoria.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        auditoria.Property(x => x.OcurridoEn).HasColumnName("ocurrido_en");
        auditoria.Property(x => x.CreadoEn).HasColumnName("creado_en");
        auditoria.HasIndex(x => new { x.UsuarioId, x.OcurridoEn });
        auditoria.HasIndex(x => new { x.Recurso, x.RecursoId, x.OcurridoEn });
        auditoria.HasIndex(x => x.CorrelationId);
        auditoria.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
        auditoria.HasOne<GrupoFamiliar>().WithMany().HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigurarFamilias(ModelBuilder builder)
    {
        var grupo = builder.Entity<GrupoFamiliar>();
        grupo.ToTable("grupos_familiares", "familias");
        grupo.HasKey(x => x.Id);
        grupo.Property(x => x.Id).HasColumnName("id");
        grupo.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
        grupo.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(grupo);

        var integrante = builder.Entity<IntegranteFamiliar>();
        integrante.ToTable("integrantes", "familias");
        integrante.HasKey(x => x.Id);
        integrante.Property(x => x.Id).HasColumnName("id");
        integrante.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        integrante.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        integrante.Property(x => x.Rol).HasColumnName("rol").HasMaxLength(20);
        integrante.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(integrante);
        integrante.HasIndex(x => new { x.GrupoFamiliarId, x.UsuarioId })
            .IsUnique().HasFilter("eliminado_en IS NULL");
        integrante.HasOne<GrupoFamiliar>().WithMany()
            .HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Cascade);
        integrante.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        var invitacion = builder.Entity<InvitacionFamiliar>();
        invitacion.ToTable("invitaciones", "familias");
        invitacion.HasKey(x => x.Id);
        invitacion.Property(x => x.Id).HasColumnName("id");
        invitacion.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        invitacion.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(320);
        invitacion.Property(x => x.UsuarioDestinoId).HasColumnName("usuario_destino_id");
        invitacion.Property(x => x.Rol).HasColumnName("rol").HasMaxLength(20);
        invitacion.Property(x => x.HashToken).HasColumnName("hash_token").HasMaxLength(64);
        invitacion.Property(x => x.HashCodigo).HasColumnName("hash_codigo").HasMaxLength(64);
        invitacion.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        invitacion.Property(x => x.ExpiraEn).HasColumnName("expira_en");
        ConfigurarMutable(invitacion);
        invitacion.HasIndex(x => x.HashToken).IsUnique();
        invitacion.HasOne<GrupoFamiliar>().WithMany()
            .HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Cascade);

        var cuenta = builder.Entity<CuentaCompartida>();
        cuenta.ToTable("cuentas_compartidas", "familias");
        cuenta.HasKey(x => x.Id);
        cuenta.Property(x => x.Id).HasColumnName("id");
        cuenta.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        cuenta.Property(x => x.CuentaId).HasColumnName("cuenta_id");
        cuenta.Property(x => x.CompartidaPorUsuarioId).HasColumnName("compartida_por_usuario_id");
        cuenta.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(cuenta);
        cuenta.HasIndex(x => new { x.GrupoFamiliarId, x.CuentaId })
            .IsUnique().HasFilter("eliminado_en IS NULL");
        cuenta.HasOne<GrupoFamiliar>().WithMany()
            .HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Cascade);
        cuenta.HasOne<Cuenta>().WithMany()
            .HasForeignKey(x => x.CuentaId).OnDelete(DeleteBehavior.Restrict);

        var categoria = builder.Entity<CategoriaFamiliar>();
        categoria.ToTable("categorias", "familias");
        categoria.HasKey(x => x.Id).HasName("PK_familias_categorias");
        categoria.Property(x => x.Id).HasColumnName("id");
        categoria.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        categoria.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100);
        categoria.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20);
        categoria.Property(x => x.Icono).HasColumnName("icono").HasMaxLength(50);
        categoria.Property(x => x.Color).HasColumnName("color").HasMaxLength(7);
        categoria.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(categoria);
        categoria.HasIndex(x => new { x.GrupoFamiliarId, x.Nombre })
            .IsUnique().HasFilter("eliminado_en IS NULL");
        categoria.HasOne<GrupoFamiliar>().WithMany()
            .HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Cascade);

        var movimiento = builder.Entity<MovimientoFamiliar>();
        movimiento.ToTable("movimientos", "familias", table =>
        {
            table.HasCheckConstraint("ck_movimientos_familiares_monto", "monto > 0");
        });
        movimiento.HasKey(x => x.Id).HasName("PK_familias_movimientos");
        movimiento.Property(x => x.Id).HasColumnName("id");
        movimiento.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        movimiento.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        movimiento.Property(x => x.CuentaId).HasColumnName("cuenta_id");
        movimiento.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(10);
        movimiento.Property(x => x.Monto).HasColumnName("monto");
        movimiento.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300);
        movimiento.Property(x => x.Fecha).HasColumnName("fecha");
        movimiento.Property(x => x.CategoriaIds).HasColumnName("categoria_ids");
        movimiento.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        ConfigurarMutable(movimiento);
        movimiento.HasIndex(x => new { x.GrupoFamiliarId, x.Fecha, x.Id });
        movimiento.HasIndex(x => x.CuentaId)
            .HasDatabaseName("IX_familias_movimientos_cuenta_id");
        movimiento.HasOne<GrupoFamiliar>().WithMany()
            .HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Cascade);
        movimiento.HasOne<Cuenta>().WithMany()
            .HasForeignKey(x => x.CuentaId).OnDelete(DeleteBehavior.Restrict);

        var caja = builder.Entity<CajaCompartida>();
        caja.ToTable("cajas_compartidas", "familias");
        caja.HasKey(x => x.Id);
        caja.Property(x => x.Id).HasColumnName("id");
        caja.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        caja.Property(x => x.Saldo).HasColumnName("saldo");
        ConfigurarMutable(caja);
        caja.HasIndex(x => x.GrupoFamiliarId).IsUnique();
        caja.HasOne<GrupoFamiliar>().WithOne()
            .HasForeignKey<CajaCompartida>(x => x.GrupoFamiliarId)
            .OnDelete(DeleteBehavior.Cascade);

        var operacion = builder.Entity<OperacionCaja>();
        operacion.ToTable("operaciones_caja", "familias");
        operacion.HasKey(x => x.Id);
        operacion.Property(x => x.Id).HasColumnName("id");
        operacion.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        operacion.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        operacion.Property(x => x.CuentaPrivadaId).HasColumnName("cuenta_privada_id");
        operacion.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(10);
        operacion.Property(x => x.Monto).HasColumnName("monto");
        operacion.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300);
        operacion.Property(x => x.SaldoAnterior).HasColumnName("saldo_anterior");
        operacion.Property(x => x.SaldoPosterior).HasColumnName("saldo_posterior");
        operacion.Property(x => x.CreadoEn).HasColumnName("creado_en");
        operacion.HasOne<GrupoFamiliar>().WithMany()
            .HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Cascade);
        operacion.HasOne<Cuenta>().WithMany()
            .HasForeignKey(x => x.CuentaPrivadaId).OnDelete(DeleteBehavior.Restrict);

        var presupuesto = builder.Entity<PresupuestoFamiliar>();
        presupuesto.ToTable("presupuestos", "familias");
        presupuesto.HasKey(x => x.Id);
        presupuesto.Property(x => x.Id).HasColumnName("id");
        presupuesto.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        presupuesto.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
        presupuesto.Property(x => x.Monto).HasColumnName("monto");
        presupuesto.Property(x => x.Periodo).HasColumnName("periodo").HasMaxLength(20);
        presupuesto.Property(x => x.CategoriaIds).HasColumnName("categoria_ids");
        presupuesto.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        ConfigurarMutable(presupuesto);
        presupuesto.HasOne<GrupoFamiliar>().WithMany()
            .HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Cascade);

        var eliminacion = builder.Entity<EliminacionGrupoFamiliar>();
        eliminacion.ToTable("eliminaciones", "familias");
        eliminacion.HasKey(x => x.Id);
        eliminacion.Property(x => x.Id).HasColumnName("id");
        eliminacion.Property(x => x.GrupoFamiliarId).HasColumnName("grupo_familiar_id");
        eliminacion.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        eliminacion.Property(x => x.CompletadoEn).HasColumnName("completado_en");
        eliminacion.Property(x => x.ErrorCodigo).HasColumnName("error_codigo").HasMaxLength(80);
        ConfigurarMutable(eliminacion);
        eliminacion.HasIndex(x => x.GrupoFamiliarId)
            .IsUnique().HasFilter("estado = 'pendiente'");
        eliminacion.HasOne<GrupoFamiliar>().WithMany()
            .HasForeignKey(x => x.GrupoFamiliarId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarSuscripciones(ModelBuilder builder)
    {
        var plan = builder.Entity<PlanSuscripcion>();
        plan.ToTable("planes", "suscripciones");
        plan.HasKey(x => x.Id);
        plan.Property(x => x.Id).HasColumnName("id");
        plan.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(60);
        plan.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
        plan.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
        plan.Property(x => x.Precio).HasColumnName("precio");
        plan.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        plan.Property(x => x.Periodo).HasColumnName("periodicidad").HasMaxLength(20);
        plan.Property(x => x.Capacidades).HasColumnName("capacidades");
        plan.Property(x => x.Destacado).HasColumnName("destacado");
        plan.Property(x => x.Activo).HasColumnName("activo");
        plan.Property(x => x.CreadoEn).HasColumnName("creado_en");
        plan.HasIndex(x => x.Codigo).IsUnique();

        var fechaSeed = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        plan.HasData(
            new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000101"),
                Codigo = "gratis",
                Nombre = "Gratis",
                Descripcion = "Funciones esenciales de finanzas personales.",
                Precio = 0L,
                Moneda = "PYG",
                Periodo = "sin-vencimiento",
                Capacidades = new[] { "cuentas", "categorias", "movimientos" },
                Destacado = false,
                Activo = true,
                CreadoEn = fechaSeed
            },
            new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                Codigo = "premium-mensual",
                Nombre = "Premium mensual",
                Descripcion = "Todas las capacidades premium con renovación mensual.",
                Precio = 45_000L,
                Moneda = "PYG",
                Periodo = "mensual",
                Capacidades = new[] { "ocr", "predicciones", "exportaciones", "alertas-prioritarias" },
                Destacado = true,
                Activo = true,
                CreadoEn = fechaSeed
            },
            new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000103"),
                Codigo = "premium-anual",
                Nombre = "Premium anual",
                Descripcion = "Todas las capacidades premium con renovación anual.",
                Precio = 450_000L,
                Moneda = "PYG",
                Periodo = "anual",
                Capacidades = new[] { "ocr", "predicciones", "exportaciones", "alertas-prioritarias" },
                Destacado = false,
                Activo = true,
                CreadoEn = fechaSeed
            });

        var suscripcion = builder.Entity<Suscripcion>();
        suscripcion.ToTable("suscripciones", "suscripciones", table =>
        {
            table.HasCheckConstraint(
                "ck_suscripciones_periodo",
                "fin_periodo_en > iniciada_en");
        });
        suscripcion.HasKey(x => x.Id);
        suscripcion.Property(x => x.Id).HasColumnName("id");
        suscripcion.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        suscripcion.Property(x => x.PlanId).HasColumnName("plan_id");
        suscripcion.Property(x => x.Proveedor).HasColumnName("proveedor").HasMaxLength(30);
        suscripcion.Property(x => x.HashComprobante).HasColumnName("hash_comprobante").HasMaxLength(64);
        suscripcion.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        suscripcion.Property(x => x.IniciadaEn).HasColumnName("iniciada_en");
        suscripcion.Property(x => x.CanceladaEn).HasColumnName("cancelada_en");
        suscripcion.Property(x => x.FinPeriodoEn).HasColumnName("fin_periodo_en");
        suscripcion.Property(x => x.MotivoCancelacion).HasColumnName("motivo_cancelacion").HasMaxLength(300);
        suscripcion.Property(x => x.SuscripcionAnteriorId).HasColumnName("suscripcion_anterior_id");
        suscripcion.Property(x => x.ReemplazadaPorId).HasColumnName("reemplazada_por_id");
        ConfigurarMutable(suscripcion);
        suscripcion.HasIndex(x => new { x.Proveedor, x.HashComprobante }).IsUnique();
        suscripcion.HasIndex(x => x.UsuarioId).IsUnique()
            .HasFilter("estado IN ('activa', 'en_gracia')");
        suscripcion.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        suscripcion.HasOne<PlanSuscripcion>().WithMany()
            .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);

        var transaccion = builder.Entity<TransaccionSuscripcion>();
        transaccion.ToTable("transacciones", "suscripciones");
        transaccion.HasKey(x => x.Id);
        transaccion.Property(x => x.Id).HasColumnName("id");
        transaccion.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        transaccion.Property(x => x.SuscripcionId).HasColumnName("suscripcion_id");
        transaccion.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(30);
        transaccion.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(30);
        transaccion.Property(x => x.Proveedor).HasColumnName("proveedor").HasMaxLength(30);
        transaccion.Property(x => x.ReferenciaExternaHash).HasColumnName("referencia_externa_hash").HasMaxLength(64);
        transaccion.Property(x => x.Monto).HasColumnName("monto");
        transaccion.Property(x => x.Moneda).HasColumnName("moneda").HasMaxLength(3);
        transaccion.Property(x => x.OcurridoEn).HasColumnName("ocurrido_en");
        transaccion.Property(x => x.CreadoEn).HasColumnName("creado_en");
        transaccion.HasIndex(x => new { x.Proveedor, x.ReferenciaExternaHash, x.Tipo }).IsUnique();
        transaccion.HasIndex(x => new { x.UsuarioId, x.OcurridoEn });
        transaccion.HasOne<Suscripcion>().WithMany().HasForeignKey(x => x.SuscripcionId)
            .OnDelete(DeleteBehavior.Restrict);

        var aviso = builder.Entity<AvisoSuscripcion>();
        aviso.ToTable("avisos", "suscripciones");
        aviso.HasKey(x => x.Id);
        aviso.Property(x => x.Id).HasColumnName("id");
        aviso.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        aviso.Property(x => x.SuscripcionId).HasColumnName("suscripcion_id");
        aviso.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(30);
        aviso.Property(x => x.PeriodoFinEn).HasColumnName("periodo_fin_en");
        aviso.Property(x => x.GeneradoEn).HasColumnName("generado_en");
        aviso.Property(x => x.CreadoEn).HasColumnName("creado_en");
        aviso.HasIndex(x => new { x.SuscripcionId, x.Tipo, x.PeriodoFinEn }).IsUnique();
        aviso.HasOne<Suscripcion>().WithMany().HasForeignKey(x => x.SuscripcionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarInfraestructura(ModelBuilder builder)
    {
        var evento = builder.Entity<EventoOutbox>();
        evento.ToTable("outbox_eventos", "infra");
        evento.HasKey(x => x.Id);
        evento.Property(x => x.Id).HasColumnName("id");
        evento.Property(x => x.Tipo).HasColumnName("tipo");
        evento.Property(x => x.AgregadoTipo).HasColumnName("agregado_tipo");
        evento.Property(x => x.AgregadoId).HasColumnName("agregado_id");
        evento.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
        evento.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        evento.Property(x => x.DisponibleEn).HasColumnName("disponible_en");
        evento.Property(x => x.ProcesadoEn).HasColumnName("procesado_en");
        evento.Property(x => x.Intentos).HasColumnName("intentos");
        evento.Property(x => x.UltimoError).HasColumnName("ultimo_error").HasMaxLength(1000);
        evento.Property(x => x.Estado).HasColumnName("estado");
        evento.Property(x => x.CreadoEn).HasColumnName("ocurrido_en");
        evento.HasIndex(x => new { x.Estado, x.DisponibleEn });

        var entrega = builder.Entity<EntregaOutbox>();
        entrega.ToTable("outbox_entregas", "infra");
        entrega.HasKey(x => x.Id);
        entrega.Property(x => x.Id).HasColumnName("id");
        entrega.Property(x => x.EventoOutboxId).HasColumnName("evento_outbox_id");
        entrega.Property(x => x.Canal).HasColumnName("canal").HasMaxLength(30);
        entrega.Property(x => x.DestinatarioHash).HasColumnName("destinatario_hash").HasMaxLength(64);
        entrega.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        entrega.Property(x => x.ReservadaEn).HasColumnName("reservada_en");
        entrega.Property(x => x.EnviadaEn).HasColumnName("enviada_en");
        entrega.Property(x => x.CreadoEn).HasColumnName("creado_en");
        entrega.HasIndex(x => new { x.EventoOutboxId, x.Canal }).IsUnique();
        entrega.HasIndex(x => new { x.Canal, x.ReservadaEn });
        entrega.HasOne<EventoOutbox>().WithMany().HasForeignKey(x => x.EventoOutboxId)
            .OnDelete(DeleteBehavior.Cascade);

        var idempotencia = builder.Entity<Idempotencia>();
        idempotencia.ToTable("idempotencias", "infra");
        idempotencia.HasKey(x => x.Id);
        idempotencia.Property(x => x.Id).HasColumnName("id");
        idempotencia.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        idempotencia.Property(x => x.Clave).HasColumnName("clave").HasMaxLength(100);
        idempotencia.Property(x => x.Metodo).HasColumnName("metodo").HasMaxLength(10);
        idempotencia.Property(x => x.Ruta).HasColumnName("ruta").HasMaxLength(300);
        idempotencia.Property(x => x.HashSolicitud).HasColumnName("hash_solicitud").HasMaxLength(64);
        idempotencia.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        idempotencia.Property(x => x.CodigoRespuesta).HasColumnName("codigo_respuesta");
        idempotencia.Property(x => x.CuerpoRespuesta).HasColumnName("cuerpo_respuesta").HasColumnType("jsonb");
        idempotencia.Property(x => x.ExpiraEn).HasColumnName("expira_en");
        idempotencia.Property(x => x.CreadoEn).HasColumnName("creado_en");
        idempotencia.HasIndex(x => new { x.UsuarioId, x.Clave }).IsUnique().HasFilter("usuario_id IS NOT NULL");
    }

    private static void ConfigurarMutable<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : FinanzasInteligentes.BuildingBlocks.MutableEntity
    {
        entity.Property(x => x.CreadoEn).HasColumnName("creado_en");
        entity.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
        entity.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}
