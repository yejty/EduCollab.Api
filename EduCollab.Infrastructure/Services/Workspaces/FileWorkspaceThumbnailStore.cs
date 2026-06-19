using EduCollab.Application.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EduCollab.Infrastructure.Services.Workspaces
{
    public sealed class FileWorkspaceThumbnailStore : IWorkspaceThumbnailStore
    {
        private static readonly Dictionary<string, string> ContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif",
        };

        private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif",
        };

        private readonly string _rootPath;

        public FileWorkspaceThumbnailStore(IOptions<WorkspaceThumbnailStorageOptions> options, IHostEnvironment hostEnvironment)
        {
            var configuredPath = options.Value.RootPath?.Trim();
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidOperationException("Workspace thumbnail storage root path must be configured.");

            _rootPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configuredPath));
        }

        public async Task<WorkspaceThumbnailContent?> GetAsync(int workspaceId, CancellationToken cancellationToken)
        {
            var path = FindExistingFilePath(workspaceId);
            if (path is null)
                return null;

            var extension = Path.GetExtension(path);
            if (!ExtensionToContentType.TryGetValue(extension, out var contentType))
                return null;

            var data = await File.ReadAllBytesAsync(path, cancellationToken);
            return new WorkspaceThumbnailContent(contentType, data);
        }

        public async Task SaveAsync(int workspaceId, string contentType, Stream content, CancellationToken cancellationToken)
        {
            if (!ContentTypeToExtension.TryGetValue(contentType, out var extension))
                throw new ArgumentException("Unsupported thumbnail content type.", nameof(contentType));

            Directory.CreateDirectory(_rootPath);
            DeleteExistingFiles(workspaceId);

            var path = GetFilePath(workspaceId, extension);
            var tempPath = path + ".tmp";

            await using (var fileStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            File.Move(tempPath, path, true);
        }

        public Task DeleteAsync(int workspaceId, CancellationToken cancellationToken)
        {
            DeleteExistingFiles(workspaceId);
            return Task.CompletedTask;
        }

        private void DeleteExistingFiles(int workspaceId)
        {
            if (!Directory.Exists(_rootPath))
                return;

            foreach (var path in Directory.EnumerateFiles(_rootPath, $"{workspaceId}.*"))
            {
                File.Delete(path);
            }
        }

        private string? FindExistingFilePath(int workspaceId)
        {
            if (!Directory.Exists(_rootPath))
                return null;

            return Directory.EnumerateFiles(_rootPath, $"{workspaceId}.*")
                .FirstOrDefault(path => ExtensionToContentType.ContainsKey(Path.GetExtension(path)));
        }

        private string GetFilePath(int workspaceId, string extension)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            return Path.Combine(_rootPath, $"{workspaceId}{extension}");
        }
    }
}
