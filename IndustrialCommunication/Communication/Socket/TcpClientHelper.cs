using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using IndustrialCommunication.Communication.Interfaces;

namespace IndustrialCommunication.Communication.Socket
{
    /// <summary>
    /// TCP 클라이언트 통신 헬퍼 클래스
    /// </summary>
    public class TcpClientHelper : ICommunication
    {
        #region Fields

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isRunning;
        private readonly object _lockObject = new object();
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>
        /// 서버 IP 주소
        /// </summary>
        public string ServerIp { get; set; }

        /// <summary>
        /// 서버 포트
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// 연결 타임아웃 (밀리초)
        /// </summary>
        public int ConnectTimeout { get; set; } = 5000;

        /// <summary>
        /// 수신 타임아웃 (밀리초)
        /// </summary>
        public int ReceiveTimeout { get; set; } = 3000;

        /// <summary>
        /// 송신 타임아웃 (밀리초)
        /// </summary>
        public int SendTimeout { get; set; } = 3000;

        /// <summary>
        /// 수신 버퍼 크기
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 4096;

        /// <summary>
        /// 자동 재연결 사용 여부
        /// </summary>
        public bool AutoReconnect { get; set; } = false;

        /// <summary>
        /// 비동기 수신 모드 사용 여부
        /// </summary>
        public bool UseAsyncReceive { get; set; } = false;

        /// <summary>
        /// 연결 상태
        /// </summary>
        public bool IsConnected
        {
            get
            {
                try
                {
                    return _client != null && _client.Connected;
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
        public TcpClientHelper()
        {
        }

        /// <summary>
        /// IP와 포트를 지정하는 생성자
        /// </summary>
        public TcpClientHelper(string serverIp, int serverPort)
        {
            ServerIp = serverIp;
            ServerPort = serverPort;
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// 서버에 연결
        /// </summary>
        public bool Connect()
        {
            try
            {
                lock (_lockObject)
                {
                    if (IsConnected)
                        return true;

                    _client = new TcpClient();
                    _client.ReceiveTimeout = ReceiveTimeout;
                    _client.SendTimeout = SendTimeout;
                    _client.ReceiveBufferSize = ReceiveBufferSize;

                    // 타임아웃을 적용한 연결
                    var result = _client.BeginConnect(ServerIp, ServerPort, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(ConnectTimeout);

                    if (!success)
                    {
                        _client.Close();
                        _client = null;
                        OnConnectionStateChanged(false, "Connection timeout");
                        return false;
                    }

                    _client.EndConnect(result);
                    _stream = _client.GetStream();

                    if (UseAsyncReceive)
                    {
                        StartReceiveThread();
                    }

                    OnConnectionStateChanged(true, "Connected");
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
        /// 연결 해제
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

                if (_stream != null)
                {
                    _stream.Close();
                    _stream.Dispose();
                    _stream = null;
                }

                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }

                OnConnectionStateChanged(false, "Disconnected");
            }
        }

        #endregion

        #region Send/Receive

        /// <summary>
        /// 데이터 전송
        /// </summary>
        public bool Send(byte[] data)
        {
            try
            {
                if (!IsConnected || _stream == null)
                    return false;

                lock (_lockObject)
                {
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                }
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                if (AutoReconnect)
                {
                    Disconnect();
                    Connect();
                }
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
                if (!IsConnected || _stream == null)
                    return null;

                lock (_lockObject)
                {
                    // 수신 버퍼 클리어
                    if (_stream.DataAvailable)
                    {
                        byte[] dummy = new byte[ReceiveBufferSize];
                        while (_stream.DataAvailable)
                        {
                            _stream.Read(dummy, 0, dummy.Length);
                        }
                    }

                    // 데이터 전송
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();

                    // 응답 수신
                    _stream.ReadTimeout = timeout;
                    byte[] buffer = new byte[ReceiveBufferSize];
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        byte[] result = new byte[bytesRead];
                        Array.Copy(buffer, result, bytesRead);
                        return result;
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
                if (!IsConnected || _stream == null)
                    return null;

                lock (_lockObject)
                {
                    _stream.ReadTimeout = timeout;
                    byte[] buffer = new byte[ReceiveBufferSize];
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        byte[] result = new byte[bytesRead];
                        Array.Copy(buffer, result, bytesRead);
                        return result;
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
                if (!IsConnected || _stream == null)
                    return null;

                lock (_lockObject)
                {
                    _stream.ReadTimeout = timeout;
                    byte[] buffer = new byte[length];
                    int totalRead = 0;

                    while (totalRead < length)
                    {
                        int bytesRead = _stream.Read(buffer, totalRead, length - totalRead);
                        if (bytesRead == 0)
                            break;
                        totalRead += bytesRead;
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

        #endregion

        #region Async Receive Thread

        private void StartReceiveThread()
        {
            _isRunning = true;
            _receiveThread = new Thread(ReceiveThreadProc)
            {
                IsBackground = true,
                Name = "TcpClientHelper_ReceiveThread"
            };
            _receiveThread.Start();
        }

        private void ReceiveThreadProc()
        {
            byte[] buffer = new byte[ReceiveBufferSize];

            while (_isRunning)
            {
                try
                {
                    if (!IsConnected || _stream == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    if (_stream.DataAvailable)
                    {
                        int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            byte[] data = new byte[bytesRead];
                            Array.Copy(buffer, data, bytesRead);
                            OnDataReceived(data);
                        }
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

        ~TcpClientHelper()
        {
            Dispose(false);
        }

        #endregion
    }
}
