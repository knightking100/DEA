using MongoDB.Bson;
using System;

namespace DEA.Database.Models
{
    public partial class User : Model
    {
        public User(ulong userId, ulong guildId)
        {
            UserId = userId;
            GuildId = guildId;
        }

        public ulong UserId { get; set; }

        public ulong GuildId { get; set; }

        public decimal Cash { get; set; } = 0;
        
        public decimal Bounty { get; set; } = 0;

        public BsonDocument Inventory { get; set; } = new BsonDocument();

        //Cooldowns

        public DateTime Whore { get; set; } = DateTime.UtcNow.AddYears(-1);

        public DateTime Withdraw { get; set; } = DateTime.UtcNow.AddYears(-1);

        public DateTime Jump { get; set; } = DateTime.UtcNow.AddYears(-1);

        public DateTime Message { get; set; } = DateTime.UtcNow.AddYears(-1);

        public DateTime Rob { get; set; } = DateTime.UtcNow.AddYears(-1);

        public DateTime Steal { get; set; } = DateTime.UtcNow.AddYears(-1);

    }
}
