using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ClassBooking.Api.OpenApi;

/// <summary>
/// Restores the "type" keyword for the values a custom converter writes as text. The generator
/// reads the CLR type, not the converter, so it publishes the format without the type, and a
/// schema carrying only a format matches any JSON value.
/// </summary>
internal sealed class StringSchemaTypeTransformer : IOpenApiSchemaTransformer
{
  public Task TransformAsync(
      OpenApiSchema schema,
      OpenApiSchemaTransformerContext context,
      CancellationToken cancellationToken)
  {
    Type? nullableArgument = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type);
    Type type = nullableArgument ?? context.JsonTypeInfo.Type;

    if (schema.Type is null && (type.IsEnum || type == typeof(DateTimeOffset)))
    {
      schema.Type = nullableArgument is null
          ? JsonSchemaType.String
          : JsonSchemaType.String | JsonSchemaType.Null;
    }

    return Task.CompletedTask;
  }
}
