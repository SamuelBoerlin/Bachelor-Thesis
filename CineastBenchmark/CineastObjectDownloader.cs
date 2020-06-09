using IO.Swagger.Api;
using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cineast_OpenAPI_Implementation
{
    public class CineastObjectDownloader
    {
        private readonly int timeout;

        public bool UseCineastServer { get; set; } = true;

        public string HostBaseUrl { get; set; }
        public string HostThumbnailsPath { get; set; } = "thumbnails/:o/:s:x";
        public string HostContentPath { get; set; } = "objects/:p";
        public bool UseDescriptorContentPath { get; set; } = false;

        private Dictionary<MediaObjectDescriptor.MediatypeEnum, string> suffices = new Dictionary<MediaObjectDescriptor.MediatypeEnum, string>()
        {
            { MediaObjectDescriptor.MediatypeEnum.IMAGE, "png" },
            { MediaObjectDescriptor.MediatypeEnum.VIDEO, "png" }
        };
        public string DefaultSuffix = "jpg";


        public CineastObjectDownloader(int timeout)
        {
            this.timeout = timeout;
        }

        public void RegisterSuffix(MediaObjectDescriptor.MediatypeEnum type, string suffix)
        {
            suffices[type] = suffix;
        }

        // Note on creating a new HttpClient for each request:
        // For some reason HttpClientHandler.MaxConnectionsPerServer causes a "the method or operation is not implemented" exception
        // when changing the value. Only one or a few downloads would be able to be started and run at once causing a slow down.
        // Hence the workaround of creating a new client for each request and then disposing it after finishing.

        public async Task<(Stream, HttpClient)> RequestThumbnailAsync(Apiv1Api api, MediaObjectDescriptor objectDescriptor, MediaSegmentDescriptor segmentDescriptor)
        {
            if (UseCineastServer)
            {
                //TODO Currently not supported
                //return await api.ApiV1GetThumbnailsIdGetAsync(objectDescriptor.ObjectId);
            }
            if (HostBaseUrl == null)
            {
                throw new InvalidOperationException("HostBaseUrl is null");
            }
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeout);
            return (await client.GetStreamAsync(HostBaseUrl + CompletePath(HostThumbnailsPath, objectDescriptor, segmentDescriptor)), client);
        }

        public async Task<(Stream, HttpClient)> RequestContentAsync(Apiv1Api api, MediaObjectDescriptor objectDescriptor, MediaSegmentDescriptor segmentDescriptor)
        {
            if (UseCineastServer)
            {
                //TODO Currently not supported
                //return await api.ApiV1GetObjectsIdGetAsync(objectDescriptor.ObjectId);
            }
            HttpClient client;
            if (UseDescriptorContentPath)
            {
                client = new HttpClient();
                return (await client.GetStreamAsync(HostBaseUrl + objectDescriptor.ContentURL), client);
            }
            if (HostBaseUrl == null)
            {
                throw new InvalidOperationException("HostBaseUrl is null");
            }
            client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeout);
            return (await client.GetStreamAsync(HostBaseUrl + CompletePath(HostContentPath, objectDescriptor, segmentDescriptor)), client);
        }

        private string CompletePath(string path, MediaObjectDescriptor objectDescriptor, MediaSegmentDescriptor segmentDescriptor)
        {
            string suffix = DefaultSuffix;
            if (objectDescriptor.Mediatype.HasValue)
            {
                if (!suffices.TryGetValue(objectDescriptor.Mediatype.Value, out suffix)) suffix = DefaultSuffix;
            }

            //Same as in vitrivr-ng: https://github.com/vitrivr/vitrivr-ng/blob/master/src/app/core/basics/resolver.service.ts
            path = path.Replace(":o", objectDescriptor.ObjectId);
            path = path.Replace(":n", objectDescriptor.Name);
            path = path.Replace(":p", objectDescriptor.Path);
            if (objectDescriptor.Mediatype.HasValue)
            {
                path = path.Replace(":t", Enum.GetName(typeof(MediaObjectDescriptor.MediatypeEnum), objectDescriptor.Mediatype.Value).ToLower());
                path = path.Replace(":T", Enum.GetName(typeof(MediaObjectDescriptor.MediatypeEnum), objectDescriptor.Mediatype.Value).ToUpper());
            }
            path = path.Replace(":s", segmentDescriptor.SegmentId);
            path = path.Replace(":x", "." + suffix);

            return path;
        }
    }
}
