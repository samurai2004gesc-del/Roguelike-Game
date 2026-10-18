using UnityEngine;

namespace Umbral
{
    /// <summary>Raíz de todo lo que pertenece a la sala o al santuario actual. Se destruye al cambiar de sala.</summary>
    public static class World
    {
        public static Transform Root { get; private set; }

        public static void Reset()
        {
            if (Root != null) Object.Destroy(Root.gameObject);
            Root = new GameObject("World").transform;
            Arena.Current = null;
        }

        public static Transform Child(string name) => Root.Child(name);
    }

    /// <summary>Control de escala de tiempo: pausa, congelación de golpe (hitstop) y cámara lenta.</summary>
    public static class HitStop
    {
        static float stopUntil;
        static float slowUntil, slowScale = 1f;
        public static bool Paused;
        public static int pauseLocks;

        public static void Do(float seconds)
        {
            if (!GameSettings.HitStop) return;
            stopUntil = Mathf.Max(stopUntil, Time.unscaledTime + seconds);
        }

        public static void Slow(float scale, float seconds)
        {
            slowScale = scale;
            slowUntil = Time.unscaledTime + seconds;
        }

        public static void Tick()
        {
            float target = 1f;
            if (Time.unscaledTime < slowUntil) target = slowScale;
            if (Time.unscaledTime < stopUntil) target = 0.04f;
            if (Paused || pauseLocks > 0) target = 0f;
            Time.timeScale = target;
        }

        public static void Clear() { stopUntil = 0; slowUntil = 0; }
    }

    /// <summary>Opciones del jugador (persisten en PlayerPrefs).</summary>
    public static class GameSettings
    {
        public static float MasterVolume { get => PlayerPrefs.GetFloat("umbral_master", 0.9f); set => PlayerPrefs.SetFloat("umbral_master", value); }
        public static float MusicVolume { get => PlayerPrefs.GetFloat("umbral_music", 0.8f); set => PlayerPrefs.SetFloat("umbral_music", value); }
        public static float SfxVolume { get => PlayerPrefs.GetFloat("umbral_sfx", 1f); set { PlayerPrefs.SetFloat("umbral_sfx", value); Sfx.volume = 0.8f * value; } }
        public static bool ScreenShake { get => PlayerPrefs.GetInt("umbral_shake", 1) == 1; set => PlayerPrefs.SetInt("umbral_shake", value ? 1 : 0); }
        public static bool HitStop { get => PlayerPrefs.GetInt("umbral_hitstop", 1) == 1; set => PlayerPrefs.SetInt("umbral_hitstop", value ? 1 : 0); }
        public static bool DamageNumbers { get => PlayerPrefs.GetInt("umbral_numbers", 1) == 1; set => PlayerPrefs.SetInt("umbral_numbers", value ? 1 : 0); }
        /// <summary>Modo Dios (accesibilidad, como en Hades): reducción de daño que crece con cada muerte.</summary>
        public static bool GodMode { get => PlayerPrefs.GetInt("umbral_godmode", 0) == 1; set => PlayerPrefs.SetInt("umbral_godmode", value ? 1 : 0); }
    }
}
