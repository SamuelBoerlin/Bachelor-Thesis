using UnityEngine;
using System.Collections;
using System;

namespace Voxel
{
    public abstract class VoxelChunkContainer<TIndexer> : MonoBehaviour
        where TIndexer : struct, IIndexer
    {
        private VoxelChunk<TIndexer> _chunk;
        public VoxelChunk<TIndexer> Chunk
        {
            internal set
            {
                _chunk = value;
                OnSetChunk(value);
            }
            get
            {
                return _chunk;
            }
        }

        public Type ChunkType
        {
            get
            {
                return typeof(VoxelChunk<TIndexer>);
            }
        }

        public Type IndexerType
        {
            get
            {
                return typeof(TIndexer);
            }
        }

        protected virtual void OnSetChunk(VoxelChunk<TIndexer> chunk)
        {

        }
    }
}
