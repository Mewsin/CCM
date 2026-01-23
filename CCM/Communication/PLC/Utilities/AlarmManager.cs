using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CCM.Communication.Interfaces;

namespace CCM.Communication.PLC.Utilities
{
    /// <summary>
    /// 알람 정의
    /// </summary>
    public class AlarmDefinition
    {
        /// <summary>
        /// 알람 코드 (고유 식별자)
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 알람 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 알람 설명
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 워드 인덱스 (알람 영역 내 오프셋)
        /// </summary>
        public int WordIndex { get; set; }

        /// <summary>
        /// 비트 위치 (0-15)
        /// </summary>
        public int BitPosition { get; set; }

        /// <summary>
        /// 알람 등급
        /// </summary>
        public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;

        /// <summary>
        /// 알람 그룹/카테고리
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// 자동 리셋 여부
        /// </summary>
        public bool AutoReset { get; set; } = false;
    }

    /// <summary>
    /// 알람 등급
    /// </summary>
    public enum AlarmSeverity
    {
        /// <summary>정보</summary>
        Info = 0,
        /// <summary>경고</summary>
        Warning = 1,
        /// <summary>에러</summary>
        Error = 2,
        /// <summary>치명적</summary>
        Critical = 3
    }

    /// <summary>
    /// 알람 상태
    /// </summary>
    public enum AlarmState
    {
        /// <summary>정상</summary>
        Normal,
        /// <summary>발생</summary>
        Active,
        /// <summary>확인됨 (Acknowledged)</summary>
        Acknowledged,
        /// <summary>해제됨</summary>
        Cleared
    }

    /// <summary>
    /// 알람 이력 항목
    /// </summary>
    public class AlarmHistoryItem
    {
        /// <summary>
        /// 알람 정의
        /// </summary>
        public AlarmDefinition Definition { get; set; }

        /// <summary>
        /// 발생 시간
        /// </summary>
        public DateTime OccurredTime { get; set; }

        /// <summary>
        /// 해제 시간
        /// </summary>
        public DateTime? ClearedTime { get; set; }

        /// <summary>
        /// 확인 시간
        /// </summary>
        public DateTime? AcknowledgedTime { get; set; }

        /// <summary>
        /// 확인자
        /// </summary>
        public string AcknowledgedBy { get; set; }

        /// <summary>
        /// 현재 상태
        /// </summary>
        public AlarmState State { get; set; }

        /// <summary>
        /// 지속 시간 (해제된 경우)
        /// </summary>
        public TimeSpan? Duration => ClearedTime.HasValue ? ClearedTime.Value - OccurredTime : (TimeSpan?)null;
    }

    /// <summary>
    /// 알람 이벤트 인자
    /// </summary>
    public class AlarmEventArgs : EventArgs
    {
        public AlarmHistoryItem Alarm { get; }
        public bool IsActive { get; }
        public DateTime Timestamp { get; }

        public AlarmEventArgs(AlarmHistoryItem alarm, bool isActive)
        {
            Alarm = alarm;
            IsActive = isActive;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// PLC 알람 관리 클래스
    /// - 워드 단위 알람 비트 파싱
    /// - 알람 발생/해제 이력 관리
    /// - 알람 확인(Acknowledge) 기능
    /// </summary>
    public class AlarmManager : IDisposable
    {
        #region Fields

        private readonly IPlcCommunication _plc;
        private readonly Dictionary<int, AlarmDefinition> _alarmDefinitions = new Dictionary<int, AlarmDefinition>();
        private readonly Dictionary<int, AlarmHistoryItem> _activeAlarms = new Dictionary<int, AlarmHistoryItem>();
        private readonly List<AlarmHistoryItem> _alarmHistory = new List<AlarmHistoryItem>();
        private readonly object _lockObject = new object();

        private Thread _monitorThread;
        private bool _isRunning;
        private bool _disposed;

        private short[] _previousAlarmWords;

        #endregion

        #region Properties

        /// <summary>
        /// 알람 영역 디바이스 (예: D1000)
        /// </summary>
        public string AlarmDevice { get; set; } = "D";

        /// <summary>
        /// 알람 영역 시작 주소
        /// </summary>
        public int AlarmStartAddress { get; set; } = 1000;

        /// <summary>
        /// 알람 워드 수
        /// </summary>
        public int AlarmWordCount { get; set; } = 10;

        /// <summary>
        /// 폴링 간격 (밀리초), 기본값 500ms
        /// </summary>
        public int PollingInterval { get; set; } = 500;

        /// <summary>
        /// 모니터링 실행 중 여부
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 현재 활성 알람 수
        /// </summary>
        public int ActiveAlarmCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _activeAlarms.Count;
                }
            }
        }

        /// <summary>
        /// 이력 최대 보관 개수 (기본값 1000)
        /// </summary>
        public int MaxHistoryCount { get; set; } = 1000;

        /// <summary>
        /// 에러 발생 시 모니터링 계속 여부 (기본값: true)
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        #endregion

        #region Events

        /// <summary>
        /// 알람 발생 시 발생
        /// </summary>
        public event EventHandler<AlarmEventArgs> AlarmOccurred;

        /// <summary>
        /// 알람 해제 시 발생
        /// </summary>
        public event EventHandler<AlarmEventArgs> AlarmCleared;

        /// <summary>
        /// 에러 발생 시 발생
        /// </summary>
        public event EventHandler<Exception> ErrorOccurred;

        #endregion

        #region Constructor

        /// <summary>
        /// AlarmManager 생성자
        /// </summary>
        /// <param name="plc">PLC 통신 인터페이스</param>
        public AlarmManager(IPlcCommunication plc)
        {
            _plc = plc ?? throw new ArgumentNullException(nameof(plc));
        }

        /// <summary>
        /// AlarmManager 생성자 (알람 영역 설정 포함)
        /// </summary>
        public AlarmManager(IPlcCommunication plc, string device, int startAddress, int wordCount)
            : this(plc)
        {
            AlarmDevice = device;
            AlarmStartAddress = startAddress;
            AlarmWordCount = wordCount;
        }

        #endregion

        #region Alarm Definition

        /// <summary>
        /// 알람 정의 추가
        /// </summary>
        public void AddAlarm(AlarmDefinition alarm)
        {
            lock (_lockObject)
            {
                _alarmDefinitions[alarm.Code] = alarm;
            }
        }

        /// <summary>
        /// 알람 정의 추가 (간편 버전)
        /// </summary>
        public void AddAlarm(int code, string name, int wordIndex, int bitPosition,
            AlarmSeverity severity = AlarmSeverity.Warning, string description = null, string group = null)
        {
            AddAlarm(new AlarmDefinition
            {
                Code = code,
                Name = name,
                WordIndex = wordIndex,
                BitPosition = bitPosition,
                Severity = severity,
                Description = description ?? name,
                Group = group
            });
        }

        /// <summary>
        /// 알람 정의 일괄 추가 (워드별 자동 생성)
        /// </summary>
        /// <param name="wordIndex">워드 인덱스</param>
        /// <param name="baseCode">시작 알람 코드</param>
        /// <param name="alarmNames">비트별 알람 이름 (최대 16개)</param>
        /// <param name="severity">알람 등급</param>
        /// <param name="group">알람 그룹</param>
        public void AddAlarmsForWord(int wordIndex, int baseCode, string[] alarmNames,
            AlarmSeverity severity = AlarmSeverity.Warning, string group = null)
        {
            for (int i = 0; i < Math.Min(alarmNames.Length, 16); i++)
            {
                if (!string.IsNullOrEmpty(alarmNames[i]))
                {
                    AddAlarm(baseCode + i, alarmNames[i], wordIndex, i, severity, null, group);
                }
            }
        }

        /// <summary>
        /// 알람 정의 제거
        /// </summary>
        public bool RemoveAlarm(int code)
        {
            lock (_lockObject)
            {
                return _alarmDefinitions.Remove(code);
            }
        }

        /// <summary>
        /// 모든 알람 정의 제거
        /// </summary>
        public void ClearAlarmDefinitions()
        {
            lock (_lockObject)
            {
                _alarmDefinitions.Clear();
            }
        }

        /// <summary>
        /// 알람 정의 조회
        /// </summary>
        public AlarmDefinition GetAlarmDefinition(int code)
        {
            lock (_lockObject)
            {
                return _alarmDefinitions.TryGetValue(code, out var alarm) ? alarm : null;
            }
        }

        /// <summary>
        /// 모든 알람 정의 조회
        /// </summary>
        public List<AlarmDefinition> GetAllAlarmDefinitions()
        {
            lock (_lockObject)
            {
                return _alarmDefinitions.Values.ToList();
            }
        }

        #endregion

        #region Monitoring Control

        /// <summary>
        /// 알람 모니터링 시작
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            if (!_plc.IsConnected)
                throw new InvalidOperationException("PLC가 연결되어 있지 않습니다.");

            _isRunning = true;
            _previousAlarmWords = null;

            _monitorThread = new Thread(MonitorThreadProc)
            {
                IsBackground = true,
                Name = "AlarmManager_Thread"
            };
            _monitorThread.Start();
        }

        /// <summary>
        /// 알람 모니터링 중지
        /// </summary>
        public void Stop()
        {
            _isRunning = false;

            if (_monitorThread != null && _monitorThread.IsAlive)
            {
                _monitorThread.Join(2000);
                _monitorThread = null;
            }
        }

        /// <summary>
        /// 수동으로 한 번 스캔
        /// </summary>
        public void ScanOnce()
        {
            if (!_plc.IsConnected)
                return;

            var result = _plc.ReadWords(AlarmDevice, AlarmStartAddress, AlarmWordCount);
            if (result.IsSuccess)
            {
                ProcessAlarmWords(result.Value);
            }
        }

        #endregion

        #region Alarm Operations

        /// <summary>
        /// 활성 알람 목록 조회
        /// </summary>
        public List<AlarmHistoryItem> GetActiveAlarms()
        {
            lock (_lockObject)
            {
                return _activeAlarms.Values.ToList();
            }
        }

        /// <summary>
        /// 특정 등급 이상의 활성 알람 조회
        /// </summary>
        public List<AlarmHistoryItem> GetActiveAlarms(AlarmSeverity minSeverity)
        {
            lock (_lockObject)
            {
                return _activeAlarms.Values
                    .Where(a => a.Definition.Severity >= minSeverity)
                    .ToList();
            }
        }

        /// <summary>
        /// 알람 이력 조회
        /// </summary>
        public List<AlarmHistoryItem> GetAlarmHistory(int count = 100)
        {
            lock (_lockObject)
            {
                return _alarmHistory.OrderByDescending(a => a.OccurredTime).Take(count).ToList();
            }
        }

        /// <summary>
        /// 기간별 알람 이력 조회
        /// </summary>
        public List<AlarmHistoryItem> GetAlarmHistory(DateTime from, DateTime to)
        {
            lock (_lockObject)
            {
                return _alarmHistory
                    .Where(a => a.OccurredTime >= from && a.OccurredTime <= to)
                    .OrderByDescending(a => a.OccurredTime)
                    .ToList();
            }
        }

        /// <summary>
        /// 알람 확인 (Acknowledge)
        /// </summary>
        public bool AcknowledgeAlarm(int code, string acknowledgedBy = null)
        {
            lock (_lockObject)
            {
                if (_activeAlarms.TryGetValue(code, out var alarm))
                {
                    if (alarm.State == AlarmState.Active)
                    {
                        alarm.State = AlarmState.Acknowledged;
                        alarm.AcknowledgedTime = DateTime.Now;
                        alarm.AcknowledgedBy = acknowledgedBy;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 모든 활성 알람 확인 (Acknowledge All)
        /// </summary>
        public int AcknowledgeAll(string acknowledgedBy = null)
        {
            int count = 0;
            lock (_lockObject)
            {
                foreach (var alarm in _activeAlarms.Values)
                {
                    if (alarm.State == AlarmState.Active)
                    {
                        alarm.State = AlarmState.Acknowledged;
                        alarm.AcknowledgedTime = DateTime.Now;
                        alarm.AcknowledgedBy = acknowledgedBy;
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// 이력 초기화
        /// </summary>
        public void ClearHistory()
        {
            lock (_lockObject)
            {
                _alarmHistory.Clear();
            }
        }

        /// <summary>
        /// 특정 알람 활성 여부 확인
        /// </summary>
        public bool IsAlarmActive(int code)
        {
            lock (_lockObject)
            {
                return _activeAlarms.ContainsKey(code);
            }
        }

        /// <summary>
        /// Critical 알람 존재 여부
        /// </summary>
        public bool HasCriticalAlarm()
        {
            lock (_lockObject)
            {
                return _activeAlarms.Values.Any(a => a.Definition.Severity == AlarmSeverity.Critical);
            }
        }

        /// <summary>
        /// 알람 통계 조회
        /// </summary>
        public AlarmStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                return new AlarmStatistics
                {
                    TotalHistoryCount = _alarmHistory.Count,
                    ActiveAlarmCount = _activeAlarms.Count,
                    InfoCount = _activeAlarms.Values.Count(a => a.Definition.Severity == AlarmSeverity.Info),
                    WarningCount = _activeAlarms.Values.Count(a => a.Definition.Severity == AlarmSeverity.Warning),
                    ErrorCount = _activeAlarms.Values.Count(a => a.Definition.Severity == AlarmSeverity.Error),
                    CriticalCount = _activeAlarms.Values.Count(a => a.Definition.Severity == AlarmSeverity.Critical),
                    UnacknowledgedCount = _activeAlarms.Values.Count(a => a.State == AlarmState.Active)
                };
            }
        }

        #endregion

        #region Private Methods

        private void MonitorThreadProc()
        {
            while (_isRunning)
            {
                try
                {
                    if (!_plc.IsConnected)
                    {
                        Thread.Sleep(PollingInterval);
                        continue;
                    }

                    var result = _plc.ReadWords(AlarmDevice, AlarmStartAddress, AlarmWordCount);
                    if (result.IsSuccess)
                    {
                        ProcessAlarmWords(result.Value);
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

                Thread.Sleep(PollingInterval);
            }
        }

        private void ProcessAlarmWords(short[] currentWords)
        {
            lock (_lockObject)
            {
                foreach (var alarmDef in _alarmDefinitions.Values)
                {
                    if (alarmDef.WordIndex >= currentWords.Length)
                        continue;

                    bool currentBit = ((currentWords[alarmDef.WordIndex] >> alarmDef.BitPosition) & 1) == 1;
                    bool previousBit = false;

                    if (_previousAlarmWords != null && alarmDef.WordIndex < _previousAlarmWords.Length)
                    {
                        previousBit = ((_previousAlarmWords[alarmDef.WordIndex] >> alarmDef.BitPosition) & 1) == 1;
                    }

                    // 알람 발생 (Off → On)
                    if (currentBit && !previousBit)
                    {
                        var historyItem = new AlarmHistoryItem
                        {
                            Definition = alarmDef,
                            OccurredTime = DateTime.Now,
                            State = AlarmState.Active
                        };

                        _activeAlarms[alarmDef.Code] = historyItem;
                        AddToHistory(historyItem);

                        AlarmOccurred?.Invoke(this, new AlarmEventArgs(historyItem, true));
                    }
                    // 알람 해제 (On → Off)
                    else if (!currentBit && previousBit)
                    {
                        if (_activeAlarms.TryGetValue(alarmDef.Code, out var historyItem))
                        {
                            historyItem.ClearedTime = DateTime.Now;
                            historyItem.State = AlarmState.Cleared;

                            _activeAlarms.Remove(alarmDef.Code);

                            AlarmCleared?.Invoke(this, new AlarmEventArgs(historyItem, false));
                        }
                    }
                }

                _previousAlarmWords = (short[])currentWords.Clone();
            }
        }

        private void AddToHistory(AlarmHistoryItem item)
        {
            _alarmHistory.Add(item);

            // 최대 개수 초과 시 오래된 항목 제거
            while (_alarmHistory.Count > MaxHistoryCount)
            {
                _alarmHistory.RemoveAt(0);
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
                lock (_lockObject)
                {
                    _alarmDefinitions.Clear();
                    _activeAlarms.Clear();
                    _alarmHistory.Clear();
                }
            }

            _disposed = true;
        }

        ~AlarmManager()
        {
            Dispose(false);
        }

        #endregion
    }

    /// <summary>
    /// 알람 통계
    /// </summary>
    public class AlarmStatistics
    {
        public int TotalHistoryCount { get; set; }
        public int ActiveAlarmCount { get; set; }
        public int InfoCount { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public int CriticalCount { get; set; }
        public int UnacknowledgedCount { get; set; }
    }
}
