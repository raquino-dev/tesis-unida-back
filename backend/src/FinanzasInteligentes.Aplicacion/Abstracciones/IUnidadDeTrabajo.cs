namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IUnidadDeTrabajo
{
    Task<int> GuardarCambios(CancellationToken cancellationToken);

    Task<ITransaccionAplicacion> IniciarTransaccion(CancellationToken cancellationToken);
}

public interface ITransaccionAplicacion : IAsyncDisposable
{
    Task Confirmar(CancellationToken cancellationToken);
}