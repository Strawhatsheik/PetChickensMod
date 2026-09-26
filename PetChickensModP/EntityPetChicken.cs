using UnityEngine;

namespace PetChickensMod
{
    // Inherits EntityAnimal directly — bypasses EntityAnimalRabbit's hardcoded flee C#.
    // Model, textures, animations, and AI tasks all come from entityclasses.xml.
    public class EntityPetChicken : EntityAnimal
    {
        public override void PostInit()
        {
            base.PostInit();
            Debug.Log($"[ChickenMod] EntityPetChicken.PostInit() — custom class IS loaded (entity {entityId})");
        }
    }
}
