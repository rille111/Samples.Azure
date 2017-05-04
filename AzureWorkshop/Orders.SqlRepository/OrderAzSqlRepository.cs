using System;
using System.Collections.Generic;
using Orders.Shared;
using System.Data;
using System.Data.SqlClient;

namespace Orders.SqlRepository
{
    public class OrderAzSqlRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderAzSqlRepository()
        {
            var username = "SOMELOGIN";
            var password = "SOMEPASSWORD";
            var database = "SOMESQLAZUREDATABASENAME";
            var server = "SOMEAZURESQLSERVER.database.windows.net";

            _connectionString = $"Server=tcp:{server},1433;Initial Catalog={database};Persist Security Info=False;User ID={username};Password={password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }

        public OrderModel Get(Guid identifier)
        {
            var command = new SqlCommand($"select * from Orders where Identifier = '{identifier}'");
            var dt = ExecuteReaderReturnDataTable(command);
            var retur = new OrderModel();

            if (dt?.Rows != null && dt.Rows.Count > 0)
            {
                retur.Identifier = (Guid) dt.Rows[0]["Identifier"];
                retur.Name = (string )dt.Rows[0]["Name"];
                retur.TimeStamp = (DateTime)dt.Rows[0]["TimeStamp"];
            }
            return retur;
        }

        public List<OrderModel> GetAll()
        {
            var command = new SqlCommand($"select * from Orders");
            var dt = ExecuteReaderReturnDataTable(command);
            var retur = new List<OrderModel>();

            if (dt?.Rows != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    var item = new OrderModel();
                    item.Identifier = (Guid)row ["Identifier"];
                    item.Name = (string)row["Name"];
                    item.TimeStamp = (DateTime)row["TimeStamp"];
                    retur.Add(item);
                }
            }
            return retur;
        }

        public void Add(OrderModel entity)
        {
            throw new NotImplementedException("Uups!");
        }

        // Helpers

        protected DataTable ExecuteReaderReturnDataTable(SqlCommand command, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var preparedCommand = GetPreparedCommand(command, connection, parameters))
            {
                connection.Open();
                var dt = new DataTable();
                var dataReader = preparedCommand.ExecuteReader();
                dt.Load(dataReader);
                connection.Close();
                return dt;
            }
        }

        protected int ExecuteNonQuery(SqlCommand command, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var preparedCommand = GetPreparedCommand(command, connection, parameters))
            {
                connection.Open();
                var affectedRows = preparedCommand.ExecuteNonQuery();
                connection.Close();
                return affectedRows;
            }
        }

        protected static SqlCommand GetPreparedCommand(SqlCommand command, SqlConnection connection, SqlParameter[] parameters = null)
        {
            var clonedCommand = command.Clone(); //Cloning to avoid side-effects, since we want to modify the provided command
            clonedCommand.Connection = connection;
            if (parameters != null)
            {
                clonedCommand.Parameters.AddRange(parameters);
            }
            return clonedCommand;
        }
    }
}
