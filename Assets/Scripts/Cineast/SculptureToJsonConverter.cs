using System.Text;
using UnityEngine;

public class SculptureToJsonConverter
{
    public static string Convert(DefaultVoxelWorldContainer sculpture)
    {
        var chunkPositions = sculpture.Instance.Chunks;

        if (chunkPositions.Count > 0)
        {
            var sb = new StringBuilder();

            sb.Append("{ \"vertices\": [");

            foreach (var chunkPos in chunkPositions)
            {
                var chunk = sculpture.Instance.GetChunk(chunkPos);
                var mesh = chunk.mesh;

                if (mesh != null)
                {
                    var chunkSize = chunk.ChunkSize;

                    var triangles = mesh.triangles;
                    var vertices = mesh.vertices;
                    var colors = mesh.colors;

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
                        sb.Append(pos.x + chunkPos.x * chunkSize);
                        sb.Append(",");
                        sb.Append(pos.y + chunkPos.y * chunkSize);
                        sb.Append(",");
                        sb.Append(pos.z + chunkPos.z * chunkSize);
                        sb.Append(",");
                    }
                }
            }

            //Remove trailing comma
            sb.Remove(sb.Length - 1, 1);

            sb.Append("] }");

            return sb.ToString();
        }

        return "";
    }
}