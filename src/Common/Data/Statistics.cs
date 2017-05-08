namespace DEA.Common.Data
{
    /// <summary>
    /// Contains all statistics regarding DEA command usage.
    /// </summary>
    public class Statistics
    {
        public int MessagesRecieved { get; set; } = 0;

        public int CommandsRun { get; set; } = 0;
    }
}
