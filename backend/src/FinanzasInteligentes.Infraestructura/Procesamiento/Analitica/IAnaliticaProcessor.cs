namespace FinanzasInteligentes.Infraestructura.Procesamiento.Analitica;

public interface IAnaliticaProcessor
{
    Task<int> Procesar(CancellationToken cancellationToken);
}