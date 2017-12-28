﻿// All code in this folder is a reworking (stripping down) of the code that Dave wrote
// here: https://github.com/Unity3dAzure/StorageServices
// I just removed the pieces that I didn't need and refactored a few pieces to suit
// what I did need.
namespace SharedHolograms.AzureBlobs
{
    using System;
    using System.Text;
    using System.Collections.Generic;

    public class UrlHelper
    {
        /// <summary>
        /// The returned url format will be: baseUrl + path(s) + query string
        /// </summary>
        /// <returns>The URL.</returns>
        /// <param name="baseUrl">Base URL.</param>
        /// <param name="queryParams">Query parameters.</param>
        /// <param name="paths">Paths.</param>
        public static string BuildQuery(string baseUrl, Dictionary<string, string> queryParams = null, params string[] paths)
        {
            StringBuilder q = new StringBuilder();
            if (queryParams == null)
            {
                return BuildQuery(baseUrl, "", paths);
            }

            foreach (KeyValuePair<string, string> param in queryParams)
            {
                if (q.Length == 0)
                {
                    q.Append("?");
                }
                else
                {
                    q.Append("&");
                }
                q.Append(param.Key + "=" + param.Value);
            }

            return BuildQuery(baseUrl, q.ToString(), paths);
        }

        public static string BuildQuery(string baseUrl, string queryString, params string[] paths)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(baseUrl);
            if (!baseUrl.EndsWith("/"))
            {
                sb.Append("/");
            }

            foreach (string path in paths)
            {
                if (!path.EndsWith("/"))
                {
                    sb.Append(path);
                }
                else
                {
                    sb.Append(path + "/");
                }
            }

            sb.Append(queryString);
            return sb.ToString();
        }
    }
}
