using System;

namespace BopCustomTextures.Json;

public readonly struct MixtapeInfo(uint release, bool unsafeMode) : IEquatable<MixtapeInfo>
{
    public readonly uint Release = release;
    public readonly bool Unsafe = unsafeMode;

    public bool Equals(MixtapeInfo other) => Release == other.Release && Unsafe == other.Unsafe;

    public override bool Equals(object obj) => obj is MixtapeInfo other && Equals(other);

    public override int GetHashCode()
    {
        int hash = 17;
        hash = (hash * 23) + Release.GetHashCode();
        hash = (hash * 23) + Unsafe.GetHashCode();
        return hash;
    }

    public static bool operator ==(MixtapeInfo a, MixtapeInfo b) => a.Equals(b);

    public static bool operator !=(MixtapeInfo a, MixtapeInfo b) => !a.Equals(b);
}