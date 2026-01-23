using System;
using System.Collections.Generic;
using System.Net.Sockets;
using IndustrialCommunication.Communication.Interfaces;

namespace IndustrialCommunication.Communication.PLC
{
    /// <summary>
    /// Siemens PLC S7 Protocol 통신 클래스
    /// </summary>
    public class SiemensS7Protocol : PlcBase
    {
        #region Constants

        // TPKT Header
        private const byte TPKT_VERSION = 0x03;
        private const byte TPKT_RESERVED = 0x00;

        // COTP (ISO 8073)
        private const byte COTP_CR_PDU_TYPE = 0xE0;   // Connect Request
        private const byte COTP_CC_PDU_TYPE = 0xD0;   // Connect Confirm
        private const byte COTP_DT_PDU_TYPE = 0xF0;   // Data Transfer

        // S7 Protocol
        private const byte S7_PROTOCOL_ID = 0x32;
        private const byte S7_MSG_JOB = 0x01;
        private const byte S7_MSG_ACK_DATA = 0x03;
        private const byte S7_FUNC_SETUP_COMM = 0xF0;
        private const byte S7_FUNC_READ_VAR = 0x04;
        private const byte S7_FUNC_WRITE_VAR = 0x05;

        // Area Codes
        private const byte S7_AREA_I = 0x81;   // Input
        private const byte S7_AREA_Q = 0x82;   // Output
        private const byte S7_AREA_M = 0x83;   // Merker (Flag)
        private const byte S7_AREA_DB = 0x84;  // Data Block

        // Transport Size
        private const byte TS_BIT = 0x01;
        private const byte TS_BYTE = 0x02;
        private const byte TS_WORD = 0x04;
        private const byte TS_DWORD = 0x06;
        private const byte TS_REAL = 0x08;

        #endregion

        #region Fields

        private TcpClient _client;
        private NetworkStream _stream;
        private ushort _pduSize = 480;

        #endregion

        #region Properties

        /// <summary>
        /// CPU 타입 (S7-300, S7-400, S7-1200, S7-1500)
        /// </summary>
        public S7CpuType CpuType { get; set; } = S7CpuType.S71200;

        /// <summary>
        /// 랙 번호
        /// </summary>
        public byte Rack { get; set; } = 0;

        /// <summary>
        /// 슬롯 번호
        /// </summary>
        public byte Slot { get; set; } = 1;

        public override bool IsConnected => _client != null && _client.Connected;

        public override string PlcModel => $"Siemens {CpuType} (S7 Protocol)";

        #endregion

        #region Constructor

        public SiemensS7Protocol()
        {
            Port = 102; // S7 Protocol 기본 포트
        }

        public SiemensS7Protocol(string ipAddress, S7CpuType cpuType = S7CpuType.S71200, byte rack = 0, byte slot = 1)
        {
            IpAddress = ipAddress;
            Port = 102;
            CpuType = cpuType;
            Rack = rack;
            Slot = slot;
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

                    // COTP 연결
                    if (!ConnectCotp())
                    {
                        Disconnect();
                        return false;
                    }

                    // S7 통신 설정
                    if (!SetupCommunication())
                    {
                        Disconnect();
                        return false;
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

        private bool ConnectCotp()
        {
            // COTP Connect Request 생성
            byte[] cotpCr = BuildCotpConnectRequest();

            _stream.Write(cotpCr, 0, cotpCr.Length);
            _stream.Flush();

            // COTP Connect Confirm 수신
            byte[] response = ReceivePacket();
            if (response == null || response.Length < 4)
                return false;

            // PDU Type 확인
            if (response[5] != COTP_CC_PDU_TYPE)
                return false;

            return true;
        }

        private byte[] BuildCotpConnectRequest()
        {
            // TSAP 계산 (CPU 타입에 따라 다름)
            byte localTsap1 = 0x01;
            byte localTsap2 = 0x00;
            byte remoteTsap1 = (byte)(CpuType == S7CpuType.S7200 ? 0x10 : 0x01);
            byte remoteTsap2 = (byte)((Rack << 5) | Slot);

            byte[] cotp = new byte[]
            {
                TPKT_VERSION,    // TPKT Version
                TPKT_RESERVED,   // TPKT Reserved
                0x00, 0x16,      // TPKT Length (22 bytes)
                0x11,            // COTP Length
                COTP_CR_PDU_TYPE,// COTP PDU Type (CR)
                0x00, 0x00,      // Destination Reference
                0x00, 0x01,      // Source Reference
                0x00,            // Class & Options
                0xC0, 0x01, 0x0A,// TPDU Size (1024)
                0xC1, 0x02, localTsap1, localTsap2,   // Source TSAP
                0xC2, 0x02, remoteTsap1, remoteTsap2  // Destination TSAP
            };

            return cotp;
        }

        private bool SetupCommunication()
        {
            // S7 Setup Communication 요청
            byte[] s7Setup = new byte[]
            {
                TPKT_VERSION, TPKT_RESERVED, 0x00, 0x19,  // TPKT Header
                0x02, COTP_DT_PDU_TYPE, 0x80,             // COTP DT
                S7_PROTOCOL_ID,                           // S7 Protocol ID
                S7_MSG_JOB,                               // Message Type: Job
                0x00, 0x00,                               // Reserved
                0x00, 0x00,                               // PDU Reference
                0x00, 0x08,                               // Parameter Length
                0x00, 0x00,                               // Data Length
                S7_FUNC_SETUP_COMM,                       // Function: Setup Communication
                0x00,                                     // Reserved
                0x00, 0x01,                               // Max AmQ Calling
                0x00, 0x01,                               // Max AmQ Called
                0x03, 0xC0                                // PDU Size (960)
            };

            _stream.Write(s7Setup, 0, s7Setup.Length);
            _stream.Flush();

            byte[] response = ReceivePacket();
            if (response == null || response.Length < 20)
                return false;

            // PDU 크기 추출
            _pduSize = (ushort)((response[25] << 8) | response[26]);

            return true;
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
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();

                    return ReceivePacket();
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return null;
            }
        }

        private byte[] ReceivePacket()
        {
            // TPKT 헤더 수신 (4바이트)
            byte[] tpktHeader = new byte[4];
            int read = _stream.Read(tpktHeader, 0, 4);
            if (read < 4) return null;

            // 패킷 길이 추출
            int length = (tpktHeader[2] << 8) | tpktHeader[3];

            // 나머지 데이터 수신
            byte[] data = new byte[length];
            Array.Copy(tpktHeader, data, 4);

            int remaining = length - 4;
            int offset = 4;
            while (remaining > 0)
            {
                read = _stream.Read(data, offset, remaining);
                if (read == 0) break;
                offset += read;
                remaining -= read;
            }

            return data;
        }

        #endregion

        #region Read/Write Operations

        private byte GetAreaCode(string device)
        {
            switch (device.ToUpper())
            {
                case "I": case "E": return S7_AREA_I;
                case "Q": case "A": return S7_AREA_Q;
                case "M": case "F": return S7_AREA_M;
                case "DB": return S7_AREA_DB;
                default:
                    if (device.ToUpper().StartsWith("DB"))
                        return S7_AREA_DB;
                    throw new ArgumentException($"Unknown device: {device}");
            }
        }

        private int GetDbNumber(string device)
        {
            if (device.ToUpper().StartsWith("DB"))
            {
                string numStr = device.Substring(2);
                if (int.TryParse(numStr, out int dbNum))
                    return dbNum;
            }
            return 0;
        }

        /// <summary>
        /// 바이트 배열 읽기
        /// </summary>
        public PlcResult<byte[]> ReadBytes(string device, int startAddress, int count)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult<byte[]>.Fail("Not connected");

                byte areaCode = GetAreaCode(device);
                int dbNumber = GetDbNumber(device);

                // S7 Read 요청 생성
                byte[] request = BuildReadRequest(areaCode, dbNumber, startAddress, count);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                if (response == null || response.Length < 25)
                    return PlcResult<byte[]>.Fail("Invalid response");

                // 에러 확인
                byte errorClass = response[17];
                byte errorCode = response[18];
                if (errorClass != 0 || errorCode != 0)
                    return PlcResult<byte[]>.Fail($"S7 Error: Class={errorClass}, Code={errorCode}");

                // 데이터 추출
                int dataLength = (response[23] << 8) | response[24];
                if (dataLength == 0)
                    return PlcResult<byte[]>.Fail("No data returned");

                dataLength = dataLength / 8; // 비트 -> 바이트
                byte[] data = new byte[dataLength];
                Array.Copy(response, 25, data, 0, Math.Min(dataLength, response.Length - 25));

                return PlcResult<byte[]>.Success(data);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult<byte[]>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 바이트 배열 쓰기
        /// </summary>
        public PlcResult WriteBytes(string device, int startAddress, byte[] data)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult.Fail("Not connected");

                byte areaCode = GetAreaCode(device);
                int dbNumber = GetDbNumber(device);

                // S7 Write 요청 생성
                byte[] request = BuildWriteRequest(areaCode, dbNumber, startAddress, data);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                if (response == null || response.Length < 22)
                    return PlcResult.Fail("Invalid response");

                // 에러 확인
                byte errorClass = response[17];
                byte errorCode = response[18];
                if (errorClass != 0 || errorCode != 0)
                    return PlcResult.Fail($"S7 Error: Class={errorClass}, Code={errorCode}");

                // 쓰기 결과 확인
                byte result = response[21];
                if (result != 0xFF)
                    return PlcResult.Fail($"Write failed: {result}");

                return PlcResult.Success();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        private byte[] BuildReadRequest(byte areaCode, int dbNumber, int startAddress, int count)
        {
            int startBit = startAddress * 8;
            List<byte> request = new List<byte>
            {
                // TPKT Header
                TPKT_VERSION, TPKT_RESERVED, 0x00, 0x1F,
                // COTP
                0x02, COTP_DT_PDU_TYPE, 0x80,
                // S7 Header
                S7_PROTOCOL_ID,
                S7_MSG_JOB,
                0x00, 0x00,          // Reserved
                0x00, 0x01,          // PDU Reference
                0x00, 0x0E,          // Parameter Length
                0x00, 0x00,          // Data Length
                // Parameters
                S7_FUNC_READ_VAR,
                0x01,                // Item Count
                // Item
                0x12,                // Structure identifier
                0x0A,                // Item length
                0x10,                // Syntax ID: S7ANY
                TS_BYTE,             // Transport Size
                (byte)(count >> 8), (byte)count,  // Length
                (byte)(dbNumber >> 8), (byte)dbNumber,  // DB Number
                areaCode,            // Area
                (byte)(startBit >> 16), (byte)(startBit >> 8), (byte)startBit  // Start Address
            };

            // TPKT Length 업데이트
            request[2] = (byte)(request.Count >> 8);
            request[3] = (byte)request.Count;

            return request.ToArray();
        }

        private byte[] BuildWriteRequest(byte areaCode, int dbNumber, int startAddress, byte[] data)
        {
            int startBit = startAddress * 8;
            int dataLen = data.Length;
            List<byte> request = new List<byte>
            {
                // TPKT Header (임시 길이)
                TPKT_VERSION, TPKT_RESERVED, 0x00, 0x00,
                // COTP
                0x02, COTP_DT_PDU_TYPE, 0x80,
                // S7 Header
                S7_PROTOCOL_ID,
                S7_MSG_JOB,
                0x00, 0x00,
                0x00, 0x01,
                0x00, 0x0E,          // Parameter Length
                (byte)((dataLen + 4) >> 8), (byte)(dataLen + 4),  // Data Length
                // Parameters
                S7_FUNC_WRITE_VAR,
                0x01,                // Item Count
                // Item
                0x12, 0x0A, 0x10,
                TS_BYTE,
                (byte)(dataLen >> 8), (byte)dataLen,
                (byte)(dbNumber >> 8), (byte)dbNumber,
                areaCode,
                (byte)(startBit >> 16), (byte)(startBit >> 8), (byte)startBit,
                // Data Item Header
                0x00,
                0x04,                // Transport Size: Byte
                (byte)((dataLen * 8) >> 8), (byte)(dataLen * 8)  // Length in bits
            };

            request.AddRange(data);

            // 패딩 (짝수 바이트)
            if (data.Length % 2 != 0)
                request.Add(0x00);

            // TPKT Length 업데이트
            request[2] = (byte)(request.Count >> 8);
            request[3] = (byte)request.Count;

            return request.ToArray();
        }

        #endregion

        #region IPlcCommunication Implementation

        public override PlcResult<bool> ReadBit(string device, int address)
        {
            int byteAddress = address / 8;
            int bitOffset = address % 8;

            var result = ReadBytes(device, byteAddress, 1);
            if (!result.IsSuccess)
                return PlcResult<bool>.Fail(result.ErrorMessage, result.ErrorCode);

            bool value = (result.Value[0] & (1 << bitOffset)) != 0;
            return PlcResult<bool>.Success(value);
        }

        public override PlcResult<bool[]> ReadBits(string device, int startAddress, int count)
        {
            int startByte = startAddress / 8;
            int endByte = (startAddress + count - 1) / 8;
            int byteCount = endByte - startByte + 1;

            var result = ReadBytes(device, startByte, byteCount);
            if (!result.IsSuccess)
                return PlcResult<bool[]>.Fail(result.ErrorMessage, result.ErrorCode);

            bool[] values = new bool[count];
            for (int i = 0; i < count; i++)
            {
                int bitAddress = startAddress + i;
                int byteIndex = (bitAddress / 8) - startByte;
                int bitOffset = bitAddress % 8;
                values[i] = (result.Value[byteIndex] & (1 << bitOffset)) != 0;
            }

            return PlcResult<bool[]>.Success(values);
        }

        public override PlcResult WriteBit(string device, int address, bool value)
        {
            int byteAddress = address / 8;
            int bitOffset = address % 8;

            // 먼저 현재 바이트 읽기
            var readResult = ReadBytes(device, byteAddress, 1);
            if (!readResult.IsSuccess)
                return PlcResult.Fail(readResult.ErrorMessage, readResult.ErrorCode);

            byte currentByte = readResult.Value[0];
            if (value)
                currentByte |= (byte)(1 << bitOffset);
            else
                currentByte &= (byte)~(1 << bitOffset);

            return WriteBytes(device, byteAddress, new byte[] { currentByte });
        }

        public override PlcResult WriteBits(string device, int startAddress, bool[] values)
        {
            // 간단한 구현: 각 비트를 개별적으로 쓰기
            for (int i = 0; i < values.Length; i++)
            {
                var result = WriteBit(device, startAddress + i, values[i]);
                if (!result.IsSuccess)
                    return result;
            }
            return PlcResult.Success();
        }

        public override PlcResult<short> ReadWord(string device, int address)
        {
            var result = ReadBytes(device, address, 2);
            if (!result.IsSuccess)
                return PlcResult<short>.Fail(result.ErrorMessage, result.ErrorCode);

            // Big Endian (Siemens)
            short value = (short)((result.Value[0] << 8) | result.Value[1]);
            return PlcResult<short>.Success(value);
        }

        public override PlcResult<short[]> ReadWords(string device, int startAddress, int count)
        {
            var result = ReadBytes(device, startAddress, count * 2);
            if (!result.IsSuccess)
                return PlcResult<short[]>.Fail(result.ErrorMessage, result.ErrorCode);

            short[] values = BytesToShorts(result.Value, true);
            return PlcResult<short[]>.Success(values);
        }

        public override PlcResult WriteWord(string device, int address, short value)
        {
            byte[] data = new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
            return WriteBytes(device, address, data);
        }

        public override PlcResult WriteWords(string device, int startAddress, short[] values)
        {
            byte[] data = ShortsToBytes(values, true);
            return WriteBytes(device, startAddress, data);
        }

        public override PlcResult<int> ReadDWord(string device, int address)
        {
            var result = ReadBytes(device, address, 4);
            if (!result.IsSuccess)
                return PlcResult<int>.Fail(result.ErrorMessage, result.ErrorCode);

            int value = (result.Value[0] << 24) | (result.Value[1] << 16) |
                       (result.Value[2] << 8) | result.Value[3];
            return PlcResult<int>.Success(value);
        }

        public override PlcResult<int[]> ReadDWords(string device, int startAddress, int count)
        {
            var result = ReadBytes(device, startAddress, count * 4);
            if (!result.IsSuccess)
                return PlcResult<int[]>.Fail(result.ErrorMessage, result.ErrorCode);

            int[] values = BytesToInts(result.Value, true);
            return PlcResult<int[]>.Success(values);
        }

        public override PlcResult WriteDWord(string device, int address, int value)
        {
            byte[] data = new byte[]
            {
                (byte)(value >> 24), (byte)(value >> 16),
                (byte)(value >> 8), (byte)(value & 0xFF)
            };
            return WriteBytes(device, address, data);
        }

        public override PlcResult WriteDWords(string device, int startAddress, int[] values)
        {
            byte[] data = IntsToBytes(values, true);
            return WriteBytes(device, startAddress, data);
        }

        public override PlcResult<float> ReadReal(string device, int address)
        {
            var result = ReadBytes(device, address, 4);
            if (!result.IsSuccess)
                return PlcResult<float>.Fail(result.ErrorMessage, result.ErrorCode);

            // Big Endian to Little Endian
            byte[] leBytes = new byte[] { result.Value[3], result.Value[2], result.Value[1], result.Value[0] };
            float value = BitConverter.ToSingle(leBytes, 0);
            return PlcResult<float>.Success(value);
        }

        public override PlcResult<float[]> ReadReals(string device, int startAddress, int count)
        {
            var result = ReadBytes(device, startAddress, count * 4);
            if (!result.IsSuccess)
                return PlcResult<float[]>.Fail(result.ErrorMessage, result.ErrorCode);

            float[] values = BytesToFloats(result.Value, true);
            return PlcResult<float[]>.Success(values);
        }

        public override PlcResult WriteReal(string device, int address, float value)
        {
            byte[] leBytes = BitConverter.GetBytes(value);
            byte[] data = new byte[] { leBytes[3], leBytes[2], leBytes[1], leBytes[0] };
            return WriteBytes(device, address, data);
        }

        public override PlcResult WriteReals(string device, int startAddress, float[] values)
        {
            byte[] data = FloatsToBytes(values, true);
            return WriteBytes(device, startAddress, data);
        }

        public override PlcResult<string> ReadString(string device, int address, int length)
        {
            var result = ReadBytes(device, address, length);
            if (!result.IsSuccess)
                return PlcResult<string>.Fail(result.ErrorMessage, result.ErrorCode);

            string value = BytesToString(result.Value);
            return PlcResult<string>.Success(value);
        }

        public override PlcResult WriteString(string device, int address, string value)
        {
            byte[] data = StringToBytes(value, value?.Length ?? 0);
            return WriteBytes(device, address, data);
        }

        #endregion
    }

    /// <summary>
    /// Siemens PLC CPU 타입
    /// </summary>
    public enum S7CpuType
    {
        S7200,
        S7300,
        S7400,
        S71200,
        S71500
    }
}
