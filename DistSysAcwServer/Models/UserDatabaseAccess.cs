using DistSysAcwServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DistSysAcwServer.Controllers
{
    public class UserDatabaseAccess
    {
        private readonly UserContext _userContext;

        public UserDatabaseAccess(UserContext userContext)
        {
            _userContext = userContext;
        }

        public async Task<string> CreateNewUser(string userName, string role)
        {
            var newUser = new User
            {
                ApiKey = System.Guid.NewGuid().ToString(),
                UserName = userName,
                Role = role
            };

            _userContext.Users.Add(newUser);

            await _userContext.SaveChangesAsync();

            return newUser.ApiKey;
        }

        public async Task<bool> UserExistenceWithAPI(string apiKey)
        {
            return await _userContext.Users.AnyAsync(u => u.ApiKey == apiKey);
        }

        public async Task<bool> UserExistenceWithNameAndAPI(string userName, string apiKey)
        {
            return await _userContext.Users
                .Where(u => EF.Functions.Collate(u.UserName, "Latin1_General_BIN") == userName && u.ApiKey == apiKey)
                .AnyAsync();
        }

        public async Task<User?> GetUserWithAPI(string apiKey)
        {
            return await _userContext.Users.FirstOrDefaultAsync(u => u.ApiKey == apiKey);
        }

        public async Task<bool> DeleteUserWithAPI(string apiKey)
        {
            var user = await _userContext.Users
                .Where(u => EF.Functions.Collate(u.ApiKey, "Latin1_General_BIN") == apiKey)
                .FirstOrDefaultAsync();

            if (user == null) return false;

            _userContext.Users.Remove(user);
            await _userContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AnyUsersExistAsync()
        {
            return await _userContext.Users.AnyAsync();
        }

        public async Task<bool> UserExistenceWithName(string userName)
        {
            return await _userContext.Users
                .Where(u => EF.Functions.Collate(u.UserName, "Latin1_General_BIN") == userName).AnyAsync();
        }

        public async Task<User?> GetUserByName(string username)
        {
            return await _userContext.Users
                .Where(u => EF.Functions.Collate(u.UserName, "Latin1_General_BIN") == username)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateUserRole(string username, string newRole)
        {
            
            var user = await GetUserByName(username);
            if (user == null) return false;

            user.Role = newRole;
            await _userContext.SaveChangesAsync();
            return true;
        }


    }
}
