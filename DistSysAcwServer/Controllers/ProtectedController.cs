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
        public async Task<IActionResult> Hello([FromHeader(Name = "ApiKey")] string apiKey)
        {
            //if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            //{
            //    return Ok(false);
            //}
            await _userDataAccess.LogActivity(apiKey, "/Protected/Hello");
            var user = await _userDataAccess.GetUserWithAPI(apiKey);
            //if (user == null)
            //{
            //    return Ok(false);
            //}

            return Ok("Hello " + user.UserName);
        }

        [HttpGet("sha1")]
        public async Task<IActionResult> Sha1([FromQuery] string? message, [FromHeader(Name = "ApiKey")] string apiKey)
        {
            //if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            //{
            //    return Ok(false);
            //}

            await _userDataAccess.LogActivity(apiKey, "/Protected/SHA1");

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
        public async Task<IActionResult> Sha256([FromQuery] string? message, [FromHeader(Name = "ApiKey")] string apiKey)
        {
            //if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            //{
            //    return Ok(false);
            //}

            await _userDataAccess.LogActivity(apiKey, "/Protected/SHA256");

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
        public async Task<IActionResult> GetPublicKey([FromHeader(Name = "ApiKey")] string apiKey)
        {
            //if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            //{
            //    return Unauthorized();
            //}
            await _userDataAccess.LogActivity(apiKey, "/Protected/GetPublicKey");
            return Ok(_rsaProvider.ToXmlString(false));

        }

        [HttpGet("sign")]
        public async Task<IActionResult> Sign([FromQuery] string? message, [FromHeader(Name = "ApiKey")] string apiKey)
        {
            await _userDataAccess.LogActivity(apiKey, "/Protected/Sign");
            //if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            //{
            //    return Unauthorized();
            //}

            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("Bad Request");
            }

            byte[] originalMessageBytes = Encoding.ASCII.GetBytes(message);
            byte[] signedDataBytes = _rsaProvider.SignData(originalMessageBytes, SHA1.Create());
            string hexWithDashes = BitConverter.ToString(signedDataBytes);

            return Ok(hexWithDashes);
        }


        [HttpGet("mashify")]
        public async Task<IActionResult> Mashify([FromHeader(Name = "ApiKey")] string apiKey)
        {
            await _userDataAccess.LogActivity(apiKey, "/Protected/Mashify");
            Console.WriteLine("hi");
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();
            Console.WriteLine(rawBody);
            Dictionary<string, string> body = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(rawBody);

            if (body == null ||
                !body.TryGetValue("message", out var message) || string.IsNullOrWhiteSpace(message) ||
                !body.TryGetValue("symmetricKey", out var symmetricKey) || string.IsNullOrWhiteSpace(symmetricKey) ||
                !body.TryGetValue("initVector", out var initVector) || string.IsNullOrWhiteSpace(initVector))
            {
                return BadRequest("Bad Request");
            }

            byte[] encryptedMsgBytes = Convert.FromHexString(message.Replace("-", ""));
            byte[] encryptedKeyBytes = Convert.FromHexString(symmetricKey.Replace("-", ""));
            byte[] encryptedInitVector = Convert.FromHexString(initVector.Replace("-", ""));

            byte[] msgBytes = _rsaProvider.Decrypt(encryptedMsgBytes, RSAEncryptionPadding.OaepSHA1);
            byte[] aesKeyBytes = _rsaProvider.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.OaepSHA1);
            byte[] iVectorBytes = _rsaProvider.Decrypt(encryptedInitVector, RSAEncryptionPadding.OaepSHA1);

            string msg = Encoding.ASCII.GetString(msgBytes);
            string mashified = new string(msg.Reverse().ToArray());

            byte[] encryptedMashifiedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKeyBytes;
                aes.IV = iVectorBytes;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                byte[] mashifiedBytes = Encoding.ASCII.GetBytes(mashified);
                encryptedMashifiedBytes = encryptor.TransformFinalBlock(mashifiedBytes, 0, mashifiedBytes.Length);
            }

            string responseHex = BitConverter.ToString(encryptedMashifiedBytes);
            return Ok(responseHex);
        }
    }
}
