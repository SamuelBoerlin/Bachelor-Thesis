using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Voxel
{
    public abstract class VoxelEditManagerContainer<TIndexer> : MonoBehaviour
        where TIndexer : struct, IIndexer
    {
        [SerializeField] private int queueSize = 5;

        private VoxelEditManager<TIndexer> _instance;
        public VoxelEditManager<TIndexer> Instance
        {
            get
            {
                if(_instance == null)
                {
                    var container = GetComponent<VoxelWorldContainer<TIndexer>>();
                    if (container == null)
                    {
                        Debug.LogError(string.Format("Cannot create {0} because {1} component does not exist on this game object", typeof(VoxelEditManager<TIndexer>).Name, typeof(VoxelWorldContainer<TIndexer>).Name));
                    }
                    else
                    {
                        _instance = new VoxelEditManager<TIndexer>(container.Instance, queueSize);
                    }
                }
                return _instance;
            }
        }

        public Type ManagerType
        {
            get
            {
                return typeof(VoxelEditManager<TIndexer>);
            }
        }

        public Type IndexerType
        {
            get
            {
                return typeof(TIndexer);
            }
        }

        public void OnApplicationQuit()
        {
            Instance.Dispose();
        }
    }
}
