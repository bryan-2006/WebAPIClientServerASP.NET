using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DistSysAcwServer.Controllers;
using DistSysAcwServer.Middleware;
using DistSysAcwServer.Models;
using DistSysAcwServer.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;


namespace DistSysAcwServer.Auth
{
    /// <summary>
    /// Authenticates clients by API Key
    /// </summary>
    public class CustomAuthenticationHandlerMiddleware
        : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationHandler
    {
        private readonly UserDatabaseAccess _userDataAccess;
        private Models.UserContext DbContext { get; set; }
        private IHttpContextAccessor HttpContextAccessor { get; set; }
        private SharedError Error { get; set; }

        public CustomAuthenticationHandlerMiddleware(
            UserDatabaseAccess userDataAccess,
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            Models.UserContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            SharedError error)
            : base(options, logger, encoder)
        {
            DbContext = dbContext;
            HttpContextAccessor = httpContextAccessor;
            Error = error;
            _userDataAccess = userDataAccess;
        }

        /// <summary>
        /// Authenticates the client by API Key
        /// </summary>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            #region Task5
            // TODO:  Find if a header ‘ApiKey’ exists, and if it does, check the database to determine if the given API Key is valid
            //        Then create the correct Claims, add these to a ClaimsIdentity, create a ClaimsPrincipal from the identity 
            //        Then use the Principal to generate a new AuthenticationTicket to return a Success AuthenticateResult
            #endregion

            if (!Request.Headers.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                return AuthenticateResult.Fail("Unauthorized. Check ApiKey in Header is correct.");
            }

           
            var user = await _userDataAccess.GetUserWithAPI(apiKey);
           

            if (user == null)
            {
                return AuthenticateResult.Fail("Unauthorized. Check ApiKey in Header is correct.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var identity = new ClaimsIdentity(claims, "ApiKey");
            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, "ApiKey");
            return AuthenticateResult.Success(ticket);
        }

        //protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        //{
        //    Response.StatusCode = 401; // Unauthorized
        //    Response.ContentType = "application/json";
        //    return Response.WriteAsync("\"Unauthorized. Check ApiKey in Header is correct.\"");
        //}

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (!Response.HasStarted)
            {   Response.StatusCode = 401; // Unauthorized
                Response.ContentType = "application/json";

                return Response.WriteAsync("Unauthorized. Check ApiKey in Header is correct.");
            }

            return Task.CompletedTask;
        }
    }
}