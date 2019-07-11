
using OptimizelySDK.Event.Entity;

namespace OptimizelySDK.Event
{
    /**
     * EventProcessor interface is used to provide an intermediary processing stage within
     * event production. It's assumed that the EventProcessor dispatches events via a provided
     **/
    public interface EventProcessor
    {
        void Process(object userEvent);
    }
}
