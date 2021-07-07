using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace code_example_net_framework_auth_code_grant.Controllers
{
    [Route("ds/[action]")]
    public class DSController : Controller
    {
        public IDSUserService _dSUserService { get; private set; }

        public DSController(IDSUserService dSUserService)
        {
            _dSUserService = dSUserService;
        }

        [HttpGet]
        public ActionResult Login(string returnUrl = "/")
        {
            return new DSChallengeResult() { RedirectUri = returnUrl };
        }

        [HttpGet]
        public ActionResult Logout()
        {
            _dSUserService.Logout();
            Request.GetOwinContext().Authentication.SignOut();
            var logout_url = ConfigurationManager.AppSettings["LogoutEndpoint"]
                + "?client_id=" + ConfigurationManager.AppSettings["ClientId"]
                + "&redirect_uri=" + ConfigurationManager.AppSettings["AppUrl"]
                + "&response_mode=logout_redirect";
            return Redirect(logout_url);
        }
    }
}