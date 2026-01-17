using FluentValidation;
using EmployeeLeaveApi.DTOs;

namespace EmployeeLeaveApi.Validators;

public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("RoleId is required")
            .Length(24).WithMessage("RoleId must be a valid ObjectId (24 characters)");
    }
}

public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateDtoValidator()
    {
        RuleFor(x => x.Username)
            .MinimumLength(3).When(x => !string.IsNullOrEmpty(x.Username))
            .WithMessage("Username must be at least 3 characters");

        RuleFor(x => x.Password)
            .MinimumLength(6).When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.RoleId)
            .Length(24).When(x => !string.IsNullOrEmpty(x.RoleId))
            .WithMessage("RoleId must be a valid ObjectId (24 characters)");
    }
}
