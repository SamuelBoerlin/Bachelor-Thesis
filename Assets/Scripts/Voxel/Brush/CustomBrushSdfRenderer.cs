using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Voxel
{
    public abstract class CustomBrushSdfRenderer<TIndexer, TBrushType, TEvaluator> : DynamicSdfShapeRenderer
        where TIndexer : struct, IIndexer
        where TBrushType : struct
        where TEvaluator : struct, IBrushSdfEvaluator<TBrushType>
    {
        [SerializeField] private SdfShapeRenderHandler sdfRenderer;

        [SerializeField] private bool renderSurface = true;
        public bool RenderSurface
        {
            get
            {
                return renderSurface;
            }
            set
            {
                renderSurface = value;
            }
        }

        [SerializeField] private bool renderPrimitives = true;
        public bool RenderPrimitives
        {
            get
            {
                return renderPrimitives;
            }
            set
            {
                renderPrimitives = value;
            }
        }

        [SerializeField] private Material surfaceMaterial;
        [SerializeField] private Material surfaceUnionMaterial;
        [SerializeField] private Material surfaceDifferenceMaterial;
        [SerializeField] private Material surfaceReplaceMaterial;

        [SerializeField] private Material primitiveUnionMaterial;
        [SerializeField] private Material primitiveDifferenceMaterial;

        private VoxelWorldContainer<TIndexer> _parentWorld;
        public VoxelWorldContainer<TIndexer> ParentWorld
        {
            get
            {
                if (_parentWorld == null)
                {
                    _parentWorld = GetParentWorld();
                }
                return _parentWorld;
            }
        }

        private CustomBrushContainer<TIndexer, TBrushType, TEvaluator> brush;

        private VoxelWorld<TIndexer> world;

        public bool NeedsRebuild
        {
            get;
            set;
        } = true;

        protected abstract VoxelWorldContainer<TIndexer> GetParentWorld();

        public void Start()
        {
            brush = GetComponent<CustomBrushContainer<TIndexer, TBrushType, TEvaluator>>();
            world = new VoxelWorld<TIndexer>(gameObject, null, transform, ParentWorld.ChunkSize, ParentWorld.CMSProperties, ParentWorld.IndexerFactory);
        }

        void Update()
        {
            if (NeedsRebuild)
            {
                NeedsRebuild = false;
                world.Clear();
                using (var sdf = brush.Instance.CreateSdf(Allocator.TempJob))
                {
                    world.ApplySdf(new Vector3(0, 0, 0), Quaternion.identity, sdf, 1, false, null);
                }
            }

            world.Update();
        }

        public override void Render(Matrix4x4 transform, SdfShapeRenderHandler.UniformSetter uniformSetter, BrushOperation operation, Material material = null)
        {
            Material surfaceMaterial;
            Material primitiveUnionMaterial;
            Material primitiveDifferenceMaterial;

            if (material != null)
            {
                surfaceMaterial = primitiveUnionMaterial = primitiveDifferenceMaterial = material;
            }
            else
            {
                surfaceMaterial = this.surfaceMaterial;
                primitiveUnionMaterial = this.primitiveUnionMaterial;
                primitiveDifferenceMaterial = this.primitiveDifferenceMaterial;
            }

            switch (operation)
            {
                case BrushOperation.Union:
                    if (surfaceUnionMaterial != null)
                    {
                        surfaceMaterial = surfaceUnionMaterial;
                    }
                    break;
                case BrushOperation.Difference:
                    if (surfaceDifferenceMaterial != null)
                    {
                        surfaceMaterial = surfaceDifferenceMaterial;
                    }
                    break;
                case BrushOperation.Replace:
                    if (surfaceReplaceMaterial != null)
                    {
                        surfaceMaterial = surfaceReplaceMaterial;
                    }
                    break;
            }

            if (RenderSurface)
            {
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                uniformSetter(properties);
                world.Render(transform, surfaceMaterial, properties);
            }

            if (RenderPrimitives && sdfRenderer != null)
            {
                foreach (var primitive in brush.Instance.Primitives)
                {
                    using (ISdf renderSdf = brush.Instance.Evaluator.GetRenderSdf(primitive))
                    {
                        if (renderSdf != null && !brush.Instance.SdfType.IsAssignableFrom(renderSdf.GetType()))
                        {
                            Material renderMaterial = primitive.operation == BrushOperation.Union ? primitiveUnionMaterial : primitiveDifferenceMaterial;
                            sdfRenderer.Render(transform * (Matrix4x4)primitive.transform, renderSdf, operation, renderMaterial);
                        }
                    }
                }
            }
        }

        public override Type SdfType()
        {
            return brush.Instance.SdfType;
        }

        public void OnApplicationQuit()
        {
            world.Dispose();
        }
    }
}
