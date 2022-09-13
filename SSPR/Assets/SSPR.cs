using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSPR : VolumeComponent, IPostProcessComponent
{
    public BoolParameter ssprEnable = new BoolParameter(false);
    
    public bool IsActive()
    {
        return ssprEnable.value;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
