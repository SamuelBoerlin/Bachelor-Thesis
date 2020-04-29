using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Voxel
{
    [BurstCompile]
    public struct ChunkGridJob : IJob
    {
        [ReadOnly] public NativeArray3D<Voxel> source;
        [ReadOnly] public int sx, sy, sz, gx, gy, gz;

        [ReadOnly] public int chunkSize;
        [WriteOnly] public NativeArray3D<Voxel> target;

        public void Execute()
        {
            var width = source.Length(0);
            var height = source.Length(1);
            var depth = source.Length(2);

            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        var cvx = x + sx;
                        var cvy = y + sy;
                        var cvz = z + sz;

                        var gvx = x + gx;
                        var gvy = y + gy;
                        var gvz = z + gz;

                        if (cvx >= 0 && cvx < chunkSize && cvy >= 0 && cvy < chunkSize && cvz >= 0 && cvz < chunkSize &&
                            gvx >= 0 && gvx < width && gvy >= 0 && gvy < height && gvz >= 0 && gvz < depth)
                        {
                            target[cvx, cvy, cvz] = source[gvx, gvy, gvz];
                        }
                    }
                }
            }
        }
    }
}