using System;
using UnityEngine;

namespace Voxel
{
    /// <summary>
    /// Static SDF shape renderers are intended to be used for simple SDF types that do not change,
    /// such as for example boxes, spheres, etc.
    /// </summary>
    public abstract class StaticSdfShapeRenderer : ScriptableObject, SdfShapeRenderHandler.ISdfRenderer
    {
        public abstract void Render(Matrix4x4 transform, SdfShapeRenderHandler.UniformSetter uniformSetter, BrushOperation operation, Material material = null);

        public virtual Type SdfType()
        {
            return null;
        }
    }
}
