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
        /// <summary>Two works whose centres are closer than this are treated as the same spot.</summary>
        public const float ReplaceRadius = 0.3f;

        readonly List<GameObject> pinned = new List<GameObject>();

        public int Count => pinned.Count;

        /// <summary>Leaves a fixed copy of the painting where it is now. Returns the copy.</summary>
        public GameObject Pin(Transform painting)
        {
            if (painting == null)
                return null;

            // Locking on top of an earlier work replaces it instead of stacking copies.
            RemoveNear(painting.position, ReplaceRadius);

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

        /// <summary>Takes down any locked work whose centre is within the radius (metres).</summary>
        public int RemoveNear(Vector3 centre, float radius)
        {
            int removed = 0;
            for (int i = pinned.Count - 1; i >= 0; i--)
            {
                GameObject holder = pinned[i];
                if (holder != null && (holder.transform.position - centre).sqrMagnitude > radius * radius)
                    continue;
                if (holder != null)
                    Destroy(holder);
                pinned.RemoveAt(i);
                removed++;
            }

            return removed;
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
