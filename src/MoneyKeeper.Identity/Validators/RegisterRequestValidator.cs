using FluentValidation;
using MoneyKeeper.Identity.Application.Common;
using MoneyKeeper.Identity.Application.Contracts.Auth;

namespace MoneyKeeper.Identity.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обязателен").WithErrorCode(ErrorCodes.EMAIL_IS_EMPTY)
                .EmailAddress().WithMessage("Некорректный Email").WithErrorCode(ErrorCodes.EMAIL_IS_INCORRECT)
                .MaximumLength(256).WithMessage("Email не может быть длиннее 256 символов").WithErrorCode(ErrorCodes.EMAIL_IS_TOO_LONG);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен").WithErrorCode(ErrorCodes.PASSWORD_IS_EMPTY)
                .MinimumLength(8).WithMessage("Пароль не может быть короче 8 символов").WithErrorCode(ErrorCodes.PASSWORD_IS_TOO_SHORT)
                .MaximumLength(100).WithMessage("Пароль не может быть длиннее 100 символов").WithErrorCode(ErrorCodes.PASSWORD_IS_TOO_LONG)
                .Matches("[A-Z]").WithMessage("Пароль должен содержать заглавную латинскую букву").WithErrorCode(ErrorCodes.PASSWORD_IS_UNSAFE)
                .Matches("[a-z]").WithMessage("Пароль должен содержать строчную латинскую букву").WithErrorCode(ErrorCodes.PASSWORD_IS_UNSAFE)
                .Matches("[0-9]").WithMessage("Пароль должен содержать цифру").WithErrorCode(ErrorCodes.PASSWORD_IS_UNSAFE);

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Имя пользователя обязательно").WithErrorCode(ErrorCodes.USERNAME_IS_EMPTY)
                .MinimumLength(4).WithMessage("Имя пользователя не может быть короче 4 символов").WithErrorCode(ErrorCodes.USERNAME_IS_TOO_SHORT)
                .MaximumLength(50).WithMessage("Имя пользователя не может быть длиннее 50 символов").WithErrorCode(ErrorCodes.USERNAME_IS_TOO_LONG);
        }
    }
}
