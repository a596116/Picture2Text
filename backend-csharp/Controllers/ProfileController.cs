using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Picture2Text.Api.Data;
using Picture2Text.Api.DTOs.Responses;

namespace Picture2Text.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProfileController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 獲取當前使用者資料（從 JWT Token 解析）
    /// </summary>
    /// <returns>使用者資料</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> GetProfile()
    {
        // 從 JWT Claims 中取得使用者 ID（由 ASP.NET Core 認證中間件自動解析）
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new ProfileResponse
            {
                Code = 401,
                Message = "無法從 Token 中取得使用者資訊",
                Data = null
            });
        }

        // 從資料庫取得使用者資料
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound(new ProfileResponse
            {
                Code = 404,
                Message = "使用者不存在",
                Data = null
            });
        }

        return Ok(new ProfileResponse
        {
            Code = 200,
            Message = "操作成功",
            Data = new ProfileData
            {
                Id = user.Id,
                IdNo = user.IdNo,
                Name = user.Name
            }
        });
    }
}
