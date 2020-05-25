using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voxel
{
    public class VoxelEditManager<TIndexer> : IDisposable, IVoxelEditConsumer<TIndexer>
        where TIndexer : struct, IIndexer
    {
        public int QueueSize
        {
            get;
            set;
        }

        private readonly VoxelWorld<TIndexer> world;

        private List<VoxelEdit<TIndexer>> edits;
        private List<VoxelEdit<TIndexer>> undone;

        private bool wasPreviousUndo = false;

        private bool _merge;
        /// <summary>
        /// Whether any queued up edits should be merged together.
        /// If true, any new queued edits will be held back until this is set to false again,
        /// then the held back edits are merged together into one edit and added to the edits list.
        /// </summary>
        public bool Merge
        {
            get
            {
                return _merge;
            }
            set
            {
                if (_merge && !value)
                {
                    _merge = false;
                    QueueMergeEdits();
                }
                else
                {
                    _merge = value;
                }
            }
        }

        private HashSet<ChunkPos> mergeQueuedChunks = new HashSet<ChunkPos>();
        private List<VoxelEdit<TIndexer>> mergeEdits = new List<VoxelEdit<TIndexer>>();

        public VoxelEditManager(VoxelWorld<TIndexer> world, int queueSize)
        {
            this.world = world;
            QueueSize = queueSize;
            edits = new List<VoxelEdit<TIndexer>>();
            undone = new List<VoxelEdit<TIndexer>>();
        }

        private class InternalConsumer : IVoxelEditConsumer<TIndexer>
        {
            internal List<VoxelEdit<TIndexer>> list;

            public void Consume(VoxelEdit<TIndexer> edit)
            {
                list.Add(edit);
            }

            public bool IgnoreChunk(ChunkPos pos)
            {
                return false;
            }
        }

        private readonly InternalConsumer internalConsumer = new InternalConsumer();

        public bool Undo()
        {
            wasPreviousUndo = true;

            if (edits.Count > 0)
            {
                if (undone.Count == 0)
                {
                    //We need a snapshot of the current state so that the latest edit can be undone as well

                    var latestSnapshots = new List<VoxelEdit<TIndexer>>();

                    var edit = edits[edits.Count - 1];
                    internalConsumer.list = latestSnapshots;
                    edit.Restore(internalConsumer);

                    undone.Add(new VoxelEdit<TIndexer>(world, latestSnapshots));

                    edits.Remove(edit);
                    undone.Add(edit);
                }
                else
                {
                    var edit = edits[edits.Count - 1];
                    edit.Restore(null);

                    edits.Remove(edit);
                    undone.Add(edit);
                }

                return true;
            }

            return false;
        }

        public bool Redo()
        {
            if (wasPreviousUndo && undone.Count > 0)
            {
                //No need to restore to before first undone edit
                var edit = undone[undone.Count - 1];
                undone.Remove(edit);
                edits.Add(edit);
            }

            wasPreviousUndo = false;

            if (undone.Count > 0)
            {
                var edit = undone[undone.Count - 1];
                edit.Restore(null);

                undone.Remove(edit);

                //Don't re-add snapshot of the original state
                if (undone.Count != 0)
                {
                    edits.Add(edit);
                }
                else
                {
                    edit.Dispose();
                }

                return true;
            }

            return false;
        }

        private void RemoveEdit(VoxelEdit<TIndexer> edit)
        {
            edits.Remove(edit);
            edit.Dispose();
        }

        public void Consume(VoxelEdit<TIndexer> edit)
        {
            QueueEdit(edit);
        }

        public bool IgnoreChunk(ChunkPos pos)
        {
            return Merge && mergeQueuedChunks.Contains(pos);
        }

        private void QueueEdit(VoxelEdit<TIndexer> edit)
        {
            if (Merge)
            {
                //Hold back edit for merging later and excempt
                //all snapshotted chunks from being further snapshotted
                edit.IgnoreChunks(mergeQueuedChunks);
                mergeEdits.Add(edit);
            }
            else
            {
                if (edits.Count >= QueueSize)
                {
                    RemoveEdit(edits[0]);
                }

                edits.Add(edit);

                //Remove all undone edits because they cannot be redone anymore
                wasPreviousUndo = true;
                foreach (VoxelEdit<TIndexer> undoneEdit in undone)
                {
                    undoneEdit.Dispose();
                }
                undone.Clear();
            }
        }

        private void QueueMergeEdits()
        {
            if (!Merge)
            {
                var merged = new VoxelEdit<TIndexer>(world, mergeEdits);
                mergeEdits = new List<VoxelEdit<TIndexer>>();
                mergeQueuedChunks.Clear();
                QueueEdit(merged);
            }
            else
            {
                throw new InvalidOperationException("Cannot queue merged edits while " + nameof(Merge) + " is still true!");
            }
        }

        public IVoxelEditConsumer<TIndexer> Consumer()
        {
            return this;
        }

        public void Dispose()
        {
            foreach (VoxelEdit<TIndexer> edit in edits)
            {
                edit.Dispose();
            }
            edits.Clear();

            foreach (VoxelEdit<TIndexer> edit in undone)
            {
                edit.Dispose();
            }
            undone.Clear();

            foreach (VoxelEdit<TIndexer> edit in mergeEdits)
            {
                edit.Dispose();
            }
            mergeEdits.Clear();
            mergeQueuedChunks.Clear();
        }
    }
}
