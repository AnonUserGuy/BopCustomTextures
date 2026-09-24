using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity;
public class MShader : MUnityObject<Shader>
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        if (val.Type == JTokenType.String)
        {
            if (ctx.TryGetShader((string)val, out var mshader))
            {
                Ref = mshader;
                HasRef = true;
                return true;
            }
            ctx.Logger.LogJsonParseError(val.Path, "Shader", $"Couldn't find shader named \"{val}\"");
            return false;
        } 
        else
        {
            return base.JsonParse(ctx, val);
        }
    }

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if (val.Type == JTokenType.String
            && (KeyMatch(key, "Name") || KeyMatch(key, "Shader"))
            && ctx.TryGetShader((string)val, out var mshader))
        {
            Ref = mshader;
            HasRef = true;
        }
        else base.JsonParsePair(ctx, key, val);
    }
}
