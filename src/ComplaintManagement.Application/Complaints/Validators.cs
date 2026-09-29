using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
using FluentValidation;

namespace ComplaintManagement.Application.Complaints;

public sealed class ComplaintFilterValidator : AbstractValidator<ComplaintFilterRequest>
{
    public ComplaintFilterValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).When(x => x.FromDate is not null && x.ToDate is not null)
            .WithMessage("'To date' must be on or after 'From date'.");
        RuleFor(x => x.ComplaintNumber).MaximumLength(32);
        RuleFor(x => x.CustomerName).MaximumLength(150);
        RuleFor(x => x.Mobile).MaximumLength(15);
        RuleFor(x => x.AccountNumber).MaximumLength(34);
        RuleFor(x => x.CustomerId).MaximumLength(32);
        RuleFor(x => x.TransactionId).MaximumLength(64);
        RuleFor(x => x.Source).Must(s => s is null or "" || ComplaintSources.All.Contains(s))
            .WithMessage("Unknown complaint source.");
    }
}

public sealed class ChangeStatusValidator : AbstractValidator<ChangeStatusRequest>
{
    public ChangeStatusValidator()
    {
        RuleFor(x => x.NewStatus).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Remarks).MaximumLength(4000);
    }
}

public sealed class AssignComplaintValidator : AbstractValidator<AssignComplaintRequest>
{
    public AssignComplaintValidator()
    {
        RuleFor(x => x.AssignedToEmployeeId).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DepartmentCode).MaximumLength(32);
        RuleFor(x => x.Remarks).MaximumLength(4000);
    }
}

public sealed class AddRemarkValidator : AbstractValidator<AddRemarkRequest>
{
    public AddRemarkValidator()
    {
        RuleFor(x => x.Remark).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Visibility).NotEmpty()
            .Must(v => Enum.TryParse<RemarkVisibility>(v, ignoreCase: true, out _))
            .WithMessage("Visibility must be INTERNAL or CUSTOMER.");
    }
}

/// <summary>Rules shared by the website form and staff intake.</summary>
public sealed class ComplaintIntakeDetailsValidator : AbstractValidator<IComplaintIntakeDetails>
{
    public ComplaintIntakeDetailsValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Mobile).NotEmpty().Matches(@"^[6-9]\d{9}$").WithMessage("Enter a valid 10-digit mobile number.");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(254).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.CustomerId).MaximumLength(32).Matches("^[A-Za-z0-9]*$");
        RuleFor(x => x.AccountNumber).MaximumLength(20).Matches(@"^\d*$").WithMessage("Account number must contain digits only.");
        RuleFor(x => x.CategoryCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.TransactionId).MaximumLength(64).Matches("^[A-Za-z0-9-]*$");
        RuleFor(x => x.TransactionDate)
            .Must(d => d is null || d <= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5)))
            .WithMessage("Transaction date cannot be in the future.");
        RuleFor(x => x.Amount).GreaterThan(0).LessThan(1_000_000_000).When(x => x.Amount is not null);
        RuleFor(x => x.Title).NotEmpty().MinimumLength(5).MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(10).MaximumLength(4000);
        RuleFor(x => x.PreferredChannel).Must(c => c is null or "" or "SMS" or "EMAIL").WithMessage("Preferred channel must be SMS or EMAIL.");
        RuleFor(x => x.Email).NotEmpty().When(x => x.PreferredChannel == "EMAIL").WithMessage("Enter an email address to be contacted by email.");
    }
}

public sealed class PublicCreateComplaintValidator : AbstractValidator<PublicCreateComplaintRequest>
{
    public PublicCreateComplaintValidator()
    {
        Include(new ComplaintIntakeDetailsValidator());
        RuleFor(x => x.BranchCode).NotEmpty().MaximumLength(32);
    }
}

/// <summary>Whether a branch is required depends on the caller's office, which the service checks.</summary>
public sealed class StaffCreateComplaintValidator : AbstractValidator<StaffCreateComplaintRequest>
{
    public StaffCreateComplaintValidator()
    {
        Include(new ComplaintIntakeDetailsValidator());
        RuleFor(x => x.BranchCode).MaximumLength(32);
    }
}
