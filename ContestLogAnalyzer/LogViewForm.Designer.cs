namespace W6OP.ContestLogAnalyzer
{
    partial class LogViewForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.TextBoxBand = new System.Windows.Forms.TextBox();
            this.TextBoxPower = new System.Windows.Forms.TextBox();
            this.TextBoxTransmitter = new System.Windows.Forms.TextBox();
            this.TextBoxStation = new System.Windows.Forms.TextBox();
            this.TextBoxAssisted = new System.Windows.Forms.TextBox();
            this.TextBoxOperator = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.LabelOperatorCall = new System.Windows.Forms.Label();
            this.DataGridViewQSO = new System.Windows.Forms.DataGridView();
            this.ColumnStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnBand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnOperatorCall = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnSent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnOperatorName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnContactCall = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnReceived = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnContactName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnRejectReason = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DataGridViewQSO)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.TextBoxBand);
            this.groupBox1.Controls.Add(this.TextBoxPower);
            this.groupBox1.Controls.Add(this.TextBoxTransmitter);
            this.groupBox1.Controls.Add(this.TextBoxStation);
            this.groupBox1.Controls.Add(this.TextBoxAssisted);
            this.groupBox1.Controls.Add(this.TextBoxOperator);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.LabelOperatorCall);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(953, 131);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Log Header";
            // 
            // TextBoxBand
            // 
            this.TextBoxBand.Location = new System.Drawing.Point(527, 42);
            this.TextBoxBand.Name = "TextBoxBand";
            this.TextBoxBand.Size = new System.Drawing.Size(100, 23);
            this.TextBoxBand.TabIndex = 12;
            // 
            // TextBoxPower
            // 
            this.TextBoxPower.Location = new System.Drawing.Point(527, 71);
            this.TextBoxPower.Name = "TextBoxPower";
            this.TextBoxPower.Size = new System.Drawing.Size(100, 23);
            this.TextBoxPower.TabIndex = 11;
            // 
            // TextBoxTransmitter
            // 
            this.TextBoxTransmitter.Location = new System.Drawing.Point(527, 100);
            this.TextBoxTransmitter.Name = "TextBoxTransmitter";
            this.TextBoxTransmitter.Size = new System.Drawing.Size(100, 23);
            this.TextBoxTransmitter.TabIndex = 10;
            // 
            // TextBoxStation
            // 
            this.TextBoxStation.Location = new System.Drawing.Point(163, 100);
            this.TextBoxStation.Name = "TextBoxStation";
            this.TextBoxStation.Size = new System.Drawing.Size(100, 23);
            this.TextBoxStation.TabIndex = 9;
            // 
            // TextBoxAssisted
            // 
            this.TextBoxAssisted.Location = new System.Drawing.Point(163, 71);
            this.TextBoxAssisted.Name = "TextBoxAssisted";
            this.TextBoxAssisted.Size = new System.Drawing.Size(100, 23);
            this.TextBoxAssisted.TabIndex = 8;
            // 
            // TextBoxOperator
            // 
            this.TextBoxOperator.Location = new System.Drawing.Point(163, 42);
            this.TextBoxOperator.Name = "TextBoxOperator";
            this.TextBoxOperator.Size = new System.Drawing.Size(100, 23);
            this.TextBoxOperator.TabIndex = 7;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(389, 43);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(95, 17);
            this.label8.TabIndex = 6;
            this.label8.Text = "Category-Band";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(389, 72);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 17);
            this.label7.TabIndex = 5;
            this.label7.Text = "Category.Power";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(389, 101);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(132, 17);
            this.label6.TabIndex = 4;
            this.label6.Text = "Category-Transmitter";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(33, 101);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(106, 17);
            this.label5.TabIndex = 3;
            this.label5.Text = "Category-Station";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(33, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(114, 17);
            this.label4.TabIndex = 2;
            this.label4.Text = "Category-Assisted";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(33, 43);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(128, 17);
            this.label3.TabIndex = 1;
            this.label3.Text = "Category - Operator";
            // 
            // LabelOperatorCall
            // 
            this.LabelOperatorCall.AutoSize = true;
            this.LabelOperatorCall.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelOperatorCall.ForeColor = System.Drawing.Color.Blue;
            this.LabelOperatorCall.Location = new System.Drawing.Point(310, 9);
            this.LabelOperatorCall.Name = "LabelOperatorCall";
            this.LabelOperatorCall.Size = new System.Drawing.Size(45, 25);
            this.LabelOperatorCall.TabIndex = 0;
            this.LabelOperatorCall.Text = "Call";
            // 
            // DataGridViewQSO
            // 
            this.DataGridViewQSO.AllowUserToAddRows = false;
            this.DataGridViewQSO.AllowUserToDeleteRows = false;
            this.DataGridViewQSO.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.DataGridViewQSO.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataGridViewQSO.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnStatus,
            this.ColumnBand,
            this.ColumnMode,
            this.ColumnDate,
            this.ColumnTime,
            this.ColumnOperatorCall,
            this.ColumnSent,
            this.ColumnOperatorName,
            this.ColumnContactCall,
            this.ColumnReceived,
            this.ColumnContactName,
            this.ColumnRejectReason});
            this.DataGridViewQSO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DataGridViewQSO.Location = new System.Drawing.Point(0, 131);
            this.DataGridViewQSO.Name = "DataGridViewQSO";
            this.DataGridViewQSO.Size = new System.Drawing.Size(953, 692);
            this.DataGridViewQSO.TabIndex = 1;
            // 
            // ColumnStatus
            // 
            this.ColumnStatus.DataPropertyName = "Status";
            this.ColumnStatus.HeaderText = "Status";
            this.ColumnStatus.Name = "ColumnStatus";
            this.ColumnStatus.Width = 64;
            // 
            // ColumnBand
            // 
            this.ColumnBand.DataPropertyName = "Band";
            this.ColumnBand.HeaderText = "Band";
            this.ColumnBand.Name = "ColumnBand";
            this.ColumnBand.Width = 59;
            // 
            // ColumnMode
            // 
            this.ColumnMode.DataPropertyName = "Mode";
            this.ColumnMode.HeaderText = "Mode";
            this.ColumnMode.Name = "ColumnMode";
            this.ColumnMode.Width = 63;
            // 
            // ColumnDate
            // 
            this.ColumnDate.DataPropertyName = "QsoDate";
            this.ColumnDate.HeaderText = "Date";
            this.ColumnDate.Name = "ColumnDate";
            this.ColumnDate.Width = 56;
            // 
            // ColumnTime
            // 
            this.ColumnTime.DataPropertyName = "QsoTime";
            this.ColumnTime.HeaderText = "Time";
            this.ColumnTime.Name = "ColumnTime";
            this.ColumnTime.Width = 59;
            // 
            // ColumnOperatorCall
            // 
            this.ColumnOperatorCall.DataPropertyName = "OperatorCall";
            this.ColumnOperatorCall.HeaderText = "Op Call";
            this.ColumnOperatorCall.Name = "ColumnOperatorCall";
            this.ColumnOperatorCall.Width = 71;
            // 
            // ColumnSent
            // 
            this.ColumnSent.DataPropertyName = "SentSerialNumber";
            this.ColumnSent.HeaderText = "Sent";
            this.ColumnSent.Name = "ColumnSent";
            this.ColumnSent.Width = 55;
            // 
            // ColumnOperatorName
            // 
            this.ColumnOperatorName.DataPropertyName = "OperatorName";
            this.ColumnOperatorName.HeaderText = "Operator";
            this.ColumnOperatorName.Name = "ColumnOperatorName";
            this.ColumnOperatorName.Width = 79;
            // 
            // ColumnContactCall
            // 
            this.ColumnContactCall.DataPropertyName = "ContactCall";
            this.ColumnContactCall.HeaderText = "QSO Call";
            this.ColumnContactCall.Name = "ColumnContactCall";
            this.ColumnContactCall.Width = 79;
            // 
            // ColumnReceived
            // 
            this.ColumnReceived.DataPropertyName = "ReceivedSerialNumber";
            this.ColumnReceived.HeaderText = "Received";
            this.ColumnReceived.Name = "ColumnReceived";
            this.ColumnReceived.Width = 79;
            // 
            // ColumnContactName
            // 
            this.ColumnContactName.DataPropertyName = "ContactName";
            this.ColumnContactName.HeaderText = "Contact";
            this.ColumnContactName.Name = "ColumnContactName";
            this.ColumnContactName.Width = 74;
            // 
            // ColumnRejectReason
            // 
            this.ColumnRejectReason.DataPropertyName = "RejectReason";
            this.ColumnRejectReason.HeaderText = "Reject Reason";
            this.ColumnRejectReason.Name = "ColumnRejectReason";
            this.ColumnRejectReason.Width = 105;
            // 
            // LogViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Honeydew;
            this.ClientSize = new System.Drawing.Size(953, 823);
            this.Controls.Add(this.DataGridViewQSO);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "LogViewForm";
            this.Text = "LogViewForm";
            this.Load += new System.EventHandler(this.LogViewForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DataGridViewQSO)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataGridView DataGridViewQSO;
        private System.Windows.Forms.TextBox TextBoxBand;
        private System.Windows.Forms.TextBox TextBoxPower;
        private System.Windows.Forms.TextBox TextBoxTransmitter;
        private System.Windows.Forms.TextBox TextBoxStation;
        private System.Windows.Forms.TextBox TextBoxAssisted;
        private System.Windows.Forms.TextBox TextBoxOperator;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label LabelOperatorCall;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnBand;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnMode;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnOperatorCall;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnSent;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnOperatorName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnContactCall;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnReceived;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnContactName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnRejectReason;
    }
}