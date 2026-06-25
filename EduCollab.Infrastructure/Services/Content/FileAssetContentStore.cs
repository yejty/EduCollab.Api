using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Content;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EduCollab.Infrastructure.Services.Content
{
    public sealed class FileAssetContentStore : IAssetContentStore
    {
        private static readonly Dictionary<string, string> ContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif",
            ["model/gltf-binary"] = ".glb",
            ["model/gltf+json"] = ".gltf",
            ["application/octet-stream"] = ".bin",
            ["application/zip"] = ".zip",
            ["application/x-zip-compressed"] = ".zip",
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif", ".glb", ".gltf", ".bin", ".fbx", ".obj", ".mp4", ".webm", ".pdf", ".zip",
        };

        private readonly string _rootPath;

        public FileAssetContentStore(IOptions<WorkspaceContentStorageOptions> options, IHostEnvironment hostEnvironment)
        {
            var configuredPath = options.Value.RootPath?.Trim();
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidOperationException("Workspace content storage root path must be configured.");

            _rootPath = Path.IsPathRooted(configuredPath)
                ? Path.Combine(configuredPath, "assets")
                : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configuredPath, "assets"));
        }

        public async Task<AssetContent?> GetAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            var path = FindExistingFilePath(workspaceId, assetId);
            if (path is null)
                return null;

            var contentType = ResolveContentType(path);
            var data = await File.ReadAllBytesAsync(path, cancellationToken);
            return new AssetContent(contentType, data);
        }

        public async Task SaveAsync(
            int workspaceId,
            int assetId,
            string contentType,
            string? fileName,
            Stream content,
            CancellationToken cancellationToken)
        {
            var extension = ResolveExtension(contentType, fileName);
            var directory = GetWorkspaceDirectory(workspaceId);
            Directory.CreateDirectory(directory);

            DeleteLegacyVersionFiles(directory, assetId);

            var path = Path.Combine(directory, $"{assetId}{extension}");
            var tempPath = path + ".tmp";

            await using (var fileStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            File.Move(tempPath, path, true);
        }

        public Task DeleteAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            var directory = GetWorkspaceDirectory(workspaceId);
            if (!Directory.Exists(directory))
                return Task.CompletedTask;

            DeleteLegacyVersionFiles(directory, assetId);
            return Task.CompletedTask;
        }

        private static void DeleteLegacyVersionFiles(string directory, int assetId)
        {
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Equals(assetId.ToString(), StringComparison.Ordinal)
                    || fileName.StartsWith($"{assetId}-v", StringComparison.Ordinal))
                {
                    File.Delete(path);
                }
            }
        }

        private string? FindExistingFilePath(int workspaceId, int assetId)
        {
            var directory = GetWorkspaceDirectory(workspaceId);
            if (!Directory.Exists(directory))
                return null;

            var currentPath = Directory.EnumerateFiles(directory, $"{assetId}.*")
                .FirstOrDefault(path => AllowedExtensions.Contains(Path.GetExtension(path)));
            if (currentPath is not null)
                return currentPath;

            return Directory.EnumerateFiles(directory, $"{assetId}-v*.*")
                .Where(path => AllowedExtensions.Contains(Path.GetExtension(path)))
                .OrderByDescending(path => ParseLegacyVersionNumber(path, assetId))
                .FirstOrDefault();
        }

        private static int ParseLegacyVersionNumber(string path, int assetId)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!fileName.StartsWith($"{assetId}-v", StringComparison.Ordinal))
                return 0;

            var suffix = fileName[(assetId.ToString().Length + 2)..];
            return int.TryParse(suffix, out var versionNumber) ? versionNumber : 0;
        }

        private static string ResolveExtension(string contentType, string? fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var fileExtension = Path.GetExtension(fileName.Trim());
                if (!string.IsNullOrWhiteSpace(fileExtension) && AllowedExtensions.Contains(fileExtension))
                    return fileExtension.ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(contentType)
                && ContentTypeToExtension.TryGetValue(contentType.Trim(), out var mappedExtension))
            {
                return mappedExtension;
            }

            return ".bin";
        }

        private static string ResolveContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".glb" => "model/gltf-binary",
                ".gltf" => "model/gltf+json",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                _ => "application/octet-stream",
            };
        }

        private string GetWorkspaceDirectory(int workspaceId)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            return Path.Combine(_rootPath, workspaceId.ToString());
        }
    }
}
