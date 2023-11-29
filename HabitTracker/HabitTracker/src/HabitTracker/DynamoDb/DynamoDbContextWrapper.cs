﻿using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace HabitTracker.DynamoDb
{
    public class DynamoDbContextWrapper
    {
        private readonly DynamoDBContext _dynamoDbContext;
        private readonly DynamoDBOperationConfig _operationConfig;

        public DynamoDbContextWrapper()
        {
            var region = Environment.GetEnvironmentVariable("TABLE_REGION") ?? throw new NullReferenceException();
            var client = new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(region));
            _dynamoDbContext = new DynamoDBContext(client);
            var tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? throw new NullReferenceException();
            _operationConfig = new DynamoDBOperationConfig
            {
                OverrideTableName = tableName
            };
        }

        public Task SaveAsync<T>(T item) where T : class
        {
            return _dynamoDbContext.SaveAsync(item, _operationConfig);
        }

        public Task DeleteAsync<T>(T item) where T : class
        {
            return _dynamoDbContext.DeleteAsync(item, _operationConfig);
        }

        public Task<List<T>> QueryAsync<T>(QueryOperationConfig config) where T : class
        {
            return _dynamoDbContext.FromQueryAsync<T>(config, _operationConfig).GetRemainingAsync();
        }

        public Task<List<T>> QueryAsync<T>(object hashKeyValue, QueryOperator queryOperator, IEnumerable<object> values) where T : class
        {
            return _dynamoDbContext.QueryAsync<T>(hashKeyValue, queryOperator, values, _operationConfig).GetRemainingAsync();
        }

        public Task<List<T>> QueryWithEmptyBeginsWithAsync<T>(object hashKeyValue) where T : class
        {
            return QueryAsync<T>(hashKeyValue, QueryOperator.BeginsWith, new string[] { string.Empty });
        }

        public T FromDocument<T>(Document document)
        {
            return _dynamoDbContext.FromDocument<T>(document);
        }
    }
}
