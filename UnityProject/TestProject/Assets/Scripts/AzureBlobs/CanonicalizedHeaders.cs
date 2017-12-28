// All code in this folder is a reworking (stripping down) of the code that Dave wrote
// here: https://github.com/Unity3dAzure/StorageServices
// I just removed the pieces that I didn't need and refactored a few pieces to suit
// what I did need.
//
namespace SharedHolograms.AzureBlobs
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class CanonicalizedHeaders
    {
        private string xMSDate;

        public string MSDate
        {
            get
            {
                return xMSDate;
            }
        }

        private string xMSVersion;

        public string MSVersion
        {
            get
            {
                return xMSVersion;
            }
        }

        private List<string> canonicalHeaders;

        public CanonicalizedHeaders(string version, Dictionary<string, string> headers = null)
        {
            this.xMSDate = DateTime.UtcNow.ToString("R");
            this.xMSVersion = version;

            canonicalHeaders = new List<string>();
            AddCanonicalHeaderKeyValue("x-ms-date", MSDate);
            AddCanonicalHeaderKeyValue("x-ms-version", MSVersion);

            if (headers == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> header in headers)
            {
                if (header.Key.StartsWith("x-ms", StringComparison.OrdinalIgnoreCase))
                {
                    AddCanonicalHeaderKeyValue(header.Key, header.Value);
                }
            }
        }

        private void AddCanonicalHeaderKeyValue(string key, string value)
        {
            canonicalHeaders.Add(FormatKeyValue(key, value));
        }

        private string FormatKeyValue(string key, string value)
        {
            return string.Format("{0}:{1}", key, value);
        }

        public override string ToString()
        {
            //return string.Format ("x-ms-date:{0}\nx-ms-version:{1}\n", MSDate, MSVersion);
            canonicalHeaders.Sort();
            StringBuilder sb = new StringBuilder();
            foreach (string canonicalHeader in canonicalHeaders)
            {
                sb.Append(canonicalHeader + "\n");
            }
            return sb.ToString();
        }
    }
}
