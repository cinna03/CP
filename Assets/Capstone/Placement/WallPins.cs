using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Capstone.Placement
{
    /// <summary>
    /// Works the buyer has locked on the wall. Locking copies the live painting, gives the copy its own
    /// materials and parents it to an AR anchor, so it stays put while the buyer walks around and
    /// several works can hang side by side. Clear() takes them all down (Reset Wall, end of visit).
    /// </summary>
    public sealed class WallPins : MonoBehaviour
    {
        readonly List<GameObject> pinned = new List<GameObject>();

        public int Count => pinned.Count;

        /// <summary>Leaves a fixed copy of the painting where it is now. Returns the copy.</summary>
        public GameObject Pin(Transform painting)
        {
            if (painting == null)
                return null;

            var holder = new GameObject("Locked work " + (pinned.Count + 1));
            holder.transform.SetPositionAndRotation(painting.position, painting.rotation);
            if (!Application.isEditor)
                holder.AddComponent<ARAnchor>();

            GameObject copy = Instantiate(painting.gameObject, painting.position, painting.rotation);
            copy.name = "Painting";
            copy.transform.localScale = painting.lossyScale;
            copy.transform.SetParent(holder.transform, true);
            copy.SetActive(true);

            PlacementSettle settle = copy.GetComponent<PlacementSettle>();
            if (settle != null)
                Destroy(settle);

            // The copy must not share materials with the live painting, or the next work would repaint it.
            Renderer[] drawn = copy.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < drawn.Length; i++)
            {
                if (drawn[i].sharedMaterial != null)
                    drawn[i].sharedMaterial = new Material(drawn[i].sharedMaterial);
            }

            RoomLight.Register(copy.GetComponent<Renderer>());
            pinned.Add(holder);
            return copy;
        }

        public void Clear()
        {
            for (int i = 0; i < pinned.Count; i++)
            {
                if (pinned[i] != null)
                    Destroy(pinned[i]);
            }

            pinned.Clear();
        }

        void OnDestroy()
        {
            Clear();
        }
    }
}
