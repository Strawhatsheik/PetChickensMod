using System.Collections.Generic;
using UnityEngine;

namespace PetChickensMod
{
    public static class CoopWatcher
    {
        private static int _frame = 0;
        private static readonly Dictionary<Vector3i, int> _prevSlotCounts = new Dictionary<Vector3i, int>();

        public static void Reset() => _prevSlotCounts.Clear();

        public static void Tick()
        {
            if (++_frame % 300 != 0) return;

            World world = GameManager.Instance?.World;
            if (world == null) return;

            var players = world.Players?.list;
            if (players == null || players.Count == 0) return;

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

            // Anchor the wander task to the coop so the chicken doesn't roam away
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
