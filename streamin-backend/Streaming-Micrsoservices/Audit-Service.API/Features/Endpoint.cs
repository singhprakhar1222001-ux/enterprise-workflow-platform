using Audit_Service.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Audit_Service.API.Features
{
    public record SearchQuery(
        Guid? ActorId = null,
        Guid? WorkId = null,
        JsonDocument? Properties = null,
        DateTime? OccuredOn = null,
        DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    //IReadOnlyDictionary<string, object>? PropertiesMatch = null,
    int PageSize = 100,
    string? PageToken = null
        )
    {

    }
    
    public class AuditQuery(NpgsqlDataSource _datasource)
    {
        //create base query
        public async Task<AuditSearchResult> Search(SearchQuery query,CancellationToken ct)
        {
            var sqlQuery = new StringBuilder("""
            Select "ActorId", "WorkId", "EventId", "Properties", "OccuredOn" from "AuditEvents"
            Where
            """);
            List<NpgsqlParameter> parameters = new();
            AuditCursor? cursor = AuditCursor.Decode(query.PageToken);
            void Addfilter(string sqlFragment, string paramName, object Value, NpgsqlDbType type)
            {
                sqlQuery.Append('\n').Append(sqlFragment);
                parameters.Add(new NpgsqlParameter(paramName, type) { Value=Value});

            }
            if(query.ActorId is { } actorId)
            {
                Addfilter("""and "ActorId"=@actorId""", "actorId", actorId, NpgsqlDbType.Uuid);
            }
            if(query.WorkId is { } workId)
            {
                Addfilter("""and "WorkId"=@workId""", "workId", workId, NpgsqlDbType.Uuid);
            }
            if(query.Properties is { } props)
            {
                string propfilters = JsonSerializer.Serialize(props);
                Addfilter("""And "Properties" @> @propfilters::jsonb""", "propfilters", propfilters, NpgsqlDbType.Jsonb);


            }
            if(cursor is { } c)
            {
                sqlQuery.Append(""""
                    AND ("OccuredOn,EventId) < (@occuredOn,@eventId)
                    """");

                parameters.Add(new NpgsqlParameter("occuredOn", NpgsqlDbType.Timestamp) { Value = c.OccurredOn });
                parameters.Add(new NpgsqlParameter("eventId",NpgsqlDbType.Uuid) { Value = c.EventId });

            }

            sqlQuery.Append("""
                Order by "OccuredOn" Desc
                """);

            using var cmd=_datasource.CreateCommand(sqlQuery.ToString());
            cmd.Parameters.AddRange(parameters.ToArray());
            var result=new List<AuditEvent>();
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    result.Add(new AuditEvent
                    {
                        ActorId = reader.GetGuid(0),
                        WorkId = reader.GetGuid(1),
                        EventId = reader.GetGuid(2),
                        Properties = await reader.GetFieldValueAsync<string>(4, ct),
                        OccuredOn = reader.GetFieldValue<DateTime>(5)
                    });
                }

            }
            string? nextToken = null;
            if (result.Count > query.PageSize)
            {
                var last = result[query.PageSize - 1]; // trim the lookahead row
                result.RemoveAt(query.PageSize);
                nextToken = new AuditCursor(last.OccuredOn, last.EventId).Encode();
            }

            return new AuditSearchResult(result, nextToken);

        }
        
    }

    public sealed record AuditSearchResult(
    IReadOnlyList<AuditEvent> Events,
    string? NextPageToken);

    internal readonly record struct AuditCursor(DateTimeOffset OccurredOn, Guid EventId)
    {
        public string Encode()
        {
            var raw = $"{OccurredOn.UtcTicks}_{EventId:N}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        public static AuditCursor? Decode(string? token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = raw.Split('_');
            return new AuditCursor(
                new DateTimeOffset(long.Parse(parts[0]), TimeSpan.Zero),
                Guid.ParseExact(parts[1], "N"));
        }
    }

    public static class Endpoints
    {
        public static void AddEndpoint(this IEndpointRouteBuilder route)
        {
            route.MapGet("api/Audit/search", async ([FromBody] SearchQuery query,[FromServices] AuditQuery queryEngine,CancellationToken ct) =>
            {
                var res=await queryEngine.Search(query,ct);
                return TypedResults.Ok(res);
            });
        }
    }

}
