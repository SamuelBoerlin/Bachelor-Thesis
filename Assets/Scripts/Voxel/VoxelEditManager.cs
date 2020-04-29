using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voxel
{
    [RequireComponent(typeof(VoxelWorld))]
    public class VoxelEditManager : MonoBehaviour, IDisposable
    {
        [SerializeField] private int queueSize = 5;

        private VoxelWorld world;

        private List<VoxelEdit> edits;
        private List<VoxelEdit> undone;

        private bool firstRedo = false;

        public void Start()
        {
            world = GetComponent<VoxelWorld>();
            edits = new List<VoxelEdit>();
            undone = new List<VoxelEdit>();
        }

        public bool Undo()
        {
            firstRedo = true;

            if (edits.Count > 0)
            {
                if (undone.Count == 0)
                {
                    //We need a snapshot of the current state so that the latest edit can be undone as well

                    var latestSnapshots = new List<VoxelEdit>();

                    var edit = edits[edits.Count - 1];
                    edit.Restore((latest) => latestSnapshots.Add(latest));

                    undone.Add(new VoxelEdit(world, latestSnapshots));

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
            if(firstRedo && undone.Count > 0)
            {
                //No need to restore to before first undone edit
                var edit = undone[undone.Count - 1];
                undone.Remove(edit);
                edits.Add(edit);
            }

            firstRedo = false;

            if (undone.Count > 0)
            {
                var edit = undone[undone.Count - 1];
                edit.Restore(null);

                undone.Remove(edit);

                //Don't re-add snapshot of the original state
                if(undone.Count != 0)
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

        private void RemoveEdit(VoxelEdit edit)
        {
            edits.Remove(edit);
            edit.Dispose();
        }

        private void QueueEdit(VoxelEdit edit)
        {
            if (edits.Count >= queueSize)
            {
                RemoveEdit(edits[0]);
            }

            edits.Add(edit);

            //Remove all undone edits because they cannot be redone anymore
            firstRedo = true;
            foreach (VoxelEdit undoneEdit in undone)
            {
                undoneEdit.Dispose();
            }
            undone.Clear();
        }

        public VoxelWorld.VoxelEditConsumer Consumer()
        {
            return QueueEdit;
        }

        public void Dispose()
        {
            foreach (VoxelEdit edit in edits)
            {
                edit.Dispose();
            }
            edits.Clear();

            foreach (VoxelEdit edit in undone)
            {
                edit.Dispose();
            }
            undone.Clear();
        }

        void OnApplicationQuit()
        {
            Dispose();
        }
    }
}
