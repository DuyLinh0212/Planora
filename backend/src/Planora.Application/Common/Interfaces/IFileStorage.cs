using Planora.Application.Common.Results;

namespace Planora.Application.Common.Interfaces;

public sealed record StoredFile(string PublicId, string ResourceType, long SizeBytes, string? Checksum, string? Url = null);

public interface IFileStorage
{
    Task<ApplicationResult<StoredFile>> UploadFileAsync(Guid projectId, Guid fileId, int versionNumber, string fileName, string contentType, Stream content, CancellationToken cancellationToken);
    Task<ApplicationResult<StoredFile>> UploadAvatarAsync(Guid userId, string fileName, string contentType, Stream content, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteFileAsync(string publicId, string resourceType, CancellationToken cancellationToken);
    Task<ApplicationResult<Stream>> GetFileStreamAsync(string publicId, string resourceType, string fileName, CancellationToken cancellationToken);
}

public interface IStoragePolicy
{
    long MaxFileSizeBytes { get; }
}
