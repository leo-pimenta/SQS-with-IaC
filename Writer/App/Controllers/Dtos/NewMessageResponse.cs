namespace App.Controllers.Dtos
{
    public class NewMessageResponse
    {
        public string Id { get; }

        public NewMessageResponse(string id)
        {
            Id = id;
        }
    }
}