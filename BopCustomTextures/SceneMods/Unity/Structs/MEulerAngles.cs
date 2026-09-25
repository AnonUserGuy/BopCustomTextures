using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity.Structs;

/// <summary>
/// Scene mod <see cref="Transform.eulerAngles"/> definition. Single floats will replace target <see cref="Vector3"/>'s Z channel.
/// </summary>
public class MEulerAngles : MVector3
{
    protected override string LogInvalidTypeMsg => "not float, int, object, or array"; 
    // "Infinity" or "-Infinity" too, but why would you ever set that in practice?

    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        if (val.Type == JTokenType.Float || val.Type == JTokenType.Integer || val.Type == JTokenType.String)
        {
            if (!MFloat.TryJsonParse(ctx, val, out float mfloat))
            {
                ctx.Logger.LogJsonParseError(val.Path, "euler angles", "single channel wasn't parseable as float");
                return false;
            }
            InitValue();
            this[2] = mfloat;
            return true;
        }
        return base.JsonParse(ctx, val);
    }

    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken val, out Vector3 vector)
    {
        var mvector = new MEulerAngles();
        if (mvector.JsonParse(null, val))
        {
            vector = mvector.Value;
            return true;
        }
        vector = default;
        return false;
    }
}