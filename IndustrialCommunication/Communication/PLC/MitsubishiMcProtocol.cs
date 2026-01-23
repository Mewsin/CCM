using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using IndustrialCommunication.Communication.Interfaces;

namespace IndustrialCommunication.Communication.PLC
{
    /// <summary>
    /// Mitsubishi PLC MC Protocol (3E Frame) 통신 클래스
    /// </summary>
    public class MitsubishiMcProtocol : PlcBase
    {
        #region Constants

        // 3E Frame 요청 서브헤더
        private const byte SUBHEADER_REQUEST_H = 0x50;
        private const byte SUBHEADER_REQUEST_L = 0x00;

        // 명령 코드
        private const ushort CMD_BATCH_READ = 0x0401;
        private const ushort CMD_BATCH_WRITE = 0x1401;

        // 서브 명령 (워드 단위)
        private const ushort SUBCMD_WORD = 0x0000;
        // 서브 명령 (비트 단위)
        private const ushort SUBCMD_BIT = 0x0001;

        #endregion

        #region Device Codes

        /// <summary>
        /// 디바이스 코드 테이블
        /// </summary>
        private static readonly Dictionary<string, byte> DeviceCodes = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            { "D", 0xA8 },   // 데이터 레지스터
            { "M", 0x90 },   // 내부 릴레이
            { "X", 0x9C },   // 입력
            { "Y", 0x9D },   // 출력
            { "B", 0xA0 },   // 링크 릴레이
            { "W", 0xB4 },   // 링크 레지스터
            { "L", 0x92 },   // 래치 릴레이
            { "F", 0x93 },   // 어넌시에이터
            { "V", 0x94 },   // 엣지 릴레이
            { "S", 0x98 },   // 스텝 릴레이
            { "SD", 0xA9 },  // 특수 레지스터
            { "SM", 0x91 },  // 특수 릴레이
            { "R", 0xAF },   // 파일 레지스터
            { "ZR", 0xB0 },  // 파일 레지스터 (확장)
            { "T", 0xC1 },   // 타이머 (현재값)
            { "TS", 0xC1 },  // 타이머 (접점)
            { "TC", 0xC0 },  // 타이머 (코일)
            { "C", 0xC4 },   // 카운터 (현재값)
            { "CS", 0xC4 },  // 카운터 (접점)
            { "CC", 0xC3 },  // 카운터 (코일)
        };

        /// <summary>
        /// 비트 디바이스 목록
        /// </summary>
        private static readonly HashSet<string> BitDevices = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "M", "X", "Y", "B", "L", "F", "V", "S", "SM", "TS", "TC", "CS", "CC"
        };

        #endregion

        #region Fields

        private TcpClient _client;
        private NetworkStream _stream;

        #endregion

        #region Properties

        /// <summary>
        /// 네트워크 번호
        /// </summary>
        public byte NetworkNo { get; set; } = 0x00;

        /// <summary>
        /// PC 번호
        /// </summary>
        public byte PcNo { get; set; } = 0xFF;

        /// <summary>
        /// 모듈 I/O 번호
        /// </summary>
        public ushort ModuleIoNo { get; set; } = 0x03FF;

        /// <summary>
        /// 국 번호
        /// </summary>
        public byte StationNo { get; set; } = 0x00;

        /// <summary>
        /// 모니터링 타이머 (250ms 단위)
        /// </summary>
        public ushort MonitoringTimer { get; set; } = 0x0010; // 4초

        public override bool IsConnected => _client != null && _client.Connected;

        public override string PlcModel => "Mitsubishi MELSEC (MC Protocol 3E)";

        #endregion

        #region Constructor

        public MitsubishiMcProtocol()
        {
            Port = 5001; // MC Protocol 기본 포트
        }

        public MitsubishiMcProtocol(string ipAddress, int port = 5001)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        #endregion

        #region Connection

        public override bool Connect()
        {
            try
            {
                lock (_lockObject)
                {
                    if (IsConnected) return true;

                    _client = new TcpClient();
                    var result = _client.BeginConnect(IpAddress, Port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(ConnectTimeout);

                    if (!success)
                    {
                        _client.Close();
                        _client = null;
                        OnConnectionStateChanged(false, "Connection timeout");
                        return false;
                    }

                    _client.EndConnect(result);
                    _client.ReceiveTimeout = ReceiveTimeout;
                    _client.SendTimeout = ReceiveTimeout;
                    _stream = _client.GetStream();

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

        public override void Disconnect()
        {
            lock (_lockObject)
            {
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

        public override bool Send(byte[] data)
        {
            try
            {
                if (!IsConnected || _stream == null) return false;
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
                return false;
            }
        }

        public override byte[] SendAndReceive(byte[] data, int timeout = 3000)
        {
            try
            {
                if (!IsConnected || _stream == null) return null;

                lock (_lockObject)
                {
                    _stream.ReadTimeout = timeout;

                    // 송신
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();

                    // 응답 헤더 수신 (11바이트: 서브헤더 + 액세스 경로 + 데이터 길이)
                    byte[] header = new byte[11];
                    int headerRead = 0;
                    while (headerRead < 11)
                    {
                        int read = _stream.Read(header, headerRead, 11 - headerRead);
                        if (read == 0) break;
                        headerRead += read;
                    }

                    if (headerRead < 11) return null;

                    // 데이터 길이 추출 (리틀 엔디안)
                    int dataLength = header[7] | (header[8] << 8);

                    // 응답 데이터 수신
                    byte[] responseData = new byte[dataLength];
                    int dataRead = 0;
                    while (dataRead < dataLength)
                    {
                        int read = _stream.Read(responseData, dataRead, dataLength - dataRead);
                        if (read == 0) break;
                        dataRead += read;
                    }

                    // 전체 응답 조합
                    byte[] fullResponse = new byte[11 + dataLength];
                    Array.Copy(header, 0, fullResponse, 0, 11);
                    Array.Copy(responseData, 0, fullResponse, 11, dataLength);

                    return fullResponse;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return null;
            }
        }

        #endregion

        #region Frame Building

        /// <summary>
        /// 3E 프레임 요청 생성
        /// </summary>
        private byte[] BuildRequest(ushort command, ushort subCommand, byte[] requestData)
        {
            // 요청 데이터 길이 = 모니터링 타이머(2) + 커맨드(2) + 서브커맨드(2) + 요청 데이터
            int requestDataLength = 2 + 2 + 2 + (requestData?.Length ?? 0);

            List<byte> frame = new List<byte>();

            // 서브헤더
            frame.Add(SUBHEADER_REQUEST_H);
            frame.Add(SUBHEADER_REQUEST_L);

            // 액세스 경로
            frame.Add(NetworkNo);
            frame.Add(PcNo);
            frame.Add((byte)(ModuleIoNo & 0xFF));
            frame.Add((byte)(ModuleIoNo >> 8));
            frame.Add(StationNo);

            // 요청 데이터 길이
            frame.Add((byte)(requestDataLength & 0xFF));
            frame.Add((byte)(requestDataLength >> 8));

            // 모니터링 타이머
            frame.Add((byte)(MonitoringTimer & 0xFF));
            frame.Add((byte)(MonitoringTimer >> 8));

            // 커맨드
            frame.Add((byte)(command & 0xFF));
            frame.Add((byte)(command >> 8));

            // 서브 커맨드
            frame.Add((byte)(subCommand & 0xFF));
            frame.Add((byte)(subCommand >> 8));

            // 요청 데이터
            if (requestData != null && requestData.Length > 0)
                frame.AddRange(requestData);

            return frame.ToArray();
        }

        /// <summary>
        /// 디바이스 주소 데이터 생성
        /// </summary>
        private byte[] BuildDeviceAddress(string device, int address, int points)
        {
            if (!DeviceCodes.TryGetValue(device, out byte deviceCode))
                throw new ArgumentException($"Unknown device: {device}");

            List<byte> data = new List<byte>();

            // 디바이스 번호 (3바이트, 리틀 엔디안)
            data.Add((byte)(address & 0xFF));
            data.Add((byte)((address >> 8) & 0xFF));
            data.Add((byte)((address >> 16) & 0xFF));

            // 디바이스 코드
            data.Add(deviceCode);

            // 점수
            data.Add((byte)(points & 0xFF));
            data.Add((byte)(points >> 8));

            return data.ToArray();
        }

        /// <summary>
        /// 응답 에러 코드 확인
        /// </summary>
        private PlcResult CheckResponse(byte[] response)
        {
            if (response == null || response.Length < 13)
                return PlcResult.Fail("Invalid response");

            // End Code (응답 데이터의 처음 2바이트)
            ushort endCode = (ushort)(response[11] | (response[12] << 8));

            if (endCode != 0)
                return PlcResult.Fail($"PLC Error Code: 0x{endCode:X4}", endCode);

            return PlcResult.Success();
        }

        #endregion

        #region Bit Operations

        public override PlcResult<bool> ReadBit(string device, int address)
        {
            var result = ReadBits(device, address, 1);
            if (!result.IsSuccess)
                return PlcResult<bool>.Fail(result.ErrorMessage, result.ErrorCode);

            return PlcResult<bool>.Success(result.Value[0]);
        }

        public override PlcResult<bool[]> ReadBits(string device, int startAddress, int count)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult<bool[]>.Fail("Not connected");

                byte[] addressData = BuildDeviceAddress(device, startAddress, count);
                byte[] request = BuildRequest(CMD_BATCH_READ, SUBCMD_BIT, addressData);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                var checkResult = CheckResponse(response);
                if (!checkResult.IsSuccess)
                    return PlcResult<bool[]>.Fail(checkResult.ErrorMessage, checkResult.ErrorCode);

                // 비트 데이터 추출 (각 비트가 1바이트로 표현됨)
                bool[] values = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = response[13 + i] != 0;
                }

                return PlcResult<bool[]>.Success(values);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult<bool[]>.Fail(ex.Message);
            }
        }

        public override PlcResult WriteBit(string device, int address, bool value)
        {
            return WriteBits(device, address, new bool[] { value });
        }

        public override PlcResult WriteBits(string device, int startAddress, bool[] values)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult.Fail("Not connected");

                List<byte> requestData = new List<byte>();
                requestData.AddRange(BuildDeviceAddress(device, startAddress, values.Length));

                // 비트 데이터 추가 (각 비트가 1바이트)
                foreach (bool value in values)
                {
                    requestData.Add((byte)(value ? 0x01 : 0x00));
                }

                byte[] request = BuildRequest(CMD_BATCH_WRITE, SUBCMD_BIT, requestData.ToArray());
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                return CheckResponse(response);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        #endregion

        #region Word Operations

        public override PlcResult<short> ReadWord(string device, int address)
        {
            var result = ReadWords(device, address, 1);
            if (!result.IsSuccess)
                return PlcResult<short>.Fail(result.ErrorMessage, result.ErrorCode);

            return PlcResult<short>.Success(result.Value[0]);
        }

        public override PlcResult<short[]> ReadWords(string device, int startAddress, int count)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult<short[]>.Fail("Not connected");

                byte[] addressData = BuildDeviceAddress(device, startAddress, count);
                byte[] request = BuildRequest(CMD_BATCH_READ, SUBCMD_WORD, addressData);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                var checkResult = CheckResponse(response);
                if (!checkResult.IsSuccess)
                    return PlcResult<short[]>.Fail(checkResult.ErrorMessage, checkResult.ErrorCode);

                // 워드 데이터 추출
                byte[] data = new byte[count * 2];
                Array.Copy(response, 13, data, 0, count * 2);

                short[] values = BytesToShorts(data, false);
                return PlcResult<short[]>.Success(values);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult<short[]>.Fail(ex.Message);
            }
        }

        public override PlcResult WriteWord(string device, int address, short value)
        {
            return WriteWords(device, address, new short[] { value });
        }

        public override PlcResult WriteWords(string device, int startAddress, short[] values)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult.Fail("Not connected");

                List<byte> requestData = new List<byte>();
                requestData.AddRange(BuildDeviceAddress(device, startAddress, values.Length));
                requestData.AddRange(ShortsToBytes(values, false));

                byte[] request = BuildRequest(CMD_BATCH_WRITE, SUBCMD_WORD, requestData.ToArray());
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                return CheckResponse(response);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        #endregion

        #region DWord Operations

        public override PlcResult<int> ReadDWord(string device, int address)
        {
            var result = ReadDWords(device, address, 1);
            if (!result.IsSuccess)
                return PlcResult<int>.Fail(result.ErrorMessage, result.ErrorCode);

            return PlcResult<int>.Success(result.Value[0]);
        }

        public override PlcResult<int[]> ReadDWords(string device, int startAddress, int count)
        {
            try
            {
                // DWord는 2개의 Word를 읽어서 조합
                var result = ReadWords(device, startAddress, count * 2);
                if (!result.IsSuccess)
                    return PlcResult<int[]>.Fail(result.ErrorMessage, result.ErrorCode);

                int[] values = new int[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = (int)((ushort)result.Value[i * 2] | ((uint)(ushort)result.Value[i * 2 + 1] << 16));
                }

                return PlcResult<int[]>.Success(values);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult<int[]>.Fail(ex.Message);
            }
        }

        public override PlcResult WriteDWord(string device, int address, int value)
        {
            return WriteDWords(device, address, new int[] { value });
        }

        public override PlcResult WriteDWords(string device, int startAddress, int[] values)
        {
            try
            {
                // DWord를 2개의 Word로 분리
                short[] words = new short[values.Length * 2];
                for (int i = 0; i < values.Length; i++)
                {
                    words[i * 2] = (short)(values[i] & 0xFFFF);
                    words[i * 2 + 1] = (short)((values[i] >> 16) & 0xFFFF);
                }

                return WriteWords(device, startAddress, words);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        #endregion

        #region Real Operations

        public override PlcResult<float> ReadReal(string device, int address)
        {
            var result = ReadReals(device, address, 1);
            if (!result.IsSuccess)
                return PlcResult<float>.Fail(result.ErrorMessage, result.ErrorCode);

            return PlcResult<float>.Success(result.Value[0]);
        }

        public override PlcResult<float[]> ReadReals(string device, int startAddress, int count)
        {
            try
            {
                // Float는 2개의 Word를 읽어서 조합
                var result = ReadWords(device, startAddress, count * 2);
                if (!result.IsSuccess)
                    return PlcResult<float[]>.Fail(result.ErrorMessage, result.ErrorCode);

                float[] values = new float[count];
                for (int i = 0; i < count; i++)
                {
                    byte[] bytes = new byte[4];
                    bytes[0] = (byte)(result.Value[i * 2] & 0xFF);
                    bytes[1] = (byte)((result.Value[i * 2] >> 8) & 0xFF);
                    bytes[2] = (byte)(result.Value[i * 2 + 1] & 0xFF);
                    bytes[3] = (byte)((result.Value[i * 2 + 1] >> 8) & 0xFF);
                    values[i] = BitConverter.ToSingle(bytes, 0);
                }

                return PlcResult<float[]>.Success(values);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult<float[]>.Fail(ex.Message);
            }
        }

        public override PlcResult WriteReal(string device, int address, float value)
        {
            return WriteReals(device, address, new float[] { value });
        }

        public override PlcResult WriteReals(string device, int startAddress, float[] values)
        {
            try
            {
                // Float를 2개의 Word로 분리
                short[] words = new short[values.Length * 2];
                for (int i = 0; i < values.Length; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(values[i]);
                    words[i * 2] = (short)(bytes[0] | (bytes[1] << 8));
                    words[i * 2 + 1] = (short)(bytes[2] | (bytes[3] << 8));
                }

                return WriteWords(device, startAddress, words);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        #endregion

        #region String Operations

        public override PlcResult<string> ReadString(string device, int address, int length)
        {
            try
            {
                // 문자열은 워드 단위로 읽음 (2바이트씩)
                int wordCount = (length + 1) / 2;
                var result = ReadWords(device, address, wordCount);
                if (!result.IsSuccess)
                    return PlcResult<string>.Fail(result.ErrorMessage, result.ErrorCode);

                byte[] bytes = ShortsToBytes(result.Value, false);
                string value = BytesToString(bytes);

                if (value.Length > length)
                    value = value.Substring(0, length);

                return PlcResult<string>.Success(value);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult<string>.Fail(ex.Message);
            }
        }

        public override PlcResult WriteString(string device, int address, string value)
        {
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);

                // 짝수 길이로 맞춤
                int paddedLength = (bytes.Length + 1) / 2 * 2;
                byte[] paddedBytes = new byte[paddedLength];
                Array.Copy(bytes, paddedBytes, bytes.Length);

                short[] words = BytesToShorts(paddedBytes, false);
                return WriteWords(device, address, words);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        #endregion
    }
}
