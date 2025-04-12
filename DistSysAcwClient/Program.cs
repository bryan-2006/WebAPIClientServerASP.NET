using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

#region Task 10 and beyond

class Client
{
    private static readonly HttpClient client = new HttpClient();
    private static string apiKey = "";
    private static string username = "";
    private static string publickey = "";
    //private static string baseUrl = "http://150.237.94.9/2928359/Api";
    private static string baseUrl = "http://localhost:53415/api";

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

            string[] inputCommandAndArg = Regex.Replace(input.Trim(), @"\s+", " ").Split(' ');
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
                            break; // Add break statement here
                        default:
                            Console.WriteLine(
                                "Invalid action API endpoint for Protected controller. " +
                                "Can only be \"Hello\", \"SHA1\", \"SHA256\", or " +
                                "\"Get PublicKey\" (case sensitive).");
                            break;
                    }
                    break;

                default:
                    Console.WriteLine("Invalid controller API endpoint. Can only go through \"Talkback\", \"User\", or \"Protected\". \"Exit\" to exit client (case sensitive).");
                    break;
            }

            Console.WriteLine("\n");
            Console.WriteLine("What would you like to do next?");
            firstCommand = false;

        }
    }

    private static async Task ProtectedPublicKeyRetrieve()
    {
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
            publickey = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Got Public Key");
        }
        else
        {
            Console.WriteLine("Couldn't Get the Public Key");
        }
    }

    private static async Task ProtectedEndpointTemplate(string endpoint)
    {
        if (apiKey == "")
        {
            Console.WriteLine("You need to do a User Post or User Set first");
            return;
        }

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
        if(username == "" || apiKey == "")
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
            Console.WriteLine("True");
        }
        else
        {
            Console.WriteLine("False");
        }
    }

    private static void UserSet(string usernameInput, string apiInput)
    {
        username = usernameInput;
        apiKey = apiInput;
        Console.WriteLine("Stored");
    }

    private static async Task UserPost(string arg)
    {
        //JsonContent content = JsonContent.Create(
        //    new { username = $"{arg}"});
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
            //inputCommandAndArg.Length > 1 ? inputCommandAndArg[1]: ""

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

    //private static async Task WriteStringAsyncWithJson(string route, HttpContent requestData)
    //{
        

    //    try
    //    {
    //        var response = await client.PostAsync($"{baseUrl}{route}", requestData);
    //        Console.WriteLine(await response.Content.ReadAsStringAsync());
    //    }
    //    catch(Exception e)
    //    {
    //        Console.WriteLine(e.Message);
    //    }

    //}
}

#endregion