using System;
using UnityEngine;

namespace Voxel
{
    [CreateAssetMenu(fileName = "MeshSdfRenderer", menuName = "ScriptableObjects/MeshSdfRenderer", order = 1)]
    public class MeshSdfRenderer : StaticSdfShapeRenderer
    {
        [SerializeField]
        private Mesh mesh = null;

        [SerializeField]
        private Material material = null;

        [SerializeField]
        private Material unionMaterial = null;

        [SerializeField]
        private Material differenceMaterial = null;

        [SerializeField]
        private Material replaceMaterial = null;

        public enum SdfTypes
        {
            Sphere,
            Box,
            Pyramid,
            Cylinder
        }

        [SerializeField]
        private SdfTypes sdfType = SdfTypes.Sphere;

        [SerializeField]
        private Vector3 translation = Vector3.zero;

        [SerializeField]
        private Vector3 scale = Vector3.one * 2;

        public override void Render(Matrix4x4 transform, SdfShapeRenderHandler.UniformSetter uniformSetter, BrushOperation operation, Material material = null)
        {
            if (material == null)
            {
                material = this.material;
            }
            switch (operation)
            {
                case BrushOperation.Union:
                    if(unionMaterial != null)
                    {
                        material = unionMaterial;
                    }
                    break;
                case BrushOperation.Difference:
                    if (differenceMaterial != null)
                    {
                        material = differenceMaterial;
                    }
                    break;
                case BrushOperation.Replace:
                    if (replaceMaterial != null)
                    {
                        material = replaceMaterial;
                    }
                    break;
            }
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            uniformSetter(properties);
            Graphics.DrawMesh(mesh, transform * Matrix4x4.TRS(translation, Quaternion.identity, scale), material, 0, null, 0, properties, false);
        }

        public override Type SdfType()
        {
            switch (sdfType)
            {
                default:
                case SdfTypes.Sphere:
                    return typeof(SphereSDF);
                case SdfTypes.Box:
                    return typeof(BoxSDF);
                case SdfTypes.Cylinder:
                    return typeof(CylinderSDF);
                case SdfTypes.Pyramid:
                    return typeof(PyramidSDF);
            }
        }
    }
}
