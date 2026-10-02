using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hoellenspiralenspiel.Tests.Balance;

//Liest eine .tres-Datei so weit, wie die Bilanz sie braucht: Verweise, Unter-Resources und die Felder der Resource.
//Ein fehlendes Feld steht nicht in der Datei, weil es den Standard der Resource hat
internal sealed class TresFile
{
    private readonly Dictionary<string, string>      extResources = new();
    private readonly Dictionary<string, TresSection> subResources = new();

    private TresFile(string path)
        => Path = path;

    public string Path { get; }

    public TresSection Resource { get; private set; } = new(new Dictionary<string, string>());

    public static TresFile Read(string path)
    {
        var file    = new TresFile(path);
        var current = (Dictionary<string, string>)null;
        var key     = (string)null;
        var value   = new StringBuilder();

        foreach (var line in File.ReadLines(path))
        {
            if (key is not null)
            {
                value.Append('\n').Append(line);

                if (!IsComplete(value.ToString()))
                    continue;

                current![key] = value.ToString();
                key           = null;

                continue;
            }

            if (line.StartsWith("[ext_resource", StringComparison.Ordinal))
            {
                file.extResources[Attribute(line, "id")] = Attribute(line, "path");

                continue;
            }

            if (line.StartsWith("[sub_resource", StringComparison.Ordinal))
            {
                current                                  = new Dictionary<string, string>();
                file.subResources[Attribute(line, "id")] = new TresSection(current);

                continue;
            }

            if (line.StartsWith("[resource]", StringComparison.Ordinal))
            {
                current       = new Dictionary<string, string>();
                file.Resource = new TresSection(current);

                continue;
            }

            if (current is null || line.StartsWith('['))
                continue;

            var separator = line.IndexOf(" = ", StringComparison.Ordinal);

            if (separator < 0)
                continue;

            var name = line[..separator];
            var text = line[(separator + 3)..];

            if (IsComplete(text))
            {
                current[name] = text;

                continue;
            }

            key = name;

            value.Clear().Append(text);
        }

        return file;
    }

    public string ResolveExt(string id)
        => extResources.TryGetValue(id, out var path) ? path : throw new InvalidDataException($"{Path}: kein ext_resource mit der Id {id}");

    public TresSection Sub(string id)
        => subResources.TryGetValue(id, out var section) ? section : throw new InvalidDataException($"{Path}: keine sub_resource mit der Id {id}");

    //Der Pfad des Skripts sagt, welche Klasse die Resource ist
    public string ScriptPath => Resource.Reference("script") is { IsExternal: true } script ? ResolveExt(script.Id) : string.Empty;

    private static string Attribute(string line, string name)
    {
        var match = Regex.Match(line, $"\\b{name}=\"([^\"]*)\"");

        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    //Ein Wert läuft über mehrere Zeilen, solange Klammern offen sind
    private static bool IsComplete(string text)
    {
        var depth    = 0;
        var isQuoted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];

            if (character == '\\' && isQuoted)
            {
                i++;

                continue;
            }

            if (character == '"')
                isQuoted = !isQuoted;
            else if (!isQuoted && character is '(' or '[' or '{')
                depth++;
            else if (!isQuoted && character is ')' or ']' or '}')
                depth--;
        }

        return depth <= 0 && !isQuoted;
    }
}

internal readonly record struct TresReference(bool IsExternal, string Id);

internal sealed partial class TresSection
{
    private readonly IReadOnlyDictionary<string, string> values;

    public TresSection(IReadOnlyDictionary<string, string> values)
        => this.values = values;

    public bool Has(string key)
        => values.ContainsKey(key);

    public string String(string key, string fallback = "")
        => values.TryGetValue(key, out var text) ? Unquote(text) : fallback;

    public float Float(string key, float fallback)
        => values.TryGetValue(key, out var text) ? float.Parse(text, CultureInfo.InvariantCulture) : fallback;

    public double Double(string key, double fallback)
        => values.TryGetValue(key, out var text) ? double.Parse(text, CultureInfo.InvariantCulture) : fallback;

    public int Int(string key, int fallback)
        => values.TryGetValue(key, out var text) ? int.Parse(text, CultureInfo.InvariantCulture) : fallback;

    public bool Bool(string key, bool fallback)
        => values.TryGetValue(key, out var text) ? text == "true" : fallback;

    public TEnum Enum<TEnum>(string key, TEnum fallback) where TEnum : struct, Enum
        => values.TryGetValue(key, out var text) ? (TEnum)System.Enum.ToObject(typeof(TEnum), int.Parse(text, CultureInfo.InvariantCulture)) : fallback;

    public TresReference? Reference(string key)
        => values.TryGetValue(key, out var text) ? ReferencesIn(text).FirstOrDefault() : null;

    //Ein getyptes Array nennt seinen Typ vorn in eckigen Klammern, die Einträge stehen in "([...])"
    public IReadOnlyList<TresReference> References(string key)
    {
        if (!values.TryGetValue(key, out var text))
            return [];

        var start = text.IndexOf("([", StringComparison.Ordinal);

        return ReferencesIn(start < 0 ? text : text[start..]).ToList();
    }

    private static IEnumerable<TresReference> ReferencesIn(string text)
        => ReferencePattern().Matches(text).Select(match => new TresReference(match.Groups[1].Value == "ExtResource", match.Groups[2].Value));

    private static string Unquote(string text)
        => text.Length >= 2 && text[0] == '"' && text[^1] == '"' ? text[1..^1] : text;

    [GeneratedRegex("(ExtResource|SubResource)\\(\"([^\"]+)\"\\)")]
    private static partial Regex ReferencePattern();
}
