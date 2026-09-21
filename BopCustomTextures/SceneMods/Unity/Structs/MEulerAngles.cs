using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MEulerAngles : MVector3
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        if (val.Type == JTokenType.Float || val.Type == JTokenType.Integer)
        {
            InitValue();
            if (!MFloat.TryJsonParse(ctx, val, out float mfloat))
            {
                return false;
            }
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