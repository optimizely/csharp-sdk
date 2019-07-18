using System;
#if NETSTANDARD
using System.Threading.Tasks;
#else
using System.Threading;
#endif


namespace OptimizelySDK.Event
{
    public class ExecutorService
    {

#if NETSTANDARD
        private System.Threading.Tasks.Task Executor;
#else
        private System.Threading.Thread Executor;
#endif

        public ExecutorService(Action action)
        {
#if NETSTANDARD
            Executor = new Task(action);
#else
            Executor = new Thread(() => { action(); });
#endif

        }

        public void Start()
        {
            Executor.Start();
        }

        public void Delay(int millisecondsTimeout = 50)
        {
#if NETSTANDARD
            Task.Delay(millisecondsTimeout).Wait();
#else
            Thread.Sleep(millisecondsTimeout);
#endif
        }

        public bool Stop(int millisecondsTimeout = 50)
        {
#if NETSTANDARD
            return Executor?.Wait(millisecondsTimeout) ?? false;
#else
            return Executor?.Join(millisecondsTimeout) ?? false;
#endif
        }
    }
}
