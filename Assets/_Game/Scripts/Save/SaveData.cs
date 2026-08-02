using System;
using System.Collections.Generic;

namespace Game.Save
{
    [Serializable]
    public class SaveData
    {
        public Game.Rewards.PlayerProgress playerProgress;
        public List<string> inventoryItemIds = new List<string>();
        public List<ItemInstanceSave> inventoryInstances = new List<ItemInstanceSave>();
        public List<EquipmentSaveData> equipment = new List<EquipmentSaveData>();
        public MapProgress mapProgress;
        public VillageProgress villageProgress;
        public MaterialInventorySave materials;
        public BuildProgress buildProgress;
        public RosterProgress rosterProgress;
        public WeeklyDungeonProgress weeklyDungeonProgress;
        public string lastSaveTime;
        public int activeStageIndex = 0;

        public SaveData()
        {
            playerProgress = new Game.Rewards.PlayerProgress();
            inventoryItemIds = new List<string>();
            inventoryInstances = new List<ItemInstanceSave>();
            equipment = new List<EquipmentSaveData>();
            mapProgress = new MapProgress();
            villageProgress = new VillageProgress();
            materials = new MaterialInventorySave();
            buildProgress = new BuildProgress();
            rosterProgress = new RosterProgress();
            weeklyDungeonProgress = new WeeklyDungeonProgress();
            lastSaveTime = DateTime.UtcNow.ToString("o");
            activeStageIndex = 0;
        }
    }

    [Serializable]
    public class EquipmentSaveData
    {
        public Game.Data.EquipmentSlot slot;
        public string itemId;
        public string instanceUid;
    }
}
