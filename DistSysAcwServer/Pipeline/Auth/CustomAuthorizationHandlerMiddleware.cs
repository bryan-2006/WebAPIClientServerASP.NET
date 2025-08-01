﻿using System;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DistSysAcwServer.Middleware;
using DistSysAcwServer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DistSysAcwServer.Auth
{
    /// <summary>
    /// Authorises clients by role
    /// </summary>
    public class CustomAuthorizationHandlerMiddleware : AuthorizationHandler<RolesAuthorizationRequirement>, IAuthorizationHandler
    {
        private IHttpContextAccessor HttpContextAccessor { get; set; }
        private SharedError Error { get; set; }

        public CustomAuthorizationHandlerMiddleware(IHttpContextAccessor httpContextAccessor, SharedError error)
        {
            HttpContextAccessor = httpContextAccessor;
            Error = error;
        }

        /// <summary>
        /// Handles success or failure of the requirement for the user to be in a specific role
        /// </summary>
        /// <param name="context">Information used to decide on authorisation</param>
        /// <param name="requirement">Authorisation requirements</param>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement)
        {
            // requirement.AllowedRoles contains the roles that are allowed to access the action
            // context.User contains the user trying to access the action
            // context.Succeed(requirement) is used to succeed the requirement
            // context.Fail() is used to fail the requirement
            var user = context.User;

            var userRoles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            bool requiresAdmin = requirement.AllowedRoles != null && 
                     requirement.AllowedRoles.Any() && 
                     requirement.AllowedRoles.Count() == 1 && 
                     requirement.AllowedRoles.Contains("Admin");


            if (requirement.AllowedRoles != null && userRoles.Any(role => requirement.AllowedRoles.Contains(role)))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }


            if (requiresAdmin)
            {
                var httpContext = HttpContextAccessor.HttpContext;
                if (httpContext != null && !httpContext.Response.HasStarted)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    httpContext.Response.ContentType = "application/json";
                    return httpContext.Response.WriteAsync("Forbidden. Admin access only.");
                }
            }


            context.Fail();

            return Task.CompletedTask;
        }
    }
}