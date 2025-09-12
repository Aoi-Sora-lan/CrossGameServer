#nullable enable
namespace CrossGameLibrary.Message;

public record struct Response
{
    public bool IsSuccess;
    public string? ErrorMessage;

    public static Response Success =>
        new Response()
        {
            IsSuccess = true
        };
}