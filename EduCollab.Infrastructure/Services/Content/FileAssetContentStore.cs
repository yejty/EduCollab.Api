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

        public async Task<AssetContent?> GetAsync(int workspaceId, int assetId, int versionNumber, CancellationToken cancellationToken)
        {
            var path = FindExistingFilePath(workspaceId, assetId, versionNumber);
            if (path is null)
                return null;

            var contentType = ResolveContentType(path);
            var data = await File.ReadAllBytesAsync(path, cancellationToken);
            return new AssetContent(contentType, data);
        }

        public async Task SaveAsync(
            int workspaceId,
            int assetId,
            int versionNumber,
            string contentType,
            string? fileName,
            Stream content,
            CancellationToken cancellationToken)
        {
            var extension = ResolveExtension(contentType, fileName);
            var directory = GetWorkspaceDirectory(workspaceId);
            Directory.CreateDirectory(directory);

            var path = GetVersionFilePath(directory, assetId, versionNumber, extension);
            var tempPath = path + ".tmp";

            await using (var fileStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            File.Move(tempPath, path, true);
        }

        public Task CopyContentAsync(int workspaceId, int assetId, int fromVersionNumber, int toVersionNumber, CancellationToken cancellationToken)
        {
            if (fromVersionNumber == toVersionNumber)
                return Task.CompletedTask;

            var directory = GetWorkspaceDirectory(workspaceId);
            var sourcePath = FindExistingFilePath(workspaceId, assetId, fromVersionNumber);
            if (sourcePath is null)
                return Task.CompletedTask;

            Directory.CreateDirectory(directory);
            var extension = Path.GetExtension(sourcePath);
            var destinationPath = GetVersionFilePath(directory, assetId, toVersionNumber, extension);
            File.Copy(sourcePath, destinationPath, true);
            return Task.CompletedTask;
        }

        public Task DeleteAllVersionsAsync(int workspaceId, int assetId, CancellationToken cancellationToken)
        {
            var directory = GetWorkspaceDirectory(workspaceId);
            if (!Directory.Exists(directory))
                return Task.CompletedTask;

            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Equals(assetId.ToString(), StringComparison.Ordinal)
                    || fileName.StartsWith($"{assetId}-v", StringComparison.Ordinal))
                {
                    File.Delete(path);
                }
            }

            return Task.CompletedTask;
        }

        private string? FindExistingFilePath(int workspaceId, int assetId, int versionNumber)
        {
            var directory = GetWorkspaceDirectory(workspaceId);
            if (!Directory.Exists(directory))
                return null;

            var versionPrefix = $"{assetId}-v{versionNumber}.";
            var versionPath = Directory.EnumerateFiles(directory, $"{versionPrefix}*")
                .FirstOrDefault(path => AllowedExtensions.Contains(Path.GetExtension(path)));
            if (versionPath is not null)
                return versionPath;

            if (versionNumber == 1)
            {
                return Directory.EnumerateFiles(directory, $"{assetId}.*")
                    .FirstOrDefault(path => AllowedExtensions.Contains(Path.GetExtension(path)));
            }

            return null;
        }

        private static string GetVersionFilePath(string directory, int assetId, int versionNumber, string extension)
        {
            return Path.Combine(directory, $"{assetId}-v{versionNumber}{extension}");
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
