using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#region Task 10 and beyond

class Client
{
    private static readonly HttpClient client = new HttpClient();
    private static string apiKey = "";
    private static string username = "";
    private static string baseUrl = "http://localhost:53415/api";

    static async Task Main()
    {
        Console.WriteLine("Hello. What would you like me to do?");

        while (true)
        {
            Console.Write("\n> ");
            string input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(input)) continue;
            if (input == "exit") break;


            string[] inputCommandAndArg = Regex.Replace(input.Trim(), @"\s+", " ").Split(' ');
            string controller = inputCommandAndArg[0];
            string action = inputCommandAndArg[1];
            string arg = inputCommandAndArg.Length > 2 ? inputCommandAndArg[2] : "";
            string arg2 = inputCommandAndArg.Length > 3 ? inputCommandAndArg[3] : "";

            switch (controller)
            {
                case "talkback":

                    switch (action)
                    {
                        case "hello":
                            await TalkBackHello();
                            break;
                        case "sort":
                            await TalkBackSort(arg);
                            break;
                        default:
                            Console.WriteLine(
                                "Invalid action API endpoint for Talkback controller. Can only be \"Hello\" or \"Sort\".");
                            break;
                    }
                    break;

                case "user":
                    switch (action)
                    {
                        case "get":
                            await UserGet(arg);
                            break;
                        case "post":
                            await UserPost(arg);
                            break;
                        case "set":
                            await UserSet(arg, arg2);
                            break;
                        case "delete":
                            await UserDelete();
                            break;
                        case "role":
                            await UserRole(arg, arg2);
                            break;
                        default:
                            Console.WriteLine(
                                "Invalid action API endpoint for User controller. Can only be \"Get\", \"Post\", \"Set\", \"Delete\" or \"Role\".");
                            break;
                    }
                    break;

                case "protected":
                    switch (action)
                    {
                        case "hello":
                            await ProtectedHello();
                            break;
                        case "sha1":
                            await ProtectedSha1(arg);
                            break;
                        case "sha256":
                            await ProtectedSha256(arg);
                            break;
                        default:
                            Console.WriteLine(
                                "Invalid action API endpoint for Protected controller. Can only be \"Hello\", \"Sha1\", or \"Sha256\".");
                            break;
                    }
                    break;

                default:
                    Console.WriteLine("Invalid controller API endpoint. Can only go through \"Talkback\", \"User\", or \"Protected\". \"Exit\" to exit client");
                    break;
            }
        }
    }

    private static async Task ProtectedSha256(string arg)
    {
        throw new NotImplementedException();
    }

    private static async Task ProtectedSha1(string arg)
    {
        throw new NotImplementedException();
    }

    private static async Task ProtectedHello()
    {
        throw new NotImplementedException();
    }

    private static async Task UserRole(string arg, string arg2)
    {
        throw new NotImplementedException();
    }

    private static async Task UserDelete()
    {
        throw new NotImplementedException();
    }

    private static async Task UserSet(string arg, string arg2)
    {
        throw new NotImplementedException();
    }

    private static async Task UserPost(string arg)
    {
        throw new NotImplementedException();
    }

    private static async Task UserGet(string arg)
    {
        throw new NotImplementedException();
    }

    private static async Task TalkBackSort(string arg)
    {
        throw new NotImplementedException();
    }

    private static async Task TalkBackHello()
    {
        Console.WriteLine("...please wait...");
        var response = await client.GetStringAsync($"{baseUrl}/talkback/hello");
        Console.WriteLine(response);
    }
}

#endregion