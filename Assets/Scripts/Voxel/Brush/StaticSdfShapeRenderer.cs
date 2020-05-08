using System;
using UnityEngine;

namespace Voxel
{
    public class StaticSdfShapeRenderer : ScriptableObject, SdfShapeRenderHandler.ISdfRenderer
    {
        public virtual void Render(Matrix4x4 transform)
        {

        }

        public virtual Type SdfType()
        {
            return null;
        }
    }
}
