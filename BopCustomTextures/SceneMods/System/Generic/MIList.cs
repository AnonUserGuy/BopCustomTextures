using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System.Generic;

public class MIList<M, T> : MIEnumerable<IList<T>, M, T>, IMIndexable<M> 
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

    public override IList<T> Apply(IList<T> list)
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
                list.Add(Values[i].Apply(default));
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

}