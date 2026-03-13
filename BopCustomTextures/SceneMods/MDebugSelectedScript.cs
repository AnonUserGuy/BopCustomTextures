#if DEBUG
using BopCustomTextures.Json;
using BopCustomTextures.Scripts;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods;

[MComponent("debug")]
public class MDebugSelectedScript : IMComponent
{
    public bool JsonParse(CustomJsonInitializer ctx, JToken jcomponent)
    {
        return true;
    }

    public void Apply(GameObject obj)
    {
        var script = obj.GetComponent<DebugSelectedScript>();
        if (script == null)
        {
            script = obj.AddComponent<DebugSelectedScript>();
        }
        script.count++;
    }
}
#endif