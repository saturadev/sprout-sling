using System.Collections.Generic;
using UnityEngine;

namespace SproutSling
{
    public struct Zone
    {
        public string name;
        public Color sky, fog, light, ambient, label;
        public float lightIntensity;
    }

    /// Builds the canyon: the golden launch pad, three zones of blocky walls, props and luck rings.
    public static class World
    {
        public const float CanyonHalfWidth = 7f;
        public const float GrottoStart = 280f;
        public const float RiftStart = 580f;
        public const float End = 900f;
        public const float RingHeight = 6f;

        public static readonly Zone[] Zones =
        {
            new Zone { name = "Sunny Meadow", sky = new Color(0.42f, 0.72f, 1f), fog = new Color(0.72f, 0.86f, 1f), light = new Color(1f, 0.95f, 0.85f), lightIntensity = 1.25f, ambient = new Color(0.62f, 0.68f, 0.78f), label = new Color(0.55f, 1f, 0.75f) },
            new Zone { name = "Glow Grotto", sky = new Color(0.12f, 0.3f, 0.38f), fog = new Color(0.16f, 0.36f, 0.42f), light = new Color(0.7f, 0.95f, 1f), lightIntensity = 0.8f, ambient = new Color(0.25f, 0.42f, 0.48f), label = new Color(0.4f, 0.95f, 1f) },
            new Zone { name = "Starless Rift", sky = new Color(0.03f, 0.015f, 0.07f), fog = new Color(0.06f, 0.02f, 0.11f), light = new Color(0.75f, 0.5f, 1f), lightIntensity = 0.35f, ambient = new Color(0.16f, 0.1f, 0.24f), label = new Color(1f, 0.45f, 1f) },
        };

        public static int ZoneIndex(float z) => z < GrottoStart ? 0 : z < RiftStart ? 1 : 2;

        /// Zone look at a distance, blended over 50 m at each border.
        public static Zone Blend(float z)
        {
            float a = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(GrottoStart - 25, GrottoStart + 25, z));
            float b = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(RiftStart - 25, RiftStart + 25, z));
            Zone l = Lerp(Zones[0], Zones[1], a);
            Zone r = Lerp(l, Zones[2], b);
            r.name = Zones[ZoneIndex(z)].name;
            r.label = Zones[ZoneIndex(z)].label;
            return r;
        }

        static Zone Lerp(Zone x, Zone y, float t) => new Zone
        {
            sky = Color.Lerp(x.sky, y.sky, t), fog = Color.Lerp(x.fog, y.fog, t), light = Color.Lerp(x.light, y.light, t),
            ambient = Color.Lerp(x.ambient, y.ambient, t), lightIntensity = Mathf.Lerp(x.lightIntensity, y.lightIntensity, t),
        };

        public class Ring
        {
            public float z;
            public Transform t;
            public bool hit;
        }

        public static List<Ring> Build(Transform root)
        {
            var rnd = new System.Random(7);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

            var stat = Kit.Node("Static", root, Vector3.zero);
            var live = Kit.Node("Live", root, Vector3.zero);

            // Launch pad: raised golden tiles.
            var pad = Kit.Node("LaunchPad", stat, Vector3.zero);
            Kit.Box(pad, new Vector3(0, -0.2f, -4f), new Vector3(14f, 0.8f, 12f), Kit.Lit(new Color(0.85f, 0.6f, 0.08f)));
            var tile = Kit.Lit(new Color(1f, 0.8f, 0.15f), 0.35f);
            for (int x = 0; x < 7; x++)
                for (int z = 0; z < 6; z++)
                    Kit.Box(pad, new Vector3(-6f + x * 2f, 0.25f, -9f + z * 2f), new Vector3(1.8f, 0.2f, 1.8f), tile);

            // Floors per zone.
            Kit.Box(stat, new Vector3(0, -0.5f, (2f + GrottoStart) / 2f), new Vector3(CanyonHalfWidth * 2, 1, GrottoStart - 2f), Kit.Lit(new Color(0.42f, 0.74f, 0.3f)));
            Kit.Box(stat, new Vector3(0, -0.49f, (2f + GrottoStart) / 2f), new Vector3(5f, 1, GrottoStart - 2f), Kit.Lit(new Color(0.93f, 0.86f, 0.64f)));
            Kit.Box(stat, new Vector3(0, -0.5f, (GrottoStart + RiftStart) / 2f), new Vector3(CanyonHalfWidth * 2, 1, RiftStart - GrottoStart), Kit.Lit(new Color(0.16f, 0.3f, 0.32f)));
            Kit.Box(stat, new Vector3(0, -0.49f, (GrottoStart + RiftStart) / 2f), new Vector3(5f, 1, RiftStart - GrottoStart), Kit.Lit(new Color(0.22f, 0.42f, 0.44f)));
            Kit.Box(stat, new Vector3(0, -0.5f, (RiftStart + End + 60) / 2f), new Vector3(CanyonHalfWidth * 2, 1, End + 60 - RiftStart), Kit.Lit(new Color(0.06f, 0.045f, 0.1f), 0.6f));
            // Back wall behind the pad and far end wall.
            Kit.Box(stat, new Vector3(0, 6f, -12f), new Vector3(40f, 14f, 2f), Kit.Lit(new Color(0.5f, 0.36f, 0.22f)));

            var meadowDirt = new[] { Kit.Lit(new Color(0.56f, 0.39f, 0.23f)), Kit.Lit(new Color(0.5f, 0.34f, 0.2f)) };
            var meadowGrass = new[] { Kit.Lit(new Color(0.38f, 0.8f, 0.24f)), Kit.Lit(new Color(0.45f, 0.86f, 0.3f)) };
            var grottoRock = new[] { Kit.Lit(new Color(0.2f, 0.33f, 0.4f)), Kit.Lit(new Color(0.16f, 0.28f, 0.35f)) };
            var grottoMoss = new[] { Kit.Lit(new Color(0.2f, 0.62f, 0.56f)), Kit.Lit(new Color(0.26f, 0.7f, 0.6f)) };
            var riftRock = new[] { Kit.Lit(new Color(0.08f, 0.06f, 0.12f), 0.5f), Kit.Lit(new Color(0.1f, 0.07f, 0.15f), 0.5f) };
            var riftEdge = new[] { Kit.Glow(new Color(1f, 0.2f, 0.85f), 3f), Kit.Glow(new Color(0.75f, 0.25f, 1f), 3f) };

            int n = 0;
            for (float z = -10f; z < End + 60f; z += 4f, n++)
            {
                int zi = ZoneIndex(z);
                var side = zi == 0 ? meadowDirt : zi == 1 ? grottoRock : riftRock;
                var top = zi == 0 ? meadowGrass : zi == 1 ? grottoMoss : riftEdge;
                for (int s = -1; s <= 1; s += 2)
                {
                    float h1 = zi == 2 ? R(4f, 9f) : R(5f, 8f);
                    float h2 = h1 + R(3f, 7f);
                    float x1 = s * (CanyonHalfWidth + 2f);
                    float x2 = s * (CanyonHalfWidth + 6f);
                    Kit.Box(stat, new Vector3(x1, h1 / 2f, z), new Vector3(4f, h1, 4f), side[(n + (s > 0 ? 1 : 0)) & 1]);
                    Kit.Box(stat, new Vector3(x2, h2 / 2f, z), new Vector3(4f, h2, 4f), side[(n + (s > 0 ? 0 : 1)) & 1]);
                    float cap = zi == 2 ? 0.25f : 0.7f;
                    Kit.Box(stat, new Vector3(x1 - s * 0.1f, h1 + cap / 2f, z), new Vector3(4.2f, cap, 4.05f), top[n & 1], zi != 2);
                    Kit.Box(stat, new Vector3(x2, h2 + cap / 2f, z), new Vector3(4.2f, cap, 4.05f), top[(n + 1) & 1], zi != 2);
                    // Grass drips down the inner wall face.
                    if (zi == 0 && rnd.NextDouble() < 0.55)
                        Kit.Box(stat, new Vector3(s * (CanyonHalfWidth + 0.02f), h1 - 0.9f, z + R(-1f, 1f)), new Vector3(0.3f, R(0.8f, 1.8f), R(0.8f, 1.6f)), top[n & 1], false);

                    if (zi == 0 && rnd.NextDouble() < 0.4) Tree(stat, new Vector3(s * R(CanyonHalfWidth + 1f, CanyonHalfWidth + 7f), (rnd.NextDouble() < 0.5 ? h1 : h2) + 0.7f, z + R(-1.5f, 1.5f)), rnd);
                    if (zi == 1 && rnd.NextDouble() < 0.45) Crystal(stat, new Vector3(s * R(CanyonHalfWidth + 0.5f, CanyonHalfWidth + 3f), h1 + 0.6f, z), rnd);
                    if (zi == 1 && rnd.NextDouble() < 0.25) Crystal(stat, new Vector3(s * R(CanyonHalfWidth - 1.5f, CanyonHalfWidth - 0.5f), 0f, z), rnd);
                }
                // Meadow bushes and flowers on the floor.
                if (zi == 0 && rnd.NextDouble() < 0.35)
                {
                    float x = (rnd.NextDouble() < 0.5 ? -1 : 1) * R(3.2f, 6f);
                    Kit.Box(stat, new Vector3(x, 0.4f, z), Vector3.one * R(0.7f, 1.1f), meadowGrass[n & 1]);
                    if (rnd.NextDouble() < 0.5) Kit.Box(stat, new Vector3(x + 0.6f, 0.15f, z + 0.8f), Vector3.one * 0.3f, Kit.Lit(rnd.NextDouble() < 0.5 ? new Color(1f, 0.45f, 0.7f) : new Color(1f, 0.9f, 0.3f)), false);
                }
                // Rift: neon strips along the floor edges and floating orbs.
                if (zi == 2)
                {
                    Kit.Box(stat, new Vector3(-CanyonHalfWidth + 0.3f, 0.06f, z), new Vector3(0.25f, 0.12f, 3.2f), riftEdge[0], false);
                    Kit.Box(stat, new Vector3(CanyonHalfWidth - 0.3f, 0.06f, z), new Vector3(0.25f, 0.12f, 3.2f), riftEdge[0], false);
                    if (rnd.NextDouble() < 0.4)
                    {
                        var orb = Kit.Ball(live, new Vector3((rnd.NextDouble() < 0.5 ? -1 : 1) * R(4.5f, 12f), R(3f, 14f), z), Vector3.one * R(0.8f, 2.2f),
                            Kit.Glow(rnd.NextDouble() < 0.7 ? new Color(1f, 0.15f, 0.85f) : new Color(0.2f, 0.9f, 1f), 4f), false);
                        orb.AddComponent<Bobber>().amplitude = R(0.3f, 0.9f);
                    }
                }
            }

            // Grotto ceiling arches give the cave feel.
            for (float z = GrottoStart + 10; z < RiftStart - 10; z += 36f)
            {
                var rock = grottoRock[0];
                Kit.Box(stat, new Vector3(0, 16f, z), new Vector3(30f, 3f, 5f), rock);
                Kit.Box(stat, new Vector3(-4f, 14.2f, z), new Vector3(2f, 1.2f, 1.2f), Kit.Glow(new Color(1f, 0.4f, 0.8f), 3f), false);
                Kit.Box(stat, new Vector3(3.5f, 14.5f, z + 1), new Vector3(1.4f, 1.6f, 1.4f), Kit.Glow(new Color(0.3f, 1f, 0.95f), 3f), false);
            }

            // Sky: blocky clouds over the meadow, stars over the rift.
            var cloud = Kit.Lit(new Color(1f, 1f, 1f));
            for (int i = 0; i < 40; i++)
            {
                var c = new Vector3(R(-70, 70), R(35, 60), R(-20, GrottoStart + 60));
                if (Mathf.Abs(c.x) < 18) c.x += Mathf.Sign(c.x) * 18;
                Kit.Box(stat, c, new Vector3(R(10, 22), R(2, 4), R(6, 12)), cloud, false);
                Kit.Box(stat, c + new Vector3(R(-3, 3), 2, R(-2, 2)), new Vector3(R(6, 10), 2.5f, R(4, 7)), cloud, false);
            }
            var star = Kit.Glow(new Color(1f, 0.9f, 1f), 3f);
            for (int i = 0; i < 260; i++)
            {
                var p = new Vector3(R(-90, 90), R(18, 90), R(RiftStart - 20, End + 140));
                if (Mathf.Abs(p.x) < 20) p.x += Mathf.Sign(p.x) * 20;
                Kit.Box(stat, p, Vector3.one * R(0.25f, 0.7f), star, false);
            }

            // Luck rings along the flight line.
            var rings = new List<Ring>();
            var ringMat = Kit.Glow(new Color(0.35f, 1f, 0.35f), 3.5f);
            for (float z = 45f; z < End; z += 55f)
            {
                var t = Kit.Ring("LuckRing", live, new Vector3(0, RingHeight, z), 1.9f, 14, 0.45f, ringMat);
                var clover = Kit.Node("Clover", t, Vector3.zero);
                foreach (var o in new[] { new Vector3(0.3f, 0.3f), new Vector3(-0.3f, 0.3f), new Vector3(0.3f, -0.3f), new Vector3(-0.3f, -0.3f) })
                    Kit.Box(clover, o, new Vector3(0.5f, 0.5f, 0.12f), ringMat, false, new Vector3(0, 0, 45));
                clover.gameObject.AddComponent<Spinner>().degreesPerSecond = new Vector3(0, 120, 0);
                rings.Add(new Ring { z = z, t = t });
            }

            StaticBatchingUtility.Combine(stat.gameObject);
            return rings;
        }

        static void Tree(Transform p, Vector3 at, System.Random rnd)
        {
            float h = 1.5f + (float)rnd.NextDouble() * 2f;
            Kit.Box(p, at + Vector3.up * h / 2f, new Vector3(0.6f, h, 0.6f), Kit.Lit(new Color(0.45f, 0.3f, 0.17f)));
            var leaf = Kit.Lit(rnd.NextDouble() < 0.5 ? new Color(0.22f, 0.62f, 0.2f) : new Color(0.3f, 0.7f, 0.25f));
            Kit.Box(p, at + Vector3.up * (h + 0.8f), new Vector3(2.6f, 1.6f, 2.6f), leaf);
            Kit.Box(p, at + Vector3.up * (h + 2.1f), new Vector3(1.6f, 1.2f, 1.6f), leaf);
        }

        static void Crystal(Transform p, Vector3 at, System.Random rnd)
        {
            var m = Kit.Glow(rnd.NextDouble() < 0.5 ? new Color(1f, 0.45f, 0.85f) : new Color(0.35f, 1f, 0.95f), 2.2f);
            int k = 2 + rnd.Next(3);
            for (int i = 0; i < k; i++)
            {
                float h = 1f + (float)rnd.NextDouble() * 2.5f;
                Kit.Box(p, at + new Vector3(((float)rnd.NextDouble() - 0.5f) * 1.2f, h / 2f, ((float)rnd.NextDouble() - 0.5f) * 1.2f), new Vector3(0.5f, h, 0.5f), m, false,
                    new Vector3(((float)rnd.NextDouble() - 0.5f) * 40f, (float)rnd.NextDouble() * 90f, ((float)rnd.NextDouble() - 0.5f) * 40f));
            }
        }
    }
}
