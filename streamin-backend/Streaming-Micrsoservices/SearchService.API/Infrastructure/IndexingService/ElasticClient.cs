using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using SearchService.API.IndexClass;

namespace SearchService.API.Infrastructure.IndexingService
{
    public class ElasticClient
    {
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim sem_lock = new SemaphoreSlim(1, 1);
        private ElasticsearchClient client;
        public ElasticClient(IConfiguration configuration)
        {
            _configuration = configuration;
            client = new ElasticsearchClient(new Uri(_configuration.GetConnectionString("elasticSearch")));
        }

        public async Task GetIndex()
        {
            if (client.Indices.Exists("work-index") == null)
            {
                try
                {
                    await sem_lock.WaitAsync();
                    var res = await client.Indices.CreateAsync("work-index");
                }
                catch { }
                finally { sem_lock.Release(); }
            }
        }

        public async Task BulkUpload(BulkRequest bulkRequest,CancellationToken ct)
        {
            try
            {
                var response = await client.BulkAsync(bulkRequest);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<WorkIndexBody>> Search(string request)
        {
            var res = await client.SearchAsync<WorkIndexBody>(q => q
            .Indices("work-index")
            .From(0)
            .Size(5)
            .Query(q => q
            .MultiMatch(q =>
            q.Query(request)
            .Fields()
            .Fuzziness("AUTO")
            )
            )
            );
            List<WorkIndexBody> resList = new();
            foreach( var item in res.HitsMetadata.Hits)
            {
                resList.Add(item.Source);
            }
            return resList;
        }
    }
}
