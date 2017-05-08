using DEA.Common.Data;
using DEA.Common.Utilities;
using DEA.Database.Models;
using DEA.Database.Repositories;
using DEA.Events;
using DEA.Services;
using DEA.Services.Handlers;
using DEA.Services.Static;
using DEA.Services.Timers;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DEA
{
    internal class Program
    {
        private static void Main()
        {
            new Program().RunAsync().GetAwaiter().GetResult();
        }

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;

        private readonly Credentials _credentials;
        private readonly Item[] _items;

        private readonly IMongoCollection<Guild> _guilds;
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Gang> _gangs;
        private readonly IMongoCollection<Poll> _polls;
        private readonly IMongoCollection<Mute> _mutes;
        private readonly IMongoCollection<Blacklist> _blacklists;

        public Program()
        {
            try
            {
                using (StreamReader file = File.OpenText(@"Credentials.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    _credentials = (Credentials)serializer.Deserialize(file, typeof(Credentials));
                }
                using (StreamReader file = File.OpenText(@"ItemList.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    _items = (Item[])serializer.Deserialize(file, typeof(Item[]));
                }
            }
            catch (IOException e)
            {
                Logger.Log(LogSeverity.Critical, "Error while loading up data, please fix this issue and restart the bot", e.Message);
                Console.ReadLine();
                Environment.Exit(0);
            }
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                MessageCacheSize = 10,
                TotalShards = _credentials.ShardCount,
                AlwaysDownloadUsers = true,
            });

            _commandService = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Error,
                DefaultRunMode = RunMode.Async,
            });

             var dbClient = new MongoClient(_credentials.MongoDBConnectionString);
             var database = dbClient.GetDatabase(_credentials.DatabaseName);

            _guilds = database.GetCollection<Guild>("guilds");
            _users = database.GetCollection<User>("users");
            _gangs = database.GetCollection<Gang>("gangs");
            _polls = database.GetCollection<Poll>("polls");
            _mutes = database.GetCollection<Mute>("mutes");
            _blacklists = database.GetCollection<Blacklist>("blacklists");
        }

        private async Task RunAsync()
        {
            Logger.NewLine("===   DEA   ===");
            Logger.NewLine();

            var sw = Stopwatch.StartNew();
            try
            {
                await _client.LoginAsync(TokenType.Bot, _credentials.Token);
            }
            catch (HttpException httpEx)
            {
                Logger.Log(LogSeverity.Critical, $"Login failed", httpEx.Reason);
                Console.ReadLine();
                Environment.Exit(0);
            }
               
            await _client.StartAsync();
            sw.Stop();
            Logger.Log(LogSeverity.Info, "Successfully connected", $"Elapsed time: {sw.Elapsed.TotalSeconds.ToString("N3")} seconds.");

            var serviceProvider = ConfigureServices();
            Logger.Log(LogSeverity.Info, "Mapping successfully configured", $"Services ready.");

            Logger.Log(LogSeverity.Info, "MongoDb Connection Verification", "Test connection has commenced...");
            sw.Restart();
            await _users.CountAsync(y => y.Cash > 0);
            sw.Stop();
            Logger.Log(LogSeverity.Info, "Test connection has succeeded", $"Elapsed time: {sw.Elapsed.TotalSeconds.ToString("N3")} seconds.");

            InitializeTimersAndEvents(serviceProvider);
            await new CommandHandler(_commandService, serviceProvider).InitializeAsync();
            Logger.Log(LogSeverity.Info, "Events and command handler successfully initialized", $"Client ready.");

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commandService)
                .AddSingleton(_credentials)
                .AddSingleton(_users)
                .AddSingleton(_guilds)
                .AddSingleton(_gangs)
                .AddSingleton(_mutes)
                .AddSingleton(_blacklists)
                .AddSingleton(_polls)
                .AddSingleton<PollRepository>()
                .AddSingleton<GuildRepository>()
                .AddSingleton<BlacklistRepository>()
                .AddSingleton<RankHandler>()
                .AddSingleton<UserRepository>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<GameService>()
                .AddSingleton<ModerationService>()
                .AddSingleton<ErrorHandler>()
                .AddSingleton<GangRepository>()
                .AddSingleton<MuteRepository>()
                .AddSingleton<Statistics>();

            return new DefaultServiceProviderFactory().CreateServiceProvider(services);
        }

        private void InitializeTimersAndEvents(IServiceProvider serviceProvider)
        {
            new Ready(serviceProvider);
            new JoinedGuild(serviceProvider);
            new GuildUpdated(serviceProvider);
            new UserJoined(serviceProvider);
            new ApplyIntrestRate(serviceProvider);
            new AutoDeletePolls(serviceProvider);
            new AutoTrivia(serviceProvider);
            new AutoUnmute(serviceProvider);
            new ResetTempMultiplier(serviceProvider);
        }

    }
}
