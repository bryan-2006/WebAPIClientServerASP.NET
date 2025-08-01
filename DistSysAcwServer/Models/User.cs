﻿using System.ComponentModel.DataAnnotations;

namespace DistSysAcwServer.Models
{
    /// <summary>
    /// User data class
    /// </summary>
    public class User
    {
        #region Task2
        // TODO: Create a User Class for use with Entity Framework
        // Note that you can use the [key] attribute to set your ApiKey Guid as the primary key 
        #endregion
        [Key]
        public required string ApiKey { get; set; }

        [Required]
        public required string UserName { get; set; }

        public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

        [Required]
        public required string Role { get; set; }
        public User() { }
    }

    #region Task13?
    // TODO: You may find it useful to add code here for Logging
    #endregion


}