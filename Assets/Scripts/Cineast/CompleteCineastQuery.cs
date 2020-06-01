using Cineast_OpenAPI_Implementation;
using IO.Swagger.Api;
using IO.Swagger.Client;

public class CompleteCineastQuery
{
    public Apiv1Api Api
    {
        get;
        private set;
    }

    public CineastObjectDownloader ObjectDownloader
    {
        get;
        private set;
    }

    public CompleteCineastQuery(string cineastApiUrl)
    {
        Api = new Apiv1Api(new Configuration
        {
            BasePath = cineastApiUrl
        });
        ObjectDownloader = new CineastObjectDownloader();
    }
}
