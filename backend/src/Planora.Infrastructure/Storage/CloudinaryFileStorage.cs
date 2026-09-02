using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;
using Planora.Application.Common.Results;

namespace Planora.Infrastructure.Storage;

public sealed class CloudinaryFileStorage : IFileStorage
{
    private readonly Cloudinary _cloudinary;
    private readonly string _rootFolder;

    public CloudinaryFileStorage(IOptions<CloudinaryOptions> options)
    {
        var cloudinaryOptions = options.Value;
        _rootFolder = cloudinaryOptions.RootFolder;
        _cloudinary = new Cloudinary(new Account(cloudinaryOptions.CloudName, cloudinaryOptions.ApiKey, cloudinaryOptions.ApiSecret)) { Api = { Secure = true } };
    }

    public async Task<ApplicationResult<StoredFile>> UploadFileAsync(Guid projectId, Guid fileId, int versionNumber, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        // Files are separated by the Workspace business area and project, not by their display name.
        cancellationToken.ThrowIfCancellationRequested();
        var basePublicId = $"{_rootFolder}/workspace/projects/{projectId}/files/{fileId}/v{versionNumber}";

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var imageResult = await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(fileName, content),
                PublicId = basePublicId,
                Overwrite = false
            });
            if (imageResult.Error is not null)
                return ApplicationResult.Failure<StoredFile>(ApplicationErrors.External("storage.upload_failed", $"Cloudinary upload failed: {imageResult.Error.Message}"));
            return ApplicationResult.Success(new StoredFile(imageResult.PublicId, "image", imageResult.Bytes, imageResult.Etag));
        }

        // Cloudinary requires raw public IDs to include the original extension. Without it,
        // the returned delivery URL cannot locate the asset later.
        var rawPublicId = basePublicId + GetSafeExtension(fileName);
        var rawResult = await _cloudinary.UploadAsync(new RawUploadParams
        {
            File = new FileDescription(fileName, content),
            PublicId = rawPublicId,
            Overwrite = false
        });
        if (rawResult.Error is not null)
            return ApplicationResult.Failure<StoredFile>(ApplicationErrors.External("storage.upload_failed", $"Cloudinary upload failed: {rawResult.Error.Message}"));
        return ApplicationResult.Success(new StoredFile(rawResult.PublicId, "raw", rawResult.Bytes, rawResult.Etag));
    }

    public async Task<ApplicationResult<StoredFile>> UploadAvatarAsync(Guid userId, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        // A fixed identity-scoped public id makes replacement atomic: the previous avatar is overwritten
        // and invalidated, so Cloudinary does not accumulate orphan avatar resources.
        var publicId = $"{_rootFolder}/identity/avatars/{userId}/profile";
        var uploadParameters = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            PublicId = publicId,
            Overwrite = true,
            Invalidate = true,
            Transformation = new Transformation().Width(512).Height(512).Crop("fill").Gravity("face")
        };
        cancellationToken.ThrowIfCancellationRequested();
        var uploadResult = await _cloudinary.UploadAsync(uploadParameters);
        if (uploadResult.Error is not null || uploadResult.SecureUrl is null)
            return ApplicationResult.Failure<StoredFile>(ApplicationErrors.External("storage.avatar_upload_failed", "Cloudinary could not save the avatar. Try again."));

        return ApplicationResult.Success(new StoredFile(
            uploadResult.PublicId,
            "image",
            uploadResult.Bytes,
            uploadResult.Etag,
            uploadResult.SecureUrl.AbsoluteUri));
    }

    public async Task<ApplicationResult> DeleteFileAsync(string publicId, string resourceType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parsedResourceType = Enum.TryParse<ResourceType>(resourceType, true, out var value)
            ? value
            : ResourceType.Raw;
        var deletionResult = await _cloudinary.DestroyAsync(new DeletionParams(publicId) { ResourceType = parsedResourceType, Invalidate = true });
        return string.Equals(deletionResult.Result, "ok", StringComparison.OrdinalIgnoreCase) || string.Equals(deletionResult.Result, "not found", StringComparison.OrdinalIgnoreCase)
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(ApplicationErrors.External("storage.delete_failed", "Cloudinary could not delete the resource. Retry later."));
    }

    public async Task<ApplicationResult<Stream>> GetFileStreamAsync(string publicId, string resourceType, string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parsedResourceType = Enum.TryParse<ResourceType>(resourceType, true, out var value)
            ? value
            : ResourceType.Raw;
        var deliveryPublicId = parsedResourceType == ResourceType.Raw && !Path.HasExtension(publicId)
            ? publicId + GetSafeExtension(fileName)
            : publicId;
        // Explicitly include the public `upload` delivery type. Without it the SDK emits
        // `/raw/v1/...` (or `/image/v1/...`) instead of `/raw/upload/v1/...`, which
        // Cloudinary rejects even when the file exists.
        var url = _cloudinary.Api.Url
            .ResourceType(parsedResourceType.ToString().ToLowerInvariant())
            .Type("upload")
            .BuildUrl(deliveryPublicId);
        if (string.IsNullOrWhiteSpace(url))
            return ApplicationResult.Failure<Stream>(ApplicationErrors.NotFound("File"));

        using var client = new HttpClient();
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return ApplicationResult.Failure<Stream>(ApplicationErrors.NotFound("File content"));

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return ApplicationResult.Success<Stream>(memoryStream);
    }

    private static string GetSafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension.Length is > 1 and <= 12 && extension.All(character => char.IsLetterOrDigit(character) || character == '.')
            ? extension
            : ".bin";
    }
}
