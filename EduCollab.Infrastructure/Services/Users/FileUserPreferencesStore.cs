using EduCollab.Application.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EduCollab.Infrastructure.Services.Users
{
    public sealed class FileUserPreferencesStore : IUserPreferencesStore
    {
        private readonly string _rootPath;

        public FileUserPreferencesStore(IOptions<UserPreferencesStorageOptions> options, IHostEnvironment hostEnvironment)
        {
            var configuredPath = options.Value.RootPath?.Trim();
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidOperationException("User preferences storage root path must be configured.");

            _rootPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, configuredPath));
        }

        public async Task<string?> GetAsync(int userId, CancellationToken cancellationToken)
        {
            var path = GetFilePath(userId);
            if (!File.Exists(path))
                return null;

            return await File.ReadAllTextAsync(path, cancellationToken);
        }

        public async Task SaveAsync(int userId, string json, CancellationToken cancellationToken)
        {
            var path = GetFilePath(userId);
            Directory.CreateDirectory(_rootPath);

            var tempPath = path + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, path, true);
        }

        private string GetFilePath(int userId)
        {
            if (userId <= 0)
                throw new ArgumentOutOfRangeException(nameof(userId));

            return Path.Combine(_rootPath, $"{userId}.json");
        }
    }
}
