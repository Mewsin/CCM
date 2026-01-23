using System;

namespace CCM.Communication.PLC.Utilities
{
    /// <summary>
    /// PLC별 디바이스 주소 헬퍼 - 각 PLC 제조사의 디바이스 표기법 상수 제공
    /// </summary>
    public static class PlcDeviceHelper
    {
        #region Mitsubishi MELSEC 디바이스

        /// <summary>
        /// Mitsubishi MELSEC 시리즈 디바이스 상수
        /// </summary>
        public static class Mitsubishi
        {
            // Bit Devices (비트 디바이스)
            /// <summary>입력 릴레이 (X0, X1, ...)</summary>
            public const string X = "X";
            /// <summary>출력 릴레이 (Y0, Y1, ...)</summary>
            public const string Y = "Y";
            /// <summary>내부 릴레이 (M0, M1, ...)</summary>
            public const string M = "M";
            /// <summary>래치 릴레이 (L0, L1, ...)</summary>
            public const string L = "L";
            /// <summary>펄스 릴레이 (F0, F1, ...)</summary>
            public const string F = "F";
            /// <summary>에지 릴레이 (V0, V1, ...)</summary>
            public const string V = "V";
            /// <summary>링크 릴레이 (B0, B1, ...)</summary>
            public const string B = "B";
            /// <summary>스텝 릴레이 (S0, S1, ...)</summary>
            public const string S = "S";
            /// <summary>타이머 접점 (TS0, TS1, ...)</summary>
            public const string TS = "TS";
            /// <summary>카운터 접점 (CS0, CS1, ...)</summary>
            public const string CS = "CS";

            // Word Devices (워드 디바이스)
            /// <summary>데이터 레지스터 (D0, D1, ...)</summary>
            public const string D = "D";
            /// <summary>링크 레지스터 (W0, W1, ...)</summary>
            public const string W = "W";
            /// <summary>타이머 현재값 (TN0, TN1, ...)</summary>
            public const string TN = "TN";
            /// <summary>카운터 현재값 (CN0, CN1, ...)</summary>
            public const string CN = "CN";
            /// <summary>파일 레지스터 (R0, R1, ...)</summary>
            public const string R = "R";
            /// <summary>특수 레지스터 (SD0, SD1, ...)</summary>
            public const string SD = "SD";

            /// <summary>
            /// 디바이스 정보 조회
            /// </summary>
            public static DeviceInfo GetDeviceInfo(string device)
            {
                switch (device.ToUpper())
                {
                    case "X": return new DeviceInfo("입력 릴레이", true, "8진수");
                    case "Y": return new DeviceInfo("출력 릴레이", true, "8진수");
                    case "M": return new DeviceInfo("내부 릴레이", true, "10진수");
                    case "L": return new DeviceInfo("래치 릴레이", true, "10진수");
                    case "B": return new DeviceInfo("링크 릴레이", true, "16진수");
                    case "D": return new DeviceInfo("데이터 레지스터", false, "10진수");
                    case "W": return new DeviceInfo("링크 레지스터", false, "16진수");
                    case "R": return new DeviceInfo("파일 레지스터", false, "10진수");
                    default: return new DeviceInfo("Unknown", false, "10진수");
                }
            }

            /// <summary>
            /// 주소 문자열 생성 예시
            /// </summary>
            /// <example>
            /// // Mitsubishi PLC 모니터링 예제
            /// var monitor = new PlcMonitor(mitsubishiPlc);
            /// monitor.AddWord("현재생산수량", Mitsubishi.D, 100);       // D100
            /// monitor.AddBit("운전중신호", Mitsubishi.M, 0);            // M0
            /// monitor.AddBit("이상신호", Mitsubishi.M, 100);            // M100
            /// </example>
            public static string FormatAddress(string device, int address)
            {
                return $"{device}{address}";
            }
        }

        #endregion

        #region Siemens S7 디바이스

        /// <summary>
        /// Siemens S7 시리즈 디바이스 상수
        /// </summary>
        public static class Siemens
        {
            // Area Codes
            /// <summary>입력 영역 (I / E) - IB0, IW0, ID0</summary>
            public const string I = "I";
            /// <summary>출력 영역 (Q / A) - QB0, QW0, QD0</summary>
            public const string Q = "Q";
            /// <summary>메모리/플래그 영역 (M / F) - MB0, MW0, MD0</summary>
            public const string M = "M";
            /// <summary>데이터 블록 (DB) - DB1.DBB0, DB1.DBW0, DB1.DBD0</summary>
            public const string DB = "DB";

            /// <summary>
            /// DB 주소 생성 (바이트 단위)
            /// </summary>
            /// <param name="dbNumber">DB 번호 (예: 1)</param>
            /// <param name="byteOffset">바이트 오프셋 (예: 0)</param>
            /// <returns>DB 디바이스 문자열 (예: "DB1")</returns>
            /// <example>
            /// // Siemens PLC 모니터링 예제
            /// var monitor = new PlcMonitor(siemensPlc);
            /// 
            /// // DB1.DBW0 읽기 (DB1의 바이트 오프셋 0부터 워드 읽기)
            /// monitor.AddWord("설비상태", Siemens.FormatDb(1), 0);       // DB1, 주소 0
            /// 
            /// // M 영역 비트 읽기 (M0.0)
            /// monitor.AddBit("비상정지", Siemens.M, 0);                  // M 영역, 비트주소 0
            /// </example>
            public static string FormatDb(int dbNumber)
            {
                return $"DB{dbNumber}";
            }

            /// <summary>
            /// 비트 주소 계산 (바이트.비트 형식을 비트 주소로 변환)
            /// </summary>
            /// <param name="byteOffset">바이트 오프셋</param>
            /// <param name="bitOffset">비트 오프셋 (0-7)</param>
            /// <returns>비트 주소</returns>
            /// <example>
            /// // M0.5 → 비트주소 5
            /// int bitAddress = Siemens.ToBitAddress(0, 5);  // 5
            /// 
            /// // M10.3 → 비트주소 83
            /// int bitAddress = Siemens.ToBitAddress(10, 3);  // 83
            /// </example>
            public static int ToBitAddress(int byteOffset, int bitOffset)
            {
                return byteOffset * 8 + bitOffset;
            }

            /// <summary>
            /// 비트 주소를 바이트.비트 형식으로 변환
            /// </summary>
            /// <param name="bitAddress">비트 주소</param>
            /// <returns>(바이트 오프셋, 비트 오프셋)</returns>
            public static (int byteOffset, int bitOffset) FromBitAddress(int bitAddress)
            {
                return (bitAddress / 8, bitAddress % 8);
            }

            /// <summary>
            /// 디바이스 정보 조회
            /// </summary>
            public static DeviceInfo GetDeviceInfo(string device)
            {
                string upperDevice = device.ToUpper();
                if (upperDevice == "I" || upperDevice == "E")
                    return new DeviceInfo("입력 영역", true, "바이트.비트");
                if (upperDevice == "Q" || upperDevice == "A")
                    return new DeviceInfo("출력 영역", true, "바이트.비트");
                if (upperDevice == "M" || upperDevice == "F")
                    return new DeviceInfo("메모리/플래그", true, "바이트.비트");
                if (upperDevice.StartsWith("DB"))
                    return new DeviceInfo("데이터 블록", false, "바이트 오프셋");
                return new DeviceInfo("Unknown", false, "");
            }
        }

        #endregion

        #region LS Electric XGT 디바이스

        /// <summary>
        /// LS Electric (구 LG산전) XGT 시리즈 디바이스 상수
        /// </summary>
        public static class LsXgt
        {
            // Bit Devices (비트 디바이스)
            /// <summary>입력 (%IX0.0.0, %I0.0.0)</summary>
            public const string I = "I";
            /// <summary>출력 (%QX0.0.0, %Q0.0.0)</summary>
            public const string Q = "Q";
            /// <summary>내부 릴레이 / 비트 메모리 (%MX0, %M0)</summary>
            public const string M = "M";
            /// <summary>키프 릴레이 (%KX0)</summary>
            public const string K = "K";
            /// <summary>링크 릴레이 (%LX0)</summary>
            public const string L = "L";
            /// <summary>특수 릴레이 (%FX0)</summary>
            public const string F = "F";

            // Word Devices (워드 디바이스)
            /// <summary>데이터 레지스터 (%DW0, %D0)</summary>
            public const string D = "D";
            /// <summary>타이머 현재값 (%TW0)</summary>
            public const string T = "T";
            /// <summary>카운터 현재값 (%CW0)</summary>
            public const string C = "C";
            /// <summary>스텝 컨트롤러 (%SW0)</summary>
            public const string S = "S";
            /// <summary>통신 데이터 레지스터 (%NW0)</summary>
            public const string N = "N";
            /// <summary>파일 레지스터 (%RW0)</summary>
            public const string R = "R";
            /// <summary>특수 레지스터 (%UW0)</summary>
            public const string U = "U";
            /// <summary>링크 레지스터 (%ZW0)</summary>
            public const string Z = "Z";

            /// <summary>
            /// XGT 주소 형식으로 변환
            /// </summary>
            /// <param name="device">디바이스 타입 (M, D, I, Q 등)</param>
            /// <param name="address">주소</param>
            /// <param name="isBit">비트 디바이스 여부</param>
            /// <returns>XGT 형식 주소 문자열</returns>
            /// <example>
            /// // LS XGT PLC 모니터링 예제
            /// var monitor = new PlcMonitor(lsXgtPlc);
            /// 
            /// // 디바이스 문자만 전달하면 됨 (내부적으로 %MW100 형식으로 변환)
            /// monitor.AddWord("현재생산수량", LsXgt.D, 100);    // %DW100 또는 %MW100
            /// monitor.AddBit("운전중신호", LsXgt.M, 0);         // %MX0
            /// </example>
            public static string FormatAddress(string device, int address, bool isBit = false)
            {
                string typeChar = isBit ? "X" : "W";
                return $"%{device.ToUpper()}{typeChar}{address}";
            }

            /// <summary>
            /// 디바이스 정보 조회
            /// </summary>
            public static DeviceInfo GetDeviceInfo(string device)
            {
                switch (device.ToUpper())
                {
                    case "I": return new DeviceInfo("입력", true, "%IX 또는 %IW");
                    case "Q": return new DeviceInfo("출력", true, "%QX 또는 %QW");
                    case "M": return new DeviceInfo("내부 릴레이/메모리", true, "%MX 또는 %MW");
                    case "D": return new DeviceInfo("데이터 레지스터", false, "%DW");
                    case "T": return new DeviceInfo("타이머", false, "%TW");
                    case "C": return new DeviceInfo("카운터", false, "%CW");
                    case "R": return new DeviceInfo("파일 레지스터", false, "%RW");
                    default: return new DeviceInfo("Unknown", false, "");
                }
            }
        }

        #endregion

        #region Modbus 디바이스

        /// <summary>
        /// Modbus 프로토콜 디바이스 상수
        /// </summary>
        public static class Modbus
        {
            // Modbus에서는 Function Code로 디바이스 영역을 구분
            // 유틸리티 클래스에서는 device 파라미터가 무시되고 주소만 사용됨

            /// <summary>코일 (Coil) - FC01/05/0F - 주소 0~65535</summary>
            public const string Coil = "0";
            /// <summary>이산 입력 (Discrete Input) - FC02 - 주소 0~65535</summary>
            public const string DiscreteInput = "1";
            /// <summary>홀딩 레지스터 (Holding Register) - FC03/06/10 - 주소 0~65535</summary>
            public const string HoldingRegister = "4";
            /// <summary>입력 레지스터 (Input Register) - FC04 - 주소 0~65535</summary>
            public const string InputRegister = "3";

            /// <summary>
            /// Modbus 주소 체계 설명
            /// </summary>
            /// <remarks>
            /// Modbus 유틸리티 클래스 사용 시:
            /// - device 파라미터: 사용되지 않음 (빈 문자열 또는 아무 값)
            /// - address 파라미터: 0-based 주소 직접 사용
            /// 
            /// Modbus 주소 표기법 참고 (문서/HMI에서 자주 사용):
            /// - 0xxxxx: Coil (0-based: 주소 그대로)
            /// - 1xxxxx: Discrete Input
            /// - 3xxxxx: Input Register (30001 → 주소 0)
            /// - 4xxxxx: Holding Register (40001 → 주소 0)
            /// </remarks>
            /// <example>
            /// // Modbus PLC 모니터링 예제
            /// var monitor = new PlcMonitor(modbusPlc);
            /// 
            /// // device 파라미터는 무시됨, 주소만 사용
            /// monitor.AddWord("홀딩레지스터0", "", 0);          // Holding Register 40001
            /// monitor.AddWord("홀딩레지스터100", "", 100);      // Holding Register 40101
            /// monitor.AddBit("코일0", "", 0);                   // Coil 00001
            /// 
            /// // 또는 상수 사용 (가독성 목적)
            /// monitor.AddWord("레지스터값", Modbus.HoldingRegister, 100);
            /// </example>
            public static int FromModbusAddress(int modbusAddress)
            {
                // 40001 → 0, 40100 → 99
                if (modbusAddress >= 40001 && modbusAddress <= 49999)
                    return modbusAddress - 40001;
                // 30001 → 0
                if (modbusAddress >= 30001 && modbusAddress <= 39999)
                    return modbusAddress - 30001;
                // 10001 → 0
                if (modbusAddress >= 10001 && modbusAddress <= 19999)
                    return modbusAddress - 10001;
                // 00001 → 0
                if (modbusAddress >= 1 && modbusAddress <= 9999)
                    return modbusAddress - 1;
                return modbusAddress;
            }

            /// <summary>
            /// 0-based 주소를 Modbus 표기 주소로 변환
            /// </summary>
            /// <param name="address">0-based 주소</param>
            /// <param name="functionCode">Function Code (1, 2, 3, 4)</param>
            /// <returns>Modbus 표기 주소</returns>
            public static int ToModbusAddress(int address, int functionCode)
            {
                switch (functionCode)
                {
                    case 1: // Coil
                    case 5:
                    case 15:
                        return address + 1;
                    case 2: // Discrete Input
                        return address + 10001;
                    case 4: // Input Register
                        return address + 30001;
                    case 3: // Holding Register
                    case 6:
                    case 16:
                    default:
                        return address + 40001;
                }
            }
        }

        #endregion

        #region Common

        /// <summary>
        /// 디바이스 정보 구조체
        /// </summary>
        public struct DeviceInfo
        {
            /// <summary>디바이스 설명</summary>
            public string Description { get; }
            /// <summary>비트 디바이스 여부</summary>
            public bool IsBitDevice { get; }
            /// <summary>주소 형식 설명</summary>
            public string AddressFormat { get; }

            public DeviceInfo(string description, bool isBitDevice, string addressFormat)
            {
                Description = description;
                IsBitDevice = isBitDevice;
                AddressFormat = addressFormat;
            }
        }

        #endregion
    }
}
