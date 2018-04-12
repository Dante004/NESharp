using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NESharp
{
    public partial class Form1 : Form
    {
        private Console console;

        public Form1()
        {
            InitializeComponent();
        }

        private void openROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                string fileName = openFileDialog1.FileName;
                console = new Console();
                console.LoadCartridge(fileName);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            console.Cycle();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.Text = console.cpu.Debuger();
        }
    }
}