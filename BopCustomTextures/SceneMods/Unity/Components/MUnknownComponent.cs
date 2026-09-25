using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Components;

/// <summary>
/// Temporary container of <see cref="MComponent{T}"/> definition that's still serialized. 
/// Once the target <see cref="UnityEngine.GameObject"/> is loaded and target <see cref="UnityEngine.Component"/> 
/// can be determined, will be replaced with <see cref="MComponent{T}"/>.
/// </summary>
/// <param name="ctx">Deserialization context.</param>
/// <param name="name"><see cref="UnityEngine.Component"/> name.</param>
/// <param name="jcomponent">Serialized <see cref="MComponent{T}"/> definition.</param>
public class MUnknownComponent(CustomJsonInitializer ctx, string name, JToken jcomponent)
{
    public CustomJsonInitializer Ctx = ctx;
    public string Name = name;
    public JToken JToken = jcomponent;
}
