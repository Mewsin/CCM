using System;
using System.Collections.Generic;
using System.Threading;
using CCM.Communication.Interfaces;

namespace CCM.Communication.PLC.Utilities
{
    /// <summary>
    /// 모니터링 항목 정의
    /// </summary>
    public class MonitorItem
    {
        /// <summary>
        /// 항목 이름 (식별용)
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
        /// 읽을 개수
        /// </summary>
        public int Count { get; set; } = 1;

        /// <summary>
        /// 데이터 타입
        /// </summary>
        public MonitorDataType DataType { get; set; } = MonitorDataType.Word;

        /// <summary>
        /// 현재 값
        /// </summary>
        public object CurrentValue { get; internal set; }

        /// <summary>
        /// 이전 값
        /// </summary>
        public object PreviousValue { get; internal set; }

        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public DateTime LastUpdated { get; internal set; }
    }

    /// <summary>
    /// 모니터링 데이터 타입
    /// </summary>
    public enum MonitorDataType
    {
        Bit,
        Word,
        DWord,
        Real,
        Words
    }

    /// <summary>
    /// 데이터 변경 이벤트 인자
    /// </summary>
    public class DataChangedEventArgs : EventArgs
    {
        public MonitorItem Item { get; }
        public object OldValue { get; }
        public object NewValue { get; }
        public DateTime Timestamp { get; }

        public DataChangedEventArgs(MonitorItem item, object oldValue, object newValue)
        {
            Item = item;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 모니터링 에러 이벤트 인자
    /// </summary>
    public class MonitorErrorEventArgs : EventArgs
    {
        public MonitorItem Item { get; }
        public Exception Exception { get; }
        public string Message { get; }

        public MonitorErrorEventArgs(MonitorItem item, Exception ex)
        {
            Item = item;
            Exception = ex;
            Message = ex.Message;
        }
    }

    /// <summary>
    /// PLC 데이터 주기적 모니터링 클래스
    /// - 설정된 간격으로 PLC 데이터를 폴링
    /// - 값 변경 시 이벤트 발생
    /// - 다중 항목 동시 모니터링 지원
    /// </summary>
    public class PlcMonitor : IDisposable
    {
        #region Fields

        private readonly IPlcCommunication _plc;
        private readonly List<MonitorItem> _items = new List<MonitorItem>();
        private Thread _monitorThread;
        private bool _isRunning;
        private readonly object _lockObject = new object();
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>
        /// 폴링 간격 (밀리초), 기본값 1000ms
        /// </summary>
        public int PollingInterval { get; set; } = 1000;

        /// <summary>
        /// 모니터링 실행 중 여부
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 등록된 모니터링 항목 수
        /// </summary>
        public int ItemCount => _items.Count;

        /// <summary>
        /// 에러 발생 시 모니터링 계속 여부 (기본값: true)
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        #endregion

        #region Events

        /// <summary>
        /// 데이터 변경 시 발생
        /// </summary>
        public event EventHandler<DataChangedEventArgs> DataChanged;

        /// <summary>
        /// 폴링 완료 시 발생 (모든 항목 읽기 완료)
        /// </summary>
        public event EventHandler PollingCompleted;

        /// <summary>
        /// 에러 발생 시 발생
        /// </summary>
        public event EventHandler<MonitorErrorEventArgs> ErrorOccurred;

        #endregion

        #region Constructor

        /// <summary>
        /// PlcMonitor 생성자
        /// </summary>
        /// <param name="plc">PLC 통신 인터페이스</param>
        public PlcMonitor(IPlcCommunication plc)
        {
            _plc = plc ?? throw new ArgumentNullException(nameof(plc));
        }

        /// <summary>
        /// PlcMonitor 생성자 (폴링 간격 지정)
        /// </summary>
        public PlcMonitor(IPlcCommunication plc, int pollingInterval) : this(plc)
        {
            PollingInterval = pollingInterval;
        }

        #endregion

        #region Item Management

        /// <summary>
        /// 모니터링 항목 추가
        /// </summary>
        public void AddItem(MonitorItem item)
        {
            lock (_lockObject)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// 모니터링 항목 추가 (간편 버전)
        /// </summary>
        public void AddItem(string name, string device, int address, MonitorDataType dataType = MonitorDataType.Word, int count = 1)
        {
            AddItem(new MonitorItem
            {
                Name = name,
                Device = device,
                Address = address,
                DataType = dataType,
                Count = count
            });
        }

        /// <summary>
        /// 비트 모니터링 항목 추가
        /// </summary>
        public void AddBit(string name, string device, int address)
        {
            AddItem(name, device, address, MonitorDataType.Bit);
        }

        /// <summary>
        /// 워드 모니터링 항목 추가
        /// </summary>
        public void AddWord(string name, string device, int address)
        {
            AddItem(name, device, address, MonitorDataType.Word);
        }

        /// <summary>
        /// 연속 워드 모니터링 항목 추가
        /// </summary>
        public void AddWords(string name, string device, int address, int count)
        {
            AddItem(name, device, address, MonitorDataType.Words, count);
        }

        /// <summary>
        /// 모니터링 항목 제거
        /// </summary>
        public bool RemoveItem(string name)
        {
            lock (_lockObject)
            {
                return _items.RemoveAll(x => x.Name == name) > 0;
            }
        }

        /// <summary>
        /// 모든 모니터링 항목 제거
        /// </summary>
        public void ClearItems()
        {
            lock (_lockObject)
            {
                _items.Clear();
            }
        }

        /// <summary>
        /// 모니터링 항목 조회
        /// </summary>
        public MonitorItem GetItem(string name)
        {
            lock (_lockObject)
            {
                return _items.Find(x => x.Name == name);
            }
        }

        /// <summary>
        /// 모든 모니터링 항목 조회
        /// </summary>
        public List<MonitorItem> GetAllItems()
        {
            lock (_lockObject)
            {
                return new List<MonitorItem>(_items);
            }
        }

        #endregion

        #region Monitoring Control

        /// <summary>
        /// 모니터링 시작
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            if (!_plc.IsConnected)
                throw new InvalidOperationException("PLC가 연결되어 있지 않습니다.");

            _isRunning = true;
            _monitorThread = new Thread(MonitorThreadProc)
            {
                IsBackground = true,
                Name = "PlcMonitor_Thread"
            };
            _monitorThread.Start();
        }

        /// <summary>
        /// 모니터링 중지
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
        /// 수동으로 한 번 폴링 실행
        /// </summary>
        public void PollOnce()
        {
            List<MonitorItem> items;
            lock (_lockObject)
            {
                items = new List<MonitorItem>(_items);
            }

            foreach (var item in items)
            {
                ReadItem(item);
            }

            PollingCompleted?.Invoke(this, EventArgs.Empty);
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

                    List<MonitorItem> items;
                    lock (_lockObject)
                    {
                        items = new List<MonitorItem>(_items);
                    }

                    foreach (var item in items)
                    {
                        if (!_isRunning) break;
                        ReadItem(item);
                    }

                    PollingCompleted?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, new MonitorErrorEventArgs(null, ex));
                    if (!ContinueOnError)
                    {
                        _isRunning = false;
                        break;
                    }
                }

                Thread.Sleep(PollingInterval);
            }
        }

        private void ReadItem(MonitorItem item)
        {
            try
            {
                object newValue = null;

                switch (item.DataType)
                {
                    case MonitorDataType.Bit:
                        var bitResult = _plc.ReadBit(item.Device, item.Address);
                        if (bitResult.IsSuccess)
                            newValue = bitResult.Value;
                        break;

                    case MonitorDataType.Word:
                        var wordResult = _plc.ReadWord(item.Device, item.Address);
                        if (wordResult.IsSuccess)
                            newValue = wordResult.Value;
                        break;

                    case MonitorDataType.DWord:
                        var dwordResult = _plc.ReadDWord(item.Device, item.Address);
                        if (dwordResult.IsSuccess)
                            newValue = dwordResult.Value;
                        break;

                    case MonitorDataType.Real:
                        var realResult = _plc.ReadReal(item.Device, item.Address);
                        if (realResult.IsSuccess)
                            newValue = realResult.Value;
                        break;

                    case MonitorDataType.Words:
                        var wordsResult = _plc.ReadWords(item.Device, item.Address, item.Count);
                        if (wordsResult.IsSuccess)
                            newValue = wordsResult.Value;
                        break;
                }

                if (newValue != null)
                {
                    bool changed = !ValuesEqual(item.CurrentValue, newValue);

                    item.PreviousValue = item.CurrentValue;
                    item.CurrentValue = newValue;
                    item.LastUpdated = DateTime.Now;

                    if (changed)
                    {
                        DataChanged?.Invoke(this, new DataChangedEventArgs(item, item.PreviousValue, newValue));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, new MonitorErrorEventArgs(item, ex));
                if (!ContinueOnError)
                    throw;
            }
        }

        private bool ValuesEqual(object oldValue, object newValue)
        {
            if (oldValue == null && newValue == null) return true;
            if (oldValue == null || newValue == null) return false;

            if (oldValue is short[] oldArr && newValue is short[] newArr)
            {
                if (oldArr.Length != newArr.Length) return false;
                for (int i = 0; i < oldArr.Length; i++)
                {
                    if (oldArr[i] != newArr[i]) return false;
                }
                return true;
            }

            return oldValue.Equals(newValue);
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

        ~PlcMonitor()
        {
            Dispose(false);
        }

        #endregion
    }
}
