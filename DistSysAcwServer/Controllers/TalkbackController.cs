using System.Collections.Generic;
using DistSysAcwServer.Middleware;
using DistSysAcwServer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DistSysAcwServer.Controllers
{
    public class TalkbackController : BaseController
    {


        /// <summary>
        /// Constructs a TalkBack controller, taking the UserContext through dependency injection
        /// </summary>
        /// <param name="context">DbContext set as a service in Startup.cs and dependency injected</param>
        public TalkbackController(Models.UserContext dbcontext, SharedError error) : base(dbcontext, error) { }


        #region TASK1
        //    added api/talkback/hello response

        // GET: api/talkback/hello
        [HttpGet]
        public IActionResult Hello()
        {
            return Ok("Hello World");
        }
        #endregion

        #region TASK1
        //    TODO:
        //       add a parameter to get integers from the URI query
        //       sort the integers into ascending order
        //       send the integers back as the api/talkback/sort response
        //       conform to the error handling requirements in the spec
        [HttpGet]
        public IActionResult Sort([FromQuery] string[] integers)
        {
            if (integers == null || integers.Length == 0)
            {
                return Ok(new int[] { }); // empty array 
            }

            List<int> parsedIntegers = new List<int>();

            foreach (var value in integers)
            {
                if (!int.TryParse(value, out int number)) // valid integer
                {
                    return BadRequest("Bad request"); // "Bad request"
                }
                parsedIntegers.Add(number);
            }

            parsedIntegers.Sort();
            return Ok(parsedIntegers);
        }
        #endregion
    }
}
