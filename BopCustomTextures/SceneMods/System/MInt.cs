using BopCustomTextures.Json;

namespace BopCustomTextures.SceneMods.System;

public class MInt : MValue<int>, IMKey<int>
{
    public virtual bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        if (int.TryParse(key, out Value)) return true;
        ctx.Logger.LogJsonParseError(key, "int", "couldn't parse as int");
        return true;
    }
}
