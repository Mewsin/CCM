namespace CCM.Example
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabDatabase = new System.Windows.Forms.TabPage();
            this.tabSocket = new System.Windows.Forms.TabPage();
            this.tabSerial = new System.Windows.Forms.TabPage();
            this.tabPlc = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.btnClearLog = new System.Windows.Forms.Button();

            // Database Tab Controls
            this.grpDbConnection = new System.Windows.Forms.GroupBox();
            this.txtDbConnectionString = new System.Windows.Forms.TextBox();
            this.btnDbConnect = new System.Windows.Forms.Button();
            this.btnDbDisconnect = new System.Windows.Forms.Button();
            this.lblDbStatus = new System.Windows.Forms.Label();
            this.grpDbQuery = new System.Windows.Forms.GroupBox();
            this.txtDbQuery = new System.Windows.Forms.TextBox();
            this.btnDbExecuteQuery = new System.Windows.Forms.Button();
            this.btnDbExecuteNonQuery = new System.Windows.Forms.Button();
            this.btnDbExecuteProcedure = new System.Windows.Forms.Button();
            this.dgvDbResult = new System.Windows.Forms.DataGridView();

            // TCP Server Controls
            this.grpTcpServer = new System.Windows.Forms.GroupBox();
            this.lblTcpServerPort = new System.Windows.Forms.Label();
            this.numTcpServerPort = new System.Windows.Forms.NumericUpDown();
            this.btnTcpServerStart = new System.Windows.Forms.Button();
            this.btnTcpServerStop = new System.Windows.Forms.Button();
            this.lblTcpServerStatus = new System.Windows.Forms.Label();
            this.lstTcpServerClients = new System.Windows.Forms.ListBox();
            this.txtTcpServerSendData = new System.Windows.Forms.TextBox();
            this.btnTcpServerSend = new System.Windows.Forms.Button();
            this.chkTcpServerBroadcast = new System.Windows.Forms.CheckBox();
            this.btnTcpServerDisconnectClient = new System.Windows.Forms.Button();

            // TCP Controls
            this.grpTcp = new System.Windows.Forms.GroupBox();
            this.txtTcpIp = new System.Windows.Forms.TextBox();
            this.numTcpPort = new System.Windows.Forms.NumericUpDown();
            this.btnTcpConnect = new System.Windows.Forms.Button();
            this.btnTcpDisconnect = new System.Windows.Forms.Button();
            this.lblTcpStatus = new System.Windows.Forms.Label();
            this.txtTcpSendData = new System.Windows.Forms.TextBox();
            this.btnTcpSend = new System.Windows.Forms.Button();

            // UDP Controls
            this.grpUdp = new System.Windows.Forms.GroupBox();
            this.txtUdpRemoteIp = new System.Windows.Forms.TextBox();
            this.numUdpRemotePort = new System.Windows.Forms.NumericUpDown();
            this.numUdpLocalPort = new System.Windows.Forms.NumericUpDown();
            this.btnUdpStart = new System.Windows.Forms.Button();
            this.btnUdpStop = new System.Windows.Forms.Button();
            this.lblUdpStatus = new System.Windows.Forms.Label();
            this.txtUdpSendData = new System.Windows.Forms.TextBox();
            this.btnUdpSend = new System.Windows.Forms.Button();

            // Serial Controls
            this.grpSerial = new System.Windows.Forms.GroupBox();
            this.cmbSerialPort = new System.Windows.Forms.ComboBox();
            this.numSerialBaudRate = new System.Windows.Forms.NumericUpDown();
            this.btnSerialRefresh = new System.Windows.Forms.Button();
            this.btnSerialConnect = new System.Windows.Forms.Button();
            this.btnSerialDisconnect = new System.Windows.Forms.Button();
            this.lblSerialStatus = new System.Windows.Forms.Label();
            this.txtSerialSendData = new System.Windows.Forms.TextBox();
            this.btnSerialSend = new System.Windows.Forms.Button();

            // PLC Controls
            this.grpPlcConnection = new System.Windows.Forms.GroupBox();
            this.cmbPlcType = new System.Windows.Forms.ComboBox();
            this.txtPlcIp = new System.Windows.Forms.TextBox();
            this.numPlcPort = new System.Windows.Forms.NumericUpDown();
            this.btnPlcConnect = new System.Windows.Forms.Button();
            this.btnPlcDisconnect = new System.Windows.Forms.Button();
            this.lblPlcStatus = new System.Windows.Forms.Label();
            this.pnlS7Options = new System.Windows.Forms.Panel();
            this.cmbS7CpuType = new System.Windows.Forms.ComboBox();
            this.numS7Rack = new System.Windows.Forms.NumericUpDown();
            this.numS7Slot = new System.Windows.Forms.NumericUpDown();
            this.pnlModbusOptions = new System.Windows.Forms.Panel();
            this.numModbusSlave = new System.Windows.Forms.NumericUpDown();
            this.cmbModbusPort = new System.Windows.Forms.ComboBox();
            this.numModbusBaudRate = new System.Windows.Forms.NumericUpDown();
            this.grpPlcReadWrite = new System.Windows.Forms.GroupBox();
            this.txtPlcDevice = new System.Windows.Forms.TextBox();
            this.numPlcAddress = new System.Windows.Forms.NumericUpDown();
            this.numPlcCount = new System.Windows.Forms.NumericUpDown();
            this.numPlcWriteValue = new System.Windows.Forms.NumericUpDown();
            this.chkPlcBitValue = new System.Windows.Forms.CheckBox();
            this.btnPlcReadWord = new System.Windows.Forms.Button();
            this.btnPlcWriteWord = new System.Windows.Forms.Button();
            this.btnPlcReadBit = new System.Windows.Forms.Button();
            this.btnPlcWriteBit = new System.Windows.Forms.Button();
            this.txtPlcResult = new System.Windows.Forms.TextBox();

            // Labels
            this.lblTcpIp = new System.Windows.Forms.Label();
            this.lblTcpPort = new System.Windows.Forms.Label();
            this.lblUdpRemoteIp = new System.Windows.Forms.Label();
            this.lblUdpRemotePort = new System.Windows.Forms.Label();
            this.lblUdpLocalPort = new System.Windows.Forms.Label();
            this.lblSerialPort = new System.Windows.Forms.Label();
            this.lblSerialBaudRate = new System.Windows.Forms.Label();
            this.lblPlcType = new System.Windows.Forms.Label();
            this.lblPlcIp = new System.Windows.Forms.Label();
            this.lblPlcPort = new System.Windows.Forms.Label();
            this.lblPlcDevice = new System.Windows.Forms.Label();
            this.lblPlcAddress = new System.Windows.Forms.Label();
            this.lblPlcCount = new System.Windows.Forms.Label();
            this.lblPlcWriteValue = new System.Windows.Forms.Label();
            this.lblS7CpuType = new System.Windows.Forms.Label();
            this.lblS7Rack = new System.Windows.Forms.Label();
            this.lblS7Slot = new System.Windows.Forms.Label();
            this.lblModbusSlave = new System.Windows.Forms.Label();

            this.tabControl1.SuspendLayout();
            this.tabDatabase.SuspendLayout();
            this.tabSocket.SuspendLayout();
            this.tabSerial.SuspendLayout();
            this.tabPlc.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.grpDbConnection.SuspendLayout();
            this.grpDbQuery.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDbResult)).BeginInit();
            this.grpTcpServer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTcpServerPort)).BeginInit();
            this.grpTcp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTcpPort)).BeginInit();
            this.grpUdp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numUdpRemotePort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUdpLocalPort)).BeginInit();
            this.grpSerial.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSerialBaudRate)).BeginInit();
            this.grpPlcConnection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPlcPort)).BeginInit();
            this.pnlS7Options.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numS7Rack)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numS7Slot)).BeginInit();
            this.pnlModbusOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numModbusSlave)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numModbusBaudRate)).BeginInit();
            this.grpPlcReadWrite.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPlcAddress)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPlcCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPlcWriteValue)).BeginInit();
            this.SuspendLayout();

            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitContainer1.Panel1.Controls.Add(this.tabControl1);
            this.splitContainer1.Panel2.Controls.Add(this.txtLog);
            this.splitContainer1.Panel2.Controls.Add(this.btnClearLog);
            this.splitContainer1.Size = new System.Drawing.Size(1000, 700);
            this.splitContainer1.SplitterDistance = 500;
            this.splitContainer1.TabIndex = 0;

            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabDatabase);
            this.tabControl1.Controls.Add(this.tabSocket);
            this.tabControl1.Controls.Add(this.tabSerial);
            this.tabControl1.Controls.Add(this.tabPlc);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1000, 500);
            this.tabControl1.TabIndex = 0;

            // 
            // tabDatabase
            // 
            this.tabDatabase.Controls.Add(this.grpDbConnection);
            this.tabDatabase.Controls.Add(this.grpDbQuery);
            this.tabDatabase.Controls.Add(this.dgvDbResult);
            this.tabDatabase.Location = new System.Drawing.Point(4, 22);
            this.tabDatabase.Name = "tabDatabase";
            this.tabDatabase.Padding = new System.Windows.Forms.Padding(3);
            this.tabDatabase.Size = new System.Drawing.Size(992, 474);
            this.tabDatabase.TabIndex = 0;
            this.tabDatabase.Text = "Database (MSSQL)";
            this.tabDatabase.UseVisualStyleBackColor = true;

            // 
            // grpDbConnection
            // 
            this.grpDbConnection.Controls.Add(this.txtDbConnectionString);
            this.grpDbConnection.Controls.Add(this.btnDbConnect);
            this.grpDbConnection.Controls.Add(this.btnDbDisconnect);
            this.grpDbConnection.Controls.Add(this.lblDbStatus);
            this.grpDbConnection.Location = new System.Drawing.Point(6, 6);
            this.grpDbConnection.Name = "grpDbConnection";
            this.grpDbConnection.Size = new System.Drawing.Size(980, 60);
            this.grpDbConnection.TabIndex = 0;
            this.grpDbConnection.TabStop = false;
            this.grpDbConnection.Text = "연결 설정";

            // 
            // txtDbConnectionString
            // 
            this.txtDbConnectionString.Location = new System.Drawing.Point(6, 25);
            this.txtDbConnectionString.Name = "txtDbConnectionString";
            this.txtDbConnectionString.Size = new System.Drawing.Size(650, 21);
            this.txtDbConnectionString.TabIndex = 0;
            this.txtDbConnectionString.Text = "Server=localhost;Database=TestDB;User Id=sa;Password=;";

            // 
            // btnDbConnect
            // 
            this.btnDbConnect.Location = new System.Drawing.Point(670, 23);
            this.btnDbConnect.Name = "btnDbConnect";
            this.btnDbConnect.Size = new System.Drawing.Size(75, 23);
            this.btnDbConnect.TabIndex = 1;
            this.btnDbConnect.Text = "연결";
            this.btnDbConnect.UseVisualStyleBackColor = true;
            this.btnDbConnect.Click += new System.EventHandler(this.btnDbConnect_Click);

            // 
            // btnDbDisconnect
            // 
            this.btnDbDisconnect.Location = new System.Drawing.Point(751, 23);
            this.btnDbDisconnect.Name = "btnDbDisconnect";
            this.btnDbDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnDbDisconnect.TabIndex = 2;
            this.btnDbDisconnect.Text = "연결 해제";
            this.btnDbDisconnect.UseVisualStyleBackColor = true;
            this.btnDbDisconnect.Click += new System.EventHandler(this.btnDbDisconnect_Click);

            // 
            // lblDbStatus
            // 
            this.lblDbStatus.AutoSize = true;
            this.lblDbStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblDbStatus.Location = new System.Drawing.Point(840, 28);
            this.lblDbStatus.Name = "lblDbStatus";
            this.lblDbStatus.Size = new System.Drawing.Size(60, 12);
            this.lblDbStatus.TabIndex = 3;
            this.lblDbStatus.Text = "연결 안됨";

            // 
            // grpDbQuery
            // 
            this.grpDbQuery.Controls.Add(this.txtDbQuery);
            this.grpDbQuery.Controls.Add(this.btnDbExecuteQuery);
            this.grpDbQuery.Controls.Add(this.btnDbExecuteNonQuery);
            this.grpDbQuery.Controls.Add(this.btnDbExecuteProcedure);
            this.grpDbQuery.Location = new System.Drawing.Point(6, 72);
            this.grpDbQuery.Name = "grpDbQuery";
            this.grpDbQuery.Size = new System.Drawing.Size(980, 100);
            this.grpDbQuery.TabIndex = 1;
            this.grpDbQuery.TabStop = false;
            this.grpDbQuery.Text = "쿼리";

            // 
            // txtDbQuery
            // 
            this.txtDbQuery.Location = new System.Drawing.Point(6, 20);
            this.txtDbQuery.Multiline = true;
            this.txtDbQuery.Name = "txtDbQuery";
            this.txtDbQuery.Size = new System.Drawing.Size(730, 70);
            this.txtDbQuery.TabIndex = 0;
            this.txtDbQuery.Text = "SELECT TOP 10 * FROM sys.tables";

            // 
            // btnDbExecuteQuery
            // 
            this.btnDbExecuteQuery.Location = new System.Drawing.Point(750, 20);
            this.btnDbExecuteQuery.Name = "btnDbExecuteQuery";
            this.btnDbExecuteQuery.Size = new System.Drawing.Size(100, 23);
            this.btnDbExecuteQuery.TabIndex = 1;
            this.btnDbExecuteQuery.Text = "SELECT 실행";
            this.btnDbExecuteQuery.UseVisualStyleBackColor = true;
            this.btnDbExecuteQuery.Click += new System.EventHandler(this.btnDbExecuteQuery_Click);

            // 
            // btnDbExecuteNonQuery
            // 
            this.btnDbExecuteNonQuery.Location = new System.Drawing.Point(750, 49);
            this.btnDbExecuteNonQuery.Name = "btnDbExecuteNonQuery";
            this.btnDbExecuteNonQuery.Size = new System.Drawing.Size(100, 23);
            this.btnDbExecuteNonQuery.TabIndex = 2;
            this.btnDbExecuteNonQuery.Text = "NonQuery 실행";
            this.btnDbExecuteNonQuery.UseVisualStyleBackColor = true;
            this.btnDbExecuteNonQuery.Click += new System.EventHandler(this.btnDbExecuteNonQuery_Click);

            // 
            // btnDbExecuteProcedure
            // 
            this.btnDbExecuteProcedure.Location = new System.Drawing.Point(860, 20);
            this.btnDbExecuteProcedure.Name = "btnDbExecuteProcedure";
            this.btnDbExecuteProcedure.Size = new System.Drawing.Size(100, 23);
            this.btnDbExecuteProcedure.TabIndex = 3;
            this.btnDbExecuteProcedure.Text = "프로시저 실행";
            this.btnDbExecuteProcedure.UseVisualStyleBackColor = true;
            this.btnDbExecuteProcedure.Click += new System.EventHandler(this.btnDbExecuteProcedure_Click);

            // 
            // dgvDbResult
            // 
            this.dgvDbResult.AllowUserToAddRows = false;
            this.dgvDbResult.AllowUserToDeleteRows = false;
            this.dgvDbResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDbResult.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDbResult.Location = new System.Drawing.Point(6, 178);
            this.dgvDbResult.Name = "dgvDbResult";
            this.dgvDbResult.ReadOnly = true;
            this.dgvDbResult.RowTemplate.Height = 23;
            this.dgvDbResult.Size = new System.Drawing.Size(980, 290);
            this.dgvDbResult.TabIndex = 2;

            // 
            // tabSocket
            // 
            this.tabSocket.Controls.Add(this.grpTcpServer);
            this.tabSocket.Controls.Add(this.grpTcp);
            this.tabSocket.Controls.Add(this.grpUdp);
            this.tabSocket.Location = new System.Drawing.Point(4, 22);
            this.tabSocket.Name = "tabSocket";
            this.tabSocket.Padding = new System.Windows.Forms.Padding(3);
            this.tabSocket.Size = new System.Drawing.Size(992, 474);
            this.tabSocket.TabIndex = 1;
            this.tabSocket.Text = "Socket (TCP/UDP)";
            this.tabSocket.UseVisualStyleBackColor = true;

            // 
            // grpTcpServer
            // 
            this.grpTcpServer.Controls.Add(this.lblTcpServerPort);
            this.grpTcpServer.Controls.Add(this.numTcpServerPort);
            this.grpTcpServer.Controls.Add(this.btnTcpServerStart);
            this.grpTcpServer.Controls.Add(this.btnTcpServerStop);
            this.grpTcpServer.Controls.Add(this.lblTcpServerStatus);
            this.grpTcpServer.Controls.Add(this.lstTcpServerClients);
            this.grpTcpServer.Controls.Add(this.txtTcpServerSendData);
            this.grpTcpServer.Controls.Add(this.btnTcpServerSend);
            this.grpTcpServer.Controls.Add(this.chkTcpServerBroadcast);
            this.grpTcpServer.Controls.Add(this.btnTcpServerDisconnectClient);
            this.grpTcpServer.Location = new System.Drawing.Point(6, 6);
            this.grpTcpServer.Name = "grpTcpServer";
            this.grpTcpServer.Size = new System.Drawing.Size(980, 130);
            this.grpTcpServer.TabIndex = 0;
            this.grpTcpServer.TabStop = false;
            this.grpTcpServer.Text = "TCP Server";

            // 
            // lblTcpServerPort
            // 
            this.lblTcpServerPort.AutoSize = true;
            this.lblTcpServerPort.Location = new System.Drawing.Point(10, 25);
            this.lblTcpServerPort.Name = "lblTcpServerPort";
            this.lblTcpServerPort.Size = new System.Drawing.Size(27, 12);
            this.lblTcpServerPort.TabIndex = 0;
            this.lblTcpServerPort.Text = "Port";

            // 
            // numTcpServerPort
            // 
            this.numTcpServerPort.Location = new System.Drawing.Point(50, 22);
            this.numTcpServerPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numTcpServerPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numTcpServerPort.Name = "numTcpServerPort";
            this.numTcpServerPort.Size = new System.Drawing.Size(80, 21);
            this.numTcpServerPort.TabIndex = 1;
            this.numTcpServerPort.Value = new decimal(new int[] { 9000, 0, 0, 0 });

            // 
            // btnTcpServerStart
            // 
            this.btnTcpServerStart.Location = new System.Drawing.Point(140, 20);
            this.btnTcpServerStart.Name = "btnTcpServerStart";
            this.btnTcpServerStart.Size = new System.Drawing.Size(75, 23);
            this.btnTcpServerStart.TabIndex = 2;
            this.btnTcpServerStart.Text = "시작";
            this.btnTcpServerStart.UseVisualStyleBackColor = true;
            this.btnTcpServerStart.Click += new System.EventHandler(this.btnTcpServerStart_Click);

            // 
            // btnTcpServerStop
            // 
            this.btnTcpServerStop.Location = new System.Drawing.Point(221, 20);
            this.btnTcpServerStop.Name = "btnTcpServerStop";
            this.btnTcpServerStop.Size = new System.Drawing.Size(75, 23);
            this.btnTcpServerStop.TabIndex = 3;
            this.btnTcpServerStop.Text = "중지";
            this.btnTcpServerStop.UseVisualStyleBackColor = true;
            this.btnTcpServerStop.Click += new System.EventHandler(this.btnTcpServerStop_Click);

            // 
            // lblTcpServerStatus
            // 
            this.lblTcpServerStatus.AutoSize = true;
            this.lblTcpServerStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblTcpServerStatus.Location = new System.Drawing.Point(310, 25);
            this.lblTcpServerStatus.Name = "lblTcpServerStatus";
            this.lblTcpServerStatus.Size = new System.Drawing.Size(41, 12);
            this.lblTcpServerStatus.TabIndex = 4;
            this.lblTcpServerStatus.Text = "중지됨";

            // 
            // lstTcpServerClients
            // 
            this.lstTcpServerClients.FormattingEnabled = true;
            this.lstTcpServerClients.ItemHeight = 12;
            this.lstTcpServerClients.Location = new System.Drawing.Point(400, 15);
            this.lstTcpServerClients.Name = "lstTcpServerClients";
            this.lstTcpServerClients.Size = new System.Drawing.Size(250, 100);
            this.lstTcpServerClients.TabIndex = 5;

            // 
            // btnTcpServerDisconnectClient
            // 
            this.btnTcpServerDisconnectClient.Location = new System.Drawing.Point(660, 15);
            this.btnTcpServerDisconnectClient.Name = "btnTcpServerDisconnectClient";
            this.btnTcpServerDisconnectClient.Size = new System.Drawing.Size(90, 23);
            this.btnTcpServerDisconnectClient.TabIndex = 6;
            this.btnTcpServerDisconnectClient.Text = "클라이언트 해제";
            this.btnTcpServerDisconnectClient.UseVisualStyleBackColor = true;
            this.btnTcpServerDisconnectClient.Click += new System.EventHandler(this.btnTcpServerDisconnectClient_Click);

            // 
            // txtTcpServerSendData
            // 
            this.txtTcpServerSendData.Location = new System.Drawing.Point(50, 55);
            this.txtTcpServerSendData.Name = "txtTcpServerSendData";
            this.txtTcpServerSendData.Size = new System.Drawing.Size(250, 21);
            this.txtTcpServerSendData.TabIndex = 7;
            this.txtTcpServerSendData.Text = "48 45 4C 4C 4F";

            // 
            // btnTcpServerSend
            // 
            this.btnTcpServerSend.Location = new System.Drawing.Point(50, 85);
            this.btnTcpServerSend.Name = "btnTcpServerSend";
            this.btnTcpServerSend.Size = new System.Drawing.Size(75, 23);
            this.btnTcpServerSend.TabIndex = 8;
            this.btnTcpServerSend.Text = "전송 (Hex)";
            this.btnTcpServerSend.UseVisualStyleBackColor = true;
            this.btnTcpServerSend.Click += new System.EventHandler(this.btnTcpServerSend_Click);

            // 
            // chkTcpServerBroadcast
            // 
            this.chkTcpServerBroadcast.AutoSize = true;
            this.chkTcpServerBroadcast.Location = new System.Drawing.Point(140, 89);
            this.chkTcpServerBroadcast.Name = "chkTcpServerBroadcast";
            this.chkTcpServerBroadcast.Size = new System.Drawing.Size(108, 16);
            this.chkTcpServerBroadcast.TabIndex = 9;
            this.chkTcpServerBroadcast.Text = "전체 전송 (All)";
            this.chkTcpServerBroadcast.UseVisualStyleBackColor = true;

            // 
            // grpTcp
            // 
            this.grpTcp.Controls.Add(this.lblTcpIp);
            this.grpTcp.Controls.Add(this.txtTcpIp);
            this.grpTcp.Controls.Add(this.lblTcpPort);
            this.grpTcp.Controls.Add(this.numTcpPort);
            this.grpTcp.Controls.Add(this.btnTcpConnect);
            this.grpTcp.Controls.Add(this.btnTcpDisconnect);
            this.grpTcp.Controls.Add(this.lblTcpStatus);
            this.grpTcp.Controls.Add(this.txtTcpSendData);
            this.grpTcp.Controls.Add(this.btnTcpSend);
            this.grpTcp.Location = new System.Drawing.Point(6, 142);
            this.grpTcp.Name = "grpTcp";
            this.grpTcp.Size = new System.Drawing.Size(980, 100);
            this.grpTcp.TabIndex = 1;
            this.grpTcp.TabStop = false;
            this.grpTcp.Text = "TCP Client";

            // 
            // lblTcpIp
            // 
            this.lblTcpIp.AutoSize = true;
            this.lblTcpIp.Location = new System.Drawing.Point(10, 28);
            this.lblTcpIp.Name = "lblTcpIp";
            this.lblTcpIp.Size = new System.Drawing.Size(17, 12);
            this.lblTcpIp.TabIndex = 0;
            this.lblTcpIp.Text = "IP";

            // 
            // txtTcpIp
            // 
            this.txtTcpIp.Location = new System.Drawing.Point(50, 25);
            this.txtTcpIp.Name = "txtTcpIp";
            this.txtTcpIp.Size = new System.Drawing.Size(120, 21);
            this.txtTcpIp.TabIndex = 1;
            this.txtTcpIp.Text = "127.0.0.1";

            // 
            // lblTcpPort
            // 
            this.lblTcpPort.AutoSize = true;
            this.lblTcpPort.Location = new System.Drawing.Point(180, 28);
            this.lblTcpPort.Name = "lblTcpPort";
            this.lblTcpPort.Size = new System.Drawing.Size(27, 12);
            this.lblTcpPort.TabIndex = 2;
            this.lblTcpPort.Text = "Port";

            // 
            // numTcpPort
            // 
            this.numTcpPort.Location = new System.Drawing.Point(220, 25);
            this.numTcpPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numTcpPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numTcpPort.Name = "numTcpPort";
            this.numTcpPort.Size = new System.Drawing.Size(80, 21);
            this.numTcpPort.TabIndex = 3;
            this.numTcpPort.Value = new decimal(new int[] { 8000, 0, 0, 0 });

            // 
            // btnTcpConnect
            // 
            this.btnTcpConnect.Location = new System.Drawing.Point(320, 23);
            this.btnTcpConnect.Name = "btnTcpConnect";
            this.btnTcpConnect.Size = new System.Drawing.Size(75, 23);
            this.btnTcpConnect.TabIndex = 4;
            this.btnTcpConnect.Text = "연결";
            this.btnTcpConnect.UseVisualStyleBackColor = true;
            this.btnTcpConnect.Click += new System.EventHandler(this.btnTcpConnect_Click);

            // 
            // btnTcpDisconnect
            // 
            this.btnTcpDisconnect.Location = new System.Drawing.Point(401, 23);
            this.btnTcpDisconnect.Name = "btnTcpDisconnect";
            this.btnTcpDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnTcpDisconnect.TabIndex = 5;
            this.btnTcpDisconnect.Text = "연결 해제";
            this.btnTcpDisconnect.UseVisualStyleBackColor = true;
            this.btnTcpDisconnect.Click += new System.EventHandler(this.btnTcpDisconnect_Click);

            // 
            // lblTcpStatus
            // 
            this.lblTcpStatus.AutoSize = true;
            this.lblTcpStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblTcpStatus.Location = new System.Drawing.Point(490, 28);
            this.lblTcpStatus.Name = "lblTcpStatus";
            this.lblTcpStatus.Size = new System.Drawing.Size(60, 12);
            this.lblTcpStatus.TabIndex = 6;
            this.lblTcpStatus.Text = "연결 안됨";

            // 
            // txtTcpSendData
            // 
            this.txtTcpSendData.Location = new System.Drawing.Point(50, 60);
            this.txtTcpSendData.Name = "txtTcpSendData";
            this.txtTcpSendData.Size = new System.Drawing.Size(400, 21);
            this.txtTcpSendData.TabIndex = 7;
            this.txtTcpSendData.Text = "48 45 4C 4C 4F";

            // 
            // btnTcpSend
            // 
            this.btnTcpSend.Location = new System.Drawing.Point(460, 58);
            this.btnTcpSend.Name = "btnTcpSend";
            this.btnTcpSend.Size = new System.Drawing.Size(75, 23);
            this.btnTcpSend.TabIndex = 8;
            this.btnTcpSend.Text = "전송 (Hex)";
            this.btnTcpSend.UseVisualStyleBackColor = true;
            this.btnTcpSend.Click += new System.EventHandler(this.btnTcpSend_Click);

            // 
            // grpUdp
            // 
            this.grpUdp.Controls.Add(this.lblUdpRemoteIp);
            this.grpUdp.Controls.Add(this.txtUdpRemoteIp);
            this.grpUdp.Controls.Add(this.lblUdpRemotePort);
            this.grpUdp.Controls.Add(this.numUdpRemotePort);
            this.grpUdp.Controls.Add(this.lblUdpLocalPort);
            this.grpUdp.Controls.Add(this.numUdpLocalPort);
            this.grpUdp.Controls.Add(this.btnUdpStart);
            this.grpUdp.Controls.Add(this.btnUdpStop);
            this.grpUdp.Controls.Add(this.lblUdpStatus);
            this.grpUdp.Controls.Add(this.txtUdpSendData);
            this.grpUdp.Controls.Add(this.btnUdpSend);
            this.grpUdp.Location = new System.Drawing.Point(6, 248);
            this.grpUdp.Name = "grpUdp";
            this.grpUdp.Size = new System.Drawing.Size(980, 100);
            this.grpUdp.TabIndex = 2;
            this.grpUdp.TabStop = false;
            this.grpUdp.Text = "UDP";

            // 
            // lblUdpRemoteIp
            // 
            this.lblUdpRemoteIp.AutoSize = true;
            this.lblUdpRemoteIp.Location = new System.Drawing.Point(10, 28);
            this.lblUdpRemoteIp.Name = "lblUdpRemoteIp";
            this.lblUdpRemoteIp.Size = new System.Drawing.Size(65, 12);
            this.lblUdpRemoteIp.TabIndex = 0;
            this.lblUdpRemoteIp.Text = "Remote IP";

            // 
            // txtUdpRemoteIp
            // 
            this.txtUdpRemoteIp.Location = new System.Drawing.Point(80, 25);
            this.txtUdpRemoteIp.Name = "txtUdpRemoteIp";
            this.txtUdpRemoteIp.Size = new System.Drawing.Size(100, 21);
            this.txtUdpRemoteIp.TabIndex = 1;
            this.txtUdpRemoteIp.Text = "127.0.0.1";

            // 
            // lblUdpRemotePort
            // 
            this.lblUdpRemotePort.AutoSize = true;
            this.lblUdpRemotePort.Location = new System.Drawing.Point(190, 28);
            this.lblUdpRemotePort.Name = "lblUdpRemotePort";
            this.lblUdpRemotePort.Size = new System.Drawing.Size(75, 12);
            this.lblUdpRemotePort.TabIndex = 2;
            this.lblUdpRemotePort.Text = "Remote Port";

            // 
            // numUdpRemotePort
            // 
            this.numUdpRemotePort.Location = new System.Drawing.Point(270, 25);
            this.numUdpRemotePort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numUdpRemotePort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numUdpRemotePort.Name = "numUdpRemotePort";
            this.numUdpRemotePort.Size = new System.Drawing.Size(70, 21);
            this.numUdpRemotePort.TabIndex = 3;
            this.numUdpRemotePort.Value = new decimal(new int[] { 8001, 0, 0, 0 });

            // 
            // lblUdpLocalPort
            // 
            this.lblUdpLocalPort.AutoSize = true;
            this.lblUdpLocalPort.Location = new System.Drawing.Point(350, 28);
            this.lblUdpLocalPort.Name = "lblUdpLocalPort";
            this.lblUdpLocalPort.Size = new System.Drawing.Size(61, 12);
            this.lblUdpLocalPort.TabIndex = 4;
            this.lblUdpLocalPort.Text = "Local Port";

            // 
            // numUdpLocalPort
            // 
            this.numUdpLocalPort.Location = new System.Drawing.Point(420, 25);
            this.numUdpLocalPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numUdpLocalPort.Name = "numUdpLocalPort";
            this.numUdpLocalPort.Size = new System.Drawing.Size(70, 21);
            this.numUdpLocalPort.TabIndex = 5;
            this.numUdpLocalPort.Value = new decimal(new int[] { 8002, 0, 0, 0 });

            // 
            // btnUdpStart
            // 
            this.btnUdpStart.Location = new System.Drawing.Point(510, 23);
            this.btnUdpStart.Name = "btnUdpStart";
            this.btnUdpStart.Size = new System.Drawing.Size(75, 23);
            this.btnUdpStart.TabIndex = 6;
            this.btnUdpStart.Text = "시작";
            this.btnUdpStart.UseVisualStyleBackColor = true;
            this.btnUdpStart.Click += new System.EventHandler(this.btnUdpStart_Click);

            // 
            // btnUdpStop
            // 
            this.btnUdpStop.Location = new System.Drawing.Point(591, 23);
            this.btnUdpStop.Name = "btnUdpStop";
            this.btnUdpStop.Size = new System.Drawing.Size(75, 23);
            this.btnUdpStop.TabIndex = 7;
            this.btnUdpStop.Text = "중지";
            this.btnUdpStop.UseVisualStyleBackColor = true;
            this.btnUdpStop.Click += new System.EventHandler(this.btnUdpStop_Click);

            // 
            // lblUdpStatus
            // 
            this.lblUdpStatus.AutoSize = true;
            this.lblUdpStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblUdpStatus.Location = new System.Drawing.Point(680, 28);
            this.lblUdpStatus.Name = "lblUdpStatus";
            this.lblUdpStatus.Size = new System.Drawing.Size(41, 12);
            this.lblUdpStatus.TabIndex = 8;
            this.lblUdpStatus.Text = "비활성";

            // 
            // txtUdpSendData
            // 
            this.txtUdpSendData.Location = new System.Drawing.Point(80, 60);
            this.txtUdpSendData.Name = "txtUdpSendData";
            this.txtUdpSendData.Size = new System.Drawing.Size(400, 21);
            this.txtUdpSendData.TabIndex = 9;
            this.txtUdpSendData.Text = "48 45 4C 4C 4F";

            // 
            // btnUdpSend
            // 
            this.btnUdpSend.Location = new System.Drawing.Point(490, 58);
            this.btnUdpSend.Name = "btnUdpSend";
            this.btnUdpSend.Size = new System.Drawing.Size(75, 23);
            this.btnUdpSend.TabIndex = 10;
            this.btnUdpSend.Text = "전송 (Hex)";
            this.btnUdpSend.UseVisualStyleBackColor = true;
            this.btnUdpSend.Click += new System.EventHandler(this.btnUdpSend_Click);

            // 
            // tabSerial
            // 
            this.tabSerial.Controls.Add(this.grpSerial);
            this.tabSerial.Location = new System.Drawing.Point(4, 22);
            this.tabSerial.Name = "tabSerial";
            this.tabSerial.Size = new System.Drawing.Size(992, 474);
            this.tabSerial.TabIndex = 2;
            this.tabSerial.Text = "Serial Port";
            this.tabSerial.UseVisualStyleBackColor = true;

            // 
            // grpSerial
            // 
            this.grpSerial.Controls.Add(this.lblSerialPort);
            this.grpSerial.Controls.Add(this.cmbSerialPort);
            this.grpSerial.Controls.Add(this.lblSerialBaudRate);
            this.grpSerial.Controls.Add(this.numSerialBaudRate);
            this.grpSerial.Controls.Add(this.btnSerialRefresh);
            this.grpSerial.Controls.Add(this.btnSerialConnect);
            this.grpSerial.Controls.Add(this.btnSerialDisconnect);
            this.grpSerial.Controls.Add(this.lblSerialStatus);
            this.grpSerial.Controls.Add(this.txtSerialSendData);
            this.grpSerial.Controls.Add(this.btnSerialSend);
            this.grpSerial.Location = new System.Drawing.Point(6, 6);
            this.grpSerial.Name = "grpSerial";
            this.grpSerial.Size = new System.Drawing.Size(980, 120);
            this.grpSerial.TabIndex = 0;
            this.grpSerial.TabStop = false;
            this.grpSerial.Text = "Serial Port";

            // 
            // lblSerialPort
            // 
            this.lblSerialPort.AutoSize = true;
            this.lblSerialPort.Location = new System.Drawing.Point(10, 28);
            this.lblSerialPort.Name = "lblSerialPort";
            this.lblSerialPort.Size = new System.Drawing.Size(27, 12);
            this.lblSerialPort.TabIndex = 0;
            this.lblSerialPort.Text = "Port";

            // 
            // cmbSerialPort
            // 
            this.cmbSerialPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSerialPort.FormattingEnabled = true;
            this.cmbSerialPort.Location = new System.Drawing.Point(50, 25);
            this.cmbSerialPort.Name = "cmbSerialPort";
            this.cmbSerialPort.Size = new System.Drawing.Size(100, 20);
            this.cmbSerialPort.TabIndex = 1;

            // 
            // lblSerialBaudRate
            // 
            this.lblSerialBaudRate.AutoSize = true;
            this.lblSerialBaudRate.Location = new System.Drawing.Point(160, 28);
            this.lblSerialBaudRate.Name = "lblSerialBaudRate";
            this.lblSerialBaudRate.Size = new System.Drawing.Size(57, 12);
            this.lblSerialBaudRate.TabIndex = 2;
            this.lblSerialBaudRate.Text = "BaudRate";

            // 
            // numSerialBaudRate
            // 
            this.numSerialBaudRate.Location = new System.Drawing.Point(220, 25);
            this.numSerialBaudRate.Maximum = new decimal(new int[] { 921600, 0, 0, 0 });
            this.numSerialBaudRate.Minimum = new decimal(new int[] { 300, 0, 0, 0 });
            this.numSerialBaudRate.Name = "numSerialBaudRate";
            this.numSerialBaudRate.Size = new System.Drawing.Size(80, 21);
            this.numSerialBaudRate.TabIndex = 3;
            this.numSerialBaudRate.Value = new decimal(new int[] { 9600, 0, 0, 0 });

            // 
            // btnSerialRefresh
            // 
            this.btnSerialRefresh.Location = new System.Drawing.Point(310, 23);
            this.btnSerialRefresh.Name = "btnSerialRefresh";
            this.btnSerialRefresh.Size = new System.Drawing.Size(60, 23);
            this.btnSerialRefresh.TabIndex = 4;
            this.btnSerialRefresh.Text = "새로고침";
            this.btnSerialRefresh.UseVisualStyleBackColor = true;
            this.btnSerialRefresh.Click += new System.EventHandler(this.btnSerialRefresh_Click);

            // 
            // btnSerialConnect
            // 
            this.btnSerialConnect.Location = new System.Drawing.Point(380, 23);
            this.btnSerialConnect.Name = "btnSerialConnect";
            this.btnSerialConnect.Size = new System.Drawing.Size(75, 23);
            this.btnSerialConnect.TabIndex = 5;
            this.btnSerialConnect.Text = "열기";
            this.btnSerialConnect.UseVisualStyleBackColor = true;
            this.btnSerialConnect.Click += new System.EventHandler(this.btnSerialConnect_Click);

            // 
            // btnSerialDisconnect
            // 
            this.btnSerialDisconnect.Location = new System.Drawing.Point(461, 23);
            this.btnSerialDisconnect.Name = "btnSerialDisconnect";
            this.btnSerialDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnSerialDisconnect.TabIndex = 6;
            this.btnSerialDisconnect.Text = "닫기";
            this.btnSerialDisconnect.UseVisualStyleBackColor = true;
            this.btnSerialDisconnect.Click += new System.EventHandler(this.btnSerialDisconnect_Click);

            // 
            // lblSerialStatus
            // 
            this.lblSerialStatus.AutoSize = true;
            this.lblSerialStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblSerialStatus.Location = new System.Drawing.Point(550, 28);
            this.lblSerialStatus.Name = "lblSerialStatus";
            this.lblSerialStatus.Size = new System.Drawing.Size(29, 12);
            this.lblSerialStatus.TabIndex = 7;
            this.lblSerialStatus.Text = "닫힘";

            // 
            // txtSerialSendData
            // 
            this.txtSerialSendData.Location = new System.Drawing.Point(50, 60);
            this.txtSerialSendData.Name = "txtSerialSendData";
            this.txtSerialSendData.Size = new System.Drawing.Size(400, 21);
            this.txtSerialSendData.TabIndex = 8;
            this.txtSerialSendData.Text = "48 45 4C 4C 4F";

            // 
            // btnSerialSend
            // 
            this.btnSerialSend.Location = new System.Drawing.Point(460, 58);
            this.btnSerialSend.Name = "btnSerialSend";
            this.btnSerialSend.Size = new System.Drawing.Size(75, 23);
            this.btnSerialSend.TabIndex = 9;
            this.btnSerialSend.Text = "전송 (Hex)";
            this.btnSerialSend.UseVisualStyleBackColor = true;
            this.btnSerialSend.Click += new System.EventHandler(this.btnSerialSend_Click);

            // 
            // tabPlc
            // 
            this.tabPlc.Controls.Add(this.grpPlcConnection);
            this.tabPlc.Controls.Add(this.grpPlcReadWrite);
            this.tabPlc.Location = new System.Drawing.Point(4, 22);
            this.tabPlc.Name = "tabPlc";
            this.tabPlc.Size = new System.Drawing.Size(992, 474);
            this.tabPlc.TabIndex = 3;
            this.tabPlc.Text = "PLC";
            this.tabPlc.UseVisualStyleBackColor = true;

            // 
            // grpPlcConnection
            // 
            this.grpPlcConnection.Controls.Add(this.lblPlcType);
            this.grpPlcConnection.Controls.Add(this.cmbPlcType);
            this.grpPlcConnection.Controls.Add(this.lblPlcIp);
            this.grpPlcConnection.Controls.Add(this.txtPlcIp);
            this.grpPlcConnection.Controls.Add(this.lblPlcPort);
            this.grpPlcConnection.Controls.Add(this.numPlcPort);
            this.grpPlcConnection.Controls.Add(this.btnPlcConnect);
            this.grpPlcConnection.Controls.Add(this.btnPlcDisconnect);
            this.grpPlcConnection.Controls.Add(this.lblPlcStatus);
            this.grpPlcConnection.Controls.Add(this.pnlS7Options);
            this.grpPlcConnection.Controls.Add(this.pnlModbusOptions);
            this.grpPlcConnection.Location = new System.Drawing.Point(6, 6);
            this.grpPlcConnection.Name = "grpPlcConnection";
            this.grpPlcConnection.Size = new System.Drawing.Size(980, 130);
            this.grpPlcConnection.TabIndex = 0;
            this.grpPlcConnection.TabStop = false;
            this.grpPlcConnection.Text = "PLC 연결";

            // 
            // lblPlcType
            // 
            this.lblPlcType.AutoSize = true;
            this.lblPlcType.Location = new System.Drawing.Point(10, 28);
            this.lblPlcType.Name = "lblPlcType";
            this.lblPlcType.Size = new System.Drawing.Size(53, 12);
            this.lblPlcType.TabIndex = 0;
            this.lblPlcType.Text = "PLC 타입";

            // 
            // cmbPlcType
            // 
            this.cmbPlcType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlcType.FormattingEnabled = true;
            this.cmbPlcType.Location = new System.Drawing.Point(70, 25);
            this.cmbPlcType.Name = "cmbPlcType";
            this.cmbPlcType.Size = new System.Drawing.Size(130, 20);
            this.cmbPlcType.TabIndex = 1;
            this.cmbPlcType.SelectedIndexChanged += new System.EventHandler(this.cmbPlcType_SelectedIndexChanged);

            // 
            // lblPlcIp
            // 
            this.lblPlcIp.AutoSize = true;
            this.lblPlcIp.Location = new System.Drawing.Point(210, 28);
            this.lblPlcIp.Name = "lblPlcIp";
            this.lblPlcIp.Size = new System.Drawing.Size(17, 12);
            this.lblPlcIp.TabIndex = 2;
            this.lblPlcIp.Text = "IP";

            // 
            // txtPlcIp
            // 
            this.txtPlcIp.Location = new System.Drawing.Point(240, 25);
            this.txtPlcIp.Name = "txtPlcIp";
            this.txtPlcIp.Size = new System.Drawing.Size(120, 21);
            this.txtPlcIp.TabIndex = 3;
            this.txtPlcIp.Text = "192.168.0.10";

            // 
            // lblPlcPort
            // 
            this.lblPlcPort.AutoSize = true;
            this.lblPlcPort.Location = new System.Drawing.Point(370, 28);
            this.lblPlcPort.Name = "lblPlcPort";
            this.lblPlcPort.Size = new System.Drawing.Size(27, 12);
            this.lblPlcPort.TabIndex = 4;
            this.lblPlcPort.Text = "Port";

            // 
            // numPlcPort
            // 
            this.numPlcPort.Location = new System.Drawing.Point(400, 25);
            this.numPlcPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numPlcPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPlcPort.Name = "numPlcPort";
            this.numPlcPort.Size = new System.Drawing.Size(70, 21);
            this.numPlcPort.TabIndex = 5;
            this.numPlcPort.Value = new decimal(new int[] { 5001, 0, 0, 0 });

            // 
            // btnPlcConnect
            // 
            this.btnPlcConnect.Location = new System.Drawing.Point(490, 23);
            this.btnPlcConnect.Name = "btnPlcConnect";
            this.btnPlcConnect.Size = new System.Drawing.Size(75, 23);
            this.btnPlcConnect.TabIndex = 6;
            this.btnPlcConnect.Text = "연결";
            this.btnPlcConnect.UseVisualStyleBackColor = true;
            this.btnPlcConnect.Click += new System.EventHandler(this.btnPlcConnect_Click);

            // 
            // btnPlcDisconnect
            // 
            this.btnPlcDisconnect.Location = new System.Drawing.Point(571, 23);
            this.btnPlcDisconnect.Name = "btnPlcDisconnect";
            this.btnPlcDisconnect.Size = new System.Drawing.Size(75, 23);
            this.btnPlcDisconnect.TabIndex = 7;
            this.btnPlcDisconnect.Text = "연결 해제";
            this.btnPlcDisconnect.UseVisualStyleBackColor = true;
            this.btnPlcDisconnect.Click += new System.EventHandler(this.btnPlcDisconnect_Click);

            // 
            // lblPlcStatus
            // 
            this.lblPlcStatus.AutoSize = true;
            this.lblPlcStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblPlcStatus.Location = new System.Drawing.Point(660, 28);
            this.lblPlcStatus.Name = "lblPlcStatus";
            this.lblPlcStatus.Size = new System.Drawing.Size(60, 12);
            this.lblPlcStatus.TabIndex = 8;
            this.lblPlcStatus.Text = "연결 안됨";

            // 
            // pnlS7Options
            // 
            this.pnlS7Options.Controls.Add(this.lblS7CpuType);
            this.pnlS7Options.Controls.Add(this.cmbS7CpuType);
            this.pnlS7Options.Controls.Add(this.lblS7Rack);
            this.pnlS7Options.Controls.Add(this.numS7Rack);
            this.pnlS7Options.Controls.Add(this.lblS7Slot);
            this.pnlS7Options.Controls.Add(this.numS7Slot);
            this.pnlS7Options.Location = new System.Drawing.Point(10, 55);
            this.pnlS7Options.Name = "pnlS7Options";
            this.pnlS7Options.Size = new System.Drawing.Size(500, 35);
            this.pnlS7Options.TabIndex = 9;
            this.pnlS7Options.Visible = false;

            // 
            // lblS7CpuType
            // 
            this.lblS7CpuType.AutoSize = true;
            this.lblS7CpuType.Location = new System.Drawing.Point(0, 10);
            this.lblS7CpuType.Name = "lblS7CpuType";
            this.lblS7CpuType.Size = new System.Drawing.Size(56, 12);
            this.lblS7CpuType.TabIndex = 0;
            this.lblS7CpuType.Text = "CPU 타입";

            // 
            // cmbS7CpuType
            // 
            this.cmbS7CpuType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbS7CpuType.FormattingEnabled = true;
            this.cmbS7CpuType.Location = new System.Drawing.Point(60, 7);
            this.cmbS7CpuType.Name = "cmbS7CpuType";
            this.cmbS7CpuType.Size = new System.Drawing.Size(100, 20);
            this.cmbS7CpuType.TabIndex = 1;

            // 
            // lblS7Rack
            // 
            this.lblS7Rack.AutoSize = true;
            this.lblS7Rack.Location = new System.Drawing.Point(170, 10);
            this.lblS7Rack.Name = "lblS7Rack";
            this.lblS7Rack.Size = new System.Drawing.Size(32, 12);
            this.lblS7Rack.TabIndex = 2;
            this.lblS7Rack.Text = "Rack";

            // 
            // numS7Rack
            // 
            this.numS7Rack.Location = new System.Drawing.Point(210, 7);
            this.numS7Rack.Maximum = new decimal(new int[] { 7, 0, 0, 0 });
            this.numS7Rack.Name = "numS7Rack";
            this.numS7Rack.Size = new System.Drawing.Size(50, 21);
            this.numS7Rack.TabIndex = 3;

            // 
            // lblS7Slot
            // 
            this.lblS7Slot.AutoSize = true;
            this.lblS7Slot.Location = new System.Drawing.Point(270, 10);
            this.lblS7Slot.Name = "lblS7Slot";
            this.lblS7Slot.Size = new System.Drawing.Size(24, 12);
            this.lblS7Slot.TabIndex = 4;
            this.lblS7Slot.Text = "Slot";

            // 
            // numS7Slot
            // 
            this.numS7Slot.Location = new System.Drawing.Point(300, 7);
            this.numS7Slot.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            this.numS7Slot.Name = "numS7Slot";
            this.numS7Slot.Size = new System.Drawing.Size(50, 21);
            this.numS7Slot.TabIndex = 5;
            this.numS7Slot.Value = new decimal(new int[] { 1, 0, 0, 0 });

            // 
            // pnlModbusOptions
            // 
            this.pnlModbusOptions.Controls.Add(this.lblModbusSlave);
            this.pnlModbusOptions.Controls.Add(this.numModbusSlave);
            this.pnlModbusOptions.Controls.Add(this.cmbModbusPort);
            this.pnlModbusOptions.Controls.Add(this.numModbusBaudRate);
            this.pnlModbusOptions.Location = new System.Drawing.Point(10, 95);
            this.pnlModbusOptions.Name = "pnlModbusOptions";
            this.pnlModbusOptions.Size = new System.Drawing.Size(500, 30);
            this.pnlModbusOptions.TabIndex = 10;
            this.pnlModbusOptions.Visible = false;

            // 
            // lblModbusSlave
            // 
            this.lblModbusSlave.AutoSize = true;
            this.lblModbusSlave.Location = new System.Drawing.Point(0, 8);
            this.lblModbusSlave.Name = "lblModbusSlave";
            this.lblModbusSlave.Size = new System.Drawing.Size(62, 12);
            this.lblModbusSlave.TabIndex = 0;
            this.lblModbusSlave.Text = "Slave 주소";

            // 
            // numModbusSlave
            // 
            this.numModbusSlave.Location = new System.Drawing.Point(70, 5);
            this.numModbusSlave.Maximum = new decimal(new int[] { 247, 0, 0, 0 });
            this.numModbusSlave.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numModbusSlave.Name = "numModbusSlave";
            this.numModbusSlave.Size = new System.Drawing.Size(50, 21);
            this.numModbusSlave.TabIndex = 1;
            this.numModbusSlave.Value = new decimal(new int[] { 1, 0, 0, 0 });

            // 
            // cmbModbusPort
            // 
            this.cmbModbusPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbModbusPort.FormattingEnabled = true;
            this.cmbModbusPort.Location = new System.Drawing.Point(140, 5);
            this.cmbModbusPort.Name = "cmbModbusPort";
            this.cmbModbusPort.Size = new System.Drawing.Size(80, 20);
            this.cmbModbusPort.TabIndex = 2;

            // 
            // numModbusBaudRate
            // 
            this.numModbusBaudRate.Location = new System.Drawing.Point(230, 5);
            this.numModbusBaudRate.Maximum = new decimal(new int[] { 921600, 0, 0, 0 });
            this.numModbusBaudRate.Minimum = new decimal(new int[] { 300, 0, 0, 0 });
            this.numModbusBaudRate.Name = "numModbusBaudRate";
            this.numModbusBaudRate.Size = new System.Drawing.Size(80, 21);
            this.numModbusBaudRate.TabIndex = 3;
            this.numModbusBaudRate.Value = new decimal(new int[] { 9600, 0, 0, 0 });

            // 
            // grpPlcReadWrite
            // 
            this.grpPlcReadWrite.Controls.Add(this.lblPlcDevice);
            this.grpPlcReadWrite.Controls.Add(this.txtPlcDevice);
            this.grpPlcReadWrite.Controls.Add(this.lblPlcAddress);
            this.grpPlcReadWrite.Controls.Add(this.numPlcAddress);
            this.grpPlcReadWrite.Controls.Add(this.lblPlcCount);
            this.grpPlcReadWrite.Controls.Add(this.numPlcCount);
            this.grpPlcReadWrite.Controls.Add(this.lblPlcWriteValue);
            this.grpPlcReadWrite.Controls.Add(this.numPlcWriteValue);
            this.grpPlcReadWrite.Controls.Add(this.chkPlcBitValue);
            this.grpPlcReadWrite.Controls.Add(this.btnPlcReadWord);
            this.grpPlcReadWrite.Controls.Add(this.btnPlcWriteWord);
            this.grpPlcReadWrite.Controls.Add(this.btnPlcReadBit);
            this.grpPlcReadWrite.Controls.Add(this.btnPlcWriteBit);
            this.grpPlcReadWrite.Controls.Add(this.txtPlcResult);
            this.grpPlcReadWrite.Location = new System.Drawing.Point(6, 142);
            this.grpPlcReadWrite.Name = "grpPlcReadWrite";
            this.grpPlcReadWrite.Size = new System.Drawing.Size(980, 150);
            this.grpPlcReadWrite.TabIndex = 1;
            this.grpPlcReadWrite.TabStop = false;
            this.grpPlcReadWrite.Text = "읽기/쓰기";

            // 
            // lblPlcDevice
            // 
            this.lblPlcDevice.AutoSize = true;
            this.lblPlcDevice.Location = new System.Drawing.Point(10, 28);
            this.lblPlcDevice.Name = "lblPlcDevice";
            this.lblPlcDevice.Size = new System.Drawing.Size(53, 12);
            this.lblPlcDevice.TabIndex = 0;
            this.lblPlcDevice.Text = "디바이스";

            // 
            // txtPlcDevice
            // 
            this.txtPlcDevice.Location = new System.Drawing.Point(70, 25);
            this.txtPlcDevice.Name = "txtPlcDevice";
            this.txtPlcDevice.Size = new System.Drawing.Size(60, 21);
            this.txtPlcDevice.TabIndex = 1;
            this.txtPlcDevice.Text = "D";

            // 
            // lblPlcAddress
            // 
            this.lblPlcAddress.AutoSize = true;
            this.lblPlcAddress.Location = new System.Drawing.Point(140, 28);
            this.lblPlcAddress.Name = "lblPlcAddress";
            this.lblPlcAddress.Size = new System.Drawing.Size(29, 12);
            this.lblPlcAddress.TabIndex = 2;
            this.lblPlcAddress.Text = "주소";

            // 
            // numPlcAddress
            // 
            this.numPlcAddress.Location = new System.Drawing.Point(175, 25);
            this.numPlcAddress.Maximum = new decimal(new int[] { 99999999, 0, 0, 0 });
            this.numPlcAddress.Name = "numPlcAddress";
            this.numPlcAddress.Size = new System.Drawing.Size(80, 21);
            this.numPlcAddress.TabIndex = 3;
            this.numPlcAddress.Value = new decimal(new int[] { 100, 0, 0, 0 });

            // 
            // lblPlcCount
            // 
            this.lblPlcCount.AutoSize = true;
            this.lblPlcCount.Location = new System.Drawing.Point(270, 28);
            this.lblPlcCount.Name = "lblPlcCount";
            this.lblPlcCount.Size = new System.Drawing.Size(29, 12);
            this.lblPlcCount.TabIndex = 4;
            this.lblPlcCount.Text = "개수";

            // 
            // numPlcCount
            // 
            this.numPlcCount.Location = new System.Drawing.Point(305, 25);
            this.numPlcCount.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numPlcCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPlcCount.Name = "numPlcCount";
            this.numPlcCount.Size = new System.Drawing.Size(60, 21);
            this.numPlcCount.TabIndex = 5;
            this.numPlcCount.Value = new decimal(new int[] { 10, 0, 0, 0 });

            // 
            // lblPlcWriteValue
            // 
            this.lblPlcWriteValue.AutoSize = true;
            this.lblPlcWriteValue.Location = new System.Drawing.Point(380, 28);
            this.lblPlcWriteValue.Name = "lblPlcWriteValue";
            this.lblPlcWriteValue.Size = new System.Drawing.Size(53, 12);
            this.lblPlcWriteValue.TabIndex = 6;
            this.lblPlcWriteValue.Text = "쓰기 값";

            // 
            // numPlcWriteValue
            // 
            this.numPlcWriteValue.Location = new System.Drawing.Point(440, 25);
            this.numPlcWriteValue.Maximum = new decimal(new int[] { 32767, 0, 0, 0 });
            this.numPlcWriteValue.Minimum = new decimal(new int[] { 32768, 0, 0, -2147483648 });
            this.numPlcWriteValue.Name = "numPlcWriteValue";
            this.numPlcWriteValue.Size = new System.Drawing.Size(80, 21);
            this.numPlcWriteValue.TabIndex = 7;
            this.numPlcWriteValue.Value = new decimal(new int[] { 100, 0, 0, 0 });

            // 
            // chkPlcBitValue
            // 
            this.chkPlcBitValue.AutoSize = true;
            this.chkPlcBitValue.Location = new System.Drawing.Point(540, 27);
            this.chkPlcBitValue.Name = "chkPlcBitValue";
            this.chkPlcBitValue.Size = new System.Drawing.Size(68, 16);
            this.chkPlcBitValue.TabIndex = 8;
            this.chkPlcBitValue.Text = "비트 ON";
            this.chkPlcBitValue.UseVisualStyleBackColor = true;

            // 
            // btnPlcReadWord
            // 
            this.btnPlcReadWord.Location = new System.Drawing.Point(70, 60);
            this.btnPlcReadWord.Name = "btnPlcReadWord";
            this.btnPlcReadWord.Size = new System.Drawing.Size(100, 23);
            this.btnPlcReadWord.TabIndex = 9;
            this.btnPlcReadWord.Text = "워드 읽기";
            this.btnPlcReadWord.UseVisualStyleBackColor = true;
            this.btnPlcReadWord.Click += new System.EventHandler(this.btnPlcReadWord_Click);

            // 
            // btnPlcWriteWord
            // 
            this.btnPlcWriteWord.Location = new System.Drawing.Point(180, 60);
            this.btnPlcWriteWord.Name = "btnPlcWriteWord";
            this.btnPlcWriteWord.Size = new System.Drawing.Size(100, 23);
            this.btnPlcWriteWord.TabIndex = 10;
            this.btnPlcWriteWord.Text = "워드 쓰기";
            this.btnPlcWriteWord.UseVisualStyleBackColor = true;
            this.btnPlcWriteWord.Click += new System.EventHandler(this.btnPlcWriteWord_Click);

            // 
            // btnPlcReadBit
            // 
            this.btnPlcReadBit.Location = new System.Drawing.Point(300, 60);
            this.btnPlcReadBit.Name = "btnPlcReadBit";
            this.btnPlcReadBit.Size = new System.Drawing.Size(100, 23);
            this.btnPlcReadBit.TabIndex = 11;
            this.btnPlcReadBit.Text = "비트 읽기";
            this.btnPlcReadBit.UseVisualStyleBackColor = true;
            this.btnPlcReadBit.Click += new System.EventHandler(this.btnPlcReadBit_Click);

            // 
            // btnPlcWriteBit
            // 
            this.btnPlcWriteBit.Location = new System.Drawing.Point(410, 60);
            this.btnPlcWriteBit.Name = "btnPlcWriteBit";
            this.btnPlcWriteBit.Size = new System.Drawing.Size(100, 23);
            this.btnPlcWriteBit.TabIndex = 12;
            this.btnPlcWriteBit.Text = "비트 쓰기";
            this.btnPlcWriteBit.UseVisualStyleBackColor = true;
            this.btnPlcWriteBit.Click += new System.EventHandler(this.btnPlcWriteBit_Click);

            // 
            // txtPlcResult
            // 
            this.txtPlcResult.Location = new System.Drawing.Point(70, 95);
            this.txtPlcResult.Multiline = true;
            this.txtPlcResult.Name = "txtPlcResult";
            this.txtPlcResult.ReadOnly = true;
            this.txtPlcResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPlcResult.Size = new System.Drawing.Size(890, 45);
            this.txtPlcResult.TabIndex = 13;

            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLog.ForeColor = System.Drawing.Color.Lime;
            this.txtLog.Location = new System.Drawing.Point(3, 3);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(910, 190);
            this.txtLog.TabIndex = 0;

            // 
            // btnClearLog
            // 
            this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLog.Location = new System.Drawing.Point(919, 3);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(75, 23);
            this.btnClearLog.TabIndex = 1;
            this.btnClearLog.Text = "로그 지우기";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this.splitContainer1);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "IndustrialCommunication Example";

            this.tabControl1.ResumeLayout(false);
            this.tabDatabase.ResumeLayout(false);
            this.tabSocket.ResumeLayout(false);
            this.tabSerial.ResumeLayout(false);
            this.tabPlc.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.grpDbConnection.ResumeLayout(false);
            this.grpDbConnection.PerformLayout();
            this.grpDbQuery.ResumeLayout(false);
            this.grpDbQuery.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDbResult)).EndInit();
            this.grpTcpServer.ResumeLayout(false);
            this.grpTcpServer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTcpServerPort)).EndInit();
            this.grpTcp.ResumeLayout(false);
            this.grpTcp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTcpPort)).EndInit();
            this.grpUdp.ResumeLayout(false);
            this.grpUdp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numUdpRemotePort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUdpLocalPort)).EndInit();
            this.grpSerial.ResumeLayout(false);
            this.grpSerial.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSerialBaudRate)).EndInit();
            this.grpPlcConnection.ResumeLayout(false);
            this.grpPlcConnection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPlcPort)).EndInit();
            this.pnlS7Options.ResumeLayout(false);
            this.pnlS7Options.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numS7Rack)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numS7Slot)).EndInit();
            this.pnlModbusOptions.ResumeLayout(false);
            this.pnlModbusOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numModbusSlave)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numModbusBaudRate)).EndInit();
            this.grpPlcReadWrite.ResumeLayout(false);
            this.grpPlcReadWrite.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPlcAddress)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPlcCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPlcWriteValue)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabDatabase;
        private System.Windows.Forms.TabPage tabSocket;
        private System.Windows.Forms.TabPage tabSerial;
        private System.Windows.Forms.TabPage tabPlc;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnClearLog;

        // Database
        private System.Windows.Forms.GroupBox grpDbConnection;
        private System.Windows.Forms.TextBox txtDbConnectionString;
        private System.Windows.Forms.Button btnDbConnect;
        private System.Windows.Forms.Button btnDbDisconnect;
        private System.Windows.Forms.Label lblDbStatus;
        private System.Windows.Forms.GroupBox grpDbQuery;
        private System.Windows.Forms.TextBox txtDbQuery;
        private System.Windows.Forms.Button btnDbExecuteQuery;
        private System.Windows.Forms.Button btnDbExecuteNonQuery;
        private System.Windows.Forms.Button btnDbExecuteProcedure;
        private System.Windows.Forms.DataGridView dgvDbResult;

        // TCP Server
        private System.Windows.Forms.GroupBox grpTcpServer;
        private System.Windows.Forms.Label lblTcpServerPort;
        private System.Windows.Forms.NumericUpDown numTcpServerPort;
        private System.Windows.Forms.Button btnTcpServerStart;
        private System.Windows.Forms.Button btnTcpServerStop;
        private System.Windows.Forms.Label lblTcpServerStatus;
        private System.Windows.Forms.ListBox lstTcpServerClients;
        private System.Windows.Forms.TextBox txtTcpServerSendData;
        private System.Windows.Forms.Button btnTcpServerSend;
        private System.Windows.Forms.CheckBox chkTcpServerBroadcast;
        private System.Windows.Forms.Button btnTcpServerDisconnectClient;

        // TCP
        private System.Windows.Forms.GroupBox grpTcp;
        private System.Windows.Forms.TextBox txtTcpIp;
        private System.Windows.Forms.NumericUpDown numTcpPort;
        private System.Windows.Forms.Button btnTcpConnect;
        private System.Windows.Forms.Button btnTcpDisconnect;
        private System.Windows.Forms.Label lblTcpStatus;
        private System.Windows.Forms.TextBox txtTcpSendData;
        private System.Windows.Forms.Button btnTcpSend;
        private System.Windows.Forms.Label lblTcpIp;
        private System.Windows.Forms.Label lblTcpPort;

        // UDP
        private System.Windows.Forms.GroupBox grpUdp;
        private System.Windows.Forms.TextBox txtUdpRemoteIp;
        private System.Windows.Forms.NumericUpDown numUdpRemotePort;
        private System.Windows.Forms.NumericUpDown numUdpLocalPort;
        private System.Windows.Forms.Button btnUdpStart;
        private System.Windows.Forms.Button btnUdpStop;
        private System.Windows.Forms.Label lblUdpStatus;
        private System.Windows.Forms.TextBox txtUdpSendData;
        private System.Windows.Forms.Button btnUdpSend;
        private System.Windows.Forms.Label lblUdpRemoteIp;
        private System.Windows.Forms.Label lblUdpRemotePort;
        private System.Windows.Forms.Label lblUdpLocalPort;

        // Serial
        private System.Windows.Forms.GroupBox grpSerial;
        private System.Windows.Forms.ComboBox cmbSerialPort;
        private System.Windows.Forms.NumericUpDown numSerialBaudRate;
        private System.Windows.Forms.Button btnSerialRefresh;
        private System.Windows.Forms.Button btnSerialConnect;
        private System.Windows.Forms.Button btnSerialDisconnect;
        private System.Windows.Forms.Label lblSerialStatus;
        private System.Windows.Forms.TextBox txtSerialSendData;
        private System.Windows.Forms.Button btnSerialSend;
        private System.Windows.Forms.Label lblSerialPort;
        private System.Windows.Forms.Label lblSerialBaudRate;

        // PLC
        private System.Windows.Forms.GroupBox grpPlcConnection;
        private System.Windows.Forms.ComboBox cmbPlcType;
        private System.Windows.Forms.TextBox txtPlcIp;
        private System.Windows.Forms.NumericUpDown numPlcPort;
        private System.Windows.Forms.Button btnPlcConnect;
        private System.Windows.Forms.Button btnPlcDisconnect;
        private System.Windows.Forms.Label lblPlcStatus;
        private System.Windows.Forms.Panel pnlS7Options;
        private System.Windows.Forms.ComboBox cmbS7CpuType;
        private System.Windows.Forms.NumericUpDown numS7Rack;
        private System.Windows.Forms.NumericUpDown numS7Slot;
        private System.Windows.Forms.Panel pnlModbusOptions;
        private System.Windows.Forms.NumericUpDown numModbusSlave;
        private System.Windows.Forms.ComboBox cmbModbusPort;
        private System.Windows.Forms.NumericUpDown numModbusBaudRate;
        private System.Windows.Forms.GroupBox grpPlcReadWrite;
        private System.Windows.Forms.TextBox txtPlcDevice;
        private System.Windows.Forms.NumericUpDown numPlcAddress;
        private System.Windows.Forms.NumericUpDown numPlcCount;
        private System.Windows.Forms.NumericUpDown numPlcWriteValue;
        private System.Windows.Forms.CheckBox chkPlcBitValue;
        private System.Windows.Forms.Button btnPlcReadWord;
        private System.Windows.Forms.Button btnPlcWriteWord;
        private System.Windows.Forms.Button btnPlcReadBit;
        private System.Windows.Forms.Button btnPlcWriteBit;
        private System.Windows.Forms.TextBox txtPlcResult;
        private System.Windows.Forms.Label lblPlcType;
        private System.Windows.Forms.Label lblPlcIp;
        private System.Windows.Forms.Label lblPlcPort;
        private System.Windows.Forms.Label lblPlcDevice;
        private System.Windows.Forms.Label lblPlcAddress;
        private System.Windows.Forms.Label lblPlcCount;
        private System.Windows.Forms.Label lblPlcWriteValue;
        private System.Windows.Forms.Label lblS7CpuType;
        private System.Windows.Forms.Label lblS7Rack;
        private System.Windows.Forms.Label lblS7Slot;
        private System.Windows.Forms.Label lblModbusSlave;
    }
}
