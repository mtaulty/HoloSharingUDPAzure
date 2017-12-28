// All code in this folder is a reworking (stripping down) of the code that Dave wrote
// here: https://github.com/Unity3dAzure/StorageServices
// I just removed the pieces that I didn't need and refactored a few pieces to suit
// what I did need.
//
namespace SharedHolograms.AzureBlobs
{
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using UnityEngine;
    using System.Text;
    using System;

    public static class Auth
    {
        /// <summary>
        /// Factory method to generate an authorized request URL using query params. (valid up to 15 minutes)
        /// </summary>
        /// <returns>The authorized request.</returns>
        /// <param name="storageDetails">StorageServiceClient</param>
        /// <param name="httpMethod">Http method.</param>
        public static StorageRequest CreateAuthorizedStorageRequest(
            AzureStorageDetails storageDetails, Method method, string resourcePath = "", Dictionary<string, string> queryParams = null, Dictionary<string, string> headers = null, int contentLength = 0)
        {
            string baseUrl = storageDetails.PrimaryEndpoint;
            string requestUrl = UrlHelper.BuildQuery(baseUrl, queryParams, resourcePath);
            StorageRequest request = new StorageRequest(requestUrl, method);
            request.AuthorizeRequest(storageDetails, method, resourcePath, queryParams, headers, contentLength);
            return request;
        }


    }
}