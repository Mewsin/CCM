using System;
using System.Text;
using CCM.Communication.Interfaces;

namespace CCM.Communication.PLC
{
    /// <summary>
    /// PLC 통신 기본 추상 클래스
    /// </summary>
    public abstract class PlcBase : IPlcCommunication
    {
        #region Fields

        protected bool _disposed;
        protected readonly object _lockObject = new object();

        #endregion

        #region Properties

        /// <summary>
        /// PLC IP 주소
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// PLC 포트
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 연결 타임아웃 (밀리초)
        /// </summary>
        public int ConnectTimeout { get; set; } = 5000;

        /// <summary>
        /// 수신 타임아웃 (밀리초)
        /// </summary>
        public int ReceiveTimeout { get; set; } = 3000;

        /// <summary>
        /// 32비트 값(DWord, Float)의 바이트 오더 모드
        /// </summary>
        public ByteOrderMode ByteOrder { get; set; } = ByteOrderMode.DCBA;

        /// <summary>
        /// 연결 상태
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// PLC 모델명
        /// </summary>
        public abstract string PlcModel { get; }

        #endregion

        #region Events

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<CommunicationErrorEventArgs> ErrorOccurred;

        #endregion

        #region Abstract Methods

        public abstract bool Connect();
        public abstract void Disconnect();
        public abstract bool Send(byte[] data);
        public abstract byte[] SendAndReceive(byte[] data, int timeout = 3000);

        #endregion

        #region IPlcCommunication Implementation

        // Bit Operations
        public abstract PlcResult<bool> ReadBit(string device, int address);
        public abstract PlcResult<bool[]> ReadBits(string device, int startAddress, int count);
        public abstract PlcResult WriteBit(string device, int address, bool value);
        public abstract PlcResult WriteBits(string device, int startAddress, bool[] values);

        // Word Operations
        public abstract PlcResult<short> ReadWord(string device, int address);
        public abstract PlcResult<short[]> ReadWords(string device, int startAddress, int count);
        public abstract PlcResult WriteWord(string device, int address, short value);
        public abstract PlcResult WriteWords(string device, int startAddress, short[] values);

        // DWord Operations
        public abstract PlcResult<int> ReadDWord(string device, int address);
        public abstract PlcResult<int[]> ReadDWords(string device, int startAddress, int count);
        public abstract PlcResult WriteDWord(string device, int address, int value);
        public abstract PlcResult WriteDWords(string device, int startAddress, int[] values);

        // Real Operations
        public abstract PlcResult<float> ReadReal(string device, int address);
        public abstract PlcResult<float[]> ReadReals(string device, int startAddress, int count);
        public abstract PlcResult WriteReal(string device, int address, float value);
        public abstract PlcResult WriteReals(string device, int startAddress, float[] values);

        // String Operations
        public abstract PlcResult<string> ReadString(string device, int address, int length);
        public abstract PlcResult WriteString(string device, int address, string value);

        #endregion

        #region Helper Methods

        /// <summary>
        /// 바이트 배열을 short 배열로 변환
        /// </summary>
        protected short[] BytesToShorts(byte[] bytes, bool bigEndian = false)
        {
            if (bytes == null || bytes.Length == 0)
                return new short[0];

            int count = bytes.Length / 2;
            short[] result = new short[count];

            for (int i = 0; i < count; i++)
            {
                if (bigEndian)
                    result[i] = (short)((bytes[i * 2] << 8) | bytes[i * 2 + 1]);
                else
                    result[i] = (short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
            }

            return result;
        }

        /// <summary>
        /// short 배열을 바이트 배열로 변환
        /// </summary>
        protected byte[] ShortsToBytes(short[] values, bool bigEndian = false)
        {
            if (values == null || values.Length == 0)
                return new byte[0];

            byte[] result = new byte[values.Length * 2];

            for (int i = 0; i < values.Length; i++)
            {
                if (bigEndian)
                {
                    result[i * 2] = (byte)(values[i] >> 8);
                    result[i * 2 + 1] = (byte)(values[i] & 0xFF);
                }
                else
                {
                    result[i * 2] = (byte)(values[i] & 0xFF);
                    result[i * 2 + 1] = (byte)(values[i] >> 8);
                }
            }

            return result;
        }

        /// <summary>
        /// 바이트 배열을 int 배열로 변환
        /// </summary>
        protected int[] BytesToInts(byte[] bytes, bool bigEndian = false)
        {
            if (bytes == null || bytes.Length == 0)
                return new int[0];

            int count = bytes.Length / 4;
            int[] result = new int[count];

            for (int i = 0; i < count; i++)
            {
                if (bigEndian)
                {
                    result[i] = (bytes[i * 4] << 24) | (bytes[i * 4 + 1] << 16) |
                                (bytes[i * 4 + 2] << 8) | bytes[i * 4 + 3];
                }
                else
                {
                    result[i] = bytes[i * 4] | (bytes[i * 4 + 1] << 8) |
                                (bytes[i * 4 + 2] << 16) | (bytes[i * 4 + 3] << 24);
                }
            }

            return result;
        }

        /// <summary>
        /// int 배열을 바이트 배열로 변환
        /// </summary>
        protected byte[] IntsToBytes(int[] values, bool bigEndian = false)
        {
            if (values == null || values.Length == 0)
                return new byte[0];

            byte[] result = new byte[values.Length * 4];

            for (int i = 0; i < values.Length; i++)
            {
                if (bigEndian)
                {
                    result[i * 4] = (byte)(values[i] >> 24);
                    result[i * 4 + 1] = (byte)(values[i] >> 16);
                    result[i * 4 + 2] = (byte)(values[i] >> 8);
                    result[i * 4 + 3] = (byte)(values[i] & 0xFF);
                }
                else
                {
                    result[i * 4] = (byte)(values[i] & 0xFF);
                    result[i * 4 + 1] = (byte)(values[i] >> 8);
                    result[i * 4 + 2] = (byte)(values[i] >> 16);
                    result[i * 4 + 3] = (byte)(values[i] >> 24);
                }
            }

            return result;
        }

        /// <summary>
        /// 바이트 배열을 float 배열로 변환
        /// </summary>
        protected float[] BytesToFloats(byte[] bytes, bool bigEndian = false)
        {
            if (bytes == null || bytes.Length == 0)
                return new float[0];

            int count = bytes.Length / 4;
            float[] result = new float[count];

            for (int i = 0; i < count; i++)
            {
                byte[] floatBytes = new byte[4];
                if (bigEndian)
                {
                    floatBytes[0] = bytes[i * 4 + 3];
                    floatBytes[1] = bytes[i * 4 + 2];
                    floatBytes[2] = bytes[i * 4 + 1];
                    floatBytes[3] = bytes[i * 4];
                }
                else
                {
                    Array.Copy(bytes, i * 4, floatBytes, 0, 4);
                }
                result[i] = BitConverter.ToSingle(floatBytes, 0);
            }

            return result;
        }

        /// <summary>
        /// float 배열을 바이트 배열로 변환
        /// </summary>
        protected byte[] FloatsToBytes(float[] values, bool bigEndian = false)
        {
            if (values == null || values.Length == 0)
                return new byte[0];

            byte[] result = new byte[values.Length * 4];

            for (int i = 0; i < values.Length; i++)
            {
                byte[] floatBytes = BitConverter.GetBytes(values[i]);
                if (bigEndian)
                {
                    result[i * 4] = floatBytes[3];
                    result[i * 4 + 1] = floatBytes[2];
                    result[i * 4 + 2] = floatBytes[1];
                    result[i * 4 + 3] = floatBytes[0];
                }
                else
                {
                    Array.Copy(floatBytes, 0, result, i * 4, 4);
                }
            }

            return result;
        }

        /// <summary>
        /// 바이트 배열을 문자열로 변환
        /// </summary>
        protected string BytesToString(byte[] bytes, Encoding encoding = null)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            encoding = encoding ?? Encoding.ASCII;
            string result = encoding.GetString(bytes);

            // NULL 문자 제거
            int nullIndex = result.IndexOf('\0');
            if (nullIndex >= 0)
                result = result.Substring(0, nullIndex);

            return result;
        }

        /// <summary>
        /// 문자열을 바이트 배열로 변환
        /// </summary>
        protected byte[] StringToBytes(string value, int length, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.ASCII;
            byte[] result = new byte[length];
            byte[] strBytes = encoding.GetBytes(value ?? string.Empty);
            int copyLength = Math.Min(strBytes.Length, length);
            Array.Copy(strBytes, result, copyLength);
            return result;
        }

        /// <summary>
        /// 2개의 워드를 32비트 정수로 변환 (ByteOrder 설정에 따름)
        /// </summary>
        protected int WordsToInt32(short word0, short word1)
        {
            ushort w0 = (ushort)word0;
            ushort w1 = (ushort)word1;

            switch (ByteOrder)
            {
                case ByteOrderMode.ABCD: // Big Endian: Word1이 상위
                    return (w1 << 16) | w0;
                case ByteOrderMode.DCBA: // Little Endian: Word0이 하위 (기본)
                    return (w1 << 16) | w0;
                case ByteOrderMode.BADC: // Byte Swap
                    w0 = SwapBytes(w0);
                    w1 = SwapBytes(w1);
                    return (w1 << 16) | w0;
                case ByteOrderMode.CDAB: // Word Swap
                    return (w0 << 16) | w1;
                default:
                    return (w1 << 16) | w0;
            }
        }

        /// <summary>
        /// 32비트 정수를 2개의 워드로 변환 (ByteOrder 설정에 따름)
        /// </summary>
        protected void Int32ToWords(int value, out short word0, out short word1)
        {
            ushort w0, w1;

            switch (ByteOrder)
            {
                case ByteOrderMode.ABCD: // Big Endian
                    w0 = (ushort)(value & 0xFFFF);
                    w1 = (ushort)((value >> 16) & 0xFFFF);
                    break;
                case ByteOrderMode.DCBA: // Little Endian (기본)
                    w0 = (ushort)(value & 0xFFFF);
                    w1 = (ushort)((value >> 16) & 0xFFFF);
                    break;
                case ByteOrderMode.BADC: // Byte Swap
                    w0 = SwapBytes((ushort)(value & 0xFFFF));
                    w1 = SwapBytes((ushort)((value >> 16) & 0xFFFF));
                    break;
                case ByteOrderMode.CDAB: // Word Swap
                    w0 = (ushort)((value >> 16) & 0xFFFF);
                    w1 = (ushort)(value & 0xFFFF);
                    break;
                default:
                    w0 = (ushort)(value & 0xFFFF);
                    w1 = (ushort)((value >> 16) & 0xFFFF);
                    break;
            }

            word0 = (short)w0;
            word1 = (short)w1;
        }

        /// <summary>
        /// 2개의 워드를 float으로 변환 (ByteOrder 설정에 따름)
        /// </summary>
        protected float WordsToFloat(short word0, short word1)
        {
            int intValue = WordsToInt32(word0, word1);
            byte[] bytes = BitConverter.GetBytes(intValue);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// float을 2개의 워드로 변환 (ByteOrder 설정에 따름)
        /// </summary>
        protected void FloatToWords(float value, out short word0, out short word1)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            int intValue = BitConverter.ToInt32(bytes, 0);
            Int32ToWords(intValue, out word0, out word1);
        }

        /// <summary>
        /// 워드 내 바이트 스왑
        /// </summary>
        protected ushort SwapBytes(ushort value)
        {
            return (ushort)((value >> 8) | (value << 8));
        }

        #endregion

        #region Event Handlers

        protected virtual void OnConnectionStateChanged(bool isConnected, string message = "")
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

        ~PlcBase()
        {
            Dispose(false);
        }

        #endregion
    }
}
