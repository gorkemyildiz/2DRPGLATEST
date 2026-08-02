using UnityEngine;

namespace Game.Units
{
    /// <summary>
    /// Marks a battle/hub hero instance as a party slot member.
    /// </summary>
    public class PartyMember : MonoBehaviour
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private string classId;
        [SerializeField] private bool isLead;

        public int SlotIndex => slotIndex;
        public string ClassId => classId;
        public bool IsLead => isLead;

        public void Configure(int slot, string heroClassId, bool lead)
        {
            slotIndex = Mathf.Max(0, slot);
            classId = heroClassId ?? string.Empty;
            isLead = lead;
        }
    }
}
