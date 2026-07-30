using Microsoft.AspNetCore.Authorization;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace FinanzasInteligentes.Api.OpenApi;

public sealed class AllowAnonymousOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        if (context is AspNetCoreOperationProcessorContext aspNetCoreContext &&
            aspNetCoreContext.ApiDescription.ActionDescriptor.EndpointMetadata
                .OfType<IAllowAnonymous>()
                .Any())
        {
            context.OperationDescription.Operation.Security?.Clear();
        }

        return true;
    }
}