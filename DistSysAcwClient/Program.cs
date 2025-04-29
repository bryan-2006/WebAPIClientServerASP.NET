using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


#region Task 10 and beyond

class Client
{
    private static readonly HttpClient client = new HttpClient();
    private static string apiKey = "";
    private static string username = "";
    private static string publicKey = "";
    // private static string baseUrl = "http://150.237.94.9/2928359/Api";
    private static string baseUrl = "http://localhost:53415/api";
    private static RSACryptoServiceProvider _clientRsaProvider = new RSACryptoServiceProvider();

    static async Task Main()
    {
        Console.WriteLine("Hello. What would you like me to do?");
        bool firstCommand = true;

        while (true)
        {
            Console.Write("\n> ");
            string input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) continue;
            if (input == "Exit") Environment.Exit(0);

            if (!firstCommand) { Console.Clear(); }

            string[] inputCommandAndArg = input.Split(new[] { ' ' }, 4);
            // string[] inputCommandAndArg = Regex.Replace(input.Trim(), @"\s+", " ").Split(new[] { ' ' }, 4); //.Split(' '); 
            string controller = inputCommandAndArg[0];
            string action = inputCommandAndArg.Length > 1 ? inputCommandAndArg[1] : "";
            string arg = inputCommandAndArg.Length > 2 ? inputCommandAndArg[2] : "";
            string arg2 = inputCommandAndArg.Length > 3 ? inputCommandAndArg[3] : "";

            switch (controller)
            {
                case "Talkback":

                    switch (action)
                    {
                        case "Hello":
                            await TalkBackHello();
                            break;
                        case "Sort":
                            await TalkBackSort(arg);
                            break;
                        default:
                            Console.WriteLine(
                                "Invalid action API endpoint for Talkback controller. " +
                                "Can only be \"Hello\" or \"Sort\" (case sensitive).");
                            break;
                    }
                    break;

                case "User":
                    switch (action)
                    {
                        case "Get":
                            await UserGet(arg);
                            break;
                        case "Post":
                            await UserPost(arg);
                            break;
                        case "Set":
                            UserSet(arg, arg2);
                            break;
                        case "Delete":
                            await UserDelete();
                            break;
                        case "Role":
                            await UserRole(arg, arg2);
                            break;
                        default:
                            Console.WriteLine(
                                "Invalid action API endpoint for User controller. " +
                                "Can only be \"Get\", \"Post\", \"Set\", \"Delete\" or " +
                                "\"Role\" (case sensitive).");
                            break;
                    }
                    break;

                case "Protected":

                    if (action == "SHA1" ||  action == "SHA256" || action == "Sign" || action == "Mashify")
                    {
                        if (arg2 != "") arg += " " + arg2;  // || arg.EndsWith(" ")
                    }

                    switch (action)
                    {
                        case "Hello":
                            await ProtectedHello();
                            break;
                        case "SHA1":
                            await ProtectedSha1(arg);
                            break;
                        case "SHA256":
                            await ProtectedSha256(arg);
                            break;
                        case "Get":
                            if (arg == "PublicKey") { await ProtectedPublicKeyRetrieve(); }
                            else
                            {
                                Console.WriteLine(
                                "Invalid action API endpoint for Protected Get method. " +
                                "Can only be \"Get PublicKey\" (case sensitive).");
                            }
                            break;
                        case "Sign":
                            await ProtectedSign(arg);
                            break;
                        case "Mashify":
                            await Mashify(arg);
                            break;
                        default:
                            Console.WriteLine(
                                "Invalid action API endpoint for Protected controller. " +
                                "Can only be \"Hello\", \"SHA1\", \"SHA256\", " +
                                "\"Get PublicKey\", \"Sign <message>\", or \"Mashify <message>\" (case sensitive).");
                            break;
                    }
                    break;

                default:
                    Console.WriteLine("Invalid controller API endpoint. " +
                        "Can only go through \"Talkback\", \"User\", or \"Protected\". " +
                        "\"Exit\" to exit client (case sensitive).");
                    break;
            }

            Console.WriteLine("\n");
            Console.WriteLine("What would you like to do next?");
            firstCommand = false;

        }
    }

    private static async Task Mashify(string arg)
    {
        Console.WriteLine("...please wait..."); Console.WriteLine("");

        if (apiKey == "")
        {
            Console.WriteLine("You need to do a User Post or User Set first");
            return;
        }

        if (string.IsNullOrEmpty(publicKey))
        {
            Console.WriteLine("Client doesn't yet have the public key");
            return;
        }

        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();

        string encryptedMsg = BitConverter.ToString(_clientRsaProvider.Encrypt(
            Encoding.ASCII.GetBytes(arg), RSAEncryptionPadding.OaepSHA1));
        string encryptedKey = BitConverter.ToString(_clientRsaProvider.Encrypt(
            aes.Key, RSAEncryptionPadding.OaepSHA1)); ;
        string encryptedIV = BitConverter.ToString(_clientRsaProvider.Encrypt(
            aes.IV, RSAEncryptionPadding.OaepSHA1)); ;

        Console.WriteLine(encryptedMsg + "\n" + encryptedKey + "\n" + encryptedIV);

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{baseUrl}/Protected/Mashify"),
            Content = new StringContent
            ($"{{\"message\":\"{encryptedMsg}\"," +
            $"\"symmetricKey\":\"{encryptedKey}\", " +
            $"\"initVector\":\"{encryptedIV}\"}}",
            Encoding.ASCII, "application/json")
        };

        requestMessage.Headers.Add("ApiKey", apiKey);
        var response = await client.SendAsync(requestMessage);
        string hexWithDashes = await response.Content.ReadAsStringAsync();
        string hexNoDashes = hexWithDashes.Replace("-", "");
        byte[] signedBytes = Convert.FromHexString(hexNoDashes);
        //Console.WriteLine($"{hexWithDashes}");

        // Convert server response hex to byte[]
        byte[] encryptedMashifiedBytes = hexWithDashes
            .Split('-')
            .Where(hex => !string.IsNullOrWhiteSpace(hex))
            .Select(hex => Convert.ToByte(hex, 16)).ToArray();

        // Decrypt the mashified bytes using original AES key and IV
        string mashifiedText;
        using (var decryptor = aes.CreateDecryptor())
        {
            byte[] decrypted = decryptor.TransformFinalBlock(encryptedMashifiedBytes, 0, encryptedMashifiedBytes.Length);
            mashifiedText = Encoding.ASCII.GetString(decrypted);
        }

        // Reverse the decrypted mashified text
        string msgWithVowelsReplaced = new string(mashifiedText.Reverse().ToArray())
            .Replace("a", "X").Replace("A", "X").Replace("e", "X").Replace("E", "X")
            .Replace("i", "X").Replace("I", "X").Replace("o", "X").Replace("O", "X")
            .Replace("u", "X").Replace("U", "X");

        char[] charArray = msgWithVowelsReplaced.ToCharArray();
        Array.Reverse(charArray);
        string reversedXMsg = new string(charArray);

        Console.WriteLine(reversedXMsg);
    }

    private static async Task ProtectedSign(string arg)
    {
        Console.WriteLine("...please wait..."); Console.WriteLine("");
        try
        {
            if (apiKey == "")
            {
                Console.WriteLine("You need to do a User Post or User Set first");
                return;
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                Console.WriteLine("Client doesn't yet have the public key");
                return;
            }
            Console.WriteLine("...please wait..."); Console.WriteLine("");
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{baseUrl}/Protected/Sign?message={arg}"),
            };

            requestMessage.Headers.Add("ApiKey", apiKey);

            var response = await client.SendAsync(requestMessage);
            string hexWithDashes = await response.Content.ReadAsStringAsync();

            string hexNoDashes = hexWithDashes.Replace("-", "");
            byte[] signedBytes = Convert.FromHexString(hexNoDashes);

            byte[] dataToCompare = Encoding.ASCII.GetBytes(arg);
            bool verified = _clientRsaProvider.VerifyData(dataToCompare, SHA1.Create(), signedBytes);

            if (verified)
            {
                Console.WriteLine("Message was successfully signed");
            }
            else
            {
                Console.WriteLine("Message was not successfully signed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Message was not successfully signed");
        }
    }

    private static async Task ProtectedPublicKeyRetrieve()
    {
        Console.WriteLine("...please wait..."); Console.WriteLine("");
        if (apiKey == "")
        {
            Console.WriteLine("You need to do a User Post or User Set first");
            return;
        }
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{baseUrl}/Protected/GetPublicKey"),
        };

        requestMessage.Headers.Add("ApiKey", apiKey);

        var response = await client.SendAsync(requestMessage);

        if (response.IsSuccessStatusCode)
        {
            publicKey = await response.Content.ReadAsStringAsync();

            _clientRsaProvider.FromXmlString(publicKey);
            Console.WriteLine("Got Public Key");
        }
        else
        {
            Console.WriteLine("Couldn't Get the Public Key");
        }
    }

    private static async Task ProtectedEndpointTemplate(string endpoint)
    {
        Console.WriteLine("...please wait..."); Console.WriteLine("");
        if (apiKey == "")
        {
            Console.WriteLine("You need to do a User Post or User Set first");
            return;
        }
        Console.WriteLine("...please wait..."); Console.WriteLine("");
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{baseUrl}/Protected/{endpoint}"),
        };

        requestMessage.Headers.Add("ApiKey", apiKey);

        var response = await client.SendAsync(requestMessage);

        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }

    private static async Task ProtectedSha256(string arg)
    {
        await ProtectedEndpointTemplate($"sha256?message={arg}");
    }

    private static async Task ProtectedSha1(string arg)
    {
        await ProtectedEndpointTemplate($"sha1?message={arg}");
    }

    private static async Task ProtectedHello()
    {
        await ProtectedEndpointTemplate("hello");
    }

    private static async Task UserRole(string username, string role)
    {
        Console.WriteLine("...please wait..."); Console.WriteLine("");
        if (apiKey == "")
        {
            Console.WriteLine("You need to do a User Post or User Set first");
            return;
        }
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{baseUrl}/User/changerole"),
            Content = new StringContent($"{{\"username\":\"{username}\",\"role\":\"{role}\"}}",
            Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.Add("ApiKey", apiKey);

        var response = await client.SendAsync(requestMessage);

        Console.WriteLine(await response.Content.ReadAsStringAsync());

    }

    private static async Task UserDelete()
    {
        Console.WriteLine("...please wait..."); Console.WriteLine("");
        if (username == "" || apiKey == "")
        {
            Console.WriteLine("You need to do a User Post or User Set first");
            return;
        }
        var deleteRequest = new HttpRequestMessage(
            HttpMethod.Delete, $"{baseUrl}/User/removeuser?username={username}");
        deleteRequest.Headers.Add("ApiKey", apiKey);

        var response = await client.SendAsync(deleteRequest);

        string responseContent = await response.Content.ReadAsStringAsync();

        if (responseContent == "true")
        {
            apiKey = ""; username = "";
            Console.WriteLine("True");
        }
        else
        {
            Console.WriteLine("False");
        }
    }

    private static void UserSet(string usernameInput, string apiInput)
    {
        Console.WriteLine("...please wait..."); Console.WriteLine("");
        username = usernameInput;
        apiKey = apiInput;
        Console.WriteLine("Stored");
    }

    private static async Task UserPost(string arg)
    {
        Console.WriteLine("...please wait..."); Console.WriteLine("");

        var jsonPayload = new StringContent($"\"{arg}\"", Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{baseUrl}/User/new", jsonPayload);

        if (response.IsSuccessStatusCode)
        {
            apiKey = await response.Content.ReadAsStringAsync();
            username = arg;
            Console.WriteLine("Got API Key");
        }
        else
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
        
    }

    private static async Task UserGet(string arg)
    {
        await WriteAsyncWithOnlyURI($"/User/new?username={arg}");
    }

    private static async Task TalkBackSort(string arg)
    {
        if (arg == "")
        {
            await WriteAsyncWithOnlyURI("/talkback/sort");
        }
        else
        {
            string[] elements = Regex.Split(arg.Trim('[', ']'), @",\s*");
            var firstElement = elements.Length > 0 ? elements[0] : "";
            string integersFromQuery = $"integers={firstElement}";

            for (int i = 1; i < elements.Length; i++)
            {
                integersFromQuery += $"&integers={elements[i]}";
            }

            await WriteAsyncWithOnlyURI($"/talkback/sort?{integersFromQuery}");
        }
    }

    private static async Task TalkBackHello()
    {
        await WriteAsyncWithOnlyURI("/talkback/hello");
    }

    private static async Task WriteAsyncWithOnlyURI(string route)
    {
        Console.WriteLine("...please wait..."); Console.WriteLine("");

        try
        {
            var response = await client.GetStringAsync($"{baseUrl}" + route);
            Console.WriteLine(response);
        }
        catch (Exception)
        {
            //Console.WriteLine(e.Message);

            var response = await client.GetAsync($"{baseUrl}{route}");
            string errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(errorContent);
        }
    }
}

#endregion