using Microsoft.Extensions.Options;
using Planora.Application.Common.Interfaces;

namespace Planora.Infrastructure.Storage;

public sealed class ConfiguredStoragePolicy(IOptions<StorageOptions> options) : IStoragePolicy
{
    public long MaxFileSizeBytes { get; } = Math.Clamp(options.Value.MaxFileSizeMb, 1, 500) * 1024L * 1024L;
}
