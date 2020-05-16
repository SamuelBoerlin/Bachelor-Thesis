using UnityEngine;
using System.Collections;

namespace Voxel
{
    public class DefaultVoxelWorldContainer : VoxelWorldContainer<MortonIndexer>
    {
        protected override IndexerFactory<MortonIndexer> CreateIndexerFactory()
        {
            return (xSize, ySize, zSize) => new MortonIndexer(xSize, ySize, zSize);
        }
    }
}