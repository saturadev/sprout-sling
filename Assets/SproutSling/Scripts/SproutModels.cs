using UnityEngine;

namespace SproutSling
{
    public struct Rarity
    {
        public string title, plant;
        public Color color;
        public double below; // odds under this number fall in this tier
    }

    /// The gardener, the seed and the six plants. All primitives, all original.
    public static class Models
    {
        public static readonly Rarity[] Tiers =
        {
            new Rarity { title = "COMMON", plant = "Pebble Sprout", color = new Color(0.9f, 0.92f, 0.95f), below = 1e3 },
            new Rarity { title = "RARE", plant = "Puff Bush", color = new Color(0.35f, 0.7f, 1f), below = 1e6 },
            new Rarity { title = "EPIC", plant = "Crystal Fern", color = new Color(0.75f, 0.4f, 1f), below = 1e9 },
            new Rarity { title = "LEGENDARY", plant = "Lantern Willow", color = new Color(1f, 0.65f, 0.15f), below = 1e12 },
            new Rarity { title = "MYTHIC", plant = "Halo Oak", color = new Color(1f, 0.3f, 0.45f), below = 1e15 },
            new Rarity { title = "SECRET!!", plant = "Starseed Colossus", color = new Color(0.45f, 1f, 0.35f), below = double.MaxValue },
        };

        public static int TierOf(double odds)
        {
            for (int i = 0; i < Tiers.Length; i++) if (odds < Tiers[i].below) return i;
            return Tiers.Length - 1;
        }

        public class Gardener
        {
            public Transform root, armPivot, hand, body;
        }

        public static Gardener BuildGardener(Transform parent, Vector3 pos)
        {
            var g = new Gardener();
            g.root = Kit.Node("Gardener", parent, pos);
            g.body = Kit.Node("Body", g.root, Vector3.zero);
            var b = g.body;
            var boots = Kit.Lit(new Color(0.32f, 0.2f, 0.12f));
            var overalls = Kit.Lit(new Color(0.2f, 0.45f, 0.78f));
            var shirt = Kit.Lit(new Color(0.95f, 0.4f, 0.3f));
            var skin = Kit.Lit(new Color(1f, 0.8f, 0.62f));
            var straw = Kit.Lit(new Color(0.98f, 0.82f, 0.4f));
            var leaf = Kit.Lit(new Color(0.35f, 0.85f, 0.3f));
            Kit.Box(b, new Vector3(-0.2f, 0.12f, 0.04f), new Vector3(0.32f, 0.24f, 0.44f), boots);
            Kit.Box(b, new Vector3(0.2f, 0.12f, 0.04f), new Vector3(0.32f, 0.24f, 0.44f), boots);
            Kit.Box(b, new Vector3(-0.2f, 0.5f, 0f), new Vector3(0.3f, 0.55f, 0.3f), overalls);
            Kit.Box(b, new Vector3(0.2f, 0.5f, 0f), new Vector3(0.3f, 0.55f, 0.3f), overalls);
            Kit.Box(b, new Vector3(0, 1.08f, 0), new Vector3(0.78f, 0.7f, 0.46f), overalls);
            Kit.Box(b, new Vector3(0, 1.36f, 0), new Vector3(0.8f, 0.2f, 0.48f), shirt);
            // Seed bag on the back, seen by the camera.
            Kit.Box(b, new Vector3(0, 1.1f, -0.36f), new Vector3(0.6f, 0.62f, 0.3f), Kit.Lit(new Color(0.6f, 0.42f, 0.22f)));
            Kit.Box(b, new Vector3(0, 1.44f, -0.36f), new Vector3(0.5f, 0.1f, 0.26f), Kit.Lit(new Color(0.45f, 0.3f, 0.15f)));
            Kit.Box(b, new Vector3(0.12f, 1.52f, -0.36f), new Vector3(0.14f, 0.14f, 0.14f), Kit.Glow(new Color(1f, 0.75f, 0.3f), 1.5f), false);
            // Head and hat.
            Kit.Box(b, new Vector3(0, 1.82f, 0), new Vector3(0.7f, 0.64f, 0.64f), skin);
            Kit.Box(b, new Vector3(0, 1.62f, -0.2f), new Vector3(0.74f, 0.3f, 0.3f), Kit.Lit(new Color(0.55f, 0.32f, 0.18f)));
            Kit.Box(b, new Vector3(0, 2.18f, 0), new Vector3(1.3f, 0.1f, 1.3f), straw);
            Kit.Box(b, new Vector3(0, 2.36f, 0), new Vector3(0.72f, 0.32f, 0.72f), straw);
            Kit.Box(b, new Vector3(0, 2.25f, 0), new Vector3(0.74f, 0.1f, 0.74f), shirt);
            Kit.Box(b, new Vector3(0, 2.62f, 0), new Vector3(0.08f, 0.24f, 0.08f), leaf);
            Kit.Box(b, new Vector3(0.14f, 2.72f, 0), new Vector3(0.26f, 0.06f, 0.16f), leaf, true, new Vector3(0, 0, 25));
            Kit.Box(b, new Vector3(-0.14f, 2.72f, 0), new Vector3(0.26f, 0.06f, 0.16f), leaf, true, new Vector3(0, 0, -25));
            // Left arm static, right arm on a shoulder pivot.
            Kit.Box(b, new Vector3(-0.5f, 1.1f, 0), new Vector3(0.22f, 0.6f, 0.24f), shirt);
            Kit.Box(b, new Vector3(-0.5f, 0.76f, 0), new Vector3(0.2f, 0.18f, 0.2f), skin);
            g.armPivot = Kit.Node("ArmPivot", b, new Vector3(0.5f, 1.36f, 0));
            Kit.Box(g.armPivot, new Vector3(0, -0.28f, 0), new Vector3(0.22f, 0.6f, 0.24f), shirt);
            Kit.Box(g.armPivot, new Vector3(0, -0.62f, 0), new Vector3(0.2f, 0.18f, 0.2f), skin);
            g.hand = Kit.Node("Hand", g.armPivot, new Vector3(0, -0.72f, 0.05f));
            return g;
        }

        public static Transform BuildSeed(Transform parent)
        {
            var root = Kit.Node("Seed", parent, Vector3.zero);
            var core = Kit.Node("Core", root, Vector3.zero);
            Kit.Ball(core, Vector3.zero, new Vector3(0.55f, 0.7f, 0.55f), Kit.Glow(new Color(1f, 0.55f, 0.85f), 1.6f, new Color(0.95f, 0.55f, 0.8f)));
            Kit.Ball(core, new Vector3(0, -0.1f, 0), new Vector3(0.6f, 0.45f, 0.6f), Kit.Lit(new Color(0.55f, 0.32f, 0.2f)));
            Kit.Box(core, new Vector3(0.1f, 0.42f, 0), new Vector3(0.3f, 0.06f, 0.16f), Kit.Lit(new Color(0.35f, 0.9f, 0.3f)), false, new Vector3(0, 0, 30));
            var trail = root.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.12f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0, 0.32f), new Keyframe(1, 0f));
            trail.sharedMaterial = Kit.Additive(new Color(1f, 0.7f, 0.95f, 0.45f));
            trail.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(new Color(1f, 0.85f, 1f), 0), new GradientColorKey(new Color(0.6f, 0.4f, 1f), 1) },
                alphaKeys = new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0, 1) },
            };
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
            return root;
        }

        /// Builds the plant of a tier with its feet at the origin. Returns the height.
        public static float BuildPlant(Transform root, int tier)
        {
            switch (tier)
            {
                case 0: return Sprout(root);
                case 1: return Bush(root);
                case 2: return Fern(root);
                case 3: return Willow(root);
                case 4: return HaloOak(root);
                default: return Colossus(root);
            }
        }

        static float Sprout(Transform r)
        {
            var g = Kit.Lit(new Color(0.4f, 0.85f, 0.3f));
            Kit.Box(r, new Vector3(0, 0.15f, 0), new Vector3(1.2f, 0.3f, 1.2f), Kit.Lit(new Color(0.5f, 0.36f, 0.22f)));
            Kit.Box(r, new Vector3(0, 0.9f, 0), new Vector3(0.2f, 1.3f, 0.2f), g);
            Kit.Box(r, new Vector3(0.38f, 1.55f, 0), new Vector3(0.8f, 0.14f, 0.45f), g, true, new Vector3(0, 0, 22));
            Kit.Box(r, new Vector3(-0.38f, 1.35f, 0), new Vector3(0.8f, 0.14f, 0.45f), g, true, new Vector3(0, 0, -22));
            Kit.Box(r, new Vector3(0, 1.72f, 0), new Vector3(0.36f, 0.36f, 0.36f), Kit.Lit(new Color(1f, 0.9f, 0.4f)));
            return 2.2f;
        }

        static float Bush(Transform r)
        {
            Kit.Box(r, new Vector3(0, 0.6f, 0), new Vector3(0.45f, 1.2f, 0.45f), Kit.Lit(new Color(0.5f, 0.33f, 0.2f)));
            var a = Kit.Lit(new Color(1f, 0.55f, 0.75f));
            var b = Kit.Lit(new Color(0.95f, 0.45f, 0.7f));
            Kit.Box(r, new Vector3(0, 2f, 0), new Vector3(2.2f, 1.6f, 2.2f), a);
            Kit.Box(r, new Vector3(-1f, 1.6f, 0.2f), new Vector3(1.3f, 1.2f, 1.3f), b);
            Kit.Box(r, new Vector3(1f, 1.7f, -0.2f), new Vector3(1.3f, 1.3f, 1.3f), b);
            Kit.Box(r, new Vector3(0.2f, 3f, 0), new Vector3(1.4f, 1f, 1.4f), b);
            Kit.Box(r, new Vector3(-0.5f, 2.6f, 1f), new Vector3(0.4f, 0.4f, 0.4f), Kit.Lit(Color.white));
            Kit.Box(r, new Vector3(0.7f, 2.3f, 1.05f), new Vector3(0.35f, 0.35f, 0.35f), Kit.Lit(Color.white));
            return 3.6f;
        }

        static float Fern(Transform r)
        {
            Kit.Box(r, new Vector3(0, 0.4f, 0), new Vector3(1.8f, 0.8f, 1.8f), Kit.Lit(new Color(0.25f, 0.22f, 0.32f)));
            var c1 = Kit.Glow(new Color(0.65f, 0.4f, 1f), 2f);
            var c2 = Kit.Glow(new Color(0.4f, 0.95f, 1f), 2f);
            for (int i = 0; i < 7; i++)
            {
                float t = (i - 3) / 3f;
                float h = 3.4f - Mathf.Abs(t) * 1.4f;
                Kit.Box(r, new Vector3(t * 1.1f, 0.8f + h / 2f, 0), new Vector3(0.45f, h, 0.45f), (i & 1) == 0 ? c1 : c2, true, new Vector3(0, 0, -t * 28f));
            }
            return 4.4f;
        }

        static float Willow(Transform r)
        {
            var bark = Kit.Lit(new Color(0.4f, 0.27f, 0.18f));
            Kit.Box(r, new Vector3(0, 1.8f, 0), new Vector3(0.7f, 3.6f, 0.7f), bark);
            var leaf = Kit.Lit(new Color(0.2f, 0.55f, 0.35f));
            Kit.Box(r, new Vector3(0, 3.9f, 0), new Vector3(4.4f, 1.1f, 4.4f), leaf);
            Kit.Box(r, new Vector3(0, 4.8f, 0), new Vector3(3f, 0.9f, 3f), leaf);
            var lantern = Kit.Glow(new Color(1f, 0.6f, 0.15f), 3f);
            for (int i = 0; i < 6; i++)
            {
                float x = -1.9f + i * 0.76f;
                float len = 1.2f + (i % 3) * 0.5f;
                Kit.Box(r, new Vector3(x, 3.35f - len / 2f, 1.9f), new Vector3(0.12f, len, 0.12f), leaf, true);
                Kit.Box(r, new Vector3(x, 3.3f - len, 1.9f), new Vector3(0.34f, 0.42f, 0.34f), lantern, false);
            }
            return 5.4f;
        }

        static float HaloOak(Transform r)
        {
            var bark = Kit.Lit(new Color(0.36f, 0.22f, 0.3f));
            Kit.Box(r, new Vector3(0, 2.2f, 0), new Vector3(1.1f, 4.4f, 1.1f), bark);
            Kit.Box(r, new Vector3(1f, 3.6f, 0), new Vector3(1.6f, 0.5f, 0.5f), bark, true, new Vector3(0, 0, 35));
            Kit.Box(r, new Vector3(-1f, 3.3f, 0), new Vector3(1.6f, 0.5f, 0.5f), bark, true, new Vector3(0, 0, -35));
            var a = Kit.Lit(new Color(0.85f, 0.3f, 0.5f));
            var b = Kit.Lit(new Color(1f, 0.45f, 0.6f));
            Kit.Box(r, new Vector3(0, 5f, 0), new Vector3(5.4f, 1.8f, 5.4f), a);
            Kit.Box(r, new Vector3(0, 6.3f, 0), new Vector3(4f, 1.3f, 4f), b);
            Kit.Box(r, new Vector3(0, 7.3f, 0), new Vector3(2.4f, 1f, 2.4f), a);
            var halo = Kit.Ring("Halo", r, new Vector3(0, 8.6f, 0), 1.6f, 16, 0.3f, Kit.Glow(new Color(1f, 0.95f, 0.7f), 4f));
            halo.localRotation = Quaternion.Euler(70, 0, 0);
            return 9.2f;
        }

        static float Colossus(Transform r)
        {
            var gold = Kit.Glow(new Color(1f, 0.62f, 0.15f), 0.35f, new Color(0.95f, 0.66f, 0.18f));
            var deep = Kit.Glow(new Color(0.8f, 0.35f, 0.08f), 0.35f, new Color(0.75f, 0.42f, 0.12f));
            var crown = Kit.Glow(new Color(1f, 0.75f, 0.25f), 0.42f, new Color(1f, 0.8f, 0.3f));
            // Roots.
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Kit.Box(r, dir * 1.6f + Vector3.up * 0.6f, new Vector3(0.8f, 1.2f, 2.4f), deep, true, new Vector3(-25, a, 0));
            }
            Kit.Box(r, new Vector3(0, 3.5f, 0), new Vector3(1.8f, 7f, 1.8f), gold);
            Kit.Box(r, new Vector3(0, 3.5f, 0), new Vector3(2.2f, 1f, 2.2f), deep);
            Kit.Box(r, new Vector3(1.6f, 6f, 0), new Vector3(3f, 0.8f, 0.8f), gold, true, new Vector3(0, 0, 35));
            Kit.Box(r, new Vector3(-1.6f, 6.3f, 0), new Vector3(3f, 0.8f, 0.8f), gold, true, new Vector3(0, 0, -35));
            Kit.Box(r, new Vector3(0, 8f, 0), new Vector3(8f, 2.4f, 8f), crown);
            Kit.Box(r, new Vector3(0, 9.9f, 0), new Vector3(6f, 1.8f, 6f), gold);
            Kit.Box(r, new Vector3(0, 11.3f, 0), new Vector3(3.8f, 1.4f, 3.8f), crown);
            Kit.Box(r, new Vector3(0, 12.4f, 0), new Vector3(1.8f, 1f, 1.8f), gold);
            Kit.Ball(r, new Vector3(0, 8.2f, 4.1f), Vector3.one * 1.6f, Kit.Glow(new Color(1f, 0.25f, 0.9f), 5f), false);
            var ringMat = Kit.Glow(new Color(1f, 0.85f, 0.3f), 4f);
            var r1 = Kit.Ring("OrbitA", r, new Vector3(0, 8f, 0), 6.2f, 30, 0.45f, ringMat);
            r1.localRotation = Quaternion.Euler(75, 0, 15);
            r1.gameObject.AddComponent<Spinner>().degreesPerSecond = new Vector3(0, 0, 40);
            var r2 = Kit.Ring("OrbitB", r, new Vector3(0, 8f, 0), 7.4f, 34, 0.4f, Kit.Glow(new Color(0.5f, 1f, 0.4f), 4f));
            r2.localRotation = Quaternion.Euler(60, 40, -20);
            r2.gameObject.AddComponent<Spinner>().degreesPerSecond = new Vector3(0, 0, -30);
            return 14f;
        }
    }
}
