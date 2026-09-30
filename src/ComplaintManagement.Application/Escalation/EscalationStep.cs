using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Domain.Entities;

namespace ComplaintManagement.Application.Escalation;

/// <summary>Moves a complaint up to a level, to the division its category routes that level to. The caller saves.</summary>
public static class EscalationStep
{
    public static async Task<ComplaintEscalation> ApplyAsync(
        IApplicationDbContext db, IIamOrganisationService org, Complaint complaint, int toLevel,
        string reason, string by, string? byName, DateTimeOffset now, CancellationToken ct)
    {
        var divisionCode = ComplaintRouting.DivisionAt(toLevel, complaint.Category);
        var division = divisionCode is null ? null : await org.FindDepartmentAsync(divisionCode, ct);
        var divisionName = division?.Name ?? divisionCode;

        var escalation = new ComplaintEscalation
        {
            ComplaintId = complaint.Id, FromLevel = complaint.EscalationLevel, ToLevel = toLevel, Reason = reason,
            ToDivisionCode = divisionCode, ToDivisionName = divisionName,
            EscalatedBy = by, EscalatedByName = byName, EscalatedAt = now,
        };
        db.ComplaintEscalations.Add(escalation);
        complaint.EscalationLevel = toLevel;
        complaint.EscalatedDivisionCode = divisionCode;
        complaint.EscalatedDivisionName = divisionName;
        complaint.UpdatedAt = now;
        return escalation;
    }
}
