namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>Stages an audit row on the current unit of work. It is written with the next SaveChanges.</summary>
public interface IAuditLogger
{
    void Log(string action, string module, string? recordId, string? details = null);
}
