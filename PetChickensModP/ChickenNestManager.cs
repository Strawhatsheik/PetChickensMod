using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PetChickensMod
{
    public static class ChickenNestManager
    {
        // ── Coop ownership ────────────────────────────────────────────────────
        private static readonly Dictionary<int, Vector3i> chickenToCoop = new Dictionary<int, Vector3i>();
        private static readonly Dictionary<int, int>     chickenToSlot = new Dictionary<int, int>();
        private const int MaxChickensPerCoop = 3;

        public static bool TryClaimCoop(Vector3i coopPos, int entityId, World world)
        {
            if (chickenToCoop.TryGetValue(entityId, out Vector3i existing))
                return existing == coopPos;

            // Find which slots are already occupied by live chickens at this coop
            var occupiedSlots = new HashSet<int>();
            foreach (var kv in chickenToCoop)
            {
                if (kv.Value != coopPos) continue;
                Entity e = world.GetEntity(kv.Key);
                if (e != null && e.IsAlive())
                    occupiedSlots.Add(chickenToSlot.TryGetValue(kv.Key, out int s) ? s : 0);
            }

            int slot = 0;
            while (occupiedSlots.Contains(slot)) slot++;
            if (slot >= MaxChickensPerCoop) return false;

            chickenToCoop[entityId] = coopPos;
            chickenToSlot[entityId] = slot;
            return true;
        }

        public static bool HasCoopClaim(int entityId, out Vector3i coopPos)
        {
            return chickenToCoop.TryGetValue(entityId, out coopPos);
        }

        public static int GetCoopOccupancy(Vector3i coopPos, World world)
        {
            int count = 0;
            foreach (var kv in chickenToCoop)
            {
                if (kv.Value != coopPos) continue;
                Entity e = world.GetEntity(kv.Key);
                if (e != null && e.IsAlive()) count++;
            }
            return count;
        }

        // Called by CoopWatcher when domesticatedChicken items are removed from a coop
        public static void ReleaseChickensFromCoop(Vector3i coopPos, int count, World world)
        {
            int released = 0;
            var toRemove = new List<int>();
            foreach (var kv in chickenToCoop)
            {
                if (kv.Value != coopPos) continue;
                toRemove.Add(kv.Key);
                if (++released >= count) break;
            }

            foreach (int id in toRemove)
            {
                // Erase the name for this coop slot (item was intentionally removed)
                if (chickenToSlot.TryGetValue(id, out int slot))
                    coopNames.Remove(CoopSlotKey(coopPos, slot));

                chickenToCoop.Remove(id);
                chickenToSlot.Remove(id);

                Entity e = world.GetEntity(id);
                if (e != null)
                    world.RemoveEntity(id, EnumRemoveEntityReason.Despawned);
            }
            SaveNames();
        }

        // ── Names (keyed by coop position + slot — survives session restarts) ─
        private static int _nextChickenNumber = 0;
        private static readonly Dictionary<string, string> coopNames = new Dictionary<string, string>();

        static string CoopSlotKey(Vector3i pos, int slot) =>
            pos.x + "," + pos.y + "," + pos.z + "," + slot;

        public static int NextChickenNumber() => ++_nextChickenNumber;

        // Get slot index for a chicken (0 if not tracked)
        public static int GetSlot(int entityId) =>
            chickenToSlot.TryGetValue(entityId, out int s) ? s : 0;

        // Store/retrieve a name by coop position + slot
        public static void SetCoopSlotName(Vector3i coopPos, int slot, string name)
        {
            coopNames[CoopSlotKey(coopPos, slot)] = name;
            SaveNames();
        }

        public static bool TryGetCoopSlotName(Vector3i coopPos, int slot, out string name) =>
            coopNames.TryGetValue(CoopSlotKey(coopPos, slot), out name);

        // Convenience: set name by entity (looks up coop+slot internally)
        public static void SetName(int entityId, string name)
        {
            if (!chickenToCoop.TryGetValue(entityId, out Vector3i coop)) return;
            SetCoopSlotName(coop, GetSlot(entityId), name);

            // Update HUD display name on the live entity
            Entity e = GameManager.Instance?.World?.GetEntity(entityId);
            if (e != null) e.SetEntityName(name);
        }

        // Convenience: get name by entity
        public static bool TryGetName(int entityId, out string name)
        {
            if (!chickenToCoop.TryGetValue(entityId, out Vector3i coop))
            { name = null; return false; }
            return TryGetCoopSlotName(coop, GetSlot(entityId), out name);
        }

        public static IEnumerable<int> GetAllChickenIds() => chickenToCoop.Keys;

        // ── Entity identity ───────────────────────────────────────────────────
        // An entity is a pet chicken if we spawned it this session (tracked in chickenToCoop).
        public static bool IsPetChicken(Entity e) =>
            e != null && chickenToCoop.ContainsKey(e.entityId);

        // ── Cleanup on death ──────────────────────────────────────────────────
        public static void ReleaseChicken(int entityId)
        {
            // Name stays in coopNames — the coop slot persists even when entity dies
            chickenToCoop.Remove(entityId);
            chickenToSlot.Remove(entityId);
        }

        // ── Persistence ───────────────────────────────────────────────────────
        static string SavePath => Path.Combine(GameIO.GetSaveGameDir(), "petChickenNames.txt");

        public static void SaveNames()
        {
            try
            {
                var lines = new List<string>();
                lines.Add("_counter=" + _nextChickenNumber);
                foreach (var kv in coopNames)
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
                coopNames.Clear();
                foreach (string line in File.ReadAllLines(path))
                {
                    int eq = line.IndexOf('=');
                    if (eq < 1) continue;
                    string key = line.Substring(0, eq);
                    string val = line.Substring(eq + 1);
                    if (key == "_counter") { int.TryParse(val, out _nextChickenNumber); continue; }
                    coopNames[key] = val;
                }
                UnityEngine.Debug.Log("[ChickenMod] Loaded " + coopNames.Count + " chicken name(s).");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[ChickenMod] Could not load names: " + e.Message);
            }
        }
    }
}
