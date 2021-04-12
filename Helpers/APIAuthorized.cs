using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Payment.Helpers
{
    public class APIAuthorized : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!CheckAccountRequest(context))
            {
                context.Result = new JsonResult(new { HttpStatusCode.Unauthorized });
            }
            else
            {
                // next() calls the action method.  
                var resultContext = await next();
            }
        }
        private bool CheckAccountRequest(ActionExecutingContext context)
        {
            if (context.HttpContext.User != null)
            {
                string oldid = (context.HttpContext.User.FindFirst(UserConst.ConfirmId)?.Value ?? Guid.NewGuid().ToString()).Trim().Replace("-","");
                //string id = context.RouteData?.Values["ConfirmId"]?.ToString().Trim().Replace("-", "");
                string id = context.HttpContext.Request.Query["ConfirmId"].ToString().Trim().Replace("-", "");
                if (id != oldid) return false;
                return true;
            }
            return false;
        }
    }
}
