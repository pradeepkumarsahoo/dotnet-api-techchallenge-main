using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ProductCatalog.Api.Controllers
{
    [Route("user")]
    public class UserController : ControllerBase
    {
        [HttpGet]
        public IActionResult FindUser()
        {
            return Ok(new UserResponseModel
            {
                Name = "John Smith",
                Token = "25a4f06f-8fd5-49b3-a711-c013c156f8c8"
            });
        }
    }

    public class UserResponseModel
    {
        [JsonProperty] public string Token { get; set; }

        [JsonProperty] public string Name { get; set; }
    }
}

//Protect the endpoint with authentication
//Hard-coded secret and identity. The same token literal also exists in ProductHttpClient, ShopperHistoryHttpClient and WooliesXProxy.
// 	The credential is committed to source control, cannot be rotated without a code change
// , cannot differ per environment, and four copies can drift apart.

//An anonymous endpoint hands out the upstream token .Security issue 
// Anyone who can reach the API can read the credential

//Missing [ApiController] and untyped IActionResult.