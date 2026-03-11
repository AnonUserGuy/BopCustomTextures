using BopCustomTextures.SceneMods;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.Json;

/// <summary>
/// Manages deserializing of scene mod component definitions through registered deserializer functions.
/// </summary>
public static class MComponentParserRegistry
{
    private static readonly Dictionary<string, JsonParse> _map = [];

    /// <summary>
    /// A function to use to deserialize a scene mod component.
    /// </summary>
    /// <param name="ctx">The invoking <see cref="CustomJsonInitializer"/>, for logging and general JSON deserializing methods.</param>
    /// <param name="jcomponent">JSON component definition.</param>
    /// <returns>The deserialized <see cref="MComponent"/> object.</returns>
    public delegate MComponent JsonParse(CustomJsonInitializer ctx, JObject jcomponent);

    /// <summary>
    /// Register a scene mod component parser.
    /// </summary>
    /// <param name="name">String name of component as used in JSON, excluding a ! at beginning.</param>
    /// <param name="parse">Method to use to parse the jcomponent into an mcomponent.</param>
    public static void Register(string name, JsonParse parse)
    {
        if (name.StartsWith("!"))
        {
            name = name.Substring(1);
        }
        _map[name] = parse;
    }

    public static MComponent Parse(CustomJsonInitializer ctx, string name, JObject jcomponent)
    {
        if (_map.TryGetValue(name, out var parse))
        {
            return parse(ctx, jcomponent);
        }
        return null;
    }
}
