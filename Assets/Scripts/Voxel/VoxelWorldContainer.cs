using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Voxel
{
    [RequireComponent(typeof(Transform))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxelWorldContainer : MonoBehaviour
    {
        [SerializeField] private int chunkSize = 16;
        public int ChunkSize
        {
            get
            {
                return chunkSize;
            }
        }

        [SerializeField] private CMSProperties cmsProperties = null;
        public CMSProperties CMSProperties
        {
            get
            {
                return cmsProperties;
            }
        }

        private MeshRenderer meshRenderer;

        public VoxelWorld<MortonIndexer> Instance
        {
            get;
            private set;
        }

        void Start()
        {
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            Instance = new VoxelWorld<MortonIndexer>(ChunkSize, CMSProperties, transform, (xSize, ySize, zSize) => new MortonIndexer(xSize, ySize, zSize));
        }

        void Update()
        {
            Instance.Transform = transform;
            Instance.Update(meshRenderer);
        }

        void OnApplicationQuit()
        {
            Instance.Dispose();
        }
    }
}
