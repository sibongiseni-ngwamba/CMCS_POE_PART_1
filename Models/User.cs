namespace CMCS.Models
{
    public class User
    {
        public int userID { get; set; }
        public string full_names { get; set; } = string.Empty;
        public string surname { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string gender { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public DateTime date { get; set; } = DateTime.Today;
    }
}
