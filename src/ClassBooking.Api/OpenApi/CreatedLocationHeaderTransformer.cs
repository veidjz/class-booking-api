using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ClassBooking.Api.OpenApi;

/// <summary>
/// Declares the "Location" header on every 201, which the contract makes normative and which is
/// the only place a client reads the identifier of what it just created.
/// </summary>
internal sealed class CreatedLocationHeaderTransformer : IOpenApiOperationTransformer
{
  private const string CreatedStatus = "201";

  public Task TransformAsync(
      OpenApiOperation operation,
      OpenApiOperationTransformerContext context,
      CancellationToken cancellationToken)
  {
    if (operation.Responses?.TryGetValue(CreatedStatus, out IOpenApiResponse? response) is true
        && response is OpenApiResponse created)
    {
      created.Headers = new Dictionary<string, IOpenApiHeader>
      {
        ["Location"] = new OpenApiHeader
        {
          Description = "Identifier of the created resource.",
          Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        },
      };
    }

    return Task.CompletedTask;
  }
}
