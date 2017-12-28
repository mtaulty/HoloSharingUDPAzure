// All code in this folder is a reworking (stripping down) of the code that Dave wrote
// here: https://github.com/Unity3dAzure/StorageServices
// I just removed the pieces that I didn't need and refactored a few pieces to suit
// what I did need.
//
namespace SharedHolograms.AzureBlobs
{
    using System.Net;

    public abstract class Response
    {
        public bool IsError { get; set; }

        public string ErrorMessage { get; set; }

        public string Url { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string Content { get; set; }

        protected Response(HttpStatusCode statusCode)
        {
            this.StatusCode = statusCode;
            this.IsError = !((int)statusCode >= 200 && (int)statusCode < 300);
        }

        // success
        protected Response(HttpStatusCode statusCode, string url, string text)
        {
            this.IsError = false;
            this.Url = url;
            this.ErrorMessage = null;
            this.StatusCode = statusCode;
            this.Content = text;
        }

        // failure
        protected Response(string error, HttpStatusCode statusCode, string url, string text)
        {
            this.IsError = true;
            this.Url = url;
            this.ErrorMessage = error;
            this.StatusCode = statusCode;
            this.Content = text;
        }
    }

    public sealed class RestResponse : Response
    {
        // success
        public RestResponse(HttpStatusCode statusCode, string url, string text) : base(statusCode, url, text)
        {
        }

        // failure
        public RestResponse(string error, HttpStatusCode statusCode, string url, string text) : base(error, statusCode, url, text)
        {
        }
    }

    internal sealed class RestResult : Response
    {
        public RestResult(HttpStatusCode statusCode) : base(statusCode)
        {
        }
    }

}
