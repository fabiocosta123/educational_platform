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
                Console.Error.WriteLine(ex.ToString());
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
                    response = new ErrorResponse(statusCode, "Dados inválidos. Revise as informações e tente de novo.");
                    break;

                case UnauthorizedAccessException:
                    statusCode = StatusCodes.Status401Unauthorized;
                    response = new ErrorResponse(statusCode, "Você precisa entrar novamente para continuar.");
                    break;

                case InvalidOperationException:
                    statusCode = StatusCodes.Status502BadGateway;
                    response = new ErrorResponse(
                        statusCode,
                        string.IsNullOrWhiteSpace(ex.Message)
                            ? "Não foi possível concluir esta operação agora. Tente de novo em alguns instantes."
                            : ex.Message);
                    break;

                case DbUpdateException:
                    statusCode = StatusCodes.Status500InternalServerError;
                    response = new ErrorResponse(
                        statusCode,
                        "Não foi possível salvar agora. Tente de novo em alguns instantes.");
                    break;

                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    response = new ErrorResponse(
                        statusCode,
                        "Não foi possível concluir esta operação agora. Tente de novo em alguns instantes.");
                    break;

            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsJsonAsync(response);
        }

    }
}
