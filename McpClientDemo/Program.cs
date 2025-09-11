using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using ModelContextProtocol.Client;

string serverProject = args.FirstOrDefault(a => a.StartsWith("--server="))?.Split('=')[1]
                      ?? "../MCPHelloWord";

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "MCPHelloWord",
    Command = "dotnet",
    Arguments = ["run", "--no-build", "--project", serverProject],
});

var client = await McpClientFactory.CreateAsync(transport);
Console.WriteLine("Connected. Commands: list | schema <tool> | call <tool> <json> | quit\n");

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

    var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
    var cmd = parts[0].ToLowerInvariant();

    try
    {
        switch (cmd)
        {
            case "list":
                {
                    var tools = await client.ListToolsAsync();
                    foreach (var t in tools)
                        Console.WriteLine($"- {t.Name}: {t.Description}");
                    break;
                }
            case "schema":
                {
                    if (parts.Length < 2) { Console.WriteLine("usage: schema <tool>"); break; }
                    var toolName = parts[1];
                    var t = (await client.ListToolsAsync()).FirstOrDefault(x => x.Name == toolName);
                    if (t is null) { Console.WriteLine("tool not found"); break; }
                    Console.WriteLine(t.JsonSchema.ToString());
                    break;
                }
            case "call":
                {
                    if (parts.Length < 3) { Console.WriteLine("usage: call <tool> <json>"); break; }
                    var toolName = parts[1];
                    var json = parts[2];

                    // (simple) parse args; for complex payloads, support @file later
                    var node = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
                    var dict = node.ToDictionary(kv => kv.Key, kv => kv.Value?.GetValue<object?>());

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var result = await client.CallToolAsync(toolName, dict, null, null, cts.Token);
                    foreach (var block in result.Content)
                    {
                        if (block.Type == "text") Console.WriteLine(block.ToAIContent());
                        else Console.WriteLine($"[{block.Type}] {JsonSerializer.Serialize(block)}");
                    }
                    break;
                }
            default:
                Console.WriteLine("unknown command");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}