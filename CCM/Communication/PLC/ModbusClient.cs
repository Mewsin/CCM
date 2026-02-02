using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;
using CCM.Communication.Interfaces;

namespace CCM.Communication.PLC
{
    /// <summary>
    /// Modbus 통신 클래스 (TCP/RTU 지원)
    /// </summary>
    public class ModbusClient : PlcBase
    {
        #region Constants

        // Function Codes
        private const byte FC_READ_COILS = 0x01;
        private const byte FC_READ_DISCRETE_INPUTS = 0x02;
        private const byte FC_READ_HOLDING_REGISTERS = 0x03;
        private const byte FC_READ_INPUT_REGISTERS = 0x04;
        private const byte FC_WRITE_SINGLE_COIL = 0x05;
        private const byte FC_WRITE_SINGLE_REGISTER = 0x06;
        private const byte FC_WRITE_MULTIPLE_COILS = 0x0F;
        private const byte FC_WRITE_MULTIPLE_REGISTERS = 0x10;

        #endregion

        #region Fields

        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private SerialPort _serialPort;
        private ushort _transactionId = 0;

        #endregion

        #region Properties

        /// <summary>
        /// 통신 모드 (TCP/RTU)
        /// </summary>
        public ModbusMode Mode { get; set; } = ModbusMode.Tcp;

        /// <summary>
        /// 슬레이브 주소 (Unit ID)
        /// </summary>
        public byte SlaveAddress { get; set; } = 1;

        /// <summary>
        /// 시리얼 포트 이름 (RTU 모드)
        /// </summary>
        public string SerialPortName { get; set; } = "COM1";

        /// <summary>
        /// 통신 속도 (RTU 모드)
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// 패리티 (RTU 모드)
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>
        /// 데이터 비트 (RTU 모드)
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 스톱 비트 (RTU 모드)
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        public override bool IsConnected
        {
            get
            {
                if (Mode == ModbusMode.Tcp)
                    return _tcpClient != null && _tcpClient.Connected;
                else
                    return _serialPort != null && _serialPort.IsOpen;
            }
        }

        public override string PlcModel => $"Modbus {Mode}";

        #endregion

        #region Constructor

        public ModbusClient()
        {
            Port = 502; // Modbus TCP 기본 포트
            ByteOrder = ByteOrderMode.ABCD; // Modbus는 Big Endian
        }

        /// <summary>
        /// TCP 모드 생성자
        /// </summary>
        public ModbusClient(string ipAddress, int port = 502, byte slaveAddress = 1)
        {
            Mode = ModbusMode.Tcp;
            IpAddress = ipAddress;
            Port = port;
            SlaveAddress = slaveAddress;
            ByteOrder = ByteOrderMode.ABCD; // Modbus는 Big Endian
        }

        /// <summary>
        /// RTU 모드 생성자
        /// </summary>
        public ModbusClient(string portName, int baudRate, Parity parity, byte slaveAddress = 1)
        {
            Mode = ModbusMode.Rtu;
            SerialPortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            SlaveAddress = slaveAddress;
            ByteOrder = ByteOrderMode.ABCD; // Modbus는 Big Endian
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

                    if (Mode == ModbusMode.Tcp)
                    {
                        return ConnectTcp();
                    }
                    else
                    {
                        return ConnectRtu();
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                OnConnectionStateChanged(false, ex.Message);
                return false;
            }
        }

        private bool ConnectTcp()
        {
            _tcpClient = new TcpClient();
            var result = _tcpClient.BeginConnect(IpAddress, Port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(ConnectTimeout);

            if (!success)
            {
                _tcpClient.Close();
                _tcpClient = null;
                OnConnectionStateChanged(false, "Connection timeout");
                return false;
            }

            _tcpClient.EndConnect(result);
            _tcpClient.ReceiveTimeout = ReceiveTimeout;
            _tcpClient.SendTimeout = ReceiveTimeout;
            _tcpStream = _tcpClient.GetStream();

            OnConnectionStateChanged(true, "Connected (TCP)");
            return true;
        }

        private bool ConnectRtu()
        {
            _serialPort = new SerialPort
            {
                PortName = SerialPortName,
                BaudRate = BaudRate,
                Parity = Parity,
                DataBits = DataBits,
                StopBits = StopBits,
                ReadTimeout = ReceiveTimeout,
                WriteTimeout = ReceiveTimeout
            };

            _serialPort.Open();
            OnConnectionStateChanged(true, "Connected (RTU)");
            return true;
        }

        public override void Disconnect()
        {
            lock (_lockObject)
            {
                if (Mode == ModbusMode.Tcp)
                {
                    if (_tcpStream != null)
                    {
                        _tcpStream.Close();
                        _tcpStream.Dispose();
                        _tcpStream = null;
                    }

                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                        _tcpClient = null;
                    }
                }
                else
                {
                    if (_serialPort != null)
                    {
                        if (_serialPort.IsOpen)
                            _serialPort.Close();
                        _serialPort.Dispose();
                        _serialPort = null;
                    }
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
                if (!IsConnected) return false;

                lock (_lockObject)
                {
                    if (Mode == ModbusMode.Tcp)
                    {
                        _tcpStream.Write(data, 0, data.Length);
                        _tcpStream.Flush();
                    }
                    else
                    {
                        _serialPort.Write(data, 0, data.Length);
                    }
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
                if (!IsConnected) return null;

                lock (_lockObject)
                {
                    if (Mode == ModbusMode.Tcp)
                    {
                        return SendAndReceiveTcp(data, timeout);
                    }
                    else
                    {
                        return SendAndReceiveRtu(data, timeout);
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return null;
            }
        }

        private byte[] SendAndReceiveTcp(byte[] data, int timeout)
        {
            _tcpStream.ReadTimeout = timeout;
            _tcpStream.Write(data, 0, data.Length);
            _tcpStream.Flush();

            // MBAP 헤더 수신 (7바이트)
            byte[] header = new byte[7];
            int headerRead = 0;
            while (headerRead < 7)
            {
                int read = _tcpStream.Read(header, headerRead, 7 - headerRead);
                if (read == 0) break;
                headerRead += read;
            }

            if (headerRead < 7) return null;

            // 데이터 길이 추출 (Length 필드)
            int dataLength = (header[4] << 8) | header[5];

            // PDU 수신
            byte[] pdu = new byte[dataLength];
            pdu[0] = header[6]; // Unit ID
            int pduRead = 1;
            while (pduRead < dataLength)
            {
                int read = _tcpStream.Read(pdu, pduRead, dataLength - pduRead);
                if (read == 0) break;
                pduRead += read;
            }

            // 전체 응답 조합
            byte[] response = new byte[7 + dataLength - 1];
            Array.Copy(header, response, 7);
            Array.Copy(pdu, 1, response, 7, dataLength - 1);

            return response;
        }

        private byte[] SendAndReceiveRtu(byte[] data, int timeout)
        {
            _serialPort.ReadTimeout = timeout;
            _serialPort.DiscardInBuffer();
            _serialPort.Write(data, 0, data.Length);

            // 응답 대기 (3.5 character time 기준, 9600bps에서 약 4ms)
            // 하지만 안전을 위해 약간 더 대기
            Thread.Sleep(50);

            // 완전한 프레임이 도착할 때까지 대기
            // RTU 프레임은 최소 5바이트: SlaveAddr(1) + FC(1) + Data(1+) + CRC(2)
            List<byte> responseBuffer = new List<byte>();
            int elapsed = 50;
            int lastBytesRead = 0;
            int stableCount = 0;
            const int STABLE_THRESHOLD = 3; // 연속 3번 동일하면 프레임 완료로 간주

            while (elapsed < timeout)
            {
                int bytesToRead = _serialPort.BytesToRead;
                
                if (bytesToRead > 0)
                {
                    byte[] chunk = new byte[bytesToRead];
                    _serialPort.Read(chunk, 0, bytesToRead);
                    responseBuffer.AddRange(chunk);
                    
                    // 프레임 완료 여부 확인
                    // 프레임 끝 감지: 일정 시간 동안 추가 데이터가 없으면 완료
                    lastBytesRead = responseBuffer.Count;
                    stableCount = 0;
                }
                else if (responseBuffer.Count > 0)
                {
                    // 데이터를 받은 후 추가 데이터가 없으면 안정화 카운트 증가
                    if (responseBuffer.Count == lastBytesRead)
                    {
                        stableCount++;
                        if (stableCount >= STABLE_THRESHOLD && responseBuffer.Count >= 5)
                        {
                            // 프레임 완료로 간주
                            break;
                        }
                    }
                }

                Thread.Sleep(10);
                elapsed += 10;
            }

            if (responseBuffer.Count == 0) return null;

            byte[] response = responseBuffer.ToArray();

            // 최소 길이 검증 (SlaveAddr + FC + CRC = 4바이트, 실제로는 최소 5바이트)
            if (response.Length < 5) return null;

            // CRC 검증
            if (!VerifyCrc(response))
            {
                OnErrorOccurred(new Exception("CRC verification failed"));
                return null;
            }

            return response;
        }

        #endregion

        #region CRC-16/Modbus

        /// <summary>
        /// CRC-16/Modbus 계산
        /// </summary>
        private ushort CalculateCrc(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;

            for (int i = offset; i < offset + length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    bool lsb = (crc & 0x0001) != 0;
                    crc >>= 1;
                    if (lsb) crc ^= 0xA001;
                }
            }

            return crc;
        }

        /// <summary>
        /// CRC 검증
        /// </summary>
        private bool VerifyCrc(byte[] data)
        {
            if (data == null || data.Length < 4)
                return false;

            int dataLen = data.Length - 2;
            ushort calculatedCrc = CalculateCrc(data, 0, dataLen);
            ushort receivedCrc = (ushort)(data[dataLen] | (data[dataLen + 1] << 8));

            return calculatedCrc == receivedCrc;
        }

        #endregion

        #region Frame Building

        /// <summary>
        /// Modbus TCP 요청 생성
        /// </summary>
        private byte[] BuildTcpRequest(byte functionCode, byte[] pdu)
        {
            _transactionId++;
            int length = pdu.Length + 1; // Unit ID + PDU

            List<byte> frame = new List<byte>
            {
                (byte)(_transactionId >> 8),    // Transaction ID Hi
                (byte)(_transactionId & 0xFF),  // Transaction ID Lo
                0x00, 0x00,                      // Protocol ID (0 = Modbus)
                (byte)(length >> 8),            // Length Hi
                (byte)(length & 0xFF),          // Length Lo
                SlaveAddress                     // Unit ID
            };
            frame.AddRange(pdu);

            return frame.ToArray();
        }

        /// <summary>
        /// Modbus RTU 요청 생성
        /// </summary>
        private byte[] BuildRtuRequest(byte functionCode, byte[] pdu)
        {
            List<byte> frame = new List<byte> { SlaveAddress };
            frame.AddRange(pdu);

            // CRC 추가
            ushort crc = CalculateCrc(frame.ToArray(), 0, frame.Count);
            frame.Add((byte)(crc & 0xFF));       // CRC Lo
            frame.Add((byte)(crc >> 8));         // CRC Hi

            return frame.ToArray();
        }

        /// <summary>
        /// 요청 생성 (모드에 따라)
        /// </summary>
        private byte[] BuildRequest(byte functionCode, byte[] pdu)
        {
            if (Mode == ModbusMode.Tcp)
                return BuildTcpRequest(functionCode, pdu);
            else
                return BuildRtuRequest(functionCode, pdu);
        }

        /// <summary>
        /// 응답 검증
        /// </summary>
        private PlcResult CheckResponse(byte[] response, byte expectedFunctionCode)
        {
            // TCP: MBAP(7) + FC(1) + ErrorCode(1) = 최소 9바이트
            // RTU: Addr(1) + FC(1) + ErrorCode(1) + CRC(2) = 최소 5바이트
            int minLength = (Mode == ModbusMode.Tcp) ? 9 : 5;
            if (response == null || response.Length < minLength)
                return PlcResult.Fail("Invalid response");

            int fcOffset = (Mode == ModbusMode.Tcp) ? 7 : 1;
            byte fc = response[fcOffset];

            // 에러 응답 확인 (Function Code + 0x80)
            if ((fc & 0x80) != 0)
            {
                byte errorCode = response[fcOffset + 1];
                return PlcResult.Fail($"Modbus Exception: {GetExceptionMessage(errorCode)}", errorCode);
            }

            if (fc != expectedFunctionCode)
                return PlcResult.Fail($"Unexpected function code: {fc}");

            return PlcResult.Success();
        }

        private string GetExceptionMessage(byte code)
        {
            switch (code)
            {
                case 0x01: return "Illegal Function";
                case 0x02: return "Illegal Data Address";
                case 0x03: return "Illegal Data Value";
                case 0x04: return "Slave Device Failure";
                case 0x05: return "Acknowledge";
                case 0x06: return "Slave Device Busy";
                case 0x08: return "Memory Parity Error";
                case 0x0A: return "Gateway Path Unavailable";
                case 0x0B: return "Gateway Target Device Failed to Respond";
                default: return $"Unknown Error (0x{code:X2})";
            }
        }

        #endregion

        #region Coil Operations

        /// <summary>
        /// 코일 읽기 (FC01)
        /// </summary>
        public PlcResult<bool[]> ReadCoils(int startAddress, int count)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult<bool[]>.Fail("Not connected");

                byte[] pdu = new byte[]
                {
                    FC_READ_COILS,
                    (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
                    (byte)(count >> 8), (byte)(count & 0xFF)
                };

                byte[] request = BuildRequest(FC_READ_COILS, pdu);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                var checkResult = CheckResponse(response, FC_READ_COILS);
                if (!checkResult.IsSuccess)
                    return PlcResult<bool[]>.Fail(checkResult.ErrorMessage, checkResult.ErrorCode);

                // 데이터 추출
                int dataOffset = (Mode == ModbusMode.Tcp) ? 9 : 3;
                int byteCount = response[dataOffset - 1];

                bool[] values = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    int byteIndex = i / 8;
                    int bitIndex = i % 8;
                    values[i] = (response[dataOffset + byteIndex] & (1 << bitIndex)) != 0;
                }

                return PlcResult<bool[]>.Success(values);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult<bool[]>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 단일 코일 쓰기 (FC05)
        /// </summary>
        public PlcResult WriteSingleCoil(int address, bool value)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult.Fail("Not connected");

                ushort coilValue = (ushort)(value ? 0xFF00 : 0x0000);

                byte[] pdu = new byte[]
                {
                    FC_WRITE_SINGLE_COIL,
                    (byte)(address >> 8), (byte)(address & 0xFF),
                    (byte)(coilValue >> 8), (byte)(coilValue & 0xFF)
                };

                byte[] request = BuildRequest(FC_WRITE_SINGLE_COIL, pdu);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                return CheckResponse(response, FC_WRITE_SINGLE_COIL);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 다중 코일 쓰기 (FC0F)
        /// </summary>
        public PlcResult WriteMultipleCoils(int startAddress, bool[] values)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult.Fail("Not connected");

                int byteCount = (values.Length + 7) / 8;
                byte[] coilData = new byte[byteCount];

                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i])
                    {
                        int byteIndex = i / 8;
                        int bitIndex = i % 8;
                        coilData[byteIndex] |= (byte)(1 << bitIndex);
                    }
                }

                List<byte> pdu = new List<byte>
                {
                    FC_WRITE_MULTIPLE_COILS,
                    (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
                    (byte)(values.Length >> 8), (byte)(values.Length & 0xFF),
                    (byte)byteCount
                };
                pdu.AddRange(coilData);

                byte[] request = BuildRequest(FC_WRITE_MULTIPLE_COILS, pdu.ToArray());
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                return CheckResponse(response, FC_WRITE_MULTIPLE_COILS);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        #endregion

        #region Register Operations

        /// <summary>
        /// 홀딩 레지스터 읽기 (FC03)
        /// </summary>
        public PlcResult<short[]> ReadHoldingRegisters(int startAddress, int count)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult<short[]>.Fail("Not connected");

                byte[] pdu = new byte[]
                {
                    FC_READ_HOLDING_REGISTERS,
                    (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
                    (byte)(count >> 8), (byte)(count & 0xFF)
                };

                byte[] request = BuildRequest(FC_READ_HOLDING_REGISTERS, pdu);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                var checkResult = CheckResponse(response, FC_READ_HOLDING_REGISTERS);
                if (!checkResult.IsSuccess)
                    return PlcResult<short[]>.Fail(checkResult.ErrorMessage, checkResult.ErrorCode);

                // 데이터 추출
                int dataOffset = (Mode == ModbusMode.Tcp) ? 9 : 3;
                byte[] data = new byte[count * 2];
                Array.Copy(response, dataOffset, data, 0, count * 2);

                // Big Endian
                short[] values = BytesToShorts(data, true);
                return PlcResult<short[]>.Success(values);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult<short[]>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 입력 레지스터 읽기 (FC04)
        /// </summary>
        public PlcResult<short[]> ReadInputRegisters(int startAddress, int count)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult<short[]>.Fail("Not connected");

                byte[] pdu = new byte[]
                {
                    FC_READ_INPUT_REGISTERS,
                    (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
                    (byte)(count >> 8), (byte)(count & 0xFF)
                };

                byte[] request = BuildRequest(FC_READ_INPUT_REGISTERS, pdu);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                var checkResult = CheckResponse(response, FC_READ_INPUT_REGISTERS);
                if (!checkResult.IsSuccess)
                    return PlcResult<short[]>.Fail(checkResult.ErrorMessage, checkResult.ErrorCode);

                int dataOffset = (Mode == ModbusMode.Tcp) ? 9 : 3;
                byte[] data = new byte[count * 2];
                Array.Copy(response, dataOffset, data, 0, count * 2);

                short[] values = BytesToShorts(data, true);
                return PlcResult<short[]>.Success(values);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult<short[]>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 단일 레지스터 쓰기 (FC06)
        /// </summary>
        public PlcResult WriteSingleRegister(int address, short value)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult.Fail("Not connected");

                byte[] pdu = new byte[]
                {
                    FC_WRITE_SINGLE_REGISTER,
                    (byte)(address >> 8), (byte)(address & 0xFF),
                    (byte)(value >> 8), (byte)(value & 0xFF)
                };

                byte[] request = BuildRequest(FC_WRITE_SINGLE_REGISTER, pdu);
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                return CheckResponse(response, FC_WRITE_SINGLE_REGISTER);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 다중 레지스터 쓰기 (FC10)
        /// </summary>
        public PlcResult WriteMultipleRegisters(int startAddress, short[] values)
        {
            try
            {
                if (!IsConnected)
                    return PlcResult.Fail("Not connected");

                byte[] registerData = ShortsToBytes(values, true);

                List<byte> pdu = new List<byte>
                {
                    FC_WRITE_MULTIPLE_REGISTERS,
                    (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
                    (byte)(values.Length >> 8), (byte)(values.Length & 0xFF),
                    (byte)(values.Length * 2)
                };
                pdu.AddRange(registerData);

                byte[] request = BuildRequest(FC_WRITE_MULTIPLE_REGISTERS, pdu.ToArray());
                byte[] response = SendAndReceive(request, ReceiveTimeout);

                return CheckResponse(response, FC_WRITE_MULTIPLE_REGISTERS);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                return PlcResult.Fail(ex.Message);
            }
        }

        #endregion

        #region IPlcCommunication Implementation

        public override PlcResult<bool> ReadBit(string device, int address)
        {
            var result = ReadCoils(address, 1);
            if (!result.IsSuccess)
                return PlcResult<bool>.Fail(result.ErrorMessage, result.ErrorCode);

            return PlcResult<bool>.Success(result.Value[0]);
        }

        public override PlcResult<bool[]> ReadBits(string device, int startAddress, int count)
        {
            return ReadCoils(startAddress, count);
        }

        public override PlcResult WriteBit(string device, int address, bool value)
        {
            return WriteSingleCoil(address, value);
        }

        public override PlcResult WriteBits(string device, int startAddress, bool[] values)
        {
            return WriteMultipleCoils(startAddress, values);
        }

        public override PlcResult<short> ReadWord(string device, int address)
        {
            var result = ReadHoldingRegisters(address, 1);
            if (!result.IsSuccess)
                return PlcResult<short>.Fail(result.ErrorMessage, result.ErrorCode);

            return PlcResult<short>.Success(result.Value[0]);
        }

        public override PlcResult<short[]> ReadWords(string device, int startAddress, int count)
        {
            return ReadHoldingRegisters(startAddress, count);
        }

        public override PlcResult WriteWord(string device, int address, short value)
        {
            return WriteSingleRegister(address, value);
        }

        public override PlcResult WriteWords(string device, int startAddress, short[] values)
        {
            return WriteMultipleRegisters(startAddress, values);
        }

        public override PlcResult<int> ReadDWord(string device, int address)
        {
            var result = ReadHoldingRegisters(address, 2);
            if (!result.IsSuccess)
                return PlcResult<int>.Fail(result.ErrorMessage, result.ErrorCode);

            int value = ((ushort)result.Value[0] << 16) | (ushort)result.Value[1];
            return PlcResult<int>.Success(value);
        }

        public override PlcResult<int[]> ReadDWords(string device, int startAddress, int count)
        {
            var result = ReadHoldingRegisters(startAddress, count * 2);
            if (!result.IsSuccess)
                return PlcResult<int[]>.Fail(result.ErrorMessage, result.ErrorCode);

            int[] values = new int[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = ((ushort)result.Value[i * 2] << 16) | (ushort)result.Value[i * 2 + 1];
            }

            return PlcResult<int[]>.Success(values);
        }

        public override PlcResult WriteDWord(string device, int address, int value)
        {
            short[] words = new short[]
            {
                (short)(value >> 16),
                (short)(value & 0xFFFF)
            };
            return WriteMultipleRegisters(address, words);
        }

        public override PlcResult WriteDWords(string device, int startAddress, int[] values)
        {
            short[] words = new short[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                words[i * 2] = (short)(values[i] >> 16);
                words[i * 2 + 1] = (short)(values[i] & 0xFFFF);
            }
            return WriteMultipleRegisters(startAddress, words);
        }

        public override PlcResult<float> ReadReal(string device, int address)
        {
            var result = ReadHoldingRegisters(address, 2);
            if (!result.IsSuccess)
                return PlcResult<float>.Fail(result.ErrorMessage, result.ErrorCode);

            byte[] bytes = new byte[4];
            bytes[0] = (byte)(result.Value[1] & 0xFF);
            bytes[1] = (byte)(result.Value[1] >> 8);
            bytes[2] = (byte)(result.Value[0] & 0xFF);
            bytes[3] = (byte)(result.Value[0] >> 8);

            float value = BitConverter.ToSingle(bytes, 0);
            return PlcResult<float>.Success(value);
        }

        public override PlcResult<float[]> ReadReals(string device, int startAddress, int count)
        {
            var result = ReadHoldingRegisters(startAddress, count * 2);
            if (!result.IsSuccess)
                return PlcResult<float[]>.Fail(result.ErrorMessage, result.ErrorCode);

            float[] values = new float[count];
            for (int i = 0; i < count; i++)
            {
                byte[] bytes = new byte[4];
                bytes[0] = (byte)(result.Value[i * 2 + 1] & 0xFF);
                bytes[1] = (byte)(result.Value[i * 2 + 1] >> 8);
                bytes[2] = (byte)(result.Value[i * 2] & 0xFF);
                bytes[3] = (byte)(result.Value[i * 2] >> 8);
                values[i] = BitConverter.ToSingle(bytes, 0);
            }

            return PlcResult<float[]>.Success(values);
        }

        public override PlcResult WriteReal(string device, int address, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            short[] words = new short[]
            {
                (short)(bytes[2] | (bytes[3] << 8)),
                (short)(bytes[0] | (bytes[1] << 8))
            };
            return WriteMultipleRegisters(address, words);
        }

        public override PlcResult WriteReals(string device, int startAddress, float[] values)
        {
            short[] words = new short[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(values[i]);
                words[i * 2] = (short)(bytes[2] | (bytes[3] << 8));
                words[i * 2 + 1] = (short)(bytes[0] | (bytes[1] << 8));
            }
            return WriteMultipleRegisters(startAddress, words);
        }

        public override PlcResult<string> ReadString(string device, int address, int length)
        {
            int wordCount = (length + 1) / 2;
            var result = ReadHoldingRegisters(address, wordCount);
            if (!result.IsSuccess)
                return PlcResult<string>.Fail(result.ErrorMessage, result.ErrorCode);

            byte[] bytes = ShortsToBytes(result.Value, true);
            string value = BytesToString(bytes);

            if (value.Length > length)
                value = value.Substring(0, length);

            return PlcResult<string>.Success(value);
        }

        public override PlcResult WriteString(string device, int address, string value)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value ?? string.Empty);
            int paddedLength = (bytes.Length + 1) / 2 * 2;
            byte[] paddedBytes = new byte[paddedLength];
            Array.Copy(bytes, paddedBytes, bytes.Length);

            short[] words = BytesToShorts(paddedBytes, true);
            return WriteMultipleRegisters(address, words);
        }

        #endregion
    }

    /// <summary>
    /// Modbus 통신 모드
    /// </summary>
    public enum ModbusMode
    {
        Tcp,
        Rtu
    }
}
