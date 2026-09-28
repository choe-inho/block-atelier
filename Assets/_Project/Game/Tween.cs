using System;
using System.Collections;
using UnityEngine;

namespace BlockAtelier.Game
{
    /// <summary>외부 트윈 라이브러리 없이 쓰는 작은 트윈. 코루틴 하나가 트윈 하나.</summary>
    public sealed class Tween : MonoBehaviour
    {
        static Tween inst;

        static Tween I
        {
            get
            {
                if (inst == null)
                {
                    var go = new GameObject("Tween");
                    DontDestroyOnLoad(go);
                    inst = go.AddComponent<Tween>();
                }
                return inst;
            }
        }

        /// <summary>dur초 동안 step(0→1)을 부른다. 대상이 사라져 예외가 나면 조용히 멈춘다.</summary>
        public static Coroutine Run(float dur, Action<float> step, Action done = null, float delay = 0f, bool realtime = false)
        {
            return I.StartCoroutine(I.Co(dur, step, done, delay, realtime));
        }

        public static Coroutine After(float delay, Action action, bool realtime = false)
        {
            return I.StartCoroutine(I.Co(0f, null, action, delay, realtime));
        }

        public static void StopAll() { if (inst != null) inst.StopAllCoroutines(); }

        IEnumerator Co(float dur, Action<float> step, Action done, float delay, bool realtime)
        {
            if (delay > 0f)
            {
                if (realtime) yield return new WaitForSecondsRealtime(delay);
                else yield return new WaitForSeconds(delay);
            }
            float t = 0f;
            while (t < dur)
            {
                if (!Safe(step, t / dur)) yield break;
                yield return null;
                t += realtime ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            if (!Safe(step, 1f)) yield break;
            if (done != null)
            {
                try { done(); }
                catch (MissingReferenceException) { }
                catch (NullReferenceException) { }
            }
        }

        static bool Safe(Action<float> step, float x)
        {
            if (step == null) return true;
            try { step(Mathf.Clamp01(x)); return true; }
            catch (MissingReferenceException) { return false; }
            catch (NullReferenceException) { return false; }
        }
    }

    public static class Ease
    {
        public static float OutCubic(float t) { t = 1f - t; return 1f - t * t * t; }
        public static float InCubic(float t) { return t * t * t; }
        public static float InOutCubic(float t) { return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f; }
        public static float OutQuad(float t) { return 1f - (1f - t) * (1f - t); }
        public static float InQuad(float t) { return t * t; }

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>0→1→0 한 번 튀는 곡선</summary>
        public static float Bump(float t) { return Mathf.Sin(t * Mathf.PI); }
    }
}
