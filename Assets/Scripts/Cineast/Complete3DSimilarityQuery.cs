using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Cineast_OpenAPI_Implementation
{
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
                Debug.Log("Start 3D Similarity Query Request");
                return query;
            }

            public SimilarityQueryResultBatch OnFinishQuery(SimilarityQueryResultBatch result)
            {
                Debug.Log("Finished 3D Similarity Query Request");

                Debug.Log("Results:");
                Debug.Log("");
                return result;
            }

            public IdList OnStartSegmentsByIdQuery(SimilarityQueryResult similarityResult, StringDoublePair entry, IdList idList)
            {
                Debug.Log("---------------------------");
                Debug.Log("Segment ID: " + entry.Key + ", Similarity Score: " + entry.Value);
                return idList;
            }

            public MediaSegmentQueryResult OnFinishSegmentsByIdQuery(SimilarityQueryResult similarityResult, StringDoublePair entry, MediaSegmentQueryResult result)
            {
                return result;
            }

            public void OnStartObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor)
            {
                Debug.Log("Object ID: " + descriptor.ObjectId);
            }

            public MediaObjectQueryResult OnFinishObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor, MediaObjectQueryResult result)
            {
                return result;
            }

            public void OnFullQueryResult(StringDoublePair entry, MediaSegmentDescriptor segmentDescriptor, MediaObjectDescriptor objectDescriptor, string objModel)
            {
                Debug.Log("Downloaded Object: ");

                var lines = objModel.Split('\n');
                int maxLines = Math.Min(lines.Length, 8);
                for (int i = 0; i < maxLines; i++)
                {
                    Console.WriteLine(lines[i]);
                }
                Debug.Log("...");
                Debug.Log("---------------------------");
                Debug.Log("");
            }
        }

        public Complete3DSimilarityQuery(string cineastApiUrl) : base(cineastApiUrl)
        {
        }

        public async Task PerformAsync(List<string> categories, string modelJson, Callback callback, Handler handler)
        {
            var testModelData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(modelJson));

            var terms = new List<QueryTerm>
            {
                new QueryTerm
                {
                    Categories = categories,
                    Type = QueryTerm.TypeEnum.MODEL3D,
                    Data = "data:application/3d-json," + testModelData
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
            var segmentQueryContext = new Dictionary<Task<MediaSegmentQueryResult>, Tuple<SimilarityQueryResult, StringDoublePair>>();

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
                    segmentQueryContext[segmentQuery] = Tuple.Create(similarityResult, similarityContent);
                }
            }

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
                            string objModel;

                            using (var stream = await ObjectDownloader.RequestContentAsync(Api, mediaObjectContent, segmentContent))
                            using (var reader = new StreamReader(stream))
                            {
                                objModel = reader.ReadToEnd();
                            }

                            callback.OnFullQueryResult(similarityContent, segmentContent, mediaObjectContent, objModel);

                            //Just one result expected
                            break;
                        }
                    }

                    //Just one result expected
                    break;
                }
            }
        }
    }
}
