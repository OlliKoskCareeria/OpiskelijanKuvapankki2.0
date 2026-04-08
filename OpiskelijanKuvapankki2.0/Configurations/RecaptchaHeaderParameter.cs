using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OpiskelijanKuvapankki2_0.Configurations
{
    public class RecaptchaHeaderParameter : IOperationFilter
    {
        

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
            
            

            var actionDescriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor?.ActionName == "AddNew" &&
                actionDescriptor.ControllerName == "Logins")
            {
                operation.Parameters ??= new List<OpenApiParameter>();
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-Recaptcha-Token",
                    In = ParameterLocation.Header,
                    Description = "reCAPTCHA token",
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" }
                });
            }
            
        }
        

    }
}
