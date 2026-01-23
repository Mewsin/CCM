# YBComm (YB.dll)

산업용 통신 라이브러리 - MSSQL, Socket, Serial, PLC 통신을 위한 C# DLL

## 개요

Visual Studio 2019 / .NET Framework 4.7.2 기반의 산업용 통신 라이브러리입니다.

## 주요 기능

### 1. Database (MSSQL)
- `MssqlHelper` 클래스
- 연결/해제, 연결 테스트
- SELECT 쿼리 (DataTable, DataSet, Generic List)
- INSERT/UPDATE/DELETE (ExecuteNonQuery)
- 저장 프로시저 호출 (OUTPUT 파라미터 지원)
- 트랜잭션 관리 (BeginTransaction, Commit, Rollback)
- Bulk Insert

### 2. Socket 통신
- **TcpClientHelper**: TCP 클라이언트
  - 연결/해제, 자동 재연결
  - 동기/비동기 송수신
- **UdpHelper**: UDP 클라이언트
  - 송수신, 브로드캐스트

### 3. Serial 통신
- **SerialPortHelper**: 시리얼 포트 통신
  - 포트 열기/닫기
  - 동기/비동기 송수신
  - 사용 가능한 포트 목록 조회

### 4. PLC 통신

| PLC | 클래스 | 프로토콜 | 기본 포트 |
|-----|--------|----------|-----------|
| Mitsubishi | `MitsubishiMcProtocol` | MC Protocol 3E Frame | 5001 |
| Siemens | `SiemensS7Protocol` | S7 Protocol (ISO-on-TCP) | 102 |
| LS Electric | `LsElectricXgt` | XGT FEnet Protocol | 2004 |
| Modbus | `ModbusClient` | Modbus TCP / RTU | 502 |

**공통 기능:**
- Bit 읽기/쓰기 (단일, 연속)
- Word 읽기/쓰기 (16비트)
- DWord 읽기/쓰기 (32비트)
- Real 읽기/쓰기 (32비트 Float)
- String 읽기/쓰기

## 프로젝트 구조

```
YBComm/
├── IndustrialCommunication.sln
├── IndustrialCommunication/           # DLL 프로젝트 (YB.dll)
│   ├── Database/
│   │   └── MssqlHelper.cs
│   ├── Communication/
│   │   ├── Interfaces/
│   │   │   ├── ICommunication.cs
│   │   │   └── IPlcCommunication.cs
│   │   ├── Socket/
│   │   │   ├── TcpClientHelper.cs
│   │   │   └── UdpHelper.cs
│   │   ├── Serial/
│   │   │   └── SerialPortHelper.cs
│   │   └── PLC/
│   │       ├── PlcBase.cs
│   │       ├── MitsubishiMcProtocol.cs
│   │       ├── SiemensS7Protocol.cs
│   │       ├── LsElectricXgt.cs
│   │       └── ModbusClient.cs
│   └── Properties/
└── IndustrialCommunication.Example/   # WinForm 예제
    ├── MainForm.cs
    └── Program.cs
```

## 사용 방법

### 빌드
1. Visual Studio 2019에서 `IndustrialCommunication.sln` 열기
2. Release 모드로 빌드
3. `IndustrialCommunication\bin\Release\YB.dll` 생성됨

### 예제 코드

#### MSSQL
```csharp
using IndustrialCommunication.Database;

var db = new MssqlHelper("Server=localhost;Database=TestDB;User Id=sa;Password=1234;");

// SELECT
DataTable dt = db.ExecuteQuery("SELECT * FROM Users WHERE Id = @Id",
    MssqlHelper.CreateParameter("@Id", 1));

// INSERT/UPDATE/DELETE
int affected = db.ExecuteNonQuery("UPDATE Users SET Name = @Name WHERE Id = @Id",
    MssqlHelper.CreateParameter("@Name", "홍길동"),
    MssqlHelper.CreateParameter("@Id", 1));

// 트랜잭션
db.BeginTransaction();
try
{
    db.ExecuteNonQuery("INSERT INTO ...");
    db.ExecuteNonQuery("UPDATE ...");
    db.Commit();
}
catch
{
    db.Rollback();
    throw;
}
```

#### TCP Client
```csharp
using IndustrialCommunication.Communication.Socket;

var tcp = new TcpClientHelper("192.168.0.100", 8000);
tcp.Connect();
tcp.Send(new byte[] { 0x01, 0x02, 0x03 });
byte[] response = tcp.SendAndReceive(new byte[] { 0x01 }, timeout: 3000);
tcp.Disconnect();
```

#### Mitsubishi PLC
```csharp
using IndustrialCommunication.Communication.PLC;

var plc = new MitsubishiMcProtocol("192.168.0.10", 5001);
plc.Connect();

// D100 읽기
var result = plc.ReadWord("D", 100);
if (result.IsSuccess)
    Console.WriteLine($"D100 = {result.Value}");

// D100~D109 연속 읽기
var words = plc.ReadWords("D", 100, 10);

// M100 비트 쓰기
plc.WriteBit("M", 100, true);

plc.Disconnect();
```

#### Siemens PLC
```csharp
var plc = new SiemensS7Protocol("192.168.0.10", S7CpuType.S71200, rack: 0, slot: 1);
plc.Connect();

// DB1.DBW0 읽기
var result = plc.ReadWord("DB1", 0);

plc.Disconnect();
```

#### Modbus
```csharp
// Modbus TCP
var modbus = new ModbusClient("192.168.0.10", 502, slaveAddress: 1);
modbus.Connect();

// 홀딩 레지스터 읽기 (FC03)
var registers = modbus.ReadHoldingRegisters(0, 10);

// 코일 쓰기 (FC05)
modbus.WriteSingleCoil(0, true);

modbus.Disconnect();
```

## 라이선스

MIT License
