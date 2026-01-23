# CCM (Common Communication Module)

공통 통신 모듈 - MSSQL, Socket, Serial, PLC 통신을 위한 C# DLL

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
- **TcpServerHelper**: TCP 서버
  - 멀티 클라이언트 지원
  - 클라이언트 연결/해제 이벤트
  - 개별/전체 브로드캐스트 전송
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
CCM/
├── CCM.sln
├── CCM/                            # DLL 프로젝트 (CCM.dll)
│   ├── Database/
│   │   └── MssqlHelper.cs
│   ├── Communication/
│   │   ├── Interfaces/
│   │   │   ├── ICommunication.cs
│   │   │   └── IPlcCommunication.cs
│   │   ├── Socket/
│   │   │   ├── TcpServerHelper.cs
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
└── CCM.Example/                    # WinForm 예제
    ├── MainForm.cs
    └── Program.cs
```

## 사용 방법

### 빌드
1. Visual Studio 2019에서 `CCM.sln` 열기
2. Release 모드로 빌드
3. `CCM\bin\Release\CCM.dll` 생성됨

### 예제 코드

#### MSSQL
```csharp
using CCM.Database;

var db = new MssqlHelper("Server=localhost;Database=TestDB;User Id=sa;Password=1234;");

// SELECT
DataTable dt = db.ExecuteQuery("SELECT * FROM Users WHERE Id = @Id",
    MssqlHelper.CreateParameter("@Id", 1));

// INSERT/UPDATE/DELETE
int affected = db.ExecuteNonQuery("UPDATE Users SET Name = @Name WHERE Id = @Id",
    MssqlHelper.CreateParameter("@Name", "홍길동"),
    MssqlHelper.CreateParameter("@Id", 1));

// 저장 프로시저 호출 (결과 반환)
DataTable dtProc = db.ExecuteProcedure("usp_GetUserList",
    MssqlHelper.CreateParameter("@DeptId", 10));

// 저장 프로시저 호출 (NonQuery)
int procResult = db.ExecuteProcedureNonQuery("usp_UpdateUserStatus",
    MssqlHelper.CreateParameter("@UserId", 1),
    MssqlHelper.CreateParameter("@Status", "Active"));

// 저장 프로시저 호출 (OUTPUT 파라미터)
var parameters = new[]
{
    MssqlHelper.CreateParameter("@UserId", 1),
    MssqlHelper.CreateParameter("@UserName", SqlDbType.NVarChar, null) { Direction = ParameterDirection.Output, Size = 50 },
    MssqlHelper.CreateOutputParameter("@TotalCount", SqlDbType.Int)
};
db.ExecuteProcedureWithOutput("usp_GetUserInfo", parameters, out var outputValues);
string userName = outputValues["@UserName"]?.ToString();
int totalCount = Convert.ToInt32(outputValues["@TotalCount"]);

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

// Bulk Insert (대량 데이터 삽입)
DataTable bulkData = new DataTable();
bulkData.Columns.Add("Id", typeof(int));
bulkData.Columns.Add("Name", typeof(string));
bulkData.Columns.Add("Value", typeof(decimal));

for (int i = 0; i < 10000; i++)
{
    bulkData.Rows.Add(i, $"Item_{i}", i * 1.5m);
}

db.BulkInsert(bulkData, "TargetTable", batchSize: 1000);
```

#### TCP Server
```csharp
using CCM.Communication.Socket;

var server = new TcpServerHelper(9000);

// 이벤트 등록
server.ClientConnected += (s, e) => Console.WriteLine($"클라이언트 연결: {e.ClientId}");
server.ClientDisconnected += (s, e) => Console.WriteLine($"클라이언트 해제: {e.ClientId}");
server.ClientDataReceived += (s, e) => Console.WriteLine($"수신 [{e.ClientId}]: {BitConverter.ToString(e.Data)}");

// 서버 시작
server.Start();

// 특정 클라이언트에게 전송
server.SendTo("clientId", new byte[] { 0x01, 0x02, 0x03 });

// 모든 클라이언트에게 브로드캐스트
server.SendToAll(new byte[] { 0x01, 0x02, 0x03 });

// 클라이언트 연결 해제
server.DisconnectClient("clientId");

// 서버 중지
server.Stop();
```

#### TCP Client
```csharp
using CCM.Communication.Socket;

var tcp = new TcpClientHelper("192.168.0.100", 8000);
tcp.Connect();
tcp.Send(new byte[] { 0x01, 0x02, 0x03 });
byte[] response = tcp.SendAndReceive(new byte[] { 0x01 }, timeout: 3000);
tcp.Disconnect();
```

#### Mitsubishi PLC
```csharp
using CCM.Communication.PLC;

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

#### LS Electric XGT
```csharp
var plc = new LsElectricXgt("192.168.0.10", 2004);
plc.Connect();

// %MW100 워드 읽기
var result = plc.ReadWord("%MW", 100);
if (result.IsSuccess)
    Console.WriteLine($"%MW100 = {result.Value}");

// %MW100~%MW109 연속 읽기
var words = plc.ReadWords("%MW", 100, 10);

// %MX100 비트 쓰기
plc.WriteBit("%MX", 100, true);

// %MW100에 값 쓰기
plc.WriteWord("%MW", 100, 1234);

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
