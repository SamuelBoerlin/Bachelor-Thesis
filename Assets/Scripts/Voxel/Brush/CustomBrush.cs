using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static CreateVoxelTerrain;

namespace Voxel
{
    public class CustomBrush : MonoBehaviour, IDisposable
    {
        public bool NeedsRebuild
        {
            private set;
            get;
        }

        public readonly struct CustomBrushPrimitive
        {
            public readonly BrushType type;
            public readonly BrushOperation operation;
            public readonly float blend;
            public readonly float3 position;

            public CustomBrushPrimitive(BrushType type, BrushOperation operation, float blend, float3 position)
            {
                this.type = type;
                this.operation = operation;
                this.blend = blend;
                this.position = position;
            }
        }

        public NativeList<CustomBrushPrimitive> Primitives {
            private set;
            get;
        }

        public void Awake()
        {
            Primitives = new NativeList<CustomBrushPrimitive>(Allocator.Persistent);
        }

        public void OnApplicationQuit()
        {
            Dispose();
        }

        public void Dispose()
        {
            Primitives.Dispose();
        }

        public CustomBrushSdf CreateSdf()
        {
            return new CustomBrushSdf(Primitives);
        }
    }
}
