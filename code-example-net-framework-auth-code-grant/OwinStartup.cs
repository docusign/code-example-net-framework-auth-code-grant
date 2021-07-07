using Autofac;
using code_example_net_framework_auth_code_grant;
using code_example_net_framework_auth_code_grant.Models;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

[assembly: OwinStartup(typeof(code_example_net_framework_auth_code_grant.OwinStartup))]

namespace code_example_net_framework_auth_code_grant
{

    public class OwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            app.UseDocuSignAuthentication();
        }
    }

    public static class DocuSignAuthenticationExtensions
    {
        public static IAppBuilder UseDocuSignAuthentication(this IAppBuilder app)
        {
            DocuSignAuthenticationOptions docuSignAuthenticationOptions = new DocuSignAuthenticationOptions
            {
                ClientId = ConfigurationManager.AppSettings["ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["SecretKey"].ToString(),
                AuthorizationEndpoint = ConfigurationManager.AppSettings["AuthorizationEndpoint"],
                TokenEndpoint = ConfigurationManager.AppSettings["TokenEndpoint"],
                UserInformationEndpoint = ConfigurationManager.AppSettings["UserInformationEndpoint"],
                AppUrl = ConfigurationManager.AppSettings["AppUrl"],
                GatewayAccountId = ConfigurationManager.AppSettings["GatewayAccountId"],
                GatewayName = ConfigurationManager.AppSettings["GatewayName"],
                GatewayDisplayName = ConfigurationManager.AppSettings["GatewayDisplayName"],
                CallbackPath = new PathString(ConfigurationManager.AppSettings["CallbackPath"]),
                RequiredAccount = ConfigurationManager.AppSettings["RequiredAccount"]
            };

            return app.Use(typeof(DocuSignAuthenticationMiddleware), app, docuSignAuthenticationOptions);
        }
    }

    public class DocuSignAuthenticationMiddleware : AuthenticationMiddleware<DocuSignAuthenticationOptions>
    {

        public DocuSignAuthenticationMiddleware(OwinMiddleware next, IAppBuilder app, DocuSignAuthenticationOptions options)
            : base(next, options)
        {
            if (options.StateDataFormat == null)
            {
                var dataProtector = app.CreateDataProtector(typeof(DocuSignAuthenticationMiddleware).FullName,
                    options.AuthenticationType);

                options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }
        }

        // Called for each request, to create a handler for each request.
        protected override AuthenticationHandler<DocuSignAuthenticationOptions> CreateHandler()
        {
            return new DocuSignAuthenticationHandler();
        }
    }

    public class DocuSignAuthenticationOptions : AuthenticationOptions
    {
        public DocuSignAuthenticationOptions()
            : base(DSConstants.DefaultAuthenticationType)
        {
            Description.Caption = DSConstants.DefaultAuthenticationType;
            AuthenticationMode = AuthenticationMode.Passive;
        }


        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AuthorizationEndpoint { get; set; }
        public string TokenEndpoint { get; set; }
        public string UserInformationEndpoint { get; set; }
        public string AppUrl { get; set; }
        public string GatewayAccountId { get; set; }
        public string GatewayName { get; set; }
        public string GatewayDisplayName { get; set; }
        public PathString CallbackPath { get; set; }
        public string RequiredAccount { get; set; }
        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }
    }

    public class DocuSignAuthenticationHandler : AuthenticationHandler<DocuSignAuthenticationOptions>
    {
        private static HttpClient client;

        /// <summary>
        /// Create new ClaimsIdentity
        /// </summary>
        /// <returns>AuthenticationTicket</returns>
        protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            var keyValues = new Dictionary<string, string>();
            var identity = Context.Authentication.User.Identity as ClaimsIdentity;

            keyValues.Add("grant_type", "authorization_code");
            keyValues.Add("code", Request.Query["code"]);

            if (identity.HasClaim(c => c.Type == "refresh_token"))
            {
                string refreshToken = identity.FindFirst(x => x.Type.Equals("refresh_token")).Value;
                var refreshExpiresIn = DateTime.Parse(identity.FindFirst(x => x.Type.Equals("refresh_expires_in")).Value);
                if (DateTime.Now.Subtract(TimeSpan.FromMinutes(DSConstants.Buffer)) < refreshExpiresIn)
                {
                    keyValues.Add("grant_type", "refresh_token");
                    keyValues.Add("refresh_token", refreshToken);
                }
            }

            Token token = Authenticate(keyValues);
            var userInfo = GetUserInfo(token.AccessToken);

            // ASP.Net Identity requires the NameIdentitifer field to be set or it won't  
            // accept the external login (AuthenticationManagerExtensions.GetExternalLoginInfo)

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userInfo.Sub));
            claims.Add(new Claim(ClaimTypes.Name, userInfo.Name));
            claims.Add(new Claim(ClaimTypes.Email, userInfo.Email));
            claims.Add(new Claim("accounts", JsonConvert.SerializeObject(userInfo.Accounts)));
            claims.Add(new Claim("access_token", token.AccessToken));
            claims.Add(new Claim("access_expires_in", DateTime.Now.Add(TimeSpan.FromSeconds(Convert.ToDouble(token.ExpiresIn))).ToString()));
            claims.Add(new Claim("refresh_token", token.RefreshToken));
            claims.Add(new Claim("refresh_expires_in", DateTime.Now.AddDays(DSConstants.RefreshTokenLife).ToString()));

            identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationType);

            // Query - code == state
            var properties = Options.StateDataFormat.Unprotect(Request.Query["state"]);

            return Task.FromResult(new AuthenticationTicket(identity, properties));
        }

        /// <summary>
        /// Called for every request to handle Response. 
        /// 401 will trigger Autorization Code Grant call and will redirect to 
        /// DocuSign oauth server https://account-d.docusign.com/oauth/auth for authentication
        /// </summary>
        /// <returns></returns>
        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode == 401)
            {
                var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);

                // Only react to 401 if there is an authentication challenge for the authentication 
                // type of this handler.
                if (challenge != null)
                {
                    var state = challenge.Properties;

                    if (string.IsNullOrEmpty(state.RedirectUri))
                    {
                        state.RedirectUri = Request.Uri.ToString();
                    }

                    var stateString = Options.StateDataFormat.Protect(state);
                    // save state in application
                    HttpContext.Current.Application[stateString] = stateString;

                    Response.Redirect(
                        WebUtilities.AddQueryString(
                            Options.AuthorizationEndpoint,
                            new Dictionary<string, string>{
                                { "client_id", Options.ClientId },
                                { "scope", "signature extended" },
                                { "response_type", "code" },
                                { "state", stateString },
                                { "redirect_uri", Options.AppUrl + Options.CallbackPath.ToString() }
                            })
                    );
                }
            }

            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Handle response from oauth server
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> InvokeAsync()
        {
            var state = Request.Query.Get("state");
            var stateOk = HttpContext.Current.Application.Get(state);

            // Handle response from oauth server
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path && stateOk != null)
            {
                var ticket = await AuthenticateAsync();

                if (ticket != null)
                {
                    Context.Authentication.SignIn(ticket.Properties, ticket.Identity);

                    //remove saved state value
                    HttpContext.Current.Application.Remove(state);

                    Response.Redirect(ticket.Properties.RedirectUri);

                    // Prevent further processing by the owin pipeline.
                    return true;
                }
            }
            // Let the rest of the pipeline run.
            return false;
        }

        /// <summary>
        /// Exchanges authorization code for access token
        /// </summary>
        /// <returns>Authentication Code Grant token</returns>
        private Token Authenticate(Dictionary<string, string> keyValues)
        {
            var bodyContent = new FormUrlEncodedContent(keyValues);

            var keySecretEncode = ASCIIEncoding.ASCII.GetBytes($"{Options.ClientId}:{Options.ClientSecret}");
            var base64KeySecretEncode = Convert.ToBase64String(keySecretEncode);

            client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);
            request.Content = bodyContent;
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64KeySecretEncode);

            var response = client.SendAsync(request).Result;
            var result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            return new Token()
            {
                AccessToken = (string)result["access_token"],
                RefreshToken = (string)result["refresh_token"],
                ExpiresIn = (string)result["expires_in"]
            };
        }

        /// <summary>
        /// Retreive user account info from https://account-d.docusign.com/oauth/userinfo
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns>UserInfo</returns>
        private DSUserInfo GetUserInfo(string access_token)
        {
            client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = client.SendAsync(request).Result;
            var result = JObject.Parse(response.Content.ReadAsStringAsync().Result);

            List<Account> accounts = new List<Account>();
            accounts = (from a in result["accounts"]
                        select new Account
                        {
                            AccountName = (string)a["account_name"],
                            AccountId = (string)a["account_id"],
                            BasePath = (string)a["base_uri"] + DSConstants.APIType,
                            IsDefault = (bool)a["is_default"]
                        }
                        ).OrderByDescending(o => o.IsDefault ? 1 : 0).ToList();
            if (ConfigurationManager.AppSettings["RequiredAccount"].Length > 0)
            {
                accounts = accounts.Where(a => a.AccountId.Equals(ConfigurationManager.AppSettings["RequiredAccount"])).ToList();
            }

            return new DSUserInfo()
            {
                Sub = (string)result["sub"],
                Name = (string)result["name"],
                Email = (string)result["email"],
                Accounts = accounts
            };
        }
    }

    /// <summary>
    /// Custom class to triger 401.  
    /// </summary>
    public class DSChallengeResult : HttpUnauthorizedResult
    {
        public string LoginProvider { get; set; }
        public string RedirectUri { get; set; }

        public DSChallengeResult(string redirectUri = "/")
        {
            LoginProvider = DSConstants.LoginProvider;
            RedirectUri = redirectUri;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
            context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
        }
    }
}
