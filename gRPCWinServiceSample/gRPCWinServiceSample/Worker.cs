using Win32APILib;

namespace gRPCWinServiceSample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Service Start: {DateTimeOffset.Now}");

            _ = Task.Run(() =>
            {
                try
                {
                    var pipeName = Program.g_PipeName;
                    while (!FileUtility.PipeExists(pipeName))
                    {
                        _logger.LogInformation($"Pipe Wait: {DateTimeOffset.Now}");
                        Thread.Sleep(100);
                    }
                    FileUtility.PipeSetAclAuthenticatedUsersPermit(pipeName);

                    _logger.LogInformation($"Pipe ACL Set: {DateTimeOffset.Now}");
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Pipe ACL Fail: {e}");
                }
            });


            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(new TimeSpan(0, 0, 10), stoppingToken);
                }
            }
            catch (OperationCanceledException e)
            {
                _logger.LogInformation($"Canceled: {e.Message}");
            }

            _logger.LogInformation($"Service Stop: {DateTimeOffset.Now}");
        }
    }
}