using Newtonsoft.Json.Linq;

public class SculptureToJsonConverter
{
    public static string Convert(DefaultVoxelWorldContainer sculpture)
    {
        var jsonVertices = new JArray();
        var json = new JObject(new JProperty("vertices", jsonVertices));

        foreach (var chunkPos in sculpture.Instance.Chunks)
        {
            var chunk = sculpture.Instance.GetChunk(chunkPos);
            var mesh = chunk.mesh;
            if (mesh != null)
            {
                var triangles = mesh.triangles;
                var vertices = mesh.vertices;

                for (int i = 0; i < triangles.Length - 3; i++)
                {
                    var index = triangles[i];
                    var pos = vertices[index];
                    jsonVertices.Add(pos.x + chunk.Pos.x * chunk.ChunkSize);
                    jsonVertices.Add(pos.y + chunk.Pos.y * chunk.ChunkSize);
                    jsonVertices.Add(pos.z + chunk.Pos.z * chunk.ChunkSize);
                }
            }
        }

        return json.ToString();
    }
}