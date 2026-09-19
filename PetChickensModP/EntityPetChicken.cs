using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PetChickensMod
{
    public class EntityPetChicken : EntityAnimalRabbit
    {
        public override void PostInit()
        {
            base.PostInit();
            RemoveFleeAI();
        }

        void RemoveFleeAI()
        {
            try
            {
                // Find the EAIManager field on EntityAlive (it's private)
                object aiMgr = null;
                foreach (FieldInfo f in typeof(EntityAlive).GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (f.FieldType.Name == "EAIManager")
                    {
                        aiMgr = f.GetValue(this);
                        break;
                    }
                }

                if (aiMgr == null)
                {
                    Debug.LogWarning("[ChickenMod] EAIManager field not found");
                    return;
                }

                // Find the tasks list inside EAIManager
                IList tasks = null;
                foreach (FieldInfo f in aiMgr.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (typeof(IList).IsAssignableFrom(f.FieldType))
                    {
                        tasks = f.GetValue(aiMgr) as IList;
                        if (tasks != null) break;
                    }
                }

                if (tasks == null)
                {
                    Debug.LogWarning("[ChickenMod] EAIManager tasks list not found");
                    return;
                }

                // Remove any flee/runaway AI tasks
                for (int i = tasks.Count - 1; i >= 0; i--)
                {
                    string typeName = tasks[i]?.GetType().Name ?? "";
                    if (typeName.IndexOf("Runaway", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Debug.Log($"[ChickenMod] Removing flee AI task: {typeName}");
                        tasks.RemoveAt(i);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ChickenMod] RemoveFleeAI failed: " + e.Message);
            }
        }
    }
}
