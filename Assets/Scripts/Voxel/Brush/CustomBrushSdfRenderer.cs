using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Voxel
{
    [RequireComponent(typeof(CustomBrush))]
    [RequireComponent(typeof(MeshRenderer))]
    public class CustomBrushSdfRenderer : DynamicSdfShapeRenderer
    {
        [SerializeField] private VoxelWorldContainer parentWorld;

        private CustomBrush brush;
        private MeshRenderer meshRenderer;

        private VoxelWorld<MortonIndexer> world;

        public void Start()
        {
            brush = GetComponent<CustomBrush>();
            meshRenderer = GetComponent<MeshRenderer>();
            world = new VoxelWorld<MortonIndexer>(parentWorld.ChunkSize, parentWorld.CMSProperties, transform, parentWorld.IndexerFactory);

            world.ApplySdf(new Vector3(30, 30, 30), Quaternion.identity, new SphereSDF(10), 1, false, null);
        }

        void Update()
        {
            world.Update(meshRenderer);
        }

        public override void Render(Matrix4x4 transform)
        {
            world.Render(meshRenderer, transform);
        }

        public override Type SdfType()
        {
            return typeof(CustomBrushSdf);
        }

        public void OnApplicationQuit()
        {
            world.Dispose();
        }
    }
}
