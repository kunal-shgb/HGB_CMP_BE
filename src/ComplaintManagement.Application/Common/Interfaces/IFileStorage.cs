namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>Stores attachment bytes outside the web root under opaque keys. Implemented in Infrastructure/FileStorage.</summary>
public interface IFileStorage
{
    /// <summary>Saves the stream and returns its storage key (never derived from the user's file name).</summary>
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public enum MalwareScanVerdict
{
    Clean,
    Infected,
    /// <summary>No scanning engine is configured.</summary>
    NotScanned,
}

/// <summary>Malware scanning hook for uploads. The engine (e.g. ClamAV) is an open decision; see README.</summary>
public interface IMalwareScanner
{
    Task<MalwareScanVerdict> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}
