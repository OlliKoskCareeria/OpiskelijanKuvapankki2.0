
    using System;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

namespace OpiskelijanKuvapankki2_0.Filters
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                var enumType = context.Type;
                var enumValues = Enum.GetValues(enumType);
                var enumMemberValues = enumValues.Cast<object>()
                    .ToDictionary(
                        v => v.ToString(),
                        v => enumType.GetField(v.ToString())
                            ?.GetCustomAttributes(typeof(EnumMemberAttribute), false)
                            .Cast<EnumMemberAttribute>()
                            .FirstOrDefault()?.Value ?? v.ToString()
                    );

                schema.Enum = enumValues.Cast<object>()
                    .Select(v => new OpenApiString(enumMemberValues[v.ToString()]))
                    .Cast<IOpenApiAny>()
                    .ToList();

                // This ensures the actual value submitted is the numeric value
                schema.Type = "integer";
                schema.Format = "int32";
            }
        }
    }
}