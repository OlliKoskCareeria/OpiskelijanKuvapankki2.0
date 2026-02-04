using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System;

namespace OpiskelijanKuvapankki2_0.Services
{
    public class BackGroundCleanUpService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackGroundCleanUpService> _logger;
        public BackGroundCleanUpService(IServiceScopeFactory scopeFactory, ILogger<BackGroundCleanUpService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var cleaner = scope.ServiceProvider.GetRequiredService<ICleanUpService>();
                await cleaner.UserCleanUpRoutine();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        
    }

}
