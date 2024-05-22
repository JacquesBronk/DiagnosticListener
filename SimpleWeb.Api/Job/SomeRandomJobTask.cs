using Diagnostics.Lib.Domain;

namespace SimpleWeb.Api.Job;

public class SomeRandomJobTask(ILogger<SomeRandomJobTask> logger, DiagnosticWrapper<SomeRandomJobTask> diagnosticWrapper)
{
    public async Task<string?> ExecuteAsync(CancellationToken stoppingToken)
    {
        string? jobResult = await diagnosticWrapper.ExecuteOperationAsync(async () =>
        {
            try
            {
                //Simulate Work
                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);

                logger.LogDebug("SomeRandomJobTask is running");

                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                
                logger.LogDebug("SomeRandomJobTask is Finished");
                // Do some work here
                return "Success";
            }
            catch (Exception e)
            {
                logger.LogError("SomeRandomJobTask Has Failed");
                throw;
            }
        }, stoppingToken).ConfigureAwait(false);

        return jobResult;
    }
}