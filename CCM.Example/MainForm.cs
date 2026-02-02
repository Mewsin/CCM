using System;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using CCM.Communication.Interfaces;
using CCM.Communication.PLC;
using CCM.Communication.Serial;
using CCM.Communication.Socket;
using CCM.Database;
using System.Linq;

namespace CCM.Example
{
    public partial class MainForm : Form
    {
        #region Fields

        private MssqlHelper _dbHelper;
        private TcpClientHelper _tcpClient;
        private UdpHelper _udpClient;
        private SerialPortHelper _serialPort;
        private TcpServerHelper _tcpServer;
        private MitsubishiMcProtocol _mitsubishiPlc;
        private SiemensS7Protocol _siemensPlc;
        private LsElectricXgt _lsPlc;
        private ModbusClient _modbusClient;

        #endregion

        public MainForm()
        {
            InitializeComponent();
            InitializeControls();
        }

        #region Initialization

        private void InitializeControls()
        {
            // ComboBox 초기화
            cmbPlcType.Items.AddRange(new object[] { "Mitsubishi MC", "Siemens S7", "LS Electric XGT", "Modbus TCP", "Modbus RTU" });
            cmbPlcType.SelectedIndex = 0;

            cmbS7CpuType.Items.AddRange(Enum.GetNames(typeof(S7CpuType)));
            cmbS7CpuType.SelectedIndex = 3; // S71200

            // ByteOrder 콤보박스 초기화
            cmbStringByteOrder.Items.AddRange(new object[] { "Big (AB)", "Little (BA)" });
            cmbStringByteOrder.SelectedIndex = 0; // Big Endian (Siemens 기본값)

            // 시리얼 포트 목록
            RefreshSerialPorts();

            // 이벤트 연결
            this.FormClosing += MainForm_FormClosing;
        }

        private void RefreshSerialPorts()
        {
            cmbSerialPort.Items.Clear();
            cmbSerialPort.Items.AddRange(SerialPortHelper.GetAvailablePorts());
            cmbModbusPort.Items.Clear();
            cmbModbusPort.Items.AddRange(SerialPortHelper.GetAvailablePorts());
            if (cmbSerialPort.Items.Count > 0)
            {
                cmbSerialPort.SelectedIndex = 0;
                cmbModbusPort.SelectedIndex = 0;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 모든 연결 해제
            _dbHelper?.Dispose();
            _tcpClient?.Dispose();
            _tcpServer?.Dispose();
            _udpClient?.Dispose();
            _serialPort?.Dispose();
            _mitsubishiPlc?.Dispose();
            _siemensPlc?.Dispose();
            _lsPlc?.Dispose();
            _modbusClient?.Dispose();
        }

        #endregion

        #region Database

        private void btnDbConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string connectionString = txtDbConnectionString.Text;
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    MessageBox.Show("연결 문자열을 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _dbHelper = new MssqlHelper(connectionString);
                if (_dbHelper.TestConnection())
                {
                    lblDbStatus.Text = "연결됨";
                    lblDbStatus.ForeColor = Color.Green;
                    AppendLog("[DB] 연결 성공");
                }
                else
                {
                    lblDbStatus.Text = "연결 실패";
                    lblDbStatus.ForeColor = Color.Red;
                    AppendLog("[DB] 연결 실패");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[DB] 오류: {ex.Message}");
                MessageBox.Show(ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDbDisconnect_Click(object sender, EventArgs e)
        {
            _dbHelper?.Dispose();
            _dbHelper = null;
            lblDbStatus.Text = "연결 안됨";
            lblDbStatus.ForeColor = Color.Gray;
            AppendLog("[DB] 연결 해제");
        }

        private void btnDbExecuteQuery_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dbHelper == null)
                {
                    MessageBox.Show("먼저 데이터베이스에 연결하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string sql = txtDbQuery.Text;
                if (string.IsNullOrWhiteSpace(sql))
                {
                    MessageBox.Show("SQL 쿼리를 입력하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataTable result = _dbHelper.ExecuteQuery(sql);
                dgvDbResult.DataSource = result;
                AppendLog($"[DB] 쿼리 실행 완료: {result.Rows.Count}개 행");
            }
            catch (Exception ex)
            {
                AppendLog($"[DB] 쿼리 오류: {ex.Message}");
                MessageBox.Show(ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDbExecuteNonQuery_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dbHelper == null)
                {
                    MessageBox.Show("먼저 데이터베이스에 연결하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string sql = txtDbQuery.Text;
                int affected = _dbHelper.ExecuteNonQuery(sql);
                AppendLog($"[DB] 실행 완료: {affected}개 행 영향받음");
                MessageBox.Show($"{affected}개 행이 영향받았습니다.", "결과", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"[DB] 실행 오류: {ex.Message}");
                MessageBox.Show(ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDbExecuteProcedure_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dbHelper == null)
                {
                    MessageBox.Show("먼저 데이터베이스에 연결하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string procName = txtDbQuery.Text.Trim();
                DataTable result = _dbHelper.ExecuteProcedure(procName);
                dgvDbResult.DataSource = result;
                AppendLog($"[DB] 프로시저 실행 완료: {result.Rows.Count}개 행");
            }
            catch (Exception ex)
            {
                AppendLog($"[DB] 프로시저 오류: {ex.Message}");
                MessageBox.Show(ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region TCP Server

        private void btnTcpServerStart_Click(object sender, EventArgs e)
        {
            try
            {
                _tcpServer = new TcpServerHelper((int)numTcpServerPort.Value);
                _tcpServer.ServerStateChanged += (s, args) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        lblTcpServerStatus.Text = args.IsConnected ? "실행 중" : "중지됨";
                        lblTcpServerStatus.ForeColor = args.IsConnected ? Color.Green : Color.Gray;
                    }));
                };
                _tcpServer.ClientConnected += (s, args) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        lstTcpServerClients.Items.Add($"{args.ClientId} ({args.ClientEndPoint})");
                        AppendLog($"[TCP Server] 클라이언트 연결: {args.ClientId} ({args.ClientEndPoint})");
                    }));
                };
                _tcpServer.ClientDisconnected += (s, args) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        for (int i = lstTcpServerClients.Items.Count - 1; i >= 0; i--)
                        {
                            if (lstTcpServerClients.Items[i].ToString().StartsWith(args.ClientId))
                            {
                                lstTcpServerClients.Items.RemoveAt(i);
                                break;
                            }
                        }
                        AppendLog($"[TCP Server] 클라이언트 연결 해제: {args.ClientId}");
                    }));
                };
                _tcpServer.ClientDataReceived += (s, args) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        string hex = BitConverter.ToString(args.Data);
                        AppendLog($"[TCP Server 수신] {args.ClientId}: {hex}");
                    }));
                };

                if (_tcpServer.Start())
                {
                    AppendLog($"[TCP Server] 포트 {numTcpServerPort.Value}에서 시작됨");
                }
                else
                {
                    AppendLog("[TCP Server] 시작 실패");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[TCP Server] 오류: {ex.Message}");
            }
        }

        private void btnTcpServerStop_Click(object sender, EventArgs e)
        {
            _tcpServer?.Stop();
            lstTcpServerClients.Items.Clear();
            AppendLog("[TCP Server] 중지됨");
        }

        private void btnTcpServerSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (_tcpServer == null || !_tcpServer.IsRunning)
                {
                    MessageBox.Show("먼저 TCP 서버를 시작하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                byte[] data = ParseHexString(txtTcpServerSendData.Text);

                if (chkTcpServerBroadcast.Checked)
                {
                    _tcpServer.SendToAll(data);
                    AppendLog($"[TCP Server 송신] 전체: {BitConverter.ToString(data)}");
                }
                else
                {
                    if (lstTcpServerClients.SelectedItem == null)
                    {
                        MessageBox.Show("클라이언트를 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string selected = lstTcpServerClients.SelectedItem.ToString();
                    string clientId = selected.Split(' ')[0];
                    if (_tcpServer.SendTo(clientId, data))
                    {
                        AppendLog($"[TCP Server 송신] {clientId}: {BitConverter.ToString(data)}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[TCP Server] 송신 오류: {ex.Message}");
            }
        }

        private void btnTcpServerDisconnectClient_Click(object sender, EventArgs e)
        {
            if (lstTcpServerClients.SelectedItem == null)
            {
                MessageBox.Show("연결 해제할 클라이언트를 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selected = lstTcpServerClients.SelectedItem.ToString();
            string clientId = selected.Split(' ')[0];
            _tcpServer?.DisconnectClient(clientId);
        }

        #endregion

        #region TCP Client

        private void btnTcpConnect_Click(object sender, EventArgs e)
        {
            try
            {
                _tcpClient = new TcpClientHelper(txtTcpIp.Text, (int)numTcpPort.Value);
                _tcpClient.ConnectionStateChanged += (s, args) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        lblTcpStatus.Text = args.IsConnected ? "연결됨" : "연결 안됨";
                        lblTcpStatus.ForeColor = args.IsConnected ? Color.Green : Color.Gray;
                    }));
                };
                _tcpClient.DataReceived += (s, args) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        string hex = BitConverter.ToString(args.Data);
                        AppendLog($"[TCP 수신] {hex}");
                    }));
                };

                if (_tcpClient.Connect())
                {
                    AppendLog("[TCP] 연결 성공");
                }
                else
                {
                    AppendLog("[TCP] 연결 실패");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[TCP] 오류: {ex.Message}");
            }
        }

        private void btnTcpDisconnect_Click(object sender, EventArgs e)
        {
            _tcpClient?.Disconnect();
            AppendLog("[TCP] 연결 해제");
        }

        private void btnTcpSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (_tcpClient == null || !_tcpClient.IsConnected)
                {
                    MessageBox.Show("먼저 TCP 서버에 연결하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                byte[] data = ParseHexString(txtTcpSendData.Text);
                if (_tcpClient.Send(data))
                {
                    AppendLog($"[TCP 송신] {BitConverter.ToString(data)}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[TCP] 송신 오류: {ex.Message}");
            }
        }

        #endregion

        #region UDP

        private void btnUdpStart_Click(object sender, EventArgs e)
        {
            try
            {
                _udpClient = new UdpHelper(txtUdpRemoteIp.Text, (int)numUdpRemotePort.Value, (int)numUdpLocalPort.Value);
                if (_udpClient.Connect())
                {
                    lblUdpStatus.Text = "활성";
                    lblUdpStatus.ForeColor = Color.Green;
                    AppendLog("[UDP] 시작됨");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[UDP] 오류: {ex.Message}");
            }
        }

        private void btnUdpStop_Click(object sender, EventArgs e)
        {
            _udpClient?.Disconnect();
            lblUdpStatus.Text = "비활성";
            lblUdpStatus.ForeColor = Color.Gray;
            AppendLog("[UDP] 중지됨");
        }

        private void btnUdpSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (_udpClient == null || !_udpClient.IsConnected)
                {
                    MessageBox.Show("먼저 UDP를 시작하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                byte[] data = ParseHexString(txtUdpSendData.Text);
                if (_udpClient.Send(data))
                {
                    AppendLog($"[UDP 송신] {BitConverter.ToString(data)}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[UDP] 송신 오류: {ex.Message}");
            }
        }

        #endregion

        #region Serial Port

        private void btnSerialRefresh_Click(object sender, EventArgs e)
        {
            RefreshSerialPorts();
        }

        private void btnSerialConnect_Click(object sender, EventArgs e)
        {
            try
            {
                _serialPort = new SerialPortHelper
                {
                    PortName = cmbSerialPort.Text,
                    BaudRate = (int)numSerialBaudRate.Value,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None
                };

                _serialPort.DataReceived += (s, args) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        string hex = BitConverter.ToString(args.Data);
                        AppendLog($"[Serial 수신] {hex}");
                    }));
                };

                _serialPort.UseAsyncReceive = true;

                if (_serialPort.Connect())
                {
                    lblSerialStatus.Text = "열림";
                    lblSerialStatus.ForeColor = Color.Green;
                    AppendLog($"[Serial] {cmbSerialPort.Text} 열림");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[Serial] 오류: {ex.Message}");
            }
        }

        private void btnSerialDisconnect_Click(object sender, EventArgs e)
        {
            _serialPort?.Disconnect();
            lblSerialStatus.Text = "닫힘";
            lblSerialStatus.ForeColor = Color.Gray;
            AppendLog("[Serial] 닫힘");
        }

        private void btnSerialSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsConnected)
                {
                    MessageBox.Show("먼저 시리얼 포트를 열어주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                byte[] data = ParseHexString(txtSerialSendData.Text);
                if (_serialPort.Send(data))
                {
                    AppendLog($"[Serial 송신] {BitConverter.ToString(data)}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[Serial] 송신 오류: {ex.Message}");
            }
        }

        #endregion

        #region PLC

        private void btnPlcConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string plcType = cmbPlcType.SelectedItem?.ToString() ?? "";
                bool connected = false;

                switch (plcType)
                {
                    case "Mitsubishi MC":
                        _mitsubishiPlc = new MitsubishiMcProtocol(txtPlcIp.Text, (int)numPlcPort.Value);
                        connected = _mitsubishiPlc.Connect();
                        if (connected)
                        {
                            UpdatePlcStatus(true, "Mitsubishi MC 연결됨");
                        }
                        break;

                    case "Siemens S7":
                        S7CpuType cpuType = (S7CpuType)Enum.Parse(typeof(S7CpuType), cmbS7CpuType.SelectedItem.ToString());
                        _siemensPlc = new SiemensS7Protocol(txtPlcIp.Text, cpuType, (byte)numS7Rack.Value, (byte)numS7Slot.Value);
                        connected = _siemensPlc.Connect();
                        if (connected)
                        {
                            UpdatePlcStatus(true, "Siemens S7 연결됨");
                        }
                        break;

                    case "LS Electric XGT":
                        _lsPlc = new LsElectricXgt(txtPlcIp.Text, (int)numPlcPort.Value);
                        connected = _lsPlc.Connect();
                        if (connected)
                        {
                            UpdatePlcStatus(true, "LS Electric XGT 연결됨");
                        }
                        break;

                    case "Modbus TCP":
                        _modbusClient = new ModbusClient(txtPlcIp.Text, (int)numPlcPort.Value, (byte)numModbusSlave.Value);
                        connected = _modbusClient.Connect();
                        if (connected)
                        {
                            UpdatePlcStatus(true, "Modbus TCP 연결됨");
                        }
                        break;

                    case "Modbus RTU":
                        _modbusClient = new ModbusClient(cmbModbusPort.Text, (int)numModbusBaudRate.Value, Parity.None, (byte)numModbusSlave.Value);
                        _modbusClient.Mode = ModbusMode.Rtu;
                        connected = _modbusClient.Connect();
                        if (connected)
                        {
                            UpdatePlcStatus(true, "Modbus RTU 연결됨");
                        }
                        break;
                }

                if (connected)
                {
                    AppendLog($"[PLC] {plcType} 연결 성공");
                }
                else
                {
                    UpdatePlcStatus(false, "연결 실패");
                    AppendLog($"[PLC] {plcType} 연결 실패");
                }
            }
            catch (Exception ex)
            {
                UpdatePlcStatus(false, "연결 실패");
                AppendLog($"[PLC] 연결 오류: {ex.Message}");
            }
        }

        private void btnPlcDisconnect_Click(object sender, EventArgs e)
        {
            _mitsubishiPlc?.Disconnect();
            _siemensPlc?.Disconnect();
            _lsPlc?.Disconnect();
            _modbusClient?.Disconnect();

            _mitsubishiPlc = null;
            _siemensPlc = null;
            _lsPlc = null;
            _modbusClient = null;

            UpdatePlcStatus(false, "연결 안됨");
            AppendLog("[PLC] 연결 해제");
        }

        private void UpdatePlcStatus(bool connected, string message)
        {
            lblPlcStatus.Text = message;
            lblPlcStatus.ForeColor = connected ? Color.Green : Color.Gray;
        }

        private IPlcCommunication GetActivePlc()
        {
            if (_mitsubishiPlc?.IsConnected == true) return _mitsubishiPlc;
            if (_siemensPlc?.IsConnected == true) return _siemensPlc;
            if (_lsPlc?.IsConnected == true) return _lsPlc;
            if (_modbusClient?.IsConnected == true) return _modbusClient;
            return null;
        }

        private void btnPlcReadWord_Click(object sender, EventArgs e)
        {
            try
            {
                var plc = GetActivePlc();
                if (plc == null)
                {
                    MessageBox.Show("먼저 PLC에 연결하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string device = txtPlcDevice.Text;
                int address = (int)numPlcAddress.Value;
                int count = (int)numPlcCount.Value;

                var result = plc.ReadWords(device, address, count);
                if (result.IsSuccess)
                {
                    StringBuilder sb = new StringBuilder();
                    if (chkPlcDisplayAsString.Checked)
                    {
                        // 문자열로 표시 (ByteOrder에 따라 변환)
                        StringBuilder strBuilder = new StringBuilder();
                        bool isBigEndian = cmbStringByteOrder.SelectedIndex == 0; // Big (AB)
                        
                        foreach (short val in result.Value)
                        {
                            byte lowByte = (byte)(val & 0xFF);
                            byte highByte = (byte)((val >> 8) & 0xFF);
                            
                            if (isBigEndian)
                            {
                                // Big Endian: High byte first (AB) - Siemens
                                if (highByte >= 0x20 && highByte <= 0x7E) strBuilder.Append((char)highByte);
                                if (lowByte >= 0x20 && lowByte <= 0x7E) strBuilder.Append((char)lowByte);
                            }
                            else
                            {
                                // Little Endian: Low byte first (BA) - Mitsubishi, LS
                                if (lowByte >= 0x20 && lowByte <= 0x7E) strBuilder.Append((char)lowByte);
                                if (highByte >= 0x20 && highByte <= 0x7E) strBuilder.Append((char)highByte);
                            }
                        }
                        sb.Append($"{device}{address}~{device}{address + count - 1} = \"{strBuilder}\"");
                    }
                    else
                    {
                        // 숫자로 표시
                        for (int i = 0; i < result.Value.Length; i++)
                        {
                            sb.Append($"{device}{address + i}={result.Value[i]} ");
                        }
                    }
                    txtPlcResult.Text = sb.ToString();
                    AppendLog($"[PLC 읽기] {sb}");
                }
                else
                {
                    txtPlcResult.Text = result.ErrorMessage;
                    AppendLog($"[PLC 읽기 실패] {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[PLC] 읽기 오류: {ex.Message}");
            }
        }

        private void btnPlcWriteWord_Click(object sender, EventArgs e)
        {
            try
            {
                var plc = GetActivePlc();
                if (plc == null)
                {
                    MessageBox.Show("먼저 PLC에 연결하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string device = txtPlcDevice.Text;
                int address = (int)numPlcAddress.Value;

                PlcResult result;
                string logMessage;

                if (chkPlcDisplayAsString.Checked)
                {
                    // 문자열 모드: 문자열을 워드 배열로 변환하여 쓰기
                    string strValue = txtPlcWriteString.Text ?? "";
                    short[] words = StringToWords(strValue);
                    result = plc.WriteWords(device, address, words);
                    logMessage = $"{device}{address}~{device}{address + words.Length - 1}=\"{strValue}\"";
                }
                else
                {
                    // 숫자 모드: 단일 워드 쓰기
                    short value = (short)numPlcWriteValue.Value;
                    result = plc.WriteWord(device, address, value);
                    logMessage = $"{device}{address}={value}";
                }

                if (result.IsSuccess)
                {
                    txtPlcResult.Text = "쓰기 성공";
                    AppendLog($"[PLC 쓰기] {logMessage}");
                }
                else
                {
                    txtPlcResult.Text = result.ErrorMessage;
                    AppendLog($"[PLC 쓰기 실패] {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[PLC] 쓰기 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 문자열을 워드 배열로 변환 (PLC 쓰기용)
        /// </summary>
        private short[] StringToWords(string str)
        {
            if (string.IsNullOrEmpty(str)) return new short[0];
            
            // 짝수 길이로 맞춤
            int paddedLength = (str.Length + 1) / 2 * 2;
            byte[] bytes = new byte[paddedLength];
            byte[] strBytes = System.Text.Encoding.ASCII.GetBytes(str);
            Array.Copy(strBytes, bytes, strBytes.Length);

            // 바이트를 워드로 변환 (Little Endian)
            short[] words = new short[paddedLength / 2];
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = (short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
            }
            return words;
        }

        private void btnPlcReadBit_Click(object sender, EventArgs e)
        {
            try
            {
                var plc = GetActivePlc();
                if (plc == null)
                {
                    MessageBox.Show("먼저 PLC에 연결하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string device = txtPlcDevice.Text;
                int address = (int)numPlcAddress.Value;

                var result = plc.ReadBit(device, address);
                if (result.IsSuccess)
                {
                    txtPlcResult.Text = $"{device}{address}={result.Value}";
                    AppendLog($"[PLC 비트 읽기] {device}{address}={result.Value}");
                }
                else
                {
                    txtPlcResult.Text = result.ErrorMessage;
                    AppendLog($"[PLC 비트 읽기 실패] {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[PLC] 비트 읽기 오류: {ex.Message}");
            }
        }

        private void btnPlcWriteBit_Click(object sender, EventArgs e)
        {
            try
            {
                var plc = GetActivePlc();
                if (plc == null)
                {
                    MessageBox.Show("먼저 PLC에 연결하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string device = txtPlcDevice.Text;
                int address = (int)numPlcAddress.Value;
                bool value = chkPlcBitValue.Checked;

                var result = plc.WriteBit(device, address, value);
                if (result.IsSuccess)
                {
                    txtPlcResult.Text = "비트 쓰기 성공";
                    AppendLog($"[PLC 비트 쓰기] {device}{address}={value}");
                }
                else
                {
                    txtPlcResult.Text = result.ErrorMessage;
                    AppendLog($"[PLC 비트 쓰기 실패] {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[PLC] 비트 쓰기 오류: {ex.Message}");
            }
        }

        private void cmbPlcType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string plcType = cmbPlcType.SelectedItem?.ToString() ?? "";

            // PLC 타입에 따라 기본 포트 설정
            switch (plcType)
            {
                case "Mitsubishi MC":
                    numPlcPort.Value = 5001;
                    pnlS7Options.Visible = false;
                    pnlModbusOptions.Visible = false;
                    if (cmbStringByteOrder.Items.Count > 1) cmbStringByteOrder.SelectedIndex = 1; // Little Endian
                    break;
                case "Siemens S7":
                    numPlcPort.Value = 102;
                    pnlS7Options.Visible = true;
                    pnlModbusOptions.Visible = false;
                    if (cmbStringByteOrder.Items.Count > 0) cmbStringByteOrder.SelectedIndex = 0; // Big Endian
                    break;
                case "LS Electric XGT":
                    numPlcPort.Value = 2004;
                    pnlS7Options.Visible = false;
                    pnlModbusOptions.Visible = false;
                    if (cmbStringByteOrder.Items.Count > 1) cmbStringByteOrder.SelectedIndex = 1; // Little Endian
                    break;
                case "Modbus TCP":
                    numPlcPort.Value = 502;
                    pnlS7Options.Visible = false;
                    pnlModbusOptions.Visible = true;
                    if (cmbStringByteOrder.Items.Count > 0) cmbStringByteOrder.SelectedIndex = 0; // Big Endian
                    break;
                case "Modbus RTU":
                    pnlS7Options.Visible = false;
                    pnlModbusOptions.Visible = true;
                    if (cmbStringByteOrder.Items.Count > 0) cmbStringByteOrder.SelectedIndex = 0; // Big Endian
                    break;
            }
        }

        private void chkPlcDisplayAsString_CheckedChanged(object sender, EventArgs e)
        {
            // 문자열 모드일 때 입력 컨트롤 전환
            bool isStringMode = chkPlcDisplayAsString.Checked;
            numPlcWriteValue.Visible = !isStringMode;
            txtPlcWriteString.Visible = isStringMode;
        }

        #endregion

        #region Utility

        private byte[] ParseHexString(string hex)
        {
            // 공백, 하이픈 제거
            hex = hex.Replace(" ", "").Replace("-", "").Replace("0x", "").Replace("0X", "");

            if (hex.Length % 2 != 0)
                hex = "0" + hex;

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        private void AppendLog(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke((Action)(() => AppendLog(message)));
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        #endregion
    }
}
