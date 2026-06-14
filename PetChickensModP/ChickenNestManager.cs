using System;
using System.Collections.Generic;

namespace PetChickensMod
{
    public static class ChickenNestManager
    {
        // ── Nest ownership ────────────────────────────────────────────────────
        private static readonly Dictionary<Vector3i, int> nestOwners = new Dictionary<Vector3i, int>();

        public static bool TryClaimNest(Vector3i nestPos, int entityId, World world)
        {
            if (nestOwners.TryGetValue(nestPos, out int ownerId))
            {
                if (ownerId == entityId) return true;
                Entity existing = world.GetEntity(ownerId);
                if (existing != null && existing.isAlive) return false;
            }
            nestOwners[nestPos] = entityId;
            return true;
        }

        public static int CountOwnedNestsNear(Vector3i troughPos, int radius, World world)
        {
            int count = 0;
            foreach (var kv in nestOwners)
            {
                Vector3i n = kv.Key;
                if (Math.Abs(n.x - troughPos.x) > radius) continue;
                if (Math.Abs(n.y - troughPos.y) > radius) continue;
                if (Math.Abs(n.z - troughPos.z) > radius) continue;
                Entity owner = world.GetEntity(kv.Value);
                if (owner != null && owner.isAlive) count++;
            }
            return count;
        }

        // ── Names ─────────────────────────────────────────────────────────────
        private static int nextChickenNumber = 0;
        private static readonly Dictionary<int, string> chickenNames = new Dictionary<int, string>();

        public static int NextChickenNumber() => ++nextChickenNumber;

        public static void SetName(int entityId, string name) =>
            chickenNames[entityId] = name;

        // Returns true and fills name if the entity has a custom/default name.
        // Falls back to rebuilding "Chicken X" from the persisted CVar on world reload.
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
        private static int _classId = -2; // -2 = not yet looked up

        public static bool IsPetChicken(Entity e)
        {
            if (_classId == -2)
                _classId = EntityClass.FromString("entityPetChicken");
            return _classId >= 0 && e.entityType == _classId;
        }

        // ── Cleanup on death ──────────────────────────────────────────────────
        public static void ReleaseChicken(int entityId)
        {
            // Free nest
            var toRemove = new List<Vector3i>();
            foreach (var kv in nestOwners)
                if (kv.Value == entityId) toRemove.Add(kv.Key);
            foreach (var k in toRemove)
                nestOwners.Remove(k);

            // Keep name in dict so the number isn't reused visually,
            // but remove from active tracking.
            chickenNames.Remove(entityId);
        }
    }
}
