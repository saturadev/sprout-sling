using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR && SATURA_SPACE
using SaturaSpace;
#endif

namespace SproutSling
{
    /// Sprout Sling: stop the power meter, sling a seed down the canyon, collect luck, roll a rare plant.
    public class SproutGame : MonoBehaviour
    {
        public enum Phase { Aim, Throwing, Flying, Reveal }

        public static SproutGame Instance { get; private set; }

        [Header("Feel")]
        public float meterSpeed = 1.35f;      // sweeps per second
        public float perfectFrom = 0.9f;
        public float minDistance = 50f, maxGoodDistance = 540f, perfectDistance = 870f;
        public float luckPerDecade = 70f;      // metres per x10 luck
        public float ringMultiplier = 3f;

        public Phase phase { get; private set; }
        public float MeterValue { get; private set; }
        public float Power { get; private set; }
        public bool Perfect { get; private set; }
        public float Distance { get; private set; }
        public double Luck { get; private set; } = 1;
        public int RingsHit { get; private set; }
        public double Odds { get; private set; }
        public int Tier { get; private set; } = -1;
        public int Rounds { get; private set; }
        public double BestOdds { get; private set; }
        public int BestTier { get; private set; } = -1;
        public bool BotControlled { get; set; }
        public Vector3 SeedPosition => seed ? seed.position : Vector3.zero;

        Camera cam;
        Light sun;
        SproutUI ui;
        Models.Gardener gardener;
        Transform seed, seedCore, plantRoot, backlight;
        ParticleSystem sparkles, confetti, puff;
        List<World.Ring> rings;
        float meterClock, phaseClock, flightDur, camBlend, dim, dimTarget, perfectPop, popClock;
        Vector3 launchPos, camVel, camLookVel, camLook;
        Vector3 revealCamPos, revealLook;
        bool released;

        static readonly Vector3 PadCamPos = new Vector3(0.35f, 2.9f, -10.4f);
        static readonly Vector3 PadLook = new Vector3(-0.1f, 1.9f, 8f);
        static readonly Vector3 GardenerPos = new Vector3(-0.85f, 0.35f, -4.6f);

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;

            // Camera, sun, post.
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.SetParent(transform, false);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 700f;
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            var vol = new GameObject("Post").AddComponent<Volume>();
            vol.transform.SetParent(transform, false);
            vol.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.1f);
            bloom.threshold.Override(1f);
            bloom.scatter.Override(0.7f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(18f);
            color.contrast.Override(8f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.22f);
            vol.sharedProfile = profile;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 260f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.skybox = null;

            rings = World.Build(Kit.Node("World", transform, Vector3.zero));
            gardener = Models.BuildGardener(transform, GardenerPos);
            seed = Models.BuildSeed(transform);
            seedCore = seed.Find("Core");
            ui = SproutUI.Build(transform);

            sparkles = MakeParticles("Sparkles", Kit.Additive(new Color(1f, 0.75f, 1f, 1f)));
            var sm = sparkles.main;
            sm.startLifetime = 0.7f; sm.startSpeed = 0.6f; sm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            sm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 1f), new Color(0.6f, 0.9f, 1f));
            var sshape = sparkles.shape; sshape.shapeType = ParticleSystemShapeType.Sphere; sshape.radius = 0.3f;

            confetti = MakeParticles("Clovers", Kit.ParticleOpaque(Color.white));
            var cm = confetti.main;
            cm.startLifetime = 4f; cm.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 3f); cm.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            cm.gravityModifier = 0.25f; cm.startRotation3D = true;
            cm.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); cm.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); cm.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            cm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.95f, 0.3f), new Color(0.6f, 1f, 0.4f));
            var cr = confetti.rotationOverLifetime; cr.enabled = true; cr.separateAxes = true;
            cr.x = new ParticleSystem.MinMaxCurve(-4f, 4f); cr.y = new ParticleSystem.MinMaxCurve(-4f, 4f); cr.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            var cs = confetti.shape; cs.shapeType = ParticleSystemShapeType.Box; cs.scale = new Vector3(16f, 1f, 6f);
            confetti.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Mesh;
            confetti.GetComponent<ParticleSystemRenderer>().mesh = Kit.Cube;

            puff = MakeParticles("Puff", Kit.ParticleOpaque(Color.white));
            var pm = puff.main;
            pm.startLifetime = 0.8f; pm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f); pm.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            pm.gravityModifier = 0.8f;
            var ps = puff.shape; ps.shapeType = ParticleSystemShapeType.Hemisphere; ps.radius = 0.5f; ps.rotation = new Vector3(-90, 0, 0);
            puff.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Mesh;
            puff.GetComponent<ParticleSystemRenderer>().mesh = Kit.Cube;

            backlight = Kit.Node("Backlight", transform, Vector3.zero);
            Kit.Ball(backlight, Vector3.zero, Vector3.one, Kit.Additive(new Color(0.7f, 0.5f, 1f, 0.16f)), false);
            Kit.Ball(backlight, new Vector3(0, -0.15f, 0), Vector3.one * 0.62f, Kit.Additive(new Color(0.95f, 0.85f, 1f, 0.28f)), false);
            Kit.Ball(backlight, new Vector3(0, -0.3f, 0), Vector3.one * 0.3f, Kit.Additive(new Color(1f, 1f, 1f, 0.45f)), false);
            backlight.name = "Backlight";
            backlight.gameObject.SetActive(false);

            StartRound();
        }

        ParticleSystem MakeParticles(string name, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var p = go.AddComponent<ParticleSystem>();
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            var em = p.emission;
            em.rateOverTime = 0;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            go.GetComponent<ParticleSystemRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            p.Play();
            return p;
        }

        /// Back on the pad, meter running.
        public void StartRound()
        {
            StopAllCoroutines();
            phase = Phase.Aim;
            meterClock = 0; MeterValue = 0; phaseClock = 0;
            Luck = 1; RingsHit = 0; Distance = 0; Odds = 0; Tier = -1; released = false;
            dim = dimTarget = 0;
            foreach (var r in rings) { r.hit = false; r.t.localScale = Vector3.one; r.t.gameObject.SetActive(true); }
            if (plantRoot) Destroy(plantRoot.gameObject);
            backlight.gameObject.SetActive(false);
            seed.gameObject.SetActive(false);
            seed.GetComponent<TrailRenderer>().emitting = false;
            seed.GetComponent<TrailRenderer>().Clear();
            gardener.armPivot.localRotation = Quaternion.identity;
            camBlend = 0;
            cam.transform.position = PadCamPos;
            camLook = PadLook;
            cam.transform.LookAt(camLook);
            ui.meter.SetActive(true);
            ui.luckPanel.SetActive(false);
            ui.hintText.gameObject.SetActive(true);
            ui.perfectText.gameObject.SetActive(false);
            ui.oddsText.text = ui.titleText.text = ui.plantText.text = ui.fullText.text = ui.popText.text = "";
            ui.bestText.text = BestTier < 0 ? "" : $"BEST  1 IN {Kit.Short(BestOdds)}  ·  {Models.Tiers[BestTier].plant}";
            ApplyZone(0f);
        }

        /// Stops the meter and throws with its current value.
        public void Press()
        {
            if (phase != Phase.Aim) return;
            Throw(MeterValue);
        }

        public void Throw(float power)
        {
            if (phase != Phase.Aim) return;
            Power = Mathf.Clamp01(power);
            MeterValue = Power;
            ui.SetMeter(Power);
            Perfect = Power >= perfectFrom;
            Distance = Perfect ? perfectDistance : Mathf.Lerp(minDistance, maxGoodDistance, Mathf.Pow(Power / perfectFrom, 1.3f));
            flightDur = 1.5f + Distance / 170f;
            phase = Phase.Throwing;
            phaseClock = 0;
            released = false;
            Rounds++;
            ui.hintText.gameObject.SetActive(false);
            ui.perfectText.gameObject.SetActive(true);
            ui.perfectText.text = Perfect ? "PERFECT!" : Power > 0.6f ? "GREAT" : Power > 0.3f ? "GOOD" : "WEAK";
            ui.perfectText.color = Perfect ? new Color(0.45f, 1f, 0.35f) : Power > 0.3f ? new Color(1f, 0.85f, 0.25f) : new Color(1f, 0.4f, 0.3f);
            perfectPop = 0;
#if UNITY_EDITOR && SATURA_SPACE
            SaturaLog.Log("sprout", $"throw power={Power:0.00} perfect={Perfect} distance={Distance:0}");
#endif
        }

        static bool PressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            phaseClock += dt;
            switch (phase)
            {
                case Phase.Aim:
                    meterClock += dt * meterSpeed;
                    MeterValue = Mathf.PingPong(meterClock, 1f);
                    ui.SetMeter(MeterValue);
                    ui.hintText.color = new Color(1, 1, 1, 0.65f + 0.35f * Mathf.Sin(Time.time * 6f));
                    gardener.body.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.04f;
                    if (!BotControlled && PressedThisFrame()) Press();
                    break;
                case Phase.Throwing: UpdateThrow(); break;
                case Phase.Flying: UpdateFlight(); break;
            }

            // PERFECT pop.
            if (ui.perfectText.gameObject.activeSelf)
            {
                perfectPop += dt;
                float s = perfectPop < 0.12f ? Mathf.Lerp(0.3f, 1.35f, perfectPop / 0.12f) : Mathf.Lerp(1.35f, 1f, (perfectPop - 0.12f) / 0.15f);
                ui.perfectText.transform.localScale = Vector3.one * s;
            }
            // Ring pop text.
            if (ui.popText.text.Length > 0)
            {
                popClock += dt;
                ui.popText.transform.localScale = Vector3.one * (popClock < 0.1f ? Mathf.Lerp(0.5f, 1.25f, popClock / 0.1f) : Mathf.Lerp(1.25f, 1f, (popClock - 0.1f) / 0.2f));
                if (popClock > 0.7f) ui.popText.text = "";
            }
            if (ui.flash.color.a > 0) ui.flash.color = new Color(1, 1, 1, Mathf.MoveTowards(ui.flash.color.a, 0, dt * 2.2f));

            dim = Mathf.MoveTowards(dim, dimTarget, dt * 2f);
            ApplyZone(phase == Phase.Aim || phase == Phase.Throwing ? 0f : seed.position.z);
        }

        void LateUpdate()
        {
            if (phase == Phase.Throwing || phase == Phase.Flying)
            {
                if (released) camBlend = Mathf.MoveTowards(camBlend, 1f, Time.deltaTime / 0.55f);
                float b = Mathf.SmoothStep(0, 1, camBlend);
                var sp = seed.position;
                var chasePos = new Vector3(sp.x * 0.5f, sp.y + 1.7f, sp.z - 6.2f);
                var chaseLook = new Vector3(0, sp.y * 0.85f, sp.z + 12f);
                cam.transform.position = Vector3.Lerp(PadCamPos, chasePos, b);
                camLook = Vector3.Lerp(PadLook, chaseLook, b);
                cam.transform.LookAt(camLook);
            }
            else if (phase == Phase.Reveal)
            {
                cam.transform.position = Vector3.SmoothDamp(cam.transform.position, revealCamPos, ref camVel, 0.3f);
                camLook = Vector3.SmoothDamp(camLook, revealLook, ref camLookVel, 0.3f);
                cam.transform.LookAt(camLook);
            }
        }

        void UpdateThrow()
        {
            float t = phaseClock;
            float angle = t < 0.16f ? Mathf.Lerp(0, 150, t / 0.16f) : Mathf.Lerp(150, 300, Mathf.Clamp01((t - 0.16f) / 0.16f));
            gardener.armPivot.localRotation = Quaternion.Euler(angle, 0, 0);
            gardener.body.localRotation = Quaternion.Euler(t < 0.16f ? -8f * t / 0.16f : Mathf.Lerp(-8f, 12f, Mathf.Clamp01((t - 0.16f) / 0.16f)), 0, 0);
            if (!released && angle >= 245f)
            {
                released = true;
                launchPos = gardener.hand.position;
                seed.position = launchPos;
                seed.gameObject.SetActive(true);
                var trail = seed.GetComponent<TrailRenderer>();
                trail.Clear();
                trail.emitting = true;
                ui.luckPanel.SetActive(true);
                phase = Phase.Flying;
                phaseClock = 0;
                SetLuck();
            }
        }

        Vector3 FlightPos(float u)
        {
            float k = 0.55f * u + 0.45f * (1f - (1f - u) * (1f - u));
            float z = Mathf.Lerp(launchPos.z, Distance, k);
            float y;
            if (u < 0.12f) y = Mathf.Lerp(launchPos.y, 7f, 1f - Mathf.Pow(1f - u / 0.12f, 2f));
            else if (u < 0.86f) { float q = (u - 0.12f) / 0.74f; y = Mathf.Lerp(7f, 5.4f, q) + Mathf.Sin(q * Mathf.PI * 3f) * 0.5f; }
            else y = Mathf.Lerp(5.4f, 0.45f, Mathf.Pow((u - 0.86f) / 0.14f, 2f));
            float x = Mathf.Lerp(launchPos.x, 0f, Mathf.SmoothStep(0, 1, u / 0.12f));
            return new Vector3(x, y, z);
        }

        void UpdateFlight()
        {
            // Arm settles back.
            gardener.armPivot.localRotation = Quaternion.Slerp(gardener.armPivot.localRotation, Quaternion.identity, Time.deltaTime * 6f);
            gardener.body.localRotation = Quaternion.Slerp(gardener.body.localRotation, Quaternion.identity, Time.deltaTime * 6f);
            if (phaseClock > 0.6f) ui.meter.SetActive(false);

            float u = Mathf.Clamp01(phaseClock / flightDur);
            seed.position = FlightPos(u);
            seedCore.Rotate(new Vector3(540f, 220f, 0) * Time.deltaTime, Space.Self);
            sparkles.transform.position = seed.position;
            sparkles.Emit(Mathf.Max(1, Mathf.RoundToInt(Time.deltaTime * 70f)));

            foreach (var r in rings)
            {
                if (r.hit || r.z > seed.position.z || r.z > Distance) continue;
                r.hit = true;
                RingsHit++;
                StartCoroutine(PopRing(r.t));
                ui.popText.text = $"x{ringMultiplier:0} LUCK!";
                popClock = 0;
            }
            SetLuck();
            if (u >= 1f) Land();
        }

        void SetLuck()
        {
            Luck = System.Math.Pow(10, Mathf.Max(0, seed.position.z) / luckPerDecade) * System.Math.Pow(ringMultiplier, RingsHit);
            ui.luckText.text = "x" + Kit.Short(Luck);
            var zone = World.Zones[World.ZoneIndex(seed.position.z)];
            ui.zoneText.text = zone.name;
            ui.zoneText.color = zone.label;
        }

        IEnumerator PopRing(Transform t)
        {
            for (float a = 0; a < 0.3f; a += Time.deltaTime)
            {
                t.localScale = Vector3.one * Mathf.Lerp(1f, 2.4f, a / 0.3f);
                yield return null;
            }
            t.gameObject.SetActive(false);
        }

        void Land()
        {
            phase = Phase.Reveal;
            phaseClock = 0;
            seed.GetComponent<TrailRenderer>().emitting = false;
            puff.transform.position = seed.position;
            var pm = puff.main;
            pm.startColor = World.ZoneIndex(Distance) == 2 ? new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 1f)) : new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.9f, 0.75f));
            puff.Emit(40);
            Odds = System.Math.Max(2, System.Math.Round(Luck * System.Math.Pow(10, Random.Range(-0.4f, 0.4f))));
            Tier = Models.TierOf(Odds);
#if UNITY_EDITOR && SATURA_SPACE
            SaturaLog.Log("sprout", $"landed z={Distance:0} luck={Luck:0.###e0} rings={RingsHit} odds={Odds:0.###e0} tier={Models.Tiers[Tier].title}");
#endif
            StartCoroutine(Reveal());
        }

        float BuildStagePlant(int tier, bool silhouette)
        {
            if (plantRoot) Destroy(plantRoot.gameObject);
            plantRoot = Kit.Node("Plant", transform, new Vector3(0, 0, Distance + 1.5f));
            float h = Models.BuildPlant(plantRoot, tier);
            if (silhouette)
            {
                var black = Kit.Unlit(new Color(0.01f, 0.01f, 0.02f));
                var gold = Kit.Outline(new Color(1f, 0.78f, 0.3f));
                foreach (var r in plantRoot.GetComponentsInChildren<MeshRenderer>())
                {
                    r.sharedMaterial = black;
                    var s = r.transform.lossyScale;
                    var hull = new GameObject("Hull");
                    hull.transform.SetParent(r.transform, false);
                    hull.transform.localScale = new Vector3(1f + 0.16f / Mathf.Max(0.01f, s.x), 1f + 0.16f / Mathf.Max(0.01f, s.y), 1f + 0.16f / Mathf.Max(0.01f, s.z));
                    hull.AddComponent<MeshFilter>().sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
                    var hr = hull.AddComponent<MeshRenderer>();
                    hr.sharedMaterial = gold;
                    hr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            if (!silhouette)
            {
                var key = new GameObject("RevealLight").AddComponent<Light>();
                key.transform.SetParent(plantRoot, false);
                key.type = LightType.Point;
                key.color = new Color(1f, 0.9f, 0.75f);
                key.range = h * 2.6f + 6f;
                key.intensity = 2.2f + h * 0.25f;
                key.transform.localPosition = new Vector3(0, h * 0.6f, -h * 0.9f - 2f);
            }
            backlight.gameObject.SetActive(silhouette);
            backlight.position = new Vector3(0, h * 0.3f, Distance + 1.5f + h * 0.35f + 2f);
            backlight.localScale = new Vector3(h * 1.2f + 2f, h * 0.8f + 1.5f, 1f);
            float dist = silhouette ? Mathf.Max(6.5f, h * 1.75f) : Mathf.Max(8f, h * 2.1f);
            revealCamPos = new Vector3(0, h * (silhouette ? 0.42f : 0.5f) + 1f, Distance + 1.5f - dist);
            revealLook = new Vector3(0, silhouette ? h * 0.5f + 0.4f : h * 0.74f + 0.6f, Distance + 1.5f);
            return h;
        }

        IEnumerator Grow(Transform t, float seconds)
        {
            for (float a = 0; a < seconds && t; a += Time.deltaTime)
            {
                float k = a / seconds;
                t.localScale = Vector3.one * (k < 0.7f ? Mathf.Lerp(0.2f, 1.08f, k / 0.7f) : Mathf.Lerp(1.08f, 1f, (k - 0.7f) / 0.3f));
                yield return null;
            }
            if (t) t.localScale = Vector3.one;
        }

        IEnumerator PopText(TMPro.TMP_Text t,float from, float seconds)
        {
            for (float a = 0; a < seconds; a += Time.deltaTime)
            {
                float k = a / seconds;
                t.transform.localScale = Vector3.one * (k < 0.6f ? Mathf.Lerp(from, 1.15f, k / 0.6f) : Mathf.Lerp(1.15f, 1f, (k - 0.6f) / 0.4f));
                yield return null;
            }
            t.transform.localScale = Vector3.one;
        }

        IEnumerator Reveal()
        {
            camVel = camLookVel = Vector3.zero;
            revealCamPos = new Vector3(0, 3f, Distance - 8f);
            revealLook = new Vector3(0, 1.5f, Distance + 2f);
            yield return new WaitForSeconds(0.45f);
            seed.gameObject.SetActive(false);
            dimTarget = 1f;
            ui.luckPanel.SetActive(false);

            // Silhouette roll: one stage per tier below the result, then the result.
            var stages = new List<(int tier, double odds)>();
            for (int t = 0; t < Tier; t++)
            {
                double lo = t == 0 ? 2 : Models.Tiers[t - 1].below;
                double hi = Models.Tiers[t].below;
                stages.Add((t, System.Math.Round(lo * System.Math.Pow(hi / lo, Random.Range(0.25f, 0.85f)))));
            }
            stages.Add((Tier, Odds));

            for (int i = 0; i < stages.Count; i++)
            {
                bool last = i == stages.Count - 1;
                BuildStagePlant(stages[i].tier, true);
                StartCoroutine(Grow(plantRoot, 0.25f));
                ui.oddsText.rectTransform.anchoredPosition = new Vector2(0, 40);
                ui.oddsText.text = $"1 IN <color=#FFC83D>{Kit.Short(stages[i].odds)}</color>";
                StartCoroutine(PopText(ui.oddsText, 0.5f, 0.2f));
                if (i > 0) confetti.transform.position = new Vector3(0, 12f, Distance + 1f);
                if (i > 0) confetti.Emit(25 + i * 10);
                yield return new WaitForSeconds(last ? 0.75f : 0.42f);
            }

            // Reveal in colour.
            BuildStagePlant(Tier, false);
            ui.flash.color = Color.white;
            var rare = Models.Tiers[Tier];
            ui.titleText.text = rare.title;
            ui.titleText.color = rare.color;
            ui.plantText.text = Tier >= 5 ? "ULTRA RARE  ·  " + rare.plant.ToUpper() : rare.plant.ToUpper();
            ui.oddsText.text = $"1 IN <color=#FFC83D>{Kit.Short(Odds)}</color>";
            ui.fullText.text = Tier >= 4 ? "1 IN " + Kit.Full(Odds) : "";
            dimTarget = 0.15f;
            backlight.gameObject.SetActive(false);
            ui.oddsText.rectTransform.anchoredPosition = new Vector2(0, Tier >= 4 ? 230 : 300);
            StartCoroutine(PopText(ui.titleText, 0.3f, 0.3f));
            confetti.transform.position = new Vector3(0, 13f, Distance + 1f);
            confetti.Emit(Tier >= 5 ? 500 : Tier >= 3 ? 220 : 80);
            if (Odds > BestOdds) { BestOdds = Odds; BestTier = Tier; }
#if UNITY_EDITOR && SATURA_SPACE
            SaturaLog.Log("sprout", $"revealed {rare.title} {rare.plant} 1 in {Kit.Short(Odds)}");
#endif
            yield return new WaitForSeconds(Tier >= 5 ? 3.0f : 2.0f);
            ui.flash.color = new Color(1, 1, 1, 0.8f);
            StartRound();
        }

        void ApplyZone(float z)
        {
            var zone = World.Blend(z);
            var darkSky = new Color(0.02f, 0.015f, 0.05f);
            cam.backgroundColor = Color.Lerp(zone.sky, darkSky, dim);
            RenderSettings.fogColor = Color.Lerp(zone.fog, darkSky, dim);
            RenderSettings.ambientLight = Color.Lerp(zone.ambient, zone.ambient * 0.35f, dim);
            sun.color = zone.light;
            sun.intensity = Mathf.Lerp(zone.lightIntensity, zone.lightIntensity * 0.35f, dim);
        }

        public string Describe() =>
            $"phase={phase} meter={MeterValue:0.00} power={Power:0.00} perfect={Perfect} distance={Distance:0} seedZ={SeedPosition.z:0} luck={Kit.Short(Luck)} rings={RingsHit} " +
            $"odds={(Odds > 0 ? Kit.Short(Odds) : "-")} tier={(Tier >= 0 ? Models.Tiers[Tier].title : "-")} rounds={Rounds} best={(BestTier >= 0 ? Models.Tiers[BestTier].title + " 1 in " + Kit.Short(BestOdds) : "-")}";
    }
}
