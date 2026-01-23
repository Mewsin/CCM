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

// 연결 문자열로 생성
var db = new MssqlHelper("Server=localhost;Database=TestDB;User Id=sa;Password=1234;");

// 또는 개별 파라미터로 생성
var db2 = new MssqlHelper("localhost", "TestDB", "sa", "1234");

// 연결 테스트
if (db.TestConnection())
    Console.WriteLine("연결 성공");

// SELECT - DataTable 반환
DataTable dt = db.ExecuteQuery("SELECT * FROM Users WHERE Id = @Id",
    MssqlHelper.CreateParameter("@Id", 1));

// SELECT - DataSet 반환 (여러 결과셋)
DataSet ds = db.ExecuteQueryDataSet("SELECT * FROM Users; SELECT * FROM Orders");

// SELECT - Generic List 반환
List<User> users = db.ExecuteQuery("SELECT * FROM Users", reader => new User
{
    Id = MssqlHelper.GetValue<int>(reader, "Id"),
    Name = MssqlHelper.GetValue<string>(reader, "Name"),
    Email = MssqlHelper.GetValue<string>(reader, "Email", "기본값")
});

// 단일 값 조회
int count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");

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
tcp.ConnectTimeout = 5000;  // 연결 타임아웃 설정
tcp.AutoReconnect = true;   // 자동 재연결 활성화

// 이벤트 등록 (비동기 수신 모드)
tcp.ConnectionStateChanged += (s, e) => Console.WriteLine($"연결 상태: {e.IsConnected}");
tcp.DataReceived += (s, e) => Console.WriteLine($"수신: {BitConverter.ToString(e.Data)}");
tcp.ErrorOccurred += (s, e) => Console.WriteLine($"오류: {e.Message}");

tcp.UseAsyncReceive = true;  // 비동기 수신 모드 활성화
tcp.Connect();

// 데이터 전송
tcp.Send(new byte[] { 0x01, 0x02, 0x03 });

// 전송 후 응답 대기 (동기)
byte[] response = tcp.SendAndReceive(new byte[] { 0x01 }, timeout: 3000);

// 지정 길이만큼 수신
byte[] exactData = tcp.ReceiveExact(10, timeout: 3000);

tcp.Disconnect();
```

#### UDP
```csharp
using CCM.Communication.Socket;

// UDP 클라이언트 생성 (원격IP, 원격포트, 로컬포트)
var udp = new UdpHelper("192.168.0.100", 8001, 8002);

// 이벤트 등록
udp.DataReceived += (s, e) => Console.WriteLine($"수신: {BitConverter.ToString(e.Data)}");

udp.Connect();

// 데이터 전송
udp.Send(new byte[] { 0x01, 0x02, 0x03 });

// 브로드캐스트 전송
udp.SendBroadcast(new byte[] { 0xFF, 0xFF }, 8001);

udp.Disconnect();
```

#### Serial Port
```csharp
using CCM.Communication.Serial;
using System.IO.Ports;

// 사용 가능한 포트 목록 조회
string[] ports = SerialPortHelper.GetAvailablePorts();

var serial = new SerialPortHelper
{
    PortName = "COM1",
    BaudRate = 9600,
    DataBits = 8,
    StopBits = StopBits.One,
    Parity = Parity.None
};

// 이벤트 등록 (비동기 수신)
serial.DataReceived += (s, e) => Console.WriteLine($"수신: {BitConverter.ToString(e.Data)}");
serial.UseAsyncReceive = true;

serial.Connect();

// 데이터 전송
serial.Send(new byte[] { 0x01, 0x02, 0x03 });

// 전송 후 응답 대기 (동기)
byte[] response = serial.SendAndReceive(new byte[] { 0x01 }, timeout: 1000);

serial.Disconnect();
```

#### Mitsubishi PLC
```csharp
using CCM.Communication.PLC;

var plc = new MitsubishiMcProtocol("192.168.0.10", 5001);
plc.Connect();

// D100 워드 읽기
var result = plc.ReadWord("D", 100);
if (result.IsSuccess)
    Console.WriteLine($"D100 = {result.Value}");

// D100~D109 연속 읽기 (10개)
var words = plc.ReadWords("D", 100, 10);

// M100 비트 읽기
var bit = plc.ReadBit("M", 100);

// D100 DWord(32비트) 읽기
var dword = plc.ReadDWord("D", 100);

// D100 Real(Float) 읽기
var realValue = plc.ReadReal("D", 100);

// D100에 워드 쓰기
plc.WriteWord("D", 100, 1234);

// M100 비트 쓰기
plc.WriteBit("M", 100, true);

// D100에 DWord 쓰기
plc.WriteDWord("D", 100, 123456789);

// D100에 Real(Float) 쓰기
plc.WriteReal("D", 100, 3.14f);

plc.Disconnect();
```

#### Siemens PLC
```csharp
using CCM.Communication.PLC;

// CPU 타입: S7200, S7300, S7400, S71200, S71500
var plc = new SiemensS7Protocol("192.168.0.10", S7CpuType.S71200, rack: 0, slot: 1);
plc.Connect();

// DB1.DBW0 워드 읽기
var result = plc.ReadWord("DB1", 0);
if (result.IsSuccess)
    Console.WriteLine($"DB1.DBW0 = {result.Value}");

// DB1.DBW0~DBW18 연속 읽기 (10개)
var words = plc.ReadWords("DB1", 0, 10);

// DB1.DBX0.0 비트 읽기
var bit = plc.ReadBit("DB1", 0);

// DB1.DBD0 DWord(32비트) 읽기
var dword = plc.ReadDWord("DB1", 0);

// DB1.DBW100에 값 쓰기
plc.WriteWord("DB1", 100, 1234);

// DB1.DBX0.0 비트 쓰기
plc.WriteBit("DB1", 0, true);

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
using CCM.Communication.PLC;
using System.IO.Ports;

// Modbus TCP
var modbusTcp = new ModbusClient("192.168.0.10", 502, slaveAddress: 1);
modbusTcp.Connect();

// 코일 읽기 (FC01)
var coils = modbusTcp.ReadCoils(0, 10);

// 입력 레지스터 읽기 (FC04)
var inputs = modbusTcp.ReadInputRegisters(0, 10);

// 홀딩 레지스터 읽기 (FC03)
var registers = modbusTcp.ReadHoldingRegisters(0, 10);

// 단일 코일 쓰기 (FC05)
modbusTcp.WriteSingleCoil(0, true);

// 단일 레지스터 쓰기 (FC06)
modbusTcp.WriteSingleRegister(0, 1234);

// 다중 코일 쓰기 (FC15)
modbusTcp.WriteMultipleCoils(0, new bool[] { true, false, true });

// 다중 레지스터 쓰기 (FC16)
modbusTcp.WriteMultipleRegisters(0, new short[] { 100, 200, 300 });

modbusTcp.Disconnect();

// Modbus RTU (시리얼)
var modbusRtu = new ModbusClient("COM1", 9600, Parity.None, slaveAddress: 1);
modbusRtu.Mode = ModbusMode.Rtu;
modbusRtu.Connect();

var rtuRegisters = modbusRtu.ReadHoldingRegisters(0, 10);

modbusRtu.Disconnect();
```

## 라이선스

MIT License
