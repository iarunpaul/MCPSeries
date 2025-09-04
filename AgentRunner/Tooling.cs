using System.Text.Json;

public record ToolParameter(string Name);

public record ToolDef(string Name, string Description, IReadOnlyDictionary<string, ToolParameter> Parameters);

public static class ToolCatalog
{
    public static readonly ToolDef Hello = new(
        Name: "hello",
        Description: "Greets the given name quickly.",
        Parameters: new Dictionary<string, ToolParameter>
        {
            ["name"] = new("name")
        }
    );

    public static readonly ToolDef ConvertCurrency = new(
        Name: "convert-currency",
        Description: "Convert an amount between currencies using exchangerate.host/convert",
        Parameters: new Dictionary<string, ToolParameter>
        {
            ["from"] = new("from"),
            ["to"] = new("to"),
            ["amount"] = new("amount"),
        }
    );

    public static IReadOnlyList<ToolDef> All => new[] { Hello, ConvertCurrency };
}