using BopCustomTextures.SceneMods.Base;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;
public abstract class MBaseVector<T> : MValue<T> where T: struct
{
    public abstract int Width { get; }

    public abstract float this[int i] { get; set; }

    public MBaseVector(T val)
    {
        Value = val;
    }

    public MBaseVector()
    {
        Value = default;
        for (int i = 0; i < Width; i++)
        {
            this[i] = float.NaN;
        }
    }

    public MBaseVector(JArray jvector2)
    {
        for (int i = 0; i < Width && i < jvector2.Count; i++)
        {
            if (MFloat.TryJsonParse(jvector2[i], out float mfloat)) this[i] = mfloat;
        }
    }

    public MBaseVector(params float[] values)
    {
        for (int i = 0; i < Width && i < values.Length; i++)
        {
            this[i] = values[i];
        }
    }

    public float X
    {
        get => this[0];
        set => this[0] = value;
    }

    public float Y
    {
        get => this[1];
        set => this[1] = value;
    }

    public float Z
    {
        get => this[2];
        set => this[2] = value;
    }

    public float W
    {
        get => this[3];
        set => this[3] = value;
    }

    public float R
    {
        get => this[0];
        set => this[0] = value;
    }

    public float G
    {
        get => this[1];
        set => this[1] = value;
    }

    public float B
    {
        get => this[2];
        set => this[2] = value;
    }

    public float A
    {
        get => this[3];
        set => this[3] = value;
    }
}
