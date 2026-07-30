using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Identidad;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.Identidad.Privacidad;

public sealed record PoliticaPrivacidadResponse(
    Guid Id, string Version, string Titulo, string UrlDocumento, DateTimeOffset VigenteDesde);
public sealed record ConsentimientoPrivacidadResponse(
    Guid Id, Guid PoliticaId, string VersionPolitica, string Finalidad,
    DateTimeOffset AceptadoEn, DateTimeOffset? RevocadoEn);

public sealed class PrivacidadHandler(
    IIdentidadRepository identidad, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<PoliticaPrivacidadResponse> ObtenerPolitica(CancellationToken ct)
    {
        var politica = await identidad.ObtenerPoliticaVigente(ct)
            ?? throw new NotFoundException("politica_no_encontrada", "No existe una política vigente.");
        return Map(politica);
    }

    public async Task<IReadOnlyCollection<ConsentimientoPrivacidadResponse>> Listar(
        Guid usuarioId, CancellationToken ct) =>
        (await identidad.ListarConsentimientos(usuarioId, ct)).Select(Map).ToArray();

    public async Task<ConsentimientoPrivacidadResponse> Aceptar(
        Guid usuarioId, string version, string finalidad, string correlationId, CancellationToken ct)
    {
        var politica = await identidad.ObtenerPoliticaPorVersion(version, ct)
            ?? throw new NotFoundException("politica_no_encontrada", "La política no está vigente.");
        var consentimiento = ConsentimientoPrivacidad.Crear(usuarioId, politica, finalidad);
        identidad.Agregar(consentimiento);
        identidad.Agregar(EventoOutbox.Crear(
            "privacidad.consentimiento-aceptado", "consentimiento", consentimiento.Id,
            new { consentimiento.Id, usuarioId, consentimiento.VersionPolitica, consentimiento.Finalidad },
            correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(consentimiento);
    }

    public async Task Revocar(
        Guid usuarioId, Guid consentimientoId, string correlationId, CancellationToken ct)
    {
        var consentimiento = await identidad.ObtenerConsentimiento(usuarioId, consentimientoId, ct)
            ?? throw new NotFoundException("consentimiento_no_encontrado", "El consentimiento no existe.");
        consentimiento.Revocar();
        identidad.Agregar(EventoOutbox.Crear(
            "privacidad.consentimiento-revocado", "consentimiento", consentimiento.Id,
            new { consentimiento.Id, usuarioId, consentimiento.Finalidad }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    private static PoliticaPrivacidadResponse Map(PoliticaPrivacidad x) =>
        new(x.Id, x.VersionPolitica, x.Titulo, x.UrlDocumento, x.VigenteDesde);
    private static ConsentimientoPrivacidadResponse Map(ConsentimientoPrivacidad x) =>
        new(x.Id, x.PoliticaId, x.VersionPolitica, x.Finalidad, x.AceptadoEn, x.RevocadoEn);
}
