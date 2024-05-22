namespace Diagnostics.Lib.Domain;

public class Observer<T>(Action<T> action) : IObserver<T>
{
    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(T value)
    {
        action(value);
    }
}