using System.Text.Json;
using System.Text.Json.Nodes;

namespace EduCollab.Api.Requests.Scenes
{
    internal static class SceneFormContentResolver
    {
        public static string ResolveJsonContent(string? jsonContent, IFormFile? jsonFile)
        {
            if (jsonFile is not null && jsonFile.Length > 0)
            {
                using var reader = new StreamReader(jsonFile.OpenReadStream());
                var fromFile = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(fromFile))
                    throw new ArgumentException("The uploaded scene JSON file is empty.");

                return fromFile.Trim();
            }

            if (string.IsNullOrWhiteSpace(jsonContent))
                throw new ArgumentException("JsonContent or a non-empty JsonFile is required.");

            return jsonContent.Trim();
        }

        public static JsonNode ParseJsonContent(string jsonContent)
        {
            try
            {
                return JsonNode.Parse(jsonContent)
                    ?? throw new ArgumentException("Scene JSON content cannot be null.");
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Scene JSON content is not valid JSON.", ex);
            }
        }
    }
}
