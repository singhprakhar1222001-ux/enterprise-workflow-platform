namespace SearchService.API.Infrastructure.Messaging.Topology
{
    public static class Topology
    {
        public const string EventQueue = "search.queue";
        public const string routingKey = "workservice.search.#";
        public const string retryQueue30s = "search.retry-30s";
        public const string retryQueue60s = "search.retry-60s";
        public const string retryQueue90s = "search.retry-90s";
        public const string retryExchange30s = "retry-exchange-30s";
        public const string retryExchange60s = "retry-exchange-60s";
        public const string retryExchange90s = "retry-exchange-90s";
        public const string DLQqueue = "search.dlq";


    }
}
