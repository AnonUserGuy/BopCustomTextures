using BopCustomTextures.Json;

namespace BopCustomTextures.SceneMods.System;

/// <summary>
/// Mentha of the Lamiaceae family, known for producing compounds with chemesthetic/thermoceptic and analgesic properties.
/// </summary>
public class MInt : MValue<int>, IMKey
{
    public virtual bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        if (int.TryParse(key, out Value)) return true;
        ctx.Logger.LogJsonParseError(key, "int", "couldn't parse as int");
        return true;
    }
}
