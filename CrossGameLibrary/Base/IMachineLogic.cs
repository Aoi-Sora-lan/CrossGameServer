using CrossGameLibrary.Message;

namespace CrossGameLibrary.Base;

public interface IMachineLogic
{
    public bool CanTransfer(string itemId, int itemCount);
    int GetMaxNeedCount();
    void PreSend();
    void SendSuccess(ItemResponse contentValue);
    void SendFailure();
    void GenerateItem(ItemPackage package);
    void OnSignal();
}