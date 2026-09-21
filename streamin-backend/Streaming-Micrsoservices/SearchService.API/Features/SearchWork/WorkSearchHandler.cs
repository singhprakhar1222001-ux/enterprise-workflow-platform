using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Newtonsoft.Json;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using SearchService.API.Infrastructure.IndexingService;

namespace SearchService.API.Features.SearchWork
{
    public class Response
    {
        public string? response {  get; set; }
        public int responseCode { get; set; }
        public string? error { get; set; }
    }
    
    public record WorkSearchRequest(string term) : IRequest<Response> { }
    public class WorkSearchHandler : IRequestHandler<WorkSearchRequest, Response>
    {
        private readonly ElasticClient _client;
        public WorkSearchHandler(ElasticClient client)
        {
            _client = client;
        }
        public async Task<Response> Handle(WorkSearchRequest request, CancellationToken cancellationToken)
        {
            var res=await _client.Search(request.term);
            return new Response
            {
                error = null,
                responseCode = 200,
                response = JsonConvert.SerializeObject(res)
            };
        }

        
    }

    public static class WebResponse
    {
        public static void MapEndpoint(this IEndpointRouteBuilder router)
        {
            router.MapPost("api/search", ReqHandler);
        }
        public static async Task<Results<Ok<Response>, NotFound<Response>>> ReqHandler(WorkSearchRequest request,IMediator mediator)
        {
            var res=await mediator.Send(request);
            if (res.error != null) {
                return TypedResults.NotFound(res);
            }
            return TypedResults.Ok(res);
        }
    }
}
