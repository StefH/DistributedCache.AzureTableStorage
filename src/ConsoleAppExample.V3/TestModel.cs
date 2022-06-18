using System;
using Azure;
using Azure.Data.Tables;

namespace ConsoleAppExample.V3;

public class TestModel : ITableEntity
{
    public string PartitionKey { get; set; }

    public string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public int Id { get; set; }

    public string Name { get; set; }
}