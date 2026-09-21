using System;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System.Generic;

public abstract class MIEnumerable<G, M, T> : MObject<G>
    where G : class, IEnumerable<T>
    where M : IMBase<T>, new()
{

    public override bool IsAssignable(Type type)
    {
        return type.IsGenericType && typeof(G).GetGenericTypeDefinition().IsAssignableFrom(type.GetGenericTypeDefinition())
            && IsInnerAssignable(type.GetGenericArguments()[0]);
    }

    public bool IsInnerAssignable(Type innerType)
    {
        M dummy = new();
        return dummy.IsAssignable(innerType);
    }
}