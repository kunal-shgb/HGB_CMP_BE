namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>Issues race-free complaint numbers (HGB-YYYY-NNNNNNNN).</summary>
public interface IComplaintNumberGenerator
{
    Task<string> NextAsync(int year, CancellationToken cancellationToken = default);
}
