using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.Base;
public class MIList: MIEnumerable<IList<object>>
{ 
    public List<MBase> Values;
    public Dictionary<int, MBase> ValuesIndexed;

    [SceneModParser(typeof(List<>))]
    [SceneModParser(typeof(IList<>))]
    public static bool TryJsonParse(Type type, JToken val, out MIList list)
    {
        if (!IsAssignable(type)) { list = null; return false; }
        var innerType = type.GetGenericArguments()[0];

        switch (val.Type)
        {
            case JTokenType.Array:
                list = new MIList()
                {
                    Values = []
                };
                foreach (var jel in (JArray)val)
                {
                    if (MBase.TryJsonParse(innerType, jel, out var el))
                    {
                        list.Values.Add(el);
                    }
                }
                return true;
            case JTokenType.Object:
                list = new MIList()
                {
                    ValuesIndexed = []
                };
                foreach(var pair in (JObject)val)
                {
                    if (!int.TryParse(pair.Key, out var index))
                    {
                        BopCustomTexturesPlugin.LogWarning($"JSON array key \"{pair.Key}\" is not an integer");
                        continue;
                    }
                    if (MBase.TryJsonParse(innerType, pair.Value, out var el))
                    {
                        list.ValuesIndexed[index] = el;
                    }
                }
                return true;
        }
        list = null;
        return false;
    }

    public override object Apply(object obj)
    {
        if (!TryGetAssigned(obj, out IList<object> list)) return obj;

        if (Values != null)
        {
            for (int i = 0; i < Values.Count; i++)
            {
                list[i] = Values[i].Apply(list[i]);
            }
            for (int i = list.Count - 1; i >= Values.Count; i--)
            {
                list.RemoveAt(i);
            }
        }
        else if (ValuesIndexed != null)
        {
            foreach (var pair in ValuesIndexed)
            {
                list[pair.Key] = pair.Value.Apply(list[pair.Key]);
            }
        }
        return list;
    }

    public static bool TryGetAssigned(object obj, out IList<object> list)
    {
        if (!TryGetAssigned(obj, out IEnumerable<object> enumerable))
        {
            list = null;
            return false;
        }
        list = [.. enumerable];
        return true;
    }
}
