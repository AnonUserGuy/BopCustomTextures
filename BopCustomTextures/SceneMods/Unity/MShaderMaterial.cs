using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity;

public class MShaderMaterial: MMaterial
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        needsNew = false;
        if (val.Type == JTokenType.String)
        {
            if (ctx.TryGetShaderMaterial((string)val, out var mshaderMaterial))
            {
                material = mshaderMaterial;
                return true;
            }
            ctx.Logger.LogJsonParseError(val.Path, "Shader", $"Couldn't find shader named \"{val}\"");
        }
        return false;
    }
}
