using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OptimizelySDK.Notifications
{
    public class NotificationManager<T> where T: class
    {
        private ILogger Logger;
        private readonly Dictionary<int, NotificationHandler<T>> handlers = new Dictionary<int, NotificationHandler<T>>();
        private int Counter;

        public NotificationManager(ILogger logger = null)
        {
            Logger = logger ?? new NoOpLogger();
        }

        public NotificationManager(int counter, ILogger logger = null)
        {
            Logger = logger ?? new NoOpLogger();
            Counter = counter;
        }

        public int addHandler(NotificationHandler<T> newHandler)
        {

            // Prevent registering a duplicate listener.
            lock(handlers) {
                foreach (NotificationHandler<T> handler in handlers.Values)
                {
                    if (handler.Equals(newHandler))
                    {
                        Logger.Log(LogLevel.WARN, "Notification listener was already added");
                        return -1;
                    }
                }
            }

            int notificationId = Interlocked.Increment(ref Counter);
            handlers.Add(notificationId, newHandler);

            return notificationId;
        }

        public void Send(T message)
        {
            lock(handlers) {
                foreach (int handlerKey in handlers.Keys)
                {
                    try
                    {
                        handlers[handlerKey].Handle(message);
                    }
                    catch (Exception e) // TODO: Nomi fix msg
                    {
                        Logger.Log(LogLevel.WARN, "Catching exception sending notification for class: {0}, handler: {1}");
                    }
                }
            }
        }

        public void Clear()
        {
            handlers.Clear();
        }

        public bool Remove(int notificationID)
        {
            return handlers.Remove(notificationID);
        }

        public int Size()
        {
            return handlers.Count;
        }
    }
}
