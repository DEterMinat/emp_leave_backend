using FluentValidation;
using EmployeeLeaveApi.DTOs;

namespace EmployeeLeaveApi.Validators;

public class LeaveRequestCreateDtoValidator : AbstractValidator<LeaveRequestCreateDto>
{
    public LeaveRequestCreateDtoValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("EmployeeId is required")
            .Length(24).WithMessage("EmployeeId must be a valid ObjectId");

        RuleFor(x => x.LeaveTypeId)
            .NotEmpty().WithMessage("LeaveTypeId is required")
            .Length(24).WithMessage("LeaveTypeId must be a valid ObjectId");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("StartDate must be today or in the future");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("EndDate is required")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("EndDate must be after or equal to StartDate");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required");
    }
}

public class LeaveRequestUpdateDtoValidator : AbstractValidator<LeaveRequestUpdateDto>
{
    public LeaveRequestUpdateDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status == "Approved" || status == "Rejected" || status == "Pending")
            .WithMessage("Status must be 'Approved', 'Rejected', or 'Pending'")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.ApproverId)
            .NotEmpty().WithMessage("ApproverId is required when updating request")
            .Length(24).WithMessage("ApproverId must be a valid ObjectId");
    }
}
