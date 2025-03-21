using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DistSysAcwServer.Models;
using System.Threading.Tasks;

namespace DistSysAcwServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserDatabaseAccess _userDataAccess;

        public UserController(UserDatabaseAccess userDataAccess)
        {
            _userDataAccess = userDataAccess;
        }

        [HttpGet("new")]
        public async Task<IActionResult> CheckUserExistence([FromQuery] string username)
        {
            bool exists = await _userDataAccess.UserExistenceWithName(username);
            return Ok(exists ? "True - User Does Exist! Did you mean to do a POST to create a new user?" : 
                "False - User Does Not Exist! Did you mean to do a POST to create a new user?");
        }

        [HttpPost("new")]
        // since IIS Express use localhost:53415
        // curl -X POST "http://localhost:53415/api/user/new" -H "Content-Type: application/json" -d "\"UserOne\""
        public async Task<IActionResult> RegisterUser([FromBody] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Oops. Make sure your body contains a string with your username and your Content-Type is Content-Type:application/json");
            }

            bool exists = await _userDataAccess.UserExistenceWithName(username);
            if (exists)
            {
                return StatusCode(403, "Oops. This username is already in use. Please try again with a new username.");
            }

            bool isFirstUser = !await _userDataAccess.AnyUsersExistAsync();
            string role = isFirstUser ? "Admin" : "User";

            string apiKey = await _userDataAccess.CreateNewUser(username, role);
            return Ok(apiKey);
        }

    }
}
