using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SproutSling
{
    /// Materials, meshes and primitive helpers shared by the world, the gardener and the plants.
    public static class Kit
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Mesh cube, sphere;
        static Shader lit, unlit, particles;

        public static Mesh Cube { get { if (!cube) Grab(); return cube; } }
        public static Mesh Sphere { get { if (!sphere) Grab(); return sphere; } }

        static void Grab()
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = c.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(c);
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere = s.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(s);
        }

        static Shader LitShader => lit ? lit : (lit = Shader.Find("Universal Render Pipeline/Lit"));
        static Shader UnlitShader => unlit ? unlit : (unlit = Shader.Find("Universal Render Pipeline/Unlit"));
        static Shader ParticleShader => particles ? particles : (particles = Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        static Material Get(string key, System.Func<Material> make)
        {
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = make();
            cache[key] = m;
            return m;
        }

        static string K(Color c) => $"{c.r:0.000},{c.g:0.000},{c.b:0.000},{c.a:0.000}";

        public static Material Lit(Color c, float smooth = 0.1f) => Get("L" + K(c) + smooth, () =>
        {
            var m = new Material(LitShader);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            return m;
        });

        /// Emissive: base colour plus glow, blooms with the volume.
        public static Material Glow(Color c, float intensity = 2.5f, Color? baseColor = null) => Get("G" + K(c) + intensity + (baseColor.HasValue ? K(baseColor.Value) : ""), () =>
        {
            var m = new Material(LitShader);
            m.SetColor("_BaseColor", baseColor ?? c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        });

        public static Material Unlit(Color c) => Get("U" + K(c), () =>
        {
            var m = new Material(UnlitShader);
            m.SetColor("_BaseColor", c);
            return m;
        });

        /// Additive transparent particle material (trails, sparkles, halos).
        /// Unlit, front faces culled: an inverted hull that draws as an outline.
        public static Material Outline(Color c) => Get("O" + K(c), () =>
        {
            var m = new Material(UnlitShader);
            m.SetColor("_BaseColor", c * 1.6f);
            m.SetFloat("_Cull", (float)CullMode.Front);
            return m;
        });

        public static Material Additive(Color c) => Get("A" + K(c), () =>
        {
            var m = new Material(ParticleShader);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_BLENDMODE_ADD");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        });

        public static Material ParticleOpaque(Color c) => Get("P" + K(c), () =>
        {
            var m = new Material(ParticleShader);
            m.SetColor("_BaseColor", c);
            return m;
        });

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Material mat, bool shadows = true, Vector3? euler = null)
            => Prim(Cube, "Box", parent, pos, size, mat, shadows, euler);

        public static GameObject Ball(Transform parent, Vector3 pos, Vector3 size, Material mat, bool shadows = true)
            => Prim(Sphere, "Ball", parent, pos, size, mat, shadows, null);

        static GameObject Prim(Mesh mesh, string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool shadows, Vector3? euler)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = size;
            if (euler.HasValue) g.transform.localRotation = Quaternion.Euler(euler.Value);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return g;
        }

        public static Transform Node(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        /// A ring of cubes, used for luck rings and halos.
        public static Transform Ring(string name, Transform parent, Vector3 pos, float radius, int count, float cubeSize, Material mat)
        {
            var root = Node(name, parent, pos);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                Box(root, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f), Vector3.one * cubeSize, mat, false, new Vector3(0, 0, a * Mathf.Rad2Deg));
            }
            return root;
        }

        static readonly string[] Suffix = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        public static string Short(double v)
        {
            if (v < 1000) return v < 10 ? v.ToString("0.##") : v.ToString("0");
            int i = 0;
            while (v >= 1000 && i < Suffix.Length - 1) { v /= 1000; i++; }
            return (v < 10 ? v.ToString("0.##") : v < 100 ? v.ToString("0.#") : v.ToString("0")) + Suffix[i];
        }

        public static string Full(double v) => v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }

    public class Spinner : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0, 90, 0);
        void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
    }

    public class Bobber : MonoBehaviour
    {
        public float amplitude = 0.4f, speed = 1.5f, phase;
        Vector3 start;
        void Start() { start = transform.localPosition; phase += Random.value * 6f; }
        void Update() => transform.localPosition = start + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amplitude;
    }
}
