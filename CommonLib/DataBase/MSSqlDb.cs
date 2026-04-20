using Microsoft.Data.SqlClient;
using Mysqlx.Session;
using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;

namespace CommonLib.DataBase
{
    public class MSSqlDb : IDisposable
    {
        private SqlConnection? _conn;
        private readonly string _connectionString;

        public MSSqlDb(string connectionString)
        {
            _connectionString = connectionString;
            Connection();
        }

        private void Connection()
        {
            if(_conn == null)
            {
                _conn = new SqlConnection(_connectionString);
            }
            try
            {
                if (_conn.State != ConnectionState.Open)
                {
                    _conn.Open();
                }
            
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }
        }

        // [Zinn님 원본 로직 유지] MSSQL 전용 파라미터 처리
        private void AddParameters(SqlCommand cmd, SqlParameter[]? parameters)
        {
            if (parameters == null) return;

            foreach (SqlParameter param in parameters)
            {
                // 이름에 @가 없으면 붙여주는 Zinn님의 친절한 로직
                string name = param.ParameterName.StartsWith("@")
                    ? param.ParameterName
                    : "@" + param.ParameterName;

                // 표준 SqlParameter의 특성상 AddWithValue로 이름과 값을 매핑하여 추가
                cmd.Parameters.AddWithValue(name, param.Value);
            }
        }

        // --- 동기 메서드 ---
        public IDataReader GetReader(string query, SqlParameter[]? parameters = null)
        {
            SqlCommand cmd = new SqlCommand(query, _conn);
            AddParameters(cmd, parameters);
            return cmd.ExecuteReader();
        }

        public DataTable GetDataTable(string query, SqlParameter[]? parameters = null)
        {
            using SqlCommand cmd = new SqlCommand(query, _conn);
            AddParameters(cmd, parameters);
            using SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public long Execute(string query, SqlParameter[]? parameters = null)
        {
            using SqlCommand cmd = new SqlCommand(query, _conn);
            AddParameters(cmd, parameters);
            return (long)cmd.ExecuteNonQuery();
        }


        // --- 2026.04.10 비동기 메서드 ---
        private async Task EnsureConnectionOpenAsync()
        {
            if(_conn == null)
            {
                _conn = new SqlConnection(_connectionString);
            }

            // Open
            if(_conn.State != ConnectionState.Open)
            {
                await _conn.OpenAsync();
            }

        }

        public async Task<SqlDataReader> GetReaderAsync(string query, SqlParameter[]? parameters)
        {
            await EnsureConnectionOpenAsync();
            SqlCommand cmd = new SqlCommand(query, _conn);
            AddParameters(cmd, parameters);
            return await cmd.ExecuteReaderAsync();
        }

        public async Task<long> ExecuteAsync(string query, SqlParameter[]? parameters)
        {
            await EnsureConnectionOpenAsync();

            // using 선언: 메서드 범위가 끝나면 자동으로 Dispose 됩니다.
            using var cmd = new SqlCommand(query, _conn);

            AddParameters(cmd, parameters);

            int result = await cmd.ExecuteNonQueryAsync();
            return (long)result;
        }

        public async Task<DataTable> GetDataTableAsync(string query, SqlParameter[]? parameters)
        {
            await EnsureConnectionOpenAsync();

            using var cmd = new SqlCommand(query, _conn);
            AddParameters(cmd, parameters);

            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();

            // Fill은 동기 메서드이므로 Task.Run으로 별도 스레드에서 실행
            await Task.Run(() => da.Fill(dt));

            return dt;
        }


        public void Dispose()
        {
            if (_conn != null)
            {
                if (_conn.State != ConnectionState.Closed) _conn.Close();
                _conn.Dispose();
                _conn = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}