using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace IndustrialCommunication.Database
{
    /// <summary>
    /// MSSQL 데이터베이스 헬퍼 클래스
    /// 연결, 쿼리, 프로시저 호출, 트랜잭션 등 DB 관련 모든 기능 제공
    /// </summary>
    public class MssqlHelper : IDisposable
    {
        #region Fields & Properties

        private readonly string _connectionString;
        private SqlConnection _connection;
        private SqlTransaction _transaction;
        private bool _disposed;

        /// <summary>
        /// 연결 문자열
        /// </summary>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// 현재 연결 상태
        /// </summary>
        public bool IsConnected => _connection?.State == ConnectionState.Open;

        /// <summary>
        /// 트랜잭션 진행 중 여부
        /// </summary>
        public bool IsInTransaction => _transaction != null;

        /// <summary>
        /// 명령 타임아웃 (초)
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        #endregion

        #region Constructor

        /// <summary>
        /// 생성자 - 연결 문자열 직접 지정
        /// </summary>
        public MssqlHelper(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
        }

        /// <summary>
        /// 생성자 - 개별 파라미터 지정
        /// </summary>
        public MssqlHelper(string server, string database, string userId = null, string password = null, bool integratedSecurity = false)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = integratedSecurity
            };

            if (!integratedSecurity && !string.IsNullOrEmpty(userId))
            {
                builder.UserID = userId;
                builder.Password = password ?? string.Empty;
            }

            _connectionString = builder.ConnectionString;
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// 데이터베이스 연결
        /// </summary>
        public bool Connect()
        {
            try
            {
                if (_connection == null)
                    _connection = new SqlConnection(_connectionString);

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                return true;
            }
            catch (SqlException)
            {
                return false;
            }
        }

        /// <summary>
        /// 데이터베이스 연결 해제
        /// </summary>
        public void Disconnect()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }

            if (_connection != null)
            {
                if (_connection.State != ConnectionState.Closed)
                    _connection.Close();

                _connection.Dispose();
                _connection = null;
            }
        }

        /// <summary>
        /// 연결 테스트
        /// </summary>
        public bool TestConnection()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private SqlConnection GetConnection()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
                return _connection;

            return new SqlConnection(_connectionString);
        }

        #endregion

        #region Transaction Management

        /// <summary>
        /// 트랜잭션 시작
        /// </summary>
        public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_transaction != null)
                throw new InvalidOperationException("Transaction already in progress.");

            if (!Connect())
                throw new InvalidOperationException("Failed to connect to database.");

            _transaction = _connection.BeginTransaction(isolationLevel);
        }

        /// <summary>
        /// 트랜잭션 커밋
        /// </summary>
        public void Commit()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction in progress.");

            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
        }

        /// <summary>
        /// 트랜잭션 롤백
        /// </summary>
        public void Rollback()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction in progress.");

            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }

        #endregion

        #region Query Execution

        /// <summary>
        /// SELECT 쿼리 실행 - DataTable 반환
        /// </summary>
        public DataTable ExecuteQuery(string sql, params SqlParameter[] parameters)
        {
            return ExecuteQueryInternal(sql, CommandType.Text, parameters);
        }

        /// <summary>
        /// SELECT 쿼리 실행 - DataTable 반환 (CommandType 지정)
        /// </summary>
        public DataTable ExecuteQuery(string sql, CommandType commandType, params SqlParameter[] parameters)
        {
            return ExecuteQueryInternal(sql, commandType, parameters);
        }

        private DataTable ExecuteQueryInternal(string sql, CommandType commandType, SqlParameter[] parameters)
        {
            var dt = new DataTable();
            bool ownConnection = _connection == null || _connection.State != ConnectionState.Open;
            SqlConnection conn = null;

            try
            {
                conn = ownConnection ? new SqlConnection(_connectionString) : _connection;
                if (ownConnection) conn.Open();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandType = commandType;
                    cmd.CommandTimeout = CommandTimeout;
                    cmd.Transaction = _transaction;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            return dt;
        }

        /// <summary>
        /// SELECT 쿼리 실행 - DataSet 반환
        /// </summary>
        public DataSet ExecuteQueryDataSet(string sql, params SqlParameter[] parameters)
        {
            var ds = new DataSet();
            bool ownConnection = _connection == null || _connection.State != ConnectionState.Open;
            SqlConnection conn = null;

            try
            {
                conn = ownConnection ? new SqlConnection(_connectionString) : _connection;
                if (ownConnection) conn.Open();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = CommandTimeout;
                    cmd.Transaction = _transaction;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(ds);
                    }
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            return ds;
        }

        /// <summary>
        /// SELECT 쿼리 실행 - 제네릭 리스트 반환
        /// </summary>
        public List<T> ExecuteQuery<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
        {
            var list = new List<T>();
            bool ownConnection = _connection == null || _connection.State != ConnectionState.Open;
            SqlConnection conn = null;

            try
            {
                conn = ownConnection ? new SqlConnection(_connectionString) : _connection;
                if (ownConnection) conn.Open();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = CommandTimeout;
                    cmd.Transaction = _transaction;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(mapper(reader));
                        }
                    }
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            return list;
        }

        #endregion

        #region NonQuery Execution

        /// <summary>
        /// INSERT, UPDATE, DELETE 쿼리 실행
        /// </summary>
        public int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            return ExecuteNonQueryInternal(sql, CommandType.Text, parameters);
        }

        /// <summary>
        /// INSERT, UPDATE, DELETE 쿼리 실행 (CommandType 지정)
        /// </summary>
        public int ExecuteNonQuery(string sql, CommandType commandType, params SqlParameter[] parameters)
        {
            return ExecuteNonQueryInternal(sql, commandType, parameters);
        }

        private int ExecuteNonQueryInternal(string sql, CommandType commandType, SqlParameter[] parameters)
        {
            bool ownConnection = _connection == null || _connection.State != ConnectionState.Open;
            SqlConnection conn = null;

            try
            {
                conn = ownConnection ? new SqlConnection(_connectionString) : _connection;
                if (ownConnection) conn.Open();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandType = commandType;
                    cmd.CommandTimeout = CommandTimeout;
                    cmd.Transaction = _transaction;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
        }

        /// <summary>
        /// 단일 값 반환 쿼리 실행
        /// </summary>
        public object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            return ExecuteScalarInternal(sql, CommandType.Text, parameters);
        }

        /// <summary>
        /// 단일 값 반환 쿼리 실행 (CommandType 지정)
        /// </summary>
        public object ExecuteScalar(string sql, CommandType commandType, params SqlParameter[] parameters)
        {
            return ExecuteScalarInternal(sql, commandType, parameters);
        }

        private object ExecuteScalarInternal(string sql, CommandType commandType, SqlParameter[] parameters)
        {
            bool ownConnection = _connection == null || _connection.State != ConnectionState.Open;
            SqlConnection conn = null;

            try
            {
                conn = ownConnection ? new SqlConnection(_connectionString) : _connection;
                if (ownConnection) conn.Open();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandType = commandType;
                    cmd.CommandTimeout = CommandTimeout;
                    cmd.Transaction = _transaction;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    return cmd.ExecuteScalar();
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
        }

        /// <summary>
        /// 단일 값 반환 쿼리 실행 (제네릭)
        /// </summary>
        public T ExecuteScalar<T>(string sql, params SqlParameter[] parameters)
        {
            var result = ExecuteScalar(sql, parameters);
            if (result == null || result == DBNull.Value)
                return default;
            return (T)Convert.ChangeType(result, typeof(T));
        }

        #endregion

        #region Stored Procedure

        /// <summary>
        /// 저장 프로시저 실행 - DataTable 반환
        /// </summary>
        public DataTable ExecuteProcedure(string procedureName, params SqlParameter[] parameters)
        {
            return ExecuteQueryInternal(procedureName, CommandType.StoredProcedure, parameters);
        }

        /// <summary>
        /// 저장 프로시저 실행 - 영향받은 행 수 반환
        /// </summary>
        public int ExecuteProcedureNonQuery(string procedureName, params SqlParameter[] parameters)
        {
            return ExecuteNonQueryInternal(procedureName, CommandType.StoredProcedure, parameters);
        }

        /// <summary>
        /// 저장 프로시저 실행 - 단일 값 반환
        /// </summary>
        public object ExecuteProcedureScalar(string procedureName, params SqlParameter[] parameters)
        {
            return ExecuteScalarInternal(procedureName, CommandType.StoredProcedure, parameters);
        }

        /// <summary>
        /// 저장 프로시저 실행 - OUTPUT 파라미터 포함
        /// </summary>
        public int ExecuteProcedureWithOutput(string procedureName, SqlParameter[] parameters, out Dictionary<string, object> outputValues)
        {
            outputValues = new Dictionary<string, object>();
            bool ownConnection = _connection == null || _connection.State != ConnectionState.Open;
            SqlConnection conn = null;
            int result;

            try
            {
                conn = ownConnection ? new SqlConnection(_connectionString) : _connection;
                if (ownConnection) conn.Open();

                using (var cmd = new SqlCommand(procedureName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = CommandTimeout;
                    cmd.Transaction = _transaction;

                    if (parameters != null && parameters.Length > 0)
                        cmd.Parameters.AddRange(parameters);

                    result = cmd.ExecuteNonQuery();

                    // OUTPUT 파라미터 값 수집
                    foreach (SqlParameter param in cmd.Parameters)
                    {
                        if (param.Direction == ParameterDirection.Output ||
                            param.Direction == ParameterDirection.InputOutput ||
                            param.Direction == ParameterDirection.ReturnValue)
                        {
                            outputValues[param.ParameterName] = param.Value;
                        }
                    }
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }

            return result;
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// 대량 데이터 삽입 (Bulk Insert)
        /// </summary>
        public void BulkInsert(DataTable dataTable, string destinationTableName, int batchSize = 1000)
        {
            bool ownConnection = _connection == null || _connection.State != ConnectionState.Open;
            SqlConnection conn = null;

            try
            {
                conn = ownConnection ? new SqlConnection(_connectionString) : _connection;
                if (ownConnection) conn.Open();

                using (var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, _transaction))
                {
                    bulkCopy.DestinationTableName = destinationTableName;
                    bulkCopy.BatchSize = batchSize;
                    bulkCopy.BulkCopyTimeout = CommandTimeout;

                    // 컬럼 매핑
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    bulkCopy.WriteToServer(dataTable);
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                {
                    conn.Close();
                    conn.Dispose();
                }
            }
        }

        #endregion

        #region Parameter Helpers

        /// <summary>
        /// SqlParameter 생성 헬퍼
        /// </summary>
        public static SqlParameter CreateParameter(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        /// <summary>
        /// SqlParameter 생성 헬퍼 (타입 지정)
        /// </summary>
        public static SqlParameter CreateParameter(string name, SqlDbType type, object value)
        {
            var param = new SqlParameter(name, type)
            {
                Value = value ?? DBNull.Value
            };
            return param;
        }

        /// <summary>
        /// OUTPUT SqlParameter 생성 헬퍼
        /// </summary>
        public static SqlParameter CreateOutputParameter(string name, SqlDbType type, int size = 0)
        {
            var param = new SqlParameter(name, type)
            {
                Direction = ParameterDirection.Output
            };
            if (size > 0) param.Size = size;
            return param;
        }

        /// <summary>
        /// RETURN VALUE SqlParameter 생성 헬퍼
        /// </summary>
        public static SqlParameter CreateReturnParameter(string name = "@ReturnValue")
        {
            return new SqlParameter(name, SqlDbType.Int)
            {
                Direction = ParameterDirection.ReturnValue
            };
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// DataReader에서 안전하게 값 읽기
        /// </summary>
        public static T GetValue<T>(SqlDataReader reader, string columnName, T defaultValue = default)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return defaultValue;
                return (T)Convert.ChangeType(reader[ordinal], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// DataRow에서 안전하게 값 읽기
        /// </summary>
        public static T GetValue<T>(DataRow row, string columnName, T defaultValue = default)
        {
            try
            {
                if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                    return defaultValue;
                return (T)Convert.ChangeType(row[columnName], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Disconnect();
            }

            _disposed = true;
        }

        ~MssqlHelper()
        {
            Dispose(false);
        }

        #endregion
    }
}
