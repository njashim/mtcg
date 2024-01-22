using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MonsterTradingCardGame
{
    public class ResponseHandler
    {

        private static readonly Dictionary<int, string> StatusCodes = new Dictionary<int, string>
        {
            { 200, "OK" },
            { 201, "Created" },
            { 204, "No Content" },
            { 400, "Bad Request" },
            { 401, "Unauthorized" },
            { 403, "Forbidden" },
            { 404, "Not Found" },
            { 409, "Conflict" }
        };

        public static string GetResponseMessage(int statusCode, string contentType, string customMessage)
        {
            string statusDescription = "";

            if(StatusCodes.ContainsKey(statusCode))
            {
                statusDescription = StatusCodes[statusCode];
            }
            
            return $"HTTP/1.1 {statusCode} {statusDescription}\r\nContent-Type: {contentType}\r\n\r\n{{ \"message\": \"{customMessage}\" }}\r\n";
        }
    }
}
