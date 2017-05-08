namespace DEA.Common.Utilities
{
    /// <summary>
    /// A class containing all the bot owner information.
    /// </summary>
    public partial class Credentials
    {
        public string Token { get; set; }

        public ulong[] OwnerIds { get; set; }

        public int ShardCount { get; set; } = 1;

        public string MongoDBConnectionString { get; set; } = string.Empty;

        public string DatabaseName { get; set; }
    }
}