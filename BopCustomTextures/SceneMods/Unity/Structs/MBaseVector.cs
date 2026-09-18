using BopCustomTextures.SceneMods.Base;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;
public abstract class MBaseVector<T>(int width) : MBase<T>
{
    public MFloat[] Values = new MFloat[width];

    public MBaseVector(int width, JArray jvector2) : this(width)
    {
        MFloat mfloat;
        for (int i = 0; i < width && i < jvector2.Count; i++)
        {
            if (MFloat.TryJsonParse(jvector2[i], out mfloat)) Values[i] = mfloat;
        }
    }

    public MBaseVector(int width, params MFloat[] values): this(width)
    {
        for (int i = 0; i < width && i < values.Length; i++)
        {
            Values[i] = values[i];
        }
    }

    public MFloat this[int i]
    {
        get => Values[i];
        set => Values[i] = value;
    }

    public MFloat X
    {
        get => Values[0];
        set => Values[0] = value;
    }

    public MFloat Y
    {
        get => Values[1];
        set => Values[1] = value;
    }

    public MFloat Z
    {
        get => Values[2];
        set => Values[2] = value;
    }

    public MFloat W
    {
        get => Values[3];
        set => Values[3] = value;
    }

    public MFloat R
    {
        get => Values[0];
        set => Values[0] = value;
    }

    public MFloat G
    {
        get => Values[1];
        set => Values[1] = value;
    }

    public MFloat B
    {
        get => Values[2];
        set => Values[2] = value;
    }

    public MFloat A
    {
        get => Values[3];
        set => Values[3] = value;
    }
}
