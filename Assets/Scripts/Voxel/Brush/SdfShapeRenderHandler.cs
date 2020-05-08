using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voxel
{
    [RequireComponent(typeof(MeshRenderer))]
    public class SdfShapeRenderHandler : MonoBehaviour
    {
        public interface ISdfRenderer
        {
            Type SdfType();

            void Render(Matrix4x4 transform);
        }

        private Dictionary<Type, ISdfRenderer> registry = new Dictionary<Type, ISdfRenderer>();

        [SerializeField]
        private StaticSdfShapeRenderer[] staticRenderers = new StaticSdfShapeRenderer[0];

        [SerializeField]
        private DynamicSdfShapeRenderer[] dynamicRenderers = new DynamicSdfShapeRenderer[0];

        private void Start()
        {
            registry.Clear();
            foreach (ISdfRenderer renderer in staticRenderers)
            {
                registry[renderer.SdfType()] = renderer;
            }
            foreach (ISdfRenderer renderer in dynamicRenderers)
            {
                registry[renderer.SdfType()] = renderer;
            }
        }

        public void Render(Vector3 position, Quaternion rotation, ISdf sdf)
        {
            var transform = Matrix4x4.TRS(position, rotation, Vector3.one);
            var renderingTransform = Matrix4x4.identity;

            var transforms = new List<Matrix4x4>();

            int depth = 0;

            var rendering = sdf;
            var cur = sdf;
            while (cur != null)
            {
                rendering = cur;

                var curTransform = cur.RenderingTransform();
                if (curTransform.HasValue)
                {
                    transforms.Add(curTransform.Value);
                }

                cur = cur.RenderChild();

                if (++depth > 30)
                {
                    Debug.Log("Max SDF rendering depth reached");
                    break;
                }
            }

            if (registry.TryGetValue(rendering.GetType(), out ISdfRenderer renderer))
            {
                transforms.Reverse();

                foreach (var matrix in transforms)
                {
                    renderingTransform = matrix * renderingTransform;
                }

                renderingTransform = transform * renderingTransform;

                renderer.Render(renderingTransform);
            }
        }
    }
}
