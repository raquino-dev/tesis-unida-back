using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace FinanzasInteligentes.Api.OpenApi;

public sealed class MultipartDocumentoOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var operation = context.OperationDescription.Operation;
        if (operation.OperationId != "postDocumentosFinancieros" ||
            operation.RequestBody?.Content.TryGetValue("application/json", out var media) != true)
            return true;

        operation.RequestBody.Content.Remove("application/json");
        operation.RequestBody.Content["multipart/form-data"] = media;
        return true;
    }
}