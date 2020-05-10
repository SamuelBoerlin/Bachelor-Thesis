using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Voxel
{
    /// <summary>
    /// Used by <see cref="CustomBrushSdf{TBrushType, TEvaluator}"/> to evaluate the SDF for a given brush type.
    /// Since the Burst compiler disallows usage of non-blittable types and inheritance the SDFs must be evaluated by
    /// explicitely checking the brush type.
    /// </summary>
    /// <typeparam name="TBrushType"></typeparam>
    public interface IBrushSdfEvaluator<TBrushType>
        where TBrushType : struct
    {
        float Eval(CustomBrushPrimitive<TBrushType> primitive, float3 pos);
        float3 Max(CustomBrushPrimitive<TBrushType> primitive);
        float3 Min(CustomBrushPrimitive<TBrushType> primitive);
    }
}
