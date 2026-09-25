namespace BopCustomTextures.Json;

public readonly struct MixtapeInfo(uint release, bool unsafeMode)
{
    public readonly uint Release = release;
    public readonly bool Unsafe = unsafeMode;
}