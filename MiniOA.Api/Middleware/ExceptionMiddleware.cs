using System.Text.Json;
using MiniOA.Core.Models;

namespace MiniOA.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
            => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;

                var response = ApiResult<string>.Fail(ex.Message);
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}
