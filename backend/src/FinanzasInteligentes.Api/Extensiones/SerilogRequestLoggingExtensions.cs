using Serilog;

namespace FinanzasInteligentes.Api.Extensiones
{
    internal static class SerilogRequestLoggingExtensions
    {
        public static IApplicationBuilder UseCustomSerilogRequestLogging(this IApplicationBuilder app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                options.GetLevel = (httpContext, elapsed, ex) => ex != null || httpContext.Response.StatusCode >= 500
                    ? Serilog.Events.LogEventLevel.Error
                    : Serilog.Events.LogEventLevel.Information;
            });
            return app;
        }
    }
}