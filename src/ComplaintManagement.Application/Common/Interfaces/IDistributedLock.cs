namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>Cluster-wide lock so a background job runs on one API instance at a time.</summary>
public interface IDistributedLock
{
    /// <summary>Returns a handle to release, or null if another instance holds the lock.</summary>
    Task<IAsyncDisposable?> TryAcquireAsync(string name, TimeSpan expiry, CancellationToken cancellationToken = default);
}
