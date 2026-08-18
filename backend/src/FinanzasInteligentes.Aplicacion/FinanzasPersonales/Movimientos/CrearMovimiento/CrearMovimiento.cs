using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.CrearMovimiento;

public sealed record CrearMovimientoCommand(
    Guid UsuarioId,
    string CorrelationId,
    string Ambito,
    Guid CuentaId,
    string Tipo,
    long Monto,
    string Descripcion,
    DateOnly Fecha,
    TimeOnly? Hora = null,
    IReadOnlyCollection<Guid>? CategoriaIds = null,
    Guid? DocumentoId = null,
    Guid? MovimientoRecurrenteId = null,
    Guid? GrupoFamiliarId = null,
    Guid? Id = null);

public sealed class CrearMovimientoHandler(
    IFinanzasRepository finanzas,
    IDocumentosRepository documentos,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<MovimientoResponse> Handle(CrearMovimientoCommand command, CancellationToken cancellationToken)
    {
        if (command.Id is { } requestedId &&
            await finanzas.ObtenerMovimiento(
                command.UsuarioId, requestedId, true, cancellationToken) is { } existing)
            return existing.ToResponse();

        if (command.Ambito != "privado")
            throw new DomainException("ambito_invalido", "Esta ruta sólo admite movimientos privados.");

        await using var transaction = await unidadDeTrabajo.IniciarTransaccion(cancellationToken);

        var cuenta = await finanzas.ObtenerCuenta(command.UsuarioId, command.CuentaId, false, cancellationToken)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
        if (command.DocumentoId is not null &&
            await documentos.ObtenerDocumento(
                command.UsuarioId, command.DocumentoId.Value, true, cancellationToken) is null)
            throw new NotFoundException("documento_no_encontrado", "El documento no existe.");

        var movimiento = Movimiento.Crear(
            command.UsuarioId, command.CuentaId, command.Tipo, command.Monto,
            command.Descripcion, command.Fecha, documentoId: command.DocumentoId,
            id: command.Id);

        if (command.CategoriaIds is not null)
        {
            var ids = command.CategoriaIds.Distinct().ToArray();
            var categorias = await finanzas.ObtenerCategorias(
                command.UsuarioId, ids, cancellationToken);
            if (categorias.Count != ids.Length)
                throw new NotFoundException(
                    "categoria_no_encontrada", "Una o más categorías no existen.");
            movimiento.AsignarCategoriasIniciales(categorias);
        }

        cuenta.AplicarMovimiento(command.Tipo, command.Monto);

        finanzas.Agregar(movimiento);

        finanzas.Agregar(EventoOutbox.Crear(
            "movimiento.creado",
            "movimiento",
            movimiento.Id,
            new { movimiento.Id, movimiento.CuentaId },
            command.CorrelationId));

        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        await transaction.Confirmar(cancellationToken);

        return movimiento.ToResponse();
    }
}
