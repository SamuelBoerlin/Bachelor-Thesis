using ObjLoader.Loader.Loaders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

public class QueryResultSpawner : MonoBehaviour
{
    [SerializeField] private UnityCineastApi api;

    [System.Serializable]
    public struct SpawnSpot
    {
        public Transform transform;
        public Vector3 offset;
    }

    [SerializeField] private GameObject prefab;
    [SerializeField] private float meshSize = 0.5f;
    [SerializeField] private Collider deletionArea;
    [SerializeField] private float deletionAreaRange = 0.1f;

    [SerializeField] private int maxSpawns = 30;
    [SerializeField] private SpawnSpot[] spawnSpots = new SpawnSpot[0];

    [SerializeField] private bool deactivateOnSpawn = true;

    [SerializeField] QueryResultDisplay resultDisplay;

    private Queue<LoadedQueryResult> loadedResults = new Queue<LoadedQueryResult>();

    private List<(JobHandle, GCHandle)> runningJobs = new List<(JobHandle, GCHandle)>();

    private void Start()
    {
        api.onQueryOutput.AddListener(OnCineastQueryOutput);
        api.onQueryComplete.AddListener(OnCineastQueryComplete);
    }

    private void OnCineastQueryComplete(int queryId, Exception ex)
    {
        resultDisplay.FinishQuery(queryId, ex);
    }

    private void OnCineastQueryOutput(int queryId, List<UnityCineastApi.QueryResult> results)
    {
        if (deletionArea != null)
        {
            //Delete already existing query result objects
            var existing = FindObjectsOfType<QueryResultObject>();
            foreach (QueryResultObject queryResultObject in existing)
            {
                var pos = queryResultObject.gameObject.transform.position;
                if ((deletionArea.ClosestPoint(pos) - pos).magnitude < deletionAreaRange)
                {
                    Destroy(queryResultObject.gameObject);
                }
            }
        }

        resultDisplay.PrepareNewQuery(queryId);

        var factory = new ObjLoaderFactory();

        //Sort by decreasing score
        results.Sort((x, y) => -x.score.CompareTo(y.score));

        for (int j = 0; j < Mathf.Min(results.Count, maxSpawns); j++)
        {
            var result = results[j];

            StartMeshConverterThread(queryId, j, result);
        }
    }

    private void Update()
    {
        List<(JobHandle, GCHandle)> completedJobs = null;
        foreach (var entry in runningJobs)
        {
            var handle = entry.Item1;
            if (handle.IsCompleted)
            {
                handle.Complete();
                entry.Item2.Free();
                if (completedJobs == null)
                {
                    completedJobs = new List<(JobHandle, GCHandle)>();
                }
                completedJobs.Add(entry);
            }
        }
        if (completedJobs != null)
        {
            foreach (var entry in completedJobs)
            {
                runningJobs.Remove(entry);
            }
        }

        if (loadedResults.Count > 0)
        {
            lock (loadedResults)
            {
                if (resultDisplay)
                {
                    var loadedResult = loadedResults.Dequeue();

                    var queryResultObject = Instantiate(prefab);
                    queryResultObject.name = prefab.name + " (" + loadedResult.result.objectDescriptor.ObjectId + ")";

                    if (deactivateOnSpawn)
                    {
                        queryResultObject.SetActive(false);
                    }

                    var queryResultObjectScript = queryResultObject.GetComponent<QueryResultObject>();
                    if (queryResultObjectScript == null)
                    {
                        Debug.LogError("Query result spawner prefab does not have a QueryResultObject component!");
                        Destroy(queryResultObject);
                        return;
                    }

                    var meshFilter = queryResultObject.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                    {
                        Debug.LogError("Query result spawner prefab does not have a MeshFilter component!");
                        Destroy(queryResultObject);
                        return;
                    }

                    //Set the query data on the object, e.g. to display the name and score on a canvas
                    queryResultObjectScript.SetQueryData(loadedResult.result);

                    var spot = spawnSpots[loadedResult.scoreIndex % spawnSpots.Length];
                    queryResultObject.transform.position = spot.transform.position + spot.transform.rotation * spot.offset;
                    queryResultObject.transform.rotation = spot.transform.rotation;

                    Mesh mesh = new Mesh();
                    meshFilter.mesh = mesh;

                    mesh.vertices = loadedResult.meshVertices;
                    mesh.triangles = loadedResult.meshIndices;

                    mesh.RecalculateNormals();

                    resultDisplay.SetQueryResult(loadedResult.queryId, loadedResult.scoreIndex, loadedResult.result, queryResultObject);
                }
                else
                {
                    Debug.Log("Result display is not set, discarding query results");
                    loadedResults.Clear();
                }
            }
        }
    }

    private void StartMeshConverterThread(int queryId, int scoreIndex, UnityCineastApi.QueryResult result)
    {
        var converter = new Converter()
        {
            loadedResults = loadedResults,
            queryId = queryId,
            scoreIndex = scoreIndex,
            result = result,
            meshSize = meshSize
        };

        var gcHandle = GCHandle.Alloc(converter);

        runningJobs.Add((new ConverterJob
        {
            handle = gcHandle
        }.Schedule(), gcHandle));
    }

    private class LoadedQueryResult
    {
        public int queryId;
        public int scoreIndex;
        public UnityCineastApi.QueryResult result;
        public Vector3[] meshVertices;
        public int[] meshIndices;
    }

    private class Converter
    {
        public Queue<LoadedQueryResult> loadedResults;
        public int queryId;
        public int scoreIndex;
        public UnityCineastApi.QueryResult result;
        public float meshSize;

        public void Execute()
        {
            var loadedResult = ConvertResult(queryId, scoreIndex, result);
            lock (loadedResults)
            {
                loadedResults.Enqueue(loadedResult);
            }
        }

        private LoadedQueryResult ConvertResult(int queryId, int scoreIndex, UnityCineastApi.QueryResult result)
        {
            var loader = new ObjLoaderFactory().Create(new DummyObjMaterialLoader());

            LoadResult objLoadResult;
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(result.objModel)))
            {
                objLoadResult = loader.Load(stream);
            }

            Vector3 center = Vector3.zero;

            Vector3[] meshVertices = new Vector3[objLoadResult.Vertices.Count];
            for (int i = 0; i < meshVertices.Length; i++)
            {
                var vertex = objLoadResult.Vertices[i];
                center += meshVertices[i] = new Vector3(vertex.X, vertex.Y, vertex.Z);
            }

            center /= meshVertices.Length;

            float maxDistance = 0.0f;

            //Offset so that center is at origin
            for (int i = 0; i < meshVertices.Length; i++)
            {
                var offsetVertex = meshVertices[i] - center;
                maxDistance = Mathf.Max(offsetVertex.magnitude, maxDistance);
                meshVertices[i] = offsetVertex;
            }

            //Scale so that maximum distance from origin == meshSize
            for (int i = 0; i < meshVertices.Length; i++)
            {
                meshVertices[i] = meshVertices[i] / maxDistance * meshSize;
            }

            var meshIndices = new List<int>();
            foreach (var group in objLoadResult.Groups)
            {
                foreach (var face in group.Faces)
                {
                    if (face.Count == 3)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            meshIndices.Add(face[i].VertexIndex - 1);
                        }
                    }
                }
            }

            return new LoadedQueryResult
            {
                queryId = queryId,
                scoreIndex = scoreIndex,
                result = result,
                meshVertices = meshVertices,
                meshIndices = meshIndices.ToArray()
            };
        }
    }

    private struct ConverterJob : IJob
    {
        public GCHandle handle;

        public void Execute()
        {
            var converter = (Converter)handle.Target;
            converter.Execute();
        }
    }
}
