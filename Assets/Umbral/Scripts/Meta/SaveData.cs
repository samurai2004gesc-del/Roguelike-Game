using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Umbral
{
    [Serializable] public class KV { public string k; public int v; public KV(string k, int v) { this.k = k; this.v = v; } }

    /// <summary>
    /// Progreso permanente (lo que sobrevive a la muerte, como en Hades): recursos, rituales, cartas del Grimorio,
    /// pactos, reliquias, afinidad con personajes, presagios cumplidos, entradas del Necronomicón y estadísticas.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public bool started;
        public bool female;
        public string lastWeapon = "baculo";
        public string equippedRelic = "";
        public string equippedFamiliar = "";
        public int grasp = 10;                     // Capacidad mental (límite de cartas)
        public List<KV> resources = new List<KV>();
        public List<KV> counters = new List<KV>();
        public List<KV> affinity = new List<KV>();
        public List<KV> vows = new List<KV>();
        public List<KV> aspects = new List<KV>();   // nivel de aspecto por arma ("baculo:1")
        public List<string> flags = new List<string>();
        public List<string> rituals = new List<string>();
        public List<string> cards = new List<string>();       // desbloqueadas
        public List<string> activeCards = new List<string>(); // equipadas
        public List<string> relics = new List<string>();
        public List<string> familiars = new List<string>();
        public List<string> weapons = new List<string> { "baculo" };
        public List<string> codex = new List<string>();
        public List<string> questsClaimed = new List<string>();
        public List<string> seenDialogue = new List<string>();
        public List<KV> weaponAspect = new List<KV>();         // aspecto elegido por arma (índice)
        public float bestTime;
        public int bestFear;

        // -------- ayudantes --------
        static int GetKV(List<KV> list, string k) { foreach (var e in list) if (e.k == k) return e.v; return 0; }
        static void SetKV(List<KV> list, string k, int v) { foreach (var e in list) if (e.k == k) { e.v = v; return; } list.Add(new KV(k, v)); }

        public int Res(string id) => GetKV(resources, id);
        public void AddRes(string id, int n) => SetKV(resources, id, Mathf.Max(0, Res(id) + n));
        public bool Spend(Dictionary<string, int> cost)
        {
            foreach (var kv in cost) if (Res(kv.Key) < kv.Value) return false;
            foreach (var kv in cost) AddRes(kv.Key, -kv.Value);
            return true;
        }
        public bool CanAfford(Dictionary<string, int> cost) { foreach (var kv in cost) if (Res(kv.Key) < kv.Value) return false; return true; }

        public int Count(string id) => GetKV(counters, id);
        public void Inc(string id, int n = 1) => SetKV(counters, id, Count(id) + n);
        public void SetCount(string id, int v) => SetKV(counters, id, v);

        public int Affinity(string npc) => GetKV(affinity, npc);
        public void AddAffinity(string npc, int n) => SetKV(affinity, npc, Affinity(npc) + n);

        public int Vow(string id) => GetKV(vows, id);
        public void SetVow(string id, int v) => SetKV(vows, id, v);

        public int AspectLevel(string weaponAspect) => GetKV(aspects, weaponAspect);
        public void SetAspectLevel(string weaponAspect, int v) => SetKV(aspects, weaponAspect, v);
        public int ChosenAspect(string weapon) => GetKV(weaponAspect, weapon);
        public void ChooseAspect(string weapon, int idx) => SetKV(weaponAspect, weapon, idx);

        public bool Flag(string f) => flags.Contains(f);
        public void SetFlag(string f) { if (!flags.Contains(f)) flags.Add(f); }

        public bool HasRitual(string id) => rituals.Contains(id);
        public void Codex(string id) { if (!string.IsNullOrEmpty(id) && !codex.Contains(id)) { codex.Add(id); } }
    }

    public static class Save
    {
        public static SaveData Data = new SaveData();
        static string PathFor(int slot) => System.IO.Path.Combine(Application.persistentDataPath, $"umbral_save_{slot}.json");
        public static int slot = 0;

        public static bool Exists => File.Exists(PathFor(slot));

        public static void Load()
        {
            try
            {
                if (File.Exists(PathFor(slot)))
                {
                    Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFor(slot))) ?? new SaveData();
                    return;
                }
            }
            catch (Exception e) { Debug.LogWarning("[Umbral] Error al cargar partida: " + e.Message); }
            Data = new SaveData();
        }

        public static void Write()
        {
            try { File.WriteAllText(PathFor(slot), JsonUtility.ToJson(Data, true)); }
            catch (Exception e) { Debug.LogWarning("[Umbral] Error al guardar: " + e.Message); }
        }

        public static void Delete()
        {
            try { if (File.Exists(PathFor(slot))) File.Delete(PathFor(slot)); } catch { }
            Data = new SaveData();
        }
    }

    /// <summary>Recursos permanentes y su presentación.</summary>
    public static class Res
    {
        public const string Ceniza = "ceniza";       // Cenizas Estelares → desbloquear cartas del Grimorio (como Cenizas)
        public const string Ensueno = "ensueno";     // Ensueño → ampliar la Capacidad mental (como Psique)
        public const string Huesos = "huesos";       // Huesos de Ghoul → mercado de Herbert West (como Huesos)
        public const string Licor = "licor";         // Licor de Luna → regalos (como Néctar)
        public const string Plata = "plata";         // Plata Onírica → rituales (como Plata)
        public const string Polvo = "polvo";         // Polvo de Kadath → armas y aspectos (como Polvo de Luna)
        public const string Lagrima = "lagrima";     // Lágrima de Yog → rituales finales (como Ambrosía/Esencia)

        public static readonly string[] AllIds = { Ceniza, Ensueno, Huesos, Plata, Polvo, Licor, Lagrima };

        public static string Name(string id)
        {
            switch (id)
            {
                case Ceniza: return "Ceniza Estelar";
                case Ensueno: return "Ensueño";
                case Huesos: return "Huesos de Ghoul";
                case Licor: return "Licor de Luna";
                case Plata: return "Plata Onírica";
                case Polvo: return "Polvo de Kadath";
                case Lagrima: return "Lágrima de Yog";
                case "oro": return "Oro Onírico";
            }
            return id;
        }

        public static Color ColorOf(string id)
        {
            switch (id)
            {
                case Ceniza: return new Color(0.75f, 0.7f, 0.95f);
                case Ensueno: return new Color(0.95f, 0.5f, 0.9f);
                case Huesos: return new Color(0.92f, 0.88f, 0.75f);
                case Licor: return new Color(0.6f, 0.95f, 0.85f);
                case Plata: return new Color(0.8f, 0.85f, 0.95f);
                case Polvo: return new Color(0.55f, 0.75f, 1f);
                case Lagrima: return new Color(0.9f, 0.4f, 1f);
                case "oro": return new Color(1f, 0.82f, 0.3f);
            }
            return Color.white;
        }

        public static string Cost(Dictionary<string, int> cost)
        {
            var parts = new List<string>();
            foreach (var kv in cost) parts.Add($"<color=#{ColorUtility.ToHtmlStringRGB(ColorOf(kv.Key))}>{kv.Value} {Name(kv.Key)}</color>");
            return string.Join("  ·  ", parts);
        }
    }
}
