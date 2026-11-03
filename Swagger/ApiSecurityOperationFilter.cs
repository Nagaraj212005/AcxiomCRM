using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AcxiomCRM.Swagger;

public sealed class ApiSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath;
        if (relativePath is null || !relativePath.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var method = context.ApiDescription.HttpMethod;
        if (method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "RequestVerificationToken",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Antiforgery request token from a signed-in MVC form.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });
        }
    }
}
