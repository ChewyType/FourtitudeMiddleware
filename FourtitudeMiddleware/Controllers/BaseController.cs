using Microsoft.AspNetCore.Mvc;
using FourtitudeMiddleware.Dtos;

namespace FourtitudeMiddleware.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected ActionResult<T> SuccessResponse<T>(T response)
            where T : class
        {
            var resultProp = typeof(T).GetProperty("Result");
            if (resultProp != null)
            {
                resultProp.SetValue(response, 1);
            }
            return Ok(response);
        }

        protected ActionResult<T> ErrorResponse<T>(string message, int statusCode = 400)
            where T : class, new()
        {
            var response = new T();
            var resultProp = typeof(T).GetProperty("Result");
            var messageProp = typeof(T).GetProperty("ResultMessage");

            if (resultProp != null)
            {
                resultProp.SetValue(response, 0);
            }
            if (messageProp != null)
            {
                messageProp.SetValue(response, message);
            }

            return StatusCode(statusCode, response);
        }

        protected ActionResult<ServiceResponse<T>> HandleServiceResponse<T>(ServiceResponse<T> serviceResponse)
        {
            if (serviceResponse.Result == 1)
            {
                return SuccessResponse(serviceResponse);
            }
            else
            {
                return ErrorResponse<ServiceResponse<T>>(serviceResponse.ResultMessage);
            }
        }
    }
} 