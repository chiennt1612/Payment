using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Payment.Dtos;
using Payment.ExceptionHandling;
using Payment.Helpers;

namespace Payment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(ControllerExceptionFilterAttribute))]
    [Produces("application/json", "application/problem+json")]
    [Authorize]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        public TestController (ILogger<TestController> _logger)
        {
            this._logger = _logger;
        }

        [Authorize(Roles = "customer")]
        [HttpGet]
        //[AllowAnonymous]
        public async Task<ActionResult<ObjectsDTO>> Get(string searchText, int page = 1, int pageSize = 10)
        {
            await ValidateJwt(Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));
            var clientsDto = new ObjectsDTO();
            var o = BaseController.SetObjAccountBalance(User);
            var o1 = BaseController.SetObjAccountBalanceByCurrency(User);
            var a = BaseController.SetObjInputTransaction(User, 1);

            return Ok(clientsDto);
        }

        //[Authorize(Roles = "User")]
        [HttpGet("{id}")]
        public ActionResult Get()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            _logger.LogInformation("claims: {claims}", claims);

            return new JsonResult(claims);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] ObjectDTO dataStoreCommand)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await Task.Delay(0);//_dataStoreService.PostAsync(dataStoreCommand);
                    return Ok();
                }

                var errorList = ModelState.Values.SelectMany(m => m.Errors).Select(e => e.ErrorMessage).ToList();
                return ValidationProblem();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> PutAsync([FromBody] ObjectDTO dataStoreCommand)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await Task.Delay(0);//_dataStoreService.PutAsync(dataStoreCommand);
                    return Ok();
                }

                var errorList = ModelState.Values.SelectMany(m => m.Errors).Select(e => e.ErrorMessage).ToList();
                return ValidationProblem();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync()
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await Task.Delay(0);
                    //var item = await _dataStoreService.GetByIdAsync(id);
                    //await _dataStoreService.DeleteAsync(item);
                    return Ok();
                }

                var errorList = ModelState.Values.SelectMany(m => m.Errors).Select(e => e.ErrorMessage).ToList();
                return ValidationProblem();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static async Task<ClaimsPrincipal> ValidateJwt(string jwt)
        {
            // read discovery document to find issuer and key material
            var client = new System.Net.Http.HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(Constants.Authority);

            var keys = new List<SecurityKey>();
            foreach (var webKey in disco.KeySet.Keys)
            {
                var key = new JsonWebKey()
                {
                    Kty = webKey.Kty,
                    Alg = webKey.Alg,
                    Kid = webKey.Kid,
                    X = webKey.X,
                    Y = webKey.Y,
                    Crv = webKey.Crv,
                    E = webKey.E,
                    N = webKey.N,
                };
                keys.Add(key);
            }

            var parameters = new TokenValidationParameters
            {
                ValidIssuer = disco.Issuer,
                ValidAudience = "api2",
                ValidateIssuer = true,
                ValidateAudience = false,
                IssuerSigningKeys = keys,

                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role
            };

            var handler = new JwtSecurityTokenHandler();
            handler.InboundClaimTypeMap.Clear();

            var user = handler.ValidateToken(jwt, parameters, out var _);
            //var cuser = user.FindFirst(UserConst.AccountId).Value;
            return user;
        }
    }
}
