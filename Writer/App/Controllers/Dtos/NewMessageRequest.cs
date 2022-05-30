using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace App.Controllers.Dtos
{
    public class NewMessageRequest
    {
        [Required]
        [MaxLength(500)]
        [JsonPropertyName("body")]
        public string Body { get; set; }
    }
}