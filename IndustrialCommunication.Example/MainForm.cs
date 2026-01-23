using System;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;
using IndustrialCommunication.Communication.Interfaces;
using IndustrialCommunication.Communication.PLC;
using IndustrialCommunication.Communication.Serial;
using IndustrialCommunication.Communication.Socket;
using IndustrialCommunication.Database;

namespace IndustrialCommunication.Example
{
    public partial class MainForm : Form
    {
        #region Fields

        private MssqlHelper _dbHelper;
        private TcpClientHelper _tcpClient;
        private UdpHelper _udpClient;
        private SerialPortHelper _serialPort;
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
                    for (int i = 0; i < result.Value.Length; i++)
                    {
                        sb.Append($"{device}{address + i}={result.Value[i]} ");
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
                short value = (short)numPlcWriteValue.Value;

                var result = plc.WriteWord(device, address, value);
                if (result.IsSuccess)
                {
                    txtPlcResult.Text = "쓰기 성공";
                    AppendLog($"[PLC 쓰기] {device}{address}={value}");
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
                    break;
                case "Siemens S7":
                    numPlcPort.Value = 102;
                    pnlS7Options.Visible = true;
                    pnlModbusOptions.Visible = false;
                    break;
                case "LS Electric XGT":
                    numPlcPort.Value = 2004;
                    pnlS7Options.Visible = false;
                    pnlModbusOptions.Visible = false;
                    break;
                case "Modbus TCP":
                    numPlcPort.Value = 502;
                    pnlS7Options.Visible = false;
                    pnlModbusOptions.Visible = true;
                    break;
                case "Modbus RTU":
                    pnlS7Options.Visible = false;
                    pnlModbusOptions.Visible = true;
                    break;
            }
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
