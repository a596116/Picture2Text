namespace Picture2Text.Api.DTOs.Responses;

public class ProfileResponse
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = "操作成功";
    public ProfileData? Data { get; set; }
}

public class ProfileData
{
    public int Id { get; set; }
    public string IdNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
