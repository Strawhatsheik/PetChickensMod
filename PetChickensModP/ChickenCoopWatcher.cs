using System.Collections.Generic;
using UnityEngine;

namespace PetChickensMod
{
    public static class CoopWatcher
    {
        private const ulong NightStart = 20000UL; // ~8 PM
        private const ulong DayStart   =  4000UL; // ~4 AM

        private static int  _frame    = 0;
        private static int  _nightTick = 0;
        private static bool _wasNight = false;

        // coopPos → chicken count last seen (detects player add/remove)
        private static readonly Dictionary<Vector3i, int> _prevSlotCounts = new Dictionary<Vector3i, int>();

        // entity IDs currently walking home at night
        private static readonly HashSet<int> _returning = new HashSet<int>();

        public static void Reset()
        {
            _prevSlotCounts.Clear();
            _returning.Clear();
            _wasNight = false;
        }

        public static void Tick()
        {
            World world = GameManager.Instance?.World;
            if (world == null) return;

            // Every frame: nudge night-returning chickens toward the coop
            if (_returning.Count > 0)
                TickNightReturn(world);

            if (++_frame % 300 != 0) return;

            var players = world.Players?.list;
            if (players == null || players.Count == 0) return;

            ulong timeOfDay = world.worldTime % 24000UL;
            bool isNight = timeOfDay >= NightStart || timeOfDay < DayStart;

            if (!_wasNight && isNight)
            {
                // Dusk: send all live chickens home
                StartNightReturn(world);
            }
            else if (_wasNight && !isNight)
            {
                // Dawn: clear counts so the coop poll re-spawns everyone
                _prevSlotCounts.Clear();
                UnityEngine.Debug.Log("[ChickenMod] Dawn — chickens coming out.");
            }

            _wasNight = isNight;

            // Only poll for item-count changes during the day
            if (!isNight)
                ScanCoopsNearPlayers(players, world);
        }

        // ── Night return ──────────────────────────────────────────────────────

        static void StartNightReturn(World world)
        {
            UnityEngine.Debug.Log("[ChickenMod] Dusk — calling chickens home.");
            foreach (int id in new List<int>(ChickenNestManager.GetAllChickenIds()))
            {
                if (!ChickenNestManager.HasCoopClaim(id, out Vector3i coopPos)) continue;
                Entity e = world.GetEntity(id);
                if (e == null || !e.IsAlive()) continue;

                SendTowardCoop(e, coopPos);
                _returning.Add(id);
            }
        }

        static void TickNightReturn(World world)
        {
            _nightTick++;
            bool refreshPath = (_nightTick % 60 == 0); // re-issue path every ~1s

            var arrived = new List<int>();
            foreach (int id in _returning)
            {
                if (!ChickenNestManager.HasCoopClaim(id, out Vector3i coopPos))
                {
                    arrived.Add(id);
                    continue;
                }

                Entity e = world.GetEntity(id);
                if (e == null || !e.IsAlive()) { arrived.Add(id); continue; }

                Vector3 coopCenter = new Vector3(coopPos.x + 0.5f, coopPos.y, coopPos.z + 0.5f);
                float dist = Vector3.Distance(e.position, coopCenter);

                if (dist <= 2.5f)
                {
                    // Made it home — despawn; set prev=0 so dawn re-spawns it
                    _prevSlotCounts[coopPos] = 0;
                    world.RemoveEntity(id, EnumRemoveEntityReason.Despawned);
                    ChickenNestManager.ReleaseChicken(id); // name is preserved
                    arrived.Add(id);
                }
                else if (refreshPath)
                {
                    SendTowardCoop(e, coopPos);
                }
            }

            foreach (int id in arrived)
                _returning.Remove(id);
        }

        static void SendTowardCoop(Entity e, Vector3i coopPos)
        {
            if (e is EntityAlive alive)
            {
                Vector3 target = new Vector3(coopPos.x + 0.5f, coopPos.y, coopPos.z + 0.5f);
                alive.FindPath(target, alive.GetMoveSpeed(), false, null);
            }
        }

        // ── Day coop scanning ─────────────────────────────────────────────────

        static void ScanCoopsNearPlayers(List<EntityPlayer> players, World world)
        {
            var checkedCoops = new HashSet<Vector3i>();
            foreach (EntityPlayer player in players)
            {
                int px = (int)player.position.x;
                int py = (int)player.position.y;
                int pz = (int)player.position.z;

                for (int dx = -20; dx <= 20; dx++)
                for (int dy = -3;  dy <= 3;  dy++)
                for (int dz = -20; dz <= 20; dz++)
                {
                    var bPos = new Vector3i(px + dx, py + dy, pz + dz);
                    if (!checkedCoops.Add(bPos)) continue;
                    if (world.GetBlock(bPos.x, bPos.y, bPos.z).Block.GetBlockName() != "cntChickenCoop") continue;
                    var te = world.GetTileEntity(bPos) as TileEntityCollector;
                    if (te == null) continue;
                    CheckCoop(bPos, te, world);
                }
            }
        }

        static void CheckCoop(Vector3i coopPos, TileEntityCollector te, World world)
        {
            int newCount = CountDomesticatedChickens(te);
            _prevSlotCounts.TryGetValue(coopPos, out int prevCount);

            if (newCount > prevCount)
                for (int i = 0; i < newCount - prevCount; i++)
                    SpawnPetChicken(coopPos, world);
            else if (newCount < prevCount)
                ChickenNestManager.ReleaseChickensFromCoop(coopPos, prevCount - newCount, world);

            _prevSlotCounts[coopPos] = newCount;
        }

        static int CountDomesticatedChickens(TileEntityCollector te)
        {
            ItemClass chickenClass = ItemClass.GetItemClass("domesticatedChicken", false);
            if (chickenClass == null) return 0;

            var slots = te.CatalystSlots;
            if (slots == null) return 0;

            int count = 0;
            foreach (var slot in slots)
                if (slot != null && !slot.IsEmpty() && slot.itemValue.type == chickenClass.Id)
                    count++;
            return count;
        }

        static void SpawnPetChicken(Vector3i coopPos, World world)
        {
            int classId = EntityClass.FromString("entityPetChicken");
            if (classId < 0)
            {
                UnityEngine.Debug.LogWarning("[ChickenMod] entityPetChicken class not found — check entityclasses.xml");
                return;
            }

            Vector3 spawnPos = new Vector3(coopPos.x + 0.5f, coopPos.y + 1f, coopPos.z + 0.5f);
            var ecd = new EntityCreationData
            {
                id          = EntityFactory.nextEntityID,
                entityClass = classId,
                pos         = spawnPos,
                rot         = Vector3.zero
            };

            Entity chicken = EntityFactory.CreateEntity(ecd);
            if (chicken == null)
            {
                UnityEngine.Debug.LogWarning("[ChickenMod] EntityFactory returned null for entityPetChicken");
                return;
            }

            world.SpawnEntityInWorld(chicken);

            if (chicken is EntityAlive alive)
                alive.homePosition = new ChunkCoordinates(coopPos.x, coopPos.y, coopPos.z);

            ChickenNestManager.TryClaimCoop(coopPos, chicken.entityId, world);
            int slot = ChickenNestManager.GetSlot(chicken.entityId);

            if (!ChickenNestManager.TryGetCoopSlotName(coopPos, slot, out string name))
            {
                name = "Chicken #" + ChickenNestManager.NextChickenNumber();
                ChickenNestManager.SetCoopSlotName(coopPos, slot, name);
            }

            UnityEngine.Debug.Log($"[ChickenMod] Spawned {name} (entity {chicken.entityId}, slot {slot}) at coop {coopPos}");
        }
    }
}
