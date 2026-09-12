using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PetChickensMod
{
    [HarmonyPatch(typeof(GameManager), "Update")]
    public class Patch_CoopWatcher
    {
        private static int _frame = 0;

        // coopPos -> count of domesticatedChicken items last seen in that coop
        private static readonly Dictionary<Vector3i, int> _prevSlotCounts = new Dictionary<Vector3i, int>();

        public static void Reset() => _prevSlotCounts.Clear();

        [HarmonyPostfix]
        static void OnUpdate(GameManager __instance)
        {
            if (++_frame % 300 != 0) return; // ~5 s at 60 fps

            World world = __instance.World;
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
            {
                int toSpawn = newCount - prevCount;
                for (int i = 0; i < toSpawn; i++)
                    SpawnPetChicken(coopPos, world);
            }
            else if (newCount < prevCount)
            {
                ChickenNestManager.ReleaseChickensFromCoop(coopPos, prevCount - newCount, world);
            }

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
            {
                if (slot != null && !slot.IsEmpty() && slot.itemValue.type == chickenClass.Id)
                    count++;
            }
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

            // Pre-register to coop so the slot index is known before the AI tick runs
            ChickenNestManager.TryClaimCoop(coopPos, chicken.entityId, world);
            int slot = ChickenNestManager.GetSlot(chicken.entityId);

            // Restore saved name or assign a new one
            if (!ChickenNestManager.TryGetCoopSlotName(coopPos, slot, out string name))
            {
                name = "Chicken #" + ChickenNestManager.NextChickenNumber();
                ChickenNestManager.SetCoopSlotName(coopPos, slot, name);
            }

            UnityEngine.Debug.Log($"[ChickenMod] Spawned {name} (entity {chicken.entityId}, slot {slot}) at coop {coopPos}");
        }
    }
}
