// All code in this folder is a reworking (stripping down) of the code that Dave wrote
// here: https://github.com/Unity3dAzure/StorageServices
// I just removed the pieces that I didn't need and refactored a few pieces to suit
// what I did need.
//
namespace SharedHolograms.AzureBlobs
{
    using UnityEngine;
    using UnityEngine.Networking;
    using System;
    using System.Net;
    using System.Collections.Generic;
    using System.Text;

    public abstract class RestRequest : IDisposable
    {
        public UnityWebRequest request { get; private set; }

        public RestRequest(string url, Method method)
        {
            request = new UnityWebRequest(url, method.ToString());
            request.downloadHandler = new DownloadHandlerBuffer();
        }

        public void AddHeader(string key, string value)
        {
            request.SetRequestHeader(key, value);
        }

        public void AddHeaders(Dictionary<string, string> headers)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                AddHeader(header.Key, header.Value);
            }
        }

        public void AddBody(byte[] bytes, string contentType)
        {
            if (request.uploadHandler != null)
            {
                Debug.LogWarning("Request body can only be set once");
                return;
            }
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.uploadHandler.contentType = contentType;
        }

        public void AddBody(string text, string contentType = "text/plain; charset=UTF-8")
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            this.AddBody(bytes, contentType);
        }

        public virtual void AddBody<T>(T data, string contentType = "application/json; charset=utf-8") where T : new()
        {
            string jsonString = JsonUtility.ToJson(data);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
            this.AddBody(bytes, contentType);
        }

        private RestResult GetRestResult(bool expectedBodyContent = true)
        {
            HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), request.responseCode.ToString());
            RestResult result = new RestResult(statusCode);

            if (result.IsError)
            {
                result.ErrorMessage = "Response failed with status: " + statusCode.ToString();
                return result;
            }

            if (expectedBodyContent && string.IsNullOrEmpty(request.downloadHandler.text))
            {
                result.IsError = true;
                result.ErrorMessage = "Response has empty body";
                return result;
            }

            return result;
        }

        /// <summary>
        /// To be used with a callback which passes the response with result including status success or error code, request url and any body text.
        /// </summary>
        /// <param name="callback">Callback.</param>
        public void Result(Action<RestResponse> callback = null)
        {
            RestResult result = GetRestResult(false);
            if (result.IsError)
            {
                Debug.LogWarning("Response error status:" + result.StatusCode + " code:" + request.responseCode + " error:" + result.ErrorMessage + " request url:" + request.url);
                callback(new RestResponse(result.ErrorMessage, result.StatusCode, request.url, request.downloadHandler.text));
            }
            else
            {
                callback(new RestResponse(result.StatusCode, request.url, request.downloadHandler.text));
            }
        }


        public void Dispose()
        {
            request.Dispose(); // request completed, clean-up resources
        }

    }
}