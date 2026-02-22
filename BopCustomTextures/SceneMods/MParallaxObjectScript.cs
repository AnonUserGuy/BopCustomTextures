namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod ParallaxObjectScript definition
/// </summary>
public class MParallaxObjectScript : MBehaviour<ParallaxObjectScript>
{
    public float? parallaxScale;
    public float? loopDistance;

    public override ParallaxObjectScript Apply(ParallaxObjectScript component)
    {
        base.Apply(component);
        if (parallaxScale != null) component.parallaxScale = (float)parallaxScale;
        if (loopDistance != null) component.loopDistance = (float)loopDistance;
        return component;
    }
}
