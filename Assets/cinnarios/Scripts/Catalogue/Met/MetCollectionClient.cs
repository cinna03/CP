using System.Threading;
using System.Threading.Tasks;
using Cinnarios.Catalogue.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace Cinnarios.Catalogue.Met
{
    /// <summary>
    /// Talks to The Met Collection API (no key needed). It only fetches and reads JSON;
    /// deciding what to keep and how to translate it is done elsewhere.
    /// </summary>
    public sealed class MetCollectionClient
    {
        private const string BaseUrl = "https://collectionapi.metmuseum.org/public/collection/v1/";

        /// <returns>The object, or null if it could not be fetched (the reason is logged).</returns>
        public async Task<MetObjectData> GetObjectAsync(int objectId, CancellationToken cancellationToken = default)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{BaseUrl}objects/{objectId}"))
            {
                await WebRequestAsync.SendAsync(request, cancellationToken);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Met object {objectId} could not be loaded: {request.error}");
                    return null;
                }

                return JsonUtility.FromJson<MetObjectData>(request.downloadHandler.text);
            }
        }
    }
}
