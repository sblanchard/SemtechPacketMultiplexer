namespace PacketInspector
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lstPacketFiles = new System.Windows.Forms.ListBox();
            this.lblMessageType = new System.Windows.Forms.Label();
            this.txtMac = new System.Windows.Forms.TextBox();
            this.txtDecoded = new System.Windows.Forms.TextBox();
            this.btnDeserialize = new System.Windows.Forms.Button();
            this.txtJson = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lstPacketFiles
            // 
            this.lstPacketFiles.FormattingEnabled = true;
            this.lstPacketFiles.ItemHeight = 15;
            this.lstPacketFiles.Location = new System.Drawing.Point(4, 19);
            this.lstPacketFiles.Name = "lstPacketFiles";
            this.lstPacketFiles.Size = new System.Drawing.Size(199, 424);
            this.lstPacketFiles.TabIndex = 0;
            this.lstPacketFiles.SelectedIndexChanged += new System.EventHandler(this.lstPacketFiles_SelectedIndexChanged);
            // 
            // lblMessageType
            // 
            this.lblMessageType.AutoSize = true;
            this.lblMessageType.Location = new System.Drawing.Point(218, 22);
            this.lblMessageType.Name = "lblMessageType";
            this.lblMessageType.Size = new System.Drawing.Size(62, 15);
            this.lblMessageType.TabIndex = 1;
            this.lblMessageType.Text = "[MsgType]";
            // 
            // txtMac
            // 
            this.txtMac.Location = new System.Drawing.Point(220, 54);
            this.txtMac.Name = "txtMac";
            this.txtMac.Size = new System.Drawing.Size(200, 23);
            this.txtMac.TabIndex = 2;
            // 
            // txtDecoded
            // 
            this.txtDecoded.Location = new System.Drawing.Point(220, 83);
            this.txtDecoded.Multiline = true;
            this.txtDecoded.Name = "txtDecoded";
            this.txtDecoded.Size = new System.Drawing.Size(521, 114);
            this.txtDecoded.TabIndex = 3;
            // 
            // btnDeserialize
            // 
            this.btnDeserialize.Location = new System.Drawing.Point(218, 412);
            this.btnDeserialize.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnDeserialize.Name = "btnDeserialize";
            this.btnDeserialize.Size = new System.Drawing.Size(82, 27);
            this.btnDeserialize.TabIndex = 4;
            this.btnDeserialize.Text = "Deserialize";
            this.btnDeserialize.UseVisualStyleBackColor = true;
            this.btnDeserialize.Click += new System.EventHandler(this.btnDeserialize_Click);
            // 
            // txtJson
            // 
            this.txtJson.Location = new System.Drawing.Point(218, 203);
            this.txtJson.Multiline = true;
            this.txtJson.Name = "txtJson";
            this.txtJson.Size = new System.Drawing.Size(521, 114);
            this.txtJson.TabIndex = 5;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txtJson);
            this.Controls.Add(this.btnDeserialize);
            this.Controls.Add(this.txtDecoded);
            this.Controls.Add(this.txtMac);
            this.Controls.Add(this.lblMessageType);
            this.Controls.Add(this.lstPacketFiles);
            this.Name = "frmMain";
            this.Text = "PacketInspector";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ListBox lstPacketFiles;
        private Label lblMessageType;
        private TextBox txtMac;
        private TextBox txtDecoded;
        private Button btnDeserialize;
        private TextBox textBox1;
        private TextBox txtJson;
    }
}