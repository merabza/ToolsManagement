using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ToolsManagement.Installer.ServiceInstaller;

//nftables.conf-ის ტექსტის ცვლილებები
public static partial class NftablesConf
{
    //პორტის დამატება სიმრავლის (set <name> { ... elements = { ... } }) elements-ის სიის ბოლოს. აბრუნებს null-ს, თუ
    //სიმრავლე ან მისი elements ტექსტში არ არის, და იმავე ტექსტს, თუ პორტი სიაში უკვე ცალკე ელემენტად წერია
    public static string? AddPortToSetElements(string conf, string setName, int port)
    {
        Match set = SetElementsRegex().Match(conf);
        while (set.Success && set.Groups["name"].Value != setName)
        {
            set = set.NextMatch();
        }

        if (!set.Success)
        {
            return null;
        }

        Group items = set.Groups["items"];
        string portText = port.ToString(CultureInfo.InvariantCulture);
        if (Array.Exists(items.Value.Split(',', StringSplitOptions.TrimEntries), x => x == portText))
        {
            return conf;
        }

        string trimmedItems = items.Value.TrimEnd();
        string newItems = trimmedItems.Length == 0 ? $" {portText} " : $"{trimmedItems}, {portText} ";
        return string.Concat(conf.AsSpan(0, items.Index), newItems, conf.AsSpan(items.Index + items.Length));
    }

    [GeneratedRegex(@"set\s+(?<name>[A-Za-z0-9_]+)\s*\{[^}]*?elements\s*=\s*\{(?<items>[^}]*)\}")]
    private static partial Regex SetElementsRegex();
}
