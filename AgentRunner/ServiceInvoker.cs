using System.Diagnostics;
using System.Text.Json;

public static class ServerInvoker
{
    public static async Task<string> InvokeAsync(string projectDir, string toolName, object args, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(args);

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // Each Add = one argv, no manual quoting needed
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--no-build");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(projectDir);
        psi.ArgumentList.Add("--");       // separates your app args
        psi.ArgumentList.Add("--tool");
        psi.ArgumentList.Add(toolName);
        psi.ArgumentList.Add("--args");
        psi.ArgumentList.Add(payload);    // raw JSON string, no escapes

        Console.WriteLine($"[runner] launching: dotnet {string.Join(" ", psi.ArgumentList)}");


        using var p = Process.Start(psi)!;
        var stdout = await p.StandardOutput.ReadToEndAsync();
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0) throw new Exception(stderr);
        return stdout.Trim();
    }
}