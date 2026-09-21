using Audit_Service.API.Infrastructure.BatchService;
using Audit_Service.API.Infrastructure.Persistence;
using Npgsql;
using NpgsqlTypes;
using System.Data.Common;

namespace Audit_Service.API.Infrastructure.BufferService
{
    public sealed class BufferWriterJob(
        BatchChannel channel,
        NpgsqlDataSource dataSource
        ) : BackgroundService


    {
        private const string CopyCommand = """
        COPY "AuditEvents"
        ("ActorId", "WorkId", "EventId", "Properties", "OccuredOn")
        FROM STDIN (FORMAT BINARY)
        """;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = channel.Reader;
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
           
            List<AuditEvent> Buffer = new();
             
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var timertick = timer.WaitForNextTickAsync(stoppingToken).AsTask();
                    var readAvailable = reader.WaitToReadAsync(stoppingToken).AsTask();
                    await Task.WhenAny(readAvailable, timertick);

                    if (timertick.IsCompleted)
                    {
                        //flush when completed even if empty
                        await flush(Buffer, stoppingToken);
                    }
                    while (Buffer.Count() < 20 && reader.TryRead(out var item))
                    {
                        Buffer.Add(item);
                    }
                    if (Buffer.Count > 20)
                    {
                        await flush(Buffer, stoppingToken);
                    }
                }
            }
            catch (Exception ex) {
                await flush(Buffer, stoppingToken);
                throw ex;
            }
            finally
            {
                await flush(Buffer, stoppingToken);
            }
        }

        private async Task flush(List<AuditEvent> buffer,CancellationToken ct)
        {
            if (buffer.Count == 0)
            {
                return;
            }

            var batch = buffer.ToArray();
            buffer.Clear();

            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(ct);
                await using var writer = await connection.BeginBinaryImportAsync(CopyCommand, ct);

                foreach (var e in batch)
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(e.EventId, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(e.ActorId, NpgsqlDbType.Uuid, ct);
                    await writer.WriteAsync(e.WorkId, NpgsqlDbType.Uuid, ct);
                    
                    await writer.WriteAsync(e.Properties, NpgsqlDbType.Jsonb, ct);
                    await writer.WriteAsync(e.OccuredOn, NpgsqlDbType.TimestampTz, ct);
                    
                    
                }

                await writer.CompleteAsync(ct);
               // logger.LogInformation("Flushed {Count} audit events via COPY", batch.Length);
            }
            catch (Exception ex)
            {
                // COPY is all-or-nothing: if it fails, none of this batch made it in.
                // Requeue for a retry on the next flush. NOTE: this is a simple at-least-once
                // strategy - a batch that fails repeatedly (e.g. one malformed row) will retry
                // forever. For production, cap retries per event and route persistent failures
                // to a dead-letter table/topic instead of requeuing indefinitely.
               // logger.LogError(ex, "Failed to flush {Count} audit events, requeuing for retry", batch.Length);

                foreach (var e in batch)
                {
                    await channel.TryWriteAsync(e, CancellationToken.None);
                }
            }

        }
    }
}

