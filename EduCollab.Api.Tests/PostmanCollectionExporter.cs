using System.Text.Json;
using System.Text.Json.Nodes;

namespace EduCollab.Api.Tests;

public static class PostmanCollectionExporter
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "post", "put", "patch", "delete", "head", "options", "trace",
    };

    private static readonly HashSet<string> TokenResponsePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/users/login",
        "/api/users/login/confirm-code",
        "/api/users/registration-confirm",
        "/api/users/token",
    };

    private const string SaveTokensTestScript = """
        if (pm.response.code === 200) {
            const body = pm.response.json();
            if (body.accessToken) {
                pm.collectionVariables.set('accessToken', body.accessToken);
            }
            if (body.refreshToken) {
                pm.collectionVariables.set('refreshToken', body.refreshToken);
            }
        }
        """;

    public static async Task ExportFromOpenApiAsync(
        string openApiPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var openApi = JsonNode.Parse(await File.ReadAllTextAsync(openApiPath, cancellationToken))
            ?? throw new InvalidOperationException("OpenAPI document is empty.");

        var components = openApi["components"]?["schemas"]?.AsObject();
        var paths = openApi["paths"]?.AsObject()
            ?? throw new InvalidOperationException("OpenAPI document has no paths.");

        var grouped = new SortedDictionary<string, List<JsonObject>>(StringComparer.Ordinal);

        foreach (var (path, pathItem) in paths)
        {
            if (pathItem is not JsonObject pathObject)
            {
                continue;
            }

            foreach (var (method, operationNode) in pathObject)
            {
                if (!HttpMethods.Contains(method) || operationNode is not JsonObject operation)
                {
                    continue;
                }

                var tag = operation["tags"]?.AsArray().FirstOrDefault()?.GetValue<string>() ?? "Default";
                if (!grouped.TryGetValue(tag, out var operations))
                {
                    operations = [];
                    grouped[tag] = operations;
                }

                operations.Add(BuildRequestItem(path, method, operation, components));
            }
        }

        var collection = new JsonObject
        {
            ["info"] = new JsonObject
            {
                ["name"] = openApi["info"]?["title"]?.GetValue<string>() ?? "EduCollab API",
                ["description"] = openApi["info"]?["description"]?.GetValue<string>() ?? string.Empty,
                ["schema"] = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
            },
            ["variable"] = BuildCollectionVariables(),
            ["auth"] = new JsonObject
            {
                ["type"] = "bearer",
                ["bearer"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["key"] = "token",
                        ["value"] = "{{accessToken}}",
                        ["type"] = "string",
                    },
                },
            },
            ["item"] = new JsonArray(grouped.Select(BuildFolder).ToArray()),
        };

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await WriteJsonAsync(outputPath, collection, cancellationToken);
    }

    public static async Task ExportEnvironmentAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var environment = new JsonObject
        {
            ["name"] = "EduCollab Local",
            ["values"] = new JsonArray
            {
                Var("baseUrl", "http://localhost:8080", "Docker Compose API (see docker-compose.override.yml)"),
                Var("accessToken", string.Empty, "Set automatically by login / confirm-code / registration-confirm requests"),
                Var("refreshToken", string.Empty, "Set automatically by token responses"),
                Var("userId", "1", "User id for path parameters"),
                Var("groupId", "1", "Group id for create/list operations"),
                Var("assetId", "1", "Asset id for path/query parameters"),
                Var("sceneId", "1", "Scene id for path/query parameters"),
                Var("flowId", "1", "Flow id for path/query parameters"),
                Var("requestId", "1", "Workspace creation request id"),
                Var("reviewToken", string.Empty, "One-click review token from admin email"),
                Var("invitationToken", string.Empty, "Workspace invitation token"),
                Var("email", "user@example.com", "Test user email"),
                Var("password", "Password1!", "Test user password (must meet complexity rules)"),
            },
            ["_postman_variable_scope"] = "environment",
        };

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await WriteJsonAsync(outputPath, environment, cancellationToken);
    }

    public static string ResolveCommittedCollectionPath() => ResolveRepoRootFile("EduCollab.Api.postman_collection.json");

    public static string ResolveCommittedEnvironmentPath() => ResolveRepoRootFile("EduCollab.Api.postman_environment.json");

    private static string ResolveRepoRootFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EduCollab.Api.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate repository root from test output directory.");
        }

        return Path.Combine(directory.FullName, fileName);
    }

    private static JsonArray BuildCollectionVariables() => new JsonArray
    {
        Var("baseUrl", "http://localhost:8080"),
        Var("accessToken", string.Empty),
        Var("refreshToken", string.Empty),
        Var("userId", "1"),
        Var("groupId", "1"),
        Var("assetId", "1"),
        Var("sceneId", "1"),
        Var("flowId", "1"),
        Var("requestId", "1"),
        Var("reviewToken", string.Empty),
        Var("invitationToken", string.Empty),
        Var("email", "user@example.com"),
        Var("password", "Password1!"),
    };

    private static JsonObject Var(string key, string value, string? description = null)
    {
        var variable = new JsonObject
        {
            ["key"] = key,
            ["value"] = value,
            ["type"] = "default",
        };

        if (!string.IsNullOrWhiteSpace(description))
        {
            variable["description"] = description;
        }

        return variable;
    }

    private static JsonObject BuildFolder(KeyValuePair<string, List<JsonObject>> group)
    {
        return new JsonObject
        {
            ["name"] = group.Key,
            ["item"] = new JsonArray(group.Value.OrderBy(item => item["name"]?.GetValue<string>(), StringComparer.Ordinal).ToArray()),
        };
    }

    private static JsonObject BuildRequestItem(string path, string method, JsonObject operation, JsonObject? components)
    {
        var postmanPath = ConvertPathToPostman(path);
        var summary = operation["summary"]?.GetValue<string>();
        var name = summary ?? $"{method.ToUpperInvariant()} {path}";
        var isBinaryDownload = path.EndsWith("/content", StringComparison.OrdinalIgnoreCase);

        var headers = new JsonArray
        {
            new JsonObject
            {
                ["key"] = "Accept",
                ["value"] = isBinaryDownload ? "*/*" : "application/json",
            },
        };

        var request = new JsonObject
        {
            ["method"] = method.ToUpperInvariant(),
            ["header"] = headers,
            ["url"] = BuildUrlObject(postmanPath),
        };

        var description = operation["description"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(description))
        {
            request["description"] = description;
        }

        if (operation["requestBody"] is JsonObject requestBody)
        {
            var body = BuildRequestBody(requestBody, components);
            if (body is not null)
            {
                request["body"] = body;

                if (string.Equals(body["mode"]?.GetValue<string>(), "raw", StringComparison.Ordinal))
                {
                    headers.Add(new JsonObject { ["key"] = "Content-Type", ["value"] = "application/json" });
                }
            }
        }

        if (HasBearerSecurity(operation))
        {
            request["auth"] = new JsonObject
            {
                ["type"] = "bearer",
                ["bearer"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["key"] = "token",
                        ["value"] = "{{accessToken}}",
                        ["type"] = "string",
                    },
                },
            };
        }

        AppendQueryParameters(request, operation["parameters"]?.AsArray());

        var item = new JsonObject
        {
            ["name"] = name,
            ["request"] = request,
        };

        if (TokenResponsePaths.Contains(path))
        {
            item["event"] = new JsonArray
            {
                new JsonObject
                {
                    ["listen"] = "test",
                    ["script"] = new JsonObject
                    {
                        ["type"] = "text/javascript",
                        ["exec"] = new JsonArray(SaveTokensTestScript.Split('\n').Select(static line => (JsonNode)line).ToArray()),
                    },
                },
            };
        }

        return item;
    }

    private static void AppendQueryParameters(JsonObject request, JsonArray? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return;
        }

        var query = new JsonArray();
        foreach (var parameterNode in parameters)
        {
            if (parameterNode is not JsonObject parameter)
            {
                continue;
            }

            if (!string.Equals(parameter["in"]?.GetValue<string>(), "query", StringComparison.Ordinal))
            {
                continue;
            }

            var name = parameter["name"]?.GetValue<string>() ?? string.Empty;
            var required = parameter["required"]?.GetValue<bool>() == true
                || (parameter["description"]?.GetValue<string>()?.Contains("(required)", StringComparison.OrdinalIgnoreCase) ?? false);

            query.Add(new JsonObject
            {
                ["key"] = name,
                ["value"] = GetQueryDefaultValue(name, parameter),
                ["description"] = parameter["description"]?.GetValue<string>() ?? string.Empty,
                ["disabled"] = !required,
            });
        }

        if (query.Count == 0)
        {
            return;
        }

        if (request["url"] is JsonObject urlObject)
        {
            urlObject["query"] = query;
        }
    }

    private static string GetQueryDefaultValue(string name, JsonObject parameter)
    {
        if (parameter["schema"]?["default"] is JsonNode defaultNode)
        {
            return defaultNode.GetValueKind() == JsonValueKind.String
                ? defaultNode.GetValue<string>()
                : defaultNode.ToJsonString();
        }

        return name switch
        {
            "page" => "1",
            "pageSize" => "20",
            "owner" => "me",
            _ when name.EndsWith("Id", StringComparison.Ordinal) => $"{{{{{char.ToLowerInvariant(name[0])}{name[1..]}}}}}",
            _ => string.Empty,
        };
    }

    private static JsonObject? BuildRequestBody(JsonObject requestBody, JsonObject? components)
    {
        var content = requestBody["content"]?.AsObject();
        if (content is null)
        {
            return null;
        }

        if (content["multipart/form-data"] is JsonObject multipart)
        {
            return BuildMultipartFormBody(multipart, components);
        }

        if (content["application/json"] is JsonObject jsonContent)
        {
            var schema = jsonContent["schema"];
            var example = schema?["example"] is JsonNode explicitExample
                ? explicitExample.ToJsonString()
                : BuildExampleFromSchema(schema, components);

            return new JsonObject
            {
                ["mode"] = "raw",
                ["raw"] = example,
                ["options"] = new JsonObject
                {
                    ["raw"] = new JsonObject
                    {
                        ["language"] = "json",
                    },
                },
            };
        }

        return null;
    }

    private static JsonObject BuildMultipartFormBody(JsonObject multipart, JsonObject? components)
    {
        var formData = new JsonArray();
        var schema = ResolveSchema(multipart["schema"], components);
        var properties = schema?["properties"]?.AsObject();
        if (properties is not null)
        {
            foreach (var (name, propertyNode) in properties)
            {
                var property = ResolveSchema(propertyNode, components);
                var isFile = string.Equals(property?["format"]?.GetValue<string>(), "binary", StringComparison.Ordinal);
                formData.Add(new JsonObject
                {
                    ["key"] = ToFormFieldName(name),
                    ["type"] = isFile ? "file" : "text",
                    ["value"] = isFile ? string.Empty : GetMultipartDefaultValue(name),
                });
            }
        }

        return new JsonObject
        {
            ["mode"] = "formdata",
            ["formdata"] = formData,
        };
    }

    private static string GetMultipartDefaultValue(string propertyName) => propertyName switch
    {
        "Name" or "name" => "Example name",
        "Description" or "description" => "Optional description",
        "GroupId" or "groupId" => "{{groupId}}",
        "JsonContent" or "jsonContent" => "{\"objects\":[]}",
        _ when propertyName.EndsWith("Id", StringComparison.Ordinal) =>
            $"{{{{{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}}}}}",
        _ => string.Empty,
    };

    private static string ToFormFieldName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }

    private static JsonObject BuildUrlObject(string postmanPath)
    {
        var segments = postmanPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return new JsonObject
        {
            ["raw"] = $"{{{{baseUrl}}}}/{postmanPath.TrimStart('/')}",
            ["host"] = new JsonArray { "{{baseUrl}}" },
            ["path"] = new JsonArray(segments.Select(static segment => (JsonNode)segment).ToArray()),
        };
    }

    private static string ConvertPathToPostman(string path)
    {
        return System.Text.RegularExpressions.Regex.Replace(path, "\\{([^}]+)\\}", "{{$1}}");
    }

    private static bool HasBearerSecurity(JsonObject operation)
    {
        if (operation["security"] is not JsonArray securityRequirements)
        {
            return false;
        }

        foreach (var requirementNode in securityRequirements)
        {
            if (requirementNode is JsonObject requirement && requirement.ContainsKey("Bearer"))
            {
                return true;
            }
        }

        return false;
    }

    private static JsonObject? ResolveSchema(JsonNode? schemaNode, JsonObject? components)
    {
        if (schemaNode is not JsonObject schema)
        {
            return null;
        }

        if (schema["$ref"] is JsonValue refValue)
        {
            var refName = refValue.GetValue<string>().Split('/').Last();
            return components?[refName]?.AsObject();
        }

        return schema;
    }

    private static string BuildExampleFromSchema(JsonNode? schemaNode, JsonObject? components)
    {
        var exampleNode = BuildExampleNode(schemaNode, components, []);
        return exampleNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "{}";
    }

    private static JsonNode? BuildExampleNode(JsonNode? schemaNode, JsonObject? components, HashSet<string> visitedRefs)
    {
        if (schemaNode is JsonObject schemaWithRef && schemaWithRef["$ref"] is JsonValue refValue)
        {
            var refPath = refValue.GetValue<string>();
            if (!visitedRefs.Add(refPath))
            {
                return JsonValue.Create(string.Empty);
            }

            if (refPath.EndsWith("/JsonNode", StringComparison.Ordinal))
            {
                return JsonNode.Parse("""{"objects":[]}""");
            }

            schemaNode = components?[refPath.Split('/').Last()];
        }

        var schema = schemaNode as JsonObject;
        if (schema is null)
        {
            return null;
        }

        if (schema["example"] is JsonNode example)
        {
            var format = schema["format"]?.GetValue<string>();
            if (format is "email" or "password")
            {
                return format == "email"
                    ? JsonValue.Create("{{email}}")
                    : JsonValue.Create("{{password}}");
            }

            return example.DeepClone();
        }

        if (schema["enum"] is JsonArray enumValues && enumValues.Count > 0)
        {
            return enumValues[0]?.DeepClone();
        }

        var type = schema["type"]?.GetValue<string>();
        if (type == "array")
        {
            var itemExample = BuildExampleNode(schema["items"], components, visitedRefs) ?? JsonValue.Create(string.Empty);
            return new JsonArray(itemExample);
        }

        if (schema["properties"] is JsonObject properties)
        {
            var result = new JsonObject();
            var required = schema["required"]?.AsArray()?.Select(static node => node?.GetValue<string>() ?? string.Empty).ToHashSet(StringComparer.Ordinal)
                ?? [];

            foreach (var (propertyName, propertyNode) in properties)
            {
                if (required.Count > 0 && !required.Contains(propertyName))
                {
                    continue;
                }

                var value = BuildExampleNode(propertyNode, components, visitedRefs);
                if (value is not null)
                {
                    result[propertyName] = value;
                }
            }

            return result;
        }

        if (type == "object")
        {
            return new JsonObject();
        }

        return CreatePrimitiveExample(schema, type);
    }

    private static JsonNode CreatePrimitiveExample(JsonObject schema, string? type)
    {
        if (schema["default"] is JsonNode defaultValue)
        {
            return defaultValue.DeepClone();
        }

        var format = schema["format"]?.GetValue<string>();
        return (type, format) switch
        {
            ("integer", _) or ("number", _) => JsonValue.Create(1),
            ("boolean", _) => JsonValue.Create(true),
            (_, "email") => JsonValue.Create("{{email}}"),
            (_, "password") => JsonValue.Create("{{password}}"),
            (_, "date-time") => JsonValue.Create("2026-01-01T00:00:00Z"),
            ("string", _) => JsonValue.Create("string"),
            _ => JsonValue.Create(string.Empty),
        };
    }

    private static async Task WriteJsonAsync(string outputPath, JsonObject document, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
        };
        await File.WriteAllTextAsync(
            outputPath,
            document.ToJsonString(options),
            cancellationToken);
    }
}
