using FinanzasInteligentes.Infraestructura.Procesamiento.Outbox;

namespace FinanzasInteligentes.Worker.Outbox;

public sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcesarLote(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falló el procesamiento del outbox.");
            }
        }
    }

    private async Task ProcesarLote(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

        var cantidad = await processor.ProcesarLote(cancellationToken);
        if (cantidad > 0)
            logger.LogInformation("Se procesaron {Cantidad} eventos de outbox.", cantidad);
    }
}