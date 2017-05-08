using Discord;
using System;
using System.Text.RegularExpressions;

namespace DEA.Common.Data
{
    public static class Config
    {
        public const string DEFAULT_PREFIX = "$";

        public const int MIN_CHAR_LENGTH = 7, LEADERBOARD_CAP = 10, RATELB_CAP = 10, WHORE_ODDS = 90, JUMP_ODDS = 85, STEAL_ODDS = 80,
        MIN_CLEAR = 2, MAX_CLEAR = 1000, GANG_NAME_CHAR_LIMIT = 24, GANGSLB_CAP = 10, DEA_CUT = 10, RAID_SUCCESS_ODDS = 60, ROB_SUCCESS_ODDS = 60,
        MAX_POLL_SIZE = 100, TRIVIA_PAYOUT_MIN = 25, TRIVIA_PAYOUT_MAX = 150;

        public const decimal LINE_COST = 250, POUND_COST = 1000, KILO_COST = 2500, POUND_MULTIPLIER = 2, KILO_MULTIPLIER = 4,
            RESET_REWARD = 10000, MAX_WHORE = 100, MIN_WHORE = 50, WHORE_FINE = 200, MAX_JUMP = 250, JUMP_FINE = 500, MIN_JUMP = 100,
            MAX_STEAL = 500, MIN_STEAL = 250, STEAL_FINE = 1000, MAX_RESOURCES = 1000, MIN_RESOURCES = 25, DONATE_MIN = 5, BET_MIN = 5,
            GANG_CREATION_COST = 1000, GANG_NAME_CHANGE_COST = 250, WITHDRAW_CAP = 0.20m, MIN_WITHDRAW = 50, MIN_DEPOSIT = 50,
            JUMP_REQUIREMENT = 500, STEAL_REQUIREMENT = 2500, ROB_REQUIREMENT = 5000, BULLY_REQUIREMENT = 10000, FIFTYX2_REQUIREMENT = 25000,
            MAX_ROB_PERCENTAGE = 0.20m, MAX_RAID_PERCENTAGE = 0.20m;

        public static readonly Random RAND = new Random();

        public static readonly Regex ALPHANUMERICAL = new Regex(@"^[a-zA-Z0-9\s]*$"), ANWITHQUESTIONMARK = new Regex(@"^[a-zA-Z0-9\s\?]*$");

        private static readonly Color[] COLORS = { new Color(255, 38, 154), new Color(0, 255, 0), new Color(0, 232, 40), new Color(8, 248, 255),
        new Color(242, 38, 255), new Color(255, 28, 142), new Color(104, 255, 34), new Color(255, 190, 17), new Color(41, 84, 255),
        new Color(150, 36, 237), new Color(168, 237, 0)};

        public static Color Color() { return COLORS[RAND.Next(1, COLORS.Length) - 1]; }

        public static readonly Color ERROR_COLOR = new Color(255, 0, 0);

        public static readonly TimeSpan WHORE_COOLDOWN = TimeSpan.FromHours(2), JUMP_COOLDOWN = TimeSpan.FromHours(4),
            STEAL_COOLDOWN = TimeSpan.FromHours(6), ROB_COOLDOWN = TimeSpan.FromHours(3), WITHDRAW_COOLDOWN = TimeSpan.FromHours(4),
            RAID_COOLDOWN = TimeSpan.FromHours(3), LINE_COOLDOWN = TimeSpan.FromSeconds(25), DEFAULT_WAITFORMESSAGE = TimeSpan.FromSeconds(30),
            DEFAULT_POLL_LENGTH = TimeSpan.FromDays(1), MAX_POLL_LENGTH = TimeSpan.FromDays(7), DEFAULT_MESSAGE_COOLDOWN = TimeSpan.FromSeconds(30),
            ELDER_TIME_REQUIRED = TimeSpan.FromDays(2), MIN_CHILL = TimeSpan.FromSeconds(5), MAX_CHILL = TimeSpan.FromHours(1),
            TEMP_MULTIPLIER_RESET_COOLDOWN = TimeSpan.FromHours(1), INTEREST_RATE_COOLDOWN = TimeSpan.FromHours(1), AUTO_UNMUTE_COOLDOWN =
            TimeSpan.FromMinutes(5), AUTO_TRIVIA_COOLDOWN = TimeSpan.FromMinutes(2), AUTO_DELETE_POLLS_COOLDOWN = TimeSpan.FromMinutes(1);

        public static readonly string[] BANKS = { "Bank of America", "Wells Fargo Bank", "JPMorgan Chase Bank", "Capital One Bank",
        "RBC Bank", "USAA Bank", "Union Bank", "Morgan Stanley Bank" }, STORES = { "7-Eleven", "Speedway", "Couche-Tard", "QuikTrip",
        "Kroger", "Circle K" };
    }
}
