using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Voxel
{
    //TODO Give one voxel padding in +X/+Y/+Z that mirrors the voxels of the adjacent chunks.
    //This will be necessary to make chunk mesh building independent and to jobify SDF modifications because applying an intersection change requires
    //both materials of each edge.
    public class VoxelChunk<TIndexer> : IDisposable
        where TIndexer : struct, IIndexer
    {
        private readonly int _chunkSize;
        private readonly int chunkSizeSq;
        public int ChunkSize
        {
            get
            {
                return _chunkSize;
            }
        }

        private NativeArray3D<Voxel, TIndexer> _voxels;
        public NativeArray3D<Voxel, TIndexer> Voxels
        {
            get
            {
                return _voxels;
            }
        }

        //TODO Cleanup, separate mesh from chunk
        public Mesh mesh = null;
        public bool NeedsRebuild
        {
            get;
            private set;
        }

        public ChunkPos Pos
        {
            get;
            private set;
        }

        public VoxelWorld<TIndexer> World
        {
            get;
            private set;
        }

        private readonly IndexerFactory<TIndexer> indexerFactory;

        public GameObject ChunkObject
        {
            get;
            private set;
        }

        private readonly NativeArray<int> _voxelCount;
        public int VoxelCount
        {
            get
            {
                return _voxelCount.IsCreated ? _voxelCount[0] : 0;
            }
        }

        public VoxelChunk(VoxelWorld<TIndexer> world, ChunkPos pos, int chunkSize, IndexerFactory<TIndexer> indexerFactory)
        {
            this.World = world;
            this._chunkSize = chunkSize;
            this.chunkSizeSq = chunkSize * chunkSize;
            this.Pos = pos;
            this.indexerFactory = indexerFactory;

            _voxels = new NativeArray3D<Voxel, TIndexer>(indexerFactory(chunkSize + 1, chunkSize + 1, chunkSize + 1), chunkSize + 1, chunkSize + 1, chunkSize + 1, Allocator.Persistent);
            _voxelCount = new NativeArray<int>(1, Allocator.Persistent);
        }

        internal void OnAddedToWorld()
        {
            if (World.ChunkPrefab != null)
            {
                Vector3 prefabWorldPos = World.VoxelWorldObject.transform.TransformPoint(new Vector3(Pos.x * ChunkSize, Pos.y * ChunkSize, Pos.z * ChunkSize));
                ChunkObject = UnityEngine.Object.Instantiate(World.ChunkPrefab, prefabWorldPos, World.VoxelWorldObject.transform.rotation, World.VoxelWorldObject.transform);
                ChunkObject.name = string.Format("{0} ({1}, {2}, {3})", World.ChunkPrefab.name, Pos.x, Pos.y, Pos.z);

                var chunkContainer = ChunkObject.GetComponent<VoxelChunkContainer<TIndexer>>();
                if (chunkContainer != null && chunkContainer.ChunkType == GetType())
                {
                    dynamic d = this;
                    chunkContainer.Chunk = d;
                }
            }
        }

        public int GetMaterial(int x, int y, int z)
        {
            return _voxels[x, y, z].Material;
        }

        public delegate void FinalizeChange();

        public readonly struct Change
        {
            public readonly JobHandle handle;
            public readonly FinalizeChange finalize;

            public Change(JobHandle handle, FinalizeChange finalize)
            {
                this.handle = handle;
                this.finalize = finalize;
            }
        }

        public Change ScheduleGrid<TGridIndexer>(int tx, int ty, int tz, int gx, int gy, int gz, NativeArray3D<Voxel, TGridIndexer> grid, bool propagatePadding, bool includePadding, bool writeUnsetVoxels)
            where TGridIndexer : struct, IIndexer
        {
            var gridJob = new ChunkGridJob<TGridIndexer, TIndexer>
            {
                source = grid,
                chunkSize = _chunkSize,
                includePadding = includePadding,
                writeUnsetVoxels = writeUnsetVoxels,
                tx = tx,
                ty = ty,
                tz = tz,
                gx = gx,
                gy = gy,
                gz = gz,
                target = _voxels,
                voxelCount = _voxelCount
            };

            return new Change(gridJob.Schedule(), () =>
                {
                    NeedsRebuild = true;

                    if (propagatePadding)
                    {
                        //Update the padding of all -X/-Y/-Z adjacent chunks
                        //TODO Only propagate those sides that have changed
                        PropagatePadding();
                    }
                }
            );
        }

        public Change ScheduleSdf<TSdf>(float ox, float oy, float oz, TSdf sdf, int material, bool replace)
            where TSdf : struct, ISdf
        {
            var changed = new NativeArray<bool>(1, Allocator.TempJob);
            var outVoxels = new NativeArray3D<Voxel, TIndexer>(indexerFactory(_voxels.Length(0), _voxels.Length(1), _voxels.Length(2)), _voxels.Length(0), _voxels.Length(1), _voxels.Length(2), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var sdfJob = new ChunkSdfJob<TSdf, TIndexer>
            {
                origin = new float3(ox, oy, oz),
                sdf = sdf,
                material = material,
                replace = replace,
                snapshot = _voxels,
                changed = changed,
                outVoxels = outVoxels,
                voxelCount = _voxelCount
            };

            return new Change(sdfJob.Schedule(), () =>
                {
                    _voxels.Dispose();
                    _voxels = outVoxels;

                    if (sdfJob.changed[0])
                    {
                        NeedsRebuild = true;
                    }
                    changed.Dispose();

                    //Update the padding of all -X/-Y/-Z adjacent chunks
                    //TODO Only propagate those sides that have changed
                    PropagatePadding();
                }
            );
        }

        /// <summary>
        /// Propagates the -X/-Y/-Z border voxels to the padding of the -X/-Y/-Z adjacent chunks
        /// </summary>
        private void PropagatePadding()
        {
            var jobs = new List<JobHandle>();

            World.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y, Pos.z))?.ScheduleUpdatePadding(this, jobs);
            World.GetChunk(ChunkPos.FromChunk(Pos.x, Pos.y - 1, Pos.z))?.ScheduleUpdatePadding(this, jobs);
            World.GetChunk(ChunkPos.FromChunk(Pos.x, Pos.y, Pos.z - 1))?.ScheduleUpdatePadding(this, jobs);

            World.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y - 1, Pos.z))?.ScheduleUpdatePadding(this, jobs);
            World.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y, Pos.z - 1))?.ScheduleUpdatePadding(this, jobs);
            World.GetChunk(ChunkPos.FromChunk(Pos.x, Pos.y - 1, Pos.z - 1))?.ScheduleUpdatePadding(this, jobs);

            World.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y - 1, Pos.z - 1))?.ScheduleUpdatePadding(this, jobs);

            foreach (var handle in jobs)
            {
                handle.Complete();
            }
        }

        /// <summary>
        /// Updates the padding of this chunk to the -X/-Y/-Z border voxels of the specified neighbor chunk
        /// </summary>
        /// <param name="neighbor"></param>    
        private void ScheduleUpdatePadding(VoxelChunk<TIndexer> neighbor, List<JobHandle> jobs)
        {
            var xOff = neighbor.Pos.x - Pos.x;
            var yOff = neighbor.Pos.y - Pos.y;
            var zOff = neighbor.Pos.z - Pos.z;

            if (xOff < 0 || xOff > 1 || yOff < 0 || yOff > 1 || zOff < 0 || zOff > 1 || xOff + yOff + zOff == 0)
            {
                throw new ArgumentException("Chunk is not a -X/-Y/-Z neighbor!");
            }

            jobs.Add(new ChunkPaddingJob<TIndexer, TIndexer>
            {
                source = neighbor._voxels,
                chunkSize = _chunkSize,
                xOff = xOff,
                yOff = yOff,
                zOff = zOff,
                target = _voxels
            }.Schedule());
        }

        public void FillCell(int x, int y, int z, int cellIndex, NativeArray<int> materials, NativeArray<float> intersections, NativeArray<float3> normals)
        {
            ChunkBuildJob<TIndexer>.FillCell(_voxels, x, y, z, cellIndex, materials, intersections, normals);
        }

        public delegate void FinalizeBuild();
        public FinalizeBuild ScheduleBuild()
        {
            NeedsRebuild = false;

            var meshVertices = new NativeList<float3>(Allocator.TempJob);
            var meshNormals = new NativeList<float3>(Allocator.TempJob);
            var meshTriangles = new NativeList<int>(Allocator.TempJob);
            var meshColors = new NativeList<Color32>(Allocator.TempJob);
            var meshMaterials = new NativeList<int>(Allocator.TempJob);

            ChunkBuildJob<TIndexer> polygonizerJob = new ChunkBuildJob<TIndexer>
            {
                Voxels = _voxels,
                PolygonizationProperties = World.CMSProperties.Data,
                MeshVertices = meshVertices,
                MeshNormals = meshNormals,
                MeshTriangles = meshTriangles,
                MeshColors = meshColors,
                MeshMaterials = meshMaterials,
            };

            var handle = polygonizerJob.Schedule();

            return () =>
            {
                handle.Complete();

                var vertices = new Vector3[meshVertices.Length];
                var indices = new int[meshTriangles.Length];
                var materials = new int[meshMaterials.Length];
                var colors = new Color32[meshColors.Length];
                var normals = new Vector3[meshNormals.Length];

                meshVertices.CopyToFast(vertices);
                meshTriangles.CopyToFast(indices);
                meshMaterials.CopyToFast(materials);
                meshColors.CopyToFast(colors);
                meshNormals.CopyToFast(normals);

                meshVertices.Dispose();
                meshNormals.Dispose();
                meshTriangles.Dispose();
                meshColors.Dispose();
                meshMaterials.Dispose();

                if (mesh == null)
                {
                    mesh = new Mesh();
                }

                mesh.Clear(false);
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTriangles(indices, 0);
                if (colors.Length > 0)
                {
                    mesh.SetColors(colors);
                }

                if (ChunkObject != null)
                {
                    ChunkObject.GetComponent<VoxelChunkContainer<TIndexer>>()?.OnChunkRebuilt();
                }
            };
        }

        public void Dispose()
        {
            _voxels.Dispose();
            _voxelCount.Dispose();

            if (ChunkObject != null)
            {
                UnityEngine.Object.Destroy(ChunkObject);
            }
        }

        public readonly struct Snapshot
        {
            public readonly JobHandle handle;
            public readonly VoxelChunk<TIndexer> chunk;

            public Snapshot(JobHandle handle, VoxelChunk<TIndexer> chunk)
            {
                this.handle = handle;
                this.chunk = chunk;
            }
        }

        public Snapshot ScheduleSnapshot()
        {
            var snapshotChunk = new VoxelChunk<TIndexer>(World, Pos, ChunkSize, indexerFactory);

            var cloneJob = new ChunkCloneJob<TIndexer, TIndexer>
            {
                source = _voxels,
                sourceVoxelCount = _voxelCount,
                chunkSize = _chunkSize + 1, //Include padding when cloning
                target = snapshotChunk._voxels,
                targetVoxelCount = snapshotChunk._voxelCount
            };

            return new Snapshot(cloneJob.Schedule(), snapshotChunk);
        }
    }
}