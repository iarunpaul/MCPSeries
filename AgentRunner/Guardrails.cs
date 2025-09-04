public static class Guardrails
{
    public static bool IsAllowed(string toolName) => toolName is "hello" or "convert-currency";

    public static (bool ok, string? error) ValidateArgs(ToolDef tool, Dictionary<string, object?> args)
    {
        // Keep it simple for the demo. You can add strict JSON Schema validation later.
        if (tool.Name == "hello" && (!args.TryGetValue("name", out var n) || string.IsNullOrWhiteSpace(n?.ToString())))
            return (false, "Missing name.");
        if (tool.Name == "convert-currency")
        {
            if (!args.TryGetValue("from", out var f) || f is null) return (false, "Missing 'from'.");
            if (!args.TryGetValue("to", out var t) || t is null) return (false, "Missing 'to'.");
            if (!args.TryGetValue("amount", out var a) || a is null) return (false, "Missing 'amount'.");
        }
        return (true, null);
    }

    public static bool NeedsConfirmation(ToolDef tool, Dictionary<string, object?> args)
    {
        if (tool.Name == "convert-currency" && args.TryGetValue("amount", out var v) &&
            double.TryParse(v?.ToString(), out var amt) && amt > 10000) return true;
        return false;
    }
}