using EducationalPlataform.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;


namespace EducationalPlataform.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
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

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            ErrorResponse response;
            int statusCode;

            switch (ex)
            {
                case ArgumentException:
                    statusCode = StatusCodes.Status400BadRequest;
                    response = new ErrorResponse(statusCode, "Invalid request data: ", ex.Message);
                    break;

                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    response = new ErrorResponse(statusCode, "Unauthorized access.", ex.Message);
                    break;

                case InvalidOperationException:
                    statusCode = StatusCodes.Status502BadGateway;
                    response = new ErrorResponse(statusCode, ex.Message, "Falha ao falar com o serviço de pagamento.");
                    break;

                case DbUpdateException:
                    statusCode = StatusCodes.Status500InternalServerError;
                    response = new ErrorResponse(
                        statusCode,
                        PostgresDateTimes.ForClient(ex),
                        "Não foi possível gravar no banco.");
                    break;

                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    response = new ErrorResponse(
                        statusCode,
                        PostgresDateTimes.ForClient(ex),
                        ex.Message);
                    break;

            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsJsonAsync(response);
        }

    }
}
