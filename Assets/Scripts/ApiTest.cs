using Cineast_OpenAPI_Implementation;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class ApiTest : MonoBehaviour
{
    [SerializeField] private bool run = false;

    [SerializeField] private DefaultVoxelWorldContainer sculpture;

    void Update()
    {
        if (run)
        {
            run = false;

            var modelData = SculptureToJsonConverter.Convert(sculpture);

            Task.Run(async () => await Run(modelData)).GetAwaiter().GetResult();
        }
    }

    private static async Task Run(string modelData)
    {
        Debug.Log("Start API Test");

        var cineastApiUrl = "http://192.168.1.112:4567/";
        var cineastFileUrl = "http://192.168.1.112:8000/";

        var query = new Complete3DSimilarityQuery(cineastApiUrl);
        var downloader = query.ObjectDownloader;
        downloader.HostBaseUrl = cineastFileUrl;
        downloader.HostContentPath = "data/3d/:p";
        downloader.HostThumbnailsPath = "thumbnails/:o/:s:x";
        downloader.UseCineastServer = false; //Cineast's thumbnail resolver is currently broken

        var categories = new List<string>
            {
                "cluster2dandcolorhistogram"
            };

        var handler = new Complete3DSimilarityQuery.LoggingHandler();

        await query.PerformAsync(categories, modelData, handler, handler);
    }
}
