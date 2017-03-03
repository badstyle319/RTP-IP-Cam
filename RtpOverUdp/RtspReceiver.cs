using System;
using System.Text;
using System.Net.Sockets;
using MyFramework;
using System.Threading;
using System.Net;
using RTP;

namespace RtspOverUdp
{
    enum RtspCommand
    {
        NONE = 0,
        OPTIONS = 1,
        DESCRIBE = 2,
        SETUP = 4,
        PLAY = 8,
        PAUSE = 16,
        TEARDOWN = 32
    }

    class RtspReceiver : BaseThread
    {
        const string CODEC = "h264";
        const string RESOLUTION = "352x240";
        const string USER = "admin";
        const string PASSWORD = "admin";
        const int BUFFER_SIZE = 4096;

        string mIPAddress = "";
        int mPort = 0;
        int mUdpPort = 0;
        int mChannel = 0;

        TcpClient mTcpClient = null;
        UdpClient mUdpClient = null;
        IPEndPoint udpIPEndPoint = null;
        Thread mUdpThread = null;
        NetworkStream mNetworkStream = null;

        public event DataEventHandler NewData = null;

        ManualResetEvent mUdpStop = null;
        
        int mRequestState = 0;
        string session = null, sprop = null;
        int mSeq = 0;

        public string Parms
        {
            get { return sprop; }
        }

        public RtspReceiver(string ip_address, int channel = 1, int port = 554)
        {
            mIPAddress = ip_address;
            mPort = port;

            mChannel = channel;
            mUdpPort = 50000 + (channel - 1) * 2;

            mUdpStop = new ManualResetEvent(false);
            Init();
        }
        ~RtspReceiver() { mUdpStop.Close(); }

        //initialize connection resource
        void Init()
        {
            mRequestState = 0;
            mSeq = 0;
            session = "";
            sprop = "";

            if (mTcpClient == null)
            {
                mTcpClient = new TcpClient();
                IPEndPoint ip_end_point = new IPEndPoint(IPAddress.Parse(mIPAddress), mPort);
                mTcpClient.Connect(ip_end_point);
                if (mNetworkStream == null)
                    mNetworkStream = mTcpClient.GetStream();
                //start tcp thread
                base.StartThread();
                base.Name = "channel" + mChannel + " thread";
            }

            if (mUdpClient == null)
            {
                mUdpClient = new UdpClient(mUdpPort);
                udpIPEndPoint = new IPEndPoint(IPAddress.Parse(mIPAddress), mUdpPort);

                //start udp thread
                mUdpThread = new Thread(new ThreadStart(UdpReceiver));
                mUdpThread.Name = "udp_port:" + mUdpPort;
                mUdpThread.Start();
                while (!mUdpThread.IsAlive) ;
            }
        }

        //close any connection
        void Free()
        {
            if (mUdpClient != null)
                mUdpClient.Close();

            if (mNetworkStream != null)
                mNetworkStream.Close();
            
            if (mTcpClient != null && mTcpClient.Connected)
                mTcpClient.Close();
        }

        public void GetReady()
        {
            if (mRequestState != 0)
            {
                StopPlay();
                Init();
            }
            SendCommand(RtspCommand.DESCRIBE);
            while (mRequestState != 1)
                Thread.Sleep(100);

            SendCommand(RtspCommand.SETUP);
            while (mRequestState != 2)
                Thread.Sleep(100);
        }

        public void StartPlay()
        {
            GetReady();

            SendCommand(RtspCommand.PLAY);
            while (mRequestState != 3)
                Thread.Sleep(100);
        }

        public void StopPlay()
        {
            if (mRequestState == 3)
            {
                SendCommand(RtspCommand.TEARDOWN);
                while (mRequestState != 4)
                    Thread.Sleep(100);
            }
        }

        void UdpReceiver()
        {
            while (true)
            {
                Byte[] receiveBytes = mUdpClient.Receive(ref udpIPEndPoint);
                if (receiveBytes.Length > 0)
                    NewData(this, new DataEventArgs(ref receiveBytes));
                if (mUdpStop.WaitOne(0, true))
                    break;
            }
        }

        protected override void WorkerThread()
        {
            while (!mStopEvent.WaitOne(0, true))
            {
                if (mNetworkStream.DataAvailable)
                {
                    byte[] readBuffer = new byte[BUFFER_SIZE];
                    int numberOfBytesRead, pos;
                    StringBuilder complegeMessage = new StringBuilder();
                    while (mNetworkStream.DataAvailable)
                    {
                        numberOfBytesRead = mNetworkStream.Read(readBuffer, 0, readBuffer.Length);
                        complegeMessage.Append(Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));
                    }
                    if (complegeMessage.Length <= 0)
                        continue;
                    //DebugFunctions.PrintChars(complegeMessage.ToString());
                    pos = parseData(complegeMessage.ToString());
                    if (pos == complegeMessage.Length)
                    {
                        switch (mRequestState)
                        {
                            case 0://DESCRIBE ok
                                mRequestState = 1;
                                break;
                            case 1://SETUP ok
                                mRequestState = 2;
                                break;
                            case 2://PLAY ok
                                mRequestState = 3;
                                break;
                            case 3://TEARDOWN ok
                                mRequestState = 4;
                                break;
                        }
                    }
                }//end of if (mNetworkStream.DataAvailable)
            }
        }

        public override void StopThread()
        {
            StopPlay();

            //stop udp thread
            if (mUdpThread.IsAlive)
            {
                mUdpStop.Set();
                mUdpThread.Abort();
            }
            
            //stop tcp thread
            if (base.IsAlive)
            {
                mStopEvent.Set();
                base.Join();
            }

            Free();
        }

        void SendCommand(RtspCommand cmd)
        {
            string request = PrepareRequest(cmd);
            byte[] requestBytes = Encoding.ASCII.GetBytes(request);

            mNetworkStream.Write(requestBytes, 0, requestBytes.Length);
            mNetworkStream.Flush();
        }

        string PrepareRequest(RtspCommand cmd)
        {
            string request = "";

            switch (cmd)
            {
                case RtspCommand.OPTIONS: request = "OPTIONS"; break;
                case RtspCommand.DESCRIBE: request = "DESCRIBE"; break;
                case RtspCommand.SETUP: request = "SETUP"; break;
                case RtspCommand.PLAY: request = "PLAY"; break;
                case RtspCommand.PAUSE: request = "PAUSE"; break;
                case RtspCommand.TEARDOWN: request = "TEARDOWN"; break;
            }
            if (request == "")
                return null;
            else
                request += " ";

            if (cmd == RtspCommand.OPTIONS)
                request += "*";
            else
                request += "rtsp://" + mIPAddress + ":" + mPort + "/v" + mChannel.ToString() + "h";

            if (cmd == RtspCommand.SETUP)
                request += "/streamid=0";

            request += " RTSP/1.0\r\n";

            request += "CSeq: " + mSeq.ToString() + "\r\n";
            mSeq++;

            request += "User-Agent: RTSP Agent\r\n";

            switch (cmd)
            {
                case RtspCommand.DESCRIBE:
                    request += "Accept: application/sdp\r\n";
                    break;
                case RtspCommand.SETUP:
                    request += "Transport: RTP/AVP";
                    request += ";unicast;client_port=" + mUdpPort + "-" + (mUdpPort + 1);
                    request += "\r\n";
                    break;
                case RtspCommand.PLAY:
                case RtspCommand.PAUSE:
                case RtspCommand.OPTIONS:
                case RtspCommand.TEARDOWN:
                    if (!string.IsNullOrEmpty(session))
                        request += "Session: " + session + "\r\n";
                    break;
            }
 
            if (!string.IsNullOrEmpty(USER))
            {
                string encode = USER;
                if (!string.IsNullOrEmpty(PASSWORD))
                    encode += ":" + PASSWORD;
                request += "Authorization: Basic "
                    + Convert.ToBase64String(Encoding.Default.GetBytes(encode)) + "\r\n";
            }
            return request += "\r\n";
        }

        int parseData(string strData)
        {
            int ofst = 0, end = 0;
            string line;

            end = strData.IndexOf("\r\n");
            if (!strData.StartsWith("RTSP/1.0 200 OK\r\n"))
                return ofst;
            ofst = end + 2;
            while (ofst < strData.Length)
            {
                end = strData.IndexOf("\r\n", ofst);
                if (end < 0)
                    break;
                line = strData.Substring(ofst, end - ofst);
                if (line.Length == 0)
                {
                    ofst = end + 2;
                    if (mRequestState == 0)
                    {
                        while ((end = strData.IndexOf("\r\n", ofst)) > 0)
                        {
                            line = strData.Substring(ofst, end - ofst);
                            if (line.StartsWith("a=fmtp"))
                            {
                                sprop = getAttribute(line, "a=fmtp", "sprop-parameter-sets");
                                sprop = "sprop-parameter-sets=" + sprop;
                            }
                            ofst = end + 2;
                        }
                        return ofst;
                    }
                    else
                        break;
                }
                if (line.StartsWith("Session"))
                    session = getAttribute(line, "Session");

                ofst = end + 2;
            }
            return ofst;
        }

        #region search methods
        string getAttribute(string line, string name, string sub_name = null)
        {
            if (!line.StartsWith(name + ":", true, null))
                return null;

            string[] attributes = line.Substring(name.Length + 1, line.Length - name.Length - 1).Trim().Split(';');

            for (int i = 0; i < attributes.Length; i++)
            {
                attributes[i] = attributes[i].Trim();
                if (sub_name != null)
                {
                    string sub_attribute = getSubAttribute(attributes[i], sub_name);
                    if (!string.IsNullOrEmpty(sub_attribute))
                        return sub_attribute;
                }
            }
            return attributes[0];
        }
        string getSubAttribute(string line, string name)
        {
            if (!line.StartsWith(name + "=", true, null))
                return null;
            return line.Substring(name.Length + 1, line.Length - name.Length - 1);
        }
        #endregion
    }
}
