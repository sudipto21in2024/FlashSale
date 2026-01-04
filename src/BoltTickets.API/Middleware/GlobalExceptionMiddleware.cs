using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BoltTickets.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "Internal Server Error",
            Detailed = exception.Message // In prod, hide this or use logger
        };

        if (exception is InvalidOperationException) // Domain/Validation errors
        {
             context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
             response = new { StatusCode = (int)HttpStatusCode.BadRequest, Message = exception.Message, Detailed = "" };
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
