using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Voxel.Voxelizer
{
    [BurstCompile]
    public struct VoxelizerApplyPatchesJob : IJob
    {
        public NativeQueue<VoxelizerFindPatchesJob.PatchedHole> queue;

        public NativeArray3D<Voxel> grid;

        public void Execute()
        {
            while (queue.TryDequeue(out VoxelizerFindPatchesJob.PatchedHole patch))
            {
                grid[patch.x, patch.y, patch.z] = grid[patch.x, patch.y, patch.z].ModifyEdge(patch.edge, patch.intersection.w, patch.intersection.xyz);
            }
        }
    }
}
