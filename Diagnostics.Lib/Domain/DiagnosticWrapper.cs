using System.Diagnostics;
using Diagnostics.Lib.App;
using Microsoft.FeatureManagement;

namespace Diagnostics.Lib.Domain;

/// <summary>
/// Diagnostic wrapper to extend logging functionality. This will help to identify the performance issues.
/// NOTE! This does add A LOT of overhead to the application. Only use this when you need to.
/// </summary>
/// <typeparam name="T"></typeparam>
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