using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Voxel
{
    [RequireComponent(typeof(VoxelWorldContainer))]
    public class VoxelEditManagerContainer : MonoBehaviour
    {
        [SerializeField] private int queueSize = 5;

        public VoxelEditManager<MortonIndexer> Instance
        {
            get;
            private set;
        }

        public void Start()
        {
            Instance = new VoxelEditManager<MortonIndexer>(GetComponent<VoxelWorldContainer>().Instance, queueSize);
        }

        public void OnApplicationQuit()
        {
            Instance.Dispose();
        }
    }
}
