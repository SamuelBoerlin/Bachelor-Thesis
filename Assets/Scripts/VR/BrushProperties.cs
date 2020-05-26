using UnityEngine;
using System.Collections;

public struct BrushProperties
{
    public float boxSize;
    public float sphereRadius;
    public float cylinderHeight;
    public float cylinderRadius;
    public float pyramidHeight;
    public float pyramidBase;

    public static readonly BrushProperties DEFAULT = new BrushProperties
    {
        boxSize = 10.0f,
        sphereRadius = 10.0f,
        cylinderHeight = 10.0f,
        cylinderRadius = 10.0f,
        pyramidHeight = 20.0f,
        pyramidBase = 20.0f
    };
}
