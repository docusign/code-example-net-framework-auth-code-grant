using code_example_net_framework_auth_code_grant.Models;
using System;
using System.Web;


namespace code_example_net_framework_auth_code_grant
{
    public class DSUserService : IDSUserService
    {
        public bool CheckAccessToken(int bufferMin = 5)
        {
            return HttpContext.Current.User.Identity.IsAuthenticated
                && (DateTime.Now.Subtract(TimeSpan.FromMinutes(bufferMin)) < AppUser?.AccessExpireIn.Value);
        }

        public void Logout()
        {
            AppUser = null;
        }

        public DSAppUser AppUser { get; set; }
    }

}
