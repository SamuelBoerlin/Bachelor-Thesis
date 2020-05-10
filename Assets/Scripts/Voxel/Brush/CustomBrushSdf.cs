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
    public struct CustomBrushSdf : ISdf
    {
        private readonly NativeList<CustomBrush.CustomBrushPrimitive> primitives;

        public CustomBrushSdf(NativeList<CustomBrush.CustomBrushPrimitive> primitives)
        {
            this.primitives = primitives;
        }

        private OffsetSDF<TSdf> CreatePrimitive<TSdf>(CustomBrush.CustomBrushPrimitive primitive, TSdf baseSdf)
            where TSdf : struct, ISdf
        {
            return new OffsetSDF<TSdf>(-primitive.position, baseSdf);
        }

        private static readonly float BOX_SIZE = 5.0f;

        public float Eval(float3 pos)
        {
            float value = 0.0f;

            for (int i = 0, len = primitives.Length; i < len; i++)
            {
                var primitive = primitives[i];

                //Evaluate primitive
                float primitiveValue = 0.0f;
                switch(primitive.type)
                {
                    case CreateVoxelTerrain.BrushType.Box:
                        primitiveValue = CreatePrimitive(primitive, new BoxSDF(BOX_SIZE)).Eval(pos);
                        break;
                    case CreateVoxelTerrain.BrushType.Sphere:
                        primitiveValue = CreatePrimitive(primitive, new SphereSDF(BOX_SIZE)).Eval(pos);
                        break;
                        //TODO Other primitives, and use transform SDF instead of offset SDF
                }

                //Blend primitive
                if (i == 0)
                {
                    value = primitiveValue;
                }
                else
                {
                    float h;
                    switch (primitive.operation)
                    {
                        case CreateVoxelTerrain.BrushOperation.Union:
                            //h = math.clamp(0.5f + 0.5f * (primitiveValue - value) / primitive.blend, 0.0f, 1.0f);
                            //value = math.min(value, math.lerp(primitiveValue, value, h) - primitive.blend * h * (1.0f - h));
                            h = math.max(primitive.blend - math.abs(primitiveValue - value), 0.0f);
                            value = math.min(value, math.min(primitiveValue, value) - h * h * 0.25f / primitive.blend);
                            break;
                        case CreateVoxelTerrain.BrushOperation.Difference:
                            //h = math.clamp(0.5f - 0.5f * (primitiveValue + value) / primitive.blend, 0.0f, 1.0f);
                            //value = math.min(value, math.lerp(primitiveValue, -value, h) + primitive.blend * h * (1.0f - h));
                            h = math.max(primitive.blend - math.abs(-primitiveValue - value), 0.0f);
                            value = math.max(value, math.max(-primitiveValue, value) + h * h * 0.25f / primitive.blend);
                            break;
                    }
                }
            }

            return value;
        }

        public float3 Max()
        {
            if (primitives.Length == 0)
            {
                return new float3(0, 0, 0);
            }

            float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);
            float maxBlend = 0;

            for (int i = 0, len = primitives.Length; i < len; i++)
            {
                var primitive = primitives[i];
                switch (primitive.type)
                {
                    case CreateVoxelTerrain.BrushType.Box:
                        max = math.max(max, CreatePrimitive(primitive, new BoxSDF(BOX_SIZE)).Max());
                        break;
                    case CreateVoxelTerrain.BrushType.Sphere:
                        max = math.max(max, CreatePrimitive(primitive, new SphereSDF(BOX_SIZE)).Max());
                        break;
                        //TODO Other primitives, and use transform SDF instead of offset SDF
                }

                maxBlend = math.max(maxBlend, primitive.blend);
            }

            return max + maxBlend;
        }

        public float3 Min()
        {
            if (primitives.Length == 0)
            {
                return new float3(0, 0, 0);
            }

            float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            float maxBlend = 0;

            for (int i = 0, len = primitives.Length; i < len; i++)
            {
                var primitive = primitives[i];
                switch (primitive.type)
                {
                    case CreateVoxelTerrain.BrushType.Box:
                        min = math.min(min, CreatePrimitive(primitive, new BoxSDF(BOX_SIZE)).Min());
                        break;
                    case CreateVoxelTerrain.BrushType.Sphere:
                        min = math.min(min, CreatePrimitive(primitive, new SphereSDF(BOX_SIZE)).Min());
                        break;
                        //TODO Other primitives, and use transform SDF instead of offset SDF
                }

                maxBlend = math.max(maxBlend, primitive.blend);
            }

            return min - maxBlend;
        }

        public ISdf RenderChild()
        {
            return null;
        }

        public Matrix4x4? RenderingTransform()
        {
            return null;
        }
    }
}
