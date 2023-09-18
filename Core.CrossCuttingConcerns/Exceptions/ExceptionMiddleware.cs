using Core.CrossCuttingConcerns.Exceptions.Handlers;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Exceptions
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HttpExceptionHandler _exceptionHandler;

        public ExceptionMiddleware(RequestDelegate next, HttpExceptionHandler httpExceptionHandler)
        {
            _next = next;
            _exceptionHandler = httpExceptionHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                 await HandleExceptionAsync(context.Response, exception);
            }

        }


        private Task HandleExceptionAsync(HttpResponse response, Exception exception)
        {
            response.ContentType = "application/json";
            _exceptionHandler.Response = response;
            return _exceptionHandler.HandleExceptionAsync(exception);
        }
    }

}
