namespace DEA.Database.Models
{
    public partial class Blacklist : Model
    {
        public Blacklist(ulong userId, string username, string avatarUrl)
        {
            UserId = userId;
            Username = username;
            AvatarUrl = avatarUrl;
        }

        public ulong UserId { get; set; }

        public ulong[] GuildIds { get; set; } = new ulong[] { };

        public string Username { get; set; } = string.Empty;

        public string AvatarUrl { get; set; } = string.Empty;

    }
}
