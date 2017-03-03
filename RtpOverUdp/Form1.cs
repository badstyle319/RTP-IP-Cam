using System;
using System.Windows.Forms;
using RtspOverUdp;

namespace RtpOverUdp
{
    public partial class Form1 : Form
    {
        const int CAM_NUMBER = 4;
        const string IP = "10.2.0.123";

        RtspAgent[] mRtspAgent = null;
        Panel[] panels = null;

        public Form1()
        {
            InitializeComponent();
            MyInitializeComponent();


            System.Threading.Thread.CurrentThread.Name = "CurrentThread";

            mRtspAgent = new RtspAgent[CAM_NUMBER];

            for (int i = 0; i < CAM_NUMBER; i++)
                mRtspAgent[i] = new RtspAgent(panels[i].Handle, IP, i + 1);
        }

        private void MyInitializeComponent()
        {
            panels = new Panel[CAM_NUMBER];
            SuspendLayout();
            for (int i = 0; i < CAM_NUMBER; i++)
            {
                panels[i] = new Panel();
                panels[i].Name = "panel" + (i + 1);
                panels[i].Size = new System.Drawing.Size(352, 240);
                if (i == 0)
                    panels[i].Location = new System.Drawing.Point(13, 13);
                else if (i == 1)
                    panels[i].Location = new System.Drawing.Point(371, 13);
                else if (i == 2)
                    panels[i].Location = new System.Drawing.Point(13, 259);
                else
                    panels[i].Location = new System.Drawing.Point(371, 259);
                Controls.Add(panels[i]);
            }

            ResumeLayout(false);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < CAM_NUMBER; i++)
                mRtspAgent[i].StartPlay();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < CAM_NUMBER; i++)
                mRtspAgent[i].StopPlay();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < CAM_NUMBER; i++)
                mRtspAgent[i].Dispose();
        }
    }
}
