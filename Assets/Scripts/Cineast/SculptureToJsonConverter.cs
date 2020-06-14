using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SculptureToJsonConverter
{
    public static string Convert(DefaultVoxelWorldContainer sculpture, MeshFilter filter = null)
    {
        var chunkPositions = sculpture.Instance.Chunks;

        if (chunkPositions.Count > 0)
        {
            List<CombineInstance> meshes = new List<CombineInstance>();

            foreach (var chunkPos in chunkPositions)
            {
                var chunk = sculpture.Instance.GetChunk(chunkPos);
                var mesh = chunk.mesh;

                if (mesh != null)
                {
                    var chunkSize = chunk.ChunkSize;
                    meshes.Add(
                        new CombineInstance()
                        {
                            mesh = mesh,
                            transform = Matrix4x4.Translate(new Vector3(chunkPos.x * chunkSize, chunkPos.y * chunkSize, chunkPos.z * chunkSize))
                        }
                        );
                }
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(meshes.ToArray(), true, true, false);
            /*var combinedColors = combinedMesh.colors;
            var combinedUVs = new Vector2[combinedColors.Length];
            var colorBytes = new byte[4];
            for(int l = combinedColors.Length, i = 0; i < l; i++)
            {
                var color = combinedColors[i];
                colorBytes[0] = (byte)(color.r * 255);
                colorBytes[1] = (byte)(color.g * 255);
                colorBytes[2] = (byte)(color.b * 255);
                colorBytes[3] = (byte)(color.a * 255);
                var u = BitConverter.ToSingle(colorBytes, 0);
                combinedUVs[i] = new Vector2(u, 1 - u);
            }
            combinedMesh.uv = combinedUVs;
            //Mesh combinedMesh = Combine(meshes.ToArray());

            var simplifier = new UnityMeshSimplifier.MeshSimplifier();

            simplifier.PreserveBorderEdges = true;
            //implifier.PreserveUVFoldoverEdges = true;
            simplifier.PreserveUVSeamEdges = true;

            simplifier.VertexLinkDistanceSqr = 0.0000001f;
            simplifier.EnableSmartLink = true;

            simplifier.Initialize(combinedMesh);
            //simplifier.SimplifyMeshLossless();
            simplifier.SimplifyMesh(0.1f);

            var decimatedMesh = simplifier.ToMesh();

            GameObject.Destroy(combinedMesh);*/

            var decimatedMesh = combinedMesh;

            var sb = new StringBuilder();

            sb.Append("{ \"vertices\": [");

            var triangles = decimatedMesh.triangles;
            var vertices = decimatedMesh.vertices;
            var colors = decimatedMesh.colors;

            for (int i = 0; i < triangles.Length - 3; i++)
            {
                var index = triangles[i];
                var pos = vertices[index];
                var color = colors[index];

                //Append color components
                sb.Append(color.r);
                sb.Append(",");
                sb.Append(color.g);
                sb.Append(",");
                sb.Append(color.b);
                sb.Append(",");

                //Append position components
                sb.Append(pos.x);
                sb.Append(",");
                sb.Append(pos.y);
                sb.Append(",");
                sb.Append(pos.z);
                sb.Append(",");
            }

            //Remove trailing comma
            sb.Remove(sb.Length - 1, 1);

            sb.Append("] }");

            if (filter != null)
            {
                filter.sharedMesh = decimatedMesh;
            }
            else
            {
                GameObject.Destroy(decimatedMesh);
            }

            return sb.ToString();
        }

        return "";
    }
}