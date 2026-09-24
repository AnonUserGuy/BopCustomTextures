using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public abstract class MBaseVector<T> : MValue<T> where T: struct
{
    protected virtual string LogInvalidTypeMsg => "not object or array";

    public abstract int Width { get; }

    public abstract float this[int i] { get; set; }

    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        switch (jtoken)
        {
            case JObject jobj:
                InitValue();
                return JsonParse(ctx, jobj);
            case JArray jarray:
                InitValue();
                return JsonParse(ctx, jarray);
        }
        ctx.Logger.LogJsonParseError(jtoken.Path, typeof(T).Name, LogInvalidTypeMsg);
        return false;
    }

    public void InitValue()
    {
        Value = default;
        for (int i = 0; i < Width; i++)
        {
            this[i] = float.NaN;
        }
    }

    public abstract bool JsonParse(CustomJsonInitializer ctx, JObject jobj);

    public virtual bool JsonParse(CustomJsonInitializer ctx, JArray jvector)
    {
        for (int i = 0; i < Width && i < jvector.Count; i++)
        {
            if (MFloat.TryJsonParse(ctx, jvector[i], out float mfloat)) this[i] = mfloat;
        }
        return true;
    }
}
