namespace Context
{
    public static class ContextAttributes
    {
        public const string ORIGINATING_MACHINE = "OriginatingMachine";
        public const string ISSUING_USER_ID = "IssuingUserId";
        public const string ISSUING_USER_NAME = "IssuingUserName";
    }

    public static class Queues
    {
        public const string MESSAGES = "lokad-cqrs-ex";
    }
}