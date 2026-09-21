
using Microsoft.EntityFrameworkCore;
using SearchService.API.Features.SearchWork;
using SearchService.API.Infrastructure.IndexingService;
using SearchService.API.Infrastructure.Messaging.Topology;
using SearchService.API.Infrastructure.Projections;

namespace SearchService.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDbContext<AppDbContext>((options) => {
            options.UseNpgsql(builder.Configuration.GetConnectionString("searchdb"));
        });
        string connectionstring = builder.Configuration.GetConnectionString("searchdb");
        Console.WriteLine($"searchDB connection : {connectionstring}");
        builder.Services.AddServices();
        var app = builder.Build();

        

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            using var scope = app.Services.CreateScope();
            using var dbcontext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbcontext.Database.Migrate();

            var ElasticClient = scope.ServiceProvider.GetRequiredService<ElasticClient>();
            await ElasticClient.GetIndex();

            var rabbitmqInitializer = scope.ServiceProvider.GetRequiredService<ITopologyInitializer>();
            await rabbitmqInitializer.Initialize();
            

        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();
        app.MapEndpoint();
        app.Run();
    }
}
