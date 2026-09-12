using System;
using System.Collections.Generic;
using System.IO;

namespace PetChickensMod
{
    public static class ChickenNestManager
    {
        // ── Coop ownership ────────────────────────────────────────────────────
        // Maps coop block position → entityId of chicken that claimed it.
        // One slot per chicken; vanilla coop supports up to 3 chickens.
        private static readonly Dictionary<Vector3i, int> coopOwners = new Dictionary<Vector3i, int>();
        private const int MaxChickensPerCoop = 3;

        public static bool TryClaimCoop(Vector3i coopPos, int entityId, World world)
        {
            // Already claimed by this chicken
            if (coopOwners.TryGetValue(coopPos, out int ownerId) && ownerId == entityId)
                return true;

            // Slot taken by a living chicken — check if still alive
            if (coopOwners.ContainsKey(coopPos))
            {
                Entity existing = world.GetEntity(coopOwners[coopPos]);
                if (existing != null && existing.IsAlive()) return false;
            }

            // Count how many slots this coop already has claimed
            int claimed = 0;
            foreach (var kv in coopOwners)
                if (kv.Key == coopPos)
                    claimed++;
            if (claimed >= MaxChickensPerCoop) return false;

            coopOwners[coopPos] = entityId;
            return true;
        }

        public static bool HasCoopClaim(int entityId, out Vector3i coopPos)
        {
            foreach (var kv in coopOwners)
            {
                if (kv.Value == entityId) { coopPos = kv.Key; return true; }
            }
            coopPos = default;
            return false;
        }

        // ── Names ─────────────────────────────────────────────────────────────
        private static int nextChickenNumber = 0;
        private static readonly Dictionary<int, string> chickenNames = new Dictionary<int, string>();

        public static int NextChickenNumber() => ++nextChickenNumber;

        public static void SetName(int entityId, string name)
        {
            chickenNames[entityId] = name;
            SaveNames();
        }

        public static bool TryGetName(int entityId, string cvarFallback, out string name)
        {
            if (chickenNames.TryGetValue(entityId, out name)) return true;
            if (!string.IsNullOrEmpty(cvarFallback) && cvarFallback != "0")
            {
                name = "Chicken " + cvarFallback;
                chickenNames[entityId] = name;
                return true;
            }
            name = null;
            return false;
        }

        // ── Entity identity ───────────────────────────────────────────────────
        private static int _classId = -2;

        public static bool IsPetChicken(Entity e)
        {
            if (_classId == -2)
                _classId = EntityClass.FromString("entityPetChicken");
            return _classId >= 0 && (int)e.entityType == _classId;
        }

        // ── Cleanup on death ──────────────────────────────────────────────────
        public static void ReleaseChicken(int entityId)
        {
            var toRemove = new List<Vector3i>();
            foreach (var kv in coopOwners)
                if (kv.Value == entityId) toRemove.Add(kv.Key);
            foreach (var k in toRemove) coopOwners.Remove(k);

            chickenNames.Remove(entityId);
        }

        // ── Persistence ───────────────────────────────────────────────────────
        static string SavePath => Path.Combine(GameIO.GetSaveGameDir(), "petChickenNames.txt");

        public static void SaveNames()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kv in chickenNames)
                    lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(SavePath, lines);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[ChickenMod] Could not save names: " + e.Message);
            }
        }

        public static void LoadNames()
        {
            try
            {
                string path = SavePath;
                if (!File.Exists(path)) return;
                chickenNames.Clear();
                foreach (string line in File.ReadAllLines(path))
                {
                    int eq = line.IndexOf('=');
                    if (eq < 1) continue;
                    if (int.TryParse(line.Substring(0, eq), out int id))
                        chickenNames[id] = line.Substring(eq + 1);
                }
                UnityEngine.Debug.Log("[ChickenMod] Loaded " + chickenNames.Count + " chicken name(s).");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[ChickenMod] Could not load names: " + e.Message);
            }
        }
    }
}
