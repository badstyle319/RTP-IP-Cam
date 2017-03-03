using System;
using MyFramework;
using NSVWrapper;

namespace RtspOverUdp
{
    public class RtspAgent : IDisposable
    {
        uint mNSVideo = 0;
        RtspReceiver mReceiver = null;

        public RtspAgent(IntPtr hwnd, string ip, int channel)
        {
            mReceiver = new RtspReceiver(ip, channel);
            mReceiver.NewData += new DataEventHandler(PushData);
            mReceiver.GetReady();

            mNSVideo = NSV.Create();
            NSV.AttachWindow(mNSVideo, hwnd);
            NSV.SetDecoder(mNSVideo, "h264");
            NSV.Start(mNSVideo);
        }
        ~RtspAgent() { }

        public void StartPlay()
        {
            mReceiver.StartPlay();
        }

        public void StopPlay()
        {
            mReceiver.StopPlay();
        }

        public void Dispose()
        {
            if (mReceiver.IsAlive)
                mReceiver.StopThread();

            if (mNSVideo != 0)
            {
                NSV.Stop(mNSVideo);
                NSV.Release(mNSVideo);
            }
        }

        void PushData(object sender, DataEventArgs e)
        {
            NSV.PushMediaPacket(mNSVideo, e.ReceivedData, e.ReceivedData.Length);
        }
    }
}
