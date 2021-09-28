using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("osOutline/Outline")]
public sealed class Outline : VolumeComponent, IPostProcessComponent
{

    public ClampedFloatParameter opacity = new ClampedFloatParameter(0f, 0f, 1f);
    public NoInterpColorParameter outlineColor = new NoInterpColorParameter(Color.clear, false);
    public NoInterpClampedFloatParameter normalDetectionThreshold = new NoInterpClampedFloatParameter(0.2f, 0f, 1f);
    public NoInterpClampedFloatParameter depthDetectionThreshold = new NoInterpClampedFloatParameter(0.2f, 0f, 1f);
    public BoolParameter objectIDDetection = new BoolParameter(true);
    public ColorParameter overwriteColor = new ColorParameter(Color.white);
    public ClampedFloatParameter ovewriteOpacity = new ClampedFloatParameter(0f, 0f, 1f);

    public bool IsActive() => (opacity.value > 0f || overwriteColor.value.a > 0f);

    /// <summary>
    /// Is the component compatible with on tile rendering
    /// </summary>
    /// <returns>false</returns>
    public bool IsTileCompatible() => false;

    

}
