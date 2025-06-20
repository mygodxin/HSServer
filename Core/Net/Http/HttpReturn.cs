using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace Core.Net.Http
{
    public struct HttpReturn
    {
        public HttpStatus Status { get; set; }
        public string Message;
        private readonly JsonSerializerOptions options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), };

        public HttpReturn(HttpStatus status, string message = "")
        {
            this.Status = status;
            this.Message = message;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, GetType(), options);
        }

        public static implicit operator string(HttpReturn value)
        {
            return value.ToString();
        }
    }
}
