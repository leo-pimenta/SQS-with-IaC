using App.Controllers.Dtos;
using App.Factories;
using App.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers
{
    [ApiController]
    [Route("messages")]
    public class MessageController : Controller
    {
        private IMessageService MessageService;

        public MessageController(IMessageService messageService) 
        {
            MessageService = messageService;
        }

        [HttpPost]
        public async Task<IActionResult> NewMessage(NewMessageRequest dto)
        {
            var message = MessageFactory.Create(dto);
            string id = await MessageService.SendAsync(message);
            return Created("", new NewMessageResponse(id));
        }
    }
}