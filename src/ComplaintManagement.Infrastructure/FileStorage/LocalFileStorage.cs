using ComplaintManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ComplaintManagement.Infrastructure.FileStorage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Folder for attachments: absolute, or relative to the API's content root. Must be outside any web root.</summary>
    public string BasePath { get; set; } = "";
}

/// <summary>
/// Stores attachments on a local or mounted volume under opaque keys ("yyyy/MM/{guid}"). The user's file
/// name is never part of the path. The storage backend (volume, NFS, object store) is still an open decision.
/// </summary>
internal sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var key = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Write to a temp name, then move, so a reader never sees a half-written file.
        var temp = path + ".tmp";
        await using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await content.CopyToAsync(file, cancellationToken);
        File.Move(temp, path);
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        if (!File.Exists(path)) throw new FileNotFoundException("Attachment file is missing from storage.");
        return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    /// <summary>Maps a key to a path and refuses anything that would land outside the storage root.</summary>
    private string Resolve(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");
        return path;
    }
}

/// <summary>
/// Placeholder until the Bank chooses a scanning engine (e.g. ClamAV via clamd). Marks files NOT_SCANNED.
/// </summary>
internal sealed class NoOpMalwareScanner(ILogger<NoOpMalwareScanner> logger) : IMalwareScanner
{
    public Task<MalwareScanVerdict> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        logger.LogWarning("No malware scanner configured; upload stored as NOT_SCANNED");
        return Task.FromResult(MalwareScanVerdict.NotScanned);
    }
}
