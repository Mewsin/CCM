using System;
using System.IO.Ports;
using System.Threading;
using CCM.Communication.Interfaces;

namespace CCM.Communication.Serial
{
    /// <summary>
    /// 시리얼 포트 통신 헬퍼 클래스
    /// </summary>
    public class SerialPortHelper : ICommunication
    {
        #region Fields

        private SerialPort _serialPort;
        private Thread _receiveThread;
        private bool _isRunning;
        private readonly object _lockObject = new object();
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>
        /// 포트 이름 (예: COM1)
        /// </summary>
        public string PortName { get; set; } = "COM1";

        /// <summary>
        /// 통신 속도 (Baud Rate)
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// 데이터 비트
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 스톱 비트
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>
        /// 패리티
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>
        /// 핸드쉐이크
        /// </summary>
        public Handshake Handshake { get; set; } = Handshake.None;

        /// <summary>
        /// 읽기 타임아웃 (밀리초)
        /// </summary>
        public int ReadTimeout { get; set; } = 3000;

        /// <summary>
        /// 쓰기 타임아웃 (밀리초)
        /// </summary>
        public int WriteTimeout { get; set; } = 3000;

        /// <summary>
        /// 수신 버퍼 크기
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 4096;

        /// <summary>
        /// 비동기 수신 모드 사용 여부
        /// </summary>
        public bool UseAsyncReceive { get; set; } = false;

        /// <summary>
        /// DTR (Data Terminal Ready) 활성화
        /// </summary>
        public bool DtrEnable { get; set; } = false;

        /// <summary>
        /// RTS (Request To Send) 활성화
        /// </summary>
        public bool RtsEnable { get; set; } = false;

        /// <summary>
        /// 연결 상태
        /// </summary>
        public bool IsConnected
        {
            get
            {
                try
                {
                    return _serialPort != null && _serialPort.IsOpen;
                }
                catch
                {
                    return false;
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<CommunicationErrorEventArgs> ErrorOccurred;

        #endregion

        #region Constructor

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public SerialPortHelper()
        {
        }

        /// <summary>
        /// 포트 설정을 지정하는 생성자
        /// </summary>
        public SerialPortHelper(string portName, int baudRate = 9600, Parity parity = Parity.None, 
            int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// 사용 가능한 시리얼 포트 목록
        /// </summary>
        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// 시리얼 포트 연결 (열기)
        /// </summary>
        public bool Connect()
        {
            try
            {
                lock (_lockObject)
                {
                    if (IsConnected)
                        return true;

                    _serialPort = new SerialPort
                    {
                        PortName = PortName,
                        BaudRate = BaudRate,
                        DataBits = DataBits,
                        StopBits = StopBits,
                        Parity = Parity,
                        Handshake = Handshake,
                        ReadTimeout = ReadTimeout,
                        WriteTimeout = WriteTimeout,
                        ReadBufferSize = ReceiveBufferSize,
                        DtrEnable = DtrEnable,
                        RtsEnable = RtsEnable
                    };

                    _serialPort.Open();

                    if (UseAsyncReceive)
                    {
                        StartReceiveThread();
                    }

                    OnConnectionStateChanged(true, $"Port {PortName} opened");
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                OnConnectionStateChanged(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 시리얼 포트 연결 해제 (닫기)
        /// </summary>
        public void Disconnect()
        {
            lock (_lockObject)
            {
                _isRunning = false;

                if (_receiveThread != null && _receiveThread.IsAlive)
                {
                    _receiveThread.Join(1000);
                    _receiveThread = null;
                }

                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();

                    _serialPort.Dispose();
                    _serialPort = null;
                }

                OnConnectionStateChanged(false, "Port closed");
            }
        }

        #endregion

        #region Send/Receive

        /// <summary>
        /// 바이트 배열 전송
        /// </summary>
        public bool Send(byte[] data)
        {
            try
            {
                if (!IsConnected)
                    return false;

                lock (_lockObject)
                {
                    _serialPort.Write(data, 0, data.Length);
                }
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return false;
            }
        }

        /// <summary>
        /// 문자열 전송
        /// </summary>
        public bool Send(string text)
        {
            try
            {
                if (!IsConnected)
                    return false;

                lock (_lockObject)
                {
                    _serialPort.Write(text);
                }
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return false;
            }
        }

        /// <summary>
        /// 문자열 전송 (라인 종료 포함)
        /// </summary>
        public bool SendLine(string text)
        {
            try
            {
                if (!IsConnected)
                    return false;

                lock (_lockObject)
                {
                    _serialPort.WriteLine(text);
                }
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return false;
            }
        }

        /// <summary>
        /// 데이터 전송 후 응답 수신
        /// </summary>
        public byte[] SendAndReceive(byte[] data, int timeout = 3000)
        {
            try
            {
                if (!IsConnected)
                    return null;

                lock (_lockObject)
                {
                    // 수신 버퍼 클리어
                    _serialPort.DiscardInBuffer();

                    // 데이터 전송
                    _serialPort.Write(data, 0, data.Length);

                    // 응답 대기
                    _serialPort.ReadTimeout = timeout;
                    Thread.Sleep(50); // 응답 대기 시간

                    // 응답 수신
                    int bytesToRead = _serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        byte[] buffer = new byte[bytesToRead];
                        _serialPort.Read(buffer, 0, bytesToRead);
                        return buffer;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return null;
            }
        }

        /// <summary>
        /// 데이터 수신 (동기)
        /// </summary>
        public byte[] Receive(int timeout = 3000)
        {
            try
            {
                if (!IsConnected)
                    return null;

                lock (_lockObject)
                {
                    _serialPort.ReadTimeout = timeout;

                    // 데이터 대기
                    int elapsed = 0;
                    while (_serialPort.BytesToRead == 0 && elapsed < timeout)
                    {
                        Thread.Sleep(10);
                        elapsed += 10;
                    }

                    int bytesToRead = _serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        byte[] buffer = new byte[bytesToRead];
                        _serialPort.Read(buffer, 0, bytesToRead);
                        return buffer;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return null;
            }
        }

        /// <summary>
        /// 지정된 길이만큼 데이터 수신
        /// </summary>
        public byte[] ReceiveExact(int length, int timeout = 3000)
        {
            try
            {
                if (!IsConnected)
                    return null;

                lock (_lockObject)
                {
                    _serialPort.ReadTimeout = timeout;
                    byte[] buffer = new byte[length];
                    int totalRead = 0;
                    int startTime = Environment.TickCount;

                    while (totalRead < length)
                    {
                        if (Environment.TickCount - startTime > timeout)
                            break;

                        if (_serialPort.BytesToRead > 0)
                        {
                            int bytesToRead = Math.Min(_serialPort.BytesToRead, length - totalRead);
                            int bytesRead = _serialPort.Read(buffer, totalRead, bytesToRead);
                            totalRead += bytesRead;
                        }
                        else
                        {
                            Thread.Sleep(10);
                        }
                    }

                    if (totalRead == length)
                        return buffer;

                    byte[] result = new byte[totalRead];
                    Array.Copy(buffer, result, totalRead);
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return null;
            }
        }

        /// <summary>
        /// 한 줄 읽기
        /// </summary>
        public string ReadLine(int timeout = 3000)
        {
            try
            {
                if (!IsConnected)
                    return null;

                lock (_lockObject)
                {
                    _serialPort.ReadTimeout = timeout;
                    return _serialPort.ReadLine();
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return null;
            }
        }

        /// <summary>
        /// 수신 버퍼 클리어
        /// </summary>
        public void ClearReceiveBuffer()
        {
            if (IsConnected)
            {
                lock (_lockObject)
                {
                    _serialPort.DiscardInBuffer();
                }
            }
        }

        /// <summary>
        /// 송신 버퍼 클리어
        /// </summary>
        public void ClearSendBuffer()
        {
            if (IsConnected)
            {
                lock (_lockObject)
                {
                    _serialPort.DiscardOutBuffer();
                }
            }
        }

        #endregion

        #region Async Receive Thread

        private void StartReceiveThread()
        {
            _isRunning = true;
            _receiveThread = new Thread(ReceiveThreadProc)
            {
                IsBackground = true,
                Name = "SerialPortHelper_ReceiveThread"
            };
            _receiveThread.Start();
        }

        private void ReceiveThreadProc()
        {
            while (_isRunning)
            {
                try
                {
                    if (!IsConnected)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    if (_serialPort.BytesToRead > 0)
                    {
                        int bytesToRead = _serialPort.BytesToRead;
                        byte[] buffer = new byte[bytesToRead];
                        _serialPort.Read(buffer, 0, bytesToRead);
                        OnDataReceived(buffer);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        OnErrorOccurred(ex);
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        protected virtual void OnConnectionStateChanged(bool isConnected, string message)
        {
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(isConnected, message));
        }

        protected virtual void OnDataReceived(byte[] data)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
        }

        protected virtual void OnErrorOccurred(Exception ex, string message = "")
        {
            ErrorOccurred?.Invoke(this, new CommunicationErrorEventArgs(ex, message));
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

        ~SerialPortHelper()
        {
            Dispose(false);
        }

        #endregion
    }
}
