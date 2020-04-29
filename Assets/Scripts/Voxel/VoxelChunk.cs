﻿using System;
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
    public class VoxelChunk : IDisposable
    {
        private readonly int chunkSize;
        private readonly int chunkSizeSq;
        public int ChunkSize
        {
            get
            {
                return chunkSize;
            }
        }

        private NativeArray3D<Voxel> voxels;
        public NativeArray3D<Voxel> Voxels
        {
            get
            {
                return voxels;
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

        private readonly VoxelWorld world;

        public VoxelChunk(VoxelWorld world, ChunkPos pos, int chunkSize)
        {
            this.world = world;
            this.chunkSize = chunkSize;
            this.chunkSizeSq = chunkSize * chunkSize;
            this.Pos = pos;

            voxels = new NativeArray3D<Voxel>(chunkSize + 1, chunkSize + 1, chunkSize + 1, Allocator.Persistent);
            //voxels = new Voxel[chunkSize * chunkSize * chunkSize];
            //voxels = new NativeArray<Voxel>(chunkSize * chunkSize * chunkSize, Allocator.Persistent); //TODO Dispose
        }

        public int GetMaterial(int x, int y, int z)
        {
            return voxels[x, y, z].Material;
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

        public Change ScheduleGrid(int sx, int sy, int sz, int gx, int gy, int gz, NativeArray3D<Voxel> grid, bool propagatePadding, bool includePadding)
        {
            var gridJob = new ChunkGridJob
            {
                source = grid,
                chunkSize = chunkSize + (includePadding ? 1 : 0),
                sx = sx,
                sy = sy,
                sz = sz,
                gx = gx,
                gy = gy,
                gz = gz,
                target = voxels
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
            var outVoxels = new NativeArray3D<Voxel>(voxels.Length(0), voxels.Length(1), voxels.Length(2), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var sdfJob = new ChunkSdfJob<TSdf>
            {
                origin = new float3(ox, oy, oz),
                sdf = sdf,
                material = material,
                replace = replace,
                snapshot = voxels,
                changed = changed,
                outVoxels = outVoxels
            };

            return new Change(sdfJob.Schedule(), () =>
                {
                    voxels.Dispose();
                    voxels = outVoxels;

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

            world.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y, Pos.z))?.ScheduleUpdatePadding(this, jobs);
            world.GetChunk(ChunkPos.FromChunk(Pos.x, Pos.y - 1, Pos.z))?.ScheduleUpdatePadding(this, jobs);
            world.GetChunk(ChunkPos.FromChunk(Pos.x, Pos.y, Pos.z - 1))?.ScheduleUpdatePadding(this, jobs);

            world.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y - 1, Pos.z))?.ScheduleUpdatePadding(this, jobs);
            world.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y, Pos.z - 1))?.ScheduleUpdatePadding(this, jobs);
            world.GetChunk(ChunkPos.FromChunk(Pos.x, Pos.y - 1, Pos.z - 1))?.ScheduleUpdatePadding(this, jobs);

            world.GetChunk(ChunkPos.FromChunk(Pos.x - 1, Pos.y - 1, Pos.z - 1))?.ScheduleUpdatePadding(this, jobs);

            foreach (var handle in jobs)
            {
                handle.Complete();
            }
        }

        /// <summary>
        /// Updates the padding of this chunk to the -X/-Y/-Z border voxels of the specified neighbor chunk
        /// </summary>
        /// <param name="neighbor"></param>    
        private void ScheduleUpdatePadding(VoxelChunk neighbor, List<JobHandle> jobs)
        {
            var xOff = neighbor.Pos.x - Pos.x;
            var yOff = neighbor.Pos.y - Pos.y;
            var zOff = neighbor.Pos.z - Pos.z;

            if (xOff < 0 || xOff > 1 || yOff < 0 || yOff > 1 || zOff < 0 || zOff > 1 || xOff + yOff + zOff == 0)
            {
                throw new ArgumentException("Chunk is not a -X/-Y/-Z neighbor!");
            }

            jobs.Add(new ChunkPaddingJob
            {
                source = neighbor.voxels,
                chunkSize = chunkSize,
                xOff = xOff,
                yOff = yOff,
                zOff = zOff,
                target = voxels
            }.Schedule());
        }

        public void FillCell(int x, int y, int z, int cellIndex, NativeArray<int> materials, NativeArray<float> intersections, NativeArray<float3> normals)
        {
            ChunkBuildJob.FillCell(voxels, x, y, z, cellIndex, materials, intersections, normals);
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

            ChunkBuildJob polygonizerJob = new ChunkBuildJob
            {
                Voxels = voxels,
                PolygonizationProperties = world.CMSProperties.Data,
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

                var vertices = new List<Vector3>(meshVertices.Length);
                var indices = new List<int>(meshTriangles.Length);
                var materials = new List<int>(meshMaterials.Length);
                var colors = new List<Color32>(meshColors.Length);
                var normals = new List<Vector3>(meshNormals.Length);

                for (int i = 0; i < meshVertices.Length; i++)
                {
                    vertices.Add(meshVertices[i]);
                }
                for (int i = 0; i < meshTriangles.Length; i++)
                {
                    indices.Add(meshTriangles[i]);
                }
                for (int i = 0; i < meshMaterials.Length; i++)
                {
                    materials.Add(meshMaterials[i]);
                }
                for (int i = 0; i < meshColors.Length; i++)
                {
                    colors.Add(meshColors[i]);
                }
                for (int i = 0; i < meshNormals.Length; i++)
                {
                    normals.Add(meshNormals[i]);
                }

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
                if (colors.Count > 0)
                {
                    mesh.SetColors(colors);
                }
            };
        }

        public void Dispose()
        {
            voxels.Dispose();
        }

        public readonly struct Snapshot
        {
            public readonly JobHandle handle;
            public readonly VoxelChunk chunk;

            public Snapshot(JobHandle handle, VoxelChunk chunk)
            {
                this.handle = handle;
                this.chunk = chunk;
            }
        }

        public Snapshot ScheduleSnapshot()
        {
            var snapshotChunk = new VoxelChunk(world, Pos, ChunkSize);

            var cloneJob = new ChunkCloneJob
            {
                source = voxels,
                chunkSize = chunkSize + 1, //Include padding when cloning
                target = snapshotChunk.voxels
            };

            return new Snapshot(cloneJob.Schedule(), snapshotChunk);
        }
    }
}