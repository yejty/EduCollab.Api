using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using DataTypeAttribute = System.ComponentModel.DataAnnotations.DataTypeAttribute;
using PasswordDataType = System.ComponentModel.DataAnnotations.DataType;

namespace EduCollab.Api.Swagger
{
    public sealed class PasswordExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties.Count == 0)
                return;

            foreach (var property in context.Type.GetProperties())
            {
                var isPassword =
                    property.GetCustomAttributes(typeof(DataTypeAttribute), inherit: true)
                        .OfType<DataTypeAttribute>()
                        .Any(attribute => attribute.DataType == PasswordDataType.Password);

                if (!isPassword)
                    continue;

                var jsonName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
                if (!schema.Properties.TryGetValue(jsonName, out var propertySchema))
                    continue;

                propertySchema.Example = new OpenApiString("string");
            }
        }
    }
}
