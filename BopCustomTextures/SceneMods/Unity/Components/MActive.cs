using BopCustomTextures.Json;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Components;

/// <summary>
/// Scene mod pseudo-component definition for the active/inactive state of a <see cref="GameObject"/>.
/// </summary>
[MComponent("Active")]
public class MActive : IMComponent
{
    public bool value;

    public bool JsonParse(CustomJsonInitializer ctx, JToken jcomponent)
    {
        if (jcomponent.Type != JTokenType.Boolean)
        {
            ctx.Logger.LogJsonParseError(jcomponent.Path, "!Active", "not a bool");
            return false;
        }
        value = (bool)jcomponent;
        return true;
    }

    public void Apply(GameObject obj)
    {
        obj.SetActive(value);
    }
}
