
namespace ElectricFox.SondeAlert
{
    public sealed class AprsWorker : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                
                
                await Task.Delay(this.options.ProcessTimerSeconds * 1000, stoppingToken);
            }
        }
    }
}
