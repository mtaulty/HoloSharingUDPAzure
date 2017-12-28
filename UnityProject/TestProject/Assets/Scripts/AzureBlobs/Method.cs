// All code in this folder is a reworking (stripping down) of the code that Dave wrote
// here: https://github.com/Unity3dAzure/StorageServices
// I just removed the pieces that I didn't need and refactored a few pieces to suit
// what I did need.
//
namespace SharedHolograms.AzureBlobs
{
    using UnityEngine;
    using System.Collections;

    public enum Method
    {
        GET,
        PUT
    }
}