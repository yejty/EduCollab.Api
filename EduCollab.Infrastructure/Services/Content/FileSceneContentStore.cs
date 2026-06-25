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

        public async Task<string?> GetAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            var path = FindExistingFilePath(workspaceId, sceneId);
            if (path is null || !File.Exists(path))
                return null;

            return await File.ReadAllTextAsync(path, cancellationToken);
        }

        public async Task SaveAsync(int workspaceId, int sceneId, string jsonContent, CancellationToken cancellationToken)
        {
            var directory = GetWorkspaceDirectory(workspaceId);
            Directory.CreateDirectory(directory);

            DeleteLegacyVersionFiles(directory, sceneId);

            var path = Path.Combine(directory, $"{sceneId}.json");
            var tempPath = path + ".tmp";
            await File.WriteAllTextAsync(tempPath, jsonContent, cancellationToken);
            File.Move(tempPath, path, true);
        }

        public Task DeleteAsync(int workspaceId, int sceneId, CancellationToken cancellationToken)
        {
            var directory = GetWorkspaceDirectory(workspaceId);
            if (!Directory.Exists(directory))
                return Task.CompletedTask;

            DeleteLegacyVersionFiles(directory, sceneId);
            return Task.CompletedTask;
        }

        private static void DeleteLegacyVersionFiles(string directory, int sceneId)
        {
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Equals(sceneId.ToString(), StringComparison.Ordinal)
                    || fileName.StartsWith($"{sceneId}-v", StringComparison.Ordinal))
                {
                    File.Delete(path);
                }
            }
        }

        private string? FindExistingFilePath(int workspaceId, int sceneId)
        {
            var currentPath = Path.Combine(GetWorkspaceDirectory(workspaceId), $"{sceneId}.json");
            if (File.Exists(currentPath))
                return currentPath;

            var directory = GetWorkspaceDirectory(workspaceId);
            if (!Directory.Exists(directory))
                return null;

            return Directory.EnumerateFiles(directory, $"{sceneId}-v*.json")
                .OrderByDescending(path => ParseLegacyVersionNumber(path, sceneId))
                .FirstOrDefault();
        }

        private static int ParseLegacyVersionNumber(string path, int sceneId)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!fileName.StartsWith($"{sceneId}-v", StringComparison.Ordinal))
                return 0;

            var suffix = fileName[(sceneId.ToString().Length + 2)..];
            return int.TryParse(suffix, out var versionNumber) ? versionNumber : 0;
        }

        private string GetWorkspaceDirectory(int workspaceId)
        {
            if (workspaceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(workspaceId));

            return Path.Combine(_rootPath, workspaceId.ToString());
        }
    }
}
