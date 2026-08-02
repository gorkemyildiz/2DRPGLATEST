using System.Collections.Generic;
using Game.Rewards;
using Game.Save;

namespace Game.Core
{
    /// <summary>
    /// In-memory session shared across Hub and Battle.
    /// Disk persistence is handled by GameSaveController.
    /// </summary>
    public static class RuntimePlayerState
    {
        public static bool IsInitialized { get; private set; }
        public static bool HasAppliedDiskSave { get; private set; }
        public static bool HasClaimedAfkThisPlaySession { get; set; }

        public static PlayerProgress Progress { get; private set; }
        public static List<string> InventoryItemIds { get; private set; }
        public static List<ItemInstanceSave> InventoryInstances { get; private set; }
        public static List<EquipmentSaveData> Equipment { get; private set; }
        public static MapProgress MapProgress { get; private set; }
        public static VillageProgress VillageProgress { get; private set; }
        public static MaterialInventorySave Materials { get; private set; }
        public static BuildProgress BuildProgress { get; private set; }
        public static RosterProgress RosterProgress { get; private set; }
        public static WeeklyDungeonProgress WeeklyDungeonProgress { get; private set; }
        public static int ActiveStageIndex { get; set; }

        /// <summary>Active class build (mirrors BuildProgress after roster sync).</summary>
        public static BuildProgress ActiveBuild
        {
            get
            {
                EnsureInitialized();
                if (RosterProgress != null && !string.IsNullOrEmpty(RosterProgress.selectedClassId))
                {
                    return RosterProgress.GetOrCreateBuild(RosterProgress.selectedClassId);
                }

                return BuildProgress;
            }
        }

        public static void EnsureInitialized()
        {
            if (IsInitialized)
            {
                return;
            }

            Progress = new PlayerProgress();
            InventoryItemIds = new List<string>();
            InventoryInstances = new List<ItemInstanceSave>();
            Equipment = CreateDefaultEquipment();
            MapProgress = new MapProgress();
            VillageProgress = new VillageProgress();
            Materials = new MaterialInventorySave();
            BuildProgress = new BuildProgress();
            RosterProgress = new RosterProgress();
            RosterProgress.EnsureDefaults();
            WeeklyDungeonProgress = new WeeklyDungeonProgress();
            ActiveStageIndex = 0;
            HasClaimedAfkThisPlaySession = false;
            IsInitialized = true;
        }

        public static void ResetSession()
        {
            IsInitialized = false;
            HasAppliedDiskSave = false;
            HasClaimedAfkThisPlaySession = false;
            Progress = null;
            InventoryItemIds = null;
            InventoryInstances = null;
            Equipment = null;
            MapProgress = null;
            VillageProgress = null;
            Materials = null;
            BuildProgress = null;
            RosterProgress = null;
            WeeklyDungeonProgress = null;
            ActiveStageIndex = 0;
            EnsureInitialized();
        }

        /// <summary>
        /// Keeps legacy BuildProgress and active class build in sync.
        /// </summary>
        public static void SyncActiveBuildFromRoster()
        {
            EnsureInitialized();
            RosterProgress.EnsureDefaults();
            BuildProgress active = RosterProgress.GetOrCreateBuild(RosterProgress.selectedClassId);
            BuildProgress = active;
        }

        public static void PushActiveBuildIntoRoster()
        {
            EnsureInitialized();
            if (RosterProgress == null || string.IsNullOrEmpty(RosterProgress.selectedClassId))
            {
                return;
            }

            RosterProgress.EnsureBuildSlot(RosterProgress.selectedClassId);
            for (int i = 0; i < RosterProgress.classBuilds.Count; i++)
            {
                ClassBuildSave slot = RosterProgress.classBuilds[i];
                if (slot != null && slot.classId == RosterProgress.selectedClassId)
                {
                    slot.build = BuildProgress != null ? BuildProgress : new BuildProgress();
                    return;
                }
            }
        }

        public static void MigrateInventoryToInstancesIfNeeded()
        {
            EnsureInitialized();
            if (InventoryInstances.Count > 0)
            {
                return;
            }

            if (InventoryItemIds == null || InventoryItemIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < InventoryItemIds.Count; i++)
            {
                string id = InventoryItemIds[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                InventoryInstances.Add(ItemInstanceSave.CreateNew(id));
            }
        }

        public static void ApplySaveData(SaveData data)
        {
            EnsureInitialized();

            if (data == null)
            {
                MarkDiskLoadComplete();
                return;
            }

            if (data.playerProgress != null)
            {
                Progress.level = System.Math.Max(1, data.playerProgress.level);
                Progress.currentExperience = System.Math.Max(0, data.playerProgress.currentExperience);
                Progress.gold = System.Math.Max(0, data.playerProgress.gold);
                Progress.totalEnemiesKilled = System.Math.Max(0, data.playerProgress.totalEnemiesKilled);
                Progress.highestCompletedStage = System.Math.Max(0, data.playerProgress.highestCompletedStage);
            }

            InventoryItemIds.Clear();
            if (data.inventoryItemIds != null)
            {
                InventoryItemIds.AddRange(data.inventoryItemIds);
            }

            InventoryInstances.Clear();
            if (data.inventoryInstances != null && data.inventoryInstances.Count > 0)
            {
                for (int i = 0; i < data.inventoryInstances.Count; i++)
                {
                    ItemInstanceSave src = data.inventoryInstances[i];
                    if (src == null || string.IsNullOrEmpty(src.itemId))
                    {
                        continue;
                    }

                    ItemInstanceSave copy = src.Clone();
                    if (string.IsNullOrEmpty(copy.uid))
                    {
                        copy.uid = System.Guid.NewGuid().ToString("N");
                    }

                    if (copy.enchantIds == null)
                    {
                        copy.enchantIds = new List<string>();
                    }

                    InventoryInstances.Add(copy);
                }
            }
            else
            {
                MigrateInventoryToInstancesIfNeeded();
            }

            // Keep legacy id list in sync
            InventoryItemIds.Clear();
            for (int i = 0; i < InventoryInstances.Count; i++)
            {
                if (InventoryInstances[i] != null)
                {
                    InventoryItemIds.Add(InventoryInstances[i].itemId);
                }
            }

            Equipment.Clear();
            if (data.equipment != null && data.equipment.Count > 0)
            {
                for (int i = 0; i < data.equipment.Count; i++)
                {
                    EquipmentSaveData slot = data.equipment[i];
                    Equipment.Add(new EquipmentSaveData
                    {
                        slot = slot.slot,
                        itemId = slot.itemId,
                        instanceUid = slot.instanceUid
                    });
                }
            }
            else
            {
                Equipment.AddRange(CreateDefaultEquipment());
            }

            if (data.mapProgress != null)
            {
                MapProgress.unlockedLayer = System.Math.Max(1, data.mapProgress.unlockedLayer);
                MapProgress.unlockedChapter = System.Math.Max(1, data.mapProgress.unlockedChapter);
                MapProgress.unlockedLevel = System.Math.Max(1, data.mapProgress.unlockedLevel);
                MapProgress.layerBossClearedUpTo = System.Math.Max(0, data.mapProgress.layerBossClearedUpTo);
                MapProgress.pendingLayerBossChapter = System.Math.Max(0, data.mapProgress.pendingLayerBossChapter);
                MapProgress.layerBossClearedChapterUpTo =
                    System.Math.Max(0, data.mapProgress.layerBossClearedChapterUpTo);
                // Migrate legacy field if present in older saves (JsonUtility keeps unknown fields out,
                // but layerBossReadyUpTo may still deserialize into a leftover if we keep the name —
                // pending is the source of truth going forward).
                MapProgress.activeLayer = System.Math.Max(0, data.mapProgress.activeLayer);
                MapProgress.activeChapter = System.Math.Max(0, data.mapProgress.activeChapter);
                MapProgress.activeLevel = System.Math.Max(0, data.mapProgress.activeLevel);
                MapProgress.activeIsLayerBoss = data.mapProgress.activeIsLayerBoss;
            }

            if (data.villageProgress != null)
            {
                VillageProgress.buildings = new List<BuildingLevelSave>();
                if (data.villageProgress.buildings != null)
                {
                    for (int i = 0; i < data.villageProgress.buildings.Count; i++)
                    {
                        BuildingLevelSave src = data.villageProgress.buildings[i];
                        VillageProgress.buildings.Add(new BuildingLevelSave
                        {
                            buildingType = src.buildingType,
                            level = System.Math.Max(1, src.level)
                        });
                    }
                }

                VillageProgress.EnsureDefaults();
            }

            Materials.stacks = new List<MaterialStackSave>();
            if (data.materials != null && data.materials.stacks != null)
            {
                for (int i = 0; i < data.materials.stacks.Count; i++)
                {
                    MaterialStackSave src = data.materials.stacks[i];
                    if (src == null || string.IsNullOrEmpty(src.materialId) || src.amount <= 0)
                    {
                        continue;
                    }

                    Materials.stacks.Add(new MaterialStackSave
                    {
                        materialId = src.materialId,
                        amount = src.amount
                    });
                }
            }

            if (BuildProgress == null)
            {
                BuildProgress = new BuildProgress();
            }

            BuildProgress.CopyFrom(data.buildProgress ?? new BuildProgress());

            if (RosterProgress == null)
            {
                RosterProgress = new RosterProgress();
            }

            RosterProgress.CopyFrom(data.rosterProgress);
            MigrateLegacyBuildIntoRoster();
            SyncActiveBuildFromRoster();

            if (WeeklyDungeonProgress == null)
            {
                WeeklyDungeonProgress = new WeeklyDungeonProgress();
            }

            WeeklyDungeonProgress.CopyFrom(data.weeklyDungeonProgress);

            ActiveStageIndex = System.Math.Max(0, data.activeStageIndex);
            MarkDiskLoadComplete();
        }

        private static void MigrateLegacyBuildIntoRoster()
        {
            EnsureInitialized();
            RosterProgress.EnsureDefaults();
            BuildProgress active = RosterProgress.GetOrCreateBuild(RosterProgress.selectedClassId);
            bool empty = active.runeRanks == null || active.runeRanks.Count == 0;
            bool hasLegacy = BuildProgress != null && BuildProgress.runeRanks != null && BuildProgress.runeRanks.Count > 0;
            bool hasLegacySkills = BuildProgress != null && (
                !string.IsNullOrEmpty(BuildProgress.activeSkillId1) ||
                !string.IsNullOrEmpty(BuildProgress.activeSkillId2) ||
                !string.IsNullOrEmpty(BuildProgress.passiveSkillId));

            if (empty && (hasLegacy || hasLegacySkills) && BuildProgress != null)
            {
                active.CopyFrom(BuildProgress);
            }
        }

        public static void MarkDiskLoadComplete()
        {
            HasAppliedDiskSave = true;
        }

        public static SaveData CaptureSaveData()
        {
            EnsureInitialized();
            VillageProgress.EnsureDefaults();
            RosterProgress.EnsureDefaults();
            PushActiveBuildIntoRoster();
            MigrateInventoryToInstancesIfNeeded();

            SaveData data = new SaveData
            {
                playerProgress = new PlayerProgress
                {
                    level = Progress.level,
                    currentExperience = Progress.currentExperience,
                    gold = Progress.gold,
                    totalEnemiesKilled = Progress.totalEnemiesKilled,
                    highestCompletedStage = Progress.highestCompletedStage,
                    ownedItemIds = new List<string>(Progress.ownedItemIds ?? new List<string>())
                },
                inventoryItemIds = new List<string>(),
                inventoryInstances = new List<ItemInstanceSave>(),
                equipment = new List<EquipmentSaveData>(),
                mapProgress = new MapProgress
                {
                    unlockedLayer = MapProgress.unlockedLayer,
                    unlockedChapter = MapProgress.unlockedChapter,
                    unlockedLevel = MapProgress.unlockedLevel,
                    layerBossClearedUpTo = MapProgress.layerBossClearedUpTo,
                    pendingLayerBossChapter = MapProgress.pendingLayerBossChapter,
                    layerBossClearedChapterUpTo = MapProgress.layerBossClearedChapterUpTo,
                    activeLayer = MapProgress.activeLayer,
                    activeChapter = MapProgress.activeChapter,
                    activeLevel = MapProgress.activeLevel,
                    activeIsLayerBoss = MapProgress.activeIsLayerBoss
                },
                villageProgress = new VillageProgress
                {
                    buildings = new List<BuildingLevelSave>()
                },
                materials = new MaterialInventorySave
                {
                    stacks = new List<MaterialStackSave>()
                },
                buildProgress = BuildProgress != null ? BuildProgress.Clone() : new BuildProgress(),
                rosterProgress = RosterProgress != null ? RosterProgress.Clone() : new RosterProgress(),
                weeklyDungeonProgress = WeeklyDungeonProgress != null
                    ? WeeklyDungeonProgress.Clone()
                    : new WeeklyDungeonProgress(),
                activeStageIndex = ActiveStageIndex
            };

            for (int i = 0; i < InventoryInstances.Count; i++)
            {
                ItemInstanceSave src = InventoryInstances[i];
                if (src == null || string.IsNullOrEmpty(src.itemId))
                {
                    continue;
                }

                data.inventoryInstances.Add(src.Clone());
                data.inventoryItemIds.Add(src.itemId);
            }

            for (int i = 0; i < Equipment.Count; i++)
            {
                EquipmentSaveData slot = Equipment[i];
                data.equipment.Add(new EquipmentSaveData
                {
                    slot = slot.slot,
                    itemId = slot.itemId,
                    instanceUid = slot.instanceUid
                });
            }

            for (int i = 0; i < VillageProgress.buildings.Count; i++)
            {
                BuildingLevelSave slot = VillageProgress.buildings[i];
                data.villageProgress.buildings.Add(new BuildingLevelSave
                {
                    buildingType = slot.buildingType,
                    level = slot.level
                });
            }

            if (Materials.stacks != null)
            {
                for (int i = 0; i < Materials.stacks.Count; i++)
                {
                    MaterialStackSave slot = Materials.stacks[i];
                    if (slot == null || string.IsNullOrEmpty(slot.materialId) || slot.amount <= 0)
                    {
                        continue;
                    }

                    data.materials.stacks.Add(new MaterialStackSave
                    {
                        materialId = slot.materialId,
                        amount = slot.amount
                    });
                }
            }

            return data;
        }

        private static List<EquipmentSaveData> CreateDefaultEquipment()
        {
            return new List<EquipmentSaveData>
            {
                new EquipmentSaveData { slot = Game.Data.EquipmentSlot.Weapon, itemId = null, instanceUid = null },
                new EquipmentSaveData { slot = Game.Data.EquipmentSlot.Helmet, itemId = null, instanceUid = null },
                new EquipmentSaveData { slot = Game.Data.EquipmentSlot.Armor, itemId = null, instanceUid = null },
                new EquipmentSaveData { slot = Game.Data.EquipmentSlot.Accessory, itemId = null, instanceUid = null },
                new EquipmentSaveData { slot = Game.Data.EquipmentSlot.Shoulders, itemId = null, instanceUid = null },
                new EquipmentSaveData { slot = Game.Data.EquipmentSlot.Legs, itemId = null, instanceUid = null },
                new EquipmentSaveData { slot = Game.Data.EquipmentSlot.Boots, itemId = null, instanceUid = null },
                new EquipmentSaveData { slot = Game.Data.EquipmentSlot.Ring2, itemId = null, instanceUid = null }
            };
        }
    }
}
