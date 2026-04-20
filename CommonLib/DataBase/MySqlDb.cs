using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

// [필수] 가문 통일: 이제 커스텀 클래스가 아닌 MySql 전용 표준 파라미터를 사용합니다.
using MySqlParameter = MySql.Data.MySqlClient.MySqlParameter;

namespace CommonLib.DataBase
{
    public class MySqlDb : IDisposable
    {
        private MySqlConnection? _conn;
        private readonly string _connectionString;

        public MySqlDb(string connectionString)
        {
            _connectionString = connectionString;
            Connection();
        }

        private void Connection()
        {
            if (_conn == null)
            {
                _conn = new MySqlConnection(_connectionString);
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
                Console.WriteLine(ex.ToString());
            }
        }

        // Zinn님의 로직: 파라미터 추가 (이름 규칙은 MSSQL과 동일하게 적용 가능)
        private void AddParameters(MySqlCommand cmd, MySqlParameter[]? parameters)
        {
            if (parameters == null)
            {
                return;
            }

            foreach (MySqlParameter param in parameters)
            {
                // MySql도 @ 접두사가 필요하므로 동일한 안전 로직을 넣어두면 좋습니다.
                string name = param.ParameterName.StartsWith("@")
                    ? param.ParameterName
                    : "@" + param.ParameterName;

                cmd.Parameters.AddWithValue(name, param.Value);
            }
        }

        // --- 비동기 연결 보장 ---
        private async Task EnsureConnectionOpenAsync()
        {
            if (_conn == null)
            {
                _conn = new MySqlConnection(_connectionString);
            }

            if (_conn.State != ConnectionState.Open)
            {
                await _conn.OpenAsync();
            }
        }

        // --- 동기 메서드 그룹 (중괄호 유지) ---
        public IDataReader GetReader(string query, MySqlParameter[]? parameters = null)
        {
            MySqlCommand cmd = new MySqlCommand(query, _conn);
            AddParameters(cmd, parameters);
            return cmd.ExecuteReader();
        }

        public DataTable GetDataTable(string query, MySqlParameter[]? parameters = null)
        {
            using (MySqlCommand cmd = new MySqlCommand(query, _conn))
            {
                AddParameters(cmd, parameters);
                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public long Execute(string query, MySqlParameter[]? parameters = null)
        {
            using (MySqlCommand cmd = new MySqlCommand(query, _conn))
            {
                AddParameters(cmd, parameters);
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }

        // --- 비동기 메서드 그룹 (2026.04.10 추가) ---

        public async Task<MySqlDataReader> GetReaderAsync(string query, MySqlParameter[]? parameters)
        {
            await EnsureConnectionOpenAsync();
            MySqlCommand cmd = new MySqlCommand(query, _conn);
            AddParameters(cmd, parameters);
            return (MySqlDataReader)await cmd.ExecuteReaderAsync();
        }

        public async Task<long> ExecuteAsync(string query, MySqlParameter[]? parameters)
        {
            await EnsureConnectionOpenAsync();
            using (MySqlCommand cmd = new MySqlCommand(query, _conn))
            {
                AddParameters(cmd, parameters);
                await cmd.ExecuteNonQueryAsync();
                return cmd.LastInsertedId;
            }
        }

        public async Task<DataTable> GetDataTableAsync(string query, MySqlParameter[]? parameters)
        {
            await EnsureConnectionOpenAsync();
            using (MySqlCommand cmd = new MySqlCommand(query, _conn))
            {
                AddParameters(cmd, parameters);
                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    await Task.Run(() => da.Fill(dt));
                    return dt;
                }
            }
        }

        public void Dispose()
        {
            if (_conn != null)
            {
                if (_conn.State != ConnectionState.Closed)
                {
                    _conn.Close();
                }
                _conn.Dispose();
                _conn = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}