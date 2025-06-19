namespace TestWin
{
    partial class Form2
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnSelectFile = new System.Windows.Forms.Button();
            this.btnSelfEncryptFile = new System.Windows.Forms.Button();
            this.btnSelfDecryptFile = new System.Windows.Forms.Button();
            this.btnSelfEncryptData = new System.Windows.Forms.Button();
            this.btnSelfDecryptData = new System.Windows.Forms.Button();
            this.btnGeneratePrivateKey = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(120, 35);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(450, 21);
            this.textBox1.TabIndex = 0;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(120, 65);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(450, 21);
            this.textBox2.TabIndex = 1;
            this.textBox2.Text = "SelfContainedPublicKey2024";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "文件路径/数据:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "公钥(PublicKey):";
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.Location = new System.Drawing.Point(580, 33);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new System.Drawing.Size(75, 23);
            this.btnSelectFile.TabIndex = 4;
            this.btnSelectFile.Text = "选择文件";
            this.btnSelectFile.UseVisualStyleBackColor = true;
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
            // 
            // btnSelfEncryptFile
            // 
            this.btnSelfEncryptFile.Location = new System.Drawing.Point(20, 25);
            this.btnSelfEncryptFile.Name = "btnSelfEncryptFile";
            this.btnSelfEncryptFile.Size = new System.Drawing.Size(120, 35);
            this.btnSelfEncryptFile.TabIndex = 5;
            this.btnSelfEncryptFile.Text = "自包含式\r\n文件加密";
            this.btnSelfEncryptFile.UseVisualStyleBackColor = true;
            this.btnSelfEncryptFile.Click += new System.EventHandler(this.btnSelfEncryptFile_Click);
            // 
            // btnSelfDecryptFile
            // 
            this.btnSelfDecryptFile.Location = new System.Drawing.Point(160, 25);
            this.btnSelfDecryptFile.Name = "btnSelfDecryptFile";
            this.btnSelfDecryptFile.Size = new System.Drawing.Size(120, 35);
            this.btnSelfDecryptFile.TabIndex = 6;
            this.btnSelfDecryptFile.Text = "自包含式\r\n文件解密";
            this.btnSelfDecryptFile.UseVisualStyleBackColor = true;
            this.btnSelfDecryptFile.Click += new System.EventHandler(this.btnSelfDecryptFile_Click);
            // 
            // btnSelfEncryptData
            // 
            this.btnSelfEncryptData.Location = new System.Drawing.Point(20, 25);
            this.btnSelfEncryptData.Name = "btnSelfEncryptData";
            this.btnSelfEncryptData.Size = new System.Drawing.Size(120, 35);
            this.btnSelfEncryptData.TabIndex = 7;
            this.btnSelfEncryptData.Text = "自包含式\r\n数据加密";
            this.btnSelfEncryptData.UseVisualStyleBackColor = true;
            this.btnSelfEncryptData.Click += new System.EventHandler(this.btnSelfEncryptData_Click);
            // 
            // btnSelfDecryptData
            // 
            this.btnSelfDecryptData.Location = new System.Drawing.Point(160, 25);
            this.btnSelfDecryptData.Name = "btnSelfDecryptData";
            this.btnSelfDecryptData.Size = new System.Drawing.Size(120, 35);
            this.btnSelfDecryptData.TabIndex = 8;
            this.btnSelfDecryptData.Text = "自包含式\r\n数据解密";
            this.btnSelfDecryptData.UseVisualStyleBackColor = true;
            this.btnSelfDecryptData.Click += new System.EventHandler(this.btnSelfDecryptData_Click);
            // 
            // btnGeneratePrivateKey
            // 
            this.btnGeneratePrivateKey.Location = new System.Drawing.Point(20, 25);
            this.btnGeneratePrivateKey.Name = "btnGeneratePrivateKey";
            this.btnGeneratePrivateKey.Size = new System.Drawing.Size(120, 35);
            this.btnGeneratePrivateKey.TabIndex = 9;
            this.btnGeneratePrivateKey.Text = "生成2048位\r\n随机私钥";
            this.btnGeneratePrivateKey.UseVisualStyleBackColor = true;
            this.btnGeneratePrivateKey.Click += new System.EventHandler(this.btnGeneratePrivateKey_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.richTextBox1.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(0, 320);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(884, 161);
            this.richTextBox1.TabIndex = 10;
            this.richTextBox1.Text = "";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnSelfEncryptFile);
            this.groupBox1.Controls.Add(this.btnSelfDecryptFile);
            this.groupBox1.Location = new System.Drawing.Point(25, 110);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(300, 80);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "文件加密/解密";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnSelfEncryptData);
            this.groupBox2.Controls.Add(this.btnSelfDecryptData);
            this.groupBox2.Location = new System.Drawing.Point(345, 110);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(300, 80);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "数据加密/解密";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnGeneratePrivateKey);
            this.groupBox3.Location = new System.Drawing.Point(25, 210);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(300, 80);
            this.groupBox3.TabIndex = 13;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "辅助功能";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.ForeColor = System.Drawing.Color.DarkBlue;
            this.label3.Location = new System.Drawing.Point(25, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(283, 22);
            this.label3.TabIndex = 14;
            this.label3.Text = "自包含式加密系统测试 (无需预设私钥)";
            // 
            // btnClearLog
            // 
            this.btnClearLog.Location = new System.Drawing.Point(345, 230);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(75, 23);
            this.btnClearLog.TabIndex = 15;
            this.btnClearLog.Text = "清空日志";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(538, 230);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 16;
            this.button1.Text = "获取设备指纹";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 481);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.btnSelectFile);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Name = "Form2";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "自包含式加密系统测试界面";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.Button btnSelfEncryptFile;
        private System.Windows.Forms.Button btnSelfDecryptFile;
        private System.Windows.Forms.Button btnSelfEncryptData;
        private System.Windows.Forms.Button btnSelfDecryptData;
        private System.Windows.Forms.Button btnGeneratePrivateKey;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.Button button1;
    }
}