using System.Text.Json;

var serverProject = args.FirstOrDefault(a => a.StartsWith("--server="))?.Split('=')[1] ?? "./MCPHelloWord";
var reasoner = new HeuristicReasoner();
var tools = ToolCatalog.All;

Console.WriteLine("Try: 'hello Polly' or 'convert 25 usd to eur'. Ctrl+C to exit.");

while (true)
{
    var msg = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(msg)) continue;

    var toolName = await reasoner.DecideToolAsync(msg, tools);
    if (!Guardrails.IsAllowed(toolName)) { Console.WriteLine($"Blocked: {toolName}"); continue; }

    var tool = tools.First(t => t.Name == toolName);
    var argsDict = await reasoner.FillArgsAsync(msg, tool);

    var (ok, err) = Guardrails.ValidateArgs(tool, argsDict);
    if (!ok) { Console.WriteLine($"Validation failed: {err}"); continue; }

    if (Guardrails.NeedsConfirmation(tool, argsDict))
    {
        Console.WriteLine($"About to call '{tool.Name}' with: {JsonSerializer.Serialize(argsDict)}. Proceed? (y/N)");
        if (!string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase))
        { Console.WriteLine("Cancelled."); continue; }
    }

    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var response = await ServerInvoker.InvokeAsync(serverProject, tool.Name, argsDict, cts.Token);
        Console.WriteLine($"→ {response}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Execution failed: {ex.Message}");
    }
}