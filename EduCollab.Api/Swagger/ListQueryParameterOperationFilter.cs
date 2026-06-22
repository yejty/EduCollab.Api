using EduCollab.Api.Query;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EduCollab.Api.Swagger
{
    public sealed class ListQueryParameterOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var parameter in operation.Parameters)
            {
                switch (parameter.Name)
                {
                    case "page":
                        parameter.Description =
                            $"1-based page index. Default: {PaginationDefaults.DefaultPage}. Invalid values return `invalid_pagination`.";
                        parameter.Required = false;
                        parameter.Schema ??= new OpenApiSchema { Type = "integer", Format = "int32" };
                        parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiInteger(PaginationDefaults.DefaultPage);
                        parameter.Schema.Minimum = 1;
                        break;
                    case "pageSize":
                        parameter.Description =
                            $"Page size. Default: {PaginationDefaults.DefaultPageSize}, maximum: {PaginationDefaults.MaxPageSize}. Invalid values return `invalid_pagination`.";
                        parameter.Required = false;
                        parameter.Schema ??= new OpenApiSchema { Type = "integer", Format = "int32" };
                        parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiInteger(PaginationDefaults.DefaultPageSize);
                        parameter.Schema.Minimum = 1;
                        parameter.Schema.Maximum = PaginationDefaults.MaxPageSize;
                        break;
                    case "sort":
                        parameter.Description =
                            "Sort field. Prefix with `-` for descending (e.g. `-createdAt`). Allowed fields vary by resource; see API description. Invalid fields return `invalid_sort`.";
                        parameter.Required = false;
                        parameter.Schema ??= new OpenApiSchema { Type = "string" };
                        break;
                    case "status":
                        parameter.Description =
                            "Filter by status where supported. Invalid values return `invalid_status`.";
                        parameter.Required = false;
                        parameter.Schema ??= new OpenApiSchema { Type = "string" };
                        break;
                }
            }
        }
    }
}
