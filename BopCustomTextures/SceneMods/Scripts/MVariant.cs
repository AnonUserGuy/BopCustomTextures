using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Scripts;

[NotMType]
public class MVariant : MValue<int>
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        if (jtoken.Type == JTokenType.String)
        {
            return ctx.TryGetVariant((string)jtoken, out Value);
        }
        return base.JsonParse(ctx, jtoken);
    }
}
