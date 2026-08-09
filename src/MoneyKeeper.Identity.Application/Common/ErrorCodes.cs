namespace MoneyKeeper.Identity.Application.Common
{
    public static class ErrorCodes
    {
        public const string INVALID_EMAIL_OR_PASSWORD = "INVALID_EMAIL_OR_PASSWORD";

        public const string EMAIL_IS_EMPTY = "EMAIL_IS_EMPTY";

        public const string EMAIL_IS_INCORRECT = "EMAIL_IS_INCORRECT";

        public const string EMAIL_IS_TOO_LONG = "EMAIL_IS_TOO_LONG";

        public const string PASSWORD_IS_EMPTY = "PASSWORD_IS_EMPTY";

        public const string PASSWORD_IS_TOO_SHORT = "PASSWORD_IS_TOO_SHORT";

        public const string PASSWORD_IS_TOO_LONG = "PASSWORD_IS_TOO_LONG";

        public const string PASSWORD_IS_UNSAFE = "PASSWORD_IS_UNSAFE";

        public const string USERNAME_IS_EMPTY = "USERNAME_IS_EMPTY";

        public const string USERNAME_IS_TOO_SHORT = "USERNAME_IS_TOO_SHORT";

        public const string USERNAME_IS_TOO_LONG = "USERNAME_IS_TOO_LONG";

        public const string EMAIL_ALREADY_TAKEN = "EMAIL_ALREADY_TAKEN";

        public const string TOKEN_REUSE_DETECTED = "TOKEN_REUSE_DETECTED";

        public const string INVALID_TOKEN = "INVALID_TOKEN";

        public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    }
}
