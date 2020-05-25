using UnityEngine;
using System.Collections;

namespace Voxel
{
    public interface IVoxelEditConsumer<TIndexer>
        where TIndexer : struct, IIndexer
    {
        void Consume(VoxelEdit<TIndexer> edit);
        bool IgnoreChunk(ChunkPos pos);
    }
}