using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Voxel
{
    [RequireComponent(typeof(CustomBrushContainer))]
    [RequireComponent(typeof(MeshRenderer))]
    public class CustomBrushSdfRenderer : DynamicSdfShapeRenderer
    {
        [SerializeField] private VoxelWorldContainer parentWorld;
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

        private CustomBrushContainer brush;
        private MeshRenderer meshRenderer;

        private VoxelWorld<MortonIndexer> world;

        public bool NeedsRebuild
        {
            get;
            set;
        } = true;

        public void Start()
        {
            brush = GetComponent<CustomBrushContainer>();
            meshRenderer = GetComponent<MeshRenderer>();
            world = new VoxelWorld<MortonIndexer>(parentWorld.ChunkSize, parentWorld.CMSProperties, transform, parentWorld.IndexerFactory);
        }

        void Update()
        {
            if(NeedsRebuild)
            {
                NeedsRebuild = false;
                world.Clear();
                world.ApplySdf(new Vector3(0, 0, 0), Quaternion.identity, brush.Instance.CreateSdf(), 1, false, null);
            }

            world.Update(meshRenderer);
        }

        public override void Render(Matrix4x4 transform)
        {
            if(RenderSurface)
            {
                world.Render(meshRenderer, transform);
            }
            else if (sdfRenderer != null)
            {
                foreach (var primitive in brush.Instance.Primitives)
                {
                    ISdf renderSdf = brush.Instance.Evaluator.GetRenderSdf(primitive);
                    if (renderSdf != null)
                    {
                        sdfRenderer.Render(transform * (Matrix4x4)primitive.transform, renderSdf);
                    }
                }
            }
        }

        public override Type SdfType()
        {
            return brush.Instance.GetSdfType();
        }

        public void OnApplicationQuit()
        {
            world.Dispose();
        }
    }
}
