using Cineast_OpenAPI_Implementation;
using IO.Swagger.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class UnityCineastApi : MonoBehaviour
{
    [System.Serializable]
    private struct TestSettings
    {
        public bool runTest;
        public DefaultVoxelWorldContainer voxels;
        public bool debugLog;
    }
    [SerializeField] private TestSettings testSettings = new TestSettings();

    [SerializeField] private string cineastApiUrl = "http://192.168.1.112:4567/";

    [SerializeField] private string[] queryCategories = { "cluster2dandcolorhistogram" };

    [System.Serializable]
    public struct ObjectDownloaderSettings
    {
        public bool useCineastServer;
        public string hostBaseUrl;
        public string hostThumbnailsPath;
        public string hostContentPath;
        public bool useDescriptorContentPath;
        public string defaultSuffix;
    }
    [SerializeField]
    private ObjectDownloaderSettings objectDownloaderSettings = new ObjectDownloaderSettings
    {
        useCineastServer = false, //Cineast's thumbnail resolver is currently broken
        hostBaseUrl = "http://192.168.1.112:8000/",
        hostThumbnailsPath = "thumbnails/:o/:s:x",
        hostContentPath = "data/3d/:p",
        useDescriptorContentPath = false,
        defaultSuffix = "jpg"
    };

    private int queryIdCounter = 0;

    [Serializable]
    public class QueryCompletedEvent : UnityEvent<int, List<QueryResult>> { }
    public QueryCompletedEvent onQueryCompleted;

    public struct QueryResult
    {
        public double score;
        public MediaSegmentDescriptor segmentDescriptor;
        public MediaObjectDescriptor objectDescriptor;
        public string objModel;
    }

    private delegate void IntermediateQueryResultCallbackDelegate(QueryResult result);
    private class CollectingQueryResultCallback : Complete3DSimilarityQuery.Callback
    {
        private readonly List<QueryResult> results;

        private readonly IntermediateQueryResultCallbackDelegate callback;

        public CollectingQueryResultCallback(List<QueryResult> results, IntermediateQueryResultCallbackDelegate callback)
        {
            this.results = results;
        }

        public void OnFullQueryResult(StringDoublePair entry, MediaSegmentDescriptor segmentDescriptor, MediaObjectDescriptor objectDescriptor, string objModel)
        {
            var result = new QueryResult
            {
                score = entry.Value.Value,
                segmentDescriptor = segmentDescriptor,
                objectDescriptor = objectDescriptor,
                objModel = objModel
            };
            if (callback != null)
            {
                callback(result);
            }
            else
            {
                lock (results)
                {
                    results.Add(result);
                }
            }
        }
    }

    private class LoggingWrapper : Complete3DSimilarityQuery.Handler, Complete3DSimilarityQuery.Callback
    {
        private readonly Complete3DSimilarityQuery.Handler handler;
        private readonly Complete3DSimilarityQuery.Callback callback;

        public LoggingWrapper(Complete3DSimilarityQuery.Handler handler, Complete3DSimilarityQuery.Callback callback)
        {
            this.handler = handler;
            this.callback = callback;
        }

        public SimilarityQuery OnStartQuery(SimilarityQuery query)
        {
            Debug.Log("Start 3D Similarity Query Request");
            return handler != null ? handler.OnStartQuery(query) : query;
        }

        public SimilarityQueryResultBatch OnFinishQuery(SimilarityQueryResultBatch result)
        {
            Debug.Log("Finished 3D Similarity Query Request");

            Debug.Log("Results:");
            Debug.Log("");
            return handler != null ? handler.OnFinishQuery(result) : result;
        }

        public IdList OnStartSegmentsByIdQuery(SimilarityQueryResult similarityResult, StringDoublePair entry, IdList idList)
        {
            Debug.Log("---------------------------");
            Debug.Log("Segment ID: " + entry.Key + ", Similarity Score: " + entry.Value);
            return handler != null ? handler.OnStartSegmentsByIdQuery(similarityResult, entry, idList) : idList;
        }

        public MediaSegmentQueryResult OnFinishSegmentsByIdQuery(SimilarityQueryResult similarityResult, StringDoublePair entry, MediaSegmentQueryResult result)
        {
            return handler != null ? handler.OnFinishSegmentsByIdQuery(similarityResult, entry, result) : result;
        }

        public void OnStartObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor)
        {
            Debug.Log("Object ID: " + descriptor.ObjectId);
            if (handler != null)
            {
                handler.OnStartObjectByIdQuery(queryResult, entry, descriptor);
            }
        }

        public MediaObjectQueryResult OnFinishObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor, MediaObjectQueryResult result)
        {
            return handler != null ? handler.OnFinishObjectByIdQuery(queryResult, entry, descriptor, result) : result;
        }

        public void OnFullQueryResult(StringDoublePair entry, MediaSegmentDescriptor segmentDescriptor, MediaObjectDescriptor objectDescriptor, string objModel)
        {
            Debug.Log("Downloaded Object: ");

            var lines = objModel.Split('\n');
            int maxLines = Mathf.Min(lines.Length, 8);
            for (int i = 0; i < maxLines; i++)
            {
                Debug.Log(lines[i]);
            }
            Debug.Log("...");
            Debug.Log("---------------------------");
            Debug.Log("");

            if (callback != null)
            {
                callback.OnFullQueryResult(entry, segmentDescriptor, objectDescriptor, objModel);
            }
        }
    }

    void Update()
    {
        if (testSettings.runTest)
        {
            testSettings.runTest = false;

            var modelData = SculptureToJsonConverter.Convert(testSettings.voxels);
            StartQuery(modelData, false);
        }
    }

    public int StartQuery(string modelJson, bool continuousOutputs)
    {
        var queryId = queryIdCounter++;

        if (testSettings.debugLog)
        {
            Debug.Log("Start Similarity Query");
        }

        var query = new Complete3DSimilarityQuery(cineastApiUrl);
        var downloader = query.ObjectDownloader;

        downloader.UseCineastServer = objectDownloaderSettings.useCineastServer;
        downloader.HostBaseUrl = objectDownloaderSettings.hostBaseUrl;
        downloader.HostThumbnailsPath = objectDownloaderSettings.hostThumbnailsPath;
        downloader.HostContentPath = objectDownloaderSettings.hostContentPath;
        downloader.UseDescriptorContentPath = objectDownloaderSettings.useDescriptorContentPath;
        downloader.DefaultSuffix = objectDownloaderSettings.defaultSuffix;

        var categories = new List<string>(queryCategories);

        StartCoroutine(CreateQueryCoroutine(query, categories, modelJson, results => onQueryCompleted?.Invoke(queryId, results), continuousOutputs, testSettings.debugLog));

        return queryId;
    }

    public delegate void QueryResultCallbackDelegate(List<QueryResult> results);
    public static IEnumerator CreateQueryCoroutine(Complete3DSimilarityQuery query, List<string> categories, string modelJson, QueryResultCallbackDelegate callback, bool continuous = false, bool log = false)
    {
        var results = new List<QueryResult>();

        IntermediateQueryResultCallbackDelegate immediateCallback = null;
        if (continuous)
        {
            immediateCallback = (result) =>
            {
                lock (results)
                {
                    results.Add(result);
                }
            };
        }

        //This callback just puts all results in the results list
        Complete3DSimilarityQuery.Callback queryCallback = new CollectingQueryResultCallback(results, immediateCallback);
        if (log)
        {
            queryCallback = new LoggingWrapper(null, queryCallback);
        }

        //Create query task
        Task queryTask = null;

        //Run query task in another thread
        var task = Task.Run(async () =>
            {
                try
                {
                    queryTask = query.PerformAsync(categories, modelJson, queryCallback, queryCallback is Complete3DSimilarityQuery.Handler ? (Complete3DSimilarityQuery.Handler)queryCallback : null);
                    await queryTask;
                }
                catch(Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
        );

        //Check if task is completed, otherwise make the coroutine wait
        while (queryTask == null || !queryTask.IsCompleted)
        {
            if (continuous)
            {
                //Call callback with intermediate query results
                lock (results)
                {
                    callback(results);
                    results.Clear();
                }
            }

            yield return null;
        }

        if (!continuous)
        {
            //Call callback with query results
            lock (results)
            {
                callback(results);
            }
        }
    }
}
