using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System.Generic;

public interface IMIndexable<M> where M : IMBase
{
    public List<M> Values { get; set; }
    public Dictionary<int, M> ValuesIndexed { get; set; }
}

public static class MIndexable
{
    public static bool JsonParse<M, T>(CustomJsonInitializer ctx, JToken val, IMIndexable<M> mindexable) where M : IMBase<T>, new()
    {
        M mel;
        switch (val)
        {
            case JArray jarray:
                mindexable.Values = [];
                foreach (var jel in jarray)
                {
                    mel = new();
                    if (mel.JsonParse(ctx, jel))
                    {
                        mindexable.Values.Add(mel);
                    }
                }
                return true;
            case JObject jobj:
                mindexable.ValuesIndexed = [];
                foreach (var pair in jobj)
                {
                    if (!int.TryParse(pair.Key, out var index))
                    {
                        ctx.Logger.LogJsonParseError(val.Path, typeof(M).Name, $"key \"{pair.Key}\" isn't an int");
                        continue;
                    }
                    mel = new();
                    if (mel.JsonParse(ctx, pair.Value))
                    {
                        mindexable.ValuesIndexed[index] = mel;
                    }
                }
                return true;
            default:
                mel = new();
                if (mel.JsonParse(ctx, val))
                {
                    mindexable.Values = [mel];
                    return true;
                }
                break;
        }
        ctx.Logger.LogJsonParseError(val.Path, typeof(M).Name, $"not object, array, or single {typeof(T).Name}");
        return false;
    }
}
