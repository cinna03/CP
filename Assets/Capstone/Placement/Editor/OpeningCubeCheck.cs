using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Capstone.Placement
{
    [InitializeOnLoad]
    static class OpeningCubeCheck
    {
        const string Flag = "Capstone.OpeningCubeCheck";
        static bool skipped;
        static double started;
        static int shots;

        static OpeningCubeCheck()
        {
            if (SessionState.GetBool(Flag, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Check Opening Cube")]
        static void Run()
        {
            skipped = false;
            shots = 0;
            SessionState.SetBool(Flag, true);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = !EditorApplication.isPlaying;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Flag, false))
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (EditorApplication.isCompiling)
                return;

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
                return;
            }

            if (!skipped)
            {
                Type instructions = Type.GetType("Capstone.Placement.Instructions, Capstone.Placement");
                UnityEngine.Object found = instructions == null ? null : UnityEngine.Object.FindAnyObjectByType(instructions);
                if (found == null)
                    return;

                instructions.GetMethod("Finish", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(found, null);
                skipped = true;
                started = EditorApplication.timeSinceStartup;
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - started;
            Transform cube = GameObject.Find("Rotating cube") != null
                ? GameObject.Find("Rotating cube").transform
                : null;
            float yaw = cube == null ? 0f : cube.eulerAngles.y;
            float[] marks = { 0.4f, 1.6f, 3.2f, 4.8f, 6.4f, 8.2f };
            if (shots < marks.Length && elapsed >= marks[shots])
            {
                SaveFrame(shots, elapsed, yaw);
                shots++;
            }

            if (shots >= marks.Length || elapsed > 12f)
            {
                SessionState.SetBool(Flag, false);
                EditorApplication.update -= Tick;
                EditorApplication.isPlaying = false;
            }
        }

        static void SaveFrame(int index, double elapsed, float yaw)
        {
            Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Camera stage = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].name == "Cube camera")
                    stage = cameras[i];
            }

            string note = "/tmp/cube-" + index + ".txt";
            if (stage == null || stage.targetTexture == null)
            {
                File.WriteAllText(note, "no camera at " + elapsed);
                return;
            }

            RenderTexture texture = stage.targetTexture;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            Texture2D shot = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes("/tmp/cube-" + index + ".png", shot.EncodeToPNG());
            UnityEngine.Object.Destroy(shot);
            GameObject glass = GameObject.Find("Glass body");
            string glassNote = glass == null ? "no glass" : "glass active " + glass.activeInHierarchy + " scale " + glass.transform.localScale.x;
            File.WriteAllText(note, "saved " + elapsed + " yaw " + yaw + " rt " + texture.width + "x" + texture.height + " " + glassNote);
        }
    }
}
