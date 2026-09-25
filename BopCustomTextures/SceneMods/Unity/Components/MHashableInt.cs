using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Components;

/// <summary>
/// Scene mod <see cref="int"/> definition for ints that can accept a string hashed via <see cref="Animator.StringToHash"/>.
/// Used by <see cref="MAnimator"/>.
/// </summary>
public class MHashableInt : MInt
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        return TryJsonParse(ctx, jtoken, out Value);
    }

    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken jtoken, out int res)
    {
        if (jtoken.Type == JTokenType.String)
        {
            res = Animator.StringToHash((string)jtoken);
            return true;
        }

        return MInt.TryJsonParse(ctx, jtoken, out res);
    }

    public override bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        if (int.TryParse(key, out Value)) return true;
        Value = Animator.StringToHash(key);
        return true;
    }
}
