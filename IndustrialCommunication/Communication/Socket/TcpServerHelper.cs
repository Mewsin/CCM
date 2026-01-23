using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using IndustrialCommunication.Communication.Interfaces;

namespace IndustrialCommunication.Communication.Socket
{
    /// <summary>
    /// 클라이언트 연결 이벤트 인자
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; }
        public string ClientEndPoint { get; }

        public ClientConnectedEventArgs(string clientId, string clientEndPoint)
        {
            ClientId = clientId;
            ClientEndPoint = clientEndPoint;
        }
    }

    /// <summary>
    /// 클라이언트 연결 해제 이벤트 인자
    /// </summary>
    public class ClientDisconnectedEventArgs : EventArgs
    {
        public string ClientId { get; }
        public string Reason { get; }

        public ClientDisconnectedEventArgs(string clientId, string reason = "")
        {
            ClientId = clientId;
            Reason = reason;
        }
    }

    /// <summary>
    /// 클라이언트 데이터 수신 이벤트 인자
    /// </summary>
    public class ClientDataReceivedEventArgs : EventArgs
    {
        public string ClientId { get; }
        public byte[] Data { get; }
        public DateTime ReceivedTime { get; }

        public ClientDataReceivedEventArgs(string clientId, byte[] data)
        {
            ClientId = clientId;
            Data = data;
            ReceivedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// TCP 서버 통신 헬퍼 클래스
    /// </summary>
    public class TcpServerHelper : IDisposable
    {
        #region Fields

        private TcpListener _listener;
        private Thread _acceptThread;
        private bool _isRunning;
        private readonly object _lockObject = new object();
        private bool _disposed;
        private readonly ConcurrentDictionary<string, TcpClient> _clients = new ConcurrentDictionary<string, TcpClient>();
        private readonly ConcurrentDictionary<string, Thread> _clientThreads = new ConcurrentDictionary<string, Thread>();

        #endregion

        #region Properties

        /// <summary>
        /// 서버 리스닝 IP 주소 (기본값: Any)
        /// </summary>
        public string ListenIp { get; set; } = "0.0.0.0";

        /// <summary>
        /// 서버 리스닝 포트
        /// </summary>
        public int ListenPort { get; set; }

        /// <summary>
        /// 수신 버퍼 크기
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 4096;

        /// <summary>
        /// 최대 클라이언트 수 (0 = 무제한)
        /// </summary>
        public int MaxClients { get; set; } = 0;

        /// <summary>
        /// 서버 실행 상태
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 현재 연결된 클라이언트 수
        /// </summary>
        public int ClientCount => _clients.Count;

        #endregion

        #region Events

        /// <summary>
        /// 서버 시작/중지 이벤트
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ServerStateChanged;

        /// <summary>
        /// 클라이언트 연결 이벤트
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// 클라이언트 연결 해제 이벤트
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

        /// <summary>
        /// 클라이언트 데이터 수신 이벤트
        /// </summary>
        public event EventHandler<ClientDataReceivedEventArgs> ClientDataReceived;

        /// <summary>
        /// 에러 발생 이벤트
        /// </summary>
        public event EventHandler<CommunicationErrorEventArgs> ErrorOccurred;

        #endregion

        #region Constructor

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public TcpServerHelper()
        {
        }

        /// <summary>
        /// 포트를 지정하는 생성자
        /// </summary>
        public TcpServerHelper(int listenPort)
        {
            ListenPort = listenPort;
        }

        /// <summary>
        /// IP와 포트를 지정하는 생성자
        /// </summary>
        public TcpServerHelper(string listenIp, int listenPort)
        {
            ListenIp = listenIp;
            ListenPort = listenPort;
        }

        #endregion

        #region Server Control

        /// <summary>
        /// 서버 시작
        /// </summary>
        public bool Start()
        {
            try
            {
                lock (_lockObject)
                {
                    if (_isRunning)
                        return true;

                    IPAddress ipAddress = ListenIp == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(ListenIp);
                    _listener = new TcpListener(ipAddress, ListenPort);
                    _listener.Start();

                    _isRunning = true;

                    _acceptThread = new Thread(AcceptThreadProc)
                    {
                        IsBackground = true,
                        Name = "TcpServerHelper_AcceptThread"
                    };
                    _acceptThread.Start();

                    OnServerStateChanged(true, $"Server started on {ListenIp}:{ListenPort}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                OnServerStateChanged(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 서버 중지
        /// </summary>
        public void Stop()
        {
            lock (_lockObject)
            {
                _isRunning = false;

                // 모든 클라이언트 연결 해제
                foreach (var clientId in _clients.Keys)
                {
                    DisconnectClient(clientId);
                }

                // Accept 스레드 종료 대기
                if (_acceptThread != null && _acceptThread.IsAlive)
                {
                    _acceptThread.Join(1000);
                    _acceptThread = null;
                }

                // 리스너 종료
                if (_listener != null)
                {
                    _listener.Stop();
                    _listener = null;
                }

                OnServerStateChanged(false, "Server stopped");
            }
        }

        #endregion

        #region Client Management

        /// <summary>
        /// 특정 클라이언트 연결 해제
        /// </summary>
        public void DisconnectClient(string clientId)
        {
            if (_clients.TryRemove(clientId, out TcpClient client))
            {
                try
                {
                    client.Close();
                    client.Dispose();
                }
                catch { }

                OnClientDisconnected(clientId, "Disconnected by server");
            }

            if (_clientThreads.TryRemove(clientId, out Thread thread))
            {
                try
                {
                    if (thread.IsAlive)
                        thread.Join(500);
                }
                catch { }
            }
        }

        /// <summary>
        /// 모든 클라이언트에게 데이터 전송 (브로드캐스트)
        /// </summary>
        public void SendToAll(byte[] data)
        {
            foreach (var clientId in _clients.Keys)
            {
                SendTo(clientId, data);
            }
        }

        /// <summary>
        /// 특정 클라이언트에게 데이터 전송
        /// </summary>
        public bool SendTo(string clientId, byte[] data)
        {
            if (!_clients.TryGetValue(clientId, out TcpClient client))
                return false;

            try
            {
                if (!client.Connected)
                {
                    DisconnectClient(clientId);
                    return false;
                }

                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);
                stream.Flush();
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex, $"Send error to client {clientId}");
                DisconnectClient(clientId);
                return false;
            }
        }

        /// <summary>
        /// 연결된 클라이언트 ID 목록 조회
        /// </summary>
        public string[] GetConnectedClients()
        {
            return _clients.Keys.ToArray();
        }

        /// <summary>
        /// 클라이언트 연결 상태 확인
        /// </summary>
        public bool IsClientConnected(string clientId)
        {
            if (_clients.TryGetValue(clientId, out TcpClient client))
            {
                return client.Connected;
            }
            return false;
        }

        #endregion

        #region Accept Thread

        private void AcceptThreadProc()
        {
            while (_isRunning)
            {
                try
                {
                    if (!_listener.Pending())
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    TcpClient client = _listener.AcceptTcpClient();

                    // 최대 클라이언트 수 체크
                    if (MaxClients > 0 && _clients.Count >= MaxClients)
                    {
                        client.Close();
                        continue;
                    }

                    string clientId = Guid.NewGuid().ToString("N").Substring(0, 8);
                    string clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

                    if (_clients.TryAdd(clientId, client))
                    {
                        // 클라이언트 수신 스레드 시작
                        Thread clientThread = new Thread(() => ClientReceiveThreadProc(clientId))
                        {
                            IsBackground = true,
                            Name = $"TcpServerHelper_Client_{clientId}"
                        };
                        _clientThreads.TryAdd(clientId, clientThread);
                        clientThread.Start();

                        OnClientConnected(clientId, clientEndPoint);
                    }
                }
                catch (SocketException)
                {
                    // 서버 중지 시 발생 가능
                    if (!_isRunning) break;
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

        private void ClientReceiveThreadProc(string clientId)
        {
            if (!_clients.TryGetValue(clientId, out TcpClient client))
                return;

            byte[] buffer = new byte[ReceiveBufferSize];
            NetworkStream stream = null;

            try
            {
                stream = client.GetStream();
            }
            catch
            {
                DisconnectClient(clientId);
                return;
            }

            while (_isRunning && client.Connected)
            {
                try
                {
                    if (!stream.DataAvailable)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // 클라이언트가 연결을 끊음
                        break;
                    }

                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    OnClientDataReceived(clientId, data);
                }
                catch (Exception)
                {
                    break;
                }
            }

            // 정리
            if (_clients.ContainsKey(clientId))
            {
                DisconnectClient(clientId);
            }
        }

        #endregion

        #region Event Handlers

        protected virtual void OnServerStateChanged(bool isRunning, string message)
        {
            ServerStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(isRunning, message));
        }

        protected virtual void OnClientConnected(string clientId, string clientEndPoint)
        {
            ClientConnected?.Invoke(this, new ClientConnectedEventArgs(clientId, clientEndPoint));
        }

        protected virtual void OnClientDisconnected(string clientId, string reason)
        {
            ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(clientId, reason));
        }

        protected virtual void OnClientDataReceived(string clientId, byte[] data)
        {
            ClientDataReceived?.Invoke(this, new ClientDataReceivedEventArgs(clientId, data));
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
                Stop();
            }

            _disposed = true;
        }

        ~TcpServerHelper()
        {
            Dispose(false);
        }

        #endregion
    }
}
