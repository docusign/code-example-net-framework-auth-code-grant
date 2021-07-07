using code_example_net_framework_auth_code_grant.Models;

namespace code_example_net_framework_auth_code_grant
{
    public interface IDSUserService
    {
        DSAppUser AppUser { get; set; }
        bool CheckAccessToken(int bufferMin);
        void Logout();
    }

}
