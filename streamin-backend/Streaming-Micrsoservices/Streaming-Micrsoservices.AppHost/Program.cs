using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var password=builder.AddParameter("pg-password",secret:true);

var postgres = builder.AddPostgres("postgres")
    .WithPassword(password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();
    

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithLifetime(ContainerLifetime.Session)
    .WithManagementPlugin();

var elasticSearch = builder.AddElasticsearch("elasticSearch")
    .WithImage("elastic/elasticsearch", "9.4.7")
    .WithEnvironment("xpack.security.enabled", "false")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithLifetime(ContainerLifetime.Session);


// Define the user and password as Aspire parameters first
var dbUser = builder.AddParameter("audit-db-user", "audit_svc");
var dbPass = builder.AddParameter("audit-db-pass", "devpassword");

var auditPrimaryServer = builder.AddPostgres("audit-primary", dbUser, dbPass)
    .WithEnvironment("POSTGRES_DB", "init_auditdb")
    .WithEnvironment("POSTGRES_USER", "audit_svc")
    .WithEnvironment("POSTGRES_PASSWORD", "devpassword")
    .WithEnvironment("REPLICATION_USER", "replicator")
    .WithEnvironment("REPLICATION_PASSWORD", "replpassword")
    .WithEndpoint(targetPort: 5432)
    .WithArgs(
        "-c", "wal_level=replica",
        "-c", "max_wal_senders=2",
        "-c", "max_replication_slots=1",
        "-c", "wal_keep_size=64MB")
    .WithBindMount("./postgres-primary/001_init-replication.sh", "/docker-entrypoint-initdb.d/001_init-replication.sh", isReadOnly: true)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();
var auditdb = auditPrimaryServer.AddDatabase("auditdb");

var auditReplicaServer = builder.AddPostgres("audit-replica")
    .WithDockerfile(contextPath: "postgres-replica")
    .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
    //.WithEnvironment("PRIMARY_HOST", "host.docker.internal")
    .WithEnvironment("PRIMARY_HOST", "audit-primary")
    .WithEnvironment("REPLICATION_USER", "replicator")
    .WithEnvironment("REPLICATION_PASSWORD", "replpassword")
    .WithEndpoint(targetPort: 5432, name: "tcp")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WaitFor(auditPrimaryServer)
    ;
//var auditReplicaDb = auditReplicaServer.AddDatabase("replicadb");
//var identity_db = postgres.AddDatabase("identity_db");

var workservicedb = postgres.AddDatabase("workservicedb");
var identitydb = postgres.AddDatabase("identitydb");
var searchdb = postgres.AddDatabase("searchdb");
builder.AddProject<Projects.Identity_API>("identity-api")
    .WaitForStart(identitydb)
    .WithReference(identitydb)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithEnvironment("ConnectionStrings__identitydb", identitydb);

var workservice=builder.AddProject<Projects.Work_Service_API>("work-service-api")
    .WaitForStart(workservicedb)
    .WithReference(workservicedb)
    .WaitFor(rabbitmq)
    .WithReference(rabbitmq);


builder.AddProject<Projects.SearchService_API>("searchservice-api")
    .WaitForStart(searchdb)
    .WithReference(searchdb)
    .WaitForStart(elasticSearch)
    .WithReference(elasticSearch)
    .WaitForStart(rabbitmq)
    .WithReference(rabbitmq)
    .WaitFor(workservice);


builder.AddProject<Projects.Audit_Service_API>("audit-service-api")
    .WaitForStart(auditdb)
    .WithReference(auditdb)
    .WaitFor(rabbitmq)
    .WithReference(rabbitmq);


builder.Build().Run();
