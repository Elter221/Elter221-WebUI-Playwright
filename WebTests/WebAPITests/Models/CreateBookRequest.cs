using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebAPITests.Models
{
    public class CreateBookRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("isbn")]
        public string ISBN { get; set; } = string.Empty;

        [JsonPropertyName("publishedDate")]
        public DateTime PublishedDate { get; set; }
    }
}
