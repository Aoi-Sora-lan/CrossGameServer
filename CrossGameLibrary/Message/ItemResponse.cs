#nullable enable
namespace CrossGameLibrary.Message;

public record struct ItemResponse
{
    public bool IsSuccess;
    public string? Reason;
    public int ItemCount;
    public string? ItemId;
}