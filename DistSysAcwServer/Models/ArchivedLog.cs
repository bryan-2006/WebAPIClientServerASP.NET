using System.ComponentModel.DataAnnotations;

namespace DistSysAcwServer.Models
{
    public class ArchivedLog
    {
        [Key]
        public int LogID { get; set; }
        //[Required]
        public string LogString { get; set; }
        //[Required]
        public DateTime LogDateTime { get; set; }
        public string UserApiKey {  get; set; }

        //public int UserID { get; set; }
        //public User User { get; set; }

        public ArchivedLog() { }
        public ArchivedLog(string logString, DateTime dateTime, string userApiKey)
        {
            LogString = logString;
            LogDateTime = dateTime;
            UserApiKey = userApiKey;
        }
    }
}
