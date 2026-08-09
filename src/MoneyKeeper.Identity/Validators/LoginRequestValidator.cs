using FluentValidation;
using MoneyKeeper.Identity.Application.Common;
using MoneyKeeper.Identity.Application.Contracts.Auth;

namespace MoneyKeeper.Identity.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обязателен").WithErrorCode(ErrorCodes.EMAIL_IS_EMPTY)
                .EmailAddress().WithMessage("Некорректный Email").WithErrorCode(ErrorCodes.EMAIL_IS_INCORRECT);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен").WithErrorCode(ErrorCodes.PASSWORD_IS_EMPTY);
        }
    }
}
