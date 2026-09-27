using ComplaintManagement.Application.Common.Interfaces;
using StackExchange.Redis;

namespace ComplaintManagement.Infrastructure.Services;

/// <summary>
/// Redis lock (SET NX PX with a unique token; released only by its owner). The expiry caps how long a crashed
/// instance can hold it.
/// </summary>
internal sealed class RedisDistributedLock(IConnectionMultiplexer redis) : IDistributedLock
{
    private const string ReleaseScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    public async Task<IAsyncDisposable?> TryAcquireAsync(string name, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var key = $"cmp:lock:{name}";
        var token = Guid.NewGuid().ToString("N");
        var db = redis.GetDatabase();
        return await db.StringSetAsync(key, token, expiry, When.NotExists)
            ? new Releaser(() => db.ScriptEvaluateAsync(ReleaseScript, [key], [token]))
            : null;
    }

    private sealed class Releaser(Func<Task> release) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await release();
    }
}

/// <summary>In-process lock for a single instance (used when Redis is not configured).</summary>
internal sealed class LocalDistributedLock : IDistributedLock
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IAsyncDisposable?> TryAcquireAsync(string name, TimeSpan expiry, CancellationToken cancellationToken = default) =>
        await _gate.WaitAsync(0, cancellationToken) ? new Releaser(_gate) : null;

    private sealed class Releaser(SemaphoreSlim gate) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            gate.Release();
            return ValueTask.CompletedTask;
        }
    }
}
