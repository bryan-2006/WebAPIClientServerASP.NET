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
        private readonly string ContainerName = "ProtectedKeyContainer";

        public ProtectedController(UserDatabaseAccess userDataAccess)
        {
            _userDataAccess = userDataAccess;

            /*
             * While working on Sign I realized the server was not properly signing the message for client. 
             * 
             * After further inspection on the client and server I realized this is due to fact that the 
             * private and public key pair was getting regenerated on the server for each new request. 
             * 
             * After discussing with peers I still could not pinpoint why exactly it did not work for me since 
             * it seemed to work fine for them. I dug out the code I assumed was faulty and tried using ChatGPT
             * to debug but it kept hallucinating and thinking it did work and/or saying all I have to do is add more
             * Console.WriteLines to debug which was not helpful at all since I already knew that it was not in fact working
             * and why exactly it was not working. I needed a proper way to have the public private key pair to persist
             * while using different methods in this controller. 
             * 
             * This is when I tried to then consult Stack Overflow. I have never needed to make my very own post before and
             * was pretty excited to but Stack Overflow was not letting me create a new account for some reason.
             * 
             * I then joined the C# Discord server and made a thread on the help channel with a description of my problem
             * and the code I assumed was faulty as mentioned previously. I had used the code clip provided in the lectures
             * when using CspParameters and I realized this would not work for me. My old code on here was as such:
             * 
             * 
             
             cspParams = new CspParameters();
             cspParams.Flags = CspProviderFlags.UseMachineKeyStore;
             _rsaProvider = new RSACryptoServiceProvider(cspParams);

             * I was told that I instead have to set a constant string to CspParameters.KeyContainerName and 
             * RSACryptoServiceProvider.PersistKeyInCsp to true (GoldenFapple, 2025) to achieve what I need. 
             * Hence my code has been updated
             * 
             * 
             * 
             * GoldenFapple (2025) Verify Signed Message with Server's Public Key?. C# [Discord]. 19 April. 
             * https://discord.com/channels/143867839282020352/1362850494044705004/1363049423927640164 [Accessed 19 April 2025].
             */


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
            await _userDataAccess.LogActivity(apiKey, "/Protected/Hello");
            var user = await _userDataAccess.GetUserWithAPI(apiKey);

            return Ok("Hello " + user.UserName);
        }

        [HttpGet("sha1")]
        public async Task<IActionResult> Sha1([FromQuery] string? message, [FromHeader(Name = "ApiKey")] string apiKey)
        {
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
            await _userDataAccess.LogActivity(apiKey, "/Protected/GetPublicKey");
            return Ok(_rsaProvider.ToXmlString(false));
        }

        [HttpGet("sign")]
        public async Task<IActionResult> Sign([FromQuery] string? message, [FromHeader(Name = "ApiKey")] string apiKey)
        {
            await _userDataAccess.LogActivity(apiKey, "/Protected/Sign");
            
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("Bad Request");
            }

            byte[] originalMessageBytes = Encoding.ASCII.GetBytes(message);
            byte[] signedDataBytes = _rsaProvider.SignData(originalMessageBytes, SHA1.Create());
            string hexWithDashes = BitConverter.ToString(signedDataBytes);

            return Ok(hexWithDashes);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("mashify")]
        public async Task<IActionResult> Mashify([FromHeader(Name = "ApiKey")] string apiKey, [FromBody] Dictionary<string, string> body)
        {
            await _userDataAccess.LogActivity(apiKey, "/Protected/Mashify");
            Console.WriteLine(body);
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();
            
            // Dictionary<string, string> body = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(rawBody);

            if (body == null ||
                !body.TryGetValue("EncryptedString", out var message) || string.IsNullOrWhiteSpace(message) ||
                !body.TryGetValue("EncryptedSymKey", out var symmetricKey) || string.IsNullOrWhiteSpace(symmetricKey) ||
                !body.TryGetValue("EncryptedIV", out var initVector) || string.IsNullOrWhiteSpace(initVector))
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
