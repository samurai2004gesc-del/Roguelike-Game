using System.Collections.Generic;
using UnityEngine;

namespace Umbral
{
    public enum SfxId
    {
        Hit, HitArmor, Crit, PlayerHurt, Dodge, ArmorBreak, Freeze, Thunder, Swing, SwingHeavy, Dash, Cast, Bind,
        Magic, Projectile, Explosion, EnemyDeath, Pickup, Gold, Boon, DoorOpen, RoomClear, UiMove, UiConfirm, UiBack,
        Telegraph, BossRoar, Heal, Charge, ChargeReady, Fire, Water, Whisper, Growl, Bell, PlayerDeath, LevelUp, Portal, Count
    }

    /// <summary>
    /// Sonido sintetizado por código (sin archivos de audio): golpes, tajos, conjuros, campanas, rugidos.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 22050;
        static AudioClip[] clips;
        static AudioSource[] pool;
        static int next;
        static readonly float[] lastPlayed = new float[(int)SfxId.Count];
        public static float volume = 0.8f;

        public static void Init(Transform parent)
        {
            var root = parent.Child("SFX");
            pool = new AudioSource[24];
            for (int i = 0; i < pool.Length; i++)
            {
                var s = root.gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0f;
                pool[i] = s;
            }
            clips = new AudioClip[(int)SfxId.Count];
            for (int i = 0; i < clips.Length; i++) clips[i] = Generate((SfxId)i);
        }

        public static void Play(SfxId id, Vector3? at = null, float vol = 1f, float pitch = 1f)
        {
            if (pool == null || clips == null) return;
            int i = (int)id;
            if (Time.unscaledTime - lastPlayed[i] < 0.035f) return; // evita saturación
            lastPlayed[i] = Time.unscaledTime;
            var s = pool[next];
            next = (next + 1) % pool.Length;
            s.clip = clips[i];
            s.volume = vol * volume * GameSettings.MasterVolume;
            s.pitch = pitch * Random.Range(0.93f, 1.07f);
            float pan = 0;
            if (at.HasValue && Camera.main != null)
            {
                var vp = Camera.main.WorldToViewportPoint(at.Value);
                pan = Mathf.Clamp((vp.x - 0.5f) * 1.2f, -0.7f, 0.7f);
            }
            s.panStereo = pan;
            s.Play();
        }

        // ---------------- Síntesis ----------------
        static float Noise(System.Random r) => (float)r.NextDouble() * 2f - 1f;

        static AudioClip Clip(string name, float[] data)
        {
            float peak = 0.0001f;
            foreach (var d in data) peak = Mathf.Max(peak, Mathf.Abs(d));
            float norm = 0.9f / peak;
            for (int i = 0; i < data.Length; i++) data[i] *= norm;
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        delegate float SampleFn(float t, float k, System.Random r);

        static float[] Render(float seconds, SampleFn f, int seed = 1)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var d = new float[n];
            var r = new System.Random(seed);
            for (int i = 0; i < n; i++) { float t = (float)i / Rate; d[i] = f(t, t / seconds, r); }
            return d;
        }

        static float[] LowPass(float[] d, float a)
        {
            float y = 0;
            for (int i = 0; i < d.Length; i++) { y += a * (d[i] - y); d[i] = y; }
            return d;
        }

        static float Env(float k, float attack, float decayPow) => (k < attack ? k / attack : Mathf.Pow(1f - (k - attack) / (1f - attack), decayPow));
        static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        static AudioClip Generate(SfxId id)
        {
            float[] d;
            switch (id)
            {
                case SfxId.Hit:
                    d = Render(0.16f, (t, k, r) => (Noise(r) * 0.6f + Sin(160 - 100 * k, t) * 0.9f) * Env(k, 0.02f, 3f));
                    LowPass(d, 0.35f); break;
                case SfxId.HitArmor:
                    d = Render(0.2f, (t, k, r) => (Sin(900, t) * 0.4f + Sin(1370, t) * 0.3f + Noise(r) * 0.4f) * Env(k, 0.01f, 4f)); break;
                case SfxId.Crit:
                    d = Render(0.3f, (t, k, r) => (Noise(r) * 0.5f * (1 - k) + Sin(220 - 120 * k, t) + Sin(1760, t) * 0.25f * (1 - k)) * Env(k, 0.01f, 2.5f));
                    break;
                case SfxId.PlayerHurt:
                    d = Render(0.3f, (t, k, r) => (Sin(140 - 60 * k, t) + Noise(r) * 0.5f + Sin(311, t) * 0.3f) * Env(k, 0.02f, 2f));
                    LowPass(d, 0.3f); break;
                case SfxId.Dodge:
                    d = Render(0.25f, (t, k, r) => Noise(r) * Mathf.Sin(k * Mathf.PI) * 0.6f); LowPass(d, 0.15f); break;
                case SfxId.ArmorBreak:
                    d = Render(0.5f, (t, k, r) => (Noise(r) * 0.7f + Sin(600 * (1 - k * 0.5f), t) * 0.4f + Sin(1200 * (1 - k * 0.3f), t) * 0.3f) * Env(k, 0.01f, 2f)); break;
                case SfxId.Freeze:
                    d = Render(0.45f, (t, k, r) => (Sin(2400 + 800 * k, t) * 0.3f + Sin(3100, t) * 0.2f + Noise(r) * 0.3f) * Env(k, 0.02f, 2f)); break;
                case SfxId.Thunder:
                    d = Render(0.7f, (t, k, r) => (Noise(r) * (k < 0.05f ? 1f : 0.6f) + Sin(55, t) * 0.5f) * Env(k, 0.005f, 1.5f)); LowPass(d, 0.25f); break;
                case SfxId.Swing:
                    d = Render(0.2f, (t, k, r) => Noise(r) * Mathf.Sin(k * Mathf.PI)); LowPass(d, 0.22f); break;
                case SfxId.SwingHeavy:
                    d = Render(0.35f, (t, k, r) => (Noise(r) + Sin(90, t) * 0.3f) * Mathf.Sin(k * Mathf.PI)); LowPass(d, 0.12f); break;
                case SfxId.Dash:
                    d = Render(0.25f, (t, k, r) => Noise(r) * Mathf.Pow(1 - k, 2) * (0.5f + 0.5f * Sin(30, t))); LowPass(d, 0.1f); break;
                case SfxId.Cast:
                    d = Render(0.5f, (t, k, r) => (Sin(440 + 440 * k, t) * 0.4f + Sin(660 + 660 * k, t) * 0.3f + Sin(990, t) * 0.15f) * Env(k, 0.05f, 2f)); break;
                case SfxId.Bind:
                    d = Render(0.6f, (t, k, r) => (Sin(220, t) * 0.5f + Sin(330.5f, t) * 0.4f + Sin(440.7f, t) * 0.3f) * Env(k, 0.02f, 1.5f) * (0.7f + 0.3f * Sin(12, t))); break;
                case SfxId.Magic:
                    d = Render(0.4f, (t, k, r) => (Sin(880 * (1 + k), t) * 0.4f + Sin(1320, t) * 0.2f) * Env(k, 0.03f, 2f)); break;
                case SfxId.Projectile:
                    d = Render(0.18f, (t, k, r) => (Sin(700 - 400 * k, t) * 0.6f + Noise(r) * 0.2f) * Env(k, 0.02f, 2f)); break;
                case SfxId.Explosion:
                    d = Render(0.8f, (t, k, r) => (Noise(r) + Sin(60 - 30 * k, t)) * Env(k, 0.005f, 2f)); LowPass(d, 0.18f); break;
                case SfxId.EnemyDeath:
                    d = Render(0.5f, (t, k, r) => (Sin(180 - 140 * k, t) * 0.7f + Noise(r) * 0.4f * (1 - k) + Sin(90 - 60 * k + 8 * Sin(9, t), t) * 0.4f) * Env(k, 0.02f, 1.6f)); LowPass(d, 0.3f); break;
                case SfxId.Pickup:
                    d = Render(0.35f, (t, k, r) => (Sin(k < 0.3f ? 988 : 1318, t) * 0.5f + Sin(k < 0.3f ? 1976 : 2636, t) * 0.15f) * Env(k, 0.01f, 1.5f)); break;
                case SfxId.Gold:
                    d = Render(0.3f, (t, k, r) => (Sin(1568, t) * 0.4f + Sin(2093, t) * 0.3f + Sin(3136, t) * 0.1f) * Env(k, 0.005f, 3f)); break;
                case SfxId.Boon:
                    d = Render(1.4f, (t, k, r) =>
                    {
                        float s = 0; float[] ch = { 261.6f, 311.1f, 392f, 466.2f, 587.3f };
                        for (int i = 0; i < ch.Length; i++) s += Sin(ch[i] * (1 + 0.003f * i), t) * (k > i * 0.08f ? 1 : 0) * 0.22f;
                        return s * Env(k, 0.1f, 1.3f);
                    }); break;
                case SfxId.DoorOpen:
                    d = Render(0.9f, (t, k, r) => (Sin(70 + 20 * Sin(3, t), t) * 0.6f + Noise(r) * 0.3f) * Env(k, 0.2f, 1.5f)); LowPass(d, 0.12f); break;
                case SfxId.RoomClear:
                    d = Render(1.6f, (t, k, r) =>
                    {
                        float s = Sin(196, t) * 0.4f + Sin(293.7f, t) * 0.3f + Sin(392, t) * 0.25f + Sin(587.3f, t) * 0.15f * (k > 0.15f ? 1 : 0);
                        return s * Env(k, 0.03f, 1.6f);
                    }); break;
                case SfxId.UiMove:
                    d = Render(0.06f, (t, k, r) => Sin(1200, t) * Env(k, 0.05f, 2f)); break;
                case SfxId.UiConfirm:
                    d = Render(0.2f, (t, k, r) => (Sin(k < 0.4f ? 660 : 990, t)) * Env(k, 0.02f, 2f)); break;
                case SfxId.UiBack:
                    d = Render(0.15f, (t, k, r) => Sin(500 - 200 * k, t) * Env(k, 0.02f, 2f)); break;
                case SfxId.Telegraph:
                    d = Render(0.3f, (t, k, r) => (Sin(300 + 300 * k, t) * 0.5f) * Env(k, 0.1f, 1f)); break;
                case SfxId.BossRoar:
                    d = Render(1.8f, (t, k, r) => (Sin(55 + 15 * Sin(5, t), t) * 0.7f + Sin(82.5f + 9 * Sin(7, t), t) * 0.5f + Noise(r) * 0.5f) * Env(k, 0.15f, 1.2f)); LowPass(d, 0.2f); break;
                case SfxId.Heal:
                    d = Render(0.6f, (t, k, r) => (Sin(523, t) * 0.3f + Sin(659, t) * 0.3f + Sin(784, t) * 0.3f) * Env(k, 0.1f, 1.5f)); break;
                case SfxId.Charge:
                    d = Render(0.5f, (t, k, r) => (Sin(200 + 600 * k, t) * 0.4f + Noise(r) * 0.1f) * k); break;
                case SfxId.ChargeReady:
                    d = Render(0.25f, (t, k, r) => (Sin(1046, t) * 0.5f + Sin(1568, t) * 0.3f) * Env(k, 0.01f, 2f)); break;
                case SfxId.Fire:
                    d = Render(0.5f, (t, k, r) => Noise(r) * Env(k, 0.05f, 1.2f) * (0.6f + 0.4f * Mathf.PerlinNoise(t * 60, 0))); LowPass(d, 0.2f); break;
                case SfxId.Water:
                    d = Render(0.5f, (t, k, r) => (Noise(r) * 0.5f + Sin(300 + 200 * Sin(13, t), t) * 0.4f) * Env(k, 0.05f, 1.5f)); LowPass(d, 0.15f); break;
                case SfxId.Whisper:
                    d = Render(0.9f, (t, k, r) => Noise(r) * (0.5f + 0.5f * Sin(4, t)) * Env(k, 0.2f, 1f)); LowPass(d, 0.35f); break;
                case SfxId.Growl:
                    d = Render(0.6f, (t, k, r) => (Sin(90 + 10 * Noise(r), t) * 0.6f + Noise(r) * 0.4f) * Env(k, 0.1f, 1.2f)); LowPass(d, 0.15f); break;
                case SfxId.Bell:
                    d = Render(2.2f, (t, k, r) => (Sin(220, t) * 0.5f + Sin(220 * 2.76f, t) * 0.3f + Sin(220 * 5.4f, t) * 0.15f) * Mathf.Pow(1 - k, 2.5f)); break;
                case SfxId.PlayerDeath:
                    d = Render(2.0f, (t, k, r) => (Sin(110 * (1 - k * 0.5f), t) * 0.5f + Sin(164.8f * (1 - k * 0.5f), t) * 0.4f + Noise(r) * 0.2f * (1 - k)) * Env(k, 0.02f, 1.2f)); LowPass(d, 0.25f); break;
                case SfxId.LevelUp:
                    d = Render(0.8f, (t, k, r) => (Sin(k < 0.25f ? 523 : k < 0.5f ? 659 : k < 0.75f ? 784 : 1046, t) * 0.5f) * Env(k, 0.02f, 1f)); break;
                case SfxId.Portal:
                    d = Render(1.2f, (t, k, r) => (Sin(110 + 220 * k, t) * 0.4f + Sin(165 + 330 * k, t) * 0.3f + Noise(r) * 0.15f) * Mathf.Sin(k * Mathf.PI)); break;
                default:
                    d = Render(0.1f, (t, k, r) => 0f); break;
            }
            return Clip(id.ToString(), d);
        }
    }

    /// <summary>
    /// Música procedural: drones, coros disonantes, arpegios en modo frigio y percusión tribal para combate y jefes.
    /// </summary>
    public static class Music
    {
        const int Rate = 22050;
        static AudioSource a, b;
        static bool aActive = true;
        static string current;
        static readonly Dictionary<string, AudioClip> tracks = new Dictionary<string, AudioClip>();
        public static float volume = 0.55f;
        static float fadeT = 1f;

        public static void Init(Transform parent)
        {
            var root = parent.Child("Music");
            a = root.gameObject.AddComponent<AudioSource>();
            b = root.gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { a, b }) { s.loop = true; s.playOnAwake = false; s.volume = 0; }
        }

        public static void Play(string id)
        {
            if (a == null || current == id) return;
            current = id;
            var clip = Get(id);
            var to = aActive ? b : a;
            to.clip = clip;
            to.volume = 0;
            to.Play();
            aActive = !aActive;
            fadeT = 0;
        }

        public static void Stop() { current = null; fadeT = 0; aActive = !aActive; if (aActive) a.clip = null; else b.clip = null; }

        public static void Tick()
        {
            if (a == null) return;
            fadeT = Mathf.Min(1f, fadeT + Time.unscaledDeltaTime / 1.5f);
            var on = aActive ? a : b; var off = aActive ? b : a;
            float v = volume * GameSettings.MasterVolume * GameSettings.MusicVolume;
            on.volume = on.clip != null ? v * fadeT : 0;
            off.volume = v * (1f - fadeT);
            if (fadeT >= 1f && off.isPlaying) off.Stop();
        }

        static AudioClip Get(string id)
        {
            if (tracks.TryGetValue(id, out var c)) return c;
            c = Compose(id);
            tracks[id] = c;
            return c;
        }

        struct Style
        {
            public float root;          // Hz de la tónica
            public int[] scale;         // semitonos
            public float bpm;
            public float drums;         // 0..1
            public float arp;           // 0..1
            public float choir;         // 0..1
            public float bright;        // brillo del timbre
            public int seed;
        }

        static Style StyleFor(string id)
        {
            int[] phrygian = { 0, 1, 3, 5, 7, 8, 10 };
            int[] locrian = { 0, 1, 3, 5, 6, 8, 10 };
            int[] harmonic = { 0, 2, 3, 5, 7, 8, 11 };
            int[] dorian = { 0, 2, 3, 5, 7, 9, 10 };
            switch (id)
            {
                case "menu": return new Style { root = 55f, scale = harmonic, bpm = 60, drums = 0, arp = 0.3f, choir = 1f, bright = 0.3f, seed = 1 };
                case "hub": return new Style { root = 61.7f, scale = dorian, bpm = 72, drums = 0.1f, arp = 0.6f, choir = 0.6f, bright = 0.5f, seed = 2 };
                case "bosque": return new Style { root = 55f, scale = phrygian, bpm = 110, drums = 0.7f, arp = 0.8f, choir = 0.4f, bright = 0.6f, seed = 3 };
                case "rlyeh": return new Style { root = 49f, scale = harmonic, bpm = 96, drums = 0.6f, arp = 0.6f, choir = 0.7f, bright = 0.35f, seed = 4 };
                case "leng": return new Style { root = 58.3f, scale = locrian, bpm = 118, drums = 0.8f, arp = 0.7f, choir = 0.5f, bright = 0.7f, seed = 5 };
                case "azathoth": return new Style { root = 46.2f, scale = locrian, bpm = 128, drums = 0.9f, arp = 0.9f, choir = 0.8f, bright = 0.8f, seed = 6 };
                case "boss": return new Style { root = 41.2f, scale = phrygian, bpm = 140, drums = 1f, arp = 1f, choir = 1f, bright = 0.9f, seed = 7 };
                case "calm": return new Style { root = 65.4f, scale = dorian, bpm = 60, drums = 0f, arp = 0.4f, choir = 0.5f, bright = 0.4f, seed = 8 };
                default: return new Style { root = 55f, scale = phrygian, bpm = 100, drums = 0.5f, arp = 0.5f, choir = 0.5f, bright = 0.5f, seed = 9 };
            }
        }

        static float Saw(float ph) => 2f * (ph - Mathf.Floor(ph + 0.5f));

        static AudioClip Compose(string id)
        {
            var st = StyleFor(id);
            var rng = new System.Random(st.seed);
            float beat = 60f / st.bpm;
            int bars = 8;
            float seconds = beat * 4 * bars;
            int n = Mathf.CeilToInt(seconds * Rate);
            var d = new float[n];

            float Note(int degree, int octave)
            {
                int len = st.scale.Length;
                int o = Mathf.FloorToInt((float)degree / len);
                int idx = ((degree % len) + len) % len;
                return st.root * Mathf.Pow(2f, octave + o + st.scale[idx] / 12f);
            }

            // progresión de acordes (grados de la escala)
            int[] prog = { 0, 0, 5, 5, 3, 3, 1, 4 };
            for (int i = 0; i < prog.Length; i++) if (rng.NextDouble() < 0.3) prog[i] = rng.Next(0, 7);
            prog[0] = 0;

            // patrón de arpegio
            int[] arpPat = new int[16];
            for (int i = 0; i < 16; i++) arpPat[i] = new[] { 0, 2, 4, 7, 4, 2, 0, -3 }[rng.Next(8)];
            bool[] kick = new bool[16], snare = new bool[16], hat = new bool[16];
            for (int i = 0; i < 16; i++)
            {
                kick[i] = i % 4 == 0 || (rng.NextDouble() < 0.18 && i % 2 == 0);
                snare[i] = i % 8 == 4 || (rng.NextDouble() < 0.1);
                hat[i] = i % 2 == 0 || rng.NextDouble() < 0.3;
            }

            float lp = 0, lp2 = 0;
            var nr = new System.Random(st.seed * 7);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                int bar = Mathf.FloorToInt(t / (beat * 4)) % bars;
                float barT = Mathf.Max(0f, t - Mathf.FloorToInt(t / (beat * 4)) * beat * 4);
                int step = ((Mathf.FloorToInt(barT / (beat / 4)) % 16) + 16) % 16;
                float stepT = barT - step * (beat / 4);
                int chord = prog[bar];

                // drone grave (sierras desafinadas, filtradas)
                float f0 = Note(chord, 0);
                float drone = Saw(t * f0) * 0.5f + Saw(t * f0 * 1.005f) * 0.5f + Saw(t * f0 * 0.5f) * 0.6f;
                lp += 0.02f * (drone - lp);
                float s = lp * 0.55f;

                // coro / pad
                if (st.choir > 0)
                {
                    float pad = 0;
                    for (int v = 0; v < 3; v++)
                    {
                        float f = Note(chord + v * 2, 2);
                        pad += Mathf.Sin(2 * Mathf.PI * f * t + Mathf.Sin(t * (0.7f + v)) * 2f) * 0.33f;
                    }
                    float swell = 0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * t / (beat * 8));
                    s += pad * 0.22f * st.choir * swell;
                }

                // arpegio pulsado
                if (st.arp > 0 && (step % 2 == 0 || st.bpm > 100))
                {
                    float f = Note(chord + arpPat[step], 3);
                    float env = Mathf.Exp(-stepT * (10f - st.bright * 5f));
                    float tone = Mathf.Sin(2 * Mathf.PI * f * t) + Saw(t * f) * 0.35f * st.bright;
                    s += tone * env * 0.16f * st.arp;
                }

                // percusión tribal
                if (st.drums > 0)
                {
                    float noise = (float)nr.NextDouble() * 2 - 1;
                    if (kick[step]) s += Mathf.Sin(2 * Mathf.PI * (50f + 90f * Mathf.Exp(-stepT * 30f)) * stepT) * Mathf.Exp(-stepT * 8f) * 0.8f * st.drums;
                    if (snare[step]) { lp2 += 0.3f * (noise - lp2); s += (lp2 * 0.7f + Mathf.Sin(2 * Mathf.PI * 180 * stepT) * 0.3f) * Mathf.Exp(-stepT * 14f) * 0.5f * st.drums; }
                    if (hat[step] && st.drums > 0.5f) s += noise * Mathf.Exp(-stepT * 60f) * 0.08f * st.drums;
                }

                // fundido en los bordes para que el bucle no haga clic
                float edge = Mathf.Min(1f, t / 0.05f, (seconds - t) / 0.05f);
                d[i] = s * edge;
            }
            float peak = 0.001f;
            foreach (var x in d) peak = Mathf.Max(peak, Mathf.Abs(x));
            for (int i = 0; i < n; i++) d[i] = d[i] / peak * 0.8f;
            var clip = AudioClip.Create("music_" + id, n, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
