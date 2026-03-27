using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MiniOA.Core.Models;
using System.Linq;

namespace MiniOA.Api.Filters
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                context.Result = new JsonResult(ApiResult<object>.Fail(
                    string.Join("; ", errors),
                    400
                ));
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

    }
}