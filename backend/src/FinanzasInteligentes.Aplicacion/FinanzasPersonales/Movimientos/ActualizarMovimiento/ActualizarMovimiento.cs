using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.ActualizarMovimiento;

public sealed record ActualizarMovimientoCommand(
    Guid UsuarioId,
    Guid MovimientoId,
    long VersionEsperada,
    string? Descripcion,
    IReadOnlyCollection<Guid>? CategoriaIds,
    Guid? DocumentoId,
    string CorrelationId);

public sealed class ActualizarMovimientoHandler(
    IFinanzasRepository finanzas,
    IDocumentosRepository documentos,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<MovimientoResponse> Handle(
        ActualizarMovimientoCommand command,
        CancellationToken cancellationToken)
    {
        var movimiento = await finanzas.ObtenerMovimiento(
            command.UsuarioId, command.MovimientoId, false, cancellationToken)
            ?? throw new NotFoundException(
                "movimiento_no_encontrado", "El movimiento no existe.");
        if (movimiento.Version != command.VersionEsperada)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión del movimiento está desactualizada.");

        IReadOnlyCollection<Categoria>? categorias = null;
        if (command.CategoriaIds is not null)
        {
            var ids = command.CategoriaIds.Distinct().ToArray();
            categorias = await finanzas.ObtenerCategorias(
                command.UsuarioId, ids, cancellationToken);
            if (categorias.Count != ids.Length)
                throw new NotFoundException(
                    "categoria_no_encontrada", "Una o más categorías no existen.");
        }

        if (command.DocumentoId is not null &&
            await documentos.ObtenerDocumento(
                command.UsuarioId, command.DocumentoId.Value, true, cancellationToken) is null)
            throw new NotFoundException(
                "documento_no_encontrado", "El comprobante no existe.");

        movimiento.Actualizar(command.Descripcion, categorias, command.DocumentoId);
        finanzas.Agregar(EventoOutbox.Crear(
            "movimiento.actualizado", "movimiento", movimiento.Id,
            new { movimiento.Id }, command.CorrelationId));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return movimiento.ToResponse();
    }
}
