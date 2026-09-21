using MediatR;
using Microsoft.AspNetCore.Mvc;
using Work_Service.Application.ProjectContext;
using Work_Service.Application.ProjectUser;

namespace Work_Service.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ProjectController:Controller

    {
        private readonly IMediator mediator;
        public ProjectController(IMediator mediator)
        {
            this.mediator = mediator;
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] ProjectCreateCommand command)
        {
            await mediator.Send(command);
            return Ok("All good");
        }
        [HttpPost]
        public async Task<IActionResult> AssignUser([FromBody] ProjectUserCommand command)
        {
            var result= await mediator.Send(command);
            if (result.Error)
            {
                return StatusCode(result.StatusCode, new
                {
                    errorMessage = result.ErrorMessage,
                });
            }
            return StatusCode(result.StatusCode, new
            {
                
                message="ProjectUser was created"
            });
        }
        
    }
}
