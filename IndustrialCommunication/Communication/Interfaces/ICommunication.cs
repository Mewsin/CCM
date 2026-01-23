using System;

namespace IndustrialCommunication.Communication.Interfaces
{
    /// <summary>
    /// 통신 상태 변경 이벤트 인자
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Message { get; }

        public ConnectionStateChangedEventArgs(bool isConnected, string message = "")
        {
            IsConnected = isConnected;
            Message = message;
        }
    }

    /// <summary>
    /// 데이터 수신 이벤트 인자
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public DateTime ReceivedTime { get; }

        public DataReceivedEventArgs(byte[] data)
        {
            Data = data;
            ReceivedTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 에러 발생 이벤트 인자
    /// </summary>
    public class CommunicationErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Message { get; }

        public CommunicationErrorEventArgs(Exception ex, string message = "")
        {
            Exception = ex;
            Message = string.IsNullOrEmpty(message) ? ex.Message : message;
        }
    }

    /// <summary>
    /// 기본 통신 인터페이스
    /// </summary>
    public interface ICommunication : IDisposable
    {
        /// <summary>
        /// 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 연결 상태 변경 이벤트
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        /// 데이터 수신 이벤트
        /// </summary>
        event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        /// 에러 발생 이벤트
        /// </summary>
        event EventHandler<CommunicationErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// 연결
        /// </summary>
        bool Connect();

        /// <summary>
        /// 연결 해제
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 데이터 전송
        /// </summary>
        bool Send(byte[] data);

        /// <summary>
        /// 데이터 전송 후 응답 수신
        /// </summary>
        byte[] SendAndReceive(byte[] data, int timeout = 3000);
    }
}
