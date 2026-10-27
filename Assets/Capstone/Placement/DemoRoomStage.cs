using UnityEngine;

namespace Capstone.Placement
{
    /// <summary>
    /// Builds the three editor rooms and points the camera at the chosen wall.
    /// Only the feature wall keeps a collider, so Place hits that wall.
    /// </summary>
    public sealed class DemoRoomStage
    {
        readonly DemoRoom[] rooms;
        readonly GameObject[] roots;
        readonly Vector3[] facePoints;
        int index;

        public DemoRoomStage(DemoRoom[] rooms)
        {
            this.rooms = rooms != null ? rooms : new DemoRoom[0];
            roots = new GameObject[this.rooms.Length];
            facePoints = new Vector3[this.rooms.Length];
        }

        public DemoRoom Current { get { return rooms.Length == 0 ? null : rooms[index]; } }

        public int Index { get { return index; } }

        public void Build(Transform parent, Material source)
        {
            Shader shader = RoomShader(source);
            for (int i = 0; i < rooms.Length; i++)
            {
                DemoRoom room = rooms[i];
                float originX = i * 40f;
                GameObject root = new GameObject(room.Name);
                root.transform.SetParent(parent, false);
                roots[i] = root;
                facePoints[i] = new Vector3(originX, room.HeightMetres * 0.5f, 0f);
                BuildShell(root.transform, room, originX, shader);
                if (i != 0)
                    root.SetActive(false);
            }
        }

        public void Show(int next)
        {
            if (next < 0 || next >= rooms.Length)
                return;
            index = next;
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                    roots[i].SetActive(i == index);
            }
        }

        public Vector3 WallFace()
        {
            return facePoints[index];
        }

        public Vector3 CameraPosition()
        {
            Vector3 face = WallFace();
            return new Vector3(face.x, 1.55f, face.z + Current.ViewDistanceMetres());
        }

        public void Frame(Camera camera)
        {
            if (camera == null || Current == null)
                return;

            Vector3 face = WallFace();
            Vector3 position = CameraPosition();
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(face - position, Vector3.up));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(Current.WallR, Current.WallG, Current.WallB, 1f) * 0.35f;
        }

        public bool TryHit(Camera camera, out Vector3 point, out Vector3 normal)
        {
            point = Vector3.zero;
            normal = Vector3.forward;
            if (camera == null)
                return false;

            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 40f))
                return false;
            if (hit.collider == null || hit.collider.gameObject.name != DemoRoom.WallObjectName)
                return false;

            point = hit.point;
            normal = hit.normal;
            return true;
        }

        static void BuildShell(Transform parent, DemoRoom room, float originX, Shader shader)
        {
            float width = room.WidthMetres;
            float height = room.HeightMetres;
            float depth = room.DepthMetres;
            Material wall = Paint(shader, room.WallR, room.WallG, room.WallB);
            Material floor = Paint(shader, room.FloorR, room.FloorG, room.FloorB);
            Material trim = Paint(shader, room.WallR * 0.72f, room.WallG * 0.72f, room.WallB * 0.72f);

            Box(parent, DemoRoom.WallObjectName, new Vector3(originX, height * 0.5f, -0.08f), new Vector3(width, height, 0.16f), wall, true);
            Box(parent, "Floor", new Vector3(originX, -0.04f, depth * 0.5f), new Vector3(width, 0.08f, depth), floor, false);
            Box(parent, "Ceiling", new Vector3(originX, height, depth * 0.5f), new Vector3(width, 0.08f, depth), trim, false);
            Box(parent, "Left wall", new Vector3(originX - width * 0.5f, height * 0.5f, depth * 0.5f), new Vector3(0.12f, height, depth), trim, false);
            Box(parent, "Right wall", new Vector3(originX + width * 0.5f, height * 0.5f, depth * 0.5f), new Vector3(0.12f, height, depth), trim, false);

            Light lamp = new GameObject("Lamp").AddComponent<Light>();
            lamp.transform.SetParent(parent, false);
            lamp.type = LightType.Spot;
            lamp.intensity = 6f;
            lamp.range = depth + 6f;
            lamp.spotAngle = 120f;
            lamp.color = Color.white;
            Vector3 lampPosition = new Vector3(originX, height - 0.25f, depth * 0.45f);
            lamp.transform.position = lampPosition;
            lamp.transform.rotation = Quaternion.LookRotation(new Vector3(originX, height * 0.45f, 0f) - lampPosition, Vector3.up);
        }

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
            if (!keepCollider)
            {
                Collider collider = box.GetComponent<Collider>();
                if (collider != null)
                    Object.Destroy(collider);
            }

            return box;
        }

        static Material Paint(Shader shader, float r, float g, float b)
        {
            if (shader == null)
                return null;

            Material material = new Material(shader);
            Color color = new Color(r, g, b, 1f);
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.08f);
            return material;
        }

        static Shader RoomShader(Material source)
        {
            if (source != null && source.shader != null)
                return source.shader;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
                return shader;
            return Shader.Find("Standard");
        }
    }
}
