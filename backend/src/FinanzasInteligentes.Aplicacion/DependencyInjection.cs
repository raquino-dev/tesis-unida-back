using Microsoft.Extensions.DependencyInjection;

namespace FinanzasInteligentes.Aplicacion;

public static class DependencyInjection
{
    public static IServiceCollection AddAplicacion(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        var handlers = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.Name.EndsWith("Handler", StringComparison.Ordinal));
        foreach (var handler in handlers)
            services.AddScoped(handler.AsType());

        return services;
    }
}