using System;
using UnityEngine;

namespace Voxel
{
    /// <summary>
    /// Dynamic SDF renderers are used by SDFs that change their properties during runtime. An example
    /// for this are <see cref="CustomBrush{TBrushType, TEvaluator}"/>'s.
    /// </summary>
    public abstract class DynamicSdfShapeRenderer : MonoBehaviour, SdfShapeRenderHandler.ISdfRenderer
    {
        public abstract void Render(Matrix4x4 transform, SdfShapeRenderHandler.UniformSetter uniformSetter, Material material = null);

        public virtual Type SdfType()
        {
            return null;
        }
    }
}
