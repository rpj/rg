using Roentgenium.Interfaces;
using Roentgenium.Config;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Roentgenium
{
    public class PipelineManagerService : IHostedService, IDisposable
    {
        private const double WorkFreq = 0.2;
        private IPipelineManager _pm;
        private LimitsConfig _limits;
        private Timer _timer;

        public PipelineManagerService(IPipelineManager pipelineManager, IOptions<LimitsConfig> limits)
        {
            _pm = pipelineManager;
            _limits = limits.Value;
        }
        
        public Task StartAsync(CancellationToken ct)
        {
            if (_limits.CompletedJobExpiryTime.HasValue)
                _timer = new Timer(Work, null, 0, (int)(1.0 / WorkFreq * 1000.0));

            return Task.CompletedTask;
        }

        public void Work(object s)
        {
            _pm.Info().List.ForEach(pInfo =>
            {
                // never expire a job that is still being executed
                if (pInfo.TypedStatus < PipelineBase.PipelineStatus.Success)
                    return;

                if (pInfo.Age.Value > TimeSpan.FromSeconds(_limits.CompletedJobExpiryTime.Value))
                {
                    Console.WriteLine($"{this}: Expiring job {pInfo.Id} at {DateTime.UtcNow} with age {pInfo.Age.Value}");
                    _pm.Remove(pInfo.Id);
                }
            });
        }

        public Task StopAsync(CancellationToken ct)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}