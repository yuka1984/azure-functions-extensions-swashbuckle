using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Extensions.Swashbuckle
{
    public static class SwashBuckleClientExtension
    {
        public static HttpResponseMessage CreateSwaggerDocumentResponse(this ISwashBuckleClient client,
            HttpRequestMessage requestMessage, string documentName = "v1")
        {
            var stream = client.GetSwaggerDocument(requestMessage.RequestUri.Authority, documentName);
            var reader = new StreamReader(stream);
            var document = reader.ReadToEnd();

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = requestMessage;
            response.Content = new StringContent(document, Encoding.UTF8, "application/json");

            return response;
        }

        public static HttpResponseMessage CreateSwaggerUIResponse(this ISwashBuckleClient client,
            HttpRequestMessage requestMessage, string documentRoute)
        {
            string routePrefix = string.IsNullOrEmpty(client.RoutePrefix)
                ? string.Empty
                : $"/{client.RoutePrefix}";

            var stream =
                client.GetSwaggerUi(
                    $"{requestMessage.RequestUri.Scheme}://{requestMessage.RequestUri.Authority.TrimEnd('/')}{routePrefix}/{documentRoute}");
            var reader = new StreamReader(stream);
            var document = reader.ReadToEnd();
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            result.RequestMessage = requestMessage;
            result.Content = new StringContent(document, Encoding.UTF8, "text/html");
            return result;
        }

        public static IActionResult CreateSwaggerDocumentResponse(this ISwashBuckleClient client,
            HttpRequest request, string documentName = "v1")
        {
            var stream = client.GetSwaggerDocument(request.Host.ToString(), documentName);
            var reader = new StreamReader(stream);
            var document = reader.ReadToEnd();

            return new FileContentResult(Encoding.UTF8.GetBytes(document), "application/json");
        }

        public static IActionResult CreateSwaggerUIResponse(this ISwashBuckleClient client,
            HttpRequest request, string documentRoute)
        {
            string routePrefix = string.IsNullOrEmpty(client.RoutePrefix)
                ? string.Empty
                : $"/{client.RoutePrefix}";

            var stream =
                client.GetSwaggerUi(
                    $"{request.Scheme}://{request.Host.ToString().TrimEnd('/')}{routePrefix}/{documentRoute}");
            var reader = new StreamReader(stream);
            var document = reader.ReadToEnd();

            return new FileContentResult(Encoding.UTF8.GetBytes(document), "text/html");
        }
    }
}
