namespace W6OP.ContestLogAnalyzer
{
    partial class QSOForm
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
            this.components = new System.ComponentModel.Container();
            this.DataGridViewQSOs = new System.Windows.Forms.DataGridView();
            this.qSOBindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.DataGridViewQSOs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.qSOBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // DataGridViewQSOs
            // 
            this.DataGridViewQSOs.AllowUserToDeleteRows = false;
            this.DataGridViewQSOs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataGridViewQSOs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DataGridViewQSOs.Location = new System.Drawing.Point(0, 0);
            this.DataGridViewQSOs.Name = "DataGridViewQSOs";
            this.DataGridViewQSOs.Size = new System.Drawing.Size(1559, 301);
            this.DataGridViewQSOs.TabIndex = 0;
            this.DataGridViewQSOs.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGridViewQSOs_CellContentClick);
            // 
            // qSOBindingSource
            // 
            this.qSOBindingSource.DataSource = typeof(W6OP.ContestLogAnalyzer.QSO);
            // 
            // QSOForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Honeydew;
            this.ClientSize = new System.Drawing.Size(1559, 301);
            this.Controls.Add(this.DataGridViewQSOs);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "QSOForm";
            this.Text = "QSOForm";
            ((System.ComponentModel.ISupportInitialize)(this.DataGridViewQSOs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.qSOBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView DataGridViewQSOs;
        private System.Windows.Forms.BindingSource qSOBindingSource;
    }
}