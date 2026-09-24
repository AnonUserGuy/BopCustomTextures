using BopCustomTextures.Json;

namespace BopCustomTextures.SceneMods.System;

public class MBool : MValue<bool>, IMKey<bool>
{
    public bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        if (bool.TryParse(key, out Value)) return true;
        ctx.Logger.LogJsonParseError(key, "bool", "couldn't parse as bool");
        return true;
    }
}
