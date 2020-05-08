using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Voxel
{
    public struct CustomBrushSdf : ISdf
    {
        public float Eval(float3 pos)
        {
            return 0;
        }

        public float3 Max()
        {
            return new float3(0, 0, 0);
        }

        public float3 Min()
        {
            return new float3(0, 0, 0);
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
