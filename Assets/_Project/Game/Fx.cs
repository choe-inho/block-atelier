using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 이펙트 모음: 블록 조각, 불꽃, 페인트 방울 비행, 충격파, 붓질 빛줄기, 화면 흔들림, 떠오르는 글자.
    /// 강도와 타이밍 값은 전부 여기 모아서 한 곳에서 다듬는다.
    /// </summary>
    public sealed class Fx : MonoBehaviour
    {
        // ---- 조절값 ----
        public static float DropTime = 0.52f;
        public static float DropStagger = 0.02f;
        public static float ShakePerLine = 0.07f;
        public static float ShakeExplode = 0.22f;

        ParticleSystem chunks, sparks, confetti;
        Canvas textCanvas;
        Camera cam;
        Vector3 camBase;
        float shakeAmp, shakeTime, shakeDur;

        public void Init(Camera camera)
        {
            cam = camera;
            chunks = MakePS("Chunks", Gfx.ParticleSquareMat, 25, 2.2f, true);
            sparks = MakePS("Sparks", Gfx.ParticleMat, 26, 0.4f, false);
            confetti = MakePS("Confetti", Gfx.ParticleSquareMat, 60, 0.9f, true);
            textCanvas = UI.WorldCanvas("FxText", transform, 70);
            MakeAmbient();
        }

        public void SetCameraBase(Vector3 p) { camBase = p; }

        /// <summary>배경에 천천히 떠다니는 먼지 빛 (화실 분위기)</summary>
        void MakeAmbient()
        {
            var go = new GameObject("Ambient");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -12f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 1.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.65f, 1f, 0.07f), new Color(1f, 0.85f, 0.6f, 0.12f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            main.maxParticles = 60;
            var em = ps.emission;
            em.rateOverTime = 3f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 0.5f, 0.1f);
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.2f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Gfx.ParticleMat;
            r.sortingOrder = -5;
            ps.Play();
        }

        /// <summary>레벨을 다시 시작할 때 남은 연출 정리</summary>
        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var ch = transform.GetChild(i);
                if (ch.GetComponent<ParticleSystem>() != null || ch == textCanvas.transform) continue;
                Destroy(ch.gameObject);
            }
            for (int i = textCanvas.transform.childCount - 1; i >= 0; i--) Destroy(textCanvas.transform.GetChild(i).gameObject);
            chunks.Clear();
            sparks.Clear();
            confetti.Clear();
            shakeTime = 0f;
        }

        ParticleSystem MakePS(string name, Material mat, int order, float gravity, bool spin)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = 0.6f;
            main.startSpeed = 0f;
            main.startSize = 0.2f;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 3000;
            var em = ps.emission;
            em.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.8f), new Keyframe(1f, 0f)));

            if (spin)
            {
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            }

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.12f;
            limit.limit = 30f;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.sortingOrder = order;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, Color c, float size, float life)
        {
            var ep = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = vel,
                startColor = c,
                startSize = size,
                startLifetime = life,
                rotation = UnityEngine.Random.Range(0f, 360f),
                applyShapeToPosition = false
            };
            ps.Emit(ep, 1);
        }

        /// <summary>블록이 부서지는 조각 + 불꽃</summary>
        public void Shatter(Vector3 pos, Color c, float power)
        {
            int n = Mathf.RoundToInt(4 * power);
            for (int i = 0; i < n; i++)
            {
                var dir = UnityEngine.Random.insideUnitCircle.normalized;
                var v = new Vector3(dir.x, dir.y * 0.7f + 0.9f, 0f) * UnityEngine.Random.Range(2.5f, 6.5f) * power;
                Emit(chunks, pos + (Vector3)(dir * 0.2f), v, c, UnityEngine.Random.Range(0.14f, 0.28f), UnityEngine.Random.Range(0.4f, 0.65f));
            }
            for (int i = 0; i < 3; i++)
            {
                var dir = UnityEngine.Random.insideUnitCircle;
                Emit(sparks, pos, (Vector3)dir * UnityEngine.Random.Range(1f, 3.5f), Color.Lerp(c, Color.white, 0.6f), UnityEngine.Random.Range(0.35f, 0.7f), 0.35f);
            }
        }

        public void Puff(Vector3 pos, Color c, int n, float speed, float size)
        {
            for (int i = 0; i < n; i++)
            {
                var dir = UnityEngine.Random.insideUnitCircle.normalized;
                Emit(sparks, pos, (Vector3)dir * speed * UnityEngine.Random.Range(0.5f, 1f), c, size * UnityEngine.Random.Range(0.6f, 1.2f), UnityEngine.Random.Range(0.25f, 0.45f));
            }
        }

        public void Splash(Vector3 pos, Color c)
        {
            for (int i = 0; i < 4; i++)
            {
                var dir = UnityEngine.Random.insideUnitCircle.normalized;
                Emit(chunks, pos, new Vector3(dir.x, Mathf.Abs(dir.y) + 0.5f, 0f) * UnityEngine.Random.Range(1.2f, 2.6f), c, UnityEngine.Random.Range(0.06f, 0.11f), 0.4f);
            }
            Emit(sparks, pos, Vector3.zero, Color.Lerp(c, Color.white, 0.5f), 0.55f, 0.22f);
            var ring = Gfx.MakeSprite("Ripple", transform, Gfx.Ring, c, 32);
            ring.transform.position = pos;
            var rt = ring.transform;
            Tween.Run(0.28f, x =>
            {
                rt.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.75f, Ease.OutCubic(x));
                var k = Color.Lerp(c, Color.white, 0.4f) * 1.4f; k.a = 1f - x;
                ring.color = k;
            }, () => Destroy(ring.gameObject));
        }

        /// <summary>화면 전체가 잠깐 밝아진다 (큰 폭발)</summary>
        public void ScreenFlash(Color c, float alpha, float dur)
        {
            var sr = Gfx.MakeSprite("ScreenFlash", transform, Gfx.Pixel, c, 75);
            float h = cam.orthographicSize * 2f + 2f;
            sr.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            sr.transform.localScale = new Vector3(h * cam.aspect + 2f, h, 1f);
            Tween.Run(dur, x =>
            {
                var k = c; k.a = alpha * (1f - Ease.OutQuad(x));
                sr.color = k;
            }, () => Destroy(sr.gameObject));
        }

        public void Confetti(Vector3 center, Color[] colors, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var c = colors[UnityEngine.Random.Range(0, colors.Length)];
                var v = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(6f, 13f), 0f);
                Emit(confetti, center + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, 0f), v, c, UnityEngine.Random.Range(0.14f, 0.26f), UnityEngine.Random.Range(1.6f, 2.6f));
            }
        }

        /// <summary>퍼져 나가는 고리 (십자 폭발)</summary>
        public void Shockwave(Vector3 pos, Color c, float size, float dur = 0.45f)
        {
            var sr = Gfx.MakeSprite("Wave", transform, Gfx.Ring, c, 27);
            sr.transform.position = pos;
            var t = sr.transform;
            Tween.Run(dur, x =>
            {
                t.localScale = Vector3.one * Mathf.Lerp(0.3f, size, Ease.OutCubic(x));
                var k = c * 1.8f; k.a = (1f - x) * (1f - x) * c.a * 0.8f;
                sr.color = k;
            }, () => Destroy(sr.gameObject));

            var flash = Gfx.MakeSprite("Flash", transform, Gfx.Soft, Color.white, 28);
            flash.transform.position = pos;
            var ft = flash.transform;
            Tween.Run(dur * 0.6f, x =>
            {
                ft.localScale = Vector3.one * Mathf.Lerp(1f, size * 0.7f, Ease.OutCubic(x));
                flash.color = new Color(2.2f, 2.1f, 1.8f, 0.9f * (1f - x));
            }, () => Destroy(flash.gameObject));
        }

        /// <summary>붓질: 한 줄을 빛줄기가 쓸고 지나간다</summary>
        public void Sweep(Vector3 from, Vector3 to, Color c, float thickness, float dur = 0.28f)
        {
            var sr = Gfx.MakeSprite("Sweep", transform, Gfx.Streak, Color.Lerp(c, Color.white, 0.5f) * 2.2f, 29);
            var t = sr.transform;
            var dir = (to - from).normalized;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            t.rotation = Quaternion.Euler(0f, 0f, ang);
            float len = 4f;
            t.localScale = new Vector3(len / 4f, thickness, 1f);
            Tween.Run(dur, x =>
            {
                t.position = Vector3.LerpUnclamped(from - dir * 1.5f, to + dir * 0.5f, Ease.InOutCubic(x));
                var k = sr.color; k.a = Mathf.Clamp01(Ease.Bump(Mathf.Clamp01(x * 1.1f)) * 1.8f);
                sr.color = k;
            }, () => Destroy(sr.gameObject));
        }

        public void Shake(float amp, float dur = 0.28f)
        {
            if (amp > shakeAmp * (shakeTime / Mathf.Max(shakeDur, 0.001f)))
            {
                shakeAmp = amp;
                shakeDur = dur;
                shakeTime = dur;
            }
        }

        /// <summary>순간 멈춤 (타격감). 실제 시간 기준.</summary>
        public void HitStop(float seconds, float scale = 0.05f)
        {
            Time.timeScale = scale;
            Tween.After(seconds, () => Time.timeScale = GameRoot.BaseTimeScale, true);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = Mathf.Max(0f, shakeTime / shakeDur);
                float a = shakeAmp * k * k;
                float tt = Time.unscaledTime * 38f;
                cam.transform.position = camBase + new Vector3((Mathf.PerlinNoise(tt, 0.3f) - 0.5f) * 2f * a, (Mathf.PerlinNoise(0.7f, tt) - 0.5f) * 2f * a, 0f);
            }
            else cam.transform.position = camBase;
        }

        /// <summary>
        /// 페인트 방울 하나를 from에서 to로 날린다. 꼬리(트레일)가 따라오고, 도착하면 onArrive.
        /// </summary>
        public void Drop(Vector3 from, Vector3 to, Color c, float delay, Action onArrive)
        {
            Tween.After(delay, () =>
            {
                var go = new GameObject("Drop");
                go.transform.SetParent(transform, false);
                go.transform.position = from;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Gfx.Circle;
                sr.color = Color.Lerp(c, Color.white, 0.25f) * 1.25f;
                sr.sortingOrder = 31;

                var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
                glow.transform.SetParent(go.transform, false);
                glow.sprite = Gfx.Soft;
                var gc = c * 1.4f; gc.a = 0.55f;
                glow.color = gc;
                glow.sortingOrder = 30;
                glow.transform.localScale = Vector3.one * 2.6f;

                var trail = go.AddComponent<TrailRenderer>();
                trail.sharedMaterial = Gfx.ParticleMat;
                trail.time = 0.16f;
                trail.minVertexDistance = 0.03f;
                trail.widthMultiplier = 0.26f;
                trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                          new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
                trail.colorGradient = g;
                trail.sortingOrder = 30;
                trail.numCapVertices = 4;

                var side = UnityEngine.Random.Range(-1f, 1f);
                var mid = (from + to) * 0.5f;
                var perp = new Vector3(-(to - from).y, (to - from).x, 0f).normalized;
                var ctrl = mid + perp * side * 1.6f + Vector3.up * 1.2f;
                float dur = DropTime * UnityEngine.Random.Range(0.85f, 1.15f);
                var t = go.transform;
                Tween.Run(dur, x =>
                {
                    float e = Ease.InOutCubic(x);
                    var a = Vector3.Lerp(from, ctrl, e);
                    var b = Vector3.Lerp(ctrl, to, e);
                    t.position = Vector3.Lerp(a, b, e);
                    t.localScale = Vector3.one * Mathf.Lerp(0.34f, 0.2f, x);
                }, () =>
                {
                    sr.enabled = false;
                    glow.enabled = false;
                    Destroy(go, trail.time + 0.05f);
                    if (onArrive != null) onArrive();
                });
            });
        }

        /// <summary>필요 없는 색의 방울: 위로 조금 튀었다가 흐려진다</summary>
        public void Fizzle(Vector3 from, Color c, float delay)
        {
            Tween.After(delay, () =>
            {
                var sr = Gfx.MakeSprite("Fizzle", transform, Gfx.Circle, c, 31);
                sr.transform.position = from;
                var t = sr.transform;
                var to = from + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0.9f, 0f);
                Tween.Run(0.4f, x =>
                {
                    t.position = Vector3.Lerp(from, to, Ease.OutCubic(x));
                    t.localScale = Vector3.one * Mathf.Lerp(0.3f, 0.05f, x);
                    var k = Color.Lerp(c, UI.GrayBlock, x); k.a = 1f - x;
                    sr.color = k;
                }, () => Destroy(sr.gameObject));
            });
        }

        /// <summary>떠오르는 글자 (붓질, 폭발, 콤보)</summary>
        public void Text(string text, Vector3 pos, Color color, float size, float dur = 0.9f)
        {
            var local = textCanvas.transform.InverseTransformPoint(pos) * UI.CanvasScale;
            var label = UI.Label(textCanvas, text, local, size, color, TextAnchor.MiddleCenter, 8f, true);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.08f, 0.2f, 0.85f);
            outline.effectDistance = new Vector2(3f, -3f);
            var rt = label.rectTransform;
            var start = rt.anchoredPosition;
            Tween.Run(dur, x =>
            {
                float pop = x < 0.18f ? Ease.OutBack(x / 0.18f) : 1f;
                float settle = x < 0.18f ? 0f : Mathf.Clamp01((x - 0.18f) / 0.12f);
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1.2f, pop) * Mathf.Lerp(1f, 1f / 1.2f, settle);
                rt.anchoredPosition = start + new Vector2(0f, 60f * Ease.OutCubic(x));
                var k = color; k.a = x < 0.7f ? 1f : 1f - (x - 0.7f) / 0.3f;
                label.color = k;
            }, () => Destroy(label.gameObject));
        }
    }
}
