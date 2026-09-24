using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System.Generic;

[NotMType] // Too weird to be registered, registry uses class directly
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
            T[] newArray = new T[Values.Count];

            int min = array.Length < Values.Count ? array.Length : Values.Count;
            for (int i = 0; i < min; i++)
            {
                newArray[i] = Values[i].Apply(array[i]);
            }
            for (int i = array.Length; i < Values.Count; i++)
            {
                newArray[i] = Values[i].Apply();
            }
            return newArray;
        }
        else if (ValuesIndexed != null)
        {
            foreach (var pair in ValuesIndexed)
            {
                if (array.Length > pair.Key)
                {
                    array[pair.Key] = pair.Value.Apply(array[pair.Key]);
                }
            }
        }
        return array;
    }

    public override T[] Apply()
    {
        if (Values != null)
        {
            T[] newArray = new T[Values.Count];
            for (int i = 0; i < Values.Count; i++)
            {
                newArray[i] = Values[i].Apply();
            }
            return newArray;
        }
        return null;
    }

    public override void ApplyReadOnly(T[] array)
    {
        if (Values != null)
        {
            int min = array.Length < Values.Count ? array.Length : Values.Count;
            for (int i = 0; i < min; i++)
            {
                array[i] = Values[i].Apply(array[i]);
            }
        }
        else if (ValuesIndexed != null)
        {
            foreach (var pair in ValuesIndexed)
            {
                array[pair.Key] = pair.Value.Apply(array[pair.Key]);
            }
        }
    }
}