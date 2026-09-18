using System;
using System.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.Base;

public abstract class MIEnumerable<T> : MBase
{
    public static bool IsAssignable(Type type)
    {
        return type.IsGenericType && typeof(T).GetGenericTypeDefinition().IsAssignableFrom(type.GetGenericTypeDefinition());
    }

    public static bool TryGetAssigned(object obj, out IEnumerable<object> list)
    {
        if (!IsAssignable(obj.GetType()))
        {
            list = default;
            return false;
        }
        list = ((IEnumerable<T>)obj).Cast<object>();
        return true;
    }
}