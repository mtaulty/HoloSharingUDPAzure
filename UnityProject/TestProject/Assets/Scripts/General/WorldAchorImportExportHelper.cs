namespace SharedHolograms
{
    using System.IO;
    using UnityEngine;
    using UnityEngine.XR.WSA;
    using UnityEngine.XR.WSA.Sharing;
    using System.Linq;
    using System;

#if !UNITY_EDITOR
    using System.Threading.Tasks;

    public static class WorldAnchorImportExportHelper
    {
        public static async void ImportWorldAnchorToGameObject(
            GameObject gameObject,
            byte[] worldAnchorBits,
            Action<bool> callback)
        {
            var worked = await ImportWorldAnchorToGameObjectAsync(gameObject, worldAnchorBits);

            if (callback != null)
            {
                callback(worked);
            }
        }
        public static async Task<bool> ImportWorldAnchorToGameObjectAsync(
            GameObject gameObject,
            byte[] worldAnchorBits)
        {
            Debug.Log("Starting...");
            var completion = new TaskCompletionSource<bool>();
            bool worked = false;

            Debug.Log("Importing spatial anchor...");

            WorldAnchorTransferBatch.ImportAsync(worldAnchorBits,
              (reason, batch) =>
              {
                  Debug.Log("Import completed - succeeded? " +
                    (reason == SerializationCompletionReason.Succeeded));

                  if (reason == SerializationCompletionReason.Succeeded)
                  {
                      Debug.Log("Attempting to look into world anchor batch");

                      var anchorId = batch.GetAllIds().FirstOrDefault();

                      Debug.Log("Anchor id found? " + (anchorId != null));

                      if (!string.IsNullOrEmpty(anchorId))
                      {
                          Debug.Log("Locking world anchor");

                          batch.LockObject(anchorId, gameObject);
                          worked = true;
                      }
                  }
                  batch.Dispose();
                  completion.SetResult(true);
              }
            );
            await completion.Task;

            return (worked);
        }
        public static async void ExportWorldAnchorFromGameObject(
            GameObject gameObject,
            Action<byte[]> callback)
        {
            var bits = await ExportWorldAnchorForGameObjectAsync(gameObject);
            callback(bits);
        }
        static async Task<byte[]> ExportWorldAnchorForGameObjectAsync(
          GameObject gameObject)
        {
            byte[] bits = null;

            var worldAnchor = gameObject.GetComponent<WorldAnchor>();

            await UpdateCheckPredicate.WaitForPredicateAsync(
              gameObject,
              () => worldAnchor.isLocated);

            using (var worldAnchorBatch = new WorldAnchorTransferBatch())
            {
                worldAnchorBatch.AddWorldAnchor("anchor", worldAnchor);

                var completion = new TaskCompletionSource<bool>();

                using (var memoryStream = new MemoryStream())
                {
                    Debug.Log("Exporting world anchor...");

                    WorldAnchorTransferBatch.ExportAsync(
                      worldAnchorBatch,
                      data =>
                      {
                          memoryStream.Write(data, 0, data.Length);
                      },
                      reason =>
                      {
                          Debug.Log("Export completed - succeeded? " +
                            (reason == SerializationCompletionReason.Succeeded));

                          if (reason != SerializationCompletionReason.Succeeded)
                          {
                              bits = null;
                          }
                          else
                          {
                              bits = memoryStream.ToArray();
                          }
                          completion.SetResult(bits != null);
                      }
                    );
                    await completion.Task;
                }
            }
            return (bits);
        }
    }
#endif
}
