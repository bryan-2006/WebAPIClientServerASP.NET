using System.ComponentModel.DataAnnotations;

namespace DistSysAcwServer.Models
{
    public class Log
    {
        [Key]
        public int LogID { get; set; }
        //[Required]
        public string LogString { get; set; }
        //[Required]
        public DateTime LogDateTime { get; set; }

        public string UserApiKey { get; set; }
        public User User { get; set; }

        public Log() { }
        public Log(string endpoint)
        {
            LogString = $"User requested {endpoint}";
            LogDateTime = DateTime.Now;
        }

    }
}
