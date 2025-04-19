using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace DistSysAcwServer.Controllers
{
    [Authorize(Roles = "Admin, User")]
    [Route("api/[controller]")]
    [ApiController]

    public class ProtectedController : ControllerBase
    {
        private readonly UserDatabaseAccess _userDataAccess;
        private static RSACryptoServiceProvider _rsaProvider;
        // Fixed the declaration of the ContainerName field to resolve all the specified errors.
        private readonly string ContainerName = "ProtectedKeyContainer";

        public ProtectedController(UserDatabaseAccess userDataAccess)
        {
            _userDataAccess = userDataAccess;

            CspParameters cspParameters = new CspParameters
            {
                KeyContainerName = ContainerName,
                Flags = CspProviderFlags.UseMachineKeyStore
            };
            _rsaProvider = new RSACryptoServiceProvider(cspParameters)
            {
                PersistKeyInCsp = true
            };
        }

        [HttpGet("hello")]
        public async Task<IActionResult> Hello()
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

            return Ok("Hello " + user.UserName);
        }

        [HttpGet("sha1")]
        public IActionResult Sha1([FromQuery] string? message)
        {
            if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                return Ok(false);
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("Bad Request");
            }

            byte[] messageBytes = System.Text.Encoding.ASCII.GetBytes(message);
            byte[] sha1ByteMessage;

            using (SHA1 sha1 = SHA1.Create())
            {
                sha1ByteMessage = sha1.ComputeHash(messageBytes);
            }

            string sha1Hex = BitConverter.ToString(sha1ByteMessage).Replace("-", "").ToUpper();

            return Ok(sha1Hex);
        }

        [HttpGet("sha256")]
        public IActionResult Sha256([FromQuery] string? message)
        {
            if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                return Ok(false);
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("Bad Request");
            }

            byte[] messageBytes = System.Text.Encoding.ASCII.GetBytes(message);
            byte[] sha256ByteMessage;

            using (SHA256 sha256 = SHA256.Create())
            {
                sha256ByteMessage = sha256.ComputeHash(messageBytes);
            }

            string sha256Hex = BitConverter.ToString(sha256ByteMessage).Replace("-", "").ToUpper();

            return Ok(sha256Hex);
        }


        [HttpGet("getpublickey")]
        public IActionResult GetPublicKey()
        {
            if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                return Unauthorized();
            }

            return Ok(_rsaProvider.ToXmlString(false));

        }

        [HttpGet("sign")]
        public IActionResult Sign([FromQuery] string? message)
        {
            if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("Bad Request");
            }

            byte[] originalMessageBytes = Encoding.ASCII.GetBytes(message);
            byte[] signedDataBytes = _rsaProvider.SignData(originalMessageBytes, SHA1.Create());
            string hexWithDashes = BitConverter.ToString(signedDataBytes);

            return Ok(hexWithDashes);
        }
    }
}
