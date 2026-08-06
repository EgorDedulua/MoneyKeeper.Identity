namespace MoneyKeeper.Identity.Application.Common
{
    public static class ErrorCodes
    {
        public const string INVALID_EMAIL_OR_PASSWORD = "INVALID_EMAIL_OR_PASSWORD";

        public const string EMAIL_ALREADY_TAKEN = "EMAIL_ALREADY_TAKEN";

        public const string TOKEN_REUSE_DETECTED = "TOKEN_REUSE_DETECTED";

        public const string INVALID_TOKEN = "INVALID_TOKEN";

        public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    }
}
