using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Mathematics;
using Voxel;

public struct DefaultCustomBrushSdfEvaluator : IBrushSdfEvaluator<DefaultCustomBrushType>
{
    private readonly BrushProperties properties;

    public DefaultCustomBrushSdfEvaluator(BrushProperties properties)
    {
        this.properties = properties;
    }

    public float Eval(CustomBrushPrimitive<DefaultCustomBrushType> primitive, float3 pos)
    {
        if (primitive.type == DefaultCustomBrushType.BOX)
        {
            return new BoxSDF(properties.boxSize).Eval(pos);
        }
        else if (primitive.type == DefaultCustomBrushType.SPHERE)
        {
            return new SphereSDF(properties.sphereRadius).Eval(pos);
        }
        return 0.0f;
    }

    public float3 Max(CustomBrushPrimitive<DefaultCustomBrushType> primitive)
    {
        if (primitive.type == DefaultCustomBrushType.BOX)
        {
            return new BoxSDF(properties.boxSize).Max();
        }
        else if (primitive.type == DefaultCustomBrushType.SPHERE)
        {
            return new SphereSDF(properties.sphereRadius).Max();
        }
        return 0;
    }

    public float3 Min(CustomBrushPrimitive<DefaultCustomBrushType> primitive)
    {
        if (primitive.type == DefaultCustomBrushType.BOX)
        {
            return new BoxSDF(properties.boxSize).Min();
        }
        else if (primitive.type == DefaultCustomBrushType.SPHERE)
        {
            return new SphereSDF(properties.sphereRadius).Min();
        }
        return 0;
    }

    [BurstDiscard]
    public ISdf GetRenderSdf(CustomBrushPrimitive<DefaultCustomBrushType> primitive)
    {
        if (primitive.type == DefaultCustomBrushType.BOX)
        {
            return new BoxSDF(properties.boxSize);
        }
        else if (primitive.type == DefaultCustomBrushType.SPHERE)
        {
            return new SphereSDF(properties.sphereRadius);
        }
        return null;
    }
}
