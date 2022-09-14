using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSPR : VolumeComponent, IPostProcessComponent
{
    public BoolParameter SSPREnable = new BoolParameter(false);
    public IntParameter RTSize = new IntParameter(512);
    public FloatParameter HorizontalReflectionPlaneHeightWS = new FloatParameter(0.01f);
    [Range(0,8f)]
    public FloatParameter ScreenLRStretchIntensity = new FloatParameter(4f);
    [Range(-1f,1f)]
    public FloatParameter ScreenLRStretchThreshold = new FloatParameter(0.7f);
    [Range(0.01f, 1f)]
    public FloatParameter FadeOutScreenBorderWidthVerticle = new FloatParameter(0.25f);
    [Range(0.01f, 1f)]
    public FloatParameter FadeOutScreenBorderWidthHorizontal = new FloatParameter(0.35f);

    public ColorParameter TintColor = new ColorParameter(Color.white);
    public bool IsActive()
    {
        return SSPREnable.value;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
