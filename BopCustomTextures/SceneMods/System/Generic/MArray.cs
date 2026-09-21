using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System.Generic;

[NotMType] // Too weird to be parsed, parser uses class directly
public class MArray<M, T> : MBase<T[]>, IMIndexable<M> where M : IMBase<T>, new()
{
    private List<M> values;
    private Dictionary<int, M> valuesIndexed;

    public List<M> Values { get => values; set => values = value; }
    public Dictionary<int, M> ValuesIndexed { get => valuesIndexed; set => valuesIndexed = value; }

    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        return MIndexable.JsonParse<M, T>(ctx, val, this);
    }

    public override T[] Apply(T[] array)
    {
        if (Values != null)
        {
            T[] newArray = [];
            for (int i = 0; i < Values.Count; i++)
            {
                newArray[i] = Values[i].Apply(array[i]);
            }
            return newArray;
        }
        else if (ValuesIndexed != null)
        {
            foreach (var pair in ValuesIndexed)
            {
                array[pair.Key] = pair.Value.Apply(array[pair.Key]);
            }
        }
        return array;
    }

    public override bool IsAssignable(Type type)
    {
        return type.IsArray && IsInnerAssignable(type.GetElementType());
    }

    public bool IsInnerAssignable(Type innerType)
    {
        M dummy = new();
        return dummy.IsAssignable(innerType);
    }
}