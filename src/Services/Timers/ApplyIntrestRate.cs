using DEA.Common.Data;
using DEA.Database.Models;
using DEA.Database.Repositories;
using DEA.Services.Static;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DEA.Services.Timers
{
    /// <summary>
    /// Periodically applies the interest rate to all gangs.
    /// </summary>
    class ApplyIntrestRate
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly GangRepository _gangRepo;

        private readonly Timer _timer;

        public ApplyIntrestRate(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _gangRepo = _serviceProvider.GetService<GangRepository>();

            ObjectState StateObj = new ObjectState();

            TimerCallback TimerDelegate = new TimerCallback(InterestRate);

            _timer = new Timer(TimerDelegate, StateObj, TimeSpan.FromMilliseconds(250), Config.INTEREST_RATE_COOLDOWN);

            StateObj.TimerReference = _timer;
        }

        private void InterestRate(object stateObj)
        {
            Task.Run(async () =>
            {
                Logger.Log(LogSeverity.Debug, $"Timers", "Interest Rate");
                var updateBuilder = Builders<Gang>.Update;

                foreach (var gang in await _gangRepo.AllAsync())
                {
                    try
                    {
                        await _gangRepo.Collection.UpdateOneAsync(y => y.Id == gang.Id,
                        updateBuilder.Set(x => x.Wealth, Static.InterestRate.Calculate(gang.Wealth) * gang.Wealth + gang.Wealth));
                    }
                    catch (OverflowException) { }
                }
            });
        }
    }
}