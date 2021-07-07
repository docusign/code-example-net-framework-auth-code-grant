using System;
using System.Collections.Generic;

namespace code_example_net_framework_auth_code_grant.Models
{
    public class DSAppUser
    {
        public string Name { get; set; }
        public string UserGuid { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? AccessExpireIn { get; set; }
        public DateTime? RefreshExpireIn { get; set; }
        public List<Account> Accounts { get; set; }
    }
}