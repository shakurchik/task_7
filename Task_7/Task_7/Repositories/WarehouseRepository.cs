using System.Data;
using System.Data.SqlClient;
using Task_7.Dto;


namespace Task_7.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly IConfiguration _configuration;

        public WarehouseRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task<SqlConnection> CreateOpenConnectionAsync()
        {
            var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            await connection.OpenAsync();
            return connection;
        }

        private async Task<bool> CheckIfRecordExistsAsync(string query, params SqlParameter[] parameters)
        {
            using var connection = await CreateOpenConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddRange(parameters);

            return (await command.ExecuteScalarAsync()) != null;
        }

        public Task<bool> DoesProductExist(int id)
        {
            var query = "SELECT 1 FROM Product WHERE IdProduct = @ID";
            var parameters = new[] { new SqlParameter("@ID", id) };
            return CheckIfRecordExistsAsync(query, parameters);
        }

        public Task<bool> DoesWarehouseExist(int id)
        {
            var query = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @ID";
            var parameters = new[] { new SqlParameter("@ID", id) };
            return CheckIfRecordExistsAsync(query, parameters);
        }

        public Task<bool> DoesOrderExist(int id, int amount, DateTime createdAt)
        {
            var query = "SELECT 1 FROM [Order] WHERE Amount = @Amount AND IdProduct = @ID AND CreatedAt < @CreatedAt";
            var parameters = new[]
            {
                new SqlParameter("@ID", id),
                new SqlParameter("@Amount", amount),
                new SqlParameter("@CreatedAt", createdAt)
            };
            return CheckIfRecordExistsAsync(query, parameters);
        }

        public Task<bool> DoesOrderCompleted(int id, int amount, DateTime createdAt)
        {
            throw new NotImplementedException();
        }

        public Task UpdateOrder(int id, int amount, DateTime createdAt)
        {
            throw new NotImplementedException();
        }

        public Task<int> InsertToProductWarehouse(WarehouseDTO warehouseDto)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetOrderId(int id, int amount, DateTime createdAt)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetProductPrice(int id)
        {
            throw new NotImplementedException();
        }

        public Task<int> ExecuteProcedure(WarehouseDTO warehouseDto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsOrderCompleted(int id, int amount, DateTime createdAt)
        {
            var query = @"SELECT 1 FROM Product_Warehouse 
                          JOIN [Order] O ON Product_Warehouse.IdOrder = O.IdOrder 
                          WHERE O.Amount = @Amount AND O.IdProduct = @ID AND O.CreatedAt < @CreatedAt";
            var parameters = new[]
            {
                new SqlParameter("@ID", id),
                new SqlParameter("@Amount", amount),
                new SqlParameter("@CreatedAt", createdAt)
            };
            return CheckIfRecordExistsAsync(query, parameters);
        }

        public async Task UpdateOrderAsync(int id, int amount, DateTime createdAt)
        {
            var query = "UPDATE [Order] SET FulfilledAt = @FulfilledAt WHERE IdProduct = @ID AND Amount = @Amount AND CreatedAt = @CreatedAt";
            using var connection = await CreateOpenConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);
            command.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> InsertToProductWarehouseAsync(WarehouseDTO warehouseDto)
        {
            var insertQuery = @"INSERT INTO Product_Warehouse 
                                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                                SELECT SCOPE_IDENTITY();";
            using var connection = await CreateOpenConnectionAsync();
            using var command = new SqlCommand(insertQuery, connection);
            
            var orderId = await GetOrderIdAsync(warehouseDto.IdProduct, warehouseDto.Amount, warehouseDto.CreatedAt);
            var productPrice = await GetProductPriceAsync(warehouseDto.IdProduct);

            command.Parameters.AddWithValue("@IdWarehouse", warehouseDto.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", warehouseDto.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", orderId);
            command.Parameters.AddWithValue("@Amount", warehouseDto.Amount);
            command.Parameters.AddWithValue("@Price", warehouseDto.Amount * productPrice);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? throw new Exception("Insert failed") : Convert.ToInt32(result);
        }

        private async Task<int> GetOrderIdAsync(int id, int amount, DateTime createdAt)
        {
            var query = "SELECT IdOrder FROM [Order] WHERE Amount = @Amount AND IdProduct = @ID AND CreatedAt < @CreatedAt";
            var parameters = new[]
            {
                new SqlParameter("@ID", id),
                new SqlParameter("@Amount", amount),
                new SqlParameter("@CreatedAt", createdAt)
            };
            using var connection = await CreateOpenConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddRange(parameters);

            var orderId = await command.ExecuteScalarAsync();
            return orderId == DBNull.Value ? 0 : Convert.ToInt32(orderId);
        }

        private async Task<double> GetProductPriceAsync(int id)
        {
            var query = "SELECT Price FROM Product WHERE IdProduct = @ID";
            var parameters = new[] { new SqlParameter("@ID", id) };
            using var connection = await CreateOpenConnectionAsync();
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddRange(parameters);

            var price = await command.ExecuteScalarAsync();
            return price == DBNull.Value ? 0.0 : Convert.ToDouble(price);
        }

        public async Task<int> ExecuteProcedureAsync(WarehouseDTO warehouseDto)
        {
            using var connection = await CreateOpenConnectionAsync();
            using var command = new SqlCommand("AddProductToWarehouse", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@IdProduct", warehouseDto.IdProduct);
            command.Parameters.AddWithValue("@IdWarehouse", warehouseDto.IdWarehouse);
            command.Parameters.AddWithValue("@Amount", warehouseDto.Amount);
            command.Parameters.AddWithValue("@CreatedAt", warehouseDto.CreatedAt);

            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }
    }
}
