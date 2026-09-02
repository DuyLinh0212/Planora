using System.Security.Cryptography;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;

namespace Planora.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _storageRoot;
    private readonly string _legacyStorageRoot = Path.Combine(AppContext.BaseDirectory, ".planora-storage");

    public LocalFileStorage()
    {
        // AppContext.BaseDirectory points into bin/Debug or bin/Release. That changes
        // across builds and made previously uploaded files appear to disappear. Keep
        // new local content under the application's working/content directory and
        // still read the old location.
        _storageRoot = Path.Combine(Directory.GetCurrentDirectory(), ".planora-storage");
    }

    public async Task<ApplicationResult<StoredFile>> UploadFileAsync(Guid projectId, Guid fileId, int versionNumber, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        var relativeDirectory = Path.Combine("projects", projectId.ToString(), "files", fileId.ToString(), $"v{versionNumber}");
        var absoluteDirectory = Path.Combine(_storageRoot, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);
        var absoluteFilePath = Path.Combine(absoluteDirectory, Path.GetFileName(fileName));

        await using var output = File.Create(absoluteFilePath);
        using var checksum = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long uploadedSize = 0;
        int bytesRead;
        while ((bytesRead = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            checksum.AppendData(buffer, 0, bytesRead);
            uploadedSize += bytesRead;
        }

        return ApplicationResult.Success(new StoredFile(relativeDirectory.Replace('\\', '/'), "local", uploadedSize, Convert.ToHexString(checksum.GetHashAndReset())));
    }

    public async Task<ApplicationResult<StoredFile>> UploadAvatarAsync(Guid userId, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        var fileId = $"identity/avatars/{userId}/profile";
        var absoluteDirectory = Path.Combine(_storageRoot, "identity", "avatars", userId.ToString());
        Directory.CreateDirectory(absoluteDirectory);
        var absoluteFilePath = Path.Combine(absoluteDirectory, "profile");

        await using var output = File.Create(absoluteFilePath);
        using var checksum = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long uploadedSize = 0;
        int bytesRead;
        while ((bytesRead = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            checksum.AppendData(buffer, 0, bytesRead);
            uploadedSize += bytesRead;
        }

        return ApplicationResult.Success(new StoredFile(fileId, "local", uploadedSize, Convert.ToHexString(checksum.GetHashAndReset())));
    }

    public Task<ApplicationResult> DeleteFileAsync(string publicId, string resourceType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deleted = DeleteAtRoot(_storageRoot, publicId);
        if (!string.Equals(_storageRoot, _legacyStorageRoot, StringComparison.OrdinalIgnoreCase))
            deleted |= DeleteAtRoot(_legacyStorageRoot, publicId);
        if (!deleted && !IsSafePath(_storageRoot, publicId))
            return Task.FromResult(ApplicationResult.Failure(ApplicationErrors.Validation("storage.invalid_path", "Storage path is invalid.")));
        return Task.FromResult(ApplicationResult.Success());
    }

    public Task<ApplicationResult<Stream>> GetFileStreamAsync(string publicId, string resourceType, string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var targetFilePath = FindStoredFile(_storageRoot, publicId);
        if (targetFilePath is null && !string.Equals(_storageRoot, _legacyStorageRoot, StringComparison.OrdinalIgnoreCase))
            targetFilePath = FindStoredFile(_legacyStorageRoot, publicId);
        if (targetFilePath is null && !IsSafePath(_storageRoot, publicId))
            return Task.FromResult(ApplicationResult.Failure<Stream>(ApplicationErrors.Validation("storage.invalid_path", "Storage path is invalid.")));
        if (targetFilePath is null || !File.Exists(targetFilePath))
            return Task.FromResult(ApplicationResult.Failure<Stream>(ApplicationErrors.NotFound("File content")));

        Stream stream = File.OpenRead(targetFilePath);
        return Task.FromResult(ApplicationResult.Success(stream));
    }

    private static string? FindStoredFile(string storageRoot, string publicId)
    {
        if (!IsSafePath(storageRoot, publicId)) return null;
        var absolutePath = GetAbsolutePath(storageRoot, publicId);
        if (File.Exists(absolutePath)) return absolutePath;
        return Directory.Exists(absolutePath) ? Directory.EnumerateFiles(absolutePath).FirstOrDefault() : null;
    }

    private static bool DeleteAtRoot(string storageRoot, string publicId)
    {
        if (!IsSafePath(storageRoot, publicId)) return false;
        var absolutePath = GetAbsolutePath(storageRoot, publicId);
        if (File.Exists(absolutePath)) { File.Delete(absolutePath); return true; }
        if (Directory.Exists(absolutePath)) { Directory.Delete(absolutePath, true); return true; }
        return false;
    }

    private static string GetAbsolutePath(string storageRoot, string publicId) =>
        Path.GetFullPath(Path.Combine(storageRoot, publicId.Replace('/', Path.DirectorySeparatorChar)));

    private static bool IsSafePath(string storageRoot, string publicId)
    {
        var absolutePath = GetAbsolutePath(storageRoot, publicId);
        var relativePath = Path.GetRelativePath(Path.GetFullPath(storageRoot), absolutePath);
        return !relativePath.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relativePath);
    }
}
