using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using toMpThree;

namespace flvmp3Converter
{
    public partial class MainWindow : Form
    {
        static string targetFilePath;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonPickFile_Click(object sender, EventArgs e)
        {
            //
            //launch openDialogue box
            switch (filePicker.ShowDialog())
            {
                case System.Windows.Forms.DialogResult.OK:
                    filePath.Text = filePicker.SafeFileName;
                    targetFilePath = filePicker.FileName;
                    break;
            }
        }

        private void buttonConvert_Click(object sender, EventArgs e)
        {
            try
            {
                FLVFile convrt = new FLVFile(targetFilePath);
                convrt.ExtractStreams(true, false, false, null);

                MessageBox.Show("Convertion Done.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
