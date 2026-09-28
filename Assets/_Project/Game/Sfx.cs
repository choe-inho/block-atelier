using UnityEngine;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 코드로 합성한 효과음. 프로토타입용이라 출시 전에는 녹음·구매한 소리로 바꾼다.
    /// 페인트 방울 소리는 5음계를 타고 올라가서, 많이 칠할수록 음이 높아진다.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 44100;
        static AudioSource[] pool;
        static int next;
        static bool ready;

        public static AudioClip Place, Clear, Plip, Pop, Win, Boom, Whoosh, Fail, Pick, Tick;

        static readonly float[] Scale = { 1f, 1.125f, 1.25f, 1.5f, 1.6667f, 2f, 2.25f, 2.5f, 3f, 3.3333f, 4f };

        public static bool Muted;

        public static void Init(Transform parent)
        {
            if (ready) return;
            ready = true;
            pool = new AudioSource[16];
            for (int i = 0; i < pool.Length; i++)
            {
                var src = parent.gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                pool[i] = src;
            }

            Place = Make("place", 0.12f, (t, i) => Sine(t, Mathf.Lerp(190f, 80f, t / 0.12f)) * Env(t, 0.002f, 0.1f) * 0.9f + Noise(i) * Env(t, 0.001f, 0.015f) * 0.25f);
            Pick = Make("pick", 0.06f, (t, i) => Sine(t, Mathf.Lerp(520f, 700f, t / 0.06f)) * Env(t, 0.002f, 0.05f) * 0.35f);
            Tick = Make("tick", 0.04f, (t, i) => Sine(t, 1400f) * Env(t, 0.001f, 0.03f) * 0.25f);
            Clear = Make("clear", 0.55f, (t, i) =>
                (Sine(t, 523.25f) + Sine(t, 659.25f) * 0.8f + Sine(t, 783.99f) * 0.7f + Sine(t, 1046.5f) * 0.35f) * Env(t, 0.004f, 0.5f) * 0.28f
                + Noise(i) * Env(t, 0.001f, 0.05f) * 0.12f);
            Plip = Make("plip", 0.09f, (t, i) => (Sine(t, 880f) + Sine(t, 1760f) * 0.25f) * Env(t, 0.002f, 0.08f) * 0.3f);
            Pop = Make("pop", 0.18f, (t, i) => Sine(t, Mathf.Lerp(600f, 1200f, t / 0.18f)) * Env(t, 0.003f, 0.16f) * 0.4f);
            Boom = Make("boom", 0.45f, (t, i) => Sine(t, Mathf.Lerp(90f, 40f, t / 0.45f)) * Env(t, 0.003f, 0.4f) * 0.9f + LowNoise(i) * Env(t, 0.002f, 0.25f) * 0.5f);
            Whoosh = Make("whoosh", 0.3f, (t, i) => LowNoise(i) * Mathf.Sin(Mathf.PI * t / 0.3f) * 0.35f + Sine(t, Mathf.Lerp(300f, 900f, t / 0.3f)) * Mathf.Sin(Mathf.PI * t / 0.3f) * 0.08f);
            Win = Make("win", 1.1f, (t, i) =>
            {
                float s = 0f;
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                for (int n = 0; n < notes.Length; n++)
                {
                    float start = n * 0.11f;
                    if (t >= start) s += (Sine(t - start, notes[n]) + Sine(t - start, notes[n] * 2f) * 0.2f) * Env(t - start, 0.004f, 0.7f);
                }
                return s * 0.22f;
            });
            Fail = Make("fail", 0.6f, (t, i) =>
                (t < 0.25f ? Sine(t, 392f) * Env(t, 0.005f, 0.24f) : Sine(t - 0.25f, 311.13f) * Env(t - 0.25f, 0.005f, 0.34f)) * 0.3f);
        }

        delegate float Gen(float t, int i);

        static AudioClip Make(string name, float dur, Gen gen)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            var data = new float[n];
            seed = 12345;
            lp = 0f;
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(gen(i / (float)Rate, i), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sine(float t, float f) { return Mathf.Sin(2f * Mathf.PI * f * t); }

        /// <summary>빠른 어택, 지수 감쇠</summary>
        static float Env(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            return Mathf.Exp(-(t - attack) / (decay * 0.3f));
        }

        static uint seed = 12345;
        static float Noise(int i)
        {
            seed = seed * 1664525u + 1013904223u;
            return ((seed >> 8) / 8388608f) - 1f;
        }

        static float lp;
        static float LowNoise(int i)
        {
            lp += (Noise(i) - lp) * 0.08f;
            return lp * 3f;
        }

        public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (!ready || Muted || clip == null) return;
            var src = pool[next];
            next = (next + 1) % pool.Length;
            src.pitch = pitch;
            src.volume = volume;
            src.clip = clip;
            src.Play();
        }

        /// <summary>n번째 페인트 방울 소리 (5음계로 올라감)</summary>
        public static void PlayNote(int n, float volume = 0.8f)
        {
            int octave = n / Scale.Length;
            float p = Scale[n % Scale.Length] * (octave > 0 ? 1.5f : 1f);
            Play(Plip, volume, Mathf.Min(p, 4f) * 0.75f);
        }
    }
}
