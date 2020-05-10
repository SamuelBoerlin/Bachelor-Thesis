using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
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

            /*brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Box, CreateVoxelTerrain.BrushOperation.Union, 5f, new float3(0.5f, 0.5f, 0.5f)));
            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Box, CreateVoxelTerrain.BrushOperation.Union, 5f, new float3(8.5f, 4.5f, 2.5f)));
            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Sphere, CreateVoxelTerrain.BrushOperation.Difference, 5.0f, new float3(8.5f, 4.5f, -3.5f)));*/

            /*brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Sphere, CreateVoxelTerrain.BrushOperation.Union, 5f, new float3(0.5f, 0.5f, 0.5f)));
            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Box, CreateVoxelTerrain.BrushOperation.Difference, 2f, new float3(8.5f, 0.5f, 0.5f)));
            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Box, CreateVoxelTerrain.BrushOperation.Difference, 2f, new float3(0.5f, 8.5f, 0.5f)));
            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Box, CreateVoxelTerrain.BrushOperation.Difference, 2f, new float3(0.5f, 0.5f, 8.5f)));
            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Box, CreateVoxelTerrain.BrushOperation.Difference, 2f, new float3(-7.5f, 0.5f, 0.5f)));
            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Box, CreateVoxelTerrain.BrushOperation.Difference, 2f, new float3(0.5f, -7.5f, 0.5f)));
            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Box, CreateVoxelTerrain.BrushOperation.Difference, 2f, new float3(0.5f, 0.5f, -7.5f)));*/

            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Box, CreateVoxelTerrain.BrushOperation.Union, 5f, new float3(0.5f, 0.5f, 0.5f)));
            brush.Primitives.Add(new CustomBrush.CustomBrushPrimitive(CreateVoxelTerrain.BrushType.Sphere, CreateVoxelTerrain.BrushOperation.Union, 3f, new float3(0.5f, 7.5f, 0.5f)));

            world.ApplySdf(new Vector3(30, 30, 30), Quaternion.identity, brush.CreateSdf(), 1, false, null);
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
