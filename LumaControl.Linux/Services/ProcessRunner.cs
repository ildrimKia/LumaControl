using System.Diagnostics;
using System.Text;

namespace LumaControl.Services;

/// <summary>
/// Runs an external process asynchronously and captures stdout/stderr.
/// Uses event-based async reading to avoid deadlocks with large output.
/// </summary>
internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var outputSb = new StringBuilder();
        var errorSb = new StringBuilder();

        var outputDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) outputDone.TrySetResult(true);
            else outputSb.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) errorDone.TrySetResult(true);
            else errorSb.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        await Task.WhenAll(
            outputDone.Task.WaitAsync(cts.Token),
            errorDone.Task.WaitAsync(cts.Token));

        await process.WaitForExitAsync(cts.Token);

        return new ProcessResult(
            outputSb.ToString().Trim(),
            errorSb.ToString().Trim(),
            process.ExitCode);
    }
}

internal readonly record struct ProcessResult(string Output, string Error, int ExitCode)
{
    public bool Success => ExitCode == 0;
}
