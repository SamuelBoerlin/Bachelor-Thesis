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

    /// <summary>
    /// Time timeout in seconds
    /// </summary>
    public int Timeout
    {
        get;
        set;
    } = 4 * 60;

    public CompleteCineastQuery(string cineastApiUrl)
    {
        Api = new Apiv1Api(new Configuration
        {
            BasePath = cineastApiUrl,
            Timeout = Timeout * 1000
        });
        ObjectDownloader = new CineastObjectDownloader(Timeout);
    }
}
