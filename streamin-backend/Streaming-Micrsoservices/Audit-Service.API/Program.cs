using Audit_Service.API;
using Audit_Service.API.Features;
using Audit_Service.API.Infrastructure.BatchService;
using Audit_Service.API.Infrastructure.Messaging.Topology;
using Audit_Service.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("auditdb"));
});
//test remove post work
string connectionstring = builder.Configuration.GetConnectionString("auditdb");
Console.WriteLine($"audit DB: {connectionstring}");
//end of test
//builder.Services.AddDbContext<ReplicaContext>
//    (options =>
//    {
//        options.UseNpgsql(builder.Configuration.GetConnectionString("auditdb"));
//    });
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("auditdb")));
builder.Services.AddSingleton<BatchChannel>();
builder.Services.AddDependency();

var app = builder.Build();


app.MapDefaultEndpoints();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    ITopologyInitializer topologyInitializer = scope.ServiceProvider.GetRequiredService<ITopologyInitializer>();
    using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await topologyInitializer.Initialize();
    dbContext.Database.Migrate();
    
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.AddEndpoint();

app.Run();
