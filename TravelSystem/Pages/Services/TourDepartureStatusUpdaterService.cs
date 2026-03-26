using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TravelSystem.Hubs;
using TravelSystem.Models;

namespace TravelSystem.Pages.Services
{
    public class TourDepartureStatusUpdaterService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public TourDepartureStatusUpdaterService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run the loop until the application stops
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateStatusesAsync();
                
                // Wait for 1 minute before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task UpdateStatusesAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<FinalPrnContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<HubServer>>();

                var today = DateOnly.FromDateTime(DateTime.Today);
                bool hasChanges = false;

                // 1. "active" -> "ongoing" if StartDate <= Today
                // Note: To avoid tracking issues or large queries, we only query what needs to change
                var toOngoing = context.TourDepartures
                    .Where(d => d.Status != null && d.Status.Trim() == "active"
                             && d.StartDate != null && d.StartDate <= today)
                    .ToList();

                if (toOngoing.Any())
                {
                    foreach (var d in toOngoing)
                    {
                        d.Status = "ongoing";
                    }
                    hasChanges = true;
                }

                // 2. "ongoing" -> "completed" if EndDate < Today
                var toCompleted = context.TourDepartures
                    .Where(d => d.Status != null && d.Status.Trim() == "ongoing"
                             && d.EndDate != null && d.EndDate < today)
                    .ToList();

                if (toCompleted.Any())
                {
                    foreach (var d in toCompleted)
                    {
                        d.Status = "completed";
                    }
                    hasChanges = true;
                }

                // If any records were updated, save changes and notify clients
                if (hasChanges)
                {
                    await context.SaveChangesAsync();
                    try
                    {
                        await hubContext.Clients.All.SendAsync("loadAll");
                    }
                    catch
                    {
                        // Ignore SignalR errors if no clients are connected or hub is unready
                    }
                }
            }
            catch (Exception ex)
            {
                // In a production app, log the exception here. 
                // We wrap this in a try-catch so the background service doesn't crash on DB issues.
                Console.WriteLine($"Error updating departure statuses: {ex.Message}");
            }
        }
    }
}
