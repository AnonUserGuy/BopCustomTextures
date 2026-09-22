using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Scripts;

public class MVariant : MInt
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        if (jtoken.Type == JTokenType.String)
        {
            return ctx.TryGetVariant((string)jtoken, out Value);
        }
        return base.JsonParse(ctx, jtoken);
    }

    public override bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        return ctx.TryGetVariant(key, out Value);
    }
}
