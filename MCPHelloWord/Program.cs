// Program.cs (top-level statements)
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;

// ── DIRECT TOOL SWITCH (demo‑only) ────────────────────────────────────────────
if (args.Length > 0 && args.Contains("--tool"))
{
    var toolIndex = Array.IndexOf(args, "--tool");
    var name = args[toolIndex + 1];

    var argsIndex = Array.IndexOf(args, "--args");
    var json = argsIndex > -1 ? args[argsIndex + 1] : "{}";
    var parsed = System.Text.Json.JsonDocument.Parse(json).RootElement;

    if (name == "hello")
    {
        var nm = parsed.TryGetProperty("name", out var v) ? v.GetString() ?? "there" : "there";
        Console.WriteLine($"Hello, {nm}!");
        return;
    }

    if (name == "convert-currency")
    {
        var from = parsed.GetProperty("from").GetString()!;
        var to = parsed.GetProperty("to").GetString()!;
        var amount = parsed.GetProperty("amount").GetDecimal();
        var http = new HttpClient();

        var result = await CurrencyTools.ConvertCurrency(from, to, amount, http, CancellationToken.None);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
        return;
    }

    Console.Error.WriteLine($"Unknown tool: {name}");
    Environment.Exit(1);
}
// ─────────────────────────────────────────────────────────────────────────────

// Build & run the MCP server over STDIO
var builder = Host.CreateApplicationBuilder(args);

// Log to stderr (what many MCP clients expect)
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Register the MCP server + STDIO transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()     // run over stdio (Inspector / Claude Desktop friendly)
    .WithToolsFromAssembly();       // scan this assembly for [McpServerTool] methods

await builder.Build().RunAsync();


// --------------------------- TOOLS BELOW ---------------------------

// Group tools in classes marked with [McpServerToolType]
[McpServerToolType]
public static class HelloTools
{
    // Exposes a tool named "hello"
    [McpServerTool, Description("Greets the supplied name.")]
    public static string Hello([Description("Name to greet")] string name = "world")
        => $"Hello MCP, {name}!";
}

[McpServerToolType]
public static class CurrencyTools
{
    // Dependency injection works: IMcpServer and HttpClient can be injected if needed.
    [McpServerTool(Name = "convert-currency"),
     Description("Converts an amount from one currency to another using exchangerate.host.")]
    public static async Task<string> ConvertCurrency(
        [Description("From currency, e.g. USD")] string from,
        [Description("To currency, e.g. EUR")] string to,
        [Description("Amount to convert")] decimal amount,
        HttpClient http,
        CancellationToken ct)
    {

        // Prefer parameter; fall back to environment variable
        var apiKey = Environment.GetEnvironmentVariable("EXCHANGERATE_API_KEY") ?? "528e1ccca45d033791c59781d41e703a";
        if (string.IsNullOrWhiteSpace(apiKey))
            return "Missing API key. Pass 'apiKey' or set EXCHANGERATE_API_KEY.";

        // Use HTTPS if possible
        var baseUrl = "https://api.exchangerate.host/convert";
        var url =
            $"{baseUrl}" +
            $"?access_key={Uri.EscapeDataString(apiKey)}" +
            $"&from={Uri.EscapeDataString(from)}" +
            $"&to={Uri.EscapeDataString(to)}" +
            $"&amount={Uri.EscapeDataString(amount.ToString(CultureInfo.InvariantCulture))}" +
            $"&format=1";

        ConvertResponse? payload;
        try
        {
            payload = await http.GetFromJsonAsync<ConvertResponse>(url, ct);
        }
        catch (Exception ex)
        {
            return $"Conversion failed (request error): {ex.Message}";
        }

        if (payload is null)
            return "Conversion failed: empty response.";
        if (!payload.success)
            return "Conversion failed: API reported success=false.";
        if (payload.info is null || payload.query is null)
            return "Conversion failed: missing fields in response.";
        if (payload.result is null)
            return "Conversion failed: 'result' was null.";

        var rate = payload.info.quote;
        var converted = payload.result.Value;

        // Optional: timestamp (if present)
        string when = payload.info.timestamp > 0
            ? $" @ {DateTimeOffset.FromUnixTimeSeconds(payload.info.timestamp):u}"
            : string.Empty;

        return $"{payload.query.amount} {payload.query.from} = {converted:F4} {payload.query.to} (rate {rate:F6}{when})";
    }

    // Response models matching your sample JSON
    private sealed class ConvertResponse
    {
        public bool success { get; set; }
        public string? terms { get; set; }
        public string? privacy { get; set; }
        public Query? query { get; set; }
        public Info? info { get; set; }
        public decimal? result { get; set; }
    }

    private sealed class Query
    {
        public string from { get; set; } = "";
        public string to { get; set; } = "";
        public decimal amount { get; set; }
    }

    private sealed class Info
    {
        public long timestamp { get; set; }
        public decimal quote { get; set; }
    }
}
