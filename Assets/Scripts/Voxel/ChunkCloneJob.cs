using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Voxel
{
    [BurstCompile]
    public struct ChunkCloneJob : IJob
    {
        [ReadOnly] public NativeArray3D<Voxel> source;
        [ReadOnly] public int chunkSize;

        [WriteOnly] public NativeArray3D<Voxel> target;

        public void Execute()
        {
            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        target[x, y, z] = source[x, y, z];
                    }
                }
            }
        }
    }
}
