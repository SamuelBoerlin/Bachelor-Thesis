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
    public abstract class VoxelWorldContainer<TIndexer> : MonoBehaviour
        where TIndexer : struct, IIndexer
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

        [SerializeField] private GameObject chunkPrefab = null;

        private IndexerFactory<TIndexer> _indexerFactory;
        public IndexerFactory<TIndexer> IndexerFactory
        {
            get
            {
                if (_indexerFactory == null)
                {
                    _indexerFactory = CreateIndexerFactory();
                }
                return _indexerFactory;
            }
        }

        private MeshRenderer meshRenderer;

        private VoxelWorld<TIndexer> _instance;
        public VoxelWorld<TIndexer> Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VoxelWorld<TIndexer>(gameObject, chunkPrefab, transform, ChunkSize, CMSProperties, IndexerFactory);
                }
                return _instance;
            }
        }

        public Type WorldType
        {
            get
            {
                return typeof(VoxelWorld<TIndexer>);
            }
        }

        public Type IndexerType
        {
            get
            {
                return typeof(TIndexer);
            }
        }

        protected abstract IndexerFactory<TIndexer> CreateIndexerFactory();

        protected virtual void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        protected virtual void LateUpdate()
        {
            Instance.Transform = transform;
            Instance.Update();
            Instance.Render(Matrix4x4.identity, meshRenderer.material);
        }

        protected virtual void OnApplicationQuit()
        {
            Instance.Dispose();
        }
    }
}
