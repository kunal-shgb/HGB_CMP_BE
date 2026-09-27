using ComplaintManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ComplaintManagement.Infrastructure.IAM;

/// <summary>
/// Bank IAM organisation API client. The endpoints for Regional Offices, branches and departments have
/// not been shared yet, so this is a seam: implement it once the IAM API specification is available.
/// </summary>
internal sealed class IamOrganisationService : IIamOrganisationService
{
    private const string Pending = "Bank IAM organisation integration is pending the IAM API specification.";

    public Task<IReadOnlyList<IamRegion>> GetRegionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException(Pending);
    public Task<IReadOnlyList<IamBranch>> GetBranchesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException(Pending);
    public Task<IReadOnlyList<IamDepartment>> GetDepartmentsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException(Pending);
}

/// <summary>
/// Caches organisation data from the IAM for a few minutes: it changes rarely and is read on every
/// registration, assignment and filter. Swap for a Redis cache when running more than one API instance.
/// </summary>
internal sealed class CachedIamOrganisationService(IIamOrganisationService inner, IMemoryCache cache) : IIamOrganisationService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public Task<IReadOnlyList<IamRegion>> GetRegionsAsync(CancellationToken cancellationToken = default) =>
        GetAsync("iam:org:regions", () => inner.GetRegionsAsync(cancellationToken));

    public Task<IReadOnlyList<IamBranch>> GetBranchesAsync(CancellationToken cancellationToken = default) =>
        GetAsync("iam:org:branches", () => inner.GetBranchesAsync(cancellationToken));

    public Task<IReadOnlyList<IamDepartment>> GetDepartmentsAsync(CancellationToken cancellationToken = default) =>
        GetAsync("iam:org:departments", () => inner.GetDepartmentsAsync(cancellationToken));

    private async Task<IReadOnlyList<T>> GetAsync<T>(string key, Func<Task<IReadOnlyList<T>>> load) =>
        await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            return await load();
        }) ?? [];
}
