using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using DEA.Database.Repositories;
using System;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Xml;
using DEA.Common;
using DEA.Common.Preconditions;
using DEA.Common.Data;

namespace DEA.Modules
{
    public class NSFW : DEAModule
    {
        private readonly GuildRepository _guildRepo;

        public NSFW(GuildRepository guildRepo)
        {
            _guildRepo = guildRepo;
        }

        [Command("ChangeNSFWSettings")]
        [Alias("EnableNSFW", "DisableNSFW")]
        [Require(Attributes.Admin)]
        [Summary("Enables/disables NSFW commands in your server.")]
        public async Task ChangeNSFWSettings()
        {
            if (Context.DbGuild.Nsfw)
            {
                await _guildRepo.ModifyAsync(Context.DbGuild, x => x.Nsfw = false);
                await ReplyAsync($"You have successfully disabled NSFW commands!");
            }
            else
            {
                await _guildRepo.ModifyAsync(Context.DbGuild, x => x.Nsfw = true);
                await ReplyAsync($"You have successfully enabled NSFW commands!");
            }
        }

        [Command("SetNSFWChannel")]
        [Require(Attributes.Admin)]
        [Summary("Sets a specific channel for all NSFW commands.")]
        public async Task SetNSFWChannel([Remainder] ITextChannel nsfwChannel)
        {
            await _guildRepo.ModifyAsync(Context.DbGuild, x => x.NsfwChannelId = nsfwChannel.Id);
            await ReplyAsync($"You have successfully set the NSFW channel to {nsfwChannel.Mention}.");
        }

        [Command("Tits")]
        [Alias("titties", "tities", "boobs", "boob")]
        [Require(Attributes.Nsfw)]
        [Summary("Motorboat that shit.")]
        public async Task Tits()
        {
            using (var http = new HttpClient())
            {
                var obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{Config.RAND.Next(0, 10330)}"))[0];
                await Context.Channel.SendMessageAsync($"http://media.oboobs.ru/{obj["preview"]}");
            }
        }

        [Command("Ass")]
        [Alias("butt", "butts", "booty")]
        [Require(Attributes.Nsfw)]
        [Summary("Sauce me some booty how about that.")]
        public async Task Ass()
        {
            using (var http = new HttpClient())
            {
                var obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{Config.RAND.Next(0, 4335)}"))[0];
                await Context.Channel.SendMessageAsync($"http://media.obutts.ru/{obj["preview"]}");
            }
        }

        [Command("Hentai")]
        [Require(Attributes.Nsfw)]
        [Summary("The real shit goes down with custom hentai tags.")]
        public async Task Gelbooru([Remainder] string tag = "")
        {
            tag = tag?.Replace(" ", "_");

            using (var http = new HttpClient())
            {
                var data = await http.GetStreamAsync($"http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}");
                var doc = new XmlDocument();
                doc.Load(data);

                var node = doc.LastChild.ChildNodes[Config.RAND.Next(0, doc.LastChild.ChildNodes.Count)];
                if (node == null)
                {
                    ReplyError("No result found.");
                }

                var url = node.Attributes["file_url"].Value;

                if (!url.StartsWith("http"))
                {
                    url = "https:" + url;
                }

                await Context.Channel.SendMessageAsync(url);
            }
        }
    }
}
