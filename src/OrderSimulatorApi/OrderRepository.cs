using Dapper;
using System.Data;
using Npgsql;
using Microsoft.Extensions.Options;
using Contracts;



namespace OrderSimulatorApi;

public interface IOrderRepository
{
    Task InsertOrderAsync(Order order);
    Task InsertOrdersAsync(IEnumerable<Order> orders);
    Task<Order?> GetByIdAsync(int orderId);
    Task InsertExecutionAsync(Execution execution);
    Task<bool> IsMessageProcessedAsync(string messageId);
    Task MarkAsExecutedAsync(int orderId, DateTime executedAtUtc);
    Task RegisterProcessedMessageAsync(string messageId, DateTime processedAtUtc);

}

public class OrderRepository : IOrderRepository
{
    private readonly DatabaseSettings _databaseSettings;

    public OrderRepository(IOptions<DatabaseSettings> databaseSettings)
    {
        _databaseSettings = databaseSettings.Value;
    }
public async Task InsertOrderAsync(Order order)
    {
        const string sql = @"
        INSERT INTO pte.orders
            (UserId, Symbol, Type, Quantity, LimitPrice, Status, CreatedAtUtc)
        VALUES
            (@UserId, @Symbol, @Type, @Quantity, @LimitPrice, @Status, @CreatedAtUtc)
            )
        ";

        await using var conn = new NpgsqlConnection(_databaseSettings.ConnectionString);

        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, order, commandTimeout: _databaseSettings.Timeout);

    }
public async Task InsertOrdersAsync(IEnumerable<Order> orders)
    {
        await using var conn = 
        new NpgsqlConnection(_databaseSettings.ConnectionString);

        await conn.OpenAsync();
        await using var writer = conn.BeginBinaryImport(@"
            COPY pte.orders
            (
            UserId,
            Symbol,
            Type,
            Quantity,
            LimitPrice,
            Status,
            CreatedAtUtc

            )
            FROM STDIN (FORMAT BINARY)");

            foreach (var order in orders)
        {
            await writer.StartRowAsync();

            await writer.WriteAsync(order.Symbol);
            await writer.WriteAsync(order.UserId);
            await writer.WriteAsync(order.Symbol);
            await writer.WriteAsync((short)order.Type);
            await writer.WriteAsync(order.Quantity);

            if (order.LimitPrice.HasValue)
            {
                await writer.WriteAsync(order.LimitPrice.Value);

            }
            else
            {
                await writer.WriteNullAsync();

            }
            await writer.WriteAsync((short)order.Status);
            await writer.WriteAsync(order.CreatedAtUtc);
        }
        await writer.CompleteAsync();

    }

    public async Task<Order?> GetByIdAsync(int orderId)
    {
        const string sql =@"
        SELECT
		OrderId,
		UserId,
		Symbol,
		Type,
		Quantity,
		LimitPrice,
		Status,
		CreatedAtUtc,
		ExecutedAtUt
		
			FROM pte.Orders where OrderId = @OrderId";

		await using var conn = new NpgsqlConnection(_databaseSettings.ConnectionString); 
		await conn.OpenAsync();
		return await conn.QuerySingleOrDefaultAsync<Order>(sql, new {OrderId = orderId}, commandTimeout: _databaseSettings.Timeout);

      
    }
	public async Task InsertExecutionAsync(Execution execution)
	{
		const string sql = @"
			INSERT INTO pte.executions
			(
			 OrderId,
			 ExecutedPrice,
			 Quantity,
			 ExecutedAtUtc
			)
			VALUES
			(
			 @OrderId,
			 @ExecutedPrice,
			 @Quantity,
			 @ExecutedAtUtc
			);
		";
		await using var conn = new NpgsqlConnection(_databaseSettings.ConnectionString);
		await conn.OpenAsync();
		await conn.ExecuteAsync(
				sql,
				execution,
				commandTimeout: _databaseSettings.Timeout);
	}

	public async Task<bool> IsMessageProcessedAsync(string messageId)
	{
		const string sql = @"
			SELECT 1 FROM pte.processed_messages where messageId = @messageId;
		";

		await using var conn = new NpgsqlConnection(_databaseSettings.ConnectionString);
		var result = await conn.QueryFirstOrDefaultAsync<int?>(
				sql,
				new {MessageId = messageId}, commandTimeout: _databaseSettings.Timeout);
		
		return result.HasValue;
	}

	public async Task MarkAsExecutedAsync(int orderId, DateTime executedAtUtc)
	{
		const string sql = @"
			UPDATE pte.orders
			SET
				Status = 1,
				ExecutedAtUtc = @ExecutedAtUtc
			WHERE
			OrderId = @OrderId;
			";
			await using var conn = new NpgsqlConnection(_databaseSettings.ConnectionString);
			await conn.ExecuteAsync(sql,
			new
			{
				OrderId = orderId,
				ExecutedAtUtc = executedAtUtc
			}, commandTimeout: _databaseSettings.Timeout);

	}

	public async Task RegisterProcessedMessageAsync(string messageId, DateTime processedAtUtc)
	{
		const string sql = @"
			INSERT INTO pte.processed_messages
			(
			 MessageId,
			 ProcessedAtUtc
			)
			VALUES
			(
			 @MessageId,
			 @ProcessedAtUtc
			)";
		await using var conn = new NpgsqlConnection(_databaseSettings.ConnectionString);
		await conn.OpenAsync();
		await conn.ExecuteAsync(
				sql,
				new
				{
					MessageId = messageId
				},
				commandTimeout: _databaseSettings.Timeout);


    }

}
