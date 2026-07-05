using UnityEngine;

namespace SOAP
{
    /// <summary>
    /// Abstract base for all Npc interactions. One asset per interaction.
    ///
    /// Concrete subtypes own only the fields they need and implement their
    /// own logic — no discriminator enum, no dead fields, no factory.
    /// </summary>
    public abstract class Npc : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable identifier — match this to whatever your dialogue or NPC system uses.")]
        public string NpcId;

        [Tooltip("Human-readable name shown in UI. E.g. 'Captain Aldric Ironwall'.")]
        public string DisplayName;
    }
}