namespace BopCustomTextures.SceneMods;

public class MParallaxObjectScript : MComponent
{
    public float? parallaxScale;
    public float? loopDistance;

    public void Apply(ParallaxObjectScript component)
    {
        if (parallaxScale != null) component.parallaxScale = (float)parallaxScale;
        if (loopDistance != null) component.loopDistance = (float)loopDistance;
    }
}
