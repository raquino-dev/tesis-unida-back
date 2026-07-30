using FinanzasInteligentes.Aplicacion.Abstracciones;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinanzasInteligentes.Infraestructura.Persistencia;

public sealed class UnidadDeTrabajo(FinanzasDbContext db) : IUnidadDeTrabajo
{
    public Task<int> GuardarCambios(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<ITransaccionAplicacion> IniciarTransaccion(CancellationToken cancellationToken) =>
        new TransaccionAplicacion(await db.Database.BeginTransactionAsync(cancellationToken));

    private sealed class TransaccionAplicacion(IDbContextTransaction transaction) : ITransaccionAplicacion
    {
        public Task Confirmar(CancellationToken cancellationToken) =>
            transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}