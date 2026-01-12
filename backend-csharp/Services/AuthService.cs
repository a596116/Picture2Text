using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.DTOs.Requests;
using Picture2Text.Api.DTOs.Responses;
using Picture2Text.Api.Models;

namespace Picture2Text.Api.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtService _jwtService;

    public AuthService(ApplicationDbContext context, JwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return new LoginResponse
            {
                Code = 401,
                Message = "使用者 ID 或密碼錯誤",
                Data = null
            };
        }

        var token = _jwtService.GenerateToken(user.Id, user.IdNo);
        var expiresAt = DateTime.UtcNow.AddMinutes(30); // 與 JWT 設定一致

        return new LoginResponse
        {
            Code = 200,
            Message = "登入成功",
            Data = new LoginData
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresAt = expiresAt
            }
        };
    }
}
