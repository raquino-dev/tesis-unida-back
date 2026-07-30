using FinanzasInteligentes.Infraestructura.Procesamiento.Recurrencias;

namespace FinanzasInteligentes.Worker.Recurrencias;

public sealed class RecurrenciasWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RecurrenciasWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await Procesar(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falló la generación de movimientos recurrentes.");
            }
        }
    }

    private async Task Procesar(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IRecurrenciasProcessor>();
        var cantidad = await processor.ProcesarVencidas(cancellationToken);
        if (cantidad > 0)
            logger.LogInformation(
                "Se procesaron {Cantidad} movimientos recurrentes.", cantidad);
    }
}