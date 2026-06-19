using EduCollab.Application.Repositories;
using EduCollab.Application.Services.Content;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EduCollab.Infrastructure.Services.Content
{
    public sealed class FileSceneContentStore : ISceneContentStore
    {
        private readonly string _rootPath;

        public FileSceneContentStore(IOptions<WorkspaceContentStorageOptions> options, IHostEnvironment hostEnvironment)
        {
            var configuredPath = options.Value.RootPath?.Trim();
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidOperationException("Workspace content storage root path must be configured.");

            _rootPath = Path.IsPathRooted(configuredPath)
                ? Path.Combine(configuredPath, "scenes")
                : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configuredPath, "scenes"));
        }

        public async Task<string?> GetAsync(int workspaceId, int sceneId, int versionNumber, CancellationToken cancellationToken)
        {
            var path = GetExistingFilePath(workspaceId, sceneId, versionNumber);
            if (path is null || !File.Exists(path))
                return null;

            return await File.ReadAllTextAsync(path, cancellationToken);
        }

        public async Task SaveAsync(int workspaceId, int sceneId, int versionNumber, string jsonContent, CancellationToken cancellationToken)
        {
            var directory = GetWorkspaceDirectory(workspaceId);
            Directory.CreateDirectory(directory);

            var path = GetVersionFilePath(workspaceId, sceneId, versionNumber);
            var tempPath = path + ".tmp";
            await File.WriteAllTextAsync(tempPath, jsonContent, cancellationToken);
            File.Move(tempPath, path, true);
        }

        public Task CopyContentAsync(int workspaceId, int sceneId, int fromVersionNumber, int toVersionNumber, CancellationToken cancellationToken)
        {
            if (fromVersionNumber == toVersionNumber)
                return Task.CompletedTask;

            var sourcePath = GetExistingFilePath(workspaceId, sceneId, fromVersionNumber);
            if (sourcePath is null || !File.Exists(sourcePath))
                return Task.CompletedTask;

            var directory = GetWorkspaceDirectory(workspaceId);
            Directory.CreateDirectory(directory);
            var destinationPath = GetVersionFilePath(workspaceId, sceneId, toVersionNumber);
            File.Copy(sourcePath, destinationPath, true);
            return Task.CompletedTask;
        }

        public Task DeleteAllVersionsAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            var directory = GetWorkspaceDirectory(workspaceId);
            if (!Directory.Exists(directory))
                return Task.CompletedTask;

            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Equals(sceneId.ToString(), StringComparison.Ordinal)
                    || fileName.StartsWith($"{sceneId}-v", StringComparison.Ordinal))
                {
                    File.Delete(path);
                }
            }

            return Task.CompletedTask;
        }

        private string? GetExistingFilePath(int workspaceId, int sceneId, int versionNumber)
        {
            var versionPath = GetVersionFilePath(workspaceId, sceneId, versionNumber);
            if (File.Exists(versionPath))
                return versionPath;

            if (versionNumber == 1)
            {
                var legacyPath = Path.Combine(GetWorkspaceDirectory(workspaceId), $"{sceneId}.json");
                if (File.Exists(legacyPath))
                    return legacyPath;
            }

            return null;
        }

        private string GetWorkspaceDirectory(int workspaceId)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            return Path.Combine(_rootPath, workspaceId.ToString());
        }

        private string GetVersionFilePath(int workspaceId, int sceneId, int versionNumber)
        {
            if (sceneId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneId));

            return Path.Combine(GetWorkspaceDirectory(workspaceId), $"{sceneId}-v{versionNumber}.json");
        }
    }
}
