using App.Controllers.Dtos;
using Domain;

namespace App.Factories
{
    internal class MessageFactory
    {
        internal static Message Create(NewMessageRequest dto) => new Message(dto.Body);
    }
}