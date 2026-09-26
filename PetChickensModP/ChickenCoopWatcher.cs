using System.Collections.Generic;
using UnityEngine;

namespace PetChickensMod
{
    public static class CoopWatcher
    {
        private const ulong NightStart = 20000UL; // ~8 PM
        private const ulong DayStart   =  4000UL; // ~4 AM

        private static int  _frame     = 0;
        private static int  _nightTick = 0;
        private static int  _tether    = 0;
        private static bool _wasNight  = false;
        private const  float TetherDist = 40f;

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

            // Every ~30 frames: pull any pet chicken that drifted/fled back toward its coop
            if (++_tether % 30 == 0 && !_wasNight)
                TetherChickens(world);

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

        // ── Daytime tether ────────────────────────────────────────────────────

        static void TetherChickens(World world)
        {
            foreach (int id in new List<int>(ChickenNestManager.GetAllChickenIds()))
            {
                if (!ChickenNestManager.HasCoopClaim(id, out Vector3i coopPos)) continue;
                Entity e = world.GetEntity(id);
                if (e == null || !e.IsAlive()) continue;

                Vector3 home = new Vector3(coopPos.x + 0.5f, coopPos.y, coopPos.z + 0.5f);
                if (Vector3.Distance(e.position, home) > TetherDist && e is EntityAlive alive)
                    alive.FindPath(home, alive.GetMoveSpeed(), false, null);
            }
        }

        // ── Day coop scanning ─────────────────────────────────────────────────

        static void ScanCoopsNearPlayers(List<EntityPlayer> players, World world)
        {
            var checkedBlocks = new HashSet<Vector3i>();
            // Track seen TileEntity objects by identity so multi-block coops
            // (multiple block positions sharing one TileEntity) are processed once.
            var seenTEs = new List<TileEntityCollector>();

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
                    if (!checkedBlocks.Add(bPos)) continue;
                    if (world.GetBlock(bPos.x, bPos.y, bPos.z).Block.GetBlockName() != "cntChickenCoop") continue;
                    var te = world.GetTileEntity(bPos) as TileEntityCollector;
                    if (te == null) continue;

                    // Skip if we already processed this exact TileEntity object this scan
                    bool alreadySeen = false;
                    foreach (var s in seenTEs)
                        if (object.ReferenceEquals(s, te)) { alreadySeen = true; break; }
                    if (alreadySeen) continue;
                    seenTEs.Add(te);

                    CheckCoop(bPos, te, world);
                }
            }
        }

        static void CheckCoop(Vector3i coopPos, TileEntityCollector te, World world)
        {
            int itemCount = CountDomesticatedChickens(te);
            // Always compare against actual live entities — this self-corrects any
            // desync (chicken didn't despawn, session reload, etc.) every scan cycle.
            int liveCount = ChickenNestManager.GetCoopOccupancy(coopPos, world);

            if (itemCount > liveCount)
                for (int i = 0; i < itemCount - liveCount; i++)
                    SpawnPetChicken(coopPos, world);
            else if (itemCount < liveCount)
                ChickenNestManager.ReleaseChickensFromCoop(coopPos, liveCount - itemCount, world);

            _prevSlotCounts[coopPos] = itemCount;
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

        // Searches outward from the coop for a clear ground tile to spawn on
        static Vector3 FindSpawnNearCoop(Vector3i coopPos, World world)
        {
            // Start far enough out to clear multi-block coop models
            int[] d = { 4, -4, 5, -5, 3, -3, 6, -6 };
            for (int i = 0; i < d.Length; i++)
            {
                int[][] pairs = { new[] { d[i], 0 }, new[] { 0, d[i] } };
                foreach (int[] off in pairs)
                {
                    int x = coopPos.x + off[0];
                    int z = coopPos.z + off[1];
                    // Scan downward from above the coop to find the first solid+air pair
                    for (int y = coopPos.y + 5; y >= coopPos.y - 5; y--)
                    {
                        bool groundBelow = world.GetBlock(x, y - 1, z).type != 0;
                        bool clearHere   = world.GetBlock(x, y,     z).type == 0;
                        bool clearAbove  = world.GetBlock(x, y + 1, z).type == 0;
                        if (groundBelow && clearHere && clearAbove)
                        {
                            var pos = new Vector3(x + 0.5f, y, z + 0.5f);
                            UnityEngine.Debug.Log($"[ChickenMod] Spawn ground found at {pos} (coop={coopPos})");
                            return pos;
                        }
                    }
                }
            }
            var fallback = new Vector3(coopPos.x + 0.5f, coopPos.y + 1f, coopPos.z + 0.5f);
            UnityEngine.Debug.LogWarning($"[ChickenMod] No ground found near coop — fallback {fallback}");
            return fallback;
        }

        static void SpawnPetChicken(Vector3i coopPos, World world)
        {
            int classId = EntityClass.FromString("entityPetChicken");
            if (classId < 0)
            {
                UnityEngine.Debug.LogWarning("[ChickenMod] entityPetChicken class not found — check entityclasses.xml");
                return;
            }

            Vector3 spawnPos = FindSpawnNearCoop(coopPos, world);
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

            // Set homePosition to the spawn location so EAIWander has a valid centre
            // for picking wander targets. Use spawn coords, not coop coords, so the
            // radius is around a confirmed ground position.
            if (chicken is EntityAlive alive)
                alive.homePosition = new ChunkCoordinates(
                    (int)spawnPos.x, (int)spawnPos.y, (int)spawnPos.z);

            ChickenNestManager.TryClaimCoop(coopPos, chicken.entityId, world);
            int slot = ChickenNestManager.GetSlot(chicken.entityId);

            if (!ChickenNestManager.TryGetCoopSlotName(coopPos, slot, out string name))
            {
                name = ChickenRenameUI.ConsumePendingName()
                       ?? "Chicken #" + ChickenNestManager.NextChickenNumber();
                ChickenNestManager.SetCoopSlotName(coopPos, slot, name);
            }

            // Show the chicken's name in the targeting HUD
            chicken.SetEntityName(name);

            UnityEngine.Debug.Log($"[ChickenMod] Spawned {name} (entity {chicken.entityId}, slot {slot}) at coop {coopPos}");
        }
    }
}
