using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System;

public interface IMIndexable: IMIndexable<IMBase>;

public interface IMIndexable<M> where M: IMBase
{
    public List<M> Values { get; set; }
    public Dictionary<int, M> ValuesIndexed { get; set; }
}

public static class MIndexable
{
    public static bool JsonParse<M, T>(CustomJsonInitializer ctx, JToken val, IMIndexable<M> mindexable) where M : IMBase<T>, new()
    {
        switch (val.Type)
        {
            case JTokenType.Array:
                mindexable.Values = [];
                foreach (var jel in (JArray)val)
                {
                    M mel = new();
                    if (mel.JsonParse(ctx, jel))
                    {
                        mindexable.Values.Add(mel);
                    }
                }
                return true;
            case JTokenType.Object:
                mindexable.ValuesIndexed = [];
                foreach (var pair in (JObject)val)
                {
                    if (!int.TryParse(pair.Key, out var index))
                    {
                        ctx.Logger.LogJsonParseError(val.Path, typeof(M).Name, $"key \"{pair.Key}\" isn't an int");
                        continue;
                    }
                    M mel = new();
                    if (mel.JsonParse(ctx, pair.Value))
                    {
                        mindexable.ValuesIndexed[index] = mel;
                    }
                }
                return true;
        }
        ctx.Logger.LogJsonParseError(val.Path, typeof(M).Name, "not object or array");
        return false;
    }
}
