using AccountProvider.Models;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;

namespace AccountProvider.Functions
{
    public class Verify(ILogger<Verify> logger, UserManager<UserAccount> userManager)
    {
        private readonly ILogger<Verify> _logger = logger;
        private readonly UserManager<UserAccount> _userManager = userManager;

        [Function("Verify")]
        public async Task <IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            string body = null!;
            try
            {
                body = await new StreamReader(req.Body).ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"StreamReader :: {ex.Message}");
            }
            if(body != null)
            {
                VerificationsRequest vr = null!;
                try
                {
                    vr = JsonConvert.DeserializeObject<VerificationsRequest>(body)!;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"JsonConvert.DeserializeObject<VerificationsRequest> :: {ex.Message}");
                }
                
                if(vr != null && !string.IsNullOrEmpty(vr.Email) && !string.IsNullOrEmpty(vr.VerificationCode))
                {
                    try
                    {
                        string Vau = Environment.GetEnvironmentVariable("Vau")!;
                        using var http = new HttpClient();
                        StringContent content = new StringContent(JsonConvert.SerializeObject(new {vr}), Encoding.UTF8, "application/json");
                        var response = await http.PostAsync(Vau, content);

                        if (response.IsSuccessStatusCode)
                        {
                            var userAccount = await _userManager.FindByEmailAsync(vr.Email);
                            if (userAccount != null)
                            {
                                userAccount.EmailConfirmed = true;
                                await _userManager.UpdateAsync(userAccount);

                                if (await _userManager.IsEmailConfirmedAsync(userAccount))
                                {
                                    return new OkResult();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"http.PostAsync:: {ex.Message}");
                    }
                    
                   
                }
            }

            return new UnauthorizedResult();
           
        }
    }
}
