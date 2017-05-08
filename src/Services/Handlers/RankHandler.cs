using DEA.Common;
using DEA.Database.Models;
using DEA.Database.Repositories;
using Discord;
using Discord.WebSocket;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DEA.Services.Handlers
{
    /// <summary>
    /// Handles all things related to DEA ranks.
    /// </summary>
    public class RankHandler
    {
        private readonly GuildRepository _guildRepo;

        public RankHandler(GuildRepository guildRepo)
        {
            _guildRepo = guildRepo;
        }

        public async Task HandleAsync(IGuild guild, IGuildUser user, Guild dbGuild, User dbUser)
        {
            if (dbGuild.RankRoles.ElementCount != 0)
            {
                var currentUser = await guild.GetCurrentUserAsync() as SocketGuildUser;
                if (!currentUser.GuildPermissions.ManageRoles)
                {
                    return;
                }

                decimal cash = dbUser.Cash;

                List<IRole> rolesToAdd = new List<IRole>();
                List<IRole> rolesToRemove = new List<IRole>();

                var highestRolePosition = currentUser.Roles.OrderByDescending(x => x.Position).First().Position;

                foreach (var rankRole in dbGuild.RankRoles)
                {
                    var cashRequired = (decimal)rankRole.Value.AsDouble;
                    var role = guild.GetRole(ulong.Parse(rankRole.Name));
                    if (role != null && role.Position < highestRolePosition)
                    {
                        bool hasRole = user.RoleIds.Any(x => x == role.Id);
                        if (cash >= cashRequired && !hasRole)
                        {
                            rolesToAdd.Add(role);
                        }

                        if (cash < cashRequired && hasRole)
                        {
                            rolesToRemove.Add(role);
                        }
                    }
                }

                foreach (var role in rolesToAdd)
                {
                    await user.AddRoleAsync(role);
                }

                foreach (var role in rolesToRemove)
                {
                    await user.RemoveRoleAsync(role);
                }
            }
        }

        public Task<IRole> GetRankAsync(DEAContext context, User dbUser)
        {
            IRole role = null;

            if (context.DbGuild.RankRoles.ElementCount != 0 && context.Guild != null)
            {
                foreach (var rankRole in context.DbGuild.RankRoles.OrderBy(x => x.Value))
                {
                    if (dbUser.Cash >= (decimal)rankRole.Value.AsDouble)
                    {
                        role = context.Guild.GetRole(ulong.Parse(rankRole.Name));
                    }
                }
            }

            return Task.FromResult(role);
        }
    }
}