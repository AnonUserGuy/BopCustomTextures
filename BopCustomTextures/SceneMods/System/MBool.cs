using BopCustomTextures.Json;

namespace BopCustomTextures.SceneMods.System;

/// <summary>
/// Scene mod <see cref="bool"/> definition.
/// </summary>
public class MBool : MValue<bool>, IMKey<bool>
{
    public bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        if (bool.TryParse(key, out Value)) return true;
        ctx.Logger.LogJsonParseError(key, "bool", "couldn't parse as bool");
        return true;
    }
}
