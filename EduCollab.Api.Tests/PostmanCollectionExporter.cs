using System.Text.Json;
using System.Text.Json.Nodes;

namespace EduCollab.Api.Tests;

public static class PostmanCollectionExporter
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "post", "put", "patch", "delete", "head", "options", "trace",
    };

    public static async Task ExportFromOpenApiAsync(
        string openApiPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var openApi = JsonNode.Parse(await File.ReadAllTextAsync(openApiPath, cancellationToken))
            ?? throw new InvalidOperationException("OpenAPI document is empty.");

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

                operations.Add(BuildRequestItem(path, method, operation));
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
            ["variable"] = new JsonArray
            {
                new JsonObject { ["key"] = "baseUrl", ["value"] = "https://localhost:7001" },
                new JsonObject { ["key"] = "accessToken", ["value"] = string.Empty },
            },
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

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
        };
        await File.WriteAllTextAsync(
            outputPath,
            collection.ToJsonString(options),
            cancellationToken);
    }

    public static string ResolveCommittedCollectionPath()
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

        return Path.Combine(directory.FullName, "EduCollab.Api.postman_collection.json");
    }

    private static JsonObject BuildFolder(KeyValuePair<string, List<JsonObject>> group)
    {
        return new JsonObject
        {
            ["name"] = group.Key,
            ["item"] = new JsonArray(group.Value.OrderBy(item => item["name"]?.GetValue<string>(), StringComparer.Ordinal).ToArray()),
        };
    }

    private static JsonObject BuildRequestItem(string path, string method, JsonObject operation)
    {
        var postmanPath = ConvertPathToPostman(path);
        var summary = operation["summary"]?.GetValue<string>();
        var name = summary ?? $"{method.ToUpperInvariant()} {path}";

        var headers = new JsonArray
        {
            new JsonObject { ["key"] = "Accept", ["value"] = "application/json" },
        };

        var request = new JsonObject
        {
            ["method"] = method.ToUpperInvariant(),
            ["header"] = headers,
            ["url"] = BuildUrlObject(postmanPath),
        };

        if (operation["requestBody"] is JsonObject requestBody)
        {
            headers.Add(new JsonObject { ["key"] = "Content-Type", ["value"] = "application/json" });
            request["body"] = BuildRequestBody(requestBody);
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

        return new JsonObject
        {
            ["name"] = name,
            ["request"] = request,
        };
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

            query.Add(new JsonObject
            {
                ["key"] = parameter["name"]?.GetValue<string>() ?? string.Empty,
                ["value"] = parameter["schema"]?["default"]?.ToJsonString() ?? string.Empty,
                ["description"] = parameter["description"]?.GetValue<string>() ?? string.Empty,
                ["disabled"] = true,
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

    private static JsonObject BuildRequestBody(JsonObject requestBody)
    {
        var schema = requestBody["content"]?["application/json"]?["schema"];
        var example = schema?["example"]?.ToJsonString()
            ?? (schema?["$ref"] is not null ? "{}" : "{}");

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
}
