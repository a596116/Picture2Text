namespace Picture2Text.Api.DTOs.Responses;

public class ValidationErrorResponse
{
    public int Code { get; set; } = 422;
    public string Message { get; set; } = "驗證失敗";
    public Dictionary<string, string[]>? Errors { get; set; }
}
