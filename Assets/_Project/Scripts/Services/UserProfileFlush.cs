using Capstone.App.Data;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// Writes the user list, hearts, and the per-code JSON profile when the phone goes to the background.
    /// iOS often skips OnApplicationQuit, so the pause path is the one that matters.
    /// </summary>
    public sealed class UserProfileFlush : MonoBehaviour
    {
        static UserProfileFlush live;

        public static void Ensure()
        {
            if (live != null)
                return;

            var host = new GameObject("User profile flush");
            DontDestroyOnLoad(host);
            live = host.AddComponent<UserProfileFlush>();
        }

        void OnDestroy()
        {
            if (live == this)
                live = null;
        }

        void OnApplicationPause(bool pause)
        {
            if (pause)
                Save();
        }

        void OnApplicationQuit()
        {
            Save();
        }

        static void Save()
        {
            try
            {
                UserProfileTracker.Flush();
                CatalogueShelf shelf = Resources.Load<CatalogueShelf>("CatalogueShelf");
                if (shelf != null && shelf.Session != null)
                    shelf.Session.FlushSaved();
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("The visit could not be stored before the app closed. " + error.Message);
            }
        }
    }
}
