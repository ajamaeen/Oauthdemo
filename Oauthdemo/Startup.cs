using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Oauthdemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureUiPathAuth(services);
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // To Fix the error Exception: Correlation failed. Unknown location
            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                MinimumSameSitePolicy = SameSiteMode.Lax
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        private void ConfigureUiPathAuth(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                // If an authentication cookie is present, use it to get authentication information
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // If authentication is required, and no cookie is present, use Okta (configured below) to sign in
                options.DefaultChallengeScheme = "UiPath";
            })
           .AddCookie() // cookie authentication middleware first
           .AddOAuth("UiPath", options =>
           {
               // Oauth authentication middleware is second

               // When a user needs to sign in, they will be redirected to the authorize endpoint
               options.AuthorizationEndpoint = $"https://cloud.uipath.com/identity_/connect/authorize";

               // scopes when redirecting to the authorization endpoint
               options.Scope.Add("DataService.Schema.Read DataService.Data.Read DataService.Data.Write");
               options.Scope.Add("openid");

               //After the user signs in, an authorization code will be sent to a callback
               // in this app. The OAuth middleware will intercept it
               options.CallbackPath = new PathString("/authorization-code/callback");

               // The OAuth middleware will send the ClientId, ClientSecret, and the
               // authorization code to the token endpoint, and get an access token in return
               options.ClientId = Configuration["AppID"];
               options.ClientSecret = Configuration["AppSecret"];
               options.TokenEndpoint = $"https://cloud.uipath.com/identity_/connect/token";

               //Below we call the userinfo endpoint to get information about the user
               options.UserInformationEndpoint = "https://cloud.uipath.com/identity_/connect/userinfo";

               //Describe how to map the user info we receive to user claims
               options.ClaimActions.MapJsonKey(ClaimTypes.Country, "country");
               options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
               options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
               options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "last_name");
               options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "first_name");
               options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

               options.SaveTokens = true;


               options.Events = new OAuthEvents
               {
                   OnCreatingTicket = async context =>
                   {
                       //Get user info from the userinfo endpoint and use it to populate user claims
                       var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                       request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                       request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                       var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                       response.EnsureSuccessStatusCode();
                       var data = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                       context.RunClaimActions(data);
                   }
               };
           });
        }
    }
}
