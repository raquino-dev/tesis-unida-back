using FinanzasInteligentes.Infraestructura.Procesamiento.Suscripciones;

namespace FinanzasInteligentes.Worker.Suscripciones;

public sealed class SuscripcionesWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SuscripcionesWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<ISuscripcionesProcessor>();
                var cantidad = await processor.ProcesarAvisos(stoppingToken);
                if (cantidad > 0)
                    logger.LogInformation("Se generaron {Cantidad} avisos de suscripción", cantidad);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Error procesando avisos de suscripción");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
