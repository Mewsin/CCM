using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using IndustrialCommunication.Communication.Interfaces;

namespace IndustrialCommunication.Communication.Socket
{
    /// <summary>
    /// UDP 통신 헬퍼 클래스
    /// </summary>
    public class UdpHelper : ICommunication
    {
        #region Fields

        private UdpClient _client;
        private Thread _receiveThread;
        private bool _isRunning;
        private IPEndPoint _remoteEndPoint;
        private readonly object _lockObject = new object();
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>
        /// 대상 IP 주소
        /// </summary>
        public string RemoteIp { get; set; }

        /// <summary>
        /// 대상 포트
        /// </summary>
        public int RemotePort { get; set; }

        /// <summary>
        /// 로컬 포트 (수신용)
        /// </summary>
        public int LocalPort { get; set; }

        /// <summary>
        /// 수신 타임아웃 (밀리초)
        /// </summary>
        public int ReceiveTimeout { get; set; } = 3000;

        /// <summary>
        /// 비동기 수신 모드 사용 여부
        /// </summary>
        public bool UseAsyncReceive { get; set; } = false;

        /// <summary>
        /// 연결(바인딩) 상태
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return _client != null;
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
        public UdpHelper()
        {
        }

        /// <summary>
        /// 원격 IP, 원격 포트, 로컬 포트를 지정하는 생성자
        /// </summary>
        public UdpHelper(string remoteIp, int remotePort, int localPort = 0)
        {
            RemoteIp = remoteIp;
            RemotePort = remotePort;
            LocalPort = localPort;
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// UDP 클라이언트 초기화 (연결)
        /// </summary>
        public bool Connect()
        {
            try
            {
                lock (_lockObject)
                {
                    if (_client != null)
                        return true;

                    // 로컬 포트가 지정된 경우 해당 포트로 바인딩
                    if (LocalPort > 0)
                    {
                        _client = new UdpClient(LocalPort);
                    }
                    else
                    {
                        _client = new UdpClient();
                    }

                    _client.Client.ReceiveTimeout = ReceiveTimeout;
                    _remoteEndPoint = new IPEndPoint(IPAddress.Parse(RemoteIp), RemotePort);

                    if (UseAsyncReceive)
                    {
                        StartReceiveThread();
                    }

                    OnConnectionStateChanged(true, "UDP client initialized");
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
        /// UDP 클라이언트 종료
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

                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }

                OnConnectionStateChanged(false, "UDP client closed");
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
                if (_client == null)
                    return false;

                lock (_lockObject)
                {
                    _client.Send(data, data.Length, _remoteEndPoint);
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
        /// 특정 대상에 데이터 전송
        /// </summary>
        public bool Send(byte[] data, string ip, int port)
        {
            try
            {
                if (_client == null)
                    return false;

                var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                lock (_lockObject)
                {
                    _client.Send(data, data.Length, endPoint);
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
                if (_client == null)
                    return null;

                lock (_lockObject)
                {
                    _client.Client.ReceiveTimeout = timeout;

                    // 데이터 전송
                    _client.Send(data, data.Length, _remoteEndPoint);

                    // 응답 수신
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] receivedData = _client.Receive(ref remoteEP);
                    return receivedData;
                }
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
                if (_client == null)
                    return null;

                lock (_lockObject)
                {
                    _client.Client.ReceiveTimeout = timeout;
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    return _client.Receive(ref remoteEP);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return null;
            }
        }

        /// <summary>
        /// 데이터 수신 (발신자 정보 포함)
        /// </summary>
        public byte[] Receive(out IPEndPoint sender, int timeout = 3000)
        {
            sender = null;
            try
            {
                if (_client == null)
                    return null;

                lock (_lockObject)
                {
                    _client.Client.ReceiveTimeout = timeout;
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _client.Receive(ref remoteEP);
                    sender = remoteEP;
                    return data;
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
                Name = "UdpHelper_ReceiveThread"
            };
            _receiveThread.Start();
        }

        private void ReceiveThreadProc()
        {
            while (_isRunning)
            {
                try
                {
                    if (_client == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    if (_client.Available > 0)
                    {
                        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = _client.Receive(ref remoteEP);
                        if (data != null && data.Length > 0)
                        {
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

        #region Broadcast

        /// <summary>
        /// 브로드캐스트 전송
        /// </summary>
        public bool Broadcast(byte[] data, int port)
        {
            try
            {
                if (_client == null)
                    return false;

                _client.EnableBroadcast = true;
                var broadcastEP = new IPEndPoint(IPAddress.Broadcast, port);

                lock (_lockObject)
                {
                    _client.Send(data, data.Length, broadcastEP);
                }
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return false;
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

        ~UdpHelper()
        {
            Dispose(false);
        }

        #endregion
    }
}
