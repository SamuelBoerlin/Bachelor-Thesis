using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voxel
{
    public class VoxelEdit : IDisposable
    {
        private readonly VoxelWorld world;
        private readonly List<VoxelChunk> snapshots;

        public VoxelEdit(VoxelWorld world, List<VoxelChunk> snapshots)
        {
            this.world = world;
            this.snapshots = snapshots;
        }

        /// <summary>
        /// Merges multiple voxel edits into one edit.
        /// The ownership of the chunk snapshots is transferred entirely to the new merged voxel edit!
        /// </summary>
        /// <param name="world"></param>
        /// <param name="edits"></param>
        public VoxelEdit(VoxelWorld world, List<VoxelEdit> edits)
        {
            this.world = world;

            snapshots = new List<VoxelChunk>();
            foreach (var edit in edits)
            {
                snapshots.AddRange(edit.snapshots);
            }
        }

        /// <summary>
        /// Restores the voxels to before the edit was applied
        /// </summary>
        /// <param name="edits">Consumes the voxel edit. Can be null if no voxel edits should be stored</param>
        public void Restore(VoxelWorld.VoxelEditConsumer edits)
        {
            if (edits != null)
            {
                foreach (VoxelChunk snapshot in snapshots)
                {
                    //TODO Queue all jobs at once
                    world.ApplyGrid(snapshot.Pos.x * snapshot.ChunkSize, snapshot.Pos.y * snapshot.ChunkSize, snapshot.Pos.z * snapshot.ChunkSize, snapshot.Voxels, false, true, edits, false);
                }
            }

            foreach (VoxelChunk snapshot in snapshots)
            {
                //TODO Queue all jobs at once
                world.ApplyGrid(snapshot.Pos.x * snapshot.ChunkSize, snapshot.Pos.y * snapshot.ChunkSize, snapshot.Pos.z * snapshot.ChunkSize, snapshot.Voxels, false, true, null);
            }
        }

        public void Dispose()
        {
            foreach (VoxelChunk snapshot in snapshots)
            {
                snapshot.Dispose();
            }
            snapshots.Clear();
        }
    }
}
