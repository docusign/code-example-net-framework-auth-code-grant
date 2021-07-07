using System.Collections.Generic;

namespace code_example_net_framework_auth_code_grant.Models
{
    public class DSUserInfo
    {
        public string Sub { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<Account> Accounts { get; set; }
    }
}