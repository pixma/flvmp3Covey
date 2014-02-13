namespace flvmp3Converter
{
    partial class MainWindow
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
            this.groupBoxMain = new System.Windows.Forms.GroupBox();
            this.buttonConvert = new System.Windows.Forms.Button();
            this.buttonPickFile = new System.Windows.Forms.Button();
            this.filePath = new System.Windows.Forms.Label();
            this.filePicker = new System.Windows.Forms.OpenFileDialog();
            this.groupBoxMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxMain
            // 
            this.groupBoxMain.Controls.Add(this.buttonConvert);
            this.groupBoxMain.Controls.Add(this.buttonPickFile);
            this.groupBoxMain.Controls.Add(this.filePath);
            this.groupBoxMain.Location = new System.Drawing.Point(13, 13);
            this.groupBoxMain.Name = "groupBoxMain";
            this.groupBoxMain.Size = new System.Drawing.Size(369, 78);
            this.groupBoxMain.TabIndex = 0;
            this.groupBoxMain.TabStop = false;
            this.groupBoxMain.Text = "Details";
            // 
            // buttonConvert
            // 
            this.buttonConvert.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonConvert.Location = new System.Drawing.Point(73, 42);
            this.buttonConvert.Name = "buttonConvert";
            this.buttonConvert.Size = new System.Drawing.Size(61, 20);
            this.buttonConvert.TabIndex = 2;
            this.buttonConvert.Text = "Convert";
            this.buttonConvert.UseVisualStyleBackColor = true;
            this.buttonConvert.Click += new System.EventHandler(this.buttonConvert_Click);
            // 
            // buttonPickFile
            // 
            this.buttonPickFile.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonPickFile.Location = new System.Drawing.Point(6, 42);
            this.buttonPickFile.Name = "buttonPickFile";
            this.buttonPickFile.Size = new System.Drawing.Size(61, 20);
            this.buttonPickFile.TabIndex = 1;
            this.buttonPickFile.Text = "Browse";
            this.buttonPickFile.UseVisualStyleBackColor = true;
            this.buttonPickFile.Click += new System.EventHandler(this.buttonPickFile_Click);
            // 
            // filePath
            // 
            this.filePath.Location = new System.Drawing.Point(7, 20);
            this.filePath.Name = "filePath";
            this.filePath.Size = new System.Drawing.Size(288, 19);
            this.filePath.TabIndex = 0;
            this.filePath.Text = "Choose you file";
            // 
            // filePicker
            // 
            this.filePicker.FileName = "pickFile";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(394, 98);
            this.Controls.Add(this.groupBoxMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "MainWindow";
            this.Text = "Flv to mp3";
            this.groupBoxMain.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxMain;
        private System.Windows.Forms.OpenFileDialog filePicker;
        private System.Windows.Forms.Button buttonPickFile;
        private System.Windows.Forms.Label filePath;
        private System.Windows.Forms.Button buttonConvert;
    }
}

