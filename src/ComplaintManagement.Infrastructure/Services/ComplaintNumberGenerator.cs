using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.ValueObjects;
using ComplaintManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Infrastructure.Services;

/// <summary>
/// Atomic per-year counter using a single UPSERT, so concurrent API instances never issue the
/// same number. A failed save after this call leaves a gap, which is acceptable for reference numbers.
/// </summary>
internal sealed class ComplaintNumberGenerator(ApplicationDbContext db) : IComplaintNumberGenerator
{
    public async Task<string> NextAsync(int year, CancellationToken cancellationToken = default)
    {
        var next = await db.Database.SqlQuery<long>($"""
            INSERT INTO complaint_number_sequences (year, last_value) VALUES ({year}, 1)
            ON CONFLICT (year) DO UPDATE SET last_value = complaint_number_sequences.last_value + 1
            RETURNING last_value AS "Value"
            """).ToListAsync(cancellationToken);

        return ComplaintNumber.Format(year, next.Single());
    }
}
