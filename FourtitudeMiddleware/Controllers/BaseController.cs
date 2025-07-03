using Microsoft.AspNetCore.Mvc;
using FourtitudeMiddleware.Dtos;

namespace FourtitudeMiddleware.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected ActionResult<T> HandleResponse<T>(T response)
            where T : class
        {
            var resultProp = typeof(T).GetProperty("Result");
            var messageProp = typeof(T).GetProperty("ResultMessage");
            var resultValue = resultProp?.GetValue(response);
            bool isSuccess = resultValue is int r && r == 1;

            if (isSuccess)
            {
                if (messageProp != null)
                {
                    messageProp.SetValue(response, null);
                }
                return StatusCode(200, response);
            }
            else
            {
                if (resultProp != null)
                {
                    resultProp.SetValue(response, 0);
                }
                if (messageProp != null && messageProp.GetValue(response) == null)
                {
                    messageProp.SetValue(response, "An error occurred.");
                }
                return StatusCode(400, response);
            }
        }
    }
} 