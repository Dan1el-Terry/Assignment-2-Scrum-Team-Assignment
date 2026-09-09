using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assign_2
{
    /// <summary>
    /// For the table in admin view
    /// </summary>
    public class UserRecord
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string first_name { get; set; }

        public string last_name { get; set; }
        
        public string password_hash { get; set; }
        
        public DateTime date_created  { get; set; }
    }
}