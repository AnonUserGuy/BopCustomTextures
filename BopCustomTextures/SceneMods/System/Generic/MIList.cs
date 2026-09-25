using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System.Generic;

/// <summary>
/// Scene mod <see cref="IList{T}"/> definition.
/// </summary>
/// <typeparam name="G">Target <see cref="IList{T}"/> type.</typeparam>
/// <typeparam name="M">Scene mod definition for element type.</typeparam>
/// <typeparam name="T">Element type.</typeparam>
public class MIList<G, M, T> : MObject<G>, IMIndexable<M> 
    where G : class, IList<T>, new()
    where M : IMBase<T>, new()
{
    private List<M> values;
    private Dictionary<int, M> valuesIndexed;

    public List<M> Values { get => values; set => values = value; }
    public Dictionary<int, M> ValuesIndexed { get => valuesIndexed; set => valuesIndexed = value; }

    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        return MIndexable.JsonParse<M, T>(ctx, val, this);
    }

    public override G Apply(G list)
    {
        if (Values != null)
        {
            int min = list.Count < Values.Count ? list.Count : Values.Count;
            for (int i = 0; i < min; i++)
            {
                list[i] = Values[i].Apply(list[i]);
            }
            for (int i = list.Count; i < Values.Count; i++)
            {
                list.Add(Values[i].Apply());
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
                if (list.Count > pair.Key)
                {
                    list[pair.Key] = pair.Value.Apply(list[pair.Key]);
                }
            }
        }
        return list;
    }

    public override G Apply()
    {
        if (Values != null)
        {
            G list = [];
            for (int i = 0; i < Values.Count; i++)
            {
                list[i] = Values[i].Apply();
            }
            return list;
        }
        return null;
    }
}