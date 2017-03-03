using System.Threading;

namespace MyFramework
{
    abstract class BaseThread
    {
        Thread mThread = null;

        public ManualResetEvent mStopEvent = null;

        public BaseThread()
        {
            mStopEvent = new ManualResetEvent(false);
        }

        ~BaseThread()
        {
            mStopEvent.Close();
        }

        public string Name
        {
            set { if (mThread != null)mThread.Name = value; }
        }

        public bool IsAlive
        {
            get { return mThread.IsAlive; }
        }

        public void Join()
        {
            mThread.Join();
        }

        public void StartThread()
        {
            if (mThread == null)
            {
                mThread = new Thread(new ThreadStart(WorkerThread));
                mThread.Start();
                while (!mThread.IsAlive) ;
            }
        }

        abstract protected void WorkerThread();
        abstract public void StopThread();
    }
}
