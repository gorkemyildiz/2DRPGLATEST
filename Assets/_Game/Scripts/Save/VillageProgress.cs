using System;
using System.Collections.Generic;

namespace Game.Save
{
    [Serializable]
    public class BuildingLevelSave
    {
        public Game.Data.VillageBuildingType buildingType;
        public int level = 1;
    }

    [Serializable]
    public class VillageProgress
    {
        public List<BuildingLevelSave> buildings = new List<BuildingLevelSave>();

        public VillageProgress()
        {
            buildings = CreateDefaults();
        }

        public static List<BuildingLevelSave> CreateDefaults()
        {
            return new List<BuildingLevelSave>
            {
                new BuildingLevelSave { buildingType = Game.Data.VillageBuildingType.Library, level = 1 },
                new BuildingLevelSave { buildingType = Game.Data.VillageBuildingType.Mine, level = 1 },
                new BuildingLevelSave { buildingType = Game.Data.VillageBuildingType.Blacksmith, level = 1 }
            };
        }

        public int GetLevel(Game.Data.VillageBuildingType type)
        {
            EnsureDefaults();
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i].buildingType == type)
                {
                    return System.Math.Max(1, buildings[i].level);
                }
            }

            return 1;
        }

        public void SetLevel(Game.Data.VillageBuildingType type, int level)
        {
            EnsureDefaults();
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i].buildingType == type)
                {
                    buildings[i].level = System.Math.Max(1, level);
                    return;
                }
            }

            buildings.Add(new BuildingLevelSave { buildingType = type, level = System.Math.Max(1, level) });
        }

        public void EnsureDefaults()
        {
            if (buildings == null || buildings.Count == 0)
            {
                buildings = CreateDefaults();
            }
        }
    }
}
