# Diagnostic Listeners

Diagnostic listeners are a part of the `System.Diagnostics` namespace in .NET and are used to collect detailed runtime information about the application. They are particularly useful for tracing and debugging complex scenarios, performance testing, and monitoring health and diagnostics in production. 

In a real-world scenario, diagnostic listeners can be used to:  
1. Monitor application health: By logging detailed information about the application's operations, you can monitor the application's health and performance in real time.  
2. Debug complex scenarios: Diagnostic listeners can provide detailed logs that can help you understand and debug complex scenarios that are difficult to reproduce.  
3. Performance testing: By logging the start and stop times of operations, you can identify performance bottlenecks in your application.  
4. Audit and compliance: Diagnostic listeners can be used to log detailed information about operations for audit and compliance purposes.

## In this demo

The Diagnostic Listener in this project is used to extend the logging functionality and help identify performance issues. It is implemented in the `DiagnosticWrapper` class and the `DiagnosticListenerService` class.

The `DiagnosticWrapper` class is a generic class that wraps around any operation and logs diagnostic information about it. It uses the `DiagnosticListener` class from the `System.Diagnostics` namespace to log the start, stop, and any errors that occur during the operation. It also logs the elapsed time of the operation. The diagnostic logging is only enabled if the `EnableDiagnostics` feature flag is set to true.
```csharp
public class DiagnosticWrapper<T>(IFeatureManager featureManager)
{
    private readonly DiagnosticListener _diagnosticListener = new(typeof(T).FullName ?? string.Empty);

    public async Task<TResult?> ExecuteOperationAsync<TResult>(Func<Task<TResult>> operationExpression, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (operationExpression == null) throw new ArgumentNullException(nameof(operationExpression));

        if (!await featureManager.IsEnabledAsync(FeatureManagementConstant.EnableDiagnostics).ConfigureAwait(false))
        {
            return await operationExpression().ConfigureAwait(false);
        }

        string operationName = operationExpression.Method.Name;
        string operationType = typeof(T).Name;

        string operationKey = $"{operationType}.{operationName}";
        Stopwatch stopwatch = new Stopwatch();
    
        if (_diagnosticListener.IsEnabled($"{operationKey}.Start"))
        {
            stopwatch.Start();
            _diagnosticListener.Write($"{operationKey}.Start", null);

        }

        TResult? result = default;
        try
        {
            result = await operationExpression().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _diagnosticListener.Write($"{operationKey}.Error", $"Error: {ex} -> {operationType}.{operationName} at {DateTime.UtcNow}. Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            return result;
        }
        finally
        {
            if (_diagnosticListener.IsEnabled($"{operationKey}.Stop"))
            {
                stopwatch.Stop();
                var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                if (result != null) _diagnosticListener.Write($"{operationKey}.Stop", result);
                _diagnosticListener.Write($"{operationKey}.ElapsedMs", elapsedMilliseconds);
                _diagnosticListener.Write($"{operationKey}.Stop", $"{operationName} completed in Elapsed Time: {elapsedMilliseconds} ms.");
            }
        }

        return result;
    }
}
```


The `DiagnosticListenerService` class is a hosted service that subscribes to the `DiagnosticListener`. `AllListeners` observable. It logs debug information about any diagnostic events that occur in the application. It only subscribes to listeners that are included in the `listenerNames` array.

```csharp
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
```
### How to use it
1. Add the `DiagnosticWrapper` and `DiagnosticListenerService` services to the DI container using the `AddDiagnostics` extension method.
```csharp
services.AddDiagnostics();
```
2. Inject the `DiagnosticWrapper` into any class where you want to log diagnostic information about an operation.
```csharp
public class SomeRandomJobTask(ILogger<SomeRandomJobTask> logger, DiagnosticWrapper<SomeRandomJobTask> diagnosticWrapper)
```
3. Use the `ExecuteOperationAsync` method of the `DiagnosticWrapper` to execute the operation and log diagnostic information about it.

```csharp
string? jobResult = await diagnosticWrapper.ExecuteOperationAsync(async () =>
{
    // Operation code here
}, stoppingToken).ConfigureAwait(false);
```

Please note that the diagnostic logging can add a lot of overhead to the application, so it should only be used when necessary. The `EnableDiagnostics` feature flag can be used to enable or disable the diagnostic logging.