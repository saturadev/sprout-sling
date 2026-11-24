using System.Collections;
using UnityEngine;
#if UNITY_EDITOR && SATURA_SPACE
using SaturaSpace;
#endif

namespace SproutSling
{
    public static class Sprout
    {
#if UNITY_EDITOR && SATURA_SPACE
        static SproutGame G => SproutGame.Instance ? SproutGame.Instance : Object.FindFirstObjectByType<SproutGame>();

        [SaturaHandle("Back on the launch pad with the power meter running. Optional random seed for repeatable rolls.")]
        public static string StartRound(int randomSeed = 0)
        {
            if (randomSeed != 0) Random.InitState(randomSeed);
            G.BotControlled = false;
            G.StartRound();
            return G.Describe();
        }

        [SaturaHandle("Throws now with the given power (0..1, 0.9+ is PERFECT)")]
        public static string Throw(float power)
        {
            G.Throw(power);
            return G.Describe();
        }

        [SaturaHandle("State: phase, meter, distance, luck, rings, odds, tier, best")]
        public static string GetState() => G.Describe();

        [SaturaHandle("Waits until the current throw has landed and been revealed, then returns the result")]
        public static IEnumerator WaitForResult(float timeout = 30f)
        {
            float t = 0;
            while (G.phase == SproutGame.Phase.Aim && t < 1f) { t += Time.deltaTime; yield return null; }
            while (G.phase != SproutGame.Phase.Reveal && t < timeout) { t += Time.deltaTime; yield return null; }
            string result = G.Describe();
            while (G.phase == SproutGame.Phase.Reveal && t < timeout) { t += Time.deltaTime; result = G.Describe(); yield return null; }
            yield return result;
        }

        /// The bot stops the real meter when the marker crosses its target.
        static IEnumerator BotThrow(float target)
        {
            var g = G;
            g.BotControlled = true;
            while (g.phase != SproutGame.Phase.Aim) yield return null;
            float wait = 0;
            while (wait < 0.45f) { wait += Time.deltaTime; yield return null; }
            float prev = g.MeterValue;
            while (true)
            {
                yield return null;
                float v = g.MeterValue;
                bool crossed = (prev < target && v >= target) || (prev > target && v <= target) || (target >= 0.95f && v >= target);
                prev = v;
                if (crossed) break;
            }
            g.Press();
            while (g.phase != SproutGame.Phase.Reveal) yield return null;
            while (g.phase == SproutGame.Phase.Reveal) yield return null;
        }

        [SaturaHandle("Bot plays one round, stopping the meter at target (0..1). Returns the result.")]
        public static IEnumerator BotRound(float target)
        {
            yield return BotThrow(target);
            yield return $"after: {G.Describe()}";
        }

        [SaturaHandle("Demo for filming: a GREAT throw into the Glow Grotto, then a PERFECT throw to the Starless Rift and a SECRET plant")]
        public static IEnumerator DemoRun()
        {
            Random.InitState(20261124);
            G.BotControlled = true;
            G.StartRound();
            yield return BotThrow(0.74f);
            string first = G.Describe();
            yield return BotThrow(0.985f);
            G.BotControlled = false;
            yield return $"first: {first}\nsecond: {G.Describe()}";
        }
#endif
    }
}
