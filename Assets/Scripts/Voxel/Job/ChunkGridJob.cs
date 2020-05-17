using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Voxel
{
    [BurstCompile]
    public struct ChunkGridJob<TSourceIndexer, TTargetIndexer> : IJob
        where TSourceIndexer : struct, IIndexer
        where TTargetIndexer : struct, IIndexer
    {
        [ReadOnly] public NativeArray3D<Voxel, TSourceIndexer> source;
        [ReadOnly] public int tx, ty, tz, gx, gy, gz;
        [ReadOnly] public int chunkSize;
        [ReadOnly] public bool includePadding;
        [ReadOnly] public bool writeUnsetVoxels;

        public NativeArray3D<Voxel, TTargetIndexer> target;
        public NativeArray<int> voxelCount;

        public void Execute()
        {
            var width = source.Length(0);
            var height = source.Length(1);
            var depth = source.Length(2);

            var selectionSize = chunkSize + (includePadding ? 1 : 0);

            for (int z = 0; z < selectionSize; z++)
            {
                for (int y = 0; y < selectionSize; y++)
                {
                    for (int x = 0; x < selectionSize; x++)
                    {
                        var cvx = x + tx;
                        var cvy = y + ty;
                        var cvz = z + tz;

                        var gvx = x + gx;
                        var gvy = y + gy;
                        var gvz = z + gz;

                        if (cvx >= 0 && cvx < selectionSize && cvy >= 0 && cvy < selectionSize && cvz >= 0 && cvz < selectionSize &&
                            gvx >= 0 && gvx < width && gvy >= 0 && gvy < height && gvz >= 0 && gvz < depth)
                        {
                            Voxel sourceVoxel = source[gvx, gvy, gvz];
                            if (writeUnsetVoxels || sourceVoxel.Data.IsVoxelSet)
                            {
                                if (cvx < chunkSize && cvy < chunkSize && cvz < chunkSize)
                                {
                                    ChunkJobUtils.CompareMaterialsAndAdjustCounter(voxelCount, target[cvx, cvy, cvz].Material, sourceVoxel.Material);
                                }
                                target[cvx, cvy, cvz] = sourceVoxel;
                            }
                        }
                    }
                }
            }
        }
    }
}