using code_example_net_framework_auth_code_grant.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;

namespace code_example_net_framework_auth_code_grant.Controllers
{
    public abstract class DSBaseController : Controller
    {
        public IDSUserService _dSUserService { get; set; }

        public DSBaseController(IDSUserService requestItemsService)
        {
            _dSUserService = requestItemsService;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Controller controller = filterContext.Controller as Controller;
            var identity = filterContext.HttpContext.User.Identity as ClaimsIdentity;

            if (identity == null || !identity.IsAuthenticated)
                return;

            if (_dSUserService.AppUser?.AccessToken != null)
                return;

            _dSUserService.AppUser = new DSAppUser()
            {
                Name = identity.FindFirst(x => x.Type.Equals(ClaimTypes.Name)).Value,
                UserGuid = identity.FindFirst(x => x.Type.Equals(ClaimTypes.NameIdentifier)).Value,
                Email = identity.FindFirst(x => x.Type.Equals(ClaimTypes.Email)).Value,
                AccessToken = identity.FindFirst(x => x.Type.Equals("access_token")).Value,
                RefreshToken = identity.FindFirst(x => x.Type.Equals("refresh_token")).Value,
                AccessExpireIn = DateTime.Parse(identity.FindFirst(x => x.Type.Equals("access_expires_in")).Value ??
                    identity.Claims.First(x => x.Type.Equals("access_expires_in")).Value),
                RefreshExpireIn = DateTime.Parse(identity.FindFirst(x => x.Type.Equals("refresh_expires_in")).Value ??
                    identity.Claims.First(x => x.Type.Equals("refresh_expires_in")).Value),
                Accounts = JsonConvert.DeserializeObject<List<Account>>(identity.FindFirst(x => x.Type.Equals("accounts")).Value)
            };
        }
    }
}