namespace Web.StartupTasks;

internal sealed class DatabaseMigrationSignal
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Complete() => _tcs.TrySetResult();

    public Task WaitAsync(CancellationToken cancellationToken) =>
        _tcs.Task.WaitAsync(cancellationToken);
}
