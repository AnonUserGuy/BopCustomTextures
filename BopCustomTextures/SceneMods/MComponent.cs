using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene Mod generic <see cref="Component"/> interface. Can be parsed from JSON and applied to a <see cref="GameObject"/>.
/// </summary>
public interface IMComponent
{
    public abstract bool JsonParse(CustomJsonInitializer ctx, JToken jcomponent);
    public abstract void Apply(GameObject obj);
}

/// <summary>
/// Scene Mod generic <see cref="Component"/> definition. Can be applied to a component of type T.
/// </summary>
public abstract class MComponent<T>: MObject<T>, IMComponent where T: Component
{
    public virtual bool JsonParse(CustomJsonInitializer ctx, JToken jcomponent)
    {
        if (jcomponent.Type != JTokenType.Object)
        {
            ctx.logger.LogWarning($"JSON Component is a {jcomponent.Type} when it should be a Object");
            return false;
        }
        JsonParse(ctx, (JObject)jcomponent);
        return true;
    }

    public abstract bool JsonParse(CustomJsonInitializer ctx, JObject jcomponent);

    public virtual void Apply(GameObject obj)
    {
        T component = obj.GetComponent<T>();
        if (component)
        {
            Apply(component);
        }
    }
}