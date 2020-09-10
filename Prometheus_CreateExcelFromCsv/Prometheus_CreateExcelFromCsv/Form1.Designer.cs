namespace Prometheus_CreateExcelFromCsv
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.createReportButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.csvFilePath = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rdoBarButton = new System.Windows.Forms.RadioButton();
            this.rdoColumnButton = new System.Windows.Forms.RadioButton();
            this.rdoLineButton = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.ReportOptionOnly = new System.Windows.Forms.RadioButton();
            this.ReportBaseOnly = new System.Windows.Forms.RadioButton();
            this.ReportBaseButton = new System.Windows.Forms.RadioButton();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(28, 29);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(174, 34);
            this.button1.TabIndex = 0;
            this.button1.Text = "Open CSV File";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // createReportButton
            // 
            this.createReportButton.Location = new System.Drawing.Point(40, 231);
            this.createReportButton.Name = "createReportButton";
            this.createReportButton.Size = new System.Drawing.Size(174, 43);
            this.createReportButton.TabIndex = 2;
            this.createReportButton.Text = "Create Report";
            this.createReportButton.UseVisualStyleBackColor = true;
            this.createReportButton.Click += new System.EventHandler(this.createReportButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.csvFilePath);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Location = new System.Drawing.Point(12, 21);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(731, 88);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "ファイル選択";
            // 
            // csvFilePath
            // 
            this.csvFilePath.AllowDrop = true;
            this.csvFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.csvFilePath.Font = new System.Drawing.Font("MS UI Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.csvFilePath.Location = new System.Drawing.Point(236, 29);
            this.csvFilePath.Multiline = true;
            this.csvFilePath.Name = "csvFilePath";
            this.csvFilePath.Size = new System.Drawing.Size(480, 34);
            this.csvFilePath.TabIndex = 5;
            this.csvFilePath.DragDrop += new System.Windows.Forms.DragEventHandler(this.csvFilePath_DragDrop);
            this.csvFilePath.DragEnter += new System.Windows.Forms.DragEventHandler(this.csvFilePath_DragEnter);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rdoBarButton);
            this.groupBox2.Controls.Add(this.rdoColumnButton);
            this.groupBox2.Controls.Add(this.rdoLineButton);
            this.groupBox2.Location = new System.Drawing.Point(19, 132);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(361, 82);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "グラフ作成";
            // 
            // rdoBarButton
            // 
            this.rdoBarButton.AutoSize = true;
            this.rdoBarButton.Location = new System.Drawing.Point(247, 29);
            this.rdoBarButton.Name = "rdoBarButton";
            this.rdoBarButton.Size = new System.Drawing.Size(47, 16);
            this.rdoBarButton.TabIndex = 9;
            this.rdoBarButton.TabStop = true;
            this.rdoBarButton.Text = "横棒";
            this.rdoBarButton.UseVisualStyleBackColor = true;
            // 
            // rdoColumnButton
            // 
            this.rdoColumnButton.AutoSize = true;
            this.rdoColumnButton.Location = new System.Drawing.Point(135, 29);
            this.rdoColumnButton.Name = "rdoColumnButton";
            this.rdoColumnButton.Size = new System.Drawing.Size(47, 16);
            this.rdoColumnButton.TabIndex = 8;
            this.rdoColumnButton.TabStop = true;
            this.rdoColumnButton.Text = "縦棒";
            this.rdoColumnButton.UseVisualStyleBackColor = true;
            // 
            // rdoLineButton
            // 
            this.rdoLineButton.AutoSize = true;
            this.rdoLineButton.Checked = true;
            this.rdoLineButton.Location = new System.Drawing.Point(31, 29);
            this.rdoLineButton.Name = "rdoLineButton";
            this.rdoLineButton.Size = new System.Drawing.Size(58, 16);
            this.rdoLineButton.TabIndex = 7;
            this.rdoLineButton.TabStop = true;
            this.rdoLineButton.Text = "折れ線";
            this.rdoLineButton.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.ReportOptionOnly);
            this.groupBox3.Controls.Add(this.ReportBaseOnly);
            this.groupBox3.Controls.Add(this.ReportBaseButton);
            this.groupBox3.Location = new System.Drawing.Point(415, 132);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(292, 127);
            this.groupBox3.TabIndex = 7;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Excel作成オプション";
            this.groupBox3.Visible = false;
            // 
            // ReportOptionOnly
            // 
            this.ReportOptionOnly.AutoSize = true;
            this.ReportOptionOnly.Location = new System.Drawing.Point(18, 87);
            this.ReportOptionOnly.Name = "ReportOptionOnly";
            this.ReportOptionOnly.Size = new System.Drawing.Size(87, 16);
            this.ReportOptionOnly.TabIndex = 2;
            this.ReportOptionOnly.TabStop = true;
            this.ReportOptionOnly.Text = "オプションのみ";
            this.ReportOptionOnly.UseVisualStyleBackColor = true;
            this.ReportOptionOnly.Visible = false;
            // 
            // ReportBaseOnly
            // 
            this.ReportBaseOnly.AutoSize = true;
            this.ReportBaseOnly.Location = new System.Drawing.Point(18, 53);
            this.ReportBaseOnly.Name = "ReportBaseOnly";
            this.ReportBaseOnly.Size = new System.Drawing.Size(68, 16);
            this.ReportBaseOnly.TabIndex = 1;
            this.ReportBaseOnly.TabStop = true;
            this.ReportBaseOnly.Text = "基本のみ";
            this.ReportBaseOnly.UseVisualStyleBackColor = true;
            this.ReportBaseOnly.Visible = false;
            // 
            // ReportBaseButton
            // 
            this.ReportBaseButton.AutoSize = true;
            this.ReportBaseButton.Checked = true;
            this.ReportBaseButton.Location = new System.Drawing.Point(18, 20);
            this.ReportBaseButton.Name = "ReportBaseButton";
            this.ReportBaseButton.Size = new System.Drawing.Size(178, 16);
            this.ReportBaseButton.TabIndex = 0;
            this.ReportBaseButton.TabStop = true;
            this.ReportBaseButton.Text = "基本レポート + オプションレポート";
            this.ReportBaseButton.UseVisualStyleBackColor = true;
            this.ReportBaseButton.Visible = false;
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(755, 287);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.createReportButton);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "ExcelFileFromCSV";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button createReportButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rdoBarButton;
        private System.Windows.Forms.RadioButton rdoColumnButton;
        private System.Windows.Forms.RadioButton rdoLineButton;
        private System.Windows.Forms.TextBox csvFilePath;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton ReportOptionOnly;
        private System.Windows.Forms.RadioButton ReportBaseOnly;
        private System.Windows.Forms.RadioButton ReportBaseButton;
    }
}

