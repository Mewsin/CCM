using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using CCM.Communication.Interfaces;

namespace CCM.Communication.PLC
{
    /// <summary>
    /// LS Electric (LG) XGT PLC 통신 클래스 (FEnet - XGT Protocol)
    /// </summary>
    public class LsElectricXgt : PlcBase
    {
        #region Constants

        // XGT Protocol Header
        private const string HEADER = "LSIS-XGT";

        // 명령 코드
        private const ushort CMD_READ_REQUEST = 0x0054;      // 읽기 요청
        private const ushort CMD_READ_RESPONSE = 0x0055;     // 읽기 응답
        private const ushort CMD_WRITE_REQUEST = 0x0058;     // 쓰기 요청
        private const ushort CMD_WRITE_RESPONSE = 0x0059;    // 쓰기 응답

        // 데이터 타입
        private const ushort DATA_TYPE_BIT = 0x0000;
        private const ushort DATA_TYPE_BYTE = 0x0001;
        private const ushort DATA_TYPE_WORD = 0x0002;
        private const ushort DATA_TYPE_DWORD = 0x0003;
        private const ushort DATA_TYPE_LWORD = 0x0004;
        private const ushort DATA_TYPE_CONTINUOUS = 0x0014;  // 연속 읽기 (바이트)

        #endregion

        #region Fields

        private TcpClient _client;
        private NetworkStream _stream;
        private ushort _invokeId = 0;

        #endregion

        #region Properties

        /// <summary>
        /// PLC 정보 (CPU 타입에 따라 자동 설정)
        /// XGK: 0x33, XGI: 0x34, XGR: 0x35
        /// </summary>
        public byte PlcInfo { get; set; } = 0x33;

        /// <summary>
        /// CPU 타입
        /// </summary>
        public XgtCpuType CpuType
        {
            get => _cpuType;
            set
            {
                _cpuType = value;
                PlcInfo = GetPlcInfoByCpuType(value);
            }
        }
        private XgtCpuType _cpuType = XgtCpuType.XGK;

        /// <summary>
        /// CPU 정보
        /// </summary>
        public byte CpuInfo { get; set; } = 0x00;

        private static byte GetPlcInfoByCpuType(XgtCpuType cpuType)
        {
            switch (cpuType)
            {
                case XgtCpuType.XGK: return 0x33;
                case XgtCpuType.XGI: return 0x34;
                case XgtCpuType.XGR: return 0x35;
                case XgtCpuType.XGB: return 0x31;
                case XgtCpuType.XEC: return 0x32;
                default: return 0x33;
            }
        }

        public override bool IsConnected => _client != null && _client.Connected;

        public override string PlcModel => "LS Electric XGT (FEnet)";

        #endregion

        #region Constructor

        public LsElectricXgt()
        {
            Port = 2004; // FEnet TCP 기본 포트
            ByteOrder = ByteOrderMode.DCBA; // LS XGT는 Little Endian
        }

        public LsElectricXgt(string ipAddress, int port = 2004, XgtCpuType cpuType = XgtCpuType.XGK)
        {
            IpAddress = ipAddress;
            Port = port;
            CpuType = cpuType;
            ByteOrder = ByteOrderMode.DCBA; // LS XGT는 Little Endian
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

                    // 응답 헤더 수신 (20바이트 고정 헤더)
                    byte[] header = new byte[20];
                    int headerRead = 0;
                    while (headerRead < 20)
                    {
                        int read = _stream.Read(header, headerRead, 20 - headerRead);
                        if (read == 0) break;
                        headerRead += read;
                    }

                    if (headerRead < 20) return null;

                    // 데이터 길이 추출 (인덱스 16-17, 리틀 엔디안)
                    int dataLength = header[16] | (header[17] << 8);

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
                    byte[] fullResponse = new byte[20 + dataLength];
                    Array.Copy(header, 0, fullResponse, 0, 20);
                    Array.Copy(responseData, 0, fullResponse, 20, dataLength);

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
        /// XGT 프레임 요청 생성
        /// FEnet 프로토콜 헤더 구조 (20 bytes):
        /// - Company ID: 10 bytes (LSIS-XGT + padding)
        /// - Reserved: 2 bytes (PLC Info, CPU Info - 일부 구현에서 사용)
        /// - Reserved: 1 byte
        /// - Source of Frame: 1 byte (0x33: Client/HMI, 0x11: Server/PLC)
        /// - Invoke ID: 2 bytes
        /// - Data Length: 2 bytes
        /// - Reserved: 1 byte (FEnet Slot)
        /// - Checksum: 1 byte (헤더 체크섬)
        /// </summary>
        private byte[] BuildRequest(ushort command, byte[] applicationData)
        {
            List<byte> frame = new List<byte>();

            // Company ID (10 bytes) - "LSIS-XGT" + 패딩
            byte[] headerBytes = Encoding.ASCII.GetBytes(HEADER);
            frame.AddRange(headerBytes);
            // 10바이트로 패딩
            for (int i = headerBytes.Length; i < 10; i++)
                frame.Add(0x00);

            // PLC Info (1 byte) - CPU 타입에 따라 설정
            frame.Add(PlcInfo);

            // CPU Info (1 byte)
            frame.Add(CpuInfo);

            // Reserved (1 byte)
            frame.Add(0x00);

            // Source of Frame (1 byte) - 0x33: Client(HMI) -> PLC
            frame.Add(0x33);

            // Invoke ID (2 bytes, Little Endian)
            _invokeId++;
            frame.Add((byte)(_invokeId & 0xFF));
            frame.Add((byte)((_invokeId >> 8) & 0xFF));

            // Data Length (2 bytes, Little Endian)
            int dataLength = applicationData?.Length ?? 0;
            frame.Add((byte)(dataLength & 0xFF));
            frame.Add((byte)((dataLength >> 8) & 0xFF));

            // FEnet Slot (1 byte)
            frame.Add(0x00);

            // Checksum (1 byte) - 헤더 19바이트의 합계 % 256
            byte checksum = 0;
            for (int i = 0; i < frame.Count; i++)
                checksum += frame[i];
            frame.Add(checksum);

            // Application Data
            if (applicationData != null && applicationData.Length > 0)
                frame.AddRange(applicationData);

            return frame.ToArray();
        }

        /// <summary>
        /// 디바이스 주소 문자열 생성 (예: %MW100, %DW0)
        /// </summary>
        private string BuildDeviceAddress(string device, int address, PlcDeviceType type)
        {
            string typeChar;
            switch (type)
            {
                case PlcDeviceType.Bit:
                    typeChar = "X";
                    break;
                case PlcDeviceType.Word:
                    typeChar = "W";
                    break;
                case PlcDeviceType.DWord:
                    typeChar = "D";
                    break;
                default:
                    typeChar = "W";
                    break;
            }

            return $"%{device.ToUpper()}{typeChar}{address}";
        }

        /// <summary>
        /// 읽기 요청 애플리케이션 데이터 생성
        /// </summary>
        private byte[] BuildReadApplicationData(string deviceAddress, int count, ushort dataType)
        {
            List<byte> data = new List<byte>();

            // Command (2 bytes)
            data.Add((byte)(CMD_READ_REQUEST & 0xFF));
            data.Add((byte)((CMD_READ_REQUEST >> 8) & 0xFF));

            // Data Type (2 bytes)
            data.Add((byte)(dataType & 0xFF));
            data.Add((byte)((dataType >> 8) & 0xFF));

            // Reserved (2 bytes)
            data.Add(0x00);
            data.Add(0x00);

            // Block Count (2 bytes)
            data.Add(0x01);
            data.Add(0x00);

            // Variable Name Length (2 bytes)
            byte[] nameBytes = Encoding.ASCII.GetBytes(deviceAddress);
            data.Add((byte)(nameBytes.Length & 0xFF));
            data.Add((byte)((nameBytes.Length >> 8) & 0xFF));

            // Variable Name
            data.AddRange(nameBytes);

            // Data Count (2 bytes)
            data.Add((byte)(count & 0xFF));
            data.Add((byte)((count >> 8) & 0xFF));

            return data.ToArray();
        }

        /// <summary>
        /// 쓰기 요청 애플리케이션 데이터 생성
        /// </summary>
        private byte[] BuildWriteApplicationData(string deviceAddress, byte[] writeData, ushort dataType)
        {
            List<byte> data = new List<byte>();

            // Command (2 bytes)
            data.Add((byte)(CMD_WRITE_REQUEST & 0xFF));
            data.Add((byte)((CMD_WRITE_REQUEST >> 8) & 0xFF));

            // Data Type (2 bytes)
            data.Add((byte)(dataType & 0xFF));
            data.Add((byte)((dataType >> 8) & 0xFF));

            // Reserved (2 bytes)
            data.Add(0x00);
            data.Add(0x00);

            // Block Count (2 bytes)
            data.Add(0x01);
            data.Add(0x00);

            // Variable Name Length (2 bytes)
            byte[] nameBytes = Encoding.ASCII.GetBytes(deviceAddress);
            data.Add((byte)(nameBytes.Length & 0xFF));
            data.Add((byte)((nameBytes.Length >> 8) & 0xFF));

            // Variable Name
            data.AddRange(nameBytes);

            // Data Count (2 bytes)
            int count = writeData.Length;
            if (dataType == DATA_TYPE_WORD) count /= 2;
            else if (dataType == DATA_TYPE_DWORD) count /= 4;
            data.Add((byte)(count & 0xFF));
            data.Add((byte)((count >> 8) & 0xFF));

            // Write Data
            data.AddRange(writeData);

            return data.ToArray();
        }

        /// <summary>
        /// 응답 검증
        /// XGT 응답 구조 (정상 응답):
        /// - Header: 20 bytes (인덱스 0-19)
        /// - Command Echo: 2 bytes (인덱스 20-21) - 읽기=0x0055, 쓰기=0x0059
        /// - Data Type: 2 bytes (인덱스 22-23)
        /// - Reserved: 4 bytes (인덱스 24-27)
        /// - Block Count: 2 bytes (인덱스 28-29)
        /// - Data Count: 2 bytes (인덱스 30-31)
        /// - Data: 인덱스 32부터
        /// </summary>
        private PlcResult CheckResponse(byte[] response)
        {
            if (response == null || response.Length < 22)
                return PlcResult.Fail("Invalid response: too short");

            // Command Echo 확인 (인덱스 20-21)
            ushort commandEcho = (ushort)(response[20] | (response[21] << 8));
            
            // 읽기 응답(0x0055) 또는 쓰기 응답(0x0059)이어야 함
            if (commandEcho != CMD_READ_RESPONSE && commandEcho != CMD_WRITE_RESPONSE)
            {
                // 에러 응답일 수 있음 - NAK 코드 확인
                return PlcResult.Fail($"XGT Error/NAK: 0x{commandEcho:X4}", commandEcho);
            }

            return PlcResult.Success();
        }

        /// <summary>
        /// 응답에서 데이터 시작 오프셋 및 데이터 개수 추출
        /// XGT 응답 구조:
        /// Header(20) + Command(2) + DataType(2) + Reserved(4) + BlockCount(2) + DataCount(2) + Data
        /// 데이터 시작 오프셋 = 32
        /// </summary>
        private int CalculateDataOffset(byte[] response)
        {
            // 최소 헤더 + 명령 + 데이터타입 + 예약 + 블록수 + 데이터수 = 32 bytes
            if (response == null || response.Length < 32)
                return -1;
                
            return 32;
        }
        
        /// <summary>
        /// 응답에서 실제 데이터 바이트 수 추출 (인덱스 30-31)
        /// </summary>
        private int GetResponseDataCount(byte[] response)
        {
            if (response == null || response.Length < 32)
                return 0;
            return response[30] | (response[31] << 8);
        }

        #endregion

        #region IPlcCommunication Implementation

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

                string deviceAddress = BuildDeviceAddress(device, startAddress, PlcDeviceType.Bit);
                byte[] appData = BuildReadApplicationData(deviceAddress, count, DATA_TYPE_BIT);
                byte[] request = BuildRequest(CMD_READ_REQUEST, appData);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                var checkResult = CheckResponse(response);
                if (!checkResult.IsSuccess)
                    return PlcResult<bool[]>.Fail(checkResult.ErrorMessage, checkResult.ErrorCode);

                // 데이터 오프셋 동적 계산
                int dataOffset = CalculateDataOffset(response);
                if (dataOffset < 0 || dataOffset >= response.Length)
                    return PlcResult<bool[]>.Fail("Invalid response structure");

                bool[] values = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    if (dataOffset + i < response.Length)
                        values[i] = response[dataOffset + i] != 0;
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

                string deviceAddress = BuildDeviceAddress(device, startAddress, PlcDeviceType.Bit);
                byte[] writeData = new byte[values.Length];
                for (int i = 0; i < values.Length; i++)
                    writeData[i] = (byte)(values[i] ? 1 : 0);

                byte[] appData = BuildWriteApplicationData(deviceAddress, writeData, DATA_TYPE_BIT);
                byte[] request = BuildRequest(CMD_WRITE_REQUEST, appData);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                return CheckResponse(response);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

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

                // 여러 워드를 읽을 때는 개별적으로 읽어서 합침
                short[] values = new short[count];
                
                for (int i = 0; i < count; i++)
                {
                    string deviceAddress = BuildDeviceAddress(device, startAddress + i, PlcDeviceType.Word);
                    byte[] appData = BuildReadApplicationData(deviceAddress, 1, DATA_TYPE_WORD);
                    byte[] request = BuildRequest(CMD_READ_REQUEST, appData);
                    byte[] response = SendAndReceive(request, ReceiveTimeout);

                    var checkResult = CheckResponse(response);
                    if (!checkResult.IsSuccess)
                        return PlcResult<short[]>.Fail($"Read failed at {device}{startAddress + i}: {checkResult.ErrorMessage}", checkResult.ErrorCode);

                    // 데이터 오프셋
                    int dataOffset = CalculateDataOffset(response);
                    if (dataOffset < 0 || dataOffset + 2 > response.Length)
                        return PlcResult<short[]>.Fail($"Invalid response at {device}{startAddress + i}");

                    // 1 워드(2 바이트) 읽기
                    values[i] = (short)(response[dataOffset] | (response[dataOffset + 1] << 8));
                }

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

                string deviceAddress = BuildDeviceAddress(device, startAddress, PlcDeviceType.Word);
                byte[] writeData = ShortsToBytes(values, false);

                byte[] appData = BuildWriteApplicationData(deviceAddress, writeData, DATA_TYPE_WORD);
                byte[] request = BuildRequest(CMD_WRITE_REQUEST, appData);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                return CheckResponse(response);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

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
                if (!IsConnected)
                    return PlcResult<int[]>.Fail("Not connected");

                string deviceAddress = BuildDeviceAddress(device, startAddress, PlcDeviceType.DWord);
                byte[] appData = BuildReadApplicationData(deviceAddress, count, DATA_TYPE_DWORD);
                byte[] request = BuildRequest(CMD_READ_REQUEST, appData);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                var checkResult = CheckResponse(response);
                if (!checkResult.IsSuccess)
                    return PlcResult<int[]>.Fail(checkResult.ErrorMessage, checkResult.ErrorCode);

                // 데이터 오프셋 동적 계산
                int dataOffset = CalculateDataOffset(response);
                if (dataOffset < 0 || dataOffset + count * 4 > response.Length)
                    return PlcResult<int[]>.Fail("Invalid response structure");

                byte[] data = new byte[count * 4];
                Array.Copy(response, dataOffset, data, 0, Math.Min(count * 4, response.Length - dataOffset));

                int[] values = BytesToInts(data, false);
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
                if (!IsConnected)
                    return PlcResult.Fail("Not connected");

                string deviceAddress = BuildDeviceAddress(device, startAddress, PlcDeviceType.DWord);
                byte[] writeData = IntsToBytes(values, false);

                byte[] appData = BuildWriteApplicationData(deviceAddress, writeData, DATA_TYPE_DWORD);
                byte[] request = BuildRequest(CMD_WRITE_REQUEST, appData);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                return CheckResponse(response);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

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
                // DWord로 읽어서 float로 변환
                var result = ReadDWords(device, startAddress, count);
                if (!result.IsSuccess)
                    return PlcResult<float[]>.Fail(result.ErrorMessage, result.ErrorCode);

                float[] values = new float[count];
                for (int i = 0; i < count; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(result.Value[i]);
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
                int[] intValues = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(values[i]);
                    intValues[i] = BitConverter.ToInt32(bytes, 0);
                }

                return WriteDWords(device, startAddress, intValues);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        public override PlcResult<string> ReadString(string device, int address, int length)
        {
            try
            {
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

    /// <summary>
    /// LS XGT CPU 타입
    /// </summary>
    public enum XgtCpuType
    {
        /// <summary>XGK 시리즈</summary>
        XGK,
        /// <summary>XGI 시리즈</summary>
        XGI,
        /// <summary>XGR 시리즈</summary>
        XGR,
        /// <summary>XGB 시리즈</summary>
        XGB,
        /// <summary>XEC 시리즈</summary>
        XEC
    }
}
