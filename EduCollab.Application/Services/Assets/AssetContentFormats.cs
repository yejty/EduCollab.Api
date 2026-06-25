namespace EduCollab.Application.Services.Assets
{
    public static class AssetContentFormats
    {
        public const string ZipContentType = "application/zip";

        public const string DefaultAssetType = "Package";

        public static bool IsZipContent(string contentType, string? fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName)
                && Path.GetExtension(fileName.Trim()).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(contentType))
                return false;

            var normalized = contentType.Split(';', 2)[0].Trim();
            return normalized.Equals(ZipContentType, StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("application/x-zip-compressed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
