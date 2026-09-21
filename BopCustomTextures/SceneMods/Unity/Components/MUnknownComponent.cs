using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Components;

public class MUnknownComponent(CustomJsonInitializer ctx, string name, JToken jcomponent)
{
    public CustomJsonInitializer Ctx = ctx;
    public string Name = name;
    public JToken JToken = jcomponent;
}
