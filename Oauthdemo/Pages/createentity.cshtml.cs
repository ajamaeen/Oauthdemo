using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Oauthdemo.Models;

namespace Oauthdemo.Pages
{
    [Authorize]
    public class CreatEentityModel : PageModel
    {
        private readonly ILogger<CreatEentityModel> _logger;
        private readonly IConfiguration _configuration;

        public CreatEentityModel(ILogger<CreatEentityModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            var httpClient = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri($"https://cloud.uipath.com/{_configuration["OrgName"]}/{_configuration["Tenant"]}/dataservice_/api/EntityService/OAuthDemo/read", System.UriKind.RelativeOrAbsolute)
            };

            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            var token = await HttpContext.GetTokenAsync("access_token");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var respons = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            if (respons.IsSuccessStatusCode)
            {
                ViewData["TotalRecords"] = JsonConvert.DeserializeObject<Result>(await respons.Content.ReadAsStringAsync())?.TotalRecordCount;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var httpClient = new HttpClient();

            var body = new Dictionary<string, object>
            {
                { "Id",Guid.NewGuid().ToString()},
                { "Name", this.Name },
                { "Type", this.Type } ,
                { "CreatedBy", User.FindFirstValue(ClaimTypes.NameIdentifier) },
                { "CreateTime", DateTime.UtcNow.ToUniversalTime()}
            };

            var request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(body, new JsonSerializerSettings())),
                Method = new HttpMethod("POST"),
                RequestUri = new Uri($"https://cloud.uipath.com/{_configuration["OrgName"]}/{_configuration["Tenant"]}/dataservice_/api/EntityService/OAuthDemo/insert", System.UriKind.RelativeOrAbsolute)
            };
            request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await HttpContext.GetTokenAsync("access_token"));

            var token = await HttpContext.GetTokenAsync("access_token");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);


            return RedirectToPage($"./createentity");
        }


        [BindProperty()]
        public string Name { get; set; }


        [BindProperty()]
        public string Type { get; set; }
    }
}
