// Unlike the other code in this "AzureBlobs" folder, this is one class that I added
// to bring a few things together and to suit my particular purposes.
//
namespace SharedHolograms.AzureBlobs
{
    using System;
    using System.Collections;
    using System.Net;
    using UnityEngine;
    using System.Collections.Generic;

    public static class AzureBlobStorageHelper
    {
        public static void UploadWorldAnchorBlob(AzureStorageDetails storageDetails, 
            string identifier, byte[] bits, Action<bool,byte[]> callback)
        {
            Debug.Log(string.Format("Uploading {0:G2}MB blob to Azure",
              bits.Length / (1024 * 1024)));

            var headers = new Dictionary<string, string>();

            headers.Add("Content-Type", contentType);

            headers.Add("x-ms-blob-content-disposition", 
                string.Format("attachment; filename=\"{0}\"", identifier));

            headers.Add("x-ms-blob-type", "BlockBlob");

            StorageRequest request = Auth.CreateAuthorizedStorageRequest(
                storageDetails, 
                Method.PUT, 
                storageDetails.GetFullPathForResource(identifier), 
                null, 
                headers, 
                bits.Length);

            request.AddBody(bits, contentType);

            CoRoutineRunner.Instance.StartCoroutine(EnumerateRequest(request, callback));
        }
        public static void DownloadWorldAnchorBlob(AzureStorageDetails storageDetails, 
            string identifier, Action<bool, byte[]> callback)
        {
            Debug.Log("Downloading blob from Azure storage");

            // The blob service doesn't seem to include a method to just download 
            // a blob as a byte[] so doing it here out of the pieces that the
            // service uses internally.
            string url = UrlHelper.BuildQuery(
              storageDetails.PrimaryEndpoint,
              string.Empty,
              storageDetails.GetFullPathForResource(identifier));

            StorageRequest request = new StorageRequest(url, Method.GET);

            CoRoutineRunner.Instance.StartCoroutine(EnumerateRequest(request, callback));
        }
        static IEnumerator EnumerateRequest(StorageRequest request, Action<bool, byte[]> callback)
        {
            yield return request.request.Send();
            request.Result(
              response =>
              {
                  Debug.Log("Transfer completed - succeeded? " + !response.IsError);

                  byte[] bits = null;

                  if (!response.IsError)
                  {
                      bits = request.request.downloadHandler.data;
                  }
                  callback(!response.IsError, bits);
              }
            );
        }
        static readonly string contentType = "application/octet-stream";
    }
}