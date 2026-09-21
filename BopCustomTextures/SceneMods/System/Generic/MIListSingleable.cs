using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.System.Generic;

public class MIListSingleable<M, T> : MIList<M, T> where M : IMBase<T>, new()
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        M mel = new();
        if (mel.JsonParse(ctx, val))
        {
            Values = [mel];
            return true;
        }

        return base.JsonParse(ctx, val);
    }
}
