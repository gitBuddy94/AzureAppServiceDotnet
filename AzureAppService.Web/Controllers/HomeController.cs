using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory? _httpClientFactory;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Gets a secret from the Azure Key Vault
        /// </summary>
        /// <param name="userAssignedClientId">Optional parameter if you want to get a token for a user assigned managed identity 
        /// using the client id of the user assigned managed identity</param>
        /// <param name="userAssignedResourceId">Optional parameter if you want to get a token for a user assigned managed identity 
        /// using the resource id of the user assigned managed identity</param>
        /// <returns></returns>
        public async Task<ActionResult> GetSecret([FromQuery(Name = "userAssignedClientId")] string? userAssignedClientId = null,
            [FromQuery(Name = "userAssignedResourceId")] string? userAssignedResourceId = null)
        {
            try
            {
                string resource = "https://vault.azure.net";
                var kvUri = _configuration.GetValue<string>("KeyVaultUrl");
                var secretName = "test";

                //Get a managed identity token using Microsoft Identity Client
                IManagedIdentityApplication mi = CreateManagedIdentityApplication(userAssignedClientId, userAssignedResourceId);
                var result = await mi.AcquireTokenForManagedIdentity(resource).ExecuteAsync().ConfigureAwait(false);
                var accessToken = result.AccessToken;

                //create an HttpClient using IHttpClientFactory
                HttpClient httpClient = _httpClientFactory.CreateClient();

                //Use the access token to read secrets from the key vault 
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var response = await httpClient.GetAsync($"{kvUri}/secrets/{secretName}?api-version=7.2");
                var secretValue = await response.Content.ReadAsStringAsync();

                ViewBag.Message = secretValue;
                return View();
            }
            catch (MsalException ex)
            {
                ViewBag.Title = "MsalException Thrown!!!";
                ViewBag.Error = "MsalException";
                ViewBag.Message = ex.Message;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Title = "Exception Thrown!!!";
                ViewBag.Error = "Exception";
                ViewBag.Message = ex.Message;
                return View();
            }
        }

        private static IManagedIdentityApplication CreateManagedIdentityApplication(string? userAssignedClientId, string? userAssignedResourceId)
        {
            if (!string.IsNullOrEmpty(userAssignedClientId)) // Create managed identity application using user assigned client id.
            {
                return ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId))
                    .Build();
            }
            else if (!string.IsNullOrEmpty(userAssignedResourceId)) // Create managed identity application using user assigned resource id.
            {
                return ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.WithUserAssignedResourceId(userAssignedResourceId))
                    .Build();
            }
            else // Create managed identity application using system assigned managed identity.
            {
                return ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .Build();
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
