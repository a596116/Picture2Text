namespace Picture2Text.Api.DTOs.Responses;

public class LoginResponse
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = "登入成功";
    public LoginData? Data { get; set; }
}

public class LoginData
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
}
