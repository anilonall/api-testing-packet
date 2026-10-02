using ApiTestingPacket.Core.DTOs.Auth;
using FluentValidation;

namespace ApiTestingPacket.Service.Validators.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).MinimumLength(12).MaximumLength(200);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}
