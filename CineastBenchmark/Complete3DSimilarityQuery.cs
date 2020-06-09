using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class Complete3DSimilarityQuery : CompleteCineastQuery
{
    public interface Callback
    {
        void OnFullQueryResult(StringDoublePair entry, MediaSegmentDescriptor segmentDescriptor, MediaObjectDescriptor objectDescriptor, string objModel);
    }

    public interface Handler
    {
        SimilarityQuery OnStartQuery(SimilarityQuery query);

        SimilarityQueryResultBatch OnFinishQuery(SimilarityQueryResultBatch result);

        IdList OnStartSegmentsByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, IdList idList);

        MediaSegmentQueryResult OnFinishSegmentsByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentQueryResult result);

        void OnStartObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor);

        MediaObjectQueryResult OnFinishObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor, MediaObjectQueryResult result);
    }

    public class LoggingHandler : Handler, Callback
    {
        public SimilarityQuery OnStartQuery(SimilarityQuery query)
        {
            Console.WriteLine("Start 3D Similarity Query Request");
            return query;
        }

        public SimilarityQueryResultBatch OnFinishQuery(SimilarityQueryResultBatch result)
        {
            Console.WriteLine("Finished 3D Similarity Query Request");

            Console.WriteLine("Results:");
            Console.WriteLine("");
            return result;
        }

        public IdList OnStartSegmentsByIdQuery(SimilarityQueryResult similarityResult, StringDoublePair entry, IdList idList)
        {
            Console.WriteLine("---------------------------");
            Console.WriteLine("Segment ID: " + entry.Key + ", Similarity Score: " + entry.Value);
            return idList;
        }

        public MediaSegmentQueryResult OnFinishSegmentsByIdQuery(SimilarityQueryResult similarityResult, StringDoublePair entry, MediaSegmentQueryResult result)
        {
            return result;
        }

        public void OnStartObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor)
        {
            Console.WriteLine("Object ID: " + descriptor.ObjectId);
        }

        public MediaObjectQueryResult OnFinishObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor, MediaObjectQueryResult result)
        {
            return result;
        }

        public void OnFullQueryResult(StringDoublePair entry, MediaSegmentDescriptor segmentDescriptor, MediaObjectDescriptor objectDescriptor, string objModel)
        {
            Console.WriteLine("Downloaded Object: ");

            var lines = objModel.Split('\n');
            int maxLines = Math.Min(lines.Length, 8);
            for (int i = 0; i < maxLines; i++)
            {
                Console.WriteLine(lines[i]);
            }
            Console.WriteLine("...");
            Console.WriteLine("---------------------------");
            Console.WriteLine("");
        }
    }

    public Complete3DSimilarityQuery(string cineastApiUrl) : base(cineastApiUrl)
    {
    }

    public async Task PerformAsync(List<string> categories, string modelJson, Callback callback, Handler handler)
    {
        var queryModelData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(modelJson));

        var terms = new List<QueryTerm>
        {
            new QueryTerm
            {
                Categories = categories,
                Type = QueryTerm.TypeEnum.MODEL3D,
                Data = "data:application/3d-color-json," + queryModelData
            }
        };

        var components = new List<QueryComponent>
        {
            new QueryComponent
            {
                Terms = terms
            }
        };

        var conf = new QueryConfig();

        var query = new SimilarityQuery
        {
            Containers = components,
            Components = components, //???
            Config = conf
        };
        if (handler != null)
        {
            query = handler.OnStartQuery(query);
        }

        var response = await Api.OrgVitrivrCineastApiRestHandlersActionsFindSegmentSimilarActionHandlerPOSTAsync(query);
        if (handler != null)
        {
            response = handler.OnFinishQuery(response);
        }

        var segmentQueries = new List<Task<MediaSegmentQueryResult>>();
        var segmentQueryContext = new Dictionary<Task<MediaSegmentQueryResult>, (SimilarityQueryResult, StringDoublePair)>();

        foreach (var similarityResult in response.Results)
        {
            foreach (var similarityContent in similarityResult.Content)
            {
                var segmentsIdList = new IdList(new List<string>() { similarityContent.Key });
                if (handler != null)
                {
                    segmentsIdList = handler.OnStartSegmentsByIdQuery(similarityResult, similarityContent, segmentsIdList);
                }

                var segmentQuery = Api.OrgVitrivrCineastApiRestHandlersActionsFindSegmentsByIdActionHandlerPOSTAsync(segmentsIdList);

                segmentQueries.Add(segmentQuery);
                segmentQueryContext[segmentQuery] = (similarityResult, similarityContent);
            }
        }

        var downloads = new List<Task<(Stream, HttpClient)>>();
        var downloadsContext = new Dictionary<Task<(Stream, HttpClient)>, (StringDoublePair, MediaSegmentDescriptor, MediaObjectDescriptor)>();

        while (segmentQueries.Count > 0)
        {
            var task = await Task.WhenAny(segmentQueries);
            var segmentsResult = await task;

            segmentQueries.Remove(task);

            var context = segmentQueryContext[task];
            var similarityResult = context.Item1;
            var similarityContent = context.Item2;

            if (handler != null)
            {
                segmentsResult = handler.OnFinishSegmentsByIdQuery(similarityResult, similarityContent, segmentsResult);
            }

            foreach (var segmentContent in segmentsResult.Content)
            {
                if (handler != null)
                {
                    handler.OnStartObjectByIdQuery(similarityResult, similarityContent, segmentContent);
                }
                var mediaObjectResult = await Api.OrgVitrivrCineastApiRestHandlersActionsFindObjectByActionHandlerGETAsync("id", segmentContent.ObjectId);
                if (handler != null)
                {
                    mediaObjectResult = handler.OnFinishObjectByIdQuery(similarityResult, similarityContent, segmentContent, mediaObjectResult);
                }

                if (callback != null)
                {
                    foreach (var mediaObjectContent in mediaObjectResult.Content)
                    {
                        //Start download and queue up
                        var downloadTask = ObjectDownloader.RequestContentAsync(Api, mediaObjectContent, segmentContent);
                        downloads.Add(downloadTask);
                        downloadsContext[downloadTask] = (similarityContent, segmentContent, mediaObjectContent);

                        //Just one result expected
                        break;
                    }
                }

                //Just one result expected
                break;
            }
        }

        var reads = new List<Task<string>>();
        var readsContext = new Dictionary<Task<string>, (HttpClient, StringDoublePair, MediaSegmentDescriptor, MediaObjectDescriptor, Stream, StreamReader)>();

        while (downloads.Count > 0)
        {
            var task = await Task.WhenAny(downloads);
            var taskResult = await task;
            var stream = taskResult.Item1;
            var client = taskResult.Item2;

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            downloads.Remove(task);

            var context = downloadsContext[task];
            var similarityContent = context.Item1;
            var segmentContent = context.Item2;
            var mediaObjectContent = context.Item3;

            var reader = new StreamReader(stream);

            var readTask = reader.ReadToEndAsync();
            reads.Add(readTask);
            readsContext[readTask] = (client, similarityContent, segmentContent, mediaObjectContent, stream, reader);

            sw.Stop();
        }

        while (reads.Count > 0)
        {
            var task = await Task.WhenAny(reads);
            var objModel = await task;

            reads.Remove(task);

            var context = readsContext[task];
            var client = context.Item1;
            var similarityContent = context.Item2;
            var segmentContent = context.Item3;
            var mediaObjectContent = context.Item4;
            var stream = context.Item5;
            var reader = context.Item6;

            reader.Dispose();
            stream.Dispose();
            client.Dispose();

            callback.OnFullQueryResult(similarityContent, segmentContent, mediaObjectContent, objModel);
        }
    }
}
