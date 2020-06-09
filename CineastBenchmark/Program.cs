using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IO.Swagger.Model;
using ObjLoader.Loader.Loaders;

class Program
{
    static void Main(string[] args)
    {
        Task.Run(async () => await Benchmark()).GetAwaiter().GetResult();
    }

    private class Result
    {
        public string segmentId;
        public string objectId;
        public string objectName;
        public double score;
    }

    private class Handler : Complete3DSimilarityQuery.Handler
    {
        private List<Result> results;

        public Handler(List<Result> results)
        {
            this.results = results;
        }

        public MediaObjectQueryResult OnFinishObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor, MediaObjectQueryResult result)
        {
            lock (results)
            {
                results.Add(new Result()
                {
                    segmentId = entry.Key,
                    score = entry.Value.Value,
                    objectId = result.Content[0].ObjectId,
                    objectName = result.Content[0].Name
                });
            }
            return result;
        }

        public SimilarityQueryResultBatch OnFinishQuery(SimilarityQueryResultBatch result)
        {
            return result;
        }

        public MediaSegmentQueryResult OnFinishSegmentsByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentQueryResult result)
        {
            return result;
        }

        public void OnStartObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor)
        {
        }

        public SimilarityQuery OnStartQuery(SimilarityQuery query)
        {
            return query;
        }

        public IdList OnStartSegmentsByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, IdList idList)
        {
            return idList;
        }
    }

    private static async Task Benchmark()
    {
        var query = new Complete3DSimilarityQuery("http://127.0.0.1:4567/");

        var downloader = query.ObjectDownloader;
        downloader.UseCineastServer = false;
        downloader.HostBaseUrl = "http://127.0.0.1:8000/";
        downloader.HostThumbnailsPath = "thumbnails/:o/:s:x";
        downloader.HostContentPath = "data/3d/:p";
        downloader.UseDescriptorContentPath = false;
        downloader.DefaultSuffix = "jpg";

        var categories = new List<string>()
        {
            "cluster2dandcolorhistogram"
        };

        string basePath = "E:/Git Repos/Cineast/build/run/jobs/data/";

        string outPath = "E:/Git Repos/Cineast/build/run/jobs/benchmark/";

        Directory.CreateDirectory(outPath);

        var files = Directory.GetFiles(basePath, "*.obj");

        var loaderFactory = new ObjLoaderFactory();
        var materialLoader = new DummyObjMaterialLoader();

        while (true)
        {
            try
            {
                foreach (var file in files)
                {
                    Console.WriteLine("Running file: " + file);

                    var outFile = Path.Combine(outPath, Path.GetFileName(file) + ".csv");

                    if (File.Exists(outFile))
                    {
                        Console.WriteLine("File already exists, skipping...");
                        continue;
                    }

                    Console.WriteLine("Loading model");

                    string modelJson;
                    using (var fs = new FileStream(file, FileMode.Open))
                    {
                        modelJson = ObjToJsonConverter.Convert(fs);
                    }

                    Console.WriteLine("Running query");

                    var results = new List<Result>();
                    var handler = new Handler(results);

                    await query.PerformAsync(categories, modelJson, null, handler);
                    var sortedResults = results.OrderByDescending(r => r.score).ToList();

                    Console.WriteLine("Writing " + sortedResults.Count + " results");

                    var outCsv = new StringBuilder();
                    outCsv.AppendLine("SegmentId,ObjectId,ObjectName,Score");
                    foreach (var result in sortedResults)
                    {
                        outCsv.Append(result.segmentId).Append(",");
                        outCsv.Append(result.objectId).Append(",");
                        outCsv.Append(result.objectName).Append(",");
                        outCsv.AppendLine(result.score.ToString());
                    }

                    File.WriteAllText(outFile, outCsv.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error has occurred:");
                Console.WriteLine(ex.Message);

                //Error, start over after timeout.
                //Usually happens after ~50 queries due to a RestSharp problem causing sockets to not
                //be closed soon enough: https://github.com/restsharp/RestSharp/issues/1322
                await Task.Delay(2*60000);
                continue;
            }

            //No errors, finish
            break;
        }
    }
}
