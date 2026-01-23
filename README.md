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

### 5. PLC 유틸리티

| 클래스 | 설명 |
|--------|------|
| `PlcDeviceHelper` | PLC별 디바이스 주소 상수 및 헬퍼 (Mitsubishi, Siemens, LS XGT, Modbus) |
| `PlcMonitor` | PLC 데이터 주기적 모니터링 및 변경 감지 |
| `RecipeManager` | 레시피 데이터 업로드/다운로드 (XML 지원) |
| `HandshakeHelper` | PC↔PLC 명령-응답 핸드쉐이크 |
| `AlarmManager` | 알람 비트 파싱 및 이력 관리 |
| `ProductionLogger` | 생산 데이터 수집 및 DB 저장 |

> **참고**: 모든 유틸리티 클래스는 `IPlcCommunication` 인터페이스를 사용하므로 **모든 PLC(Mitsubishi, Siemens, LS, Modbus)에서 동일하게 사용** 가능합니다. 각 PLC별 디바이스 주소 표기법은 `PlcDeviceHelper` 클래스를 참조하세요.

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
│   │       ├── ModbusClient.cs
│   │       └── Utilities/
│   │           ├── PlcDeviceHelper.cs
│   │           ├── PlcMonitor.cs
│   │           ├── RecipeManager.cs
│   │           ├── HandshakeHelper.cs
│   │           ├── AlarmManager.cs
│   │           └── ProductionLogger.cs
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

---

## 예제 코드

<details>
<summary><b>MSSQL</b></summary>

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

</details>

<details>
<summary><b>TCP Server</b></summary>

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

</details>

<details>
<summary><b>TCP Client</b></summary>

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

</details>

<details>
<summary><b>다중 소켓 관리 (배열/Dictionary)</b></summary>

여러 설비와 동시에 통신해야 할 때 소켓을 배열이나 Dictionary로 관리할 수 있습니다.

### 1. 배열 방식 (고정 개수)

```csharp
// 클라이언트 5개 고정
TcpClientHelper[] clients = new TcpClientHelper[5];

for (int i = 0; i < 5; i++)
{
    clients[i] = new TcpClientHelper($"192.168.0.{10 + i}", 8000);
    clients[i].Connect();
}

// 사용
clients[0].Send(data);
clients[2].SendAndReceive(data);
```

### 2. Dictionary 방식 (권장 - 이름으로 관리)

```csharp
Dictionary<string, TcpClientHelper> clients = new Dictionary<string, TcpClientHelper>();

// 설비별로 추가
clients["설비A"] = new TcpClientHelper("192.168.0.10", 8000);
clients["설비B"] = new TcpClientHelper("192.168.0.11", 8000);
clients["검사기"] = new TcpClientHelper("192.168.0.20", 9000);

// 전체 연결
foreach (var client in clients.Values)
    client.Connect();

// 사용
clients["설비A"].Send(data);
clients["검사기"].SendAndReceive(data);
```

### 3. List 방식 (동적 추가/제거)

```csharp
List<TcpClientHelper> clients = new List<TcpClientHelper>();

// 동적 추가
clients.Add(new TcpClientHelper("192.168.0.10", 8000));
clients.Add(new TcpClientHelper("192.168.0.11", 8000));

// 전체 전송
foreach (var client in clients)
{
    if (client.IsConnected)
        client.Send(data);
}
```

### 4. 실전 예시: 소켓 매니저 클래스

```csharp
public class SocketManager : IDisposable
{
    private Dictionary<string, TcpClientHelper> _clients = new Dictionary<string, TcpClientHelper>();

    public void Add(string name, string ip, int port)
    {
        var client = new TcpClientHelper(ip, port);
        client.AutoReconnect = true;
        _clients[name] = client;
    }

    public void ConnectAll()
    {
        foreach (var client in _clients.Values)
            client.Connect();
    }

    public TcpClientHelper this[string name] => _clients[name];

    public void SendToAll(byte[] data)
    {
        foreach (var client in _clients.Values)
            if (client.IsConnected) client.Send(data);
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values)
            client.Dispose();
    }
}

// 사용
var sockets = new SocketManager();
sockets.Add("라인1", "192.168.0.10", 8000);
sockets.Add("라인2", "192.168.0.11", 8000);
sockets.Add("라인3", "192.168.0.12", 8000);
sockets.ConnectAll();

sockets["라인1"].Send(data);
sockets.SendToAll(broadcastData);
```

> **Tip**: Dictionary가 이름으로 접근 가능해서 실무에서 가장 편리합니다.

</details>

<details>
<summary><b>UDP</b></summary>

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

</details>

<details>
<summary><b>Serial Port</b></summary>

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

</details>

<details>
<summary><b>Mitsubishi PLC</b></summary>

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

</details>

<details>
<summary><b>Siemens PLC</b></summary>

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

</details>

<details>
<summary><b>LS Electric XGT</b></summary>

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

</details>

<details>
<summary><b>Modbus</b></summary>

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

</details>

---

## PLC별 디바이스 주소 가이드

<details>
<summary><b>PlcDeviceHelper (디바이스 주소 헬퍼)</b></summary>

### 개념

PLC마다 **디바이스 표기법이 다릅니다**. `PlcDeviceHelper` 클래스는 각 PLC 제조사별 디바이스 상수와 변환 메서드를 제공합니다.

| PLC | 비트 디바이스 | 워드 디바이스 |
|-----|--------------|--------------|
| **Mitsubishi** | X, Y, M, B, L | D, W, R |
| **Siemens** | I, Q, M (바이트.비트) | DB (바이트 오프셋) |
| **LS XGT** | %IX, %QX, %MX | %DW, %MW |
| **Modbus** | Coil (0xxxxx) | Holding Register (4xxxxx) |

### Mitsubishi MELSEC

```csharp
using CCM.Communication.PLC.Utilities;
using static CCM.Communication.PLC.Utilities.PlcDeviceHelper;

// ============================================
// Mitsubishi 디바이스 상수 사용
// ============================================

var plc = new MitsubishiMcProtocol("192.168.0.10", 5001);
plc.Connect();

var monitor = new PlcMonitor(plc);

// 상수 사용 (권장)
monitor.AddWord("현재생산수량", Mitsubishi.D, 100);     // D100
monitor.AddWord("목표생산수량", Mitsubishi.D, 101);     // D101
monitor.AddBit("운전중", Mitsubishi.M, 0);              // M0
monitor.AddBit("이상발생", Mitsubishi.M, 100);          // M100

// 직접 문자열 사용도 가능
monitor.AddWord("온도", "D", 200);
monitor.AddBit("입력신호", "X", 0);    // 8진수: X0, X1, ..., X7, X10, ...
monitor.AddBit("출력신호", "Y", 0);

// 디바이스 정보 조회
var info = Mitsubishi.GetDeviceInfo("D");
Console.WriteLine($"{info.Description}, 비트: {info.IsBitDevice}, 형식: {info.AddressFormat}");
// 출력: 데이터 레지스터, 비트: False, 형식: 10진수

monitor.Start();
```

### Siemens S7

```csharp
using CCM.Communication.PLC.Utilities;
using static CCM.Communication.PLC.Utilities.PlcDeviceHelper;

// ============================================
// Siemens S7 디바이스 (바이트 기반 주소)
// ============================================

var plc = new SiemensS7Protocol("192.168.0.10", S7CpuType.S71200);
plc.Connect();

var monitor = new PlcMonitor(plc);

// DB (Data Block) 사용 - 가장 일반적
// DB1.DBW0 (바이트 오프셋 0부터 워드 읽기)
monitor.AddWord("설비상태", Siemens.FormatDb(1), 0);    // "DB1", 주소 0

// DB1.DBD10 (바이트 오프셋 10부터 DWord 읽기)
monitor.AddItem(new MonitorItem {
    Name = "생산수량",
    Device = Siemens.FormatDb(1),  // "DB1"
    Address = 10,                   // 바이트 오프셋 10
    DataType = MonitorDataType.DWord
});

// M 영역 (Merker/Flag)
// M0.0 비트 = 비트주소 0
monitor.AddBit("시작버튼", Siemens.M, Siemens.ToBitAddress(0, 0));  // M0.0 → 비트 0

// M10.5 비트 = 비트주소 85 (10*8 + 5)
monitor.AddBit("정지버튼", Siemens.M, Siemens.ToBitAddress(10, 5)); // M10.5 → 비트 85

// I/Q 영역 (입출력)
monitor.AddBit("입력0", Siemens.I, Siemens.ToBitAddress(0, 0));  // I0.0
monitor.AddBit("출력0", Siemens.Q, Siemens.ToBitAddress(0, 0));  // Q0.0

// 비트 주소 역변환
var (byteOff, bitOff) = Siemens.FromBitAddress(85);
Console.WriteLine($"비트 85 = M{byteOff}.{bitOff}");  // M10.5

monitor.Start();
```

### LS Electric XGT

```csharp
using CCM.Communication.PLC.Utilities;
using static CCM.Communication.PLC.Utilities.PlcDeviceHelper;

// ============================================
// LS XGT 디바이스 (%MW, %DW 형식)
// ============================================

var plc = new LsElectricXgt("192.168.0.10", 2004);
plc.Connect();

var monitor = new PlcMonitor(plc);

// 워드 디바이스 (내부적으로 %MW100 형식으로 변환됨)
monitor.AddWord("생산수량", LsXgt.M, 100);    // → %MW100
monitor.AddWord("데이터", LsXgt.D, 0);        // → %DW0

// 비트 디바이스
monitor.AddBit("운전중", LsXgt.M, 0);          // → %MX0
monitor.AddBit("입력0", LsXgt.I, 0);           // → %IX0
monitor.AddBit("출력0", LsXgt.Q, 0);           // → %QX0

// XGT 주소 형식 문자열 생성
string addr1 = LsXgt.FormatAddress("M", 100, isBit: false);  // "%MW100"
string addr2 = LsXgt.FormatAddress("M", 0, isBit: true);     // "%MX0"
Console.WriteLine($"워드: {addr1}, 비트: {addr2}");

// 디바이스 정보
var info = LsXgt.GetDeviceInfo("M");
Console.WriteLine($"{info.Description}, 형식: {info.AddressFormat}");
// 출력: 내부 릴레이/메모리, 형식: %MX 또는 %MW

monitor.Start();
```

### Modbus

```csharp
using CCM.Communication.PLC.Utilities;
using static CCM.Communication.PLC.Utilities.PlcDeviceHelper;

// ============================================
// Modbus (device 파라미터 무시, 주소만 사용)
// ============================================

var plc = new ModbusClient("192.168.0.10", 502, slaveAddress: 1);
plc.Connect();

var monitor = new PlcMonitor(plc);

// Modbus에서는 device 파라미터가 무시됨 (빈 문자열 또는 아무 값)
// 주소는 0-based로 직접 지정

// Holding Register (FC03) - 40001~
monitor.AddWord("레지스터0", "", 0);           // 40001 → 주소 0
monitor.AddWord("레지스터100", "", 100);       // 40101 → 주소 100

// Coil (FC01) - 00001~
monitor.AddBit("코일0", "", 0);                // 00001 → 주소 0
monitor.AddBit("코일10", "", 10);              // 00011 → 주소 10

// Modbus 문서 주소를 0-based로 변환
int addr1 = Modbus.FromModbusAddress(40001);   // 0
int addr2 = Modbus.FromModbusAddress(40101);   // 100
int addr3 = Modbus.FromModbusAddress(30001);   // 0 (Input Register)

Console.WriteLine($"40001 → {addr1}, 40101 → {addr2}, 30001 → {addr3}");

// 0-based 주소를 Modbus 표기로 변환
int mbAddr = Modbus.ToModbusAddress(100, 3);   // FC03 → 40101
Console.WriteLine($"0-based 100 (FC03) → {mbAddr}");

monitor.Start();
```

### 디바이스 비교 표

| 항목 | Mitsubishi | Siemens | LS XGT | Modbus |
|------|-----------|---------|--------|--------|
| 워드 0 | D0 | DB1.DBW0 | %MW0 | 40001 (주소 0) |
| 워드 100 | D100 | DB1.DBW100 | %MW100 | 40101 (주소 100) |
| 비트 0 | M0 | M0.0 (비트 0) | %MX0 | 00001 (주소 0) |
| 비트 8 | M8 | M1.0 (비트 8) | %MX8 | 00009 (주소 8) |
| 입력 | X0 | I0.0 | %IX0 | 10001 (FC02) |
| 출력 | Y0 | Q0.0 | %QX0 | 00001 (FC05) |

</details>

---

## PLC 유틸리티 상세 가이드

<details>
<summary><b>PlcMonitor (데이터 모니터링)</b></summary>

### 개념

PLC 데이터를 **주기적으로 읽어서 변경을 감지**하는 클래스입니다.

```
┌─────────────────────────────────────────────────────────────┐
│                      PlcMonitor 동작                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   [500ms]     [500ms]     [500ms]     [500ms]              │
│      ↓           ↓           ↓           ↓                 │
│   ┌─────┐    ┌─────┐    ┌─────┐    ┌─────┐               │
│   │READ │    │READ │    │READ │    │READ │  ← PLC 폴링   │
│   │D100 │    │D100 │    │D100 │    │D100 │               │
│   │=100 │    │=100 │    │=150 │    │=150 │               │
│   └─────┘    └─────┘    └──┬──┘    └─────┘               │
│                            │                               │
│                            ↓                               │
│                    DataChanged 이벤트!                     │
│                    "D100: 100 → 150"                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 사용 상황

| 상황 | 설명 |
|------|------|
| 실시간 모니터링 화면 | 온도, 압력, 속도 등을 UI에 표시 |
| 상태 변경 감지 | 운전/정지 상태, 알람 발생 등 |
| 데이터 로깅 트리거 | 값이 바뀔 때만 기록 |

### 예제 코드

```csharp
using CCM.Communication.PLC;
using CCM.Communication.PLC.Utilities;

var plc = new MitsubishiMcProtocol("192.168.0.10", 5001);
plc.Connect();

// ============================================
// 1. 모니터 생성 및 설정
// ============================================
var monitor = new PlcMonitor(plc)
{
    PollingInterval = 500,     // 500ms마다 PLC 읽기
    ContinueOnError = true     // 에러 발생해도 계속 모니터링
};

// ============================================
// 2. 모니터링 항목 추가
// ============================================

// 개별 워드 모니터링
monitor.AddWord("Temperature", "D", 100);   // D100: 온도
monitor.AddWord("Pressure", "D", 101);      // D101: 압력

// 비트 모니터링
monitor.AddBit("Running", "M", 100);        // M100: 운전 중 여부

// 연속 워드 모니터링 (배열)
monitor.AddWords("ProductData", "D", 200, 10);  // D200~D209: 제품 데이터 10개

// 상세 설정이 필요한 경우
monitor.AddItem(new MonitorItem
{
    Name = "CycleTime",
    Device = "D",
    Address = 300,
    DataType = MonitorDataType.DWord,  // 32비트 정수
    Count = 1
});

// ============================================
// 3. 이벤트 등록
// ============================================

// 값이 변경될 때마다 호출
monitor.DataChanged += (s, e) =>
{
    Console.WriteLine($"[변경] {e.Item.Name}: {e.OldValue} → {e.NewValue}");
    Console.WriteLine($"       시간: {e.Timestamp}");
    
    // 특정 조건에 따른 처리
    if (e.Item.Name == "Running" && (bool)e.NewValue == false)
    {
        Console.WriteLine(">>> 설비 정지 감지!");
    }
};

// 폴링 완료 시 호출 (모든 항목 읽기 완료)
monitor.PollingCompleted += (s, e) =>
{
    // UI 갱신 등
    UpdateUI();
};

// 에러 발생 시 호출
monitor.ErrorOccurred += (s, e) =>
{
    Console.WriteLine($"[에러] {e.Item?.Name}: {e.Message}");
};

// ============================================
// 4. 모니터링 시작/중지
// ============================================

monitor.Start();  // 백그라운드 스레드에서 폴링 시작

// ... 프로그램 실행 중 ...

// 현재 값 조회
var tempItem = monitor.GetItem("Temperature");
if (tempItem != null)
{
    Console.WriteLine($"현재 온도: {tempItem.CurrentValue}");
    Console.WriteLine($"이전 온도: {tempItem.PreviousValue}");
    Console.WriteLine($"마지막 업데이트: {tempItem.LastUpdated}");
}

// 모든 항목 조회
var allItems = monitor.GetAllItems();
foreach (var item in allItems)
{
    Console.WriteLine($"{item.Name} = {item.CurrentValue}");
}

// 수동으로 한 번만 폴링 (Start 안 해도 됨)
monitor.PollOnce();

// 모니터링 중지
monitor.Stop();
monitor.Dispose();
```

### 각 PLC별 사용 예제

```csharp
using CCM.Communication.PLC;
using CCM.Communication.PLC.Utilities;
using static CCM.Communication.PLC.Utilities.PlcDeviceHelper;

// ============================================
// Siemens S7 PLC
// ============================================
var siemensPlc = new SiemensS7Protocol("192.168.0.10", S7CpuType.S71200);
siemensPlc.Connect();

var siemensMonitor = new PlcMonitor(siemensPlc);
siemensMonitor.AddWord("온도", Siemens.FormatDb(1), 0);           // DB1.DBW0
siemensMonitor.AddWord("압력", Siemens.FormatDb(1), 2);           // DB1.DBW2
siemensMonitor.AddBit("운전중", Siemens.M, Siemens.ToBitAddress(0, 0));  // M0.0
siemensMonitor.Start();

// ============================================
// LS Electric XGT PLC
// ============================================
var lsPlc = new LsElectricXgt("192.168.0.20", 2004);
lsPlc.Connect();

var lsMonitor = new PlcMonitor(lsPlc);
lsMonitor.AddWord("생산수량", LsXgt.D, 100);    // %DW100
lsMonitor.AddWord("목표수량", LsXgt.D, 101);    // %DW101
lsMonitor.AddBit("운전신호", LsXgt.M, 0);       // %MX0
lsMonitor.Start();

// ============================================
// Modbus TCP
// ============================================
var modbusPlc = new ModbusClient("192.168.0.30", 502, slaveAddress: 1);
modbusPlc.Connect();

var modbusMonitor = new PlcMonitor(modbusPlc);
// Modbus는 device 무시, 주소만 사용 (0-based)
modbusMonitor.AddWord("레지스터0", "", 0);      // 40001
modbusMonitor.AddWord("레지스터10", "", 10);    // 40011
modbusMonitor.AddBit("코일0", "", 0);           // 00001
modbusMonitor.Start();
```

### 데이터 타입

| 타입 | 설명 | PLC 예시 |
|------|------|----------|
| `Bit` | 1비트 (true/false) | M100, X0, Y0 |
| `Word` | 16비트 정수 | D100 (1워드) |
| `DWord` | 32비트 정수 | D100-D101 (2워드) |
| `Real` | 32비트 실수 | D100-D101 (2워드, Float) |
| `Words` | 16비트 정수 배열 | D100~D109 (연속) |

</details>

<details>
<summary><b>RecipeManager (레시피 관리)</b></summary>

### 개념

**레시피**란 제품 생산에 필요한 **설정값 모음**입니다. 제품마다 다른 설정(온도, 압력, 시간 등)을 파일로 저장하고, 필요할 때 PLC에 한 번에 전송합니다.

```
┌─────────────────────────────────────────────────────────────┐
│                    RecipeManager 개념                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   [XML 파일]              [RecipeManager]           [PLC]   │
│                                                             │
│   Recipe_A.xml  ─────┐                                     │
│   - 온도: 80°C       │      ┌──────────┐      ┌───────┐   │
│   - 압력: 5bar       ├────► │          │      │ D1000 │   │
│   - 시간: 30초       │      │  Upload  │ ───► │ D1001 │   │
│                      │      │          │      │ D1002 │   │
│   Recipe_B.xml  ─────┘      │          │      │  ...  │   │
│   - 온도: 120°C             │          │      └───────┘   │
│   - 압력: 8bar              │          │                   │
│   - 시간: 45초              │ Download │ ◄─── PLC에서     │
│                             │          │      현재값 읽기  │
│                             └──────────┘                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 사용 상황

| 상황 | 설명 |
|------|------|
| 제품 변경 | A제품 → B제품 전환 시 레시피 교체 |
| 설정 백업 | 현재 PLC 설정을 파일로 저장 |
| 설정 복원 | 저장된 파일을 PLC에 전송 |
| 설정 비교 | 파일과 PLC 값이 같은지 확인 |

### 예제 코드

```csharp
using CCM.Communication.PLC;
using CCM.Communication.PLC.Utilities;

var plc = new MitsubishiMcProtocol("192.168.0.10", 5001);
plc.Connect();

// ============================================
// 1. RecipeManager 생성 및 설정
// ============================================
var recipe = new RecipeManager(plc)
{
    RecipeDevice = "D",           // 레시피 저장 디바이스
    RecipeStartAddress = 1000,    // 시작 주소: D1000
    RecipeWordCount = 50          // 전체 크기: D1000~D1049 (50워드)
};

// ============================================
// 2. 레시피 항목 정의 (PLC 주소 매핑)
// ============================================
// AddItem(이름, 오프셋, 데이터타입, 설명)
// 오프셋: RecipeStartAddress로부터의 상대 위치

recipe.AddItem("SetTemp", 0, RecipeDataType.Real, "설정 온도");
// → D1000~D1001 (Real = 2워드)

recipe.AddItem("SetPressure", 2, RecipeDataType.Real, "설정 압력");
// → D1002~D1003

recipe.AddItem("CycleTime", 4, RecipeDataType.DWord, "사이클 타임(ms)");
// → D1004~D1005

recipe.AddItem("Mode", 6, RecipeDataType.Word, "운전 모드");
// → D1006

recipe.AddItem("AlarmLimit", 7, RecipeDataType.Word, "알람 한계값");
// → D1007

// ============================================
// 3. PLC에서 레시피 읽기 (Download)
// ============================================
var currentData = recipe.Download();

Console.WriteLine($"현재 설정 온도: {currentData["SetTemp"]}°C");
Console.WriteLine($"현재 설정 압력: {currentData["SetPressure"]}bar");
Console.WriteLine($"현재 사이클 타임: {currentData["CycleTime"]}ms");
Console.WriteLine($"현재 운전 모드: {currentData["Mode"]}");

// ============================================
// 4. 레시피 수정 및 PLC에 쓰기 (Upload)
// ============================================
currentData["SetTemp"] = 85.5f;      // 온도 변경
currentData["CycleTime"] = 120000;   // 2분으로 변경

recipe.Upload(currentData);
Console.WriteLine("레시피 업로드 완료!");

// ============================================
// 5. XML 파일로 저장/로드
// ============================================

// 현재 설정을 파일로 저장
recipe.SaveToXml(currentData, "Recipe_ProductA.xml", "ProductA", "A제품 생산용 레시피");

// 저장된 XML 내용 예시:
// <?xml version="1.0" encoding="utf-8"?>
// <Recipe>
//   <Name>ProductA</Name>
//   <Description>A제품 생산용 레시피</Description>
//   <CreatedTime>2024-01-15T10:30:00</CreatedTime>
//   <Items>
//     <Item Name="SetTemp" Type="Real" Description="설정 온도">85.5</Item>
//     <Item Name="SetPressure" Type="Real" Description="설정 압력">5.0</Item>
//     <Item Name="CycleTime" Type="DWord" Description="사이클 타임(ms)">120000</Item>
//     <Item Name="Mode" Type="Word" Description="운전 모드">1</Item>
//   </Items>
// </Recipe>

// 파일에서 로드
var loadedData = recipe.LoadFromXml("Recipe_ProductA.xml");

// PLC에 적용
recipe.Upload(loadedData);

// ============================================
// 6. 레시피 비교 (파일 vs PLC)
// ============================================
var fileData = recipe.LoadFromXml("Recipe_ProductA.xml");
var plcData = recipe.Download();

var differences = recipe.Compare(fileData, plcData);

if (differences.Count == 0)
{
    Console.WriteLine("파일과 PLC 설정이 일치합니다.");
}
else
{
    Console.WriteLine("차이점 발견:");
    foreach (var diff in differences)
    {
        Console.WriteLine($"  [{diff.ItemName}] 파일: {diff.Value1} ↔ PLC: {diff.Value2}");
    }
}
```

### 실전 예시: 제품 변경

```csharp
// 작업자가 "B제품"을 선택했을 때
void ChangeProduct(string productName)
{
    string recipeFile = $"Recipe_{productName}.xml";
    
    if (!File.Exists(recipeFile))
    {
        MessageBox.Show("레시피 파일이 없습니다!");
        return;
    }
    
    // 1. 레시피 로드
    var newRecipe = recipe.LoadFromXml(recipeFile);
    
    // 2. PLC에 전송
    recipe.Upload(newRecipe);
    
    // 3. 확인 (비교)
    var plcData = recipe.Download();
    var diff = recipe.Compare(newRecipe, plcData);
    
    if (diff.Count == 0)
        MessageBox.Show($"{productName} 레시피 적용 완료!");
    else
        MessageBox.Show("레시피 적용 실패! 값이 일치하지 않습니다.");
}
```

</details>

<details>
<summary><b>HandshakeHelper (PC↔PLC 핸드쉐이크)</b></summary>

### 개념

PC와 PLC 간의 **명령-응답 동기화 메커니즘**입니다. PC가 "이 작업 해줘"라고 요청하고, PLC가 "다 했어"라고 응답하는 패턴입니다.

```
┌─────────────────────────────────────────────────────────────┐
│                   핸드쉐이크 기본 흐름                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   PC (C#)                                    PLC (래더)     │
│                                                             │
│   ┌─────────────────┐                                      │
│   │ 1. M100 = On    │ ─────────────────────►  트리거 감지  │
│   │    "요청했어"    │                          작업 수행   │
│   └─────────────────┘                                      │
│                                                             │
│                       ◄─────────────────────  ┌──────────┐ │
│   완료 비트 감지                              │ 2. M200  │ │
│                                               │    = On  │ │
│                                               │ "끝났어" │ │
│   ┌─────────────────┐                        └──────────┘ │
│   │ 3. M100 = Off   │ ─────────────────────►              │
│   │    "확인했어"    │                                     │
│   └─────────────────┘                                      │
│                                                             │
│                       ◄─────────────────────  ┌──────────┐ │
│   원상복구 완료                               │ 4. M200  │ │
│   [핸드쉐이크 종료]                           │    = Off │ │
│                                               │ "리셋"   │ │
│                                               └──────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 왜 필요한가?

**잘못된 방식 (타이밍 문제 발생):**
```csharp
plc.WriteBit("M", 100, true);   // 시작 명령
Thread.Sleep(1000);              // 그냥 1초 기다림... 충분한가? 부족한가?
var result = plc.ReadWord("D", 200);  // 결과 읽기 - 아직 준비 안됐을 수도!
```

**올바른 방식 (HandshakeHelper 사용):**
```csharp
var result = handshake.Execute("M", 100, "M", 200, timeout: 5000);
// PLC가 "완료"라고 할 때까지 확실하게 대기
if (result.IsSuccess)
    var data = plc.ReadWord("D", 200);  // 이제 안전하게 읽기
```

### 사용 상황

| 상황 | 설명 |
|------|------|
| 레시피 적용 | PC가 레시피 전송 → PLC가 적용 완료 신호 |
| 생산 시작/정지 | PC가 명령 → PLC가 준비 완료 신호 |
| 데이터 요청 | PC가 조회 요청 → PLC가 데이터 준비 완료 신호 |
| 수동 조작 | PC에서 버튼 클릭 → PLC가 동작 완료 신호 |

### 예제 코드

```csharp
using CCM.Communication.PLC;
using CCM.Communication.PLC.Utilities;

var plc = new MitsubishiMcProtocol("192.168.0.10", 5001);
plc.Connect();

// ============================================
// 1. HandshakeHelper 생성 및 설정
// ============================================
var handshake = new HandshakeHelper(plc)
{
    PollingInterval = 50,       // 완료 비트 체크 주기 (50ms마다 확인)
    StabilizeDelay = 100,       // 핸드쉐이크 완료 후 안정화 대기
    WaitForCompleteOff = true,  // PLC가 완료 비트 Off 할 때까지 대기
    CompleteOffTimeout = 5000   // 완료 비트 Off 대기 타임아웃
};

// ============================================
// 2. 기본 핸드쉐이크 (신호만)
// ============================================
// PC: M100 On → PLC 처리 → PLC: M200 On → PC: M100 Off → PLC: M200 Off

var result = handshake.Execute(
    triggerDevice: "M", triggerAddress: 100,   // PC → PLC 트리거
    completeDevice: "M", completeAddress: 200, // PLC → PC 완료
    timeout: 5000);                             // 5초 타임아웃

if (result.IsSuccess)
    Console.WriteLine($"성공! 소요시간: {result.ElapsedMilliseconds}ms");
else if (result.IsTimeout)
    Console.WriteLine("타임아웃: PLC가 5초 안에 응답하지 않음");
else
    Console.WriteLine($"에러: {result.ErrorMessage}");

// ============================================
// 3. 데이터 포함 핸드쉐이크
// ============================================
// PC가 데이터를 보내고, PLC가 처리 후 결과를 돌려주는 패턴

// 요청 데이터 준비 (예: 바코드 번호)
var requestData = new short[] { 12345 };

var dataResult = handshake.ExecuteWithData(
    triggerDevice: "M", triggerAddress: 100,
    completeDevice: "M", completeAddress: 200,
    dataDevice: "D", dataAddress: 500,  // 데이터 영역: D500~
    dataLength: 10,                      // 응답으로 읽을 워드 수
    requestData: requestData,            // 요청 데이터 (D500에 씀)
    timeout: 10000);

// 내부 동작:
// [1] D500에 12345 씀 (요청 데이터)
// [2] M100 = On (트리거)
// [3] M200 = On 대기 (PLC 처리 완료)
// [4] D500~D509 읽기 (응답 데이터)
// [5] M100 = Off
// [6] M200 = Off 대기

if (dataResult.IsSuccess && dataResult.ResponseData != null)
{
    // PLC가 D500~D509에 써준 응답 데이터
    Console.WriteLine($"제품코드: {dataResult.ResponseData[0]}");
    Console.WriteLine($"수량: {dataResult.ResponseData[1]}");
    Console.WriteLine($"상태: {dataResult.ResponseData[2]}");
}

// ============================================
// 4. 명령 객체 사용 (재사용 가능)
// ============================================
var command = new HandshakeCommand
{
    CommandId = 1,                    // 명령 ID (로깅, 식별용)
    Name = "StartProduction",         // 명령 이름
    TriggerDevice = "M",
    TriggerAddress = 100,
    CompleteDevice = "M",
    CompleteAddress = 200,
    DataDevice = "D",
    DataAddress = 500,
    DataLength = 20,                  // D500~D519
    Timeout = 10000
};

// 상태 변경 이벤트 (로깅, UI 업데이트용)
handshake.StateChanged += (s, e) =>
{
    Console.WriteLine($"[{e.Command.Name}] 상태: {e.State}");
    // 출력 예:
    // [StartProduction] 상태: WaitingComplete
    // [StartProduction] 상태: Completed
};

var prodData = new short[] { 1, 100 };  // 라인번호, 목표수량
var cmdResult = handshake.Execute(command, prodData);

// ============================================
// 5. 유틸리티 메서드
// ============================================

// 비트 상태 대기: M300이 On될 때까지 최대 5초 대기
bool bitReached = handshake.WaitForBit("M", 300, true, 5000);
if (bitReached)
    Console.WriteLine("M300이 On됨!");
else
    Console.WriteLine("타임아웃: M300이 여전히 Off");

// 펄스 전송: M100을 200ms동안 On 후 Off (원샷 트리거)
handshake.SendPulse("M", 100, pulseWidth: 200);
// 동작: M100=On → 200ms 대기 → M100=Off

// 워드 값 대기: D100이 1이 될 때까지 최대 5초 대기
bool wordReached = handshake.WaitForWord("D", 100, targetValue: 1, timeout: 5000);
// 사용 예: 스텝 번호가 특정 값이 될 때까지 대기

// 워드 범위 대기: D100이 10~20 사이가 될 때까지 대기
bool rangeReached = handshake.WaitForWordInRange("D", 100, 10, 20, timeout: 5000);
```

### PLC 래더 프로그램 예시 (Mitsubishi GX Works)

PC와 핸드쉐이크하려면 PLC에도 대응하는 프로그램이 필요합니다:

```
[M100 트리거 감지 → 작업 시작]
──┤ M100 ├──┤/M200 ├──────────────────────────( M1000 )──
                                               작업 중 플래그

[작업 수행 - 예: D500 바코드로 제품 정보 조회]
──┤ M1000 ├───────────────────────────────────[ MOV K42 D500 ]──
                                               제품코드 → D500

──┤ M1000 ├───────────────────────────────────[ MOV K100 D501 ]──
                                               수량 → D501

──┤ M1000 ├───────────────────────────────────[ MOV K1 D502 ]──
                                               상태 → D502

[작업 완료 → 완료 비트 On]
──┤ M1000 ├───────────────────────────────────( M200 )──
                                               완료 신호

──┤ M1000 ├───────────────────────────────────( RST M1000 )──
                                               작업 중 플래그 Off

[PC가 트리거 Off → 완료 비트 Off (원상복구)]
──┤/M100 ├──┤ M200 ├──────────────────────────( RST M200 )──
```

### 실전 예시: 생산 시작 전체 흐름

```csharp
void StartProduction(int lineNo, int targetQty)
{
    // 1. 안전 조건 확인 (비상정지 해제 확인)
    if (!handshake.WaitForBit("M", 500, true, 10000))
    {
        MessageBox.Show("비상정지가 해제되지 않았습니다!");
        return;
    }
    
    // 2. 생산 명령 전송
    var command = new HandshakeCommand
    {
        Name = "StartProduction",
        TriggerDevice = "M", TriggerAddress = 100,
        CompleteDevice = "M", CompleteAddress = 200,
        DataDevice = "D", DataAddress = 500, DataLength = 10,
        Timeout = 5000
    };
    
    var data = new short[] { (short)lineNo, (short)targetQty };
    var result = handshake.Execute(command, data);
    
    if (!result.IsSuccess)
    {
        MessageBox.Show($"생산 시작 실패: {result.ErrorMessage}");
        return;
    }
    
    // 3. 생산 완료 대기 (D100 스텝이 99가 되면 완료)
    lblStatus.Text = "생산 중...";
    
    if (handshake.WaitForWord("D", 100, 99, timeout: 3600000))  // 최대 1시간
    {
        // 4. 완료 확인 펄스
        handshake.SendPulse("M", 110, 100);
        MessageBox.Show("생산 완료!");
    }
    else
    {
        MessageBox.Show("생산 타임아웃!");
    }
}
```

### 주요 속성/메서드 요약

| 속성/메서드 | 설명 |
|------------|------|
| `PollingInterval` | 완료 비트 체크 주기 (ms) |
| `StabilizeDelay` | 핸드쉐이크 후 안정화 대기 (ms) |
| `WaitForCompleteOff` | 완료 비트 Off까지 대기 여부 |
| `Execute()` | 기본 핸드쉐이크 실행 |
| `ExecuteWithData()` | 데이터 포함 핸드쉐이크 |
| `WaitForBit()` | 비트 상태 대기 |
| `WaitForWord()` | 워드 값 대기 |
| `SendPulse()` | 펄스(원샷) 전송 |
| `StateChanged` | 상태 변경 이벤트 |

</details>

<details>
<summary><b>AlarmManager (알람 관리)</b></summary>

### 개념

PLC의 **알람 비트들을 모니터링**하고, 알람 **발생/해제 이력을 관리**하는 클래스입니다.

```
┌─────────────────────────────────────────────────────────────┐
│                    알람 비트 구조                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   D1000 (알람 워드 0)                                       │
│   ┌─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┐                       │
│   │0│0│0│0│0│0│0│0│0│0│0│1│0│1│1│0│                       │
│   └─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┘                       │
│    15                      4 3 2 1 0  ← 비트 위치           │
│                            │ │ │ │                          │
│                            │ │ │ └── 알람1001: 온도상한    │
│                            │ │ └──── 알람1002: 온도하한    │
│                            │ └────── 알람1003: 압력이상    │
│                            └──────── 알람1004: 유량이상    │
│                                                             │
│   D1001 (알람 워드 1)                                       │
│   ┌─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬─┐                       │
│   │0│0│0│0│0│0│0│1│0│0│0│0│1│1│0│0│                       │
│   └─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴─┘                       │
│                  8         4 3 2 1 0                        │
│                  │         │ │                              │
│                  │         │ └── 알람2002: 모터1 과부하     │
│                  │         └──── 알람2003: 모터2 과부하     │
│                  └────────────── 알람2008: 비상정지         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 알람 상태 흐름

```
   [Normal]                    [Active]                 [Acknowledged]              [Cleared]
      │                           │                           │                         │
      │   비트 On                 │   운전자 확인             │   비트 Off              │
      ├─────────────────────────► ├─────────────────────────► ├───────────────────────► │
      │   AlarmOccurred 이벤트    │                           │   AlarmCleared 이벤트   │
      │                           │                           │                         │
```

### 사용 상황

| 상황 | 설명 |
|------|------|
| 알람 모니터링 화면 | 현재 발생 중인 알람 목록 표시 |
| 알람 이력 조회 | 과거 알람 발생/해제 기록 |
| 알람 확인(Ack) | 운전자가 알람을 인지했음을 표시 |
| 알람 통계 | 알람 발생 빈도, 등급별 분류 |

### 예제 코드

```csharp
using CCM.Communication.PLC;
using CCM.Communication.PLC.Utilities;

var plc = new MitsubishiMcProtocol("192.168.0.10", 5001);
plc.Connect();

// ============================================
// 1. AlarmManager 생성 및 설정
// ============================================
var alarm = new AlarmManager(plc, "D", 1000, 10)
// 알람 영역: D1000 ~ D1009 (10워드 = 160비트 = 최대 160개 알람)
{
    PollingInterval = 500,      // 500ms마다 알람 체크
    MaxHistoryCount = 1000,     // 최대 1000개 이력 보관
    ContinueOnError = true      // 에러 발생해도 계속 모니터링
};

// ============================================
// 2. 알람 정의 (비트 ↔ 알람 매핑)
// ============================================

// 개별 알람 추가
// AddAlarm(코드, 이름, 워드인덱스, 비트위치, 등급, 설명, 그룹)
alarm.AddAlarm(
    code: 1001,
    name: "온도 상한 초과",
    wordIndex: 0,           // D1000
    bitPosition: 0,         // 비트 0
    severity: AlarmSeverity.Error,
    description: "온도가 설정값을 초과했습니다",
    group: "온도"
);
// → D1000.0 = On 이면 알람 1001 발생

alarm.AddAlarm(1002, "온도 하한 미달", 0, 1, AlarmSeverity.Warning, "온도가 설정값 미만입니다", "온도");
// → D1000.1

alarm.AddAlarm(1003, "압력 이상", 0, 2, AlarmSeverity.Critical, "압력 센서 이상! 즉시 조치 필요", "압력");
// → D1000.2

// 워드 단위 일괄 추가 (16비트 한 번에 정의)
alarm.AddAlarmsForWord(
    wordIndex: 1,               // D1001
    baseCode: 2000,             // 알람 코드 시작 번호
    alarmNames: new[] {
        "모터1 과부하",          // 비트0 → 알람 2000
        "모터2 과부하",          // 비트1 → 알람 2001
        "인버터 이상",           // 비트2 → 알람 2002
        "서보 이상",             // 비트3 → 알람 2003
        null,                   // 비트4 → 사용 안함
        null,                   // 비트5 → 사용 안함
        "비상정지",              // 비트6 → 알람 2006
        "안전문 열림"            // 비트7 → 알람 2007
    },
    severity: AlarmSeverity.Error,
    group: "구동부"
);

// ============================================
// 3. 이벤트 등록
// ============================================

// 알람 발생 시
alarm.AlarmOccurred += (s, e) =>
{
    Console.WriteLine($"[알람 발생!] {e.Alarm.Definition.Code}: {e.Alarm.Definition.Name}");
    Console.WriteLine($"  등급: {e.Alarm.Definition.Severity}");
    Console.WriteLine($"  그룹: {e.Alarm.Definition.Group}");
    Console.WriteLine($"  설명: {e.Alarm.Definition.Description}");
    Console.WriteLine($"  발생시간: {e.Alarm.OccurredTime}");
    
    // Critical 알람이면 경고음
    if (e.Alarm.Definition.Severity == AlarmSeverity.Critical)
    {
        Console.Beep(1000, 500);  // 경고음
    }
};

// 알람 해제 시
alarm.AlarmCleared += (s, e) =>
{
    Console.WriteLine($"[알람 해제] {e.Alarm.Definition.Code}: {e.Alarm.Definition.Name}");
    Console.WriteLine($"  지속시간: {e.Alarm.Duration?.TotalSeconds:F1}초");
};

// 에러 발생 시
alarm.ErrorOccurred += (s, e) =>
{
    Console.WriteLine($"[모니터링 에러] {e.Message}");
};

// ============================================
// 4. 모니터링 시작
// ============================================
alarm.Start();

// ============================================
// 5. 알람 조회
// ============================================

// 현재 활성 알람 전체
var activeAlarms = alarm.GetActiveAlarms();
Console.WriteLine($"현재 발생 중인 알람: {activeAlarms.Count}개");
foreach (var a in activeAlarms)
{
    Console.WriteLine($"  [{a.Definition.Code}] {a.Definition.Name}");
    Console.WriteLine($"    발생: {a.OccurredTime}, 상태: {a.State}");
}

// 특정 등급 이상만 조회
var criticalAlarms = alarm.GetActiveAlarms(AlarmSeverity.Critical);
var errorOrAbove = alarm.GetActiveAlarms(AlarmSeverity.Error);

// 특정 알람 확인
if (alarm.IsAlarmActive(1003))
    Console.WriteLine("압력 이상 알람 발생 중!");

// Critical 알람 존재 여부 (긴급 상황 체크)
if (alarm.HasCriticalAlarm())
{
    Console.WriteLine("!!! 치명적 알람 발생 - 즉시 조치 필요 !!!");
}

// ============================================
// 6. 알람 확인 (Acknowledge)
// ============================================
// 운전자가 알람을 인지했음을 표시 (알람 자체를 끄는 게 아님)

// 개별 알람 확인
bool acked = alarm.AcknowledgeAlarm(1001, "운전자A");
if (acked)
    Console.WriteLine("알람 1001 확인 완료");

// 모든 알람 일괄 확인
int ackCount = alarm.AcknowledgeAll("관리자");
Console.WriteLine($"{ackCount}개 알람 확인 완료");

// ============================================
// 7. 알람 통계
// ============================================
var stats = alarm.GetStatistics();
Console.WriteLine($"=== 알람 통계 ===");
Console.WriteLine($"활성 알람: {stats.ActiveAlarmCount}개");
Console.WriteLine($"미확인: {stats.UnacknowledgedCount}개");
Console.WriteLine($"Critical: {stats.CriticalCount}개");
Console.WriteLine($"Error: {stats.ErrorCount}개");
Console.WriteLine($"Warning: {stats.WarningCount}개");
Console.WriteLine($"Info: {stats.InfoCount}개");
Console.WriteLine($"총 이력: {stats.TotalHistoryCount}개");

// ============================================
// 8. 알람 이력 조회
// ============================================

// 최근 100개 이력
var history = alarm.GetAlarmHistory(100);
foreach (var h in history)
{
    Console.WriteLine($"[{h.Definition.Code}] {h.Definition.Name}");
    Console.WriteLine($"  발생: {h.OccurredTime}");
    Console.WriteLine($"  해제: {h.ClearedTime}");
    Console.WriteLine($"  확인: {h.AcknowledgedTime} by {h.AcknowledgedBy}");
}

// 기간별 이력 조회
var todayHistory = alarm.GetAlarmHistory(DateTime.Today, DateTime.Now);
var lastWeek = alarm.GetAlarmHistory(DateTime.Now.AddDays(-7), DateTime.Now);

// ============================================
// 9. 종료
// ============================================
alarm.Stop();
alarm.Dispose();
```

### 알람 등급

| 등급 | 값 | 설명 | 예시 |
|------|----|----|------|
| `Info` | 0 | 정보 | 작업 시작, 완료 알림 |
| `Warning` | 1 | 경고 | 온도 근접, 소모품 교체 예정 |
| `Error` | 2 | 에러 | 센서 이상, 모터 과부하 |
| `Critical` | 3 | 치명적 | 비상정지, 화재 감지 |

### 실전 예시: 알람 화면 UI

```csharp
// WinForm 알람 화면 예시
public partial class AlarmForm : Form
{
    private AlarmManager _alarm;
    
    public AlarmForm(IPlcCommunication plc)
    {
        InitializeComponent();
        
        _alarm = new AlarmManager(plc, "D", 1000, 20);
        SetupAlarms();
        
        _alarm.AlarmOccurred += Alarm_Occurred;
        _alarm.AlarmCleared += Alarm_Cleared;
        
        _alarm.Start();
    }
    
    private void Alarm_Occurred(object sender, AlarmEventArgs e)
    {
        // UI 스레드에서 실행
        this.Invoke((Action)(() =>
        {
            // 알람 목록에 추가
            var item = new ListViewItem(new[] {
                e.Alarm.Definition.Code.ToString(),
                e.Alarm.Definition.Name,
                e.Alarm.Definition.Severity.ToString(),
                e.Alarm.OccurredTime.ToString("HH:mm:ss")
            });
            
            // 등급별 색상
            switch (e.Alarm.Definition.Severity)
            {
                case AlarmSeverity.Critical:
                    item.BackColor = Color.Red;
                    item.ForeColor = Color.White;
                    break;
                case AlarmSeverity.Error:
                    item.BackColor = Color.Orange;
                    break;
                case AlarmSeverity.Warning:
                    item.BackColor = Color.Yellow;
                    break;
            }
            
            lvAlarms.Items.Insert(0, item);
            
            // Critical 알람 시 팝업
            if (e.Alarm.Definition.Severity == AlarmSeverity.Critical)
            {
                MessageBox.Show(
                    $"치명적 알람 발생!\n\n{e.Alarm.Definition.Name}\n{e.Alarm.Definition.Description}",
                    "긴급",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }));
    }
    
    private void btnAckAll_Click(object sender, EventArgs e)
    {
        int count = _alarm.AcknowledgeAll(Environment.UserName);
        lblStatus.Text = $"{count}개 알람 확인됨";
    }
}
```

</details>

<details>
<summary><b>ProductionLogger (생산 데이터 로깅)</b></summary>

### 개념

PLC에서 **생산 데이터를 주기적으로 읽어서 DB에 저장**하는 클래스입니다. MES(제조실행시스템)나 데이터 분석을 위한 데이터 수집에 사용합니다.

```
┌─────────────────────────────────────────────────────────────┐
│                  ProductionLogger 동작                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   [PLC]                [ProductionLogger]         [DB]      │
│                                                             │
│   D100: 850  ────┐                                         │
│   D101: 523  ────┤     ┌──────────────┐     ┌──────────┐  │
│   D102: 3.14 ────┼────►│  1초마다     │────►│ INSERT   │  │
│   M100: On   ────┤     │  PLC 읽기    │     │ INTO     │  │
│   D200: "A01"────┘     │  → DB 저장   │     │ ProdLog  │  │
│                        └──────────────┘     └──────────┘  │
│                                                             │
│   시간     온도   압력   유량    운전   제품코드           │
│   ─────────────────────────────────────────────────         │
│   10:00:01  85.0  52.3  3.14   true   A01                  │
│   10:00:02  85.1  52.1  3.15   true   A01                  │
│   10:00:03  85.2  52.4  3.14   true   A01                  │
│   ...                                                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 로깅 트리거 모드

| 모드 | 설명 | 사용 상황 |
|------|------|----------|
| `Periodic` | 일정 주기마다 로깅 | 연속 데이터 수집 (온도, 압력 트렌드) |
| `OnTrigger` | 특정 비트 On 시 로깅 | 이벤트 기반 (제품 완료, 작업 시작) |
| `OnChange` | 값 변경 시 로깅 | 상태 변경 기록 (모드 변경, 설정 변경) |

### 사용 상황

| 상황 | 설명 |
|------|------|
| 트렌드 데이터 수집 | 온도, 압력, 속도 등 시계열 데이터 |
| 생산 실적 기록 | 생산 수량, 양품/불량, 사이클 타임 |
| 품질 데이터 수집 | 측정값, 검사 결과 |
| 이벤트 로깅 | 작업 시작/종료, 상태 변경 |

### 예제 코드

```csharp
using CCM.Communication.PLC;
using CCM.Communication.PLC.Utilities;
using CCM.Database;

var plc = new MitsubishiMcProtocol("192.168.0.10", 5001);
plc.Connect();

var db = new MssqlHelper("Server=localhost;Database=Production;User Id=sa;Password=1234;");

// ============================================
// 1. ProductionLogger 생성 및 설정
// ============================================
var logger = new ProductionLogger(plc, db, "ProductionLog")
// 테이블명: ProductionLog
{
    LogInterval = 1000,                    // 1초마다 로깅
    TriggerMode = LogTriggerMode.Periodic, // 주기적 로깅
    TimestampColumn = "LogTime",           // 시간 컬럼명
    UseDynamicInsert = true,               // 동적 INSERT 쿼리 사용
    ContinueOnError = true                 // 에러 발생해도 계속 로깅
};

// ============================================
// 2. 로깅 항목 추가 (PLC 주소 ↔ DB 컬럼 매핑)
// ============================================

// Word (16비트 정수) - 스케일 적용 가능
logger.AddWord(
    name: "Temperature",     // DB 컬럼명
    device: "D",
    address: 100,
    scaleFactor: 0.1,        // D100 값 × 0.1
    offset: 0                // + 오프셋
);
// 예: D100=850 → Temperature=85.0

logger.AddWord("Pressure", "D", 101, scaleFactor: 0.01);
// 예: D101=5230 → Pressure=52.3

// Real (32비트 실수) - D102~D103 사용
logger.AddReal("FlowRate", "D", 102, decimalPlaces: 2);
// 예: D102-D103=3.14159 → FlowRate=3.14

// DWord (32비트 정수) - D110~D111 사용
logger.AddDWord("ProductCount", "D", 110);
// 예: D110-D111=123456 → ProductCount=123456

// Bit (불리언)
logger.AddBit("IsRunning", "M", 100);
// 예: M100=On → IsRunning=true

// String (문자열) - 워드 수 지정
logger.AddString("ProductCode", "D", 200, wordCount: 10);
// 예: D200~D209 → ProductCode="PRODUCT-A01"

// ============================================
// 3. 이벤트 등록
// ============================================

logger.LogCompleted += (s, e) =>
{
    if (e.IsSuccess)
    {
        Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] 로깅 완료");
        
        // 로깅된 데이터 확인
        foreach (var kv in e.LoggedData)
        {
            Console.WriteLine($"  {kv.Key}: {kv.Value}");
        }
    }
    else
    {
        Console.WriteLine($"로깅 실패: {e.ErrorMessage}");
    }
};

logger.ErrorOccurred += (s, e) =>
{
    Console.WriteLine($"[에러] {e.Message}");
};

// ============================================
// 4. 로깅 시작
// ============================================
logger.Start();

// 프로그램 실행 중... 백그라운드에서 1초마다 DB에 INSERT

// 통계 확인
Console.WriteLine($"총 로깅 횟수: {logger.TotalLogCount}");
Console.WriteLine($"마지막 로깅: {logger.LastLogTime}");

// 수동 로깅 (한 번만)
logger.LogOnce();

// 로깅 중지
logger.Stop();
logger.Dispose();
```

### 트리거 기반 로깅

```csharp
// ============================================
// 제품 완료 시점에만 로깅 (트리거 모드)
// ============================================
var completionLogger = new ProductionLogger(plc, db, "ProductionResult")
{
    TriggerMode = LogTriggerMode.OnTrigger,
    TriggerDevice = "M",
    TriggerAddress = 500,        // M500: 제품 완료 신호
    TriggerOnRisingEdge = true,  // Off→On 감지 (Rising Edge)
    LogInterval = 50             // 50ms 주기로 트리거 체크
};

completionLogger.AddDWord("ProductNo", "D", 100);    // 제품 번호
completionLogger.AddWord("CycleTime", "D", 102);     // 사이클 타임
completionLogger.AddWord("Result", "D", 103);        // 결과 (1=양품, 2=불량)
completionLogger.AddReal("MeasuredValue", "D", 104); // 측정값

completionLogger.Start();

// PLC에서 M500이 Off→On 될 때마다 자동으로 로깅
// 
// 시간         제품번호  사이클타임  결과  측정값
// ──────────────────────────────────────────────
// 10:05:23     1001     45         1     12.34
// 10:06:12     1002     49         1     12.31
// 10:06:58     1003     46         2     15.67  ← 불량
// ...
```

### 값 변경 시 로깅

```csharp
// ============================================
// 설정값 변경 이력 로깅 (변경 모드)
// ============================================
var changeLogger = new ProductionLogger(plc, db, "SettingChangeLog")
{
    TriggerMode = LogTriggerMode.OnChange,
    LogInterval = 100  // 100ms 주기로 변경 감지
};

changeLogger.AddWord("OperationMode", "D", 50);   // 운전 모드
changeLogger.AddReal("SetTemperature", "D", 52); // 설정 온도
changeLogger.AddReal("SetPressure", "D", 54);    // 설정 압력

changeLogger.Start();

// 값이 변경될 때만 로깅
// 
// 시간         운전모드  설정온도  설정압력
// ──────────────────────────────────────────
// 09:00:00     1        80.0     5.0      ← 초기값
// 10:30:15     2        80.0     5.0      ← 모드 변경
// 11:45:30     2        85.0     5.0      ← 온도 변경
// 14:20:00     2        85.0     6.5      ← 압력 변경
```

### DB 테이블 자동 생성

```csharp
// ============================================
// 개발용: 테이블/프로시저 생성 SQL 출력
// ============================================

// 로깅 항목 정의 후...
logger.AddWord("Temperature", "D", 100);
logger.AddWord("Pressure", "D", 101);
logger.AddReal("FlowRate", "D", 102);
logger.AddBit("IsRunning", "M", 100);

// 테이블 생성 SQL
Console.WriteLine(logger.GenerateCreateTableSql());
// 출력:
// CREATE TABLE ProductionLog (
//     Id INT IDENTITY(1,1) PRIMARY KEY,
//     LogTime DATETIME NOT NULL,
//     Temperature INT NULL,
//     Pressure INT NULL,
//     FlowRate FLOAT NULL,
//     IsRunning BIT NULL
// )

// 저장 프로시저 생성 SQL
logger.UseDynamicInsert = false;
logger.StoredProcedureName = "sp_InsertProductionLog";
Console.WriteLine(logger.GenerateStoredProcedureSql());
// 출력:
// CREATE PROCEDURE sp_InsertProductionLog
//     @LogTime DATETIME,
//     @Temperature INT = NULL,
//     @Pressure INT = NULL,
//     @FlowRate FLOAT = NULL,
//     @IsRunning BIT = NULL
// AS
// BEGIN
//     SET NOCOUNT ON;
//     INSERT INTO ProductionLog (LogTime, Temperature, Pressure, FlowRate, IsRunning)
//     VALUES (@LogTime, @Temperature, @Pressure, @FlowRate, @IsRunning)
// END
```

### 스케일 팩터 설명

```csharp
// PLC는 정수만 다루므로, 소수점은 스케일 변환 필요

// 예: 온도 85.3°C를 PLC에서는 853으로 저장 (×10)
logger.AddWord("Temperature", "D", 100, scaleFactor: 0.1);
// D100=853 → Temperature=85.3

// 예: 압력 5.23bar를 PLC에서는 523으로 저장 (×100)
logger.AddWord("Pressure", "D", 101, scaleFactor: 0.01);
// D101=523 → Pressure=5.23

// 예: 온도 센서가 -50~150°C 범위, 0~4000 출력
// 변환식: 온도 = (PLC값 × 0.05) - 50
logger.AddWord("SensorTemp", "D", 102, scaleFactor: 0.05, offset: -50);
// D102=2000 → SensorTemp = (2000 × 0.05) - 50 = 50°C
```

### 실전 예시: 생산 데이터 수집 시스템

```csharp
public class ProductionDataCollector : IDisposable
{
    private ProductionLogger _trendLogger;     // 1초 주기 트렌드
    private ProductionLogger _completionLogger; // 제품 완료 시
    private ProductionLogger _alarmLogger;      // 알람 발생 시
    
    public ProductionDataCollector(IPlcCommunication plc, MssqlHelper db)
    {
        // 1. 트렌드 데이터 (1초 주기)
        _trendLogger = new ProductionLogger(plc, db, "TrendData")
        {
            TriggerMode = LogTriggerMode.Periodic,
            LogInterval = 1000
        };
        _trendLogger.AddReal("Temperature", "D", 100);
        _trendLogger.AddReal("Pressure", "D", 102);
        _trendLogger.AddReal("Speed", "D", 104);
        
        // 2. 생산 실적 (제품 완료 시)
        _completionLogger = new ProductionLogger(plc, db, "ProductionResult")
        {
            TriggerMode = LogTriggerMode.OnTrigger,
            TriggerDevice = "M",
            TriggerAddress = 500,
            TriggerOnRisingEdge = true
        };
        _completionLogger.AddDWord("ProductNo", "D", 200);
        _completionLogger.AddWord("CycleTime", "D", 204);
        _completionLogger.AddWord("Result", "D", 205);
        _completionLogger.AddString("LotNo", "D", 210, 10);
        
        // 3. 알람 이력 (알람 발생 시)
        _alarmLogger = new ProductionLogger(plc, db, "AlarmLog")
        {
            TriggerMode = LogTriggerMode.OnChange,
            LogInterval = 100
        };
        _alarmLogger.AddWord("AlarmWord1", "D", 1000);
        _alarmLogger.AddWord("AlarmWord2", "D", 1001);
    }
    
    public void Start()
    {
        _trendLogger.Start();
        _completionLogger.Start();
        _alarmLogger.Start();
    }
    
    public void Stop()
    {
        _trendLogger.Stop();
        _completionLogger.Stop();
        _alarmLogger.Stop();
    }
    
    public void Dispose()
    {
        _trendLogger?.Dispose();
        _completionLogger?.Dispose();
        _alarmLogger?.Dispose();
    }
}
```

### 주요 속성/메서드 요약

| 속성/메서드 | 설명 |
|------------|------|
| `TableName` | 저장할 DB 테이블명 |
| `LogInterval` | 로깅 주기 (ms) |
| `TriggerMode` | 트리거 모드 (Periodic/OnTrigger/OnChange) |
| `TriggerDevice/Address` | 트리거 비트 주소 (OnTrigger 모드) |
| `AddWord/AddReal/AddBit/AddString` | 로깅 항목 추가 |
| `Start/Stop` | 로깅 시작/중지 |
| `LogOnce` | 수동 로깅 (1회) |
| `TotalLogCount` | 총 로깅 횟수 |
| `GenerateCreateTableSql` | 테이블 생성 SQL 출력 |

</details>

---

## 라이선스

MIT License
