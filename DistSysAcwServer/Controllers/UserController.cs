using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DistSysAcwServer.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Azure.Core;

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
        public async Task<IActionResult> CheckUserExistence()
        {
            var username = HttpContext.Request.Query["username"];
            if (string.IsNullOrWhiteSpace(username))
            {
                return Ok("\"False - User Does Not Exist! Did you mean to do a POST to create a new user?\"");
            }
            bool exists = await _userDataAccess.UserExistenceWithName(username);
            return Ok(exists ? "\"True - User Does Exist! Did you mean to do a POST to create a new user?\"" :
                "\"False - User Does Not Exist! Did you mean to do a POST to create a new user?\"");
        }

        // since IIS Express use localhost:53415 for curl commands

        [HttpPost("new")]
        // curl -X POST "http://localhost:53415/api/user/new" -H "Content-Type: application/json" -d "\"<User#>\""
        public async Task<IActionResult> RegisterUser([FromBody] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("\"Oops. Make sure your body contains a string with your username and your Content-Type is Content-Type:application/json\"");
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

        // [Authorize(Roles = "Admin, User")] causes false to not happen
        // curl -X DELETE "http://localhost:53415/api/user/removeuser?username=<username>" -H "ApiKey: <key>"
        [Authorize(Roles = "Admin, User")]
        [HttpDelete("removeuser")]
        public async Task<IActionResult> RemoveUser([FromQuery] string username)
        {
            if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                return Ok(false);
            }

            var user = await _userDataAccess.GetUserWithAPI(apiKey);
            if (user == null)
            {
                return Ok(false);
            }

            if(user.UserName != username)
            {
                return Ok(false);
            }

            bool deleted = await _userDataAccess.DeleteUserWithAPI(apiKey);
            return Ok(deleted);
        }

        // curl -X POST "http://localhost:53415/api/user/changerole" -H "Content-Type: application/json" -H "ApiKey: c9f7ed4f-20b4-492a-9f8a-3f3c4e6fcd05" -d "{\"username\":\"UserThree\", \"role\":\"User\"}"
        [Authorize(Roles = "Admin")]
        [HttpPost("changerole")]
        public async Task<IActionResult> ChangeRole([FromBody] Dictionary<string, string> body, [FromHeader(Name = "ApiKey")] string apiKey)
        {
            try
            {

                if (body == null || !body.TryGetValue("username", out var username) 
                    || !body.TryGetValue("role", out var role)
                    || string.IsNullOrWhiteSpace(username) 
                    || string.IsNullOrWhiteSpace(role))
                {
                    return BadRequest("NOT DONE: An error occurred");
                }

                var user = await _userDataAccess.GetUserByName(username);
                if (user == null)
                {
                    return BadRequest("NOT DONE: Username does not exist");
                }

                if (role != "Admin" && role != "User")
                {
                    return BadRequest("NOT DONE: Role does not exist");
                }

                var adminUser = await _userDataAccess.GetUserWithAPI(apiKey);
                if (adminUser == null || adminUser.Role != "Admin")
                {
                    return BadRequest("NOT DONE: An error occurred");
                }

                bool updated = await _userDataAccess.UpdateUserRole(username, role);
                if (!updated)
                {
                    return BadRequest("NOT DONE: An error occurred");
                }

                return Ok("DONE");
            }
            catch (Exception e)
            {
                return BadRequest("NOT DONE: An error occurred");
            }
        }

    }
}
