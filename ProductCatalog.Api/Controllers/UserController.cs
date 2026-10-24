using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProductCatalog.Api.Contracts;
using ProductCatalog.Api.Infrastructure.WooliesX;
using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("user")]
    public sealed class UserController : ControllerBase
    {
        private readonly UserOptions _user;
        private readonly WooliesXOptions _wooliesX;

        public UserController(IOptions<UserOptions> user, IOptions<WooliesXOptions> wooliesX)
        {
            _user = user.Value;
            _wooliesX = wooliesX.Value;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<UserResponse> FindUser() => Ok(new UserResponse(_user.Name, _wooliesX.Token));
    }

    public sealed class UserOptions
    {
        public const string SectionName = "User";

        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
