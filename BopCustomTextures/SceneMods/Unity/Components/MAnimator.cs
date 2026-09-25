using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity.Components;

/// <summary>
/// <para>Scene mod <see cref="Animator"/> definition.</para>
/// 
/// <para>All <see cref="int"/> fields, properties, and function arguments 
/// use <see cref="MHashableInt"/> instead of <see cref="MInt"/>.</para>
/// </summary>
public class MAnimator : MComponent<Animator>
{
    public override bool TryJsonParse(CustomJsonInitializer ctx, Type type, JToken jtoken, out IMBase res)
    {
        if (type == typeof(int))
        {
            bool success = TryJsonParse<MHashableInt>(ctx, type, jtoken, out var res0);
            res = res0;
            return success;
        }
        return base.TryJsonParse(ctx, type, jtoken, out res);
    }
}
