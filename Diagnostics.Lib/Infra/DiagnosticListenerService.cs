using System.Collections;
using System.Diagnostics;
using Diagnostics.Lib.App;
using Diagnostics.Lib.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Diagnostics.Lib.Infra;

public class DiagnosticListenerService(ILogger<DiagnosticListenerService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        SubscribeToDiagnostics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    private void SubscribeToDiagnostics()
    {
        var listenerNames = new[]
        {
            ListenerDefinition.SomeJobDefinition
        };

        DiagnosticListener.AllListeners.Subscribe(new Observer<DiagnosticListener>(listener =>
        {
            if (listenerNames.Contains(listener.Name))
            {
                listener.Subscribe(new Observer<KeyValuePair<string, object>>(kvp =>
                {
                    logger.LogDebug("Listener: {ListenerName}, Key: {ObjKey}, Value: {ObjValue}", listener.Name, kvp.Key, kvp.Value);
                    logger.LogDebug("Event Time: {EventTime}", DateTime.UtcNow);
                    logger.LogDebug("Thread ID: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                    if (kvp.Value is IDictionary dictionary)
                    {
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            logger.LogDebug("Payload Property: {Key}, Value: {Value}", entry.Key, entry.Value);
                        }
                    }
                    if (kvp.Value is Exception ex)
                    {
                        logger.LogDebug("Exception: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                    }
                })!);
            }
        }));
    }
}