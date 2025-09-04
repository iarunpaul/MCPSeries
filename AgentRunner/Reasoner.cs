public interface IReasoner
{
    Task<string> DecideToolAsync(string userMessage, IReadOnlyList<ToolDef> tools);
    Task<Dictionary<string, object?>> FillArgsAsync(string userMessage, ToolDef tool);
}

// Heuristic baseline: good enough for hands‑on learning.
public sealed class HeuristicReasoner : IReasoner
{
    public Task<string> DecideToolAsync(string userMessage, IReadOnlyList<ToolDef> tools)
    {
        var m = userMessage.ToLowerInvariant();
        if (m.Contains("convert") || m.Contains(" usd ") || m.Contains(" eur "))
            return Task.FromResult("convert-currency");
        return Task.FromResult("hello");
    }

    public Task<Dictionary<string, object?>> FillArgsAsync(string userMessage, ToolDef tool)
    {
        var args = new Dictionary<string, object?>();
        if (tool.Name == "hello")
        {
            var tokens = userMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var last = tokens.LastOrDefault();
            args["name"] = string.IsNullOrWhiteSpace(last) ? "there" : last.Trim('!', '.', ',');
        }
        else if (tool.Name == "convert-currency")
        {
            // naive parse like: "convert 25 usd to eur"
            var upper = userMessage.ToUpperInvariant();
            var toks = upper.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in toks)
                if (double.TryParse(t, out var a)) { args["amount"] = a; break; }
            args.TryAdd("from", toks.FirstOrDefault(t => t.Length == 3 && t.All(char.IsLetter)) ?? "USD");
            args.TryAdd("to", toks.LastOrDefault(t => t.Length == 3 && t.All(char.IsLetter)) ?? "EUR");
        }
        return Task.FromResult(args);
    }
}