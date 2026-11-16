using System;
using System.IO;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>
    /// Opens the phone's photo library and returns the chosen image as a texture no larger than
    /// 1024 pixels on its long edge. iOS uses the PHPicker plugin in Plugins/iOS/PaintingPicker.mm,
    /// Android uses the PaintingPicker activity, and the editor uses a file dialog.
    /// The native side answers through UnitySendMessage, so this object keeps a fixed name.
    /// </summary>
    public sealed class PhotoPicker : MonoBehaviour, IPhotoPicker
    {
        public const string ObjectName = "CapstonePhotoPicker";
        const int MaxEdge = 1024;

        static PhotoPicker instance;
        Action<Texture2D> pending;

        public static PhotoPicker Ensure()
        {
            if (instance != null)
                return instance;
            var host = new GameObject(ObjectName);
            DontDestroyOnLoad(host);
            instance = host.AddComponent<PhotoPicker>();
            return instance;
        }

        public void Pick(Action<Texture2D> done)
        {
            pending = done;
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Photo of your piece", "", new[] { "Image files", "png,jpg,jpeg" });
            OnPaintingPicked(path);
#elif UNITY_IOS
            CapstonePickPainting(ObjectName);
#elif UNITY_ANDROID
            OpenAndroidPicker();
#else
            Finish(null);
#endif
        }

        /// <summary>Called by the native picker with a file path, or an empty string on cancel.</summary>
        public void OnPaintingPicked(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Finish(null);
                return;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (IOException)
            {
                Finish(null);
                return;
            }

            var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!loaded.LoadImage(bytes))
            {
                Destroy(loaded);
                Finish(null);
                return;
            }

            Texture2D scaled = Scale(loaded);
            if (scaled != loaded)
                Destroy(loaded);
            Finish(scaled);
        }

        void Finish(Texture2D texture)
        {
            Action<Texture2D> done = pending;
            pending = null;
            done?.Invoke(texture);
        }

        /// <summary>Shrinks a photo so its long edge is at most 1024 pixels, keeping its shape.</summary>
        public static Texture2D Scale(Texture2D source)
        {
            int longEdge = Mathf.Max(source.width, source.height);
            if (longEdge <= MaxEdge)
                return source;

            float scale = MaxEdge / (float)longEdge;
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, target);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return copy;
        }

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void CapstonePickPainting(string gameObjectName);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        void OpenAndroidPicker()
        {
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            {
                string packageName = activity.Call<string>("getPackageName");
                intent.Call<AndroidJavaObject>("setClassName", packageName, "com.cinna03.capstone.picker.PaintingPickActivity");
                intent.Call<AndroidJavaObject>("putExtra", "gameObject", ObjectName);
                activity.Call("startActivity", intent);
            }
        }
#endif
    }
}
