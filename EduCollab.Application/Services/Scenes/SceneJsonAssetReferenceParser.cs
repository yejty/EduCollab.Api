using System.Text.Json;
using System.Text.Json.Nodes;

namespace EduCollab.Application.Services.Scenes
{
    internal static class SceneJsonAssetReferenceParser
    {
        public static HashSet<int> ExtractAssetIds(string? jsonContent)
        {
            var result = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(jsonContent))
                return result;

            try
            {
                var node = JsonNode.Parse(jsonContent);
                if (node is not null)
                    CollectAssetIds(node, result);
            }
            catch (JsonException)
            {
            }

            return result;
        }

        private static void CollectAssetIds(JsonNode node, HashSet<int> result)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var property in obj)
                    {
                        if (property.Key.Equals("assetId", StringComparison.OrdinalIgnoreCase)
                            && property.Value is JsonValue value
                            && value.TryGetValue<int>(out var assetId)
                            && assetId > 0)
                        {
                            result.Add(assetId);
                        }
                        else if (property.Value is not null)
                        {
                            CollectAssetIds(property.Value, result);
                        }
                    }

                    break;
                case JsonArray array:
                    foreach (var item in array)
                    {
                        if (item is not null)
                            CollectAssetIds(item, result);
                    }

                    break;
            }
        }
    }
}
