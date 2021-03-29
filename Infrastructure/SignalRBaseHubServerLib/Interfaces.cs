
namespace SignalRBaseHubServerLib
{
    public interface IStreamingDataProvider<T>
    {
        T Current { get; }
    }

    public interface ISetEvent
    {
        void SetEvent();
        bool IsValid { get; }
    }
}
