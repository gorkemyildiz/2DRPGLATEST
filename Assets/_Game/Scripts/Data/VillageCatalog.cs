using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "VillageCatalog", menuName = "Game Data/Village/Village Catalog")]
    public class VillageCatalog : ScriptableObject
    {
        [SerializeField] private List<VillageBuildingData> buildings = new List<VillageBuildingData>();

        public IReadOnlyList<VillageBuildingData> Buildings => buildings;

        public void SetBuildings(List<VillageBuildingData> list)
        {
            buildings = list ?? new List<VillageBuildingData>();
        }

        public VillageBuildingData GetByType(VillageBuildingType type)
        {
            if (buildings == null)
            {
                return null;
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i] != null && buildings[i].buildingType == type)
                {
                    return buildings[i];
                }
            }

            return null;
        }

        public VillageBuildingData GetById(string buildingId)
        {
            if (buildings == null || string.IsNullOrEmpty(buildingId))
            {
                return null;
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i] != null && buildings[i].buildingId == buildingId)
                {
                    return buildings[i];
                }
            }

            return null;
        }
    }
}
