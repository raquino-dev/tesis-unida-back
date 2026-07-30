using FinanzasInteligentes.Infraestructura.Procesamiento.Analitica;

namespace FinanzasInteligentes.Worker.Analitica;

public sealed class AnaliticaWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AnaliticaWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcesarSeguro(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ProcesarSeguro(stoppingToken);
    }

    private async Task ProcesarSeguro(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IAnaliticaProcessor>();
            var cantidad = await processor.Procesar(cancellationToken);
            if (cantidad > 0)
                logger.LogInformation("Se generaron {Cantidad} alertas financieras.", cantidad);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falló el procesamiento de alertas financieras.");
        }
    }
}