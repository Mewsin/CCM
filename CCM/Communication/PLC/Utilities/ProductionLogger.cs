using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using CCM.Communication.Interfaces;
using CCM.Database;

namespace CCM.Communication.PLC.Utilities
{
    /// <summary>
    /// 로깅 항목 정의
    /// </summary>
    public class LogItem
    {
        /// <summary>
        /// 항목 이름 (DB 컬럼명으로 사용)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 디바이스 타입 (D, M, X, Y 등)
        /// </summary>
        public string Device { get; set; }

        /// <summary>
        /// 시작 주소
        /// </summary>
        public int Address { get; set; }

        /// <summary>
        /// 데이터 타입
        /// </summary>
        public LogDataType DataType { get; set; } = LogDataType.Word;

        /// <summary>
        /// 워드 수 (String 타입인 경우)
        /// </summary>
        public int WordCount { get; set; } = 1;

        /// <summary>
        /// 스케일 팩터 (값 * ScaleFactor)
        /// </summary>
        public double ScaleFactor { get; set; } = 1.0;

        /// <summary>
        /// 오프셋 (값 + Offset)
        /// </summary>
        public double Offset { get; set; } = 0.0;

        /// <summary>
        /// 소수점 자릿수 (Real 타입인 경우)
        /// </summary>
        public int DecimalPlaces { get; set; } = 2;

        /// <summary>
        /// SQL 파라미터 타입
        /// </summary>
        public SqlDbType SqlType { get; set; } = SqlDbType.Int;

        /// <summary>
        /// 현재 값 (읽은 후 저장)
        /// </summary>
        public object CurrentValue { get; internal set; }
    }

    /// <summary>
    /// 로깅 데이터 타입
    /// </summary>
    public enum LogDataType
    {
        Bit,
        Word,
        DWord,
        Real,
        String
    }

    /// <summary>
    /// 로깅 트리거 모드
    /// </summary>
    public enum LogTriggerMode
    {
        /// <summary>주기적 로깅</summary>
        Periodic,
        /// <summary>트리거 비트 감지 시 로깅</summary>
        OnTrigger,
        /// <summary>값 변경 시 로깅</summary>
        OnChange
    }

    /// <summary>
    /// 로깅 완료 이벤트 인자
    /// </summary>
    public class LogCompletedEventArgs : EventArgs
    {
        public DateTime Timestamp { get; }
        public Dictionary<string, object> LoggedData { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public LogCompletedEventArgs(DateTime timestamp, Dictionary<string, object> data, bool success, string errorMessage = null)
        {
            Timestamp = timestamp;
            LoggedData = data;
            IsSuccess = success;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 생산 데이터 로거 클래스
    /// - PLC에서 주기적으로 데이터 수집
    /// - MssqlHelper를 통해 DB 저장
    /// - 트리거 기반 / 주기 기반 / 변경 기반 로깅 지원
    /// </summary>
    public class ProductionLogger : IDisposable
    {
        #region Fields

        private readonly IPlcCommunication _plc;
        private readonly MssqlHelper _db;
        private readonly List<LogItem> _logItems = new List<LogItem>();
        private readonly object _lockObject = new object();

        private Thread _logThread;
        private bool _isRunning;
        private bool _disposed;

        private bool _previousTriggerState;
        private Dictionary<string, object> _previousValues = new Dictionary<string, object>();

        #endregion

        #region Properties

        /// <summary>
        /// 대상 테이블명
        /// </summary>
        public string TableName { get; set; } = "ProductionLog";

        /// <summary>
        /// 타임스탬프 컬럼명
        /// </summary>
        public string TimestampColumn { get; set; } = "LogTime";

        /// <summary>
        /// 로깅 간격 (밀리초), 기본값 1000ms
        /// </summary>
        public int LogInterval { get; set; } = 1000;

        /// <summary>
        /// 로깅 트리거 모드
        /// </summary>
        public LogTriggerMode TriggerMode { get; set; } = LogTriggerMode.Periodic;

        /// <summary>
        /// 트리거 비트 디바이스 (TriggerMode가 OnTrigger인 경우)
        /// </summary>
        public string TriggerDevice { get; set; }

        /// <summary>
        /// 트리거 비트 주소
        /// </summary>
        public int TriggerAddress { get; set; }

        /// <summary>
        /// 트리거 Rising Edge 감지 (기본값: true)
        /// </summary>
        public bool TriggerOnRisingEdge { get; set; } = true;

        /// <summary>
        /// 로깅 실행 중 여부
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 등록된 로깅 항목 수
        /// </summary>
        public int ItemCount => _logItems.Count;

        /// <summary>
        /// 에러 발생 시 로깅 계속 여부 (기본값: true)
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// 동적 INSERT 쿼리 사용 여부 (기본값: true)
        /// false인 경우 저장 프로시저 사용
        /// </summary>
        public bool UseDynamicInsert { get; set; } = true;

        /// <summary>
        /// 저장 프로시저 이름 (UseDynamicInsert가 false인 경우)
        /// </summary>
        public string StoredProcedureName { get; set; } = "sp_InsertProductionLog";

        /// <summary>
        /// 총 로깅 횟수
        /// </summary>
        public long TotalLogCount { get; private set; }

        /// <summary>
        /// 마지막 로깅 시간
        /// </summary>
        public DateTime? LastLogTime { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// 로깅 완료 시 발생
        /// </summary>
        public event EventHandler<LogCompletedEventArgs> LogCompleted;

        /// <summary>
        /// 에러 발생 시 발생
        /// </summary>
        public event EventHandler<Exception> ErrorOccurred;

        #endregion

        #region Constructor

        /// <summary>
        /// ProductionLogger 생성자
        /// </summary>
        /// <param name="plc">PLC 통신 인터페이스</param>
        /// <param name="db">DB 헬퍼</param>
        public ProductionLogger(IPlcCommunication plc, MssqlHelper db)
        {
            _plc = plc ?? throw new ArgumentNullException(nameof(plc));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// ProductionLogger 생성자 (테이블명 지정)
        /// </summary>
        public ProductionLogger(IPlcCommunication plc, MssqlHelper db, string tableName)
            : this(plc, db)
        {
            TableName = tableName;
        }

        #endregion

        #region Item Management

        /// <summary>
        /// 로깅 항목 추가
        /// </summary>
        public void AddItem(LogItem item)
        {
            lock (_lockObject)
            {
                _logItems.Add(item);
            }
        }

        /// <summary>
        /// 로깅 항목 추가 (간편 버전 - Word)
        /// </summary>
        public void AddWord(string name, string device, int address, double scaleFactor = 1.0, double offset = 0.0)
        {
            AddItem(new LogItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = LogDataType.Word,
                ScaleFactor = scaleFactor,
                Offset = offset,
                SqlType = SqlDbType.Int
            });
        }

        /// <summary>
        /// 로깅 항목 추가 (간편 버전 - DWord)
        /// </summary>
        public void AddDWord(string name, string device, int address, double scaleFactor = 1.0, double offset = 0.0)
        {
            AddItem(new LogItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = LogDataType.DWord,
                ScaleFactor = scaleFactor,
                Offset = offset,
                SqlType = SqlDbType.BigInt
            });
        }

        /// <summary>
        /// 로깅 항목 추가 (간편 버전 - Real)
        /// </summary>
        public void AddReal(string name, string device, int address, int decimalPlaces = 2, double scaleFactor = 1.0, double offset = 0.0)
        {
            AddItem(new LogItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = LogDataType.Real,
                ScaleFactor = scaleFactor,
                Offset = offset,
                DecimalPlaces = decimalPlaces,
                SqlType = SqlDbType.Float
            });
        }

        /// <summary>
        /// 로깅 항목 추가 (간편 버전 - Bit)
        /// </summary>
        public void AddBit(string name, string device, int address)
        {
            AddItem(new LogItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = LogDataType.Bit,
                SqlType = SqlDbType.Bit
            });
        }

        /// <summary>
        /// 로깅 항목 추가 (간편 버전 - String)
        /// </summary>
        public void AddString(string name, string device, int address, int wordCount)
        {
            AddItem(new LogItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = LogDataType.String,
                WordCount = wordCount,
                SqlType = SqlDbType.NVarChar
            });
        }

        /// <summary>
        /// 로깅 항목 제거
        /// </summary>
        public bool RemoveItem(string name)
        {
            lock (_lockObject)
            {
                return _logItems.RemoveAll(x => x.Name == name) > 0;
            }
        }

        /// <summary>
        /// 모든 로깅 항목 제거
        /// </summary>
        public void ClearItems()
        {
            lock (_lockObject)
            {
                _logItems.Clear();
            }
        }

        /// <summary>
        /// 모든 로깅 항목 조회
        /// </summary>
        public List<LogItem> GetAllItems()
        {
            lock (_lockObject)
            {
                return new List<LogItem>(_logItems);
            }
        }

        #endregion

        #region Logging Control

        /// <summary>
        /// 로깅 시작
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            if (!_plc.IsConnected)
                throw new InvalidOperationException("PLC가 연결되어 있지 않습니다.");

            _isRunning = true;
            _previousTriggerState = false;
            _previousValues.Clear();

            _logThread = new Thread(LogThreadProc)
            {
                IsBackground = true,
                Name = "ProductionLogger_Thread"
            };
            _logThread.Start();
        }

        /// <summary>
        /// 로깅 중지
        /// </summary>
        public void Stop()
        {
            _isRunning = false;

            if (_logThread != null && _logThread.IsAlive)
            {
                _logThread.Join(2000);
                _logThread = null;
            }
        }

        /// <summary>
        /// 수동으로 한 번 로깅
        /// </summary>
        public bool LogOnce()
        {
            return PerformLogging();
        }

        #endregion

        #region Private Methods

        private void LogThreadProc()
        {
            while (_isRunning)
            {
                try
                {
                    if (!_plc.IsConnected)
                    {
                        Thread.Sleep(LogInterval);
                        continue;
                    }

                    bool shouldLog = false;

                    switch (TriggerMode)
                    {
                        case LogTriggerMode.Periodic:
                            shouldLog = true;
                            break;

                        case LogTriggerMode.OnTrigger:
                            shouldLog = CheckTrigger();
                            break;

                        case LogTriggerMode.OnChange:
                            shouldLog = CheckValueChange();
                            break;
                    }

                    if (shouldLog)
                    {
                        PerformLogging();
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    if (!ContinueOnError)
                    {
                        _isRunning = false;
                        break;
                    }
                }

                Thread.Sleep(LogInterval);
            }
        }

        private bool CheckTrigger()
        {
            if (string.IsNullOrEmpty(TriggerDevice))
                return false;

            var result = _plc.ReadBit(TriggerDevice, TriggerAddress);
            if (!result.IsSuccess)
                return false;

            bool currentState = result.Value;
            bool triggered = false;

            if (TriggerOnRisingEdge)
            {
                // Rising Edge: Off → On
                triggered = currentState && !_previousTriggerState;
            }
            else
            {
                // Falling Edge: On → Off
                triggered = !currentState && _previousTriggerState;
            }

            _previousTriggerState = currentState;
            return triggered;
        }

        private bool CheckValueChange()
        {
            List<LogItem> items;
            lock (_lockObject)
            {
                items = new List<LogItem>(_logItems);
            }

            bool changed = false;

            foreach (var item in items)
            {
                object currentValue = ReadItemValue(item);
                if (currentValue == null) continue;

                if (_previousValues.TryGetValue(item.Name, out var prevValue))
                {
                    if (!ValuesEqual(prevValue, currentValue))
                    {
                        changed = true;
                        break;
                    }
                }
                else
                {
                    // 첫 번째 읽기는 변경으로 처리하지 않음
                    _previousValues[item.Name] = currentValue;
                }
            }

            return changed;
        }

        private bool PerformLogging()
        {
            var timestamp = DateTime.Now;
            var loggedData = new Dictionary<string, object>();
            bool success = false;
            string errorMessage = null;

            try
            {
                List<LogItem> items;
                lock (_lockObject)
                {
                    items = new List<LogItem>(_logItems);
                }

                // 1. PLC에서 데이터 읽기
                foreach (var item in items)
                {
                    object value = ReadItemValue(item);
                    item.CurrentValue = value;
                    loggedData[item.Name] = value;
                    _previousValues[item.Name] = value;
                }

                // 2. DB에 저장
                if (UseDynamicInsert)
                {
                    success = InsertUsingDynamicQuery(timestamp, items);
                }
                else
                {
                    success = InsertUsingStoredProcedure(timestamp, items);
                }

                if (success)
                {
                    TotalLogCount++;
                    LastLogTime = timestamp;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                ErrorOccurred?.Invoke(this, ex);
            }

            LogCompleted?.Invoke(this, new LogCompletedEventArgs(timestamp, loggedData, success, errorMessage));
            return success;
        }

        private object ReadItemValue(LogItem item)
        {
            try
            {
                switch (item.DataType)
                {
                    case LogDataType.Bit:
                        var bitResult = _plc.ReadBit(item.Device, item.Address);
                        return bitResult.IsSuccess ? bitResult.Value : (object)null;

                    case LogDataType.Word:
                        var wordResult = _plc.ReadWord(item.Device, item.Address);
                        if (wordResult.IsSuccess)
                        {
                            double value = wordResult.Value * item.ScaleFactor + item.Offset;
                            return (int)value;
                        }
                        return null;

                    case LogDataType.DWord:
                        var dwordResult = _plc.ReadDWord(item.Device, item.Address);
                        if (dwordResult.IsSuccess)
                        {
                            double value = dwordResult.Value * item.ScaleFactor + item.Offset;
                            return (long)value;
                        }
                        return null;

                    case LogDataType.Real:
                        var realResult = _plc.ReadReal(item.Device, item.Address);
                        if (realResult.IsSuccess)
                        {
                            double value = realResult.Value * item.ScaleFactor + item.Offset;
                            return Math.Round(value, item.DecimalPlaces);
                        }
                        return null;

                    case LogDataType.String:
                        var strResult = _plc.ReadString(item.Device, item.Address, item.WordCount * 2);
                        return strResult.IsSuccess ? strResult.Value : null;

                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private bool InsertUsingDynamicQuery(DateTime timestamp, List<LogItem> items)
        {
            var columns = new List<string> { TimestampColumn };
            var paramNames = new List<string> { "@" + TimestampColumn };
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@" + TimestampColumn, SqlDbType.DateTime) { Value = timestamp }
            };

            foreach (var item in items)
            {
                if (item.CurrentValue != null)
                {
                    columns.Add(item.Name);
                    paramNames.Add("@" + item.Name);
                    parameters.Add(new SqlParameter("@" + item.Name, item.SqlType) 
                    { 
                        Value = item.CurrentValue ?? DBNull.Value 
                    });
                }
            }

            string sql = $"INSERT INTO {TableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)})";
            
            int affected = _db.ExecuteNonQuery(sql, parameters.ToArray());
            return affected > 0;
        }

        private bool InsertUsingStoredProcedure(DateTime timestamp, List<LogItem> items)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@" + TimestampColumn, SqlDbType.DateTime) { Value = timestamp }
            };

            foreach (var item in items)
            {
                parameters.Add(new SqlParameter("@" + item.Name, item.SqlType)
                {
                    Value = item.CurrentValue ?? DBNull.Value
                });
            }

            int affected = _db.ExecuteProcedureNonQuery(StoredProcedureName, parameters.ToArray());
            return affected >= 0; // 프로시저는 영향받은 행 수가 0일 수도 있음
        }

        private bool ValuesEqual(object oldValue, object newValue)
        {
            if (oldValue == null && newValue == null) return true;
            if (oldValue == null || newValue == null) return false;
            return oldValue.Equals(newValue);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 테이블 생성 SQL 생성
        /// </summary>
        public string GenerateCreateTableSql()
        {
            var columns = new List<string>
            {
                "Id INT IDENTITY(1,1) PRIMARY KEY",
                $"{TimestampColumn} DATETIME NOT NULL"
            };

            lock (_lockObject)
            {
                foreach (var item in _logItems)
                {
                    string sqlType = GetSqlTypeString(item.SqlType);
                    columns.Add($"{item.Name} {sqlType} NULL");
                }
            }

            return $"CREATE TABLE {TableName} (\n    {string.Join(",\n    ", columns)}\n)";
        }

        /// <summary>
        /// 저장 프로시저 생성 SQL 생성
        /// </summary>
        public string GenerateStoredProcedureSql()
        {
            var parameters = new List<string>
            {
                $"@{TimestampColumn} DATETIME"
            };

            var columns = new List<string> { TimestampColumn };
            var values = new List<string> { $"@{TimestampColumn}" };

            lock (_lockObject)
            {
                foreach (var item in _logItems)
                {
                    string sqlType = GetSqlTypeString(item.SqlType);
                    parameters.Add($"@{item.Name} {sqlType} = NULL");
                    columns.Add(item.Name);
                    values.Add($"@{item.Name}");
                }
            }

            return $@"CREATE PROCEDURE {StoredProcedureName}
    {string.Join(",\n    ", parameters)}
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO {TableName} ({string.Join(", ", columns)})
    VALUES ({string.Join(", ", values)})
END";
        }

        private string GetSqlTypeString(SqlDbType sqlType)
        {
            switch (sqlType)
            {
                case SqlDbType.Bit: return "BIT";
                case SqlDbType.Int: return "INT";
                case SqlDbType.BigInt: return "BIGINT";
                case SqlDbType.Float: return "FLOAT";
                case SqlDbType.NVarChar: return "NVARCHAR(100)";
                case SqlDbType.DateTime: return "DATETIME";
                default: return "NVARCHAR(MAX)";
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
                Stop();
                ClearItems();
            }

            _disposed = true;
        }

        ~ProductionLogger()
        {
            Dispose(false);
        }

        #endregion
    }
}
