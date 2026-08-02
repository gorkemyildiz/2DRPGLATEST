using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

namespace Game.Editor
{
    public class IdleRPGDemoSetupWizard : EditorWindow
    {
        private const string BASE_PATH = "Assets/_Game";
        private const string PREFAB_PATH = BASE_PATH + "/Prefabs";
        private const string SCENE_PATH = BASE_PATH + "/Scenes";
        private const string ANIMATION_PATH = BASE_PATH + "/Animations";
        private const string ART_PATH = BASE_PATH + "/Art";
        private const string SCRIPTABLE_PATH = BASE_PATH + "/ScriptableObjects";
        private const string PendingFixBattleUIFlag = BASE_PATH + "/Editor/.pending_fix_battle_ui";

        [InitializeOnLoadMethod]
        private static void AutoApplyPendingFixBattleUI()
        {
            if (!File.Exists(PendingFixBattleUIFlag))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(PendingFixBattleUIFlag))
                {
                    return;
                }

                try
                {
                    if (ApplyFixBattleUILayout())
                    {
                        File.Delete(PendingFixBattleUIFlag);
                        Debug.Log("[Setup] Pending Fix Battle UI Layout applied automatically.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[Setup] Auto Fix Battle UI failed: " + ex.Message);
                }
            };
        }

        [MenuItem("Tools/Idle RPG/Create Demo")]
        private static void CreateDemo()
        {
            if (EditorUtility.DisplayDialog("Create Demo", 
                "This will create the complete demo structure including scenes, prefabs, and animator controllers. Continue?", 
                "Yes", "Cancel"))
            {
                CreateDemoInternal();
            }
        }

        private static void CreateDemoInternal()
        {
            Debug.Log("[Setup] Starting demo creation...");

            CreatePlaceholderSprites();
            CreateAnimatorControllers();
            CreatePrefabs();
            CreateBattleScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Setup] Demo creation complete!");
            EditorUtility.DisplayDialog("Success", "Demo created successfully! Open BattleDemo scene to start.", "OK");
        }

        [MenuItem("Tools/Idle RPG/Bind Character Assets")]
        private static void BindCharacterAssets()
        {
            Debug.Log("[Setup] Binding character assets...");

            bool heroSuccess = BindHeroAssets();
            bool enemySuccess = BindEnemyAssets();
            bool bossSuccess = BindBossAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (heroSuccess && enemySuccess && bossSuccess)
            {
                Debug.Log("[Setup] All character assets bound successfully!");
                EditorUtility.DisplayDialog("Success", "Character assets bound successfully!", "OK");
            }
            else
            {
                Debug.LogWarning("[Setup] Some assets could not be bound. Check console for details.");
                EditorUtility.DisplayDialog("Partial Success", "Some assets could not be bound. Check console for details.", "OK");
            }
        }

        [MenuItem("Tools/Idle RPG/Setup Backgrounds")]
        private static void SetupBackgrounds()
        {
            Debug.Log("[Setup] Setting up background layers...");

            // First, configure background sprites
            ConfigureBackgroundSprites();

            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            
            if (scene.name != "BattleDemo")
            {
                string scenePath = SCENE_PATH + "/BattleDemo.unity";
                if (File.Exists(scenePath))
                {
                    scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "BattleDemo scene not found. Please create demo first.", "OK");
                    return;
                }
            }

            GameObject battleRoot = GameObject.Find("BattleRoot");
            if (battleRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "BattleRoot not found in scene. Please create demo first.", "OK");
                return;
            }

            CreateEnvironmentHierarchy(battleRoot.transform);

            // Rebind parallax scroller
            GameObject parallaxRoot = GameObject.Find("ParallaxRoot");
            if (parallaxRoot != null)
            {
                Game.Environment.ParallaxScroller scroller = parallaxRoot.GetComponent<Game.Environment.ParallaxScroller>();
                if (scroller != null)
                {
                    SerializedObject scrollerSO = new SerializedObject(scroller);
                    scrollerSO.FindProperty("farLayer.root").objectReferenceValue = GameObject.Find("FarLayer")?.transform;
                    scrollerSO.FindProperty("farLayer.loop").boolValue = true;
                    scrollerSO.FindProperty("midLayer.root").objectReferenceValue = GameObject.Find("MidLayer")?.transform;
                    scrollerSO.FindProperty("midLayer.loop").boolValue = true;
                    scrollerSO.FindProperty("foregroundLayer.root").objectReferenceValue = GameObject.Find("ForegroundLayer")?.transform;
                    scrollerSO.FindProperty("foregroundLayer.loop").boolValue = true;
                    scrollerSO.FindProperty("tilesPerLayer").intValue = 2;
                    scrollerSO.ApplyModifiedProperties();
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Setup] Background layers setup complete!");
            EditorUtility.DisplayDialog("Success", "Background layers setup successfully!", "OK");
        }

        private static void ConfigureBackgroundSprites()
        {
            string[] backgroundPaths = new string[]
            {
                ART_PATH + "/Backgrounds/Far/Far.png",
                ART_PATH + "/Backgrounds/Mid/Mid.png",
                ART_PATH + "/Backgrounds/Foreground/Foreground.png"
            };

            foreach (string path in backgroundPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[Setup] Background not found: {path}");
                    continue;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                    Debug.Log($"[Setup] Configured background sprite: {path}");
                }
            }
        }

        [MenuItem("Tools/Idle RPG/Create Sample Stage Data")]
        private static void CreateSampleStageData()
        {
            Debug.Log("[Setup] Creating sample stage data...");

            CreateSampleEnemyData();
            CreateSampleStageDataAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Setup] Sample stage data created!");
            EditorUtility.DisplayDialog("Success", "Sample stage data created in ScriptableObjects folder!", "OK");
        }

        [MenuItem("Tools/Idle RPG/Create Sample Loot Data")]
        private static void CreateSampleLootDataMenu()
        {
            CreateSampleLootData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Sample items, chest and item database created!", "OK");
        }

        [MenuItem("Tools/Idle RPG/Create Sample Map Data")]
        private static void CreateSampleMapDataMenu()
        {
            CreateSampleMapData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Layer 1 map data (3 chapters x 10 levels) + MapCatalog created!", "OK");
        }

        [MenuItem("Tools/Idle RPG/Create Sample Village Data")]
        private static void CreateSampleVillageDataMenu()
        {
            CreateSampleVillageData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Village buildings + catalog created!", "OK");
        }

        [MenuItem("Tools/Idle RPG/Create Sample Craft Data")]
        private static void CreateSampleCraftDataMenu()
        {
            CreateSampleCraftData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Materials, recipes, and CraftCatalog created!", "OK");
        }

        [MenuItem("Tools/Idle RPG/Create Sample Enhance Data")]
        private static void CreateSampleEnhanceDataMenu()
        {
            CreateSampleEnhanceData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Enchants + EnhanceCatalog created!", "OK");
        }

        [MenuItem("Tools/Idle RPG/Create Sample Build Data")]
        private static void CreateSampleBuildDataMenu()
        {
            CreateSampleBuildData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Runes, Skills, and catalogs created!", "OK");
        }

        [MenuItem("Tools/Idle RPG/Ensure Build UI")]
        private static void EnsureBuildUIMenu()
        {
            CreateSampleBuildData();
            EnsureBuildSystems();
            EnsureBuildPanelUI();
            WireInventoryBuildButton();
            BindBuildPanelServices();
            AssetDatabase.SaveAssets();

            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }

            EditorUtility.DisplayDialog(
                "Success",
                "Build systems + BuildPanel wired in the active scene.\nOpen Inventory → Build.",
                "OK");
        }

        [MenuItem("Tools/Idle RPG/Create Sample Roster Data")]
        private static void CreateSampleRosterDataMenu()
        {
            CreateSampleRosterData();
            CreateSampleWeeklyDungeonData();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Roster + Weekly Dungeon catalogs created!", "OK");
        }

        [MenuItem("Tools/Idle RPG/Ensure Roster UI")]
        private static void EnsureRosterUIMenu()
        {
            CreateSampleRosterData();
            EnsureRosterSystems();
            EnsureRosterPanelUI();
            WireInventoryClassButton();
            BindRosterPanelServices();
            AssetDatabase.SaveAssets();

            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }

            EditorUtility.DisplayDialog(
                "Success",
                "Roster systems + RosterPanel wired.\nOpen Inventory → CLASS.",
                "OK");
        }

        [MenuItem("Tools/Idle RPG/Ensure Weekly Dungeon UI")]
        private static void EnsureWeeklyDungeonUIMenu()
        {
            CreateSampleWeeklyDungeonData();
            EnsureWeeklyDungeonSystems();
            EnsureWeeklyDungeonPanelUI();
            WireMapSelectWeeklyButton();
            BindWeeklyDungeonPanelServices();
            AssetDatabase.SaveAssets();

            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }

            EditorUtility.DisplayDialog(
                "Success",
                "Weekly dungeon systems + panel wired.\nOpen Adventure → WEEKLY.",
                "OK");
        }

        /// <summary>
        /// Batch / CI entry: VillageHub + Faz 6 sample data + UI wiring.
        /// </summary>
        public static void BatchEnsureFaz6RosterAndWeekly()
        {
            string scenePath = SCENE_PATH + "/VillageHub.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogError("[Setup] VillageHub scene missing for Faz 6 batch.");
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            s_preserveHubLayout = true;
            CreateSampleRosterData();
            CreateSampleWeeklyDungeonData();
            EnsureRosterSystems();
            EnsureWeeklyDungeonSystems();
            EnsureRosterPanelUI();
            WireInventoryClassButton();
            BindRosterPanelServices();
            EnsureWeeklyDungeonPanelUI();
            WireMapSelectWeeklyButton();
            BindWeeklyDungeonPanelServices();
            AssetDatabase.SaveAssets();
            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[Setup] Faz 6 Roster + Weekly Dungeon batch complete.");
        }

        [MenuItem("Tools/Idle RPG/Create Village Hub")]
        private static void CreateVillageHubMenu()
        {
            if (!EditorUtility.DisplayDialog(
                "Create Village Hub",
                "This creates/updates the VillageHub scene (plaza menu with hero idle). Continue?",
                "Yes",
                "Cancel"))
            {
                return;
            }

            CreateVillageHubScene();
            UpdateBuildSettingsForHubAndBattle();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success",
                "VillageHub updated and set as first scene in Build Settings.\nOpen VillageHub and press Play.",
                "OK");
        }

        /// <summary>
        /// When true, Ensure* hub UI helpers only create missing objects and never overwrite
        /// RectTransform / visual tweaks the user made in the scene.
        /// </summary>
        private static UnityEngine.SceneManagement.Scene OpenOrGetHubScene(string scenePath)
        {
            UnityEngine.SceneManagement.Scene active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            string activePath = active.path != null ? active.path.Replace('\\', '/') : string.Empty;
            string targetPath = scenePath.Replace('\\', '/');

            // Keep the currently open VillageHub (including unsaved layout edits).
            if (active.IsValid() && activePath == targetPath)
            {
                return active;
            }

            return UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        }

        private static bool s_preserveHubLayout = true;

        [MenuItem("Tools/Idle RPG/Fix Hub UI Layout")]
        private static void FixHubUILayoutMenu()
        {
            string scenePath = SCENE_PATH + "/VillageHub.unity";
            if (!File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Error", "VillageHub scene not found. Run Create Village Hub first.", "OK");
                return;
            }

            // Strict non-destructive path: never rewrite RectTransforms of existing UI.
            s_preserveHubLayout = true;
            UnityEngine.SceneManagement.Scene scene = OpenOrGetHubScene(scenePath);
            GameObject hubRoot = GameObject.Find("HubRoot") ?? new GameObject("HubRoot");
            EnsureHubUINonDestructive(hubRoot.transform);
            CreateHubEnvironment(hubRoot.transform);
            CreateSampleMapData();
            CreateSampleVillageData();
            CreateSampleCraftData();
            CreateSampleEnhanceData();
            EnsureMapSelectUI();
            EnsureVillagePanelUI();
            EnsureBuildingPanelUI("LibraryPanel", Game.Data.VillageBuildingType.Library, "LIBRARY");
            EnsureBuildingPanelUI("MinePanel", Game.Data.VillageBuildingType.Mine, "MINE");
            EnsureCraftPanelUI();
            EnsureEnhancePanelUI();
            CreateSampleBuildData();
            EnsureBuildSystems();
            EnsureBuildPanelUI();
            WireInventoryBuildButton();
            CreateSampleRosterData();
            CreateSampleWeeklyDungeonData();
            EnsureRosterSystems();
            EnsureWeeklyDungeonSystems();
            EnsureRosterPanelUI();
            WireInventoryClassButton();
            EnsureWeeklyDungeonPanelUI();
            WireMapSelectWeeklyButton();
            EnsureAFKSummaryUI();
            EnsureLootSystems();
            BindHubReferences();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Success",
                "Hub UI wired without touching your layout.\n\n" +
                "Only missing objects were created.\n" +
                "Use Rebuild Hub UI (Reset Layout) for factory defaults.",
                "OK");
        }

        [MenuItem("Tools/Idle RPG/Rebuild Hub UI (Reset Layout)")]
        private static void RebuildHubUIResetLayoutMenu()
        {
            if (!EditorUtility.DisplayDialog(
                "Rebuild Hub UI",
                "This RESETS VillageHub UI positions/sizes to defaults and rebuilds panels.\n\n" +
                "Your manual layout tweaks will be lost. Continue?",
                "Reset",
                "Cancel"))
            {
                return;
            }

            string scenePath = SCENE_PATH + "/VillageHub.unity";
            if (!File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Error", "VillageHub scene not found. Run Create Village Hub first.", "OK");
                return;
            }

            s_preserveHubLayout = false;
            UnityEngine.SceneManagement.Scene scene = OpenOrGetHubScene(scenePath);
            GameObject hubRoot = GameObject.Find("HubRoot") ?? new GameObject("HubRoot");
            CreateHubUI(hubRoot.transform);
            CreateSampleMapData();
            CreateSampleVillageData();
            CreateSampleCraftData();
            CreateSampleEnhanceData();
            CreateHubEnvironment(hubRoot.transform);
            EnsureMapSelectUI();
            EnsureVillagePanelUI();
            EnsureBuildingPanelUI("LibraryPanel", Game.Data.VillageBuildingType.Library, "LIBRARY");
            EnsureBuildingPanelUI("MinePanel", Game.Data.VillageBuildingType.Mine, "MINE");
            EnsureCraftPanelUI();
            EnsureEnhancePanelUI();
            CreateSampleBuildData();
            EnsureBuildSystems();
            EnsureBuildPanelUI();
            WireInventoryBuildButton();
            CreateSampleRosterData();
            CreateSampleWeeklyDungeonData();
            EnsureRosterSystems();
            EnsureWeeklyDungeonSystems();
            EnsureRosterPanelUI();
            WireInventoryClassButton();
            EnsureWeeklyDungeonPanelUI();
            WireMapSelectWeeklyButton();
            EnsureAFKSummaryUI();
            EnsureLootSystems();
            BindHubReferences();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            s_preserveHubLayout = true;

            EditorUtility.DisplayDialog("Success", "Hub UI rebuilt to default layout.", "OK");
        }

        [MenuItem("Tools/Idle RPG/Delete Save File")]
        private static void DeleteSaveFileMenu()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Delete Save", "No save file found.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Delete Save", $"Delete save file?\n{path}", "Delete", "Cancel"))
            {
                File.Delete(path);
                EditorUtility.DisplayDialog("Delete Save", "Save file deleted.", "OK");
            }
        }

        [MenuItem("Tools/Idle RPG/Fix UI References")]
        private static void FixUIReferences()
        {
            string scenePath = SCENE_PATH + "/BattleDemo.unity";
            if (!File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Error", "BattleDemo scene not found.", "OK");
                return;
            }

            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            EnsureEventSystem();
            EnsureProgressionUI();
            EnsureLootSystems();
            EnsureSaveSystems();
            CreateSampleMapData();
            CreateSampleBuildData();
            EnsureBuildSystems();
            EnsureBuildPanelUI();
            WireInventoryBuildButton();
            FixBattleHudLayout();
            BindBattleControllerReferences();
            EnsureCameraFollow();
            BindBattleResultPanelHubScene();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Setup] UI references fixed, progression + loot systems ready.");
            EditorUtility.DisplayDialog("Success", "UI + Level Up + Loot systems are ready.", "OK");
        }

        [MenuItem("Tools/Idle RPG/Fix Battle UI Layout")]
        private static void FixBattleUILayoutMenu()
        {
            if (!ApplyFixBattleUILayout())
            {
                EditorUtility.DisplayDialog("Error", "BattleDemo scene not found.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("Success", "Battle HUD updated: SideNav (Adventure/Village/Inventory/Quit), Bag+Village text buttons removed.", "OK");
        }

        /// <summary>Batch-safe entry: no dialogs. Unity -executeMethod Game.Editor.IdleRPGDemoSetupWizard.ApplyFixBattleUILayoutBatch</summary>
        public static void ApplyFixBattleUILayoutBatch()
        {
            ApplyFixBattleUILayout();
        }

        private static bool ApplyFixBattleUILayout()
        {
            string scenePath = SCENE_PATH + "/BattleDemo.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogError("[Setup] BattleDemo scene not found.");
                return false;
            }

            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            FixBattleHudLayout();
            CreateSampleBuildData();
            EnsureBuildSystems();
            EnsureBuildPanelUI();
            WireInventoryBuildButton();
            BindBuildPanelServices();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return true;
        }

        [MenuItem("Tools/Idle RPG/Validate Demo")]
        private static void ValidateDemo()
        {
            Debug.Log("=== IDLE RPG DEMO VALIDATION REPORT ===\n");

            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            ValidateScene(errors, warnings);
            ValidatePrefabs(errors, warnings);
            ValidateAnimators(errors, warnings);
            ValidateData(errors, warnings);

            Debug.Log($"\n=== VALIDATION COMPLETE ===");
            Debug.Log($"Errors: {errors.Count}");
            Debug.Log($"Warnings: {warnings.Count}");

            if (errors.Count == 0 && warnings.Count == 0)
            {
                Debug.Log("✓ All systems validated successfully!");
                EditorUtility.DisplayDialog("Validation Success", "All systems validated successfully!", "OK");
            }
            else
            {
                if (errors.Count > 0)
                {
                    Debug.LogError($"✗ Found {errors.Count} errors:");
                    foreach (string error in errors)
                    {
                        Debug.LogError($"  - {error}");
                    }
                }

                if (warnings.Count > 0)
                {
                    Debug.LogWarning($"⚠ Found {warnings.Count} warnings:");
                    foreach (string warning in warnings)
                    {
                        Debug.LogWarning($"  - {warning}");
                    }
                }

                EditorUtility.DisplayDialog("Validation Issues", 
                    $"Found {errors.Count} errors and {warnings.Count} warnings. Check console for details.", "OK");
            }
        }

        [MenuItem("Tools/Idle RPG/Reset Generated Demo")]
        private static void ResetGeneratedDemo()
        {
            if (EditorUtility.DisplayDialog("Reset Demo", 
                "This will delete generated demo files (scenes, prefabs, controllers, sample data). User art assets will NOT be deleted. Continue?", 
                "Yes", "Cancel"))
            {
                Debug.Log("[Setup] Resetting generated demo files...");

                DeleteIfExists(SCENE_PATH + "/BattleDemo.unity");
                DeleteIfExists(SCENE_PATH + "/VillageHub.unity");
                DeleteIfExists(PREFAB_PATH + "/Hero/Hero.prefab");
                DeleteIfExists(PREFAB_PATH + "/Enemies/Enemy_MossSkeleton.prefab");
                DeleteIfExists(PREFAB_PATH + "/Enemies/Enemy_Dog.prefab");
                DeleteIfExists(PREFAB_PATH + "/Bosses/Enemy_Boss1.prefab");
                DeleteIfExists(ANIMATION_PATH + "/Hero/HeroController.controller");
                DeleteIfExists(ANIMATION_PATH + "/Enemies/EnemyController.controller");
                DeleteIfExists(ANIMATION_PATH + "/Bosses/BossController.controller");
                DeleteIfExists(SCRIPTABLE_PATH + "/Enemies/Enemy_MossSkeleton.asset");
                DeleteIfExists(SCRIPTABLE_PATH + "/Enemies/Enemy_Dog.asset");
                DeleteIfExists(SCRIPTABLE_PATH + "/Enemies/Enemy_Boss.asset");
                DeleteIfExists(SCRIPTABLE_PATH + "/Enemies/Enemy_DogBoss.asset");
                DeleteIfExists(SCRIPTABLE_PATH + "/Enemies/Enemy_Boss1.asset");
                DeleteIfExists(SCRIPTABLE_PATH + "/Stages/Stage_01.asset");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[Setup] Reset complete!");
                EditorUtility.DisplayDialog("Success", "Generated demo files have been reset.", "OK");
            }
        }

        private static void CreatePlaceholderSprites()
        {
            Debug.Log("[Setup] Creating placeholder sprites...");

            string placeholderPath = ART_PATH + "/Generated";
            if (!AssetDatabase.IsValidFolder(placeholderPath))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Game/Art/Generated"));
                AssetDatabase.Refresh();
            }

            CreatePlaceholderTexture(placeholderPath + "/HeroPlaceholder.png", new Color(0.2f, 0.6f, 0.8f));
            CreatePlaceholderTexture(placeholderPath + "/EnemyPlaceholder.png", new Color(0.8f, 0.3f, 0.2f));
        }

        private static void CreatePlaceholderTexture(string path, Color color)
        {
            if (File.Exists(Path.Combine(Application.dataPath, path.Replace("Assets/", ""))))
            {
                return;
            }

            Texture2D texture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            Debug.Log($"[Setup] Created placeholder texture: {path}");
        }

        private static void CreateAnimatorControllers()
        {
            Debug.Log("[Setup] Creating animator controllers...");

            CreateHeroAnimatorController();
            CreateEnemyAnimatorController();
            CreateBossAnimatorController();
        }

        private static void CreateHeroAnimatorController()
        {
            string controllerPath = ANIMATION_PATH + "/Hero/HeroController.controller";

            if (File.Exists(controllerPath))
            {
                Debug.Log($"[Setup] Hero controller already exists: {controllerPath}");
                return;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
            AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(300, 100, 0));
            AnimatorState attackState = rootStateMachine.AddState("Attack", new Vector3(300, 200, 0));
            AnimatorState deathState = rootStateMachine.AddState("Death", new Vector3(300, 300, 0));

            rootStateMachine.defaultState = idleState;

            AddTransition(idleState, walkState, "IsWalking", true);
            AddTransition(walkState, idleState, "IsWalking", false);
            AddTransition(idleState, attackState, "Attack", AnimatorConditionMode.If);
            AddTransition(walkState, attackState, "Attack", AnimatorConditionMode.If);
            AddTransition(attackState, idleState, "", AnimatorConditionMode.If, true);

            AnimatorStateTransition anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0;
            anyToDeath.canTransitionToSelf = false;

            Debug.Log($"[Setup] Created hero animator controller: {controllerPath}");
        }

        private static void CreateEnemyAnimatorController()
        {
            string controllerPath = ANIMATION_PATH + "/Enemies/EnemyController.controller";

            if (File.Exists(controllerPath))
            {
                Debug.Log($"[Setup] Enemy controller already exists: {controllerPath}");
                return;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
            AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(300, 100, 0));
            AnimatorState attackState = rootStateMachine.AddState("Attack", new Vector3(300, 200, 0));
            AnimatorState deathState = rootStateMachine.AddState("Death", new Vector3(300, 300, 0));

            rootStateMachine.defaultState = idleState;

            AddTransition(idleState, walkState, "IsWalking", true);
            AddTransition(walkState, idleState, "IsWalking", false);
            AddTransition(idleState, attackState, "Attack", AnimatorConditionMode.If);
            AddTransition(walkState, attackState, "Attack", AnimatorConditionMode.If);
            AddTransition(attackState, idleState, "", AnimatorConditionMode.If, true);

            AnimatorStateTransition anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0;
            anyToDeath.canTransitionToSelf = false;

            Debug.Log($"[Setup] Created enemy animator controller: {controllerPath}");
        }

        private static void CreateBossAnimatorController()
        {
            EnsureFolder(ANIMATION_PATH + "/Bosses");
            string controllerPath = ANIMATION_PATH + "/Bosses/BossController.controller";

            if (File.Exists(controllerPath))
            {
                Debug.Log($"[Setup] Boss controller already exists: {controllerPath}");
                return;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            AnimatorState idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
            AnimatorState walkState = rootStateMachine.AddState("Walk", new Vector3(300, 100, 0));
            AnimatorState attackState = rootStateMachine.AddState("Attack", new Vector3(300, 200, 0));
            AnimatorState deathState = rootStateMachine.AddState("Death", new Vector3(300, 300, 0));

            rootStateMachine.defaultState = idleState;

            AddTransition(idleState, walkState, "IsWalking", true);
            AddTransition(walkState, idleState, "IsWalking", false);
            AddTransition(idleState, attackState, "Attack", AnimatorConditionMode.If);
            AddTransition(walkState, attackState, "Attack", AnimatorConditionMode.If);
            AddTransition(attackState, idleState, "", AnimatorConditionMode.If, true);

            AnimatorStateTransition anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0;
            anyToDeath.canTransitionToSelf = false;

            Debug.Log($"[Setup] Created boss animator controller: {controllerPath}");
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string relative = assetFolderPath.Replace("Assets/", "");
            string fullPath = Path.Combine(Application.dataPath, relative);
            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            if (!string.IsNullOrEmpty(parameter))
            {
                // Bool false must use IfNot — If+threshold 0 still means "if true".
                AnimatorConditionMode mode = value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot;
                transition.AddCondition(mode, 0, parameter);
            }
            transition.hasExitTime = false;
            transition.duration = 0;
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, bool hasExitTime = false)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            if (!string.IsNullOrEmpty(parameter))
            {
                transition.AddCondition(mode, 0, parameter);
            }
            transition.hasExitTime = hasExitTime;
            transition.exitTime = hasExitTime ? 1f : 0f;
            transition.duration = 0;
        }

        private static void CreatePrefabs()
        {
            Debug.Log("[Setup] Creating prefabs...");

            CreateHeroPrefab();
            CreateEnemyPrefab();
            CreateBossPrefab();
        }

        private static void CreateHeroPrefab()
        {
            string prefabPath = PREFAB_PATH + "/Hero/Hero.prefab";

            if (File.Exists(prefabPath))
            {
                Debug.Log($"[Setup] Hero prefab already exists: {prefabPath}");
                return;
            }

            GameObject hero = new GameObject("Hero");

            SpriteRenderer sr = hero.AddComponent<SpriteRenderer>();
            Sprite placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(ART_PATH + "/Generated/HeroPlaceholder.png");
            sr.sprite = placeholder;
            sr.sortingLayerName = "Default";

            Animator animator = hero.AddComponent<Animator>();
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIMATION_PATH + "/Hero/HeroController.controller");
            animator.runtimeAnimatorController = controller;

            hero.AddComponent<Game.Combat.Health>();
            hero.AddComponent<Game.Combat.CharacterDeathHandler>();
            hero.AddComponent<Game.Units.HeroUnit>();
            hero.AddComponent<Game.Units.HeroProgression>();

            CreateHealthCanvas(hero.transform);

            PrefabUtility.SaveAsPrefabAsset(hero, prefabPath);
            DestroyImmediate(hero);

            Debug.Log($"[Setup] Created hero prefab: {prefabPath}");
        }

        private static void CreateEnemyPrefab()
        {
            string prefabPath = PREFAB_PATH + "/Enemies/Enemy_Dog.prefab";

            if (File.Exists(prefabPath))
            {
                Debug.Log($"[Setup] Enemy prefab already exists: {prefabPath}");
                return;
            }

            GameObject enemy = new GameObject("Enemy_Dog");

            SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
            Sprite placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(ART_PATH + "/Generated/EnemyPlaceholder.png");
            sr.sprite = placeholder;
            sr.sortingLayerName = "Default";

            Animator animator = enemy.AddComponent<Animator>();
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIMATION_PATH + "/Enemies/EnemyController.controller");
            animator.runtimeAnimatorController = controller;

            enemy.AddComponent<Game.Combat.Health>();
            enemy.AddComponent<Game.Combat.CharacterDeathHandler>();
            enemy.AddComponent<Game.Units.EnemyUnit>();

            GameObject healthCanvasObj = CreateHealthCanvas(enemy.transform);

            PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
            DestroyImmediate(enemy);

            Debug.Log($"[Setup] Created enemy prefab: {prefabPath}");
        }

        private static void CreateBossPrefab()
        {
            EnsureFolder(PREFAB_PATH + "/Bosses");
            string prefabPath = PREFAB_PATH + "/Bosses/Enemy_Boss1.prefab";

            if (File.Exists(prefabPath))
            {
                Debug.Log($"[Setup] Boss prefab already exists: {prefabPath}");
                return;
            }

            GameObject boss = new GameObject("Enemy_Boss1");

            SpriteRenderer sr = boss.AddComponent<SpriteRenderer>();
            Sprite placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(ART_PATH + "/Generated/EnemyPlaceholder.png");
            sr.sprite = placeholder;
            sr.sortingLayerName = "Default";
            sr.color = Color.white;
            sr.flipX = true; // Boss sprites face right in art; flip toward hero

            Animator animator = boss.AddComponent<Animator>();
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIMATION_PATH + "/Bosses/BossController.controller");
            animator.runtimeAnimatorController = controller;

            boss.AddComponent<Game.Combat.Health>();
            boss.AddComponent<Game.Combat.CharacterDeathHandler>();
            boss.AddComponent<Game.Units.EnemyUnit>();

            CreateHealthCanvas(boss.transform);

            // Boss slightly larger by default
            boss.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            PrefabUtility.SaveAsPrefabAsset(boss, prefabPath);
            DestroyImmediate(boss);

            Debug.Log($"[Setup] Created boss prefab: {prefabPath}");
        }

        private static GameObject CreateHealthCanvas(Transform parent)
        {
            GameObject canvasObj = new GameObject("HealthCanvas");
            canvasObj.transform.SetParent(parent);
            canvasObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100, 12);

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localScale = Vector3.one;

            UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(canvasObj.transform);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localScale = Vector3.one;

            UnityEngine.UI.Image fillImage = fillObj.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.3f, 1f);

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.pivot = new Vector2(0, 0.5f);

            Game.UI.HealthBarUI healthBarUI = canvasObj.AddComponent<Game.UI.HealthBarUI>();
            SerializedObject so = new SerializedObject(healthBarUI);
            so.FindProperty("fillTransform").objectReferenceValue = fillRect;
            so.ApplyModifiedProperties();

            return canvasObj;
        }

        private static void CreateBattleScene()
        {
            Debug.Log("[Setup] Creating Battle scene...");

            string scenePath = SCENE_PATH + "/BattleDemo.unity";

            UnityEngine.SceneManagement.Scene scene;

            if (File.Exists(scenePath))
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log($"[Setup] Opened existing scene: {scenePath}");
            }
            else
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log($"[Setup] Created new scene: {scenePath}");
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 0, -10);
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.backgroundColor = new Color(0.05f, 0.15f, 0.15f, 1f);
            }

            GameObject battleRoot = GameObject.Find("BattleRoot") ?? new GameObject("BattleRoot");

            CreatePointsHierarchy(battleRoot.transform);
            CreateEnvironmentHierarchy(battleRoot.transform);
            CreateUnitsHierarchy(battleRoot.transform);
            CreateUIHierarchy(battleRoot.transform);
            CreateSystemsHierarchy(battleRoot.transform);
            EnsureCameraFollow();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            Debug.Log($"[Setup] Battle scene created: {scenePath}");
        }

        private static void CreateVillageHubScene()
        {
            Debug.Log("[Setup] Creating Village Hub scene...");

            EnsureFolder(SCENE_PATH);

            string scenePath = SCENE_PATH + "/VillageHub.unity";
            UnityEngine.SceneManagement.Scene scene;

            if (File.Exists(scenePath))
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                Debug.Log($"[Setup] Opened existing hub scene: {scenePath}");
            }
            else
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log($"[Setup] Created new hub scene: {scenePath}");
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 0f, -10f);
                cam.orthographic = true;
                cam.orthographicSize = 1.5f;
                cam.backgroundColor = new Color(0.08f, 0.16f, 0.18f, 1f);
            }

            GameObject hubRoot = FindOrCreate("HubRoot", null);
            CreateHubEnvironment(hubRoot.transform);
            CreateHubPoints(hubRoot.transform);
            CreateHubUnits(hubRoot.transform);
            CreateHubSystems(hubRoot.transform);
            bool previousPreserve = s_preserveHubLayout;
            s_preserveHubLayout = GameObject.Find("HubMenu") != null;
            CreateHubUI(hubRoot.transform);
            s_preserveHubLayout = previousPreserve;
            EnsureEventSystem();
            EnsureLootSystems();
            EnsureSaveSystems();
            CreateSampleMapData();
            CreateSampleVillageData();
            CreateSampleCraftData();
            CreateSampleEnhanceData();
            EnsureMapSelectUI();
            EnsureVillagePanelUI();
            EnsureBuildingPanelUI("LibraryPanel", Game.Data.VillageBuildingType.Library, "LIBRARY");
            EnsureBuildingPanelUI("MinePanel", Game.Data.VillageBuildingType.Mine, "MINE");
            EnsureCraftPanelUI();
            EnsureEnhancePanelUI();
            CreateSampleBuildData();
            EnsureBuildSystems();
            EnsureBuildPanelUI();
            WireInventoryBuildButton();
            CreateSampleRosterData();
            CreateSampleWeeklyDungeonData();
            EnsureRosterSystems();
            EnsureWeeklyDungeonSystems();
            EnsureRosterPanelUI();
            WireInventoryClassButton();
            EnsureWeeklyDungeonPanelUI();
            WireMapSelectWeeklyButton();
            EnsureAFKSummaryUI();
            BindHubReferences();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            Debug.Log($"[Setup] Village Hub scene ready: {scenePath}");
        }

        private static void CreateHubEnvironment(Transform root)
        {
            GameObject env = FindOrCreate("Environment", root);

            // Prefer dedicated village art; fall back to battle backgrounds.
            string farPath = FirstExistingArtPath(
                ART_PATH + "/Village/Backgrounds/Plaza_Far.png",
                ART_PATH + "/Backgrounds/Far/Far.png");
            string midPath = FirstExistingArtPath(
                ART_PATH + "/Village/Backgrounds/Plaza_Mid.png",
                ART_PATH + "/Backgrounds/Mid/Mid.png");
            string groundPath = FirstExistingArtPath(
                ART_PATH + "/Village/Backgrounds/Plaza_Ground.png",
                ART_PATH + "/Backgrounds/Foreground/Foreground.png");

            CreateStaticBackdrop(
                "FarBackdrop",
                env.transform,
                farPath,
                new Vector3(0f, 0.15f, 2f),
                Color.white);

            CreateStaticBackdrop(
                "PlazaBackdrop",
                env.transform,
                midPath,
                new Vector3(0f, 0f, 1f),
                Color.white);

            GameObject ground = FindOrCreate("Ground", env.transform);
            ground.transform.localPosition = new Vector3(0f, -1.15f, 0f);
            SpriteRenderer groundSr = ground.GetComponent<SpriteRenderer>();
            if (groundSr == null)
            {
                groundSr = ground.AddComponent<SpriteRenderer>();
            }

            Sprite groundSprite = !string.IsNullOrEmpty(groundPath)
                ? AssetDatabase.LoadAssetAtPath<Sprite>(groundPath)
                : null;
            if (groundSprite != null)
            {
                groundSr.sprite = groundSprite;
                groundSr.color = Color.white;
            }
            else
            {
                groundSr.color = new Color(0.2f, 0.28f, 0.22f, 1f);
            }

            groundSr.sortingOrder = 5;

            // Optional world building props in plaza
            EnsurePlazaBuildingProp(env.transform, "Prop_Library", ART_PATH + "/Village/Buildings/Library.png", new Vector3(-1.6f, -0.55f, 0f), -2);
            EnsurePlazaBuildingProp(env.transform, "Prop_Mine", ART_PATH + "/Village/Buildings/Mine.png", new Vector3(-0.2f, -0.55f, 0f), -1);
            EnsurePlazaBuildingProp(env.transform, "Prop_Blacksmith", ART_PATH + "/Village/Buildings/Blacksmith.png", new Vector3(1.1f, -0.55f, 0f), 0);
        }

        private static string FirstExistingArtPath(params string[] paths)
        {
            if (paths == null)
            {
                return null;
            }

            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrEmpty(paths[i]) && File.Exists(paths[i]))
                {
                    return paths[i];
                }
            }

            return paths.Length > 0 ? paths[paths.Length - 1] : null;
        }

        private static void EnsurePlazaBuildingProp(Transform env, string name, string spritePath, Vector3 localPos, int sortingOrder)
        {
            if (!File.Exists(spritePath))
            {
                Transform old = env.Find(name);
                if (old != null)
                {
                    return;
                }

                return;
            }

            bool created = env.Find(name) == null;
            GameObject prop = FindOrCreate(name, env);
            if (created)
            {
                prop.transform.localPosition = localPos;
            }

            SpriteRenderer sr = prop.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = prop.AddComponent<SpriteRenderer>();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null && (created || sr.sprite == null))
            {
                sr.sprite = sprite;
            }

            if (created)
            {
                sr.color = Color.white;
                sr.sortingOrder = sortingOrder;
            }

            BoxCollider2D col = prop.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = prop.AddComponent<BoxCollider2D>();
            }

            if (sr.sprite != null)
            {
                col.size = sr.sprite.bounds.size;
            }
            else
            {
                col.size = new Vector2(1.2f, 1.4f);
            }

            Game.Data.VillageBuildingType buildingType = Game.Data.VillageBuildingType.Library;
            if (name.Contains("Mine"))
            {
                buildingType = Game.Data.VillageBuildingType.Mine;
            }
            else if (name.Contains("Blacksmith"))
            {
                buildingType = Game.Data.VillageBuildingType.Blacksmith;
            }

            Game.Hub.HubBuildingHotspot hotspot = prop.GetComponent<Game.Hub.HubBuildingHotspot>();
            if (hotspot == null)
            {
                hotspot = prop.AddComponent<Game.Hub.HubBuildingHotspot>();
            }

            Game.Hub.HubController hub = UnityEngine.Object.FindFirstObjectByType<Game.Hub.HubController>(FindObjectsInactive.Include);
            hotspot.Configure(buildingType, hub);
            EditorUtility.SetDirty(prop);
        }

        private static void CreateStaticBackdrop(string name, Transform parent, string spritePath, Vector3 localPos, Color tint)
        {
            GameObject obj = FindOrCreate(name, parent);
            obj.transform.localPosition = localPos;

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = obj.AddComponent<SpriteRenderer>();
            }

            if (File.Exists(spritePath))
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    sr.sprite = sprite;
                }
            }

            sr.color = tint;
            sr.sortingOrder = name.Contains("Far") ? -20 : -10;
        }

        private static void CreateHubPoints(Transform root)
        {
            GameObject points = FindOrCreate("Points", root);
            // Hero stands on the right side of the plaza
            CreatePoint("HeroStandPoint", points.transform, new Vector3(1.35f, -0.85f, 0f));
        }

        private static void CreateHubUnits(Transform root)
        {
            FindOrCreate("Units", root);
        }

        private static void CreateHubSystems(Transform root)
        {
            GameObject systems = FindOrCreate("Systems", root);
            GameObject hubControllerObj = FindOrCreate("HubController", systems.transform);
            if (hubControllerObj.GetComponent<Game.Hub.HubPlazaClickRouter>() == null)
            {
                hubControllerObj.AddComponent<Game.Hub.HubPlazaClickRouter>();
            }

            FindOrCreate("RewardController", systems.transform);
            FindOrCreate("InventoryController", systems.transform);
            FindOrCreate("EquipmentController", systems.transform);
            FindOrCreate("ChestController", systems.transform);
            GameObject villageController = FindOrCreate("VillageController", systems.transform);
            GameObject craftController = FindOrCreate("CraftController", systems.transform);
            GameObject enhanceController = FindOrCreate("EnhanceController", systems.transform);
            GameObject buildController = FindOrCreate("BuildController", systems.transform);
            GameObject rosterController = FindOrCreate("RosterController", systems.transform);
            GameObject weeklyDungeonController = FindOrCreate("WeeklyDungeonController", systems.transform);
            GameObject afkController = FindOrCreate("AFKController", systems.transform);
            GameObject saveController = FindOrCreate("SaveController", systems.transform);

            GameObject rewardController = GameObject.Find("RewardController");
            if (rewardController != null && rewardController.GetComponent<Game.Rewards.RewardService>() == null)
            {
                rewardController.AddComponent<Game.Rewards.RewardService>();
            }

            if (villageController.GetComponent<Game.Village.VillageService>() == null)
            {
                villageController.AddComponent<Game.Village.VillageService>();
            }

            if (craftController.GetComponent<Game.Crafting.CraftService>() == null)
            {
                craftController.AddComponent<Game.Crafting.CraftService>();
            }

            if (enhanceController.GetComponent<Game.Inventory.ItemEnhanceService>() == null)
            {
                enhanceController.AddComponent<Game.Inventory.ItemEnhanceService>();
            }

            if (buildController.GetComponent<Game.Build.BuildController>() == null)
            {
                buildController.AddComponent<Game.Build.BuildController>();
            }

            if (buildController.GetComponent<Game.Build.RuneService>() == null)
            {
                buildController.AddComponent<Game.Build.RuneService>();
            }

            if (buildController.GetComponent<Game.Build.SkillService>() == null)
            {
                buildController.AddComponent<Game.Build.SkillService>();
            }

            if (rosterController.GetComponent<Game.Roster.RosterService>() == null)
            {
                rosterController.AddComponent<Game.Roster.RosterService>();
            }

            if (weeklyDungeonController.GetComponent<Game.Dungeon.WeeklyDungeonService>() == null)
            {
                weeklyDungeonController.AddComponent<Game.Dungeon.WeeklyDungeonService>();
            }

            if (afkController.GetComponent<Game.AFK.AFKRewardService>() == null)
            {
                afkController.AddComponent<Game.AFK.AFKRewardService>();
            }

            if (saveController.GetComponent<Game.Save.SaveService>() == null)
            {
                saveController.AddComponent<Game.Save.SaveService>();
            }

            if (saveController.GetComponent<Game.Save.GameSaveController>() == null)
            {
                saveController.AddComponent<Game.Save.GameSaveController>();
            }
        }

        private static void CreateHubUI(Transform root)
        {
            // Respects s_preserveHubLayout (Fix Hub / existing hub keep layout).
            CreateHubUIInternal(root, forceDefaults: !s_preserveHubLayout);
        }

        /// <summary>
        /// Fix Hub entry: never moves/resizes existing UI. Only creates missing pieces + rebinds.
        /// </summary>
        private static void EnsureHubUINonDestructive(Transform root)
        {
            s_preserveHubLayout = true;
            CreateHubUIInternal(root, forceDefaults: false);
        }

        private static void CreateHubUIInternal(Transform root, bool forceDefaults)
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                canvas = new GameObject("ScreenCanvas");
                canvas.transform.SetParent(root, false);

                Canvas c = canvas.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null && forceDefaults)
            {
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else if (scaler != null && scaler.uiScaleMode != UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            GameObject menuRoot = FindOrCreate("HubMenu", canvas.transform);
            RectTransform menuRect = menuRoot.GetComponent<RectTransform>();
            if (menuRect == null)
            {
                menuRect = menuRoot.AddComponent<RectTransform>();
                menuRect.anchorMin = Vector2.zero;
                menuRect.anchorMax = Vector2.one;
                menuRect.offsetMin = Vector2.zero;
                menuRect.offsetMax = Vector2.zero;
            }
            else if (forceDefaults)
            {
                menuRect.anchorMin = Vector2.zero;
                menuRect.anchorMax = Vector2.one;
                menuRect.offsetMin = Vector2.zero;
                menuRect.offsetMax = Vector2.zero;
            }

            if (menuRoot.GetComponent<Game.UI.HubMenuUI>() == null)
            {
                menuRoot.AddComponent<Game.UI.HubMenuUI>();
            }

            bool writeDefaults = forceDefaults;

            // Legacy "VILLAGE / Plaza / Lv / Auto-save" strip — remove completely.
            Transform legacyMenuPanel = menuRoot.transform.Find("MenuPanel");
            if (legacyMenuPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyMenuPanel.gameObject);
            }

            Game.UI.HubSideNavUI sideNav = EnsureHubSideNav(menuRoot.transform, writeDefaults);
            UnityEngine.UI.Text goldText = EnsureHubGoldBar(menuRoot.transform, writeDefaults);

            SerializedObject menuSO = new SerializedObject(menuRoot.GetComponent<Game.UI.HubMenuUI>());
            menuSO.FindProperty("sideNav").objectReferenceValue = sideNav;
            if (sideNav != null)
            {
                SerializedObject navSO = new SerializedObject(sideNav);
                SerializedProperty itemsProp = navSO.FindProperty("items");
                if (itemsProp != null && itemsProp.isArray)
                {
                    for (int i = 0; i < itemsProp.arraySize; i++)
                    {
                        SerializedProperty item = itemsProp.GetArrayElementAtIndex(i);
                        int tab = item.FindPropertyRelative("tab").enumValueIndex;
                        UnityEngine.UI.Button btn = item.FindPropertyRelative("button").objectReferenceValue as UnityEngine.UI.Button;
                        if (btn == null)
                        {
                            continue;
                        }

                        // HubNavTab: None=0 Adventure=1 Village=2 Inventory=3 Quit=4
                        if (tab == 1) menuSO.FindProperty("adventureButton").objectReferenceValue = btn;
                        else if (tab == 2) menuSO.FindProperty("villageButton").objectReferenceValue = btn;
                        else if (tab == 3) menuSO.FindProperty("inventoryButton").objectReferenceValue = btn;
                        else if (tab == 4) menuSO.FindProperty("quitButton").objectReferenceValue = btn;
                    }
                }
            }

            menuSO.FindProperty("goldText").objectReferenceValue = goldText;
            menuSO.ApplyModifiedProperties();
        }

        private static void HideHubChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return;
            }

            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private static void SetHubTextPos(GameObject textObj, Vector2 anchoredPos, Vector2 size)
        {
            if (textObj == null)
            {
                return;
            }

            RectTransform rt = textObj.GetComponent<RectTransform>();
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static Game.UI.HubSideNavUI EnsureHubSideNav(Transform hubMenuRoot, bool forceDefaults)
        {
            Transform existing = hubMenuRoot.Find("SideNav");
            if (existing != null && !forceDefaults)
            {
                Game.UI.HubSideNavUI kept = existing.GetComponent<Game.UI.HubSideNavUI>();
                if (kept != null)
                {
                    ApplySideNavSpritesIfMissing(kept);
                    ResetSideNavScales(existing);
                    SerializedObject keptSO = new SerializedObject(kept);
                    keptSO.FindProperty("normalScale").floatValue = 1f;
                    keptSO.FindProperty("selectedScale").floatValue = 1.22f;
                    keptSO.ApplyModifiedProperties();
                    return kept;
                }
            }

            if (existing != null && forceDefaults)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject rail = new GameObject("SideNav");
            rail.transform.SetParent(hubMenuRoot, false);
            RectTransform railRect = rail.AddComponent<RectTransform>();
            railRect.anchorMin = new Vector2(0f, 0.5f);
            railRect.anchorMax = new Vector2(0f, 0.5f);
            railRect.pivot = new Vector2(0f, 0.5f);
            railRect.sizeDelta = new Vector2(110f, 420f);
            railRect.anchoredPosition = new Vector2(18f, -10f);

            UnityEngine.UI.VerticalLayoutGroup layout = rail.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Game.UI.HubSideNavUI sideNav = rail.AddComponent<Game.UI.HubSideNavUI>();

            var built = new List<Game.UI.HubSideNavUI.NavItem>();
            built.Add(CreateSideNavIcon(rail.transform, "AdventureIcon", Game.UI.HubNavTab.Adventure, "Icon_Adventure.png"));
            built.Add(CreateSideNavIcon(rail.transform, "VillageIcon", Game.UI.HubNavTab.Village, "Icon_Village.png"));
            built.Add(CreateSideNavIcon(rail.transform, "InventoryIcon", Game.UI.HubNavTab.Inventory, "Icon_Inventory.png"));
            built.Add(CreateSideNavIcon(rail.transform, "QuitIcon", Game.UI.HubNavTab.Quit, "Icon_Quit.png"));

            SerializedObject navSO = new SerializedObject(sideNav);
            SerializedProperty itemsProp = navSO.FindProperty("items");
            itemsProp.arraySize = built.Count;
            for (int i = 0; i < built.Count; i++)
            {
                SerializedProperty item = itemsProp.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("tab").enumValueIndex = (int)built[i].tab;
                item.FindPropertyRelative("button").objectReferenceValue = built[i].button;
                item.FindPropertyRelative("scaleRoot").objectReferenceValue = built[i].scaleRoot;
                item.FindPropertyRelative("iconImage").objectReferenceValue = built[i].iconImage;
            }

            navSO.FindProperty("normalScale").floatValue = 1f;
            navSO.FindProperty("selectedScale").floatValue = 1.22f;
            navSO.ApplyModifiedProperties();

            // Clear any stuck oversized scale from a previous Quit selection.
            for (int i = 0; i < built.Count; i++)
            {
                if (built[i].scaleRoot != null)
                {
                    built[i].scaleRoot.localScale = Vector3.one;
                }
            }

            return sideNav;
        }

        private static Game.UI.HubSideNavUI.NavItem CreateSideNavIcon(
            Transform parent,
            string name,
            Game.UI.HubNavTab tab,
            string fileName)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96f, 96f);

            UnityEngine.UI.LayoutElement le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = 96f;
            le.preferredHeight = 96f;
            le.minWidth = 72f;
            le.minHeight = 72f;

            UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
            img.sprite = LoadHubSideNavSprite(fileName);
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = true;

            UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            UnityEngine.UI.ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 1f, 0.96f, 1f);
            colors.pressedColor = new Color(0.8f, 0.9f, 0.86f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            return new Game.UI.HubSideNavUI.NavItem
            {
                tab = tab,
                button = btn,
                scaleRoot = rt,
                iconImage = img
            };
        }

        private static Sprite LoadHubSideNavSprite(string fileName)
        {
            string path = ART_PATH + "/UI/Hub/SideNav/" + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            // Multiple-sprite imports expose children; take the first.
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is Sprite s)
                    {
                        return s;
                    }
                }
            }

            return null;
        }

        private static void ApplySideNavSpritesIfMissing(Game.UI.HubSideNavUI sideNav)
        {
            if (sideNav == null)
            {
                return;
            }

            TrySetIcon(sideNav, Game.UI.HubNavTab.Adventure, "Icon_Adventure.png");
            TrySetIcon(sideNav, Game.UI.HubNavTab.Village, "Icon_Village.png");
            TrySetIcon(sideNav, Game.UI.HubNavTab.Inventory, "Icon_Inventory.png");
            TrySetIcon(sideNav, Game.UI.HubNavTab.Quit, "Icon_Quit.png");
        }

        private static void ResetSideNavScales(Transform sideNavRoot)
        {
            if (sideNavRoot == null)
            {
                return;
            }

            for (int i = 0; i < sideNavRoot.childCount; i++)
            {
                sideNavRoot.GetChild(i).localScale = Vector3.one;
            }
        }

        private static void TrySetIcon(Game.UI.HubSideNavUI sideNav, Game.UI.HubNavTab tab, string fileName)
        {
            UnityEngine.UI.Image img = sideNav.GetIconImage(tab);
            if (img != null && img.sprite == null)
            {
                img.sprite = LoadHubSideNavSprite(fileName);
                img.preserveAspect = true;
            }
        }

        private static void EnsurePanelSlider(GameObject panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform card = panelRoot.transform.Find("Card");
            RectTransform slideTarget = card != null
                ? card.GetComponent<RectTransform>()
                : panelRoot.GetComponent<RectTransform>();

            Game.UI.UIPanelSlider.EnsureOn(panelRoot, slideTarget, Game.UI.UIPanelSlider.SlideFrom.Left);
        }

        private static UnityEngine.UI.Text EnsureHubGoldBar(Transform hubMenuRoot, bool forceDefaults)
        {
            Transform existingRoot = FindDeepChild(hubMenuRoot, "GoldBarRoot");
            if (existingRoot != null && !forceDefaults)
            {
                Transform textTf = FindDeepChild(existingRoot, "GoldText");
                UnityEngine.UI.Text existingText = textTf != null
                    ? textTf.GetComponent<UnityEngine.UI.Text>()
                    : existingRoot.GetComponentInChildren<UnityEngine.UI.Text>(true);

                UnityEngine.UI.Image barImage = existingRoot.GetComponent<UnityEngine.UI.Image>();
                if (barImage != null && barImage.sprite == null)
                {
                    Sprite goldBarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_PATH + "/UI/Hub/GoldBar.png");
                    if (goldBarSprite != null)
                    {
                        barImage.sprite = goldBarSprite;
                        barImage.preserveAspect = true;
                        barImage.color = Color.white;
                    }
                }

                if (existingText != null)
                {
                    return existingText;
                }

                GameObject goldObj = new GameObject("GoldText");
                goldObj.transform.SetParent(existingRoot, false);
                RectTransform goldRect = goldObj.AddComponent<RectTransform>();
                goldRect.anchorMin = new Vector2(0f, 0f);
                goldRect.anchorMax = new Vector2(1f, 1f);
                goldRect.offsetMin = new Vector2(52f, 6f);
                goldRect.offsetMax = new Vector2(-18f, -6f);
                UnityEngine.UI.Text goldTextOnly = goldObj.AddComponent<UnityEngine.UI.Text>();
                goldTextOnly.text = "0";
                goldTextOnly.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                goldTextOnly.fontSize = 20;
                goldTextOnly.fontStyle = FontStyle.Bold;
                goldTextOnly.color = new Color(1f, 0.92f, 0.55f, 1f);
                goldTextOnly.alignment = TextAnchor.MiddleCenter;
                return goldTextOnly;
            }

            if (existingRoot != null && forceDefaults)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot.gameObject);
            }

            GameObject rootGo = new GameObject("GoldBarRoot");
            rootGo.transform.SetParent(hubMenuRoot, false);
            RectTransform rootRect = rootGo.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-24f, -18f);
            rootRect.sizeDelta = new Vector2(220f, 48f);

            UnityEngine.UI.Image barImageNew = rootGo.AddComponent<UnityEngine.UI.Image>();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_PATH + "/UI/Hub/GoldBar.png");
            if (sprite != null)
            {
                barImageNew.sprite = sprite;
                barImageNew.type = UnityEngine.UI.Image.Type.Simple;
                barImageNew.preserveAspect = true;
                barImageNew.color = Color.white;
            }
            else
            {
                barImageNew.color = new Color(0.08f, 0.1f, 0.12f, 0.9f);
                Debug.LogWarning("[Setup] Missing GoldBar.png at Art/UI/Hub/GoldBar.png — using placeholder color.");
            }

            GameObject goldObjNew = new GameObject("GoldText");
            goldObjNew.transform.SetParent(rootGo.transform, false);
            RectTransform goldRectNew = goldObjNew.AddComponent<RectTransform>();
            goldRectNew.anchorMin = new Vector2(0f, 0f);
            goldRectNew.anchorMax = new Vector2(1f, 1f);
            goldRectNew.offsetMin = new Vector2(52f, 6f);
            goldRectNew.offsetMax = new Vector2(-18f, -6f);

            UnityEngine.UI.Text goldText = goldObjNew.AddComponent<UnityEngine.UI.Text>();
            goldText.text = "0";
            goldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            goldText.fontSize = 20;
            goldText.fontStyle = FontStyle.Bold;
            goldText.color = new Color(1f, 0.92f, 0.55f, 1f);
            goldText.alignment = TextAnchor.MiddleCenter;
            goldText.horizontalOverflow = HorizontalWrapMode.Overflow;
            goldText.verticalOverflow = VerticalWrapMode.Overflow;

            return goldText;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            Transform direct = parent.Find(name);
            if (direct != null)
            {
                return direct;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject EnsureHubText(Transform parent, string name, string text, Vector2 pos, Vector2 size, bool forceDefaults)
        {
            Transform existing = parent.Find(name);
            bool existed = existing != null;
            GameObject obj = existed
                ? existing.gameObject
                : CreateUIText(name, parent, pos, text);

            if (forceDefaults || !existed)
            {
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = pos;
                rect.sizeDelta = size;

                UnityEngine.UI.Text uiText = obj.GetComponent<UnityEngine.UI.Text>();
                if (uiText != null)
                {
                    uiText.alignment = TextAnchor.MiddleCenter;
                    uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    uiText.verticalOverflow = VerticalWrapMode.Overflow;
                }
            }

            return obj;
        }

        private static GameObject EnsureHubButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, bool forceDefaults)
        {
            Transform existing = parent.Find(name);
            bool existed = existing != null;
            GameObject obj = existed
                ? existing.gameObject
                : CreateButton(name, parent, pos, size, label);

            if (forceDefaults || !existed)
            {
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = pos;
                rect.sizeDelta = size;
            }

            return obj;
        }

        private static void BindHubReferences()
        {
            GameObject hubControllerObj = GameObject.Find("HubController");
            if (hubControllerObj == null)
            {
                Debug.LogWarning("[Setup] HubController object missing.");
                return;
            }

            Game.Hub.HubController hubController = hubControllerObj.GetComponent<Game.Hub.HubController>();
            if (hubController == null)
            {
                hubController = hubControllerObj.AddComponent<Game.Hub.HubController>();
            }

            Game.Hub.HubPlazaClickRouter clickRouter = hubControllerObj.GetComponent<Game.Hub.HubPlazaClickRouter>();
            if (clickRouter == null)
            {
                clickRouter = hubControllerObj.AddComponent<Game.Hub.HubPlazaClickRouter>();
            }

            SerializedObject routerSO = new SerializedObject(clickRouter);
            routerSO.FindProperty("hubController").objectReferenceValue = hubController;
            Camera mainCam = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            routerSO.FindProperty("worldCamera").objectReferenceValue = mainCam;
            routerSO.ApplyModifiedProperties();

            Game.UI.HubMenuUI hubMenu = UnityEngine.Object.FindFirstObjectByType<Game.UI.HubMenuUI>(FindObjectsInactive.Include);
            Game.UI.InventoryPanelUI inventoryPanel = UnityEngine.Object.FindFirstObjectByType<Game.UI.InventoryPanelUI>(FindObjectsInactive.Include);
            Game.UI.MapSelectUI mapSelect = UnityEngine.Object.FindFirstObjectByType<Game.UI.MapSelectUI>(FindObjectsInactive.Include);
            Game.UI.VillagePanelUI villagePanel = UnityEngine.Object.FindFirstObjectByType<Game.UI.VillagePanelUI>(FindObjectsInactive.Include);
            Game.UI.CraftPanelUI craftPanel = UnityEngine.Object.FindFirstObjectByType<Game.UI.CraftPanelUI>(FindObjectsInactive.Include);
            Game.UI.EnhancePanelUI enhancePanel = UnityEngine.Object.FindFirstObjectByType<Game.UI.EnhancePanelUI>(FindObjectsInactive.Include);
            Game.UI.BuildPanelUI buildPanel = UnityEngine.Object.FindFirstObjectByType<Game.UI.BuildPanelUI>(FindObjectsInactive.Include);
            Game.UI.AFKSummaryUI afkSummary = UnityEngine.Object.FindFirstObjectByType<Game.UI.AFKSummaryUI>(FindObjectsInactive.Include);
            Game.Data.MapCatalog mapCatalog = AssetDatabase.LoadAssetAtPath<Game.Data.MapCatalog>(SCRIPTABLE_PATH + "/Map/MapCatalog.asset");
            Game.Data.VillageCatalog villageCatalog = AssetDatabase.LoadAssetAtPath<Game.Data.VillageCatalog>(SCRIPTABLE_PATH + "/Village/VillageCatalog.asset");
            Game.Data.CraftCatalog craftCatalog = AssetDatabase.LoadAssetAtPath<Game.Data.CraftCatalog>(SCRIPTABLE_PATH + "/Crafting/CraftCatalog.asset");
            Game.Data.EnhanceCatalog enhanceCatalog = AssetDatabase.LoadAssetAtPath<Game.Data.EnhanceCatalog>(SCRIPTABLE_PATH + "/Enhancement/EnhanceCatalog.asset");
            Game.Data.ItemDatabase itemDatabase = AssetDatabase.LoadAssetAtPath<Game.Data.ItemDatabase>(SCRIPTABLE_PATH + "/Items/ItemDatabase.asset");

            GameObject villageController = GameObject.Find("VillageController");
            Game.Village.VillageService villageService = villageController != null
                ? villageController.GetComponent<Game.Village.VillageService>()
                : null;
            if (villageService == null && villageController != null)
            {
                villageService = villageController.AddComponent<Game.Village.VillageService>();
            }

            GameObject craftController = GameObject.Find("CraftController");
            if (craftController == null)
            {
                GameObject systems = GameObject.Find("Systems");
                craftController = FindOrCreate("CraftController", systems != null ? systems.transform : null);
            }

            Game.Crafting.CraftService craftService = craftController != null
                ? craftController.GetComponent<Game.Crafting.CraftService>()
                : null;
            if (craftService == null && craftController != null)
            {
                craftService = craftController.AddComponent<Game.Crafting.CraftService>();
            }

            GameObject enhanceController = GameObject.Find("EnhanceController");
            if (enhanceController == null)
            {
                GameObject systems = GameObject.Find("Systems");
                enhanceController = FindOrCreate("EnhanceController", systems != null ? systems.transform : null);
            }

            Game.Inventory.ItemEnhanceService enhanceService = enhanceController != null
                ? enhanceController.GetComponent<Game.Inventory.ItemEnhanceService>()
                : null;
            if (enhanceService == null && enhanceController != null)
            {
                enhanceService = enhanceController.AddComponent<Game.Inventory.ItemEnhanceService>();
            }

            GameObject afkController = GameObject.Find("AFKController");
            Game.AFK.AFKRewardService afkService = afkController != null
                ? afkController.GetComponent<Game.AFK.AFKRewardService>()
                : null;
            if (afkService == null && afkController != null)
            {
                afkService = afkController.AddComponent<Game.AFK.AFKRewardService>();
            }

            Game.UI.BuildingPanelUI libraryPanel = null;
            Game.UI.BuildingPanelUI minePanel = null;
            Game.UI.BuildingPanelUI[] buildingPanels =
                UnityEngine.Object.FindObjectsByType<Game.UI.BuildingPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buildingPanels.Length; i++)
            {
                if (buildingPanels[i] == null)
                {
                    continue;
                }

                if (buildingPanels[i].BuildingType == Game.Data.VillageBuildingType.Library)
                {
                    libraryPanel = buildingPanels[i];
                }
                else if (buildingPanels[i].BuildingType == Game.Data.VillageBuildingType.Mine)
                {
                    minePanel = buildingPanels[i];
                }
            }

            SerializedObject hubSO = new SerializedObject(hubController);
            hubSO.FindProperty("hubMenuUI").objectReferenceValue = hubMenu;
            hubSO.FindProperty("inventoryPanel").objectReferenceValue = inventoryPanel;
            hubSO.FindProperty("mapSelectUI").objectReferenceValue = mapSelect;
            hubSO.FindProperty("villagePanelUI").objectReferenceValue = villagePanel;
            hubSO.FindProperty("libraryPanelUI").objectReferenceValue = libraryPanel;
            hubSO.FindProperty("minePanelUI").objectReferenceValue = minePanel;
            hubSO.FindProperty("craftPanelUI").objectReferenceValue = craftPanel;
            hubSO.FindProperty("buildPanelUI").objectReferenceValue = buildPanel;
            hubSO.FindProperty("afkSummaryUI").objectReferenceValue = afkSummary;
            hubSO.FindProperty("mapCatalog").objectReferenceValue = mapCatalog;
            hubSO.FindProperty("villageCatalog").objectReferenceValue = villageCatalog;
            hubSO.FindProperty("craftCatalog").objectReferenceValue = craftCatalog;
            hubSO.FindProperty("villageService").objectReferenceValue = villageService;
            hubSO.FindProperty("craftService").objectReferenceValue = craftService;
            hubSO.FindProperty("afkRewardService").objectReferenceValue = afkService;
            hubSO.FindProperty("battleSceneName").stringValue = Game.Core.GameScenes.Battle;
            hubSO.ApplyModifiedProperties();

            if (mapSelect != null)
            {
                SerializedObject mapSO = new SerializedObject(mapSelect);
                mapSO.FindProperty("mapCatalog").objectReferenceValue = mapCatalog;
                mapSO.ApplyModifiedProperties();
            }

            if (villageService != null)
            {
                SerializedObject villageSO = new SerializedObject(villageService);
                villageSO.FindProperty("villageCatalog").objectReferenceValue = villageCatalog;
                villageSO.ApplyModifiedProperties();
            }

            if (craftService != null)
            {
                SerializedObject craftSO = new SerializedObject(craftService);
                craftSO.FindProperty("craftCatalog").objectReferenceValue = craftCatalog;
                craftSO.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
                GameObject inv = GameObject.Find("InventoryController");
                if (inv != null)
                {
                    craftSO.FindProperty("inventoryService").objectReferenceValue = inv.GetComponent<Game.Inventory.InventoryService>();
                }

                GameObject eq = GameObject.Find("EquipmentController");
                if (eq != null)
                {
                    craftSO.FindProperty("equipmentService").objectReferenceValue = eq.GetComponent<Game.Inventory.EquipmentService>();
                }

                craftSO.FindProperty("villageService").objectReferenceValue = villageService;
                GameObject rewardObj = GameObject.Find("RewardController");
                if (rewardObj != null)
                {
                    craftSO.FindProperty("rewardService").objectReferenceValue = rewardObj.GetComponent<Game.Rewards.RewardService>();
                }

                craftSO.ApplyModifiedProperties();
            }

            if (villagePanel != null)
            {
                SerializedObject villagePanelSO = new SerializedObject(villagePanel);
                SerializedProperty craftProp = villagePanelSO.FindProperty("craftPanelUI");
                if (craftProp != null)
                {
                    craftProp.objectReferenceValue = craftPanel;
                }

                villagePanelSO.ApplyModifiedProperties();
            }

            if (craftPanel != null)
            {
                SerializedObject craftPanelSO = new SerializedObject(craftPanel);
                craftPanelSO.FindProperty("craftService").objectReferenceValue = craftService;
                craftPanelSO.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
                GameObject inv = GameObject.Find("InventoryController");
                if (inv != null)
                {
                    craftPanelSO.FindProperty("inventoryService").objectReferenceValue = inv.GetComponent<Game.Inventory.InventoryService>();
                }

                craftPanelSO.ApplyModifiedProperties();
            }

            if (enhanceService != null)
            {
                SerializedObject enhanceSO = new SerializedObject(enhanceService);
                enhanceSO.FindProperty("enhanceCatalog").objectReferenceValue = enhanceCatalog;
                enhanceSO.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
                GameObject inv = GameObject.Find("InventoryController");
                if (inv != null)
                {
                    enhanceSO.FindProperty("inventoryService").objectReferenceValue = inv.GetComponent<Game.Inventory.InventoryService>();
                }

                GameObject eq = GameObject.Find("EquipmentController");
                if (eq != null)
                {
                    enhanceSO.FindProperty("equipmentService").objectReferenceValue = eq.GetComponent<Game.Inventory.EquipmentService>();
                    SerializedObject eqSO = new SerializedObject(eq.GetComponent<Game.Inventory.EquipmentService>());
                    eqSO.FindProperty("enhanceCatalog").objectReferenceValue = enhanceCatalog;
                    eqSO.ApplyModifiedProperties();
                }

                enhanceSO.FindProperty("craftService").objectReferenceValue = craftService;
                GameObject rewardObj = GameObject.Find("RewardController");
                if (rewardObj != null)
                {
                    enhanceSO.FindProperty("rewardService").objectReferenceValue = rewardObj.GetComponent<Game.Rewards.RewardService>();
                }

                enhanceSO.ApplyModifiedProperties();
            }

            if (enhancePanel != null)
            {
                SerializedObject enhancePanelSO = new SerializedObject(enhancePanel);
                enhancePanelSO.FindProperty("enhanceService").objectReferenceValue = enhanceService;
                GameObject inv = GameObject.Find("InventoryController");
                if (inv != null)
                {
                    enhancePanelSO.FindProperty("inventoryService").objectReferenceValue = inv.GetComponent<Game.Inventory.InventoryService>();
                }

                enhancePanelSO.ApplyModifiedProperties();
            }

            Game.UI.InventoryPanelUI inventoryPanelUI = UnityEngine.Object.FindFirstObjectByType<Game.UI.InventoryPanelUI>(FindObjectsInactive.Include);
            if (inventoryPanelUI != null)
            {
                SerializedObject invPanelSO = new SerializedObject(inventoryPanelUI);
                if (invPanelSO.FindProperty("enhancePanelUI") != null)
                {
                    invPanelSO.FindProperty("enhancePanelUI").objectReferenceValue = enhancePanel;
                }

                if (invPanelSO.FindProperty("enhanceCatalog") != null)
                {
                    invPanelSO.FindProperty("enhanceCatalog").objectReferenceValue = enhanceCatalog;
                }

                invPanelSO.ApplyModifiedProperties();
            }

            if (afkService != null)
            {
                SerializedObject afkSO = new SerializedObject(afkService);
                afkSO.FindProperty("villageService").objectReferenceValue = villageService;
                afkSO.FindProperty("craftService").objectReferenceValue = craftService;
                afkSO.FindProperty("craftCatalog").objectReferenceValue = craftCatalog;
                GameObject chestObj = GameObject.Find("ChestController");
                if (chestObj != null)
                {
                    afkSO.FindProperty("chestService").objectReferenceValue = chestObj.GetComponent<Game.Inventory.ChestService>();
                }

                afkSO.ApplyModifiedProperties();
            }

            // Wire hub reward UI to session RewardService
            GameObject rewardController = GameObject.Find("RewardController");
            Game.Rewards.RewardService rewardService = rewardController != null
                ? rewardController.GetComponent<Game.Rewards.RewardService>()
                : null;
            if (rewardService == null && rewardController != null)
            {
                rewardService = rewardController.AddComponent<Game.Rewards.RewardService>();
            }

            if (rewardService != null && hubMenu != null)
            {
                SerializedObject rewardSO = new SerializedObject(rewardService);
                SerializedObject menuSO = new SerializedObject(hubMenu);
                SerializedProperty menuGold = menuSO.FindProperty("goldText");
                if (menuGold != null)
                {
                    rewardSO.FindProperty("goldText").objectReferenceValue = menuGold.objectReferenceValue;
                }

                // HubMenuUI no longer exposes level/save labels — leave RewardService.levelText as-is.
                rewardSO.ApplyModifiedProperties();
            }

            // Hero presenter on Units
            GameObject units = GameObject.Find("Units");
            GameObject standPoint = GameObject.Find("HeroStandPoint");
            if (units != null)
            {
                Game.Hub.HubHeroPresenter presenter = units.GetComponent<Game.Hub.HubHeroPresenter>();
                if (presenter == null)
                {
                    presenter = units.AddComponent<Game.Hub.HubHeroPresenter>();
                }

                GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH + "/Hero/Hero.prefab");
                SerializedObject presenterSO = new SerializedObject(presenter);
                presenterSO.FindProperty("heroStandPoint").objectReferenceValue = standPoint != null ? standPoint.transform : null;
                presenterSO.FindProperty("heroPrefab").objectReferenceValue = heroPrefab;
                presenterSO.FindProperty("unitsRoot").objectReferenceValue = units.transform;
                presenterSO.FindProperty("faceLeft").boolValue = true; // look toward plaza / menu
                presenterSO.FindProperty("displayScale").vector3Value = Vector3.one;
                presenterSO.ApplyModifiedProperties();
            }

            Debug.Log("[Setup] Hub references bound.");
        }

        private static void BindBattleResultPanelHubScene()
        {
            Game.UI.BattleResultPanel panel = UnityEngine.Object.FindFirstObjectByType<Game.UI.BattleResultPanel>(FindObjectsInactive.Include);
            if (panel == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(panel);
            SerializedProperty hubProp = so.FindProperty("hubSceneName");
            if (hubProp != null)
            {
                hubProp.stringValue = Game.Core.GameScenes.Hub;
                so.ApplyModifiedProperties();
                Debug.Log("[Setup] BattleResultPanel hub scene wired.");
            }
        }

        private static void UpdateBuildSettingsForHubAndBattle()
        {
            string hubPath = SCENE_PATH + "/VillageHub.unity";
            string battlePath = SCENE_PATH + "/BattleDemo.unity";

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            if (File.Exists(hubPath))
            {
                scenes.Add(new EditorBuildSettingsScene(hubPath, true));
            }

            if (File.Exists(battlePath))
            {
                scenes.Add(new EditorBuildSettingsScene(battlePath, true));
            }

            // Keep any other enabled scenes that are not ours
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == hubPath || existing.path == battlePath)
                {
                    continue;
                }

                if (existing.enabled)
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Setup] Build Settings updated: VillageHub (0), BattleDemo (1).");
        }

        private static void CreatePointsHierarchy(Transform root)
        {
            GameObject points = FindOrCreate("Points", root);

            CreatePoint("HeroStartPoint", points.transform, new Vector3(-6f, -0.8f, 0f));
            CreatePoint("HeroCombatPoint", points.transform, new Vector3(-1.1f, -0.8f, 0f));
            CreatePoint("EnemySpawnPoint", points.transform, new Vector3(6f, -0.8f, 0f));
            CreatePoint("EnemyCombatPoint", points.transform, new Vector3(1.1f, -0.8f, 0f));
        }

        private static void CreatePoint(string name, Transform parent, Vector3 position)
        {
            GameObject point = FindOrCreate(name, parent);
            point.transform.localPosition = position;
        }

        private static void CreateEnvironmentHierarchy(Transform root)
        {
            GameObject env = FindOrCreate("Environment", root);
            GameObject parallaxRoot = FindOrCreate("ParallaxRoot", env.transform);

            CreateParallaxLayer("FarLayer", parallaxRoot.transform, ART_PATH + "/Backgrounds/Far/Far.png", -5f);
            CreateParallaxLayer("MidLayer", parallaxRoot.transform, ART_PATH + "/Backgrounds/Mid/Mid.png", -3f);
            CreateParallaxLayer("ForegroundLayer", parallaxRoot.transform, ART_PATH + "/Backgrounds/Foreground/Foreground.png", -1f);
        }

        private static void CreateParallaxLayer(string layerName, Transform parent, string spritePath, float zPos)
        {
            GameObject layer = FindOrCreate(layerName, parent);
            layer.transform.localPosition = new Vector3(0, 0, zPos);

            if (!File.Exists(spritePath))
            {
                Debug.LogWarning($"[Setup] Background sprite not found: {spritePath}. Layer created but empty.");
                return;
            }

            Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (backgroundSprite == null)
            {
                Debug.LogWarning($"[Setup] Could not load sprite: {spritePath}");
                return;
            }

            float scale = 1f;
            Camera cam = Camera.main;
            if (cam != null && cam.orthographic && backgroundSprite.bounds.size.y > 0f)
            {
                float worldScreenHeight = cam.orthographicSize * 2f;
                scale = worldScreenHeight / backgroundSprite.bounds.size.y;
            }

            float tileWidth = backgroundSprite.bounds.size.x * scale;

            // Remove old single Background child naming conflicts by ensuring two loop tiles
            CreateOrUpdateLoopTile("Tile_0", layer.transform, backgroundSprite, scale, 0f, layerName);
            CreateOrUpdateLoopTile("Tile_1", layer.transform, backgroundSprite, scale, tileWidth, layerName);

            // Remove legacy single Background object if present
            Transform legacy = layer.transform.Find("Background");
            if (legacy != null)
            {
                DestroyImmediate(legacy.gameObject);
            }

            Debug.Log($"[Setup] Created looping parallax layer: {layerName} (tileWidth={tileWidth:F2})");
        }

        private static void CreateOrUpdateLoopTile(string tileName, Transform parent, Sprite sprite, float scale, float localX, string layerName)
        {
            GameObject tile = FindOrCreate(tileName, parent);
            tile.transform.localPosition = new Vector3(localX, 0f, 0f);
            tile.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = tile.AddComponent<SpriteRenderer>();
            }

            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = GetSortingOrderForLayer(layerName);
        }

        private static int GetSortingOrderForLayer(string layerName)
        {
            if (layerName.Contains("Far")) return -100;
            if (layerName.Contains("Mid")) return -50;
            if (layerName.Contains("Foreground")) return 50;
            return 0;
        }

        private static void CreateUnitsHierarchy(Transform root)
        {
            GameObject units = FindOrCreate("Units", root);

            GameObject hero = GameObject.Find("Hero");
            if (hero == null)
            {
                GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH + "/Hero/Hero.prefab");
                if (heroPrefab != null)
                {
                    hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
                    hero.transform.SetParent(units.transform);
                    hero.transform.localPosition = new Vector3(-6f, -0.8f, 0f);
                }
            }

            FindOrCreate("RuntimeEnemies", units.transform);
            FindOrCreate("RuntimeEffects", units.transform);
        }

        private static void CreateUIHierarchy(Transform root)
        {
            GameObject ui = FindOrCreate("UI", root);

            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                canvas = new GameObject("ScreenCanvas");
                canvas.transform.SetParent(ui.transform);

                Canvas canvasComp = canvas.AddComponent<Canvas>();
                canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                GameObject topPanel = new GameObject("TopPanel");
                topPanel.transform.SetParent(canvas.transform);
                RectTransform topRect = topPanel.AddComponent<RectTransform>();
                topRect.anchorMin = new Vector2(0, 1);
                topRect.anchorMax = new Vector2(1, 1);
                topRect.pivot = new Vector2(0.5f, 1);
                topRect.sizeDelta = new Vector2(0, 100);
                topRect.anchoredPosition = Vector2.zero;

                CreateUIText("StageText", topPanel.transform, new Vector2(-300, -20), "Stage: 1");
                CreateUIText("WaveText", topPanel.transform, new Vector2(-300, -50), "Wave: 1/3");
                CreateUIText("LevelText", topPanel.transform, new Vector2(0, -20), "Lv.1");
                CreateUIText("GoldText", topPanel.transform, new Vector2(300, -20), "Gold: 0");
                CreateUIText("ExperienceText", topPanel.transform, new Vector2(300, -50), "EXP: 0");

                GameObject resultPanel = new GameObject("BattleResultPanel");
                resultPanel.transform.SetParent(canvas.transform);
                RectTransform resultRect = resultPanel.AddComponent<RectTransform>();
                resultRect.anchorMin = Vector2.zero;
                resultRect.anchorMax = Vector2.one;
                resultRect.sizeDelta = Vector2.zero;
                resultRect.anchoredPosition = Vector2.zero;

                UnityEngine.UI.Image resultBg = resultPanel.AddComponent<UnityEngine.UI.Image>();
                resultBg.color = new Color(0, 0, 0, 0.9f);

                // Add BattleResultPanel component
                Game.UI.BattleResultPanel panelScript = resultPanel.AddComponent<Game.UI.BattleResultPanel>();

                // Content container
                GameObject content = new GameObject("Content");
                content.transform.SetParent(resultPanel.transform);
                RectTransform contentRect = content.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0.5f, 0.5f);
                contentRect.anchorMax = new Vector2(0.5f, 0.5f);
                contentRect.sizeDelta = new Vector2(400, 300);
                contentRect.anchoredPosition = Vector2.zero;

                // Title
                GameObject titleObj = CreateUIText("TitleText", content.transform, new Vector2(0, 100), "VICTORY!");
                UnityEngine.UI.Text titleText = titleObj.GetComponent<UnityEngine.UI.Text>();
                titleText.fontSize = 36;
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleCenter;

                // Stage Info
                GameObject stageInfoObj = CreateUIText("StageInfoText", content.transform, new Vector2(0, 50), "Stage Completed");
                UnityEngine.UI.Text stageInfoText = stageInfoObj.GetComponent<UnityEngine.UI.Text>();
                stageInfoText.fontSize = 20;

                // Gold Reward
                GameObject goldObj = CreateUIText("GoldRewardText", content.transform, new Vector2(0, 0), "+50 Gold");
                UnityEngine.UI.Text goldText = goldObj.GetComponent<UnityEngine.UI.Text>();
                goldText.fontSize = 24;
                goldText.color = new Color(1f, 0.84f, 0f);

                // EXP Reward
                GameObject expObj = CreateUIText("ExpRewardText", content.transform, new Vector2(0, -35), "+100 EXP");
                UnityEngine.UI.Text expText = expObj.GetComponent<UnityEngine.UI.Text>();
                expText.fontSize = 24;
                expText.color = new Color(0.5f, 0.8f, 1f);

                // Restart Button (stays at content level, always visible)
                GameObject restartBtn = CreateButton("RestartButton", content.transform, new Vector2(-80, -100), new Vector2(140, 40), "Restart");
                
                // Victory/Defeat content groups
                GameObject victoryContent = new GameObject("VictoryContent");
                victoryContent.transform.SetParent(content.transform);
                RectTransform victoryRect = victoryContent.AddComponent<RectTransform>();
                victoryRect.anchorMin = Vector2.zero;
                victoryRect.anchorMax = Vector2.one;
                victoryRect.sizeDelta = Vector2.zero;
                victoryRect.anchoredPosition = Vector2.zero;
                
                goldObj.transform.SetParent(victoryContent.transform);
                expObj.transform.SetParent(victoryContent.transform);
                
                // Continue Button (only in victory)
                GameObject continueBtn = CreateButton("ContinueButton", victoryContent.transform, new Vector2(80, -100), new Vector2(140, 40), "Continue");

                GameObject defeatContent = new GameObject("DefeatContent");
                defeatContent.transform.SetParent(content.transform);
                RectTransform defeatRect = defeatContent.AddComponent<RectTransform>();
                defeatRect.anchorMin = Vector2.zero;
                defeatRect.anchorMax = Vector2.one;
                defeatRect.sizeDelta = Vector2.zero;
                defeatRect.anchoredPosition = Vector2.zero;
                
                // Defeat message
                GameObject defeatMsgObj = CreateUIText("DefeatMessage", defeatContent.transform, new Vector2(0, -20), "Try again!");
                UnityEngine.UI.Text defeatMsg = defeatMsgObj.GetComponent<UnityEngine.UI.Text>();
                defeatMsg.fontSize = 20;
                defeatMsg.color = new Color(0.8f, 0.8f, 0.8f);
                
                defeatContent.SetActive(false);

                // Bind to panel script
                SerializedObject panelSO = new SerializedObject(panelScript);
                panelSO.FindProperty("titleText").objectReferenceValue = titleText;
                panelSO.FindProperty("goldRewardText").objectReferenceValue = goldText;
                panelSO.FindProperty("expRewardText").objectReferenceValue = expText;
                panelSO.FindProperty("stageInfoText").objectReferenceValue = stageInfoText;
                panelSO.FindProperty("restartButton").objectReferenceValue = restartBtn.GetComponent<UnityEngine.UI.Button>();
                panelSO.FindProperty("continueButton").objectReferenceValue = continueBtn.GetComponent<UnityEngine.UI.Button>();
                panelSO.FindProperty("victoryContent").objectReferenceValue = victoryContent;
                panelSO.FindProperty("defeatContent").objectReferenceValue = defeatContent;
                panelSO.ApplyModifiedProperties();

                resultPanel.SetActive(false);
            }

            FindOrCreate("WorldCanvasRoot", ui.transform);
            EnsureEventSystem();
            EnsureProgressionUI();
            FixBattleHudLayout();
        }

        private static void FixBattleHudLayout()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                Debug.LogWarning("[Setup] ScreenCanvas missing; cannot fix battle HUD.");
                return;
            }

            UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            GameObject topPanel = GameObject.Find("TopPanel");
            if (topPanel == null)
            {
                Debug.LogWarning("[Setup] TopPanel missing; cannot fix battle HUD.");
                return;
            }

            RectTransform topRect = topPanel.GetComponent<RectTransform>();
            if (topRect == null)
            {
                topRect = topPanel.AddComponent<RectTransform>();
            }

            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.sizeDelta = new Vector2(0f, 110f);
            topRect.anchoredPosition = Vector2.zero;

            // Soft bar behind HUD so text stays readable
            UnityEngine.UI.Image topBg = topPanel.GetComponent<UnityEngine.UI.Image>();
            if (topBg == null)
            {
                topBg = topPanel.AddComponent<UnityEngine.UI.Image>();
            }

            topBg.color = new Color(0.02f, 0.06f, 0.08f, 0.55f);
            topBg.raycastTarget = false;

            // Left column (offset past SideNav rail ~110px)
            LayoutHudText(topPanel.transform, "StageText", "Stage: 1",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -10f), new Vector2(280f, 28f),
                TextAnchor.MiddleLeft, 18, Color.white);

            LayoutHudText(topPanel.transform, "WaveText", "Wave: 1/3",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -42f), new Vector2(280f, 26f),
                TextAnchor.MiddleLeft, 16, new Color(0.85f, 0.9f, 0.88f));

            // Center: level only (Bag / Village text buttons removed — use left SideNav).
            LayoutHudText(topPanel.transform, "LevelText", "Lv.1",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(120f, 28f),
                TextAnchor.MiddleCenter, 20, Color.white);

            DestroyHudChild(topPanel.transform, "InventoryButton");
            DestroyHudChild(topPanel.transform, "VillageReturnButton");

            EnsureBattleSideNav(canvas.transform);

            // Right column
            LayoutHudText(topPanel.transform, "GoldText", "Gold: 0",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -10f), new Vector2(240f, 28f),
                TextAnchor.MiddleRight, 18, new Color(1f, 0.85f, 0.35f));

            LayoutHudText(topPanel.transform, "ExperienceText", "EXP: 0/100",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -42f), new Vector2(240f, 26f),
                TextAnchor.MiddleRight, 16, new Color(0.7f, 0.85f, 1f));

            Debug.Log("[Setup] Battle HUD layout fixed (SideNav + no Bag/Village text buttons).");
        }

        private static void DestroyHudChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return;
            }

            Transform child = parent.Find(childName);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void EnsureBattleSideNav(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return;
            }

            Transform existingMenu = canvasRoot.Find("BattleMenu");
            GameObject menuRoot;
            if (existingMenu != null)
            {
                menuRoot = existingMenu.gameObject;
            }
            else
            {
                menuRoot = new GameObject("BattleMenu");
                menuRoot.transform.SetParent(canvasRoot, false);
                RectTransform menuRect = menuRoot.AddComponent<RectTransform>();
                menuRect.anchorMin = Vector2.zero;
                menuRect.anchorMax = Vector2.one;
                menuRect.offsetMin = Vector2.zero;
                menuRect.offsetMax = Vector2.zero;
            }

            Game.UI.BattleMenuUI menuUI = menuRoot.GetComponent<Game.UI.BattleMenuUI>();
            if (menuUI == null)
            {
                menuUI = menuRoot.AddComponent<Game.UI.BattleMenuUI>();
            }

            // Always rebuild rail icons so battle matches current Village SideNav art.
            Game.UI.HubSideNavUI sideNav = EnsureHubSideNav(menuRoot.transform, forceDefaults: true);
            Game.UI.InventoryPanelUI inventoryPanel =
                UnityEngine.Object.FindFirstObjectByType<Game.UI.InventoryPanelUI>(FindObjectsInactive.Include);

            SerializedObject menuSO = new SerializedObject(menuUI);
            menuSO.FindProperty("sideNav").objectReferenceValue = sideNav;
            menuSO.FindProperty("inventoryPanel").objectReferenceValue = inventoryPanel;
            menuSO.FindProperty("hubSceneName").stringValue = Game.Core.GameScenes.Hub;

            if (sideNav != null)
            {
                SerializedObject navSO = new SerializedObject(sideNav);
                SerializedProperty itemsProp = navSO.FindProperty("items");
                if (itemsProp != null && itemsProp.isArray)
                {
                    for (int i = 0; i < itemsProp.arraySize; i++)
                    {
                        SerializedProperty item = itemsProp.GetArrayElementAtIndex(i);
                        int tab = item.FindPropertyRelative("tab").enumValueIndex;
                        UnityEngine.UI.Button btn = item.FindPropertyRelative("button").objectReferenceValue as UnityEngine.UI.Button;
                        if (btn == null)
                        {
                            continue;
                        }

                        if (tab == 1) menuSO.FindProperty("adventureButton").objectReferenceValue = btn;
                        else if (tab == 2) menuSO.FindProperty("villageButton").objectReferenceValue = btn;
                        else if (tab == 3) menuSO.FindProperty("inventoryButton").objectReferenceValue = btn;
                        else if (tab == 4) menuSO.FindProperty("quitButton").objectReferenceValue = btn;
                    }
                }
            }

            menuSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(menuUI);
            Debug.Log("[Setup] Battle SideNav ensured.");
        }

        private static void LayoutHudText(
            Transform parent,
            string name,
            string fallbackText,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size,
            TextAnchor alignment,
            int fontSize,
            Color color)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null
                ? existing.gameObject
                : CreateUIText(name, parent, anchoredPos, fallbackText);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin; // top-left / top-center / top-right matching column
            if (Mathf.Approximately(anchorMin.x, 0.5f))
            {
                rect.pivot = new Vector2(0.5f, 1f);
            }
            else if (Mathf.Approximately(anchorMin.x, 1f))
            {
                rect.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rect.pivot = new Vector2(0f, 1f);
            }

            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            UnityEngine.UI.Text text = obj.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
            {
                text.alignment = alignment;
                text.fontSize = fontSize;
                text.color = color;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        private static GameObject LayoutHudButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null
                ? existing.gameObject
                : CreateButton(name, parent, anchoredPos, size, label);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            return obj;
        }

        private static void EnsureProgressionUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                Debug.LogWarning("[Setup] ScreenCanvas not found. Cannot create progression UI.");
                return;
            }

            // Level text on top panel (position finalized by FixBattleHudLayout)
            GameObject topPanel = GameObject.Find("TopPanel");
            if (topPanel != null && topPanel.transform.Find("LevelText") == null)
            {
                CreateUIText("LevelText", topPanel.transform, new Vector2(0, -8), "Lv.1");
            }

            // Level up popup
            GameObject levelUpPanel = GameObject.Find("LevelUpPanel");
            if (levelUpPanel == null)
            {
                // Search inactive too
                Transform existing = canvas.transform.Find("LevelUpPanel");
                if (existing != null)
                {
                    levelUpPanel = existing.gameObject;
                }
            }

            if (levelUpPanel == null)
            {
                levelUpPanel = new GameObject("LevelUpPanel");
                levelUpPanel.transform.SetParent(canvas.transform, false);

                RectTransform panelRect = levelUpPanel.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.75f);
                panelRect.anchorMax = new Vector2(0.5f, 0.75f);
                panelRect.sizeDelta = new Vector2(360, 90);
                panelRect.anchoredPosition = Vector2.zero;

                UnityEngine.UI.Image bg = levelUpPanel.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0.05f, 0.12f, 0.14f, 0.92f);

                CanvasGroup canvasGroup = levelUpPanel.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;

                Game.UI.LevelUpPanel panelScript = levelUpPanel.AddComponent<Game.UI.LevelUpPanel>();

                GameObject titleObj = CreateUIText("LevelUpTitle", levelUpPanel.transform, new Vector2(0, 18), "LEVEL UP!  Lv.2");
                UnityEngine.UI.Text titleText = titleObj.GetComponent<UnityEngine.UI.Text>();
                titleText.fontSize = 26;
                titleText.fontStyle = FontStyle.Bold;
                titleText.color = new Color(0.35f, 0.95f, 0.75f);

                GameObject detailObj = CreateUIText("LevelUpDetail", levelUpPanel.transform, new Vector2(0, -18), "ATK 24   HP 120");
                UnityEngine.UI.Text detailText = detailObj.GetComponent<UnityEngine.UI.Text>();
                detailText.fontSize = 18;
                detailText.color = new Color(0.9f, 0.9f, 0.85f);

                SerializedObject so = new SerializedObject(panelScript);
                so.FindProperty("titleText").objectReferenceValue = titleText;
                so.FindProperty("detailText").objectReferenceValue = detailText;
                so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
                so.ApplyModifiedProperties();

                levelUpPanel.SetActive(false);
                Debug.Log("[Setup] Created LevelUpPanel.");
            }

            // Ensure Hero has HeroProgression (scene + prefab)
            GameObject hero = GameObject.Find("Hero");
            if (hero != null && hero.GetComponent<Game.Units.HeroProgression>() == null)
            {
                hero.AddComponent<Game.Units.HeroProgression>();
                Debug.Log("[Setup] Added HeroProgression to Hero in scene.");
            }

            string heroPrefabPath = PREFAB_PATH + "/Hero/Hero.prefab";
            GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(heroPrefabPath);
            if (heroPrefab != null && heroPrefab.GetComponent<Game.Units.HeroProgression>() == null)
            {
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(heroPrefabPath);
                if (prefabRoot.GetComponent<Game.Units.HeroProgression>() == null)
                {
                    prefabRoot.AddComponent<Game.Units.HeroProgression>();
                }
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, heroPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.Log("[Setup] Added HeroProgression to Hero prefab.");
            }

            // Bind level text to RewardService
            GameObject rewardController = GameObject.Find("RewardController");
            if (rewardController != null)
            {
                Game.Rewards.RewardService rewardService = rewardController.GetComponent<Game.Rewards.RewardService>();
                if (rewardService != null)
                {
                    SerializedObject rewardSO = new SerializedObject(rewardService);
                    GameObject levelText = GameObject.Find("LevelText");
                    if (levelText != null)
                    {
                        rewardSO.FindProperty("levelText").objectReferenceValue = levelText.GetComponent<UnityEngine.UI.Text>();
                    }

                    GameObject goldText = GameObject.Find("GoldText");
                    if (goldText != null)
                    {
                        rewardSO.FindProperty("goldText").objectReferenceValue = goldText.GetComponent<UnityEngine.UI.Text>();
                    }

                    GameObject experienceText = GameObject.Find("ExperienceText");
                    if (experienceText != null)
                    {
                        rewardSO.FindProperty("experienceText").objectReferenceValue = experienceText.GetComponent<UnityEngine.UI.Text>();
                    }

                    rewardSO.ApplyModifiedProperties();
                }
            }
        }

        private static void EnsureLootSystems()
        {
            CreateSampleLootData();

            GameObject systems = GameObject.Find("Systems");
            if (systems == null)
            {
                Debug.LogWarning("[Setup] Systems root not found for loot setup.");
                return;
            }

            GameObject inventoryObj = FindOrCreate("InventoryController", systems.transform);
            if (inventoryObj.GetComponent<Game.Inventory.InventoryService>() == null)
            {
                inventoryObj.AddComponent<Game.Inventory.InventoryService>();
            }

            GameObject equipmentObj = FindOrCreate("EquipmentController", systems.transform);
            if (equipmentObj.GetComponent<Game.Inventory.EquipmentService>() == null)
            {
                equipmentObj.AddComponent<Game.Inventory.EquipmentService>();
            }

            GameObject chestObj = FindOrCreate("ChestController", systems.transform);
            Game.Inventory.ChestService chestService = chestObj.GetComponent<Game.Inventory.ChestService>();
            if (chestService == null)
            {
                chestService = chestObj.AddComponent<Game.Inventory.ChestService>();
            }

            EnsureLootPopupUI();
            EnsureInventoryUI();

            Game.Data.ChestData chestData = AssetDatabase.LoadAssetAtPath<Game.Data.ChestData>(SCRIPTABLE_PATH + "/Chests/Chest_Wooden.asset");
            Game.Data.ItemDatabase itemDatabase = AssetDatabase.LoadAssetAtPath<Game.Data.ItemDatabase>(SCRIPTABLE_PATH + "/Items/ItemDatabase.asset");
            Game.Data.MapCatalog mapCatalog = AssetDatabase.LoadAssetAtPath<Game.Data.MapCatalog>(SCRIPTABLE_PATH + "/Map/MapCatalog.asset");
            Game.UI.LootPopupUI lootPopup = UnityEngine.Object.FindFirstObjectByType<Game.UI.LootPopupUI>(FindObjectsInactive.Include);

            SerializedObject chestSO = new SerializedObject(chestService);
            chestSO.FindProperty("defaultChest").objectReferenceValue = chestData;
            chestSO.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
            chestSO.FindProperty("mapCatalog").objectReferenceValue = mapCatalog;
            chestSO.FindProperty("inventoryService").objectReferenceValue = inventoryObj.GetComponent<Game.Inventory.InventoryService>();
            chestSO.FindProperty("equipmentService").objectReferenceValue = equipmentObj.GetComponent<Game.Inventory.EquipmentService>();
            chestSO.FindProperty("lootPopup").objectReferenceValue = lootPopup;
            chestSO.ApplyModifiedProperties();

            EnsureBuildSystems();
            EnsureBuildPanelUI();
            WireInventoryBuildButton();
            BindBuildPanelServices();
            EnsureRosterSystems();
            EnsureWeeklyDungeonSystems();
            EnsureRosterPanelUI();
            WireInventoryClassButton();
            BindRosterPanelServices();
            EnsureWeeklyDungeonPanelUI();
            WireMapSelectWeeklyButton();
            BindWeeklyDungeonPanelServices();

            GameObject rewardController = GameObject.Find("RewardController");
            if (rewardController != null)
            {
                Game.Rewards.RewardService rewardService = rewardController.GetComponent<Game.Rewards.RewardService>();
                if (rewardService != null)
                {
                    SerializedObject rewardSO = new SerializedObject(rewardService);
                    rewardSO.FindProperty("chestService").objectReferenceValue = chestService;
                    rewardSO.ApplyModifiedProperties();
                }
            }

            Debug.Log("[Setup] Loot systems ensured and wired.");
        }

        private static void EnsureLootPopupUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("LootPopup");
            if (existing != null)
            {
                return;
            }

            GameObject popup = new GameObject("LootPopup");
            popup.transform.SetParent(canvas.transform, false);

            RectTransform rect = popup.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(380, 220);
            rect.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image bg = popup.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.04f, 0.1f, 0.12f, 0.95f);

            Game.UI.LootPopupUI popupScript = popup.AddComponent<Game.UI.LootPopupUI>();

            GameObject titleObj = CreateUIText("LootTitle", popup.transform, new Vector2(0, 70), "CHEST LOOT!");
            UnityEngine.UI.Text titleText = titleObj.GetComponent<UnityEngine.UI.Text>();
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.85f, 0.35f);

            GameObject nameObj = CreateUIText("LootItemName", popup.transform, new Vector2(0, 25), "Item Name");
            UnityEngine.UI.Text nameText = nameObj.GetComponent<UnityEngine.UI.Text>();
            nameText.fontSize = 20;

            GameObject bonusObj = CreateUIText("LootBonus", popup.transform, new Vector2(0, -15), "ATK +0   HP +0");
            UnityEngine.UI.Text bonusText = bonusObj.GetComponent<UnityEngine.UI.Text>();
            bonusText.fontSize = 16;
            bonusObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);

            GameObject equipBtn = CreateButton("EquipButton", popup.transform, new Vector2(-80, -70), new Vector2(140, 40), "Equip");
            GameObject closeBtn = CreateButton("CloseButton", popup.transform, new Vector2(80, -70), new Vector2(140, 40), "Keep");

            SerializedObject so = new SerializedObject(popupScript);
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("itemNameText").objectReferenceValue = nameText;
            so.FindProperty("bonusText").objectReferenceValue = bonusText;
            so.FindProperty("equipButton").objectReferenceValue = equipBtn.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<UnityEngine.UI.Button>();
            so.ApplyModifiedProperties();

            popup.SetActive(false);
            Debug.Log("[Setup] Created LootPopup UI.");
        }

        private static void EnsureInventoryUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                Debug.LogWarning("[Setup] ScreenCanvas not found for Inventory UI.");
                return;
            }

            Game.Data.ItemDatabase itemDatabase = AssetDatabase.LoadAssetAtPath<Game.Data.ItemDatabase>(SCRIPTABLE_PATH + "/Items/ItemDatabase.asset");

            // Battle uses left SideNav (same as Village); do not recreate Bag text button.
            GameObject topPanel = GameObject.Find("TopPanel");
            if (topPanel != null)
            {
                DestroyHudChild(topPanel.transform, "InventoryButton");
                DestroyHudChild(topPanel.transform, "VillageReturnButton");
            }

            if (UnityEngine.Object.FindFirstObjectByType<Game.UI.HubMenuUI>(FindObjectsInactive.Include) == null
                && canvas != null)
            {
                EnsureBattleSideNav(canvas.transform);
            }

            Transform existingPanel = canvas.transform.Find("InventoryPanel");
            if (existingPanel != null && s_preserveHubLayout)
            {
                Debug.Log("[Setup] InventoryPanel kept (layout preserved).");
                Game.UI.InventoryPanelUI kept = existingPanel.GetComponent<Game.UI.InventoryPanelUI>();
                if (kept != null)
                {
                    BindInventoryPanelServices(kept, itemDatabase);
                    EnsureInventoryVisualLayout(existingPanel.gameObject, kept);
                }

                BindSampleItemIcons();
                EnsurePanelSlider(existingPanel.gameObject);
                return;
            }

            if (existingPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(existingPanel.gameObject);
            }

            GameObject panel = new GameObject("InventoryPanel");
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(900f, 600f);
            cardRect.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
            Sprite frameSprite = LoadInventorySprite("Inventory_Frame.png");
            cardBg.sprite = frameSprite;
            cardBg.preserveAspect = true;
            cardBg.color = Color.white;
            if (frameSprite == null)
            {
                cardBg.color = new Color(0.04f, 0.09f, 0.11f, 0.98f);
            }

            GameObject titleObj = CreateUIText("InventoryTitle", card.transform, new Vector2(0f, 248f), "INVENTORY");
            UnityEngine.UI.Text titleText = titleObj.GetComponent<UnityEngine.UI.Text>();
            titleText.fontSize = 26;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.45f, 0.95f, 0.9f);
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 34f);

            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(card.transform, false);
            RectTransform divRect = divider.AddComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.42f, 0.12f);
            divRect.anchorMax = new Vector2(0.42f, 0.86f);
            divRect.offsetMin = Vector2.zero;
            divRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image divImg = divider.AddComponent<UnityEngine.UI.Image>();
            divImg.color = new Color(0.2f, 0.35f, 0.34f, 0.7f);

            GameObject eqRoot = new GameObject("EquippedSection");
            eqRoot.transform.SetParent(card.transform, false);
            RectTransform eqRect = eqRoot.AddComponent<RectTransform>();
            eqRect.anchorMin = new Vector2(0f, 0.1f);
            eqRect.anchorMax = new Vector2(0.4f, 0.88f);
            eqRect.offsetMin = new Vector2(16f, 8f);
            eqRect.offsetMax = new Vector2(-8f, -8f);

            UnityEngine.UI.VerticalLayoutGroup eqLayout = eqRoot.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            eqLayout.spacing = 8f;
            eqLayout.padding = new RectOffset(4, 4, 4, 4);
            eqLayout.childAlignment = TextAnchor.UpperCenter;
            eqLayout.childControlWidth = true;
            eqLayout.childControlHeight = false;
            eqLayout.childForceExpandWidth = true;
            eqLayout.childForceExpandHeight = false;

            GameObject eqHeader = CreateUIText("EquippedHeader", eqRoot.transform, Vector2.zero, "EQUIPPED");
            UnityEngine.UI.Text eqHeaderText = eqHeader.GetComponent<UnityEngine.UI.Text>();
            eqHeaderText.fontSize = 15;
            eqHeaderText.fontStyle = FontStyle.Bold;
            eqHeaderText.alignment = TextAnchor.MiddleLeft;
            eqHeaderText.color = new Color(0.7f, 0.88f, 0.82f);
            UnityEngine.UI.LayoutElement eqHeaderLe = eqHeader.AddComponent<UnityEngine.UI.LayoutElement>();
            eqHeaderLe.minHeight = 22f;
            eqHeaderLe.preferredHeight = 22f;

            UnityEngine.UI.Text weaponText;
            UnityEngine.UI.Button unequipWeapon;
            CreateEquipSlotRow(eqRoot.transform, "Weapon", out weaponText, out unequipWeapon);

            UnityEngine.UI.Text helmetText;
            UnityEngine.UI.Button unequipHelmet;
            CreateEquipSlotRow(eqRoot.transform, "Helmet", out helmetText, out unequipHelmet);

            UnityEngine.UI.Text armorText;
            UnityEngine.UI.Button unequipArmor;
            CreateEquipSlotRow(eqRoot.transform, "Armor", out armorText, out unequipArmor);

            UnityEngine.UI.Text accessoryText;
            UnityEngine.UI.Button unequipAccessory;
            CreateEquipSlotRow(eqRoot.transform, "Accessory", out accessoryText, out unequipAccessory);

            GameObject bonusObj = CreateUIText("BonusSummary", eqRoot.transform, Vector2.zero, "Bonus   ATK +0    HP +0");
            UnityEngine.UI.Text bonusText = bonusObj.GetComponent<UnityEngine.UI.Text>();
            bonusText.fontSize = 13;
            bonusText.color = new Color(1f, 0.85f, 0.4f);
            bonusText.alignment = TextAnchor.MiddleLeft;
            UnityEngine.UI.LayoutElement bonusLe = bonusObj.AddComponent<UnityEngine.UI.LayoutElement>();
            bonusLe.minHeight = 28f;
            bonusLe.preferredHeight = 28f;

            GameObject bagRoot = new GameObject("BagSection");
            bagRoot.transform.SetParent(card.transform, false);
            RectTransform bagRect = bagRoot.AddComponent<RectTransform>();
            // Right side of framed inventory (item grid).
            bagRect.anchorMin = new Vector2(0.42f, 0.14f);
            bagRect.anchorMax = new Vector2(0.94f, 0.82f);
            bagRect.offsetMin = new Vector2(8f, 8f);
            bagRect.offsetMax = new Vector2(-12f, -8f);

            // Empty bag label intentionally omitted — bag stays blank when empty.

            GameObject scrollObj = new GameObject("BagScroll");
            scrollObj.transform.SetParent(bagRoot.transform, false);
            RectTransform scrollRectTf = scrollObj.AddComponent<RectTransform>();
            scrollRectTf.anchorMin = Vector2.zero;
            scrollRectTf.anchorMax = Vector2.one;
            scrollRectTf.offsetMin = Vector2.zero;
            scrollRectTf.offsetMax = Vector2.zero;

            UnityEngine.UI.Image scrollBg = scrollObj.AddComponent<UnityEngine.UI.Image>();
            scrollBg.color = new Color(1f, 1f, 1f, 0f);
            scrollBg.raycastTarget = true;

            UnityEngine.UI.ScrollRect scroll = scrollObj.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(4f, 4f);
            vpRect.offsetMax = new Vector2(-4f, -4f);
            viewport.AddComponent<UnityEngine.UI.RectMask2D>();
            UnityEngine.UI.Image vpImg = viewport.AddComponent<UnityEngine.UI.Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);

            GameObject content = new GameObject("BagContent");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            UnityEngine.UI.GridLayoutGroup grid = content.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = new Vector2(56f, 56f);
            grid.spacing = new Vector2(5f, 5f);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 10;
            grid.startCorner = UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.padding = new RectOffset(4, 4, 4, 4);

            UnityEngine.UI.ContentSizeFitter fitter = content.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRect;
            scroll.content = contentRect;

            // Close hits the frame's top-right X art.
            GameObject closeBtn = CreateButton("CloseInventoryButton", card.transform, new Vector2(430f, 248f), new Vector2(44f, 44f), "");
            UnityEngine.UI.Image closeImg = closeBtn.GetComponent<UnityEngine.UI.Image>();
            if (closeImg != null)
            {
                closeImg.color = new Color(1f, 1f, 1f, 0.01f);
            }

            Game.UI.ItemTooltipUI tooltip = EnsureItemTooltip(panel.transform);

            Game.UI.InventoryPanelUI panelUI = panel.AddComponent<Game.UI.InventoryPanelUI>();
            SerializedObject so = new SerializedObject(panelUI);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("cardBackground").objectReferenceValue = cardBg;
            so.FindProperty("weaponText").objectReferenceValue = weaponText;
            so.FindProperty("helmetText").objectReferenceValue = helmetText;
            so.FindProperty("armorText").objectReferenceValue = armorText;
            so.FindProperty("accessoryText").objectReferenceValue = accessoryText;
            so.FindProperty("bonusSummaryText").objectReferenceValue = bonusText;
            so.FindProperty("unequipWeaponButton").objectReferenceValue = unequipWeapon;
            so.FindProperty("unequipHelmetButton").objectReferenceValue = unequipHelmet;
            so.FindProperty("unequipArmorButton").objectReferenceValue = unequipArmor;
            so.FindProperty("unequipAccessoryButton").objectReferenceValue = unequipAccessory;
            so.FindProperty("bagContentRoot").objectReferenceValue = content.transform;
            so.FindProperty("emptyBagText").objectReferenceValue = null;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("itemTooltip").objectReferenceValue = tooltip;
            so.ApplyModifiedProperties();

            BindInventoryPanelServices(panelUI, itemDatabase);
            BindSampleItemIcons();
            eqRoot.SetActive(false);
            EnsurePaperDollEquipmentSlots(card.transform, panelUI);
            EnsureInventoryVisualLayout(panel, panelUI);

            // Legacy Bag text button wiring removed — BattleMenuUI / HubMenuUI open inventory.

            EnsurePanelSlider(panel);
            panel.SetActive(false);
            Debug.Log("[Setup] InventoryPanel UI rebuilt (clean layout).");
        }

        private static void BindInventoryPanelServices(Game.UI.InventoryPanelUI panelUI, Game.Data.ItemDatabase itemDatabase)
        {
            if (panelUI == null)
            {
                return;
            }

            GameObject inventoryObj = GameObject.Find("InventoryController");
            GameObject equipmentObj = GameObject.Find("EquipmentController");
            SerializedObject panelSO = new SerializedObject(panelUI);
            if (inventoryObj != null)
            {
                panelSO.FindProperty("inventoryService").objectReferenceValue = inventoryObj.GetComponent<Game.Inventory.InventoryService>();
            }

            if (equipmentObj != null)
            {
                panelSO.FindProperty("equipmentService").objectReferenceValue = equipmentObj.GetComponent<Game.Inventory.EquipmentService>();
            }

            panelSO.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
            panelSO.ApplyModifiedProperties();
        }

        private static Sprite LoadInventorySprite(string fileName)
        {
            string path = ART_PATH + "/UI/Inventory/" + fileName;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void BindSampleItemIcons()
        {
            AssignItemIcon("Items/Item_RustySword.asset", "UI/Inventory/Icons/Icon_Sword.png");
            AssignItemIcon("Items/Item_TealRing.asset", "UI/Inventory/Icons/Icon_Ring.png");
            AssignItemIcon("Items/Item_MossHelm.asset", "UI/Inventory/Icons/Icon_Crystal.png");
        }

        private static void AssignItemIcon(string itemRelativePath, string iconRelativePath)
        {
            Game.Data.ItemData item =
                AssetDatabase.LoadAssetAtPath<Game.Data.ItemData>(SCRIPTABLE_PATH + "/" + itemRelativePath);
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(ART_PATH + "/" + iconRelativePath);
            if (item == null || icon == null)
            {
                return;
            }

            item.icon = icon;
            EditorUtility.SetDirty(item);
        }

        /// <summary>
        /// Non-destructive: applies inventory frame, bag grid area, tooltip, close hitbox.
        /// </summary>
        private static void EnsureInventoryVisualLayout(GameObject panel, Game.UI.InventoryPanelUI panelUI)
        {
            if (panel == null || panelUI == null)
            {
                return;
            }

            Transform card = panel.transform.Find("Card");
            if (card == null)
            {
                return;
            }

            RectTransform cardRt = card.GetComponent<RectTransform>();
            // Fit inside CanvasScaler reference (1080x720). Runtime also refits to live canvas.
            if (cardRt != null)
            {
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.anchoredPosition = new Vector2(40f, 0f);
                cardRt.sizeDelta = new Vector2(780f, 520f);
                cardRt.localScale = Vector3.one;
            }

            UnityEngine.UI.Image cardBg = card.GetComponent<UnityEngine.UI.Image>();
            Sprite frame = LoadInventorySprite("Inventory_Frame.png");
            if (cardBg != null && frame != null)
            {
                cardBg.sprite = frame;
                cardBg.type = UnityEngine.UI.Image.Type.Simple;
                cardBg.preserveAspect = true;
                cardBg.color = Color.white;
            }

            // Soft dim behind the frame (was often left disabled → village bleeds through).
            UnityEngine.UI.Image dim = panel.GetComponent<UnityEngine.UI.Image>();
            if (dim != null && dim != cardBg)
            {
                dim.enabled = true;
                dim.sprite = null;
                dim.color = new Color(0f, 0f, 0f, 0.62f);
            }

            // Avoid non-1 scale on the panel root — it softens the frame on retina.
            if (Mathf.Abs(panel.transform.localScale.x - 1f) > 0.01f)
            {
                panel.transform.localScale = Vector3.one;
            }

            // Title/divider are baked into Inventory_Frame.png
            Transform inventoryTitle = card.Find("InventoryTitle");
            if (inventoryTitle != null)
            {
                inventoryTitle.gameObject.SetActive(false);
            }

            Transform divider = card.Find("Divider");
            if (divider != null)
            {
                divider.gameObject.SetActive(false);
            }

            Transform bagSection = card.Find("BagSection");
            if (bagSection != null)
            {
                RectTransform bagRt = bagSection.GetComponent<RectTransform>();
                if (bagRt != null)
                {
                    bagRt.anchorMin = new Vector2(0.445f, 0.145f);
                    bagRt.anchorMax = new Vector2(0.925f, 0.78f);
                    bagRt.offsetMin = Vector2.zero;
                    bagRt.offsetMax = Vector2.zero;
                    bagRt.anchoredPosition = Vector2.zero;
                    bagRt.sizeDelta = Vector2.zero;
                }

                Transform bagTitle = bagSection.Find("BagTitle");
                if (bagTitle != null)
                {
                    bagTitle.gameObject.SetActive(false);
                }
            }

            Transform eqSection = card.Find("EquippedSection");
            if (eqSection != null)
            {
                RectTransform eqRt = eqSection.GetComponent<RectTransform>();
                if (eqRt != null)
                {
                    eqRt.anchorMin = new Vector2(0.06f, 0.14f);
                    eqRt.anchorMax = new Vector2(0.4f, 0.82f);
                }
            }

            // Close button over frame X (top-right of 1080x720 card)
            Transform closeTf = card.Find("CloseInventoryButton");
            if (closeTf == null)
            {
                GameObject closeBtn = CreateButton("CloseInventoryButton", card, new Vector2(500f, 320f), new Vector2(48f, 48f), "");
                closeTf = closeBtn.transform;
                UnityEngine.UI.Image closeImg = closeBtn.GetComponent<UnityEngine.UI.Image>();
                if (closeImg != null)
                {
                    closeImg.color = new Color(1f, 1f, 1f, 0.01f);
                }
            }
            else
            {
                RectTransform closeRt = closeTf.GetComponent<RectTransform>();
                if (closeRt != null)
                {
                    closeRt.anchoredPosition = new Vector2(500f, 320f);
                    closeRt.sizeDelta = new Vector2(48f, 48f);
                }

                UnityEngine.UI.Text closeLabel = closeTf.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (closeLabel != null)
                {
                    closeLabel.text = string.Empty;
                }

                UnityEngine.UI.Image closeImg = closeTf.GetComponent<UnityEngine.UI.Image>();
                if (closeImg != null && closeImg.sprite == null)
                {
                    closeImg.color = new Color(1f, 1f, 1f, 0.01f);
                }
            }

            Game.UI.ItemTooltipUI tooltip = EnsureItemTooltip(panel.transform);

            SerializedObject so = new SerializedObject(panelUI);
            if (cardBg != null)
            {
                so.FindProperty("cardBackground").objectReferenceValue = cardBg;
            }

            if (closeTf != null)
            {
                so.FindProperty("closeButton").objectReferenceValue = closeTf.GetComponent<UnityEngine.UI.Button>();
            }

            if (tooltip != null)
            {
                so.FindProperty("itemTooltip").objectReferenceValue = tooltip;
            }

            // Remove BagScroll dark fill + empty label.
            Transform bagScroll = card.Find("BagSection/BagScroll");
            if (bagScroll != null)
            {
                UnityEngine.UI.Image scrollImg = bagScroll.GetComponent<UnityEngine.UI.Image>();
                if (scrollImg != null)
                {
                    scrollImg.sprite = null;
                    scrollImg.color = new Color(1f, 1f, 1f, 0f);
                }
            }

            Transform emptyLabel = card.Find("BagSection/EmptyBagText");
            if (emptyLabel != null)
            {
                emptyLabel.gameObject.SetActive(false);
            }

            so.FindProperty("emptyBagText").objectReferenceValue = null;

            Transform content = card.Find("BagSection/BagScroll/Viewport/BagContent");
            if (content != null)
            {
                so.FindProperty("bagContentRoot").objectReferenceValue = content;
                UnityEngine.UI.VerticalLayoutGroup vlg = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (vlg != null)
                {
                    UnityEngine.Object.DestroyImmediate(vlg);
                }

                UnityEngine.UI.GridLayoutGroup grid = content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                if (grid == null)
                {
                    grid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                    grid.cellSize = new Vector2(40f, 40f);
                    grid.spacing = new Vector2(5f, 5f);
                    grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = 10;
                    grid.childAlignment = TextAnchor.UpperLeft;
                }
                // Do not overwrite existing grid.cellSize — Edit Mode sticks.
            }

            GameObject slotTemplate = EnsureBagSlotTemplate(bagSection != null ? bagSection : card);
            if (slotTemplate != null)
            {
                so.FindProperty("bagSlotTemplate").objectReferenceValue = slotTemplate;
                // Keep ACTIVE in Edit Mode so you can select Icon/Image; Play Mode hides it in Awake.
                slotTemplate.SetActive(true);
            }

            EnsurePaperDollEquipmentSlots(card, panelUI);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(panelUI);
            Debug.Log("[Setup] Inventory visual layout ensured (frame + grid + tooltip).");
        }

        /// <summary>
        /// Edit Mode template for bag icons. Clone at runtime; edit Icon RectTransform here.
        /// </summary>
        private static GameObject EnsureBagSlotTemplate(Transform bagSection)
        {
            if (bagSection == null)
            {
                return null;
            }

            Transform existing = bagSection.Find("BagSlotTemplate");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("BagSlotTemplate");
                go.transform.SetParent(bagSection, false);
                RectTransform rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(56f, 56f);

                UnityEngine.UI.Image bg = go.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(1f, 1f, 1f, 0.001f);
                bg.raycastTarget = true;

                UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
                btn.targetGraphic = bg;
                btn.transition = UnityEngine.UI.Selectable.Transition.None;

                GameObject iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                RectTransform iconRt = iconGo.AddComponent<RectTransform>();
                // Edit these anchors/offsets in Edit Mode to resize icons.
                iconRt.anchorMin = new Vector2(0.08f, 0.08f);
                iconRt.anchorMax = new Vector2(0.92f, 0.92f);
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;
                UnityEngine.UI.Image icon = iconGo.AddComponent<UnityEngine.UI.Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = Color.white;

                // No stack label — game never stacks items.
                Game.UI.InventorySlotUI slot = go.AddComponent<Game.UI.InventorySlotUI>();
                SerializedObject slotSO = new SerializedObject(slot);
                slotSO.FindProperty("iconImage").objectReferenceValue = icon;
                slotSO.FindProperty("backgroundImage").objectReferenceValue = bg;
                slotSO.FindProperty("button").objectReferenceValue = btn;
                slotSO.ApplyModifiedProperties();
            }

            // Visible in Edit Mode for sizing; parent must stay outside BagContent.
            Transform stackTf = go.transform.Find("Stack");
            if (stackTf != null)
            {
                stackTf.gameObject.SetActive(false);
            }

            go.SetActive(true);
            if (go.transform.parent != null && go.transform.parent.name == "BagContent")
            {
                go.transform.SetParent(bagSection, false);
            }

            // Park template beside the bag so it is easy to select in the Scene view.
            RectTransform parkRt = go.GetComponent<RectTransform>();
            if (parkRt != null)
            {
                parkRt.anchorMin = new Vector2(0.5f, 0.5f);
                parkRt.anchorMax = new Vector2(0.5f, 0.5f);
                parkRt.pivot = new Vector2(0.5f, 0.5f);
                if (parkRt.anchoredPosition == Vector2.zero)
                {
                    parkRt.anchoredPosition = new Vector2(-40f, 200f);
                }
            }

            return go;
        }

        /// <summary>
        /// Creates 8 fixed equipment boxes around the character. Does not move existing slots.
        /// </summary>
        private static void EnsurePaperDollEquipmentSlots(Transform card, Game.UI.InventoryPanelUI panelUI)
        {
            if (card == null || panelUI == null)
            {
                return;
            }

            Transform doll = card.Find("PaperDoll");
            if (doll == null)
            {
                GameObject dollGo = new GameObject("PaperDoll");
                dollGo.transform.SetParent(card, false);
                RectTransform dollRt = dollGo.AddComponent<RectTransform>();
                // Left character well inside Inventory_Frame.png
                dollRt.anchorMin = new Vector2(0.07f, 0.16f);
                dollRt.anchorMax = new Vector2(0.40f, 0.78f);
                dollRt.offsetMin = Vector2.zero;
                dollRt.offsetMax = Vector2.zero;
                doll = dollGo.transform;
            }

            // Hide legacy text equipped list when paper doll exists.
            Transform legacy = card.Find("EquippedSection");
            if (legacy != null)
            {
                legacy.gameObject.SetActive(false);
            }

            // Left column / Right column — Edit Mode'da taşıyabilirsin; Fix Hub mevcut pozisyonu bozmaz.
            var defs = new (string name, Game.Data.EquipmentSlot slot, Vector2 pos)[]
            {
                ("Eq_Helmet", Game.Data.EquipmentSlot.Helmet, new Vector2(-70f, 110f)),
                ("Eq_Armor", Game.Data.EquipmentSlot.Armor, new Vector2(-70f, 30f)),
                ("Eq_Legs", Game.Data.EquipmentSlot.Legs, new Vector2(-70f, -50f)),
                ("Eq_Accessory", Game.Data.EquipmentSlot.Accessory, new Vector2(-70f, -130f)),
                ("Eq_Shoulders", Game.Data.EquipmentSlot.Shoulders, new Vector2(70f, 110f)),
                ("Eq_Weapon", Game.Data.EquipmentSlot.Weapon, new Vector2(70f, 30f)),
                ("Eq_Boots", Game.Data.EquipmentSlot.Boots, new Vector2(70f, -50f)),
                ("Eq_Ring2", Game.Data.EquipmentSlot.Ring2, new Vector2(70f, -130f)),
            };

            var slots = new List<Game.UI.EquipmentSlotUI>();
            for (int i = 0; i < defs.Length; i++)
            {
                Transform existing = doll.Find(defs[i].name);
                Game.UI.EquipmentSlotUI slotUi;
                if (existing != null)
                {
                    slotUi = existing.GetComponent<Game.UI.EquipmentSlotUI>()
                             ?? existing.gameObject.AddComponent<Game.UI.EquipmentSlotUI>();
                }
                else
                {
                    slotUi = CreatePaperDollSlot(doll, defs[i].name, defs[i].slot, defs[i].pos);
                }

                SerializedObject slotSO = new SerializedObject(slotUi);
                slotSO.FindProperty("slot").enumValueIndex = (int)defs[i].slot;
                UnityEngine.UI.Image bg = slotUi.GetComponent<UnityEngine.UI.Image>();
                if (bg != null)
                {
                    // No tinted slot fill — frame art provides the box.
                    bg.color = new Color(1f, 1f, 1f, 0.001f);
                    bg.sprite = null;
                    bg.raycastTarget = true;
                }

                UnityEngine.UI.Image icon = null;
                Transform iconTf = slotUi.transform.Find("Icon");
                if (iconTf != null) icon = iconTf.GetComponent<UnityEngine.UI.Image>();
                UnityEngine.UI.Text ph = null;
                Transform phTf = slotUi.transform.Find("Placeholder");
                if (phTf != null) ph = phTf.GetComponent<UnityEngine.UI.Text>();
                if (bg != null) slotSO.FindProperty("backgroundImage").objectReferenceValue = bg;
                if (icon != null) slotSO.FindProperty("iconImage").objectReferenceValue = icon;
                if (ph != null) slotSO.FindProperty("placeholderText").objectReferenceValue = ph;
                slotSO.ApplyModifiedProperties();
                slots.Add(slotUi);
            }

            // Bonus text under doll if missing
            Transform bonusTf = card.Find("PaperDollBonus");
            UnityEngine.UI.Text bonusText = null;
            if (bonusTf == null)
            {
                GameObject bonusGo = CreateUIText("PaperDollBonus", card, new Vector2(-260f, -200f), "ATK +0   HP +0");
                bonusText = bonusGo.GetComponent<UnityEngine.UI.Text>();
                bonusText.fontSize = 14;
                bonusText.color = new Color(1f, 0.85f, 0.4f);
                bonusText.alignment = TextAnchor.MiddleCenter;
                RectTransform brt = bonusGo.GetComponent<RectTransform>();
                brt.sizeDelta = new Vector2(260f, 28f);
            }
            else
            {
                bonusText = bonusTf.GetComponent<UnityEngine.UI.Text>();
            }

            SerializedObject panelSO = new SerializedObject(panelUI);
            SerializedProperty slotsProp = panelSO.FindProperty("equipmentSlots");
            if (slotsProp != null)
            {
                slotsProp.arraySize = slots.Count;
                for (int i = 0; i < slots.Count; i++)
                {
                    slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
                }
            }

            if (bonusText != null)
            {
                panelSO.FindProperty("bonusSummaryText").objectReferenceValue = bonusText;
            }

            panelSO.ApplyModifiedProperties();
            Debug.Log("[Setup] PaperDoll 8 equipment slots ensured (positions editable in Edit Mode).");
        }

        private static Game.UI.EquipmentSlotUI CreatePaperDollSlot(
            Transform parent,
            string name,
            Game.Data.EquipmentSlot slot,
            Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(72f, 72f);
            rt.anchoredPosition = anchoredPos;

            UnityEngine.UI.Image bg = go.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(1f, 1f, 1f, 0.001f);
            bg.raycastTarget = true;

            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            RectTransform iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.12f, 0.12f);
            iconRt.anchorMax = new Vector2(0.88f, 0.88f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            UnityEngine.UI.Image icon = iconGo.AddComponent<UnityEngine.UI.Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;

            GameObject phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            RectTransform phRt = phGo.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = Vector2.zero;
            phRt.offsetMax = Vector2.zero;
            UnityEngine.UI.Text ph = phGo.AddComponent<UnityEngine.UI.Text>();
            ph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ph.fontSize = 10;
            ph.alignment = TextAnchor.MiddleCenter;
            ph.color = new Color(0.45f, 0.55f, 0.52f, 0.85f);
            ph.raycastTarget = false;
            ph.text = ShortSlotLabel(slot);

            Game.UI.EquipmentSlotUI slotUi = go.AddComponent<Game.UI.EquipmentSlotUI>();
            SerializedObject so = new SerializedObject(slotUi);
            so.FindProperty("slot").enumValueIndex = (int)slot;
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("backgroundImage").objectReferenceValue = bg;
            so.FindProperty("placeholderText").objectReferenceValue = ph;
            so.ApplyModifiedProperties();
            return slotUi;
        }

        private static string ShortSlotLabel(Game.Data.EquipmentSlot slot)
        {
            switch (slot)
            {
                case Game.Data.EquipmentSlot.Helmet: return "HEL";
                case Game.Data.EquipmentSlot.Armor: return "ARM";
                case Game.Data.EquipmentSlot.Legs: return "LEG";
                case Game.Data.EquipmentSlot.Accessory: return "RNG";
                case Game.Data.EquipmentSlot.Shoulders: return "SHD";
                case Game.Data.EquipmentSlot.Weapon: return "WPN";
                case Game.Data.EquipmentSlot.Boots: return "BOT";
                case Game.Data.EquipmentSlot.Ring2: return "RN2";
                default: return slot.ToString();
            }
        }

        private static Game.UI.ItemTooltipUI EnsureItemTooltip(Transform panel)
        {
            if (panel == null)
            {
                return null;
            }

            Transform existing = panel.Find("ItemTooltip");
            GameObject tipGo;
            if (existing != null)
            {
                tipGo = existing.gameObject;
            }
            else
            {
                tipGo = new GameObject("ItemTooltip");
                tipGo.transform.SetParent(panel, false);
            }

            RectTransform tipRt = tipGo.GetComponent<RectTransform>();
            if (tipRt == null)
            {
                tipRt = tipGo.AddComponent<RectTransform>();
            }

            tipRt.anchorMin = new Vector2(0.5f, 0.5f);
            tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipRt.pivot = new Vector2(0f, 1f);
            tipRt.sizeDelta = new Vector2(280f, 160f);

            UnityEngine.UI.Image frame = tipGo.GetComponent<UnityEngine.UI.Image>();
            if (frame == null)
            {
                frame = tipGo.AddComponent<UnityEngine.UI.Image>();
            }

            Sprite tipSprite = LoadInventorySprite("Item_Tooltip_Frame.png");
            if (tipSprite != null)
            {
                frame.sprite = tipSprite;
                frame.type = UnityEngine.UI.Image.Type.Sliced;
                frame.color = Color.white;
            }
            else
            {
                frame.color = new Color(0.05f, 0.08f, 0.1f, 0.96f);
            }

            frame.raycastTarget = false;

            Transform nameTf = tipGo.transform.Find("Name");
            UnityEngine.UI.Text nameText;
            if (nameTf == null)
            {
                GameObject nameGo = CreateUIText("Name", tipGo.transform, new Vector2(0f, 48f), "Item");
                nameText = nameGo.GetComponent<UnityEngine.UI.Text>();
                RectTransform nrt = nameGo.GetComponent<RectTransform>();
                nrt.anchorMin = new Vector2(0f, 1f);
                nrt.anchorMax = new Vector2(1f, 1f);
                nrt.pivot = new Vector2(0.5f, 1f);
                nrt.anchoredPosition = new Vector2(0f, -18f);
                nrt.sizeDelta = new Vector2(-36f, 28f);
            }
            else
            {
                nameText = nameTf.GetComponent<UnityEngine.UI.Text>();
            }

            nameText.fontSize = 16;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.raycastTarget = false;

            Transform bodyTf = tipGo.transform.Find("Body");
            UnityEngine.UI.Text bodyText;
            if (bodyTf == null)
            {
                GameObject bodyGo = CreateUIText("Body", tipGo.transform, Vector2.zero, "Stats");
                bodyText = bodyGo.GetComponent<UnityEngine.UI.Text>();
                RectTransform brt = bodyGo.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0f, 0f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.offsetMin = new Vector2(22f, 22f);
                brt.offsetMax = new Vector2(-22f, -52f);
            }
            else
            {
                bodyText = bodyTf.GetComponent<UnityEngine.UI.Text>();
            }

            bodyText.fontSize = 13;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.raycastTarget = false;

            Game.UI.ItemTooltipUI tip = tipGo.GetComponent<Game.UI.ItemTooltipUI>()
                                       ?? tipGo.AddComponent<Game.UI.ItemTooltipUI>();
            SerializedObject tipSO = new SerializedObject(tip);
            tipSO.FindProperty("root").objectReferenceValue = tipGo;
            tipSO.FindProperty("frameImage").objectReferenceValue = frame;
            tipSO.FindProperty("nameText").objectReferenceValue = nameText;
            tipSO.FindProperty("bodyText").objectReferenceValue = bodyText;
            tipSO.ApplyModifiedProperties();
            tipGo.SetActive(false);
            tipGo.transform.SetAsLastSibling();
            return tip;
        }

        private static void CreateEquipSlotRow(
            Transform parent,
            string slotName,
            out UnityEngine.UI.Text slotText,
            out UnityEngine.UI.Button unequipButton)
        {
            GameObject row = new GameObject(slotName + "Slot");
            row.transform.SetParent(parent, false);

            UnityEngine.UI.LayoutElement rowLe = row.AddComponent<UnityEngine.UI.LayoutElement>();
            rowLe.minHeight = 58f;
            rowLe.preferredHeight = 58f;

            UnityEngine.UI.Image rowBg = row.AddComponent<UnityEngine.UI.Image>();
            rowBg.color = new Color(0.06f, 0.12f, 0.14f, 0.95f);

            UnityEngine.UI.HorizontalLayoutGroup h = row.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 4, 4);
            h.spacing = 6f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            GameObject textObj = new GameObject(slotName + "Text");
            textObj.transform.SetParent(row.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(160f, 50f);
            UnityEngine.UI.LayoutElement textLe = textObj.AddComponent<UnityEngine.UI.LayoutElement>();
            textLe.flexibleWidth = 1f;
            textLe.preferredWidth = 160f;
            textLe.minHeight = 50f;

            slotText = textObj.AddComponent<UnityEngine.UI.Text>();
            slotText.text = slotName + "\n— empty —";
            slotText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slotText.fontSize = 12;
            slotText.color = new Color(0.55f, 0.62f, 0.6f, 1f);
            slotText.alignment = TextAnchor.MiddleLeft;
            slotText.horizontalOverflow = HorizontalWrapMode.Wrap;
            slotText.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject btnObj = CreateButton("Unequip" + slotName, row.transform, Vector2.zero, new Vector2(72f, 28f), "Unequip");
            UnityEngine.UI.LayoutElement btnLe = btnObj.AddComponent<UnityEngine.UI.LayoutElement>();
            btnLe.preferredWidth = 72f;
            btnLe.minWidth = 72f;
            btnLe.preferredHeight = 28f;
            unequipButton = btnObj.GetComponent<UnityEngine.UI.Button>();
            unequipButton.gameObject.SetActive(false);
        }

        private static void CreateSampleLootData()
        {
            EnsureFolder(SCRIPTABLE_PATH + "/Items");
            EnsureFolder(SCRIPTABLE_PATH + "/Chests");

            Game.Data.ItemData sword = CreateOrUpdateItem(
                SCRIPTABLE_PATH + "/Items/Item_RustySword.asset",
                "rusty_sword", "Rusty Sword", Game.Data.ItemRarity.Common, Game.Data.EquipmentSlot.Weapon, 6, 0);

            Game.Data.ItemData helm = CreateOrUpdateItem(
                SCRIPTABLE_PATH + "/Items/Item_MossHelm.asset",
                "moss_helm", "Moss Helm", Game.Data.ItemRarity.Uncommon, Game.Data.EquipmentSlot.Helmet, 0, 35);

            Game.Data.ItemData armor = CreateOrUpdateItem(
                SCRIPTABLE_PATH + "/Items/Item_SwampArmor.asset",
                "swamp_armor", "Swamp Armor", Game.Data.ItemRarity.Rare, Game.Data.EquipmentSlot.Armor, 3, 55);

            Game.Data.ItemData ring = CreateOrUpdateItem(
                SCRIPTABLE_PATH + "/Items/Item_TealRing.asset",
                "teal_ring", "Teal Ring", Game.Data.ItemRarity.Epic, Game.Data.EquipmentSlot.Accessory, 5, 25);

            string dbPath = SCRIPTABLE_PATH + "/Items/ItemDatabase.asset";
            Game.Data.ItemDatabase database = AssetDatabase.LoadAssetAtPath<Game.Data.ItemDatabase>(dbPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<Game.Data.ItemDatabase>();
                AssetDatabase.CreateAsset(database, dbPath);
            }

            database.SetItems(new List<Game.Data.ItemData> { sword, helm, armor, ring });
            EditorUtility.SetDirty(database);

            string chestPath = SCRIPTABLE_PATH + "/Chests/Chest_Wooden.asset";
            Game.Data.ChestData chest = AssetDatabase.LoadAssetAtPath<Game.Data.ChestData>(chestPath);
            bool isNewChest = chest == null;
            if (isNewChest)
            {
                chest = ScriptableObject.CreateInstance<Game.Data.ChestData>();
            }

            chest.chestId = "wooden_chest";
            chest.displayName = "Wooden Chest";
            chest.minimumItems = 1;
            chest.maximumItems = 1;
            chest.possibleItems = new List<Game.Data.ItemData> { sword, helm, armor, ring };

            if (isNewChest)
            {
                AssetDatabase.CreateAsset(chest, chestPath);
            }
            else
            {
                EditorUtility.SetDirty(chest);
            }

            Debug.Log("[Setup] Sample loot data ready.");
        }

        private static Game.Data.ItemData CreateOrUpdateItem(
            string path,
            string itemId,
            string displayName,
            Game.Data.ItemRarity rarity,
            Game.Data.EquipmentSlot slot,
            int attackBonus,
            int healthBonus)
        {
            Game.Data.ItemData item = AssetDatabase.LoadAssetAtPath<Game.Data.ItemData>(path);
            bool isNew = item == null;
            if (isNew)
            {
                item = ScriptableObject.CreateInstance<Game.Data.ItemData>();
            }

            item.itemId = itemId;
            item.displayName = displayName;
            item.rarity = rarity;
            item.equipmentSlot = slot;
            item.attackBonus = attackBonus;
            item.healthBonus = healthBonus;

            if (isNew)
            {
                AssetDatabase.CreateAsset(item, path);
            }
            else
            {
                EditorUtility.SetDirty(item);
            }

            return item;
        }

        private static void EnsureSaveSystems()
        {
            GameObject systems = GameObject.Find("Systems");
            if (systems == null)
            {
                Debug.LogWarning("[Setup] Systems root not found for save setup.");
                return;
            }

            GameObject saveController = FindOrCreate("SaveController", systems.transform);
            if (saveController.GetComponent<Game.Save.SaveService>() == null)
            {
                saveController.AddComponent<Game.Save.SaveService>();
            }

            if (saveController.GetComponent<Game.Save.GameSaveController>() == null)
            {
                saveController.AddComponent<Game.Save.GameSaveController>();
            }

            Debug.Log("[Setup] Save systems ensured.");
        }

        private static void CreateSampleMapData()
        {
            CreateSampleLootData();
            CreateSampleStageDataAsset();

            EnsureFolder(SCRIPTABLE_PATH + "/Map");
            EnsureFolder(SCRIPTABLE_PATH + "/Map/Layer1");
            EnsureFolder(SCRIPTABLE_PATH + "/Chests");

            Game.Data.StageData stage01 = AssetDatabase.LoadAssetAtPath<Game.Data.StageData>(SCRIPTABLE_PATH + "/Stages/Stage_01.asset");
            Game.Data.StageData stageBossOnly = AssetDatabase.LoadAssetAtPath<Game.Data.StageData>(SCRIPTABLE_PATH + "/Stages/Stage_BossOnly.asset");
            if (stageBossOnly == null)
            {
                CreateSampleStageDataAsset();
                stageBossOnly = AssetDatabase.LoadAssetAtPath<Game.Data.StageData>(SCRIPTABLE_PATH + "/Stages/Stage_BossOnly.asset");
            }

            Game.Data.ChestData wooden = AssetDatabase.LoadAssetAtPath<Game.Data.ChestData>(SCRIPTABLE_PATH + "/Chests/Chest_Wooden.asset");

            Game.Data.ChestData normalChest = CreateOrUpdateTierChest(
                SCRIPTABLE_PATH + "/Chests/Chest_Normal.asset",
                "chest_normal", "Normal Chest", Game.Data.ChestTier.Normal, wooden);
            Game.Data.ChestData levelBossChest = CreateOrUpdateTierChest(
                SCRIPTABLE_PATH + "/Chests/Chest_LevelBoss.asset",
                "chest_level_boss", "Level Boss Chest", Game.Data.ChestTier.LevelBoss, wooden);
            Game.Data.ChestData chapterBossChest = CreateOrUpdateTierChest(
                SCRIPTABLE_PATH + "/Chests/Chest_ChapterBoss.asset",
                "chest_chapter_boss", "Chapter Boss Chest", Game.Data.ChestTier.ChapterBoss, wooden);
            Game.Data.ChestData layerBossChest = CreateOrUpdateTierChest(
                SCRIPTABLE_PATH + "/Chests/Chest_LayerBoss.asset",
                "chest_layer_boss", "Layer Boss Chest", Game.Data.ChestTier.LayerBoss, wooden);
            Game.Data.ChestData dungeonBossChest = CreateOrUpdateTierChest(
                SCRIPTABLE_PATH + "/Chests/Chest_DungeonBoss.asset",
                "chest_dungeon_boss", "Dungeon Boss Chest", Game.Data.ChestTier.DungeonBoss, wooden);

            string[] chapterNames = { "Mist Path", "Bog Crossing", "Ruined Gate" };
            List<Game.Data.ChapterData> chapters = new List<Game.Data.ChapterData>();

            for (int c = 1; c <= 3; c++)
            {
                EnsureFolder(SCRIPTABLE_PATH + $"/Map/Layer1/Chapter{c}");
                List<Game.Data.LevelData> levels = new List<Game.Data.LevelData>();

                for (int v = 1; v <= 10; v++)
                {
                    string levelPath = SCRIPTABLE_PATH + $"/Map/Layer1/Chapter{c}/Level_{v:00}.asset";
                    Game.Data.LevelData level = AssetDatabase.LoadAssetAtPath<Game.Data.LevelData>(levelPath);
                    bool isNew = level == null;
                    if (isNew)
                    {
                        level = ScriptableObject.CreateInstance<Game.Data.LevelData>();
                    }

                    bool isChapterBoss = v == 10;
                    level.levelId = $"L1_C{c}_{v:00}";
                    level.displayName = isChapterBoss
                        ? $"L1-C{c} Chapter Boss"
                        : $"L1-C{c} Level {v}";
                    level.layerIndex = 1;
                    level.chapterIndex = c;
                    level.levelIndex = v;
                    level.battleContent = isChapterBoss && stageBossOnly != null ? stageBossOnly : stage01;
                    level.isChapterBoss = isChapterBoss;
                    level.completionChestTier = isChapterBoss
                        ? Game.Data.ChestTier.ChapterBoss
                        : Game.Data.ChestTier.LevelBoss;
                    level.completionChest = isChapterBoss ? chapterBossChest : levelBossChest;

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(level, levelPath);
                    }
                    else
                    {
                        EditorUtility.SetDirty(level);
                    }

                    levels.Add(level);
                }

                string chapterPath = SCRIPTABLE_PATH + $"/Map/Layer1/Chapter_{c}.asset";
                Game.Data.ChapterData chapter = AssetDatabase.LoadAssetAtPath<Game.Data.ChapterData>(chapterPath);
                bool chapterNew = chapter == null;
                if (chapterNew)
                {
                    chapter = ScriptableObject.CreateInstance<Game.Data.ChapterData>();
                }

                chapter.chapterId = $"L1_C{c}";
                chapter.displayName = $"Chapter {c}: {chapterNames[c - 1]}";
                chapter.layerIndex = 1;
                chapter.chapterIndex = c;
                chapter.levels = levels;

                if (chapterNew)
                {
                    AssetDatabase.CreateAsset(chapter, chapterPath);
                }
                else
                {
                    EditorUtility.SetDirty(chapter);
                }

                chapters.Add(chapter);
            }

            string layerPath = SCRIPTABLE_PATH + "/Map/Layer1/Layer_01.asset";
            Game.Data.LayerData layer = AssetDatabase.LoadAssetAtPath<Game.Data.LayerData>(layerPath);
            bool layerNew = layer == null;
            if (layerNew)
            {
                layer = ScriptableObject.CreateInstance<Game.Data.LayerData>();
            }

            layer.layerId = "layer_01";
            layer.displayName = "Layer 1: Swamp Frontier";
            layer.layerIndex = 1;
            layer.chapters = chapters;
            layer.layerBossBattle = stageBossOnly != null ? stageBossOnly : stage01;
            layer.layerBossDisplayName = "Swamp Layer Boss";
            layer.layerBossChest = layerBossChest;
            layer.requireSummonStone = false;

            if (layerNew)
            {
                AssetDatabase.CreateAsset(layer, layerPath);
            }
            else
            {
                EditorUtility.SetDirty(layer);
            }

            string catalogPath = SCRIPTABLE_PATH + "/Map/MapCatalog.asset";
            Game.Data.MapCatalog catalog = AssetDatabase.LoadAssetAtPath<Game.Data.MapCatalog>(catalogPath);
            bool catalogNew = catalog == null;
            if (catalogNew)
            {
                catalog = ScriptableObject.CreateInstance<Game.Data.MapCatalog>();
            }

            catalog.SetLayers(new List<Game.Data.LayerData> { layer });
            catalog.normalChest = normalChest;
            catalog.levelBossChest = levelBossChest;
            catalog.chapterBossChest = chapterBossChest;
            catalog.layerBossChest = layerBossChest;
            catalog.dungeonBossChest = dungeonBossChest;

            if (catalogNew)
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            else
            {
                EditorUtility.SetDirty(catalog);
            }

            Debug.Log("[Setup] Sample map data created: Layer 1 with 3 chapters x 10 levels.");
        }

        private static void CreateSampleVillageData()
        {
            EnsureFolder(SCRIPTABLE_PATH + "/Village");

            Game.Data.VillageBuildingData library = CreateOrUpdateVillageBuilding(
                SCRIPTABLE_PATH + "/Village/Building_Library.asset",
                "building_library",
                "Library",
                Game.Data.VillageBuildingType.Library,
                goldPerHour: 0,
                expPerHour: 40,
                goldPerLevel: 0,
                expPerLevel: 15,
                craftBonus: 0f);

            Game.Data.VillageBuildingData mine = CreateOrUpdateVillageBuilding(
                SCRIPTABLE_PATH + "/Village/Building_Mine.asset",
                "building_mine",
                "Mine",
                Game.Data.VillageBuildingType.Mine,
                goldPerHour: 80,
                expPerHour: 0,
                goldPerLevel: 25,
                expPerLevel: 0,
                craftBonus: 0f);

            Game.Data.VillageBuildingData blacksmith = CreateOrUpdateVillageBuilding(
                SCRIPTABLE_PATH + "/Village/Building_Blacksmith.asset",
                "building_blacksmith",
                "Blacksmith",
                Game.Data.VillageBuildingType.Blacksmith,
                goldPerHour: 0,
                expPerHour: 0,
                goldPerLevel: 0,
                expPerLevel: 0,
                craftBonus: 0.05f);

            string catalogPath = SCRIPTABLE_PATH + "/Village/VillageCatalog.asset";
            Game.Data.VillageCatalog catalog = AssetDatabase.LoadAssetAtPath<Game.Data.VillageCatalog>(catalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<Game.Data.VillageCatalog>();
            }

            catalog.SetBuildings(new List<Game.Data.VillageBuildingData> { library, mine, blacksmith });
            if (isNew)
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            else
            {
                EditorUtility.SetDirty(catalog);
            }

            Debug.Log("[Setup] Village catalog created (Library / Mine / Blacksmith).");
        }

        private static void CreateSampleCraftData()
        {
            EnsureFolder(SCRIPTABLE_PATH + "/Crafting");
            EnsureFolder(SCRIPTABLE_PATH + "/Crafting/Materials");
            EnsureFolder(SCRIPTABLE_PATH + "/Crafting/Recipes");

            Game.Data.MaterialData scrap = CreateOrUpdateMaterial(
                SCRIPTABLE_PATH + "/Crafting/Materials/Mat_Scrap.asset",
                "mat_scrap", "Scrap", false, false, false, Game.Data.ItemRarity.Common, 0f);
            Game.Data.MaterialData iron = CreateOrUpdateMaterial(
                SCRIPTABLE_PATH + "/Crafting/Materials/Mat_IronOre.asset",
                "mat_iron_ore", "Iron Ore", false, false, false, Game.Data.ItemRarity.Common, 0f);
            Game.Data.MaterialData leather = CreateOrUpdateMaterial(
                SCRIPTABLE_PATH + "/Crafting/Materials/Mat_Leather.asset",
                "mat_leather", "Leather", false, false, false, Game.Data.ItemRarity.Common, 0f);
            Game.Data.MaterialData gemDust = CreateOrUpdateMaterial(
                SCRIPTABLE_PATH + "/Crafting/Materials/Mat_GemDust.asset",
                "mat_gem_dust", "Gem Dust", false, false, false, Game.Data.ItemRarity.Common, 0f);
            Game.Data.MaterialData summon = CreateOrUpdateMaterial(
                SCRIPTABLE_PATH + "/Crafting/Materials/Mat_SummonStone.asset",
                "mat_summon_stone", "Summon Stone", true, false, false, Game.Data.ItemRarity.Rare, 0f);
            Game.Data.MaterialData courage = CreateOrUpdateMaterial(
                SCRIPTABLE_PATH + "/Crafting/Materials/Mat_CourageStone.asset",
                "mat_courage_stone", "Courage Stone", false, true, false, Game.Data.ItemRarity.Epic, 0f);
            Game.Data.MaterialData boostCommon = CreateOrUpdateMaterial(
                SCRIPTABLE_PATH + "/Crafting/Materials/Mat_Boost_Common.asset",
                "mat_boost_common", "Sıradan Catalyst", false, false, true, Game.Data.ItemRarity.Common, 0.15f);
            Game.Data.MaterialData boostUncommon = CreateOrUpdateMaterial(
                SCRIPTABLE_PATH + "/Crafting/Materials/Mat_Boost_Uncommon.asset",
                "mat_boost_uncommon", "Yaygın Catalyst", false, false, true, Game.Data.ItemRarity.Uncommon, 0.15f);

            Game.Data.ItemData rusty = AssetDatabase.LoadAssetAtPath<Game.Data.ItemData>(SCRIPTABLE_PATH + "/Items/Item_RustySword.asset");
            Game.Data.ItemData helm = AssetDatabase.LoadAssetAtPath<Game.Data.ItemData>(SCRIPTABLE_PATH + "/Items/Item_MossHelm.asset");
            Game.Data.ItemData armor = AssetDatabase.LoadAssetAtPath<Game.Data.ItemData>(SCRIPTABLE_PATH + "/Items/Item_SwampArmor.asset");
            Game.Data.ItemData ring = AssetDatabase.LoadAssetAtPath<Game.Data.ItemData>(SCRIPTABLE_PATH + "/Items/Item_TealRing.asset");

            SetItemLevelBand(rusty, 1);
            SetItemLevelBand(helm, 1);
            SetItemLevelBand(armor, 1);
            SetItemLevelBand(ring, 1);

            Game.Data.ItemData steelSword = CreateOrUpdateMergeItem(
                SCRIPTABLE_PATH + "/Items/Item_SteelSword.asset",
                "steel_sword", "Steel Sword", Game.Data.ItemRarity.Uncommon, Game.Data.EquipmentSlot.Weapon, 1, 12, 0);
            Game.Data.ItemData jadeHelm = CreateOrUpdateMergeItem(
                SCRIPTABLE_PATH + "/Items/Item_JadeHelm.asset",
                "jade_helm", "Jade Helm", Game.Data.ItemRarity.Rare, Game.Data.EquipmentSlot.Helmet, 1, 0, 70);
            Game.Data.ItemData bogPlate = CreateOrUpdateMergeItem(
                SCRIPTABLE_PATH + "/Items/Item_BogPlate.asset",
                "bog_plate", "Bog Plate", Game.Data.ItemRarity.Uncommon, Game.Data.EquipmentSlot.Armor, 1, 0, 90);
            Game.Data.ItemData emberRing = CreateOrUpdateMergeItem(
                SCRIPTABLE_PATH + "/Items/Item_EmberRing.asset",
                "ember_ring", "Ember Ring", Game.Data.ItemRarity.Rare, Game.Data.EquipmentSlot.Accessory, 1, 8, 20);

            Game.Data.CraftRecipeData recipeSword = CreateOrUpdateRecipe(
                SCRIPTABLE_PATH + "/Crafting/Recipes/Recipe_RustySword.asset",
                "recipe_rusty_sword",
                "Rusty Sword",
                rusty,
                25,
                1,
                new[]
                {
                    new Game.Data.CraftIngredient { material = scrap, amount = 3 },
                    new Game.Data.CraftIngredient { material = iron, amount = 2 }
                });

            Game.Data.CraftRecipeData recipeArmor = CreateOrUpdateRecipe(
                SCRIPTABLE_PATH + "/Crafting/Recipes/Recipe_SwampArmor.asset",
                "recipe_swamp_armor",
                "Swamp Armor",
                armor,
                40,
                1,
                new[]
                {
                    new Game.Data.CraftIngredient { material = scrap, amount = 4 },
                    new Game.Data.CraftIngredient { material = leather, amount = 3 }
                });

            Game.Data.CraftRecipeData recipeSummon = CreateOrUpdateRecipe(
                SCRIPTABLE_PATH + "/Crafting/Recipes/Recipe_SummonStone.asset",
                "recipe_summon_stone",
                "Summon Stone",
                null,
                100,
                2,
                new[]
                {
                    new Game.Data.CraftIngredient { material = gemDust, amount = 5 },
                    new Game.Data.CraftIngredient { material = iron, amount = 3 },
                    new Game.Data.CraftIngredient { material = scrap, amount = 5 }
                });

            string catalogPath = SCRIPTABLE_PATH + "/Crafting/CraftCatalog.asset";
            Game.Data.CraftCatalog catalog = AssetDatabase.LoadAssetAtPath<Game.Data.CraftCatalog>(catalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<Game.Data.CraftCatalog>();
            }

            catalog.materials = new List<Game.Data.MaterialData>
            {
                scrap, iron, leather, gemDust, summon, courage, boostCommon, boostUncommon
            };
            catalog.recipes = new List<Game.Data.CraftRecipeData> { recipeSword, recipeArmor, recipeSummon };
            catalog.scrapMaterial = scrap;
            catalog.scrapByRarity = new[] { 1, 2, 3, 5, 8 };
            catalog.mergeInputCount = 9;
            catalog.chancePlusOneRarity = 0.25f;
            catalog.chancePlusTwoRarity = 0.05f;
            catalog.mergeResultPool = new List<Game.Data.ItemData>();
            if (rusty != null) catalog.mergeResultPool.Add(rusty);
            if (helm != null) catalog.mergeResultPool.Add(helm);
            if (armor != null) catalog.mergeResultPool.Add(armor);
            if (ring != null) catalog.mergeResultPool.Add(ring);
            catalog.mergeResultPool.Add(steelSword);
            catalog.mergeResultPool.Add(jadeHelm);
            catalog.mergeResultPool.Add(bogPlate);
            catalog.mergeResultPool.Add(emberRing);
            catalog.summonStone = summon;
            catalog.courageStone = courage;
            catalog.summonStoneRecipe = recipeSummon;

            if (isNew)
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            else
            {
                EditorUtility.SetDirty(catalog);
            }

            Game.Data.ItemDatabase database = AssetDatabase.LoadAssetAtPath<Game.Data.ItemDatabase>(SCRIPTABLE_PATH + "/Items/ItemDatabase.asset");
            if (database != null)
            {
                List<Game.Data.ItemData> all = new List<Game.Data.ItemData>();
                if (rusty != null) all.Add(rusty);
                if (helm != null) all.Add(helm);
                if (armor != null) all.Add(armor);
                if (ring != null) all.Add(ring);
                all.Add(steelSword);
                all.Add(jadeHelm);
                all.Add(bogPlate);
                all.Add(emberRing);
                database.SetItems(all);
                EditorUtility.SetDirty(database);
            }

            Debug.Log("[Setup] Craft catalog created (materials, recipes, merge pool, summon/courage stones).");
        }

        private static Game.Data.MaterialData CreateOrUpdateMaterial(
            string path,
            string id,
            string displayName,
            bool isSummon,
            bool isCourage,
            bool isBooster,
            Game.Data.ItemRarity boostRarity,
            float successBonus)
        {
            Game.Data.MaterialData data = AssetDatabase.LoadAssetAtPath<Game.Data.MaterialData>(path);
            bool isNew = data == null;
            if (isNew)
            {
                data = ScriptableObject.CreateInstance<Game.Data.MaterialData>();
            }

            data.materialId = id;
            data.displayName = displayName;
            data.isSummonStone = isSummon;
            data.isCourageStone = isCourage;
            data.isMergeBooster = isBooster;
            data.boostsRarity = boostRarity;
            data.successBonus = successBonus;

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static Game.Data.ItemData CreateOrUpdateMergeItem(
            string path,
            string id,
            string displayName,
            Game.Data.ItemRarity rarity,
            Game.Data.EquipmentSlot slot,
            int levelBand,
            int attack,
            int health)
        {
            Game.Data.ItemData data = AssetDatabase.LoadAssetAtPath<Game.Data.ItemData>(path);
            bool isNew = data == null;
            if (isNew)
            {
                data = ScriptableObject.CreateInstance<Game.Data.ItemData>();
            }

            data.itemId = id;
            data.displayName = displayName;
            data.rarity = rarity;
            data.equipmentSlot = slot;
            data.levelBand = levelBand;
            data.attackBonus = attack;
            data.healthBonus = health;

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static void SetItemLevelBand(Game.Data.ItemData item, int band)
        {
            if (item == null)
            {
                return;
            }

            item.levelBand = band;
            EditorUtility.SetDirty(item);
        }

        private static Game.Data.CraftRecipeData CreateOrUpdateRecipe(
            string path,
            string id,
            string displayName,
            Game.Data.ItemData output,
            int goldCost,
            int blacksmithLevel,
            Game.Data.CraftIngredient[] ingredients)
        {
            Game.Data.CraftRecipeData data = AssetDatabase.LoadAssetAtPath<Game.Data.CraftRecipeData>(path);
            bool isNew = data == null;
            if (isNew)
            {
                data = ScriptableObject.CreateInstance<Game.Data.CraftRecipeData>();
            }

            data.recipeId = id;
            data.displayName = displayName;
            data.outputItem = output;
            data.outputCount = 1;
            data.goldCost = goldCost;
            data.requiredBlacksmithLevel = blacksmithLevel;
            data.ingredients = ingredients != null
                ? new List<Game.Data.CraftIngredient>(ingredients)
                : new List<Game.Data.CraftIngredient>();

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static Game.Data.VillageBuildingData CreateOrUpdateVillageBuilding(
            string path,
            string id,
            string displayName,
            Game.Data.VillageBuildingType type,
            int goldPerHour,
            int expPerHour,
            int goldPerLevel,
            int expPerLevel,
            float craftBonus)
        {
            Game.Data.VillageBuildingData data = AssetDatabase.LoadAssetAtPath<Game.Data.VillageBuildingData>(path);
            bool isNew = data == null;
            if (isNew)
            {
                data = ScriptableObject.CreateInstance<Game.Data.VillageBuildingData>();
            }

            data.buildingId = id;
            data.displayName = displayName;
            data.buildingType = type;
            data.maxLevel = 10;
            data.baseGoldPerHour = goldPerHour;
            data.baseExperiencePerHour = expPerHour;
            data.goldPerHourPerLevel = goldPerLevel;
            data.experiencePerHourPerLevel = expPerLevel;
            data.craftSpeedBonusPerLevel = craftBonus;
            data.baseUpgradeGoldCost = 50;
            data.upgradeGoldCostPerLevel = 40;
            data.requiredPlayerLevelPerBuildingLevel = 1;

            string buildingSpritePath = ART_PATH + "/Village/Buildings/" + displayName + ".png";
            Sprite buildingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(buildingSpritePath);
            if (buildingSprite != null)
            {
                data.icon = buildingSprite;
                data.plazaSprite = buildingSprite;
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static void EnsureBuildingPanelUI(string panelName, Game.Data.VillageBuildingType type, string title)
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find(panelName);
            if (existing != null && s_preserveHubLayout)
            {
                EnsurePanelSlider(existing.gameObject);
                Game.UI.BuildingPanelUI kept = existing.GetComponent<Game.UI.BuildingPanelUI>();
                if (kept != null)
                {
                    SerializedObject keptSO = new SerializedObject(kept);
                    keptSO.FindProperty("buildingType").enumValueIndex = (int)type;
                    keptSO.ApplyModifiedProperties();
                }

                Debug.Log($"[Setup] {panelName} kept (layout preserved).");
                return;
            }

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = new GameObject(panelName);
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(420f, 280f);
            UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
            cardBg.color = new Color(0.04f, 0.09f, 0.11f, 0.98f);

            GameObject titleObj = CreateUIText("BuildingTitle", card.transform, new Vector2(0f, 95f), title);
            titleObj.GetComponent<UnityEngine.UI.Text>().fontSize = 24;
            titleObj.GetComponent<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;
            titleObj.GetComponent<UnityEngine.UI.Text>().color = new Color(0.5f, 0.92f, 0.82f);
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 32f);

            GameObject close = CreateButton("CloseBuildingButton", card.transform, new Vector2(160f, 95f), new Vector2(72f, 32f), "Close");

            GameObject detail = CreateUIText("DetailText", card.transform, new Vector2(0f, 20f), "Lv.1");
            detail.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 80f);
            detail.GetComponent<UnityEngine.UI.Text>().fontSize = 18;
            detail.GetComponent<UnityEngine.UI.Text>().alignment = TextAnchor.MiddleCenter;

            GameObject status = CreateUIText("StatusText", card.transform, new Vector2(0f, -40f), "");
            status.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 28f);
            status.GetComponent<UnityEngine.UI.Text>().fontSize = 14;
            status.GetComponent<UnityEngine.UI.Text>().color = new Color(0.7f, 0.8f, 0.75f);

            GameObject upgrade = CreateButton("UpgradeButton", card.transform, new Vector2(0f, -95f), new Vector2(220f, 40f), "Upgrade");

            Game.UI.BuildingPanelUI panelUI = panel.AddComponent<Game.UI.BuildingPanelUI>();
            SerializedObject so = new SerializedObject(panelUI);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("buildingType").enumValueIndex = (int)type;
            so.FindProperty("titleText").objectReferenceValue = titleObj.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("detailText").objectReferenceValue = detail.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("statusText").objectReferenceValue = status.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("upgradeButton").objectReferenceValue = upgrade.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("closeButton").objectReferenceValue = close.GetComponent<UnityEngine.UI.Button>();
            so.ApplyModifiedProperties();

            EnsurePanelSlider(panel);
            panel.SetActive(false);
            Debug.Log($"[Setup] {panelName} created.");
        }

        private static void EnsureVillagePanelUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("VillagePanel");
            if (existing != null && s_preserveHubLayout)
            {
                EnsurePanelSlider(existing.gameObject);
                Debug.Log("[Setup] VillagePanel kept (layout preserved).");
                return;
            }

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = new GameObject("VillagePanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(520f, 440f);
            UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
            cardBg.color = new Color(0.04f, 0.09f, 0.11f, 0.98f);

            GameObject title = CreateUIText("VillageTitle", card.transform, new Vector2(0f, 185f), "VILLAGE");
            UnityEngine.UI.Text titleText = title.GetComponent<UnityEngine.UI.Text>();
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.5f, 0.92f, 0.82f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 32f);

            GameObject close = CreateButton("CloseVillageButton", card.transform, new Vector2(210f, 185f), new Vector2(72f, 32f), "Close");

            GameObject list = new GameObject("BuildingList");
            list.transform.SetParent(card.transform, false);
            RectTransform listRect = list.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.sizeDelta = new Vector2(460f, 280f);
            listRect.anchoredPosition = new Vector2(0f, 10f);

            UnityEngine.UI.VerticalLayoutGroup vlg = list.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            UnityEngine.UI.Text libraryText;
            UnityEngine.UI.Button upLib;
            CreateVillageBuildingRow(list.transform, "Library", out libraryText, out upLib);

            UnityEngine.UI.Text mineText;
            UnityEngine.UI.Button upMine;
            CreateVillageBuildingRow(list.transform, "Mine", out mineText, out upMine);

            UnityEngine.UI.Text smithText;
            UnityEngine.UI.Button upSmith;
            CreateVillageBuildingRow(list.transform, "Blacksmith", out smithText, out upSmith);

            GameObject status = CreateUIText("VillageStatus", card.transform, new Vector2(0f, -145f), "AFK passive");
            UnityEngine.UI.Text statusText = status.GetComponent<UnityEngine.UI.Text>();
            statusText.fontSize = 13;
            statusText.color = new Color(0.85f, 0.9f, 0.75f);
            statusText.alignment = TextAnchor.MiddleCenter;
            status.GetComponent<RectTransform>().sizeDelta = new Vector2(440f, 40f);

            GameObject openCraft = CreateButton("OpenCraftButton", card.transform, new Vector2(0f, -185f), new Vector2(200f, 36f), "Open Craft");

            Game.UI.VillagePanelUI villageUI = panel.AddComponent<Game.UI.VillagePanelUI>();
            SerializedObject so = new SerializedObject(villageUI);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("libraryText").objectReferenceValue = libraryText;
            so.FindProperty("mineText").objectReferenceValue = mineText;
            so.FindProperty("blacksmithText").objectReferenceValue = smithText;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("upgradeLibraryButton").objectReferenceValue = upLib;
            so.FindProperty("upgradeMineButton").objectReferenceValue = upMine;
            so.FindProperty("upgradeBlacksmithButton").objectReferenceValue = upSmith;
            so.FindProperty("openCraftButton").objectReferenceValue = openCraft.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("closeButton").objectReferenceValue = close.GetComponent<UnityEngine.UI.Button>();
            so.ApplyModifiedProperties();

            EnsurePanelSlider(panel);
            panel.SetActive(false);
            Debug.Log("[Setup] VillagePanelUI rebuilt (clean layout).");
        }

        private static void CreateVillageBuildingRow(
            Transform parent,
            string buildingName,
            out UnityEngine.UI.Text labelText,
            out UnityEngine.UI.Button upgradeButton)
        {
            GameObject row = new GameObject(buildingName + "Row");
            row.transform.SetParent(parent, false);

            UnityEngine.UI.LayoutElement rowLe = row.AddComponent<UnityEngine.UI.LayoutElement>();
            rowLe.minHeight = 64f;
            rowLe.preferredHeight = 64f;

            UnityEngine.UI.Image rowBg = row.AddComponent<UnityEngine.UI.Image>();
            rowBg.color = new Color(0.06f, 0.12f, 0.14f, 0.95f);

            UnityEngine.UI.HorizontalLayoutGroup h = row.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 12, 8, 8);
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            GameObject textObj = new GameObject(buildingName + "Text");
            textObj.transform.SetParent(row.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(280f, 48f);
            UnityEngine.UI.LayoutElement textLe = textObj.AddComponent<UnityEngine.UI.LayoutElement>();
            textLe.flexibleWidth = 1f;
            textLe.preferredWidth = 280f;
            textLe.minHeight = 48f;

            labelText = textObj.AddComponent<UnityEngine.UI.Text>();
            labelText.text = buildingName + "\nLv.1";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject btnObj = CreateButton("Upgrade" + buildingName, row.transform, Vector2.zero, new Vector2(110f, 34f), "Upgrade");
            UnityEngine.UI.LayoutElement btnLe = btnObj.AddComponent<UnityEngine.UI.LayoutElement>();
            btnLe.preferredWidth = 110f;
            btnLe.minWidth = 110f;
            btnLe.preferredHeight = 34f;
            upgradeButton = btnObj.GetComponent<UnityEngine.UI.Button>();
        }

        private static void EnsureCraftPanelUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("CraftPanel");
            if (existing != null && s_preserveHubLayout)
            {
                EnsurePanelSlider(existing.gameObject);
                Debug.Log("[Setup] CraftPanel kept (layout preserved).");
                return;
            }

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = new GameObject("CraftPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 460f);
            UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
            cardBg.color = new Color(0.04f, 0.09f, 0.11f, 0.98f);

            GameObject title = CreateUIText("CraftTitle", card.transform, new Vector2(0f, 195f), "BLACKSMITH");
            UnityEngine.UI.Text titleText = title.GetComponent<UnityEngine.UI.Text>();
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.5f, 0.92f, 0.82f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 32f);

            GameObject close = CreateButton("CloseCraftButton", card.transform, new Vector2(220f, 195f), new Vector2(72f, 32f), "Close");

            // Materials box
            GameObject matsBox = new GameObject("MaterialsBox");
            matsBox.transform.SetParent(card.transform, false);
            RectTransform matsBoxRect = matsBox.AddComponent<RectTransform>();
            matsBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            matsBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            matsBoxRect.sizeDelta = new Vector2(500f, 70f);
            matsBoxRect.anchoredPosition = new Vector2(0f, 140f);
            UnityEngine.UI.Image matsBg = matsBox.AddComponent<UnityEngine.UI.Image>();
            matsBg.color = new Color(0.06f, 0.12f, 0.14f, 0.95f);

            GameObject mats = CreateUIText("MaterialsText", matsBox.transform, Vector2.zero, "MATERIALS");
            RectTransform matsRect = mats.GetComponent<RectTransform>();
            matsRect.anchorMin = Vector2.zero;
            matsRect.anchorMax = Vector2.one;
            matsRect.offsetMin = new Vector2(12f, 6f);
            matsRect.offsetMax = new Vector2(-12f, -6f);
            UnityEngine.UI.Text matsText = mats.GetComponent<UnityEngine.UI.Text>();
            matsText.fontSize = 13;
            matsText.alignment = TextAnchor.UpperLeft;
            matsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            matsText.verticalOverflow = VerticalWrapMode.Truncate;

            // Recipes box
            GameObject recipesBox = new GameObject("RecipesBox");
            recipesBox.transform.SetParent(card.transform, false);
            RectTransform recipesBoxRect = recipesBox.AddComponent<RectTransform>();
            recipesBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            recipesBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            recipesBoxRect.sizeDelta = new Vector2(500f, 90f);
            recipesBoxRect.anchoredPosition = new Vector2(0f, 50f);
            UnityEngine.UI.Image recipesBg = recipesBox.AddComponent<UnityEngine.UI.Image>();
            recipesBg.color = new Color(0.06f, 0.12f, 0.14f, 0.95f);

            GameObject recipes = CreateUIText("RecipeListText", recipesBox.transform, Vector2.zero, "RECIPES");
            RectTransform recipesRect = recipes.GetComponent<RectTransform>();
            recipesRect.anchorMin = Vector2.zero;
            recipesRect.anchorMax = Vector2.one;
            recipesRect.offsetMin = new Vector2(12f, 6f);
            recipesRect.offsetMax = new Vector2(-12f, -6f);
            UnityEngine.UI.Text recipesText = recipes.GetComponent<UnityEngine.UI.Text>();
            recipesText.fontSize = 13;
            recipesText.alignment = TextAnchor.UpperLeft;
            recipesText.horizontalOverflow = HorizontalWrapMode.Wrap;
            recipesText.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject status = CreateUIText("CraftStatus", card.transform, new Vector2(0f, -15f), "");
            UnityEngine.UI.Text statusText = status.GetComponent<UnityEngine.UI.Text>();
            statusText.fontSize = 13;
            statusText.color = new Color(1f, 0.85f, 0.45f);
            statusText.alignment = TextAnchor.MiddleCenter;
            status.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 28f);

            // Action buttons — two clear rows
            GameObject craft1 = CreateButton("CraftFirstButton", card.transform, new Vector2(-130f, -60f), new Vector2(220f, 36f), "Recipe 1");
            GameObject craft2 = CreateButton("CraftSecondButton", card.transform, new Vector2(130f, -60f), new Vector2(220f, 36f), "Recipe 2");
            GameObject summon = CreateButton("CraftSummonButton", card.transform, new Vector2(0f, -105f), new Vector2(260f, 36f), "Summon Stone");
            GameObject disassemble = CreateButton("DisassembleButton", card.transform, new Vector2(-130f, -155f), new Vector2(220f, 36f), "Disassemble 1");
            GameObject merge = CreateButton("MergeButton", card.transform, new Vector2(130f, -155f), new Vector2(220f, 36f), "Merge 9→1");

            // Section labels under buttons
            GameObject actionsHint = CreateUIText("ActionsHint", card.transform, new Vector2(0f, -195f), "Craft  ·  Scrap  ·  Merge");
            UnityEngine.UI.Text hintText = actionsHint.GetComponent<UnityEngine.UI.Text>();
            hintText.fontSize = 12;
            hintText.color = new Color(0.55f, 0.65f, 0.62f);
            actionsHint.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 22f);

            Game.UI.CraftPanelUI craftUI = panel.AddComponent<Game.UI.CraftPanelUI>();
            SerializedObject so = new SerializedObject(craftUI);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            so.FindProperty("materialsText").objectReferenceValue = matsText;
            so.FindProperty("recipeListText").objectReferenceValue = recipesText;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("craftFirstButton").objectReferenceValue = craft1.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("craftSecondButton").objectReferenceValue = craft2.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("craftSummonButton").objectReferenceValue = summon.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("disassembleButton").objectReferenceValue = disassemble.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("mergeButton").objectReferenceValue = merge.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("closeButton").objectReferenceValue = close.GetComponent<UnityEngine.UI.Button>();
            so.ApplyModifiedProperties();

            EnsurePanelSlider(panel);
            panel.SetActive(false);
            Debug.Log("[Setup] CraftPanelUI rebuilt (clean layout).");
        }

        private static void EnsureEnhancePanelUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("EnhancePanel");
            if (existing != null && s_preserveHubLayout)
            {
                EnsurePanelSlider(existing.gameObject);
                Debug.Log("[Setup] EnhancePanel kept (layout preserved).");
                return;
            }

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = new GameObject("EnhancePanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(480f, 400f);
            UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
            cardBg.color = new Color(0.04f, 0.09f, 0.11f, 0.98f);

            GameObject title = CreateUIText("EnhanceTitle", card.transform, new Vector2(0f, 165f), "ENHANCE");
            title.GetComponent<UnityEngine.UI.Text>().fontSize = 24;
            title.GetComponent<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;
            title.GetComponent<UnityEngine.UI.Text>().color = new Color(0.5f, 0.92f, 0.82f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 32f);

            GameObject close = CreateButton("CloseEnhanceButton", card.transform, new Vector2(180f, 165f), new Vector2(72f, 32f), "Close");

            GameObject itemBox = new GameObject("ItemBox");
            itemBox.transform.SetParent(card.transform, false);
            RectTransform itemBoxRect = itemBox.AddComponent<RectTransform>();
            itemBoxRect.sizeDelta = new Vector2(420f, 70f);
            itemBoxRect.anchoredPosition = new Vector2(0f, 100f);
            UnityEngine.UI.Image itemBg = itemBox.AddComponent<UnityEngine.UI.Image>();
            itemBg.color = new Color(0.06f, 0.12f, 0.14f, 0.95f);

            GameObject item = CreateUIText("ItemText", itemBox.transform, Vector2.zero, "No item");
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = Vector2.zero;
            itemRect.anchorMax = Vector2.one;
            itemRect.offsetMin = new Vector2(12f, 6f);
            itemRect.offsetMax = new Vector2(-12f, -6f);
            UnityEngine.UI.Text itemText = item.GetComponent<UnityEngine.UI.Text>();
            itemText.fontSize = 14;
            itemText.alignment = TextAnchor.MiddleLeft;

            GameObject stars = CreateUIText("StarsText", card.transform, new Vector2(0f, 40f), "Stars");
            stars.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 28f);
            stars.GetComponent<UnityEngine.UI.Text>().fontSize = 15;
            stars.GetComponent<UnityEngine.UI.Text>().color = new Color(1f, 0.85f, 0.4f);

            GameObject enchants = CreateUIText("EnchantsText", card.transform, new Vector2(0f, -20f), "Enchants");
            enchants.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 80f);
            enchants.GetComponent<UnityEngine.UI.Text>().fontSize = 13;
            enchants.GetComponent<UnityEngine.UI.Text>().alignment = TextAnchor.UpperCenter;

            GameObject status = CreateUIText("EnhanceStatus", card.transform, new Vector2(0f, -80f), "");
            status.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 28f);
            status.GetComponent<UnityEngine.UI.Text>().fontSize = 13;
            status.GetComponent<UnityEngine.UI.Text>().color = new Color(1f, 0.85f, 0.45f);

            GameObject addStar = CreateButton("AddStarButton", card.transform, new Vector2(-130f, -130f), new Vector2(160f, 36f), "+1 ★");
            GameObject apply = CreateButton("ApplyEnchantButton", card.transform, new Vector2(130f, -130f), new Vector2(160f, 36f), "Apply Efsun");
            GameObject clear = CreateButton("ClearEnchantButton", card.transform, new Vector2(0f, -175f), new Vector2(200f, 36f), "Clear Last");

            Game.UI.EnhancePanelUI enhanceUI = panel.AddComponent<Game.UI.EnhancePanelUI>();
            SerializedObject so = new SerializedObject(enhanceUI);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = title.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("itemText").objectReferenceValue = itemText;
            so.FindProperty("starsText").objectReferenceValue = stars.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("enchantsText").objectReferenceValue = enchants.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("statusText").objectReferenceValue = status.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("addStarButton").objectReferenceValue = addStar.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("applyEnchantButton").objectReferenceValue = apply.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("clearEnchantButton").objectReferenceValue = clear.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("closeButton").objectReferenceValue = close.GetComponent<UnityEngine.UI.Button>();
            so.ApplyModifiedProperties();

            EnsurePanelSlider(panel);
            panel.SetActive(false);
            Debug.Log("[Setup] EnhancePanelUI rebuilt.");
        }

        private static void CreateSampleEnhanceData()
        {
            EnsureFolder(SCRIPTABLE_PATH + "/Enhancement");
            EnsureFolder(SCRIPTABLE_PATH + "/Enhancement/Enchants");

            Game.Data.EnchantData sharp = CreateOrUpdateEnchant(
                SCRIPTABLE_PATH + "/Enhancement/Enchants/Enchant_Sharp.asset",
                "enchant_sharp", "Sharp", Game.Data.EnchantCategory.Attack, Game.Data.ItemRarity.Rare, 4, 0, 80);
            Game.Data.EnchantData sturdy = CreateOrUpdateEnchant(
                SCRIPTABLE_PATH + "/Enhancement/Enchants/Enchant_Sturdy.asset",
                "enchant_sturdy", "Sturdy", Game.Data.EnchantCategory.Defense, Game.Data.ItemRarity.Rare, 0, 40, 80);
            Game.Data.EnchantData swift = CreateOrUpdateEnchant(
                SCRIPTABLE_PATH + "/Enhancement/Enchants/Enchant_Swift.asset",
                "enchant_swift", "Swift", Game.Data.EnchantCategory.Utility, Game.Data.ItemRarity.Epic, 2, 15, 120);
            Game.Data.EnchantData savage = CreateOrUpdateEnchant(
                SCRIPTABLE_PATH + "/Enhancement/Enchants/Enchant_Savage.asset",
                "enchant_savage", "Savage", Game.Data.EnchantCategory.Attack, Game.Data.ItemRarity.Epic, 8, 0, 150);
            Game.Data.EnchantData immortal = CreateOrUpdateEnchant(
                SCRIPTABLE_PATH + "/Enhancement/Enchants/Enchant_ImmortalEdge.asset",
                "enchant_immortal_edge", "Immortal Edge", Game.Data.EnchantCategory.Attack, Game.Data.ItemRarity.Legendary, 12, 20, 250);

            string catalogPath = SCRIPTABLE_PATH + "/Enhancement/EnhanceCatalog.asset";
            Game.Data.EnhanceCatalog catalog = AssetDatabase.LoadAssetAtPath<Game.Data.EnhanceCatalog>(catalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<Game.Data.EnhanceCatalog>();
            }

            catalog.enchants = new List<Game.Data.EnchantData> { sharp, sturdy, swift, savage, immortal };
            catalog.starGoldCosts = new[] { 30, 60, 120, 250, 500 };
            catalog.starScrapCosts = new[] { 2, 4, 6, 10, 16 };
            catalog.scrapMaterialId = "mat_scrap";
            catalog.duplicateRequiredFromStar = 3;

            if (isNew)
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            else
            {
                EditorUtility.SetDirty(catalog);
            }

            Debug.Log("[Setup] Enhance catalog created.");
        }

        private static Game.Data.EnchantData CreateOrUpdateEnchant(
            string path,
            string id,
            string displayName,
            Game.Data.EnchantCategory category,
            Game.Data.ItemRarity requiredRarity,
            int attack,
            int health,
            int clearCost)
        {
            Game.Data.EnchantData data = AssetDatabase.LoadAssetAtPath<Game.Data.EnchantData>(path);
            bool isNew = data == null;
            if (isNew)
            {
                data = ScriptableObject.CreateInstance<Game.Data.EnchantData>();
            }

            data.enchantId = id;
            data.displayName = displayName;
            data.category = category;
            data.requiredRarity = requiredRarity;
            data.attackBonus = attack;
            data.healthBonus = health;
            data.clearGoldCost = clearCost;

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static void CreateSampleBuildData()
        {
            EnsureFolder(SCRIPTABLE_PATH + "/Build");
            EnsureFolder(SCRIPTABLE_PATH + "/Build/Runes");
            EnsureFolder(SCRIPTABLE_PATH + "/Build/Skills");

            Game.Data.RuneNodeData ironSkin = CreateOrUpdateRune(
                SCRIPTABLE_PATH + "/Build/Runes/Rune_IronSkin.asset",
                "rune_iron_skin", "Iron Skin", Game.Data.RuneBranch.Defense, 1, 5, 0, 15, 0f);
            Game.Data.RuneNodeData bulwark = CreateOrUpdateRune(
                SCRIPTABLE_PATH + "/Build/Runes/Rune_Bulwark.asset",
                "rune_bulwark", "Bulwark", Game.Data.RuneBranch.Defense, 5, 5, 0, 25, 0f);
            Game.Data.RuneNodeData fortitude = CreateOrUpdateRune(
                SCRIPTABLE_PATH + "/Build/Runes/Rune_Fortitude.asset",
                "rune_fortitude", "Fortitude", Game.Data.RuneBranch.Defense, 10, 5, 0, 40, 0f);

            Game.Data.RuneNodeData strike = CreateOrUpdateRune(
                SCRIPTABLE_PATH + "/Build/Runes/Rune_Strike.asset",
                "rune_strike", "Strike", Game.Data.RuneBranch.Attack, 1, 5, 3, 0, 0f);
            Game.Data.RuneNodeData fury = CreateOrUpdateRune(
                SCRIPTABLE_PATH + "/Build/Runes/Rune_Fury.asset",
                "rune_fury", "Fury", Game.Data.RuneBranch.Attack, 5, 5, 5, 0, 0f);
            Game.Data.RuneNodeData slaughter = CreateOrUpdateRune(
                SCRIPTABLE_PATH + "/Build/Runes/Rune_Slaughter.asset",
                "rune_slaughter", "Slaughter", Game.Data.RuneBranch.Attack, 10, 5, 8, 0, 0f);

            Game.Data.RuneNodeData swift = CreateOrUpdateRune(
                SCRIPTABLE_PATH + "/Build/Runes/Rune_SwiftCast.asset",
                "rune_swift_cast", "Swift Cast", Game.Data.RuneBranch.Utility, 1, 5, 0, 0, 0.02f);
            Game.Data.RuneNodeData tempo = CreateOrUpdateRune(
                SCRIPTABLE_PATH + "/Build/Runes/Rune_Tempo.asset",
                "rune_tempo", "Tempo", Game.Data.RuneBranch.Utility, 5, 5, 1, 5, 0.03f);
            Game.Data.RuneNodeData flow = CreateOrUpdateRune(
                SCRIPTABLE_PATH + "/Build/Runes/Rune_Flow.asset",
                "rune_flow", "Flow", Game.Data.RuneBranch.Utility, 10, 5, 2, 10, 0.04f);

            string runeCatalogPath = SCRIPTABLE_PATH + "/Build/RuneCatalog.asset";
            Game.Data.RuneCatalog runeCatalog = AssetDatabase.LoadAssetAtPath<Game.Data.RuneCatalog>(runeCatalogPath);
            bool runeNew = runeCatalog == null;
            if (runeNew)
            {
                runeCatalog = ScriptableObject.CreateInstance<Game.Data.RuneCatalog>();
            }

            runeCatalog.nodes = new List<Game.Data.RuneNodeData>
            {
                ironSkin, bulwark, fortitude, strike, fury, slaughter, swift, tempo, flow
            };

            if (runeNew)
            {
                AssetDatabase.CreateAsset(runeCatalog, runeCatalogPath);
            }
            else
            {
                EditorUtility.SetDirty(runeCatalog);
            }

            Game.Data.SkillData powerStrike = CreateOrUpdateSkill(
                SCRIPTABLE_PATH + "/Build/Skills/Skill_PowerStrike.asset",
                "skill_power_strike", "Power Strike", Game.Data.SkillKind.Active, 1, 6f, 1.8f, 0, 0);
            Game.Data.SkillData cleave = CreateOrUpdateSkill(
                SCRIPTABLE_PATH + "/Build/Skills/Skill_Cleave.asset",
                "skill_cleave", "Cleave", Game.Data.SkillKind.Active, 5, 8f, 2.2f, 0, 0);
            Game.Data.SkillData execute = CreateOrUpdateSkill(
                SCRIPTABLE_PATH + "/Build/Skills/Skill_Execute.asset",
                "skill_execute", "Execute", Game.Data.SkillKind.Active, 10, 10f, 2.8f, 0, 0);
            Game.Data.SkillData guardAura = CreateOrUpdateSkill(
                SCRIPTABLE_PATH + "/Build/Skills/Skill_GuardAura.asset",
                "skill_guard_aura", "Guard Aura", Game.Data.SkillKind.Passive, 10, 0f, 1f, 0, 60);
            Game.Data.SkillData warCry = CreateOrUpdateSkill(
                SCRIPTABLE_PATH + "/Build/Skills/Skill_WarCry.asset",
                "skill_war_cry", "War Cry", Game.Data.SkillKind.Passive, 5, 0f, 1f, 8, 20);

            string skillCatalogPath = SCRIPTABLE_PATH + "/Build/SkillCatalog.asset";
            Game.Data.SkillCatalog skillCatalog = AssetDatabase.LoadAssetAtPath<Game.Data.SkillCatalog>(skillCatalogPath);
            bool skillNew = skillCatalog == null;
            if (skillNew)
            {
                skillCatalog = ScriptableObject.CreateInstance<Game.Data.SkillCatalog>();
            }

            skillCatalog.skills = new List<Game.Data.SkillData>
            {
                powerStrike, cleave, execute, guardAura, warCry
            };
            skillCatalog.activeSlot1UnlockLevel = 1;
            skillCatalog.activeSlot2UnlockLevel = 5;
            skillCatalog.passiveSlotUnlockLevel = 10;

            if (skillNew)
            {
                AssetDatabase.CreateAsset(skillCatalog, skillCatalogPath);
            }
            else
            {
                EditorUtility.SetDirty(skillCatalog);
            }

            Debug.Log("[Setup] Build catalogs created (runes + skills).");
        }

        private static Game.Data.RuneNodeData CreateOrUpdateRune(
            string path,
            string id,
            string displayName,
            Game.Data.RuneBranch branch,
            int requiredLevel,
            int maxRank,
            int atkPerRank,
            int hpPerRank,
            float cdrPerRank)
        {
            Game.Data.RuneNodeData data = AssetDatabase.LoadAssetAtPath<Game.Data.RuneNodeData>(path);
            bool isNew = data == null;
            if (isNew)
            {
                data = ScriptableObject.CreateInstance<Game.Data.RuneNodeData>();
            }

            data.runeId = id;
            data.displayName = displayName;
            data.branch = branch;
            data.requiredLevel = requiredLevel;
            data.maxRank = maxRank;
            data.attackBonusPerRank = atkPerRank;
            data.healthBonusPerRank = hpPerRank;
            data.cooldownReducePercentPerRank = cdrPerRank;

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static Game.Data.SkillData CreateOrUpdateSkill(
            string path,
            string id,
            string displayName,
            Game.Data.SkillKind kind,
            int requiredLevel,
            float cooldown,
            float damageMultiplier,
            int attackBonus,
            int healthBonus)
        {
            Game.Data.SkillData data = AssetDatabase.LoadAssetAtPath<Game.Data.SkillData>(path);
            bool isNew = data == null;
            if (isNew)
            {
                data = ScriptableObject.CreateInstance<Game.Data.SkillData>();
            }

            data.skillId = id;
            data.displayName = displayName;
            data.kind = kind;
            data.requiredLevel = requiredLevel;
            data.cooldown = cooldown;
            data.damageMultiplier = damageMultiplier;
            data.attackBonus = attackBonus;
            data.healthBonus = healthBonus;

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static void EnsureBuildSystems()
        {
            CreateSampleBuildData();

            GameObject systems = GameObject.Find("Systems");
            Transform parent = systems != null ? systems.transform : null;
            GameObject buildObj = FindOrCreate("BuildController", parent);

            Game.Build.BuildController controller = buildObj.GetComponent<Game.Build.BuildController>();
            if (controller == null)
            {
                controller = buildObj.AddComponent<Game.Build.BuildController>();
            }

            Game.Build.RuneService runeService = buildObj.GetComponent<Game.Build.RuneService>();
            if (runeService == null)
            {
                runeService = buildObj.AddComponent<Game.Build.RuneService>();
            }

            Game.Build.SkillService skillService = buildObj.GetComponent<Game.Build.SkillService>();
            if (skillService == null)
            {
                skillService = buildObj.AddComponent<Game.Build.SkillService>();
            }

            Game.Data.RuneCatalog runeCatalog =
                AssetDatabase.LoadAssetAtPath<Game.Data.RuneCatalog>(SCRIPTABLE_PATH + "/Build/RuneCatalog.asset");
            Game.Data.SkillCatalog skillCatalog =
                AssetDatabase.LoadAssetAtPath<Game.Data.SkillCatalog>(SCRIPTABLE_PATH + "/Build/SkillCatalog.asset");

            SerializedObject controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("runeService").objectReferenceValue = runeService;
            controllerSO.FindProperty("skillService").objectReferenceValue = skillService;
            controllerSO.FindProperty("runeCatalog").objectReferenceValue = runeCatalog;
            controllerSO.FindProperty("skillCatalog").objectReferenceValue = skillCatalog;
            controllerSO.ApplyModifiedProperties();

            SerializedObject runeSO = new SerializedObject(runeService);
            runeSO.FindProperty("runeCatalog").objectReferenceValue = runeCatalog;
            GameObject rewardObj = GameObject.Find("RewardController");
            if (rewardObj != null)
            {
                runeSO.FindProperty("rewardService").objectReferenceValue = rewardObj.GetComponent<Game.Rewards.RewardService>();
            }

            runeSO.ApplyModifiedProperties();

            SerializedObject skillSO = new SerializedObject(skillService);
            skillSO.FindProperty("skillCatalog").objectReferenceValue = skillCatalog;
            if (rewardObj != null)
            {
                skillSO.FindProperty("rewardService").objectReferenceValue = rewardObj.GetComponent<Game.Rewards.RewardService>();
            }

            skillSO.ApplyModifiedProperties();

            Debug.Log("[Setup] BuildController + Rune/Skill services ensured.");
        }

        private static void EnsureBuildPanelUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("BuildPanel");
            if (existing != null && s_preserveHubLayout)
            {
                EnsurePanelSlider(existing.gameObject);
                BindBuildPanelServices();
                Debug.Log("[Setup] BuildPanel kept (layout preserved).");
                return;
            }

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = new GameObject("BuildPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 460f);
            UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
            cardBg.color = new Color(0.04f, 0.09f, 0.11f, 0.98f);

            GameObject title = CreateUIText("BuildTitle", card.transform, new Vector2(0f, 195f), "BUILD");
            title.GetComponent<UnityEngine.UI.Text>().fontSize = 24;
            title.GetComponent<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;
            title.GetComponent<UnityEngine.UI.Text>().color = new Color(0.5f, 0.92f, 0.82f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 32f);

            GameObject close = CreateButton("CloseBuildButton", card.transform, new Vector2(220f, 195f), new Vector2(72f, 32f), "Close");
            GameObject runesTab = CreateButton("RunesTabButton", card.transform, new Vector2(-90f, 155f), new Vector2(140f, 32f), "Runes");
            GameObject skillsTab = CreateButton("SkillsTabButton", card.transform, new Vector2(90f, 155f), new Vector2(140f, 32f), "Skills");

            GameObject summary = CreateUIText("BuildSummary", card.transform, new Vector2(0f, 120f), "Points");
            summary.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 36f);
            summary.GetComponent<UnityEngine.UI.Text>().fontSize = 13;
            summary.GetComponent<UnityEngine.UI.Text>().alignment = TextAnchor.MiddleCenter;

            GameObject runesPage = new GameObject("RunesPage");
            runesPage.transform.SetParent(card.transform, false);
            RectTransform runesPageRect = runesPage.AddComponent<RectTransform>();
            runesPageRect.anchorMin = Vector2.zero;
            runesPageRect.anchorMax = Vector2.one;
            runesPageRect.offsetMin = new Vector2(20f, 70f);
            runesPageRect.offsetMax = new Vector2(-20f, -160f);

            GameObject runesBody = CreateUIText("RunesBody", runesPage.transform, Vector2.zero, "Runes");
            RectTransform runesBodyRect = runesBody.GetComponent<RectTransform>();
            runesBodyRect.anchorMin = Vector2.zero;
            runesBodyRect.anchorMax = Vector2.one;
            runesBodyRect.offsetMin = Vector2.zero;
            runesBodyRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Text runesBodyText = runesBody.GetComponent<UnityEngine.UI.Text>();
            runesBodyText.fontSize = 13;
            runesBodyText.alignment = TextAnchor.UpperLeft;

            GameObject skillsPage = new GameObject("SkillsPage");
            skillsPage.transform.SetParent(card.transform, false);
            RectTransform skillsPageRect = skillsPage.AddComponent<RectTransform>();
            skillsPageRect.anchorMin = Vector2.zero;
            skillsPageRect.anchorMax = Vector2.one;
            skillsPageRect.offsetMin = new Vector2(20f, 70f);
            skillsPageRect.offsetMax = new Vector2(-20f, -160f);
            skillsPage.SetActive(false);

            GameObject skillsBody = CreateUIText("SkillsBody", skillsPage.transform, Vector2.zero, "Skills");
            RectTransform skillsBodyRect = skillsBody.GetComponent<RectTransform>();
            skillsBodyRect.anchorMin = Vector2.zero;
            skillsBodyRect.anchorMax = Vector2.one;
            skillsBodyRect.offsetMin = Vector2.zero;
            skillsBodyRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Text skillsBodyText = skillsBody.GetComponent<UnityEngine.UI.Text>();
            skillsBodyText.fontSize = 13;
            skillsBodyText.alignment = TextAnchor.UpperLeft;

            GameObject status = CreateUIText("BuildStatus", card.transform, new Vector2(0f, -145f), "");
            status.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 24f);
            status.GetComponent<UnityEngine.UI.Text>().fontSize = 13;
            status.GetComponent<UnityEngine.UI.Text>().color = new Color(1f, 0.85f, 0.45f);

            GameObject prevRune = CreateButton("PrevRuneButton", card.transform, new Vector2(-210f, -180f), new Vector2(70f, 32f), "<");
            GameObject nextRune = CreateButton("NextRuneButton", card.transform, new Vector2(-130f, -180f), new Vector2(70f, 32f), ">");
            GameObject allocate = CreateButton("AllocateButton", card.transform, new Vector2(-40f, -180f), new Vector2(90f, 32f), "+ Rank");
            GameObject refund = CreateButton("RefundButton", card.transform, new Vector2(60f, -180f), new Vector2(90f, 32f), "- Rank");

            GameObject prevSkill = CreateButton("PrevSkillButton", card.transform, new Vector2(-210f, -218f), new Vector2(70f, 32f), "<");
            GameObject nextSkill = CreateButton("NextSkillButton", card.transform, new Vector2(-130f, -218f), new Vector2(70f, 32f), ">");
            GameObject equipA1 = CreateButton("EquipActive1Button", card.transform, new Vector2(-20f, -218f), new Vector2(100f, 32f), "Act1");
            GameObject equipA2 = CreateButton("EquipActive2Button", card.transform, new Vector2(90f, -218f), new Vector2(100f, 32f), "Act2");
            GameObject equipPas = CreateButton("EquipPassiveButton", card.transform, new Vector2(200f, -218f), new Vector2(100f, 32f), "Pas");
            GameObject unequip = CreateButton("UnequipSkillButton", card.transform, new Vector2(200f, -180f), new Vector2(100f, 32f), "Unequip");

            Game.UI.BuildPanelUI buildUI = panel.AddComponent<Game.UI.BuildPanelUI>();
            SerializedObject so = new SerializedObject(buildUI);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("runesTabButton").objectReferenceValue = runesTab.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("skillsTabButton").objectReferenceValue = skillsTab.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("runesPage").objectReferenceValue = runesPage;
            so.FindProperty("skillsPage").objectReferenceValue = skillsPage;
            so.FindProperty("titleText").objectReferenceValue = title.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("summaryText").objectReferenceValue = summary.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("statusText").objectReferenceValue = status.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("closeButton").objectReferenceValue = close.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("runesBodyText").objectReferenceValue = runesBodyText;
            so.FindProperty("allocateButton").objectReferenceValue = allocate.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("refundButton").objectReferenceValue = refund.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("nextRuneButton").objectReferenceValue = nextRune.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("prevRuneButton").objectReferenceValue = prevRune.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("skillsBodyText").objectReferenceValue = skillsBodyText;
            so.FindProperty("equipActive1Button").objectReferenceValue = equipA1.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("equipActive2Button").objectReferenceValue = equipA2.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("equipPassiveButton").objectReferenceValue = equipPas.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("unequipButton").objectReferenceValue = unequip.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("nextSkillButton").objectReferenceValue = nextSkill.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("prevSkillButton").objectReferenceValue = prevSkill.GetComponent<UnityEngine.UI.Button>();
            so.ApplyModifiedProperties();

            EnsurePanelSlider(panel);
            panel.SetActive(false);
            BindBuildPanelServices();
            Debug.Log("[Setup] BuildPanelUI rebuilt.");
        }

        private static void BindBuildPanelServices()
        {
            Game.UI.BuildPanelUI buildPanel =
                UnityEngine.Object.FindFirstObjectByType<Game.UI.BuildPanelUI>(FindObjectsInactive.Include);
            Game.Build.RuneService runeService =
                UnityEngine.Object.FindFirstObjectByType<Game.Build.RuneService>();
            Game.Build.SkillService skillService =
                UnityEngine.Object.FindFirstObjectByType<Game.Build.SkillService>();

            if (buildPanel == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(buildPanel);
            so.FindProperty("runeService").objectReferenceValue = runeService;
            so.FindProperty("skillService").objectReferenceValue = skillService;
            so.ApplyModifiedProperties();

            Game.Hub.HubController hubController =
                UnityEngine.Object.FindFirstObjectByType<Game.Hub.HubController>(FindObjectsInactive.Include);
            if (hubController != null)
            {
                SerializedObject hubSO = new SerializedObject(hubController);
                SerializedProperty buildProp = hubSO.FindProperty("buildPanelUI");
                if (buildProp != null)
                {
                    buildProp.objectReferenceValue = buildPanel;
                    hubSO.ApplyModifiedProperties();
                }
            }
        }

        private static void WireInventoryBuildButton()
        {
            Game.UI.InventoryPanelUI inventoryPanel =
                UnityEngine.Object.FindFirstObjectByType<Game.UI.InventoryPanelUI>(FindObjectsInactive.Include);
            if (inventoryPanel == null)
            {
                return;
            }

            Transform card = inventoryPanel.transform.Find("Card");
            if (card == null)
            {
                card = inventoryPanel.transform;
            }

            Transform existingBtn = card.Find("BuildButton");
            GameObject buildBtnObj;
            if (existingBtn != null)
            {
                buildBtnObj = existingBtn.gameObject;
            }
            else
            {
                buildBtnObj = CreateButton("BuildButton", card, new Vector2(-300f, 210f), new Vector2(130f, 42f), "BUILD");
            }

            RectTransform buildRect = buildBtnObj.GetComponent<RectTransform>();
            if (buildRect != null)
            {
                buildRect.anchoredPosition = new Vector2(-300f, 210f);
                buildRect.sizeDelta = new Vector2(130f, 42f);
                buildRect.localScale = Vector3.one;
            }

            UnityEngine.UI.Image buildImg = buildBtnObj.GetComponent<UnityEngine.UI.Image>();
            if (buildImg != null)
            {
                buildImg.color = new Color(0.12f, 0.55f, 0.48f, 1f);
            }

            Transform labelTf = buildBtnObj.transform.Find("Text");
            if (labelTf != null)
            {
                labelTf.localScale = Vector3.one;
                UnityEngine.UI.Text label = labelTf.GetComponent<UnityEngine.UI.Text>();
                if (label != null)
                {
                    label.text = "BUILD";
                    label.fontSize = 18;
                    label.fontStyle = FontStyle.Bold;
                    label.color = Color.white;
                }
            }

            buildBtnObj.transform.SetAsLastSibling();
            buildBtnObj.SetActive(true);

            Game.UI.BuildPanelUI buildPanel =
                UnityEngine.Object.FindFirstObjectByType<Game.UI.BuildPanelUI>(FindObjectsInactive.Include);

            SerializedObject invSO = new SerializedObject(inventoryPanel);
            invSO.FindProperty("buildButton").objectReferenceValue = buildBtnObj.GetComponent<UnityEngine.UI.Button>();
            invSO.FindProperty("buildPanelUI").objectReferenceValue = buildPanel;
            invSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(inventoryPanel);
            Debug.Log("[Setup] Inventory Build button wired (Village card top-left).");
        }

        private static void CreateSampleRosterData()
        {
            EnsureFolder(SCRIPTABLE_PATH + "/Roster");

            Game.Data.HeroClassData fighter = CreateOrUpdateHeroClass(
                SCRIPTABLE_PATH + "/Roster/Class_Fighter.asset",
                "fighter", "Fighter", 28, 200, 1, 0,
                new[] { "skill_power_strike", "skill_cleave", "skill_war_cry" });
            Game.Data.HeroClassData archer = CreateOrUpdateHeroClass(
                SCRIPTABLE_PATH + "/Roster/Class_Archer.asset",
                "archer", "Archer", 30, 170, 3, 100,
                new[] { "skill_power_strike", "skill_execute" });
            Game.Data.HeroClassData mage = CreateOrUpdateHeroClass(
                SCRIPTABLE_PATH + "/Roster/Class_Mage.asset",
                "mage", "Mage", 26, 160, 5, 200,
                new[] { "skill_war_cry", "skill_guard_aura", "skill_execute" });
            Game.Data.HeroClassData assassin = CreateOrUpdateHeroClass(
                SCRIPTABLE_PATH + "/Roster/Class_Assassin.asset",
                "assassin", "Assassin", 34, 150, 7, 300,
                new[] { "skill_execute", "skill_power_strike" });
            Game.Data.HeroClassData tank = CreateOrUpdateHeroClass(
                SCRIPTABLE_PATH + "/Roster/Class_Tank.asset",
                "tank", "Tank", 22, 260, 4, 150,
                new[] { "skill_guard_aura", "skill_cleave", "skill_war_cry" });
            Game.Data.HeroClassData priest = CreateOrUpdateHeroClass(
                SCRIPTABLE_PATH + "/Roster/Class_Priest.asset",
                "priest", "Priest", 20, 210, 6, 250,
                new[] { "skill_guard_aura", "skill_war_cry" });

            string catalogPath = SCRIPTABLE_PATH + "/Roster/HeroClassCatalog.asset";
            Game.Data.HeroClassCatalog catalog = AssetDatabase.LoadAssetAtPath<Game.Data.HeroClassCatalog>(catalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<Game.Data.HeroClassCatalog>();
            }

            catalog.defaultClassId = "fighter";
            catalog.unlockAllForDemo = true;
            catalog.maxPartySlots = 3;
            catalog.partySlotGoldCosts = new int[] { 0, 1000, 5000 };
            catalog.classes = new List<Game.Data.HeroClassData>
            {
                fighter, archer, mage, assassin, tank, priest
            };

            if (isNew)
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            else
            {
                EditorUtility.SetDirty(catalog);
            }

            Debug.Log("[Setup] Hero class catalog created (6 classes).");
        }

        private static Game.Data.HeroClassData CreateOrUpdateHeroClass(
            string path,
            string classId,
            string displayName,
            int baseAtk,
            int baseHp,
            int unlockLevel,
            int unlockGold,
            string[] allowedSkills)
        {
            Game.Data.HeroClassData data = AssetDatabase.LoadAssetAtPath<Game.Data.HeroClassData>(path);
            bool isNew = data == null;
            if (isNew)
            {
                data = ScriptableObject.CreateInstance<Game.Data.HeroClassData>();
            }

            data.classId = classId;
            data.displayName = displayName;
            data.baseAttackDamage = baseAtk;
            data.baseMaxHealth = baseHp;
            data.unlockLevel = unlockLevel;
            data.unlockGoldCost = unlockGold;
            data.allowedSkillIds = allowedSkills != null
                ? new List<string>(allowedSkills)
                : new List<string>();
            data.heroPrefab = null;

            if (isNew)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static void CreateSampleWeeklyDungeonData()
        {
            EnsureFolder(SCRIPTABLE_PATH + "/Dungeon");
            EnsureFolder(SCRIPTABLE_PATH + "/Stages");

            Game.Data.EnemyData boss1 =
                AssetDatabase.LoadAssetAtPath<Game.Data.EnemyData>(SCRIPTABLE_PATH + "/Enemies/Enemy_Boss1.asset");
            Game.Data.EnemyData dogBoss =
                AssetDatabase.LoadAssetAtPath<Game.Data.EnemyData>(SCRIPTABLE_PATH + "/Enemies/Enemy_DogBoss.asset");
            Game.Data.EnemyData dog =
                AssetDatabase.LoadAssetAtPath<Game.Data.EnemyData>(SCRIPTABLE_PATH + "/Enemies/Enemy_Dog.asset");
            Game.Data.ChestData dungeonChest =
                AssetDatabase.LoadAssetAtPath<Game.Data.ChestData>(SCRIPTABLE_PATH + "/Chests/Chest_DungeonBoss.asset");

            string stagePath = SCRIPTABLE_PATH + "/Stages/Stage_WeeklyDungeon.asset";
            Game.Data.StageData stage = AssetDatabase.LoadAssetAtPath<Game.Data.StageData>(stagePath);
            bool stageNew = stage == null;
            if (stageNew)
            {
                stage = ScriptableObject.CreateInstance<Game.Data.StageData>();
            }

            stage.stageId = "stage_weekly_dungeon";
            stage.displayName = "Weekly Dungeon";
            stage.stageIndex = 900;
            stage.travelDuration = 2f;
            stage.delayAfterTravel = 0.5f;
            stage.completionGoldBonus = 120;
            stage.completionExperienceBonus = 180;
            Game.Data.EnemyData seedBoss = boss1 != null ? boss1 : dogBoss;
            stage.waves = new List<Game.Data.WaveData>
            {
                new Game.Data.WaveData
                {
                    waveName = "Weekly Boss",
                    enemies = seedBoss != null
                        ? new List<Game.Data.EnemyData> { seedBoss }
                        : new List<Game.Data.EnemyData>(),
                    delayBeforeWave = 0.5f,
                    delayBetweenEnemies = 0.5f,
                    simultaneousSpawnCount = 1
                }
            };

            if (stageNew)
            {
                AssetDatabase.CreateAsset(stage, stagePath);
            }
            else
            {
                EditorUtility.SetDirty(stage);
            }

            string catalogPath = SCRIPTABLE_PATH + "/Dungeon/WeeklyDungeonCatalog.asset";
            Game.Data.WeeklyDungeonCatalog catalog =
                AssetDatabase.LoadAssetAtPath<Game.Data.WeeklyDungeonCatalog>(catalogPath);
            bool catalogNew = catalog == null;
            if (catalogNew)
            {
                catalog = ScriptableObject.CreateInstance<Game.Data.WeeklyDungeonCatalog>();
            }

            catalog.maxEntriesPerWeek = 10;
            catalog.courageStoneCost = 1;
            catalog.courageStoneMaterialId = "mat_courage_stone";
            catalog.dungeonStage = stage;
            catalog.displayName = "Weekly Dungeon";
            catalog.dungeonChest = dungeonChest;
            catalog.layerBossCourageDropChance = 0.25f;
            catalog.weeklyClaimStoneAmount = 1;
            catalog.bossRotation = new List<Game.Data.EnemyData>();
            if (boss1 != null)
            {
                catalog.bossRotation.Add(boss1);
            }

            if (dogBoss != null)
            {
                catalog.bossRotation.Add(dogBoss);
            }

            if (dog != null)
            {
                catalog.bossRotation.Add(dog);
            }

            if (catalogNew)
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            else
            {
                EditorUtility.SetDirty(catalog);
            }

            Debug.Log("[Setup] Weekly dungeon catalog created.");
        }

        private static void EnsureRosterSystems()
        {
            CreateSampleRosterData();

            GameObject systems = GameObject.Find("Systems");
            Transform parent = systems != null ? systems.transform : null;
            GameObject rosterObj = FindOrCreate("RosterController", parent);

            Game.Roster.RosterService rosterService = rosterObj.GetComponent<Game.Roster.RosterService>();
            if (rosterService == null)
            {
                rosterService = rosterObj.AddComponent<Game.Roster.RosterService>();
            }

            Game.Data.HeroClassCatalog catalog =
                AssetDatabase.LoadAssetAtPath<Game.Data.HeroClassCatalog>(SCRIPTABLE_PATH + "/Roster/HeroClassCatalog.asset");

            SerializedObject so = new SerializedObject(rosterService);
            so.FindProperty("classCatalog").objectReferenceValue = catalog;
            GameObject rewardObj = GameObject.Find("RewardController");
            if (rewardObj != null)
            {
                so.FindProperty("rewardService").objectReferenceValue = rewardObj.GetComponent<Game.Rewards.RewardService>();
            }

            so.ApplyModifiedProperties();
            Debug.Log("[Setup] RosterService ensured.");
        }

        private static void EnsureWeeklyDungeonSystems()
        {
            CreateSampleWeeklyDungeonData();

            GameObject systems = GameObject.Find("Systems");
            Transform parent = systems != null ? systems.transform : null;
            GameObject weeklyObj = FindOrCreate("WeeklyDungeonController", parent);

            Game.Dungeon.WeeklyDungeonService weeklyService =
                weeklyObj.GetComponent<Game.Dungeon.WeeklyDungeonService>();
            if (weeklyService == null)
            {
                weeklyService = weeklyObj.AddComponent<Game.Dungeon.WeeklyDungeonService>();
            }

            Game.Data.WeeklyDungeonCatalog catalog =
                AssetDatabase.LoadAssetAtPath<Game.Data.WeeklyDungeonCatalog>(
                    SCRIPTABLE_PATH + "/Dungeon/WeeklyDungeonCatalog.asset");

            SerializedObject so = new SerializedObject(weeklyService);
            so.FindProperty("dungeonCatalog").objectReferenceValue = catalog;
            GameObject craftObj = GameObject.Find("CraftController");
            if (craftObj != null)
            {
                so.FindProperty("craftService").objectReferenceValue =
                    craftObj.GetComponent<Game.Crafting.CraftService>();
            }

            so.ApplyModifiedProperties();
            Debug.Log("[Setup] WeeklyDungeonService ensured.");
        }

        private static void EnsureRosterPanelUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("RosterPanel");
            if (existing != null && s_preserveHubLayout)
            {
                EnsurePanelSlider(existing.gameObject);
                BindRosterPanelServices();
                Debug.Log("[Setup] RosterPanel kept (layout preserved).");
                return;
            }

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = new GameObject("RosterPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(520f, 360f);
            UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
            cardBg.color = new Color(0.07f, 0.06f, 0.12f, 0.98f);

            GameObject title = CreateUIText("RosterTitle", card.transform, new Vector2(0f, 140f), "CLASS");
            title.GetComponent<UnityEngine.UI.Text>().fontSize = 24;
            title.GetComponent<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;
            title.GetComponent<UnityEngine.UI.Text>().color = new Color(0.78f, 0.7f, 1f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 32f);

            GameObject close = CreateButton("CloseRosterButton", card.transform, new Vector2(200f, 140f), new Vector2(72f, 32f), "Close");
            GameObject body = CreateUIText("RosterBody", card.transform, new Vector2(0f, 10f), "Classes");
            body.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 180f);
            body.GetComponent<UnityEngine.UI.Text>().fontSize = 14;
            body.GetComponent<UnityEngine.UI.Text>().alignment = TextAnchor.UpperLeft;

            GameObject status = CreateUIText("RosterStatus", card.transform, new Vector2(0f, -100f), "");
            status.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 24f);
            status.GetComponent<UnityEngine.UI.Text>().fontSize = 13;
            status.GetComponent<UnityEngine.UI.Text>().color = new Color(1f, 0.85f, 0.45f);

            GameObject prev = CreateButton("PrevClassButton", card.transform, new Vector2(-160f, -140f), new Vector2(80f, 34f), "<");
            GameObject next = CreateButton("NextClassButton", card.transform, new Vector2(-70f, -140f), new Vector2(80f, 34f), ">");
            GameObject select = CreateButton("SelectClassButton", card.transform, new Vector2(40f, -140f), new Vector2(100f, 34f), "Select");
            GameObject unlock = CreateButton("UnlockClassButton", card.transform, new Vector2(160f, -140f), new Vector2(100f, 34f), "Unlock");

            Game.UI.RosterPanelUI rosterUI = panel.AddComponent<Game.UI.RosterPanelUI>();
            SerializedObject so = new SerializedObject(rosterUI);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = title.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("bodyText").objectReferenceValue = body.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("statusText").objectReferenceValue = status.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("closeButton").objectReferenceValue = close.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("prevButton").objectReferenceValue = prev.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("nextButton").objectReferenceValue = next.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("selectButton").objectReferenceValue = select.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("unlockButton").objectReferenceValue = unlock.GetComponent<UnityEngine.UI.Button>();
            so.ApplyModifiedProperties();

            EnsurePanelSlider(panel);
            panel.SetActive(false);
            BindRosterPanelServices();
            Debug.Log("[Setup] RosterPanelUI rebuilt.");
        }

        private static void BindRosterPanelServices()
        {
            Game.UI.RosterPanelUI rosterPanel =
                UnityEngine.Object.FindFirstObjectByType<Game.UI.RosterPanelUI>(FindObjectsInactive.Include);
            Game.Roster.RosterService rosterService =
                UnityEngine.Object.FindFirstObjectByType<Game.Roster.RosterService>();

            if (rosterPanel == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(rosterPanel);
            so.FindProperty("rosterService").objectReferenceValue = rosterService;
            so.ApplyModifiedProperties();

            Game.Hub.HubController hubController =
                UnityEngine.Object.FindFirstObjectByType<Game.Hub.HubController>(FindObjectsInactive.Include);
            if (hubController != null)
            {
                SerializedObject hubSO = new SerializedObject(hubController);
                SerializedProperty rosterProp = hubSO.FindProperty("rosterPanelUI");
                if (rosterProp != null)
                {
                    rosterProp.objectReferenceValue = rosterPanel;
                    hubSO.ApplyModifiedProperties();
                }
            }
        }

        private static void WireInventoryClassButton()
        {
            Game.UI.InventoryPanelUI inventoryPanel =
                UnityEngine.Object.FindFirstObjectByType<Game.UI.InventoryPanelUI>(FindObjectsInactive.Include);
            if (inventoryPanel == null)
            {
                return;
            }

            Transform card = inventoryPanel.transform.Find("Card");
            if (card == null)
            {
                card = inventoryPanel.transform;
            }

            Transform existingBtn = card.Find("ClassButton");
            GameObject classBtnObj;
            if (existingBtn != null)
            {
                classBtnObj = existingBtn.gameObject;
            }
            else
            {
                classBtnObj = CreateButton("ClassButton", card, new Vector2(-160f, 210f), new Vector2(130f, 42f), "CLASS");
            }

            RectTransform classRect = classBtnObj.GetComponent<RectTransform>();
            if (classRect != null)
            {
                classRect.anchoredPosition = new Vector2(-160f, 210f);
                classRect.sizeDelta = new Vector2(130f, 42f);
                classRect.localScale = Vector3.one;
            }

            UnityEngine.UI.Image classImg = classBtnObj.GetComponent<UnityEngine.UI.Image>();
            if (classImg != null)
            {
                classImg.color = new Color(0.35f, 0.28f, 0.55f, 1f);
            }

            Transform labelTf = classBtnObj.transform.Find("Text");
            if (labelTf != null)
            {
                labelTf.localScale = Vector3.one;
                UnityEngine.UI.Text label = labelTf.GetComponent<UnityEngine.UI.Text>();
                if (label != null)
                {
                    label.text = "CLASS";
                    label.fontSize = 18;
                    label.fontStyle = FontStyle.Bold;
                    label.color = Color.white;
                }
            }

            classBtnObj.transform.SetAsLastSibling();
            classBtnObj.SetActive(true);

            Game.UI.RosterPanelUI rosterPanel =
                UnityEngine.Object.FindFirstObjectByType<Game.UI.RosterPanelUI>(FindObjectsInactive.Include);

            SerializedObject invSO = new SerializedObject(inventoryPanel);
            invSO.FindProperty("classButton").objectReferenceValue = classBtnObj.GetComponent<UnityEngine.UI.Button>();
            invSO.FindProperty("rosterPanelUI").objectReferenceValue = rosterPanel;
            invSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(inventoryPanel);
            Debug.Log("[Setup] Inventory CLASS button wired.");
        }

        private static void EnsureWeeklyDungeonPanelUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("WeeklyDungeonPanel");
            if (existing != null && s_preserveHubLayout)
            {
                EnsurePanelSlider(existing.gameObject);
                BindWeeklyDungeonPanelServices();
                Debug.Log("[Setup] WeeklyDungeonPanel kept (layout preserved).");
                return;
            }

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = new GameObject("WeeklyDungeonPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(520f, 340f);
            UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
            cardBg.color = new Color(0.05f, 0.08f, 0.1f, 0.98f);

            GameObject title = CreateUIText("WeeklyTitle", card.transform, new Vector2(0f, 130f), "WEEKLY DUNGEON");
            title.GetComponent<UnityEngine.UI.Text>().fontSize = 22;
            title.GetComponent<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;
            title.GetComponent<UnityEngine.UI.Text>().color = new Color(0.55f, 0.9f, 0.95f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(440f, 32f);

            GameObject close = CreateButton("CloseWeeklyButton", card.transform, new Vector2(200f, 130f), new Vector2(72f, 32f), "Close");
            GameObject body = CreateUIText("WeeklyBody", card.transform, new Vector2(0f, 10f), "Dungeon");
            body.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 160f);
            body.GetComponent<UnityEngine.UI.Text>().fontSize = 14;
            body.GetComponent<UnityEngine.UI.Text>().alignment = TextAnchor.UpperLeft;

            GameObject status = CreateUIText("WeeklyStatus", card.transform, new Vector2(0f, -90f), "");
            status.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 24f);
            status.GetComponent<UnityEngine.UI.Text>().fontSize = 13;
            status.GetComponent<UnityEngine.UI.Text>().color = new Color(1f, 0.85f, 0.45f);

            GameObject claim = CreateButton("ClaimStoneButton", card.transform, new Vector2(-90f, -135f), new Vector2(160f, 36f), "Claim Stone");
            GameObject enter = CreateButton("EnterWeeklyButton", card.transform, new Vector2(90f, -135f), new Vector2(160f, 36f), "Enter");

            Game.UI.WeeklyDungeonPanelUI weeklyUI = panel.AddComponent<Game.UI.WeeklyDungeonPanelUI>();
            SerializedObject so = new SerializedObject(weeklyUI);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("titleText").objectReferenceValue = title.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("bodyText").objectReferenceValue = body.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("statusText").objectReferenceValue = status.GetComponent<UnityEngine.UI.Text>();
            so.FindProperty("closeButton").objectReferenceValue = close.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("enterButton").objectReferenceValue = enter.GetComponent<UnityEngine.UI.Button>();
            so.FindProperty("claimStoneButton").objectReferenceValue = claim.GetComponent<UnityEngine.UI.Button>();
            so.ApplyModifiedProperties();

            EnsurePanelSlider(panel);
            panel.SetActive(false);
            BindWeeklyDungeonPanelServices();
            Debug.Log("[Setup] WeeklyDungeonPanelUI rebuilt.");
        }

        private static void BindWeeklyDungeonPanelServices()
        {
            Game.UI.WeeklyDungeonPanelUI weeklyPanel =
                UnityEngine.Object.FindFirstObjectByType<Game.UI.WeeklyDungeonPanelUI>(FindObjectsInactive.Include);
            Game.Dungeon.WeeklyDungeonService weeklyService =
                UnityEngine.Object.FindFirstObjectByType<Game.Dungeon.WeeklyDungeonService>();
            Game.Hub.HubController hubController =
                UnityEngine.Object.FindFirstObjectByType<Game.Hub.HubController>(FindObjectsInactive.Include);

            if (weeklyPanel == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(weeklyPanel);
            so.FindProperty("weeklyService").objectReferenceValue = weeklyService;
            so.FindProperty("hubController").objectReferenceValue = hubController;
            so.ApplyModifiedProperties();

            if (hubController != null)
            {
                SerializedObject hubSO = new SerializedObject(hubController);
                SerializedProperty weeklyProp = hubSO.FindProperty("weeklyDungeonPanelUI");
                if (weeklyProp != null)
                {
                    weeklyProp.objectReferenceValue = weeklyPanel;
                    hubSO.ApplyModifiedProperties();
                }
            }
        }

        private static void WireMapSelectWeeklyButton()
        {
            Game.UI.MapSelectUI mapSelect =
                UnityEngine.Object.FindFirstObjectByType<Game.UI.MapSelectUI>(FindObjectsInactive.Include);
            if (mapSelect == null)
            {
                return;
            }

            Transform card = mapSelect.transform.Find("Card");
            if (card == null)
            {
                card = mapSelect.transform;
            }

            Transform existingBtn = card.Find("WeeklyDungeonButton");
            if (existingBtn == null)
            {
                existingBtn = mapSelect.transform.Find("WeeklyDungeonButton");
            }

            GameObject weeklyBtnObj;
            if (existingBtn != null)
            {
                weeklyBtnObj = existingBtn.gameObject;
            }
            else
            {
                weeklyBtnObj = CreateButton(
                    "WeeklyDungeonButton",
                    card,
                    new Vector2(220f, -210f),
                    new Vector2(150f, 40f),
                    "WEEKLY");
            }

            RectTransform weeklyRect = weeklyBtnObj.GetComponent<RectTransform>();
            if (weeklyRect != null)
            {
                weeklyRect.anchoredPosition = new Vector2(220f, -210f);
                weeklyRect.sizeDelta = new Vector2(150f, 40f);
                weeklyRect.localScale = Vector3.one;
            }

            UnityEngine.UI.Image weeklyImg = weeklyBtnObj.GetComponent<UnityEngine.UI.Image>();
            if (weeklyImg != null)
            {
                weeklyImg.color = new Color(0.15f, 0.42f, 0.48f, 1f);
            }

            Transform labelTf = weeklyBtnObj.transform.Find("Text");
            if (labelTf != null)
            {
                UnityEngine.UI.Text label = labelTf.GetComponent<UnityEngine.UI.Text>();
                if (label != null)
                {
                    label.text = "WEEKLY";
                    label.fontSize = 16;
                    label.fontStyle = FontStyle.Bold;
                    label.color = Color.white;
                }
            }

            weeklyBtnObj.transform.SetAsLastSibling();
            weeklyBtnObj.SetActive(true);

            SerializedObject mapSO = new SerializedObject(mapSelect);
            SerializedProperty weeklyProp = mapSO.FindProperty("weeklyDungeonButton");
            if (weeklyProp != null)
            {
                weeklyProp.objectReferenceValue = weeklyBtnObj.GetComponent<UnityEngine.UI.Button>();
                mapSO.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(mapSelect);
            Debug.Log("[Setup] MapSelect WEEKLY button wired.");
        }

        private static void EnsureAFKSummaryUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("AFKSummaryPanel");
            GameObject panel;
            Game.UI.AFKSummaryUI afkUI;

            if (existing != null)
            {
                panel = existing.gameObject;
                afkUI = panel.GetComponent<Game.UI.AFKSummaryUI>() ?? panel.AddComponent<Game.UI.AFKSummaryUI>();
            }
            else
            {
                panel = new GameObject("AFKSummaryPanel");
                panel.transform.SetParent(canvas.transform, false);
                RectTransform panelRect = panel.AddComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
                dim.color = new Color(0f, 0f, 0f, 0.75f);
                afkUI = panel.AddComponent<Game.UI.AFKSummaryUI>();

                GameObject card = new GameObject("Card");
                card.transform.SetParent(panel.transform, false);
                RectTransform cardRect = card.AddComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(400f, 260f);
                UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
                cardBg.color = new Color(0.04f, 0.1f, 0.12f, 0.96f);

                GameObject title = CreateUIText("AFKTitle", card.transform, new Vector2(0f, 90f), "AFK REWARDS");
                title.GetComponent<UnityEngine.UI.Text>().fontSize = 24;
                title.GetComponent<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;
                title.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 36f);

                GameObject detail = CreateUIText("AFKDetail", card.transform, new Vector2(0f, 45f), "Away for 0 hours");
                detail.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 28f);

                GameObject rewards = CreateUIText("AFKRewards", card.transform, new Vector2(0f, -10f), "+0 Gold");
                rewards.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 70f);
                rewards.GetComponent<UnityEngine.UI.Text>().fontSize = 18;

                GameObject claim = CreateButton("ClaimAFKButton", card.transform, new Vector2(-80f, -85f), new Vector2(140f, 40f), "Claim");
                GameObject close = CreateButton("CloseAFKButton", card.transform, new Vector2(80f, -85f), new Vector2(140f, 40f), "OK");

                SerializedObject so = new SerializedObject(afkUI);
                so.FindProperty("panelRoot").objectReferenceValue = panel;
                so.FindProperty("titleText").objectReferenceValue = title.GetComponent<UnityEngine.UI.Text>();
                so.FindProperty("detailText").objectReferenceValue = detail.GetComponent<UnityEngine.UI.Text>();
                so.FindProperty("rewardsText").objectReferenceValue = rewards.GetComponent<UnityEngine.UI.Text>();
                so.FindProperty("claimButton").objectReferenceValue = claim.GetComponent<UnityEngine.UI.Button>();
                so.FindProperty("closeButton").objectReferenceValue = close.GetComponent<UnityEngine.UI.Button>();
                so.ApplyModifiedProperties();
            }

            panel.SetActive(false);
            Debug.Log("[Setup] AFKSummaryUI ensured.");
        }

        private static Game.Data.ChestData CreateOrUpdateTierChest(
            string path,
            string chestId,
            string displayName,
            Game.Data.ChestTier tier,
            Game.Data.ChestData template)
        {
            Game.Data.ChestData chest = AssetDatabase.LoadAssetAtPath<Game.Data.ChestData>(path);
            bool isNew = chest == null;
            if (isNew)
            {
                chest = ScriptableObject.CreateInstance<Game.Data.ChestData>();
            }

            chest.chestId = chestId;
            chest.displayName = displayName;
            chest.chestTier = tier;
            chest.minimumItems = 1;
            chest.maximumItems = tier == Game.Data.ChestTier.Normal ? 1 : 2;

            if (template != null && template.possibleItems != null)
            {
                chest.possibleItems = new List<Game.Data.ItemData>(template.possibleItems);
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(chest, path);
            }
            else
            {
                EditorUtility.SetDirty(chest);
            }

            return chest;
        }

        private static void EnsureMapSelectUI()
        {
            GameObject canvas = GameObject.Find("ScreenCanvas");
            if (canvas == null)
            {
                Debug.LogWarning("[Setup] ScreenCanvas missing; cannot create MapSelectUI.");
                return;
            }

            Transform existing = canvas.transform.Find("MapSelectPanel");
            bool hasStages = existing != null && existing.Find("AdventureCard/StagesRoot") != null;

            // NEVER rebuild if stages already exist — preserves manual stage positions.
            // Only Rebuild Hub UI (Reset Layout) may wipe them (s_preserveHubLayout == false AND no stages... 
            // actually reset should rebuild: only when !preserve).
            if (existing != null && !s_preserveHubLayout)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
                existing = null;
                hasStages = false;
            }
            else if (existing != null && !hasStages)
            {
                // Old/incomplete adventure panel without stages — safe to rebuild once.
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
                existing = null;
            }

            GameObject panel;
            Game.UI.MapSelectUI mapUI;

            if (existing != null)
            {
                panel = existing.gameObject;
                mapUI = panel.GetComponent<Game.UI.MapSelectUI>() ?? panel.AddComponent<Game.UI.MapSelectUI>();
                BindAdventureMapSprites(mapUI);

                // Soft cleanup only — do not move StagesRoot children.
                Transform cardTf = panel.transform.Find("AdventureCard");
                if (cardTf != null)
                {
                    Transform detail = cardTf.Find("AdventureDetail");
                    if (detail != null)
                    {
                        detail.gameObject.SetActive(false);
                    }

                    Transform prev = cardTf.Find("PrevChapterButton");
                    Transform next = cardTf.Find("NextChapterButton");
                    if (prev != null) UnityEngine.Object.DestroyImmediate(prev.gameObject);
                    if (next != null) UnityEngine.Object.DestroyImmediate(next.gameObject);

                    if (cardTf.Find("LayoutV2") == null)
                    {
                        GameObject marker = new GameObject("LayoutV2");
                        marker.transform.SetParent(cardTf, false);
                    }

                    UnityEngine.UI.Button closeBtn = EnsureAdventureCloseButton(cardTf);
                    if (closeBtn != null)
                    {
                        SerializedObject mapCloseSO = new SerializedObject(mapUI);
                        mapCloseSO.FindProperty("closeButton").objectReferenceValue = closeBtn;
                        mapCloseSO.ApplyModifiedProperties();
                    }

                    EnsureAdventureLayerBossNode(mapUI, cardTf);
                }
            }
            else
            {
                panel = BuildAdventureMapSelectPanel(canvas.transform, out mapUI);
            }

            Game.Data.MapCatalog catalog = AssetDatabase.LoadAssetAtPath<Game.Data.MapCatalog>(SCRIPTABLE_PATH + "/Map/MapCatalog.asset");
            SerializedObject uiSO = new SerializedObject(mapUI);
            uiSO.FindProperty("mapCatalog").objectReferenceValue = catalog;
            if (uiSO.FindProperty("panelRoot").objectReferenceValue == null)
            {
                uiSO.FindProperty("panelRoot").objectReferenceValue = panel;
            }

            uiSO.ApplyModifiedProperties();
            EnsurePanelSlider(panel);
            Transform card = panel.transform.Find("AdventureCard");
            EnsurePanelSlider(panel); // slide whole panel; card is visual root
            if (card != null)
            {
                Game.UI.UIPanelSlider.EnsureOn(panel, card.GetComponent<RectTransform>(), Game.UI.UIPanelSlider.SlideFrom.Left);
            }

            panel.SetActive(false);
            Debug.Log("[Setup] Adventure MapSelectUI ensured.");
        }

        private static GameObject BuildAdventureMapSelectPanel(Transform canvas, out Game.UI.MapSelectUI mapUI)
        {
            Sprite frameSprite = LoadAdventureSprite("Adventure_Frame.png");
            Sprite stageNormal = LoadAdventureSprite("Stage_Normal.png");
            Sprite stageSelected = LoadAdventureSprite("Stage_Selected.png");
            Sprite stageLock = LoadAdventureSprite("Stage_Lock.png");
            Sprite enterSprite = LoadAdventureSprite("Button_Enter.png");

            GameObject panel = new GameObject("MapSelectPanel");
            panel.transform.SetParent(canvas, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dim = panel.AddComponent<UnityEngine.UI.Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);

            mapUI = panel.AddComponent<Game.UI.MapSelectUI>();

            GameObject card = new GameObject("AdventureCard");
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(860f, 480f);
            UnityEngine.UI.Image cardBg = card.AddComponent<UnityEngine.UI.Image>();
            cardBg.sprite = frameSprite;
            cardBg.preserveAspect = true;
            cardBg.color = Color.white;
            if (frameSprite == null)
            {
                cardBg.color = new Color(0.04f, 0.09f, 0.11f, 0.98f);
            }

            // Layout marker so Fix Hub can migrate outdated adventure panels.
            GameObject layoutMarker = new GameObject("LayoutV2");
            layoutMarker.transform.SetParent(card.transform, false);

            GameObject titleObj = CreateUIText("AdventureTitle", card.transform, new Vector2(0f, 198f), "ADVENTURE");
            UnityEngine.UI.Text titleText = titleObj.GetComponent<UnityEngine.UI.Text>();
            titleText.fontSize = 26;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.45f, 0.95f, 0.88f, 1f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 34f);

            UnityEngine.UI.Button closeBtn = EnsureAdventureCloseButton(card.transform);

            // Chapter dropdown — closed bar + stacked option rows (Dropdown_Item_*.png).
            Sprite dropdownNormal = LoadAdventureSprite("Dropdown_Item_Normal.png");
            Sprite dropdownSelected = LoadAdventureSprite("Dropdown_Item_Selected.png");
            Sprite dropdownFallback = LoadAdventureSprite("Chapter_Dropdown.png");
            GameObject dropdownRoot = new GameObject("ChapterDropdown");
            dropdownRoot.transform.SetParent(card.transform, false);
            RectTransform dropdownRt = dropdownRoot.AddComponent<RectTransform>();
            dropdownRt.anchorMin = new Vector2(0.5f, 0.5f);
            dropdownRt.anchorMax = new Vector2(0.5f, 0.5f);
            dropdownRt.sizeDelta = new Vector2(360f, 52f);
            dropdownRt.anchoredPosition = new Vector2(0f, 158f);

            GameObject dropdownBtnGo = new GameObject("DropdownButton");
            dropdownBtnGo.transform.SetParent(dropdownRoot.transform, false);
            RectTransform dropdownBtnRt = dropdownBtnGo.AddComponent<RectTransform>();
            dropdownBtnRt.anchorMin = Vector2.zero;
            dropdownBtnRt.anchorMax = Vector2.one;
            dropdownBtnRt.offsetMin = Vector2.zero;
            dropdownBtnRt.offsetMax = Vector2.zero;
            UnityEngine.UI.Image dropdownImg = dropdownBtnGo.AddComponent<UnityEngine.UI.Image>();
            dropdownImg.sprite = dropdownSelected != null ? dropdownSelected : (dropdownNormal != null ? dropdownNormal : dropdownFallback);
            dropdownImg.preserveAspect = false;
            dropdownImg.type = UnityEngine.UI.Image.Type.Simple;
            dropdownImg.color = Color.white;
            if (dropdownImg.sprite == null)
            {
                dropdownImg.color = new Color(0.08f, 0.16f, 0.16f, 0.95f);
            }

            UnityEngine.UI.Button dropdownBtn = dropdownBtnGo.AddComponent<UnityEngine.UI.Button>();
            dropdownBtn.targetGraphic = dropdownImg;
            dropdownBtn.transition = UnityEngine.UI.Selectable.Transition.None;

            GameObject dropdownLabelGo = CreateUIText("ChapterLabel", dropdownBtnGo.transform, Vector2.zero, "Chapter 1");
            RectTransform dropdownLabelRt = dropdownLabelGo.GetComponent<RectTransform>();
            dropdownLabelRt.anchorMin = Vector2.zero;
            dropdownLabelRt.anchorMax = Vector2.one;
            dropdownLabelRt.offsetMin = new Vector2(28f, 4f);
            dropdownLabelRt.offsetMax = new Vector2(-28f, -4f);
            UnityEngine.UI.Text dropdownLabel = dropdownLabelGo.GetComponent<UnityEngine.UI.Text>();
            dropdownLabel.fontSize = 15;
            dropdownLabel.fontStyle = FontStyle.Bold;
            dropdownLabel.alignment = TextAnchor.MiddleCenter;
            dropdownLabel.color = new Color(0.05f, 0.12f, 0.12f, 1f);
            dropdownLabel.raycastTarget = false;

            GameObject listRoot = new GameObject("ChapterList");
            listRoot.transform.SetParent(dropdownRoot.transform, false);
            RectTransform listRt = listRoot.AddComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.5f, 0f);
            listRt.anchorMax = new Vector2(0.5f, 0f);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.sizeDelta = new Vector2(360f, 180f);
            listRt.anchoredPosition = new Vector2(0f, -4f);
            // Transparent holder — each option paints its own banner art.
            UnityEngine.UI.Image listBg = listRoot.AddComponent<UnityEngine.UI.Image>();
            listBg.color = new Color(0f, 0f, 0f, 0.01f);
            listBg.raycastTarget = true;
            Canvas listCanvas = listRoot.AddComponent<Canvas>();
            listCanvas.overrideSorting = true;
            listCanvas.sortingOrder = 40;
            listRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            GameObject listContent = new GameObject("Content");
            listContent.transform.SetParent(listRoot.transform, false);
            RectTransform contentRt = listContent.AddComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            // No VerticalLayoutGroup — rows are scene objects you can move in Edit Mode.
            listRoot.SetActive(false);

            GameObject stagesRoot = new GameObject("StagesRoot");
            stagesRoot.transform.SetParent(card.transform, false);
            RectTransform stagesRect = stagesRoot.AddComponent<RectTransform>();
            stagesRect.anchorMin = new Vector2(0.5f, 0.5f);
            stagesRect.anchorMax = new Vector2(0.5f, 0.5f);
            stagesRect.sizeDelta = new Vector2(700f, 240f);
            stagesRect.anchoredPosition = new Vector2(10f, -8f);

            // Path along swamp road (left → portal).
            Vector2[] path =
            {
                new Vector2(-270f, -75f),
                new Vector2(-195f, -25f),
                new Vector2(-125f, 30f),
                new Vector2(-50f, -5f),
                new Vector2(25f, 45f),
                new Vector2(100f, 8f),
                new Vector2(175f, 55f),
                new Vector2(245f, 20f),
                new Vector2(305f, 65f),
                new Vector2(255f, -50f)
            };

            var nodes = new List<Game.UI.AdventureStageNodeUI>();
            for (int i = 0; i < path.Length; i++)
            {
                nodes.Add(CreateAdventureStageNode(
                    stagesRoot.transform,
                    i + 1,
                    path[i],
                    stageNormal,
                    stageSelected,
                    stageLock));
            }

            // Enter button
            GameObject enterGo = new GameObject("EnterButton");
            enterGo.transform.SetParent(card.transform, false);
            RectTransform enterRect = enterGo.AddComponent<RectTransform>();
            enterRect.anchorMin = new Vector2(0.5f, 0.5f);
            enterRect.anchorMax = new Vector2(0.5f, 0.5f);
            enterRect.sizeDelta = new Vector2(260f, 68f);
            enterRect.anchoredPosition = new Vector2(0f, -185f);
            UnityEngine.UI.Image enterImg = enterGo.AddComponent<UnityEngine.UI.Image>();
            enterImg.sprite = enterSprite;
            enterImg.preserveAspect = true;
            enterImg.color = Color.white;
            if (enterSprite == null)
            {
                enterImg.color = new Color(0.1f, 0.35f, 0.32f, 1f);
            }

            UnityEngine.UI.Button enterBtn = enterGo.AddComponent<UnityEngine.UI.Button>();
            enterBtn.targetGraphic = enterImg;
            GameObject enterLabel = CreateUIText("EnterLabel", enterGo.transform, Vector2.zero, "ENTER");
            RectTransform enterLabelRt = enterLabel.GetComponent<RectTransform>();
            enterLabelRt.anchorMin = Vector2.zero;
            enterLabelRt.anchorMax = Vector2.one;
            enterLabelRt.offsetMin = Vector2.zero;
            enterLabelRt.offsetMax = Vector2.zero;
            UnityEngine.UI.Text enterText = enterLabel.GetComponent<UnityEngine.UI.Text>();
            enterText.fontSize = 26;
            enterText.fontStyle = FontStyle.Bold;
            enterText.color = new Color(0.55f, 1f, 0.92f, 1f);
            enterText.alignment = TextAnchor.MiddleCenter;

            GameObject bossBtn = CreateButton("LayerBossButton", card.transform, new Vector2(0f, -220f), new Vector2(180f, 32f), "Layer Boss");
            bossBtn.SetActive(false);

            SerializedObject mapSO = new SerializedObject(mapUI);
            mapSO.FindProperty("panelRoot").objectReferenceValue = panel;
            mapSO.FindProperty("titleText").objectReferenceValue = titleText;
            mapSO.FindProperty("detailText").objectReferenceValue = null;
            mapSO.FindProperty("startButton").objectReferenceValue = enterBtn;
            mapSO.FindProperty("layerBossButton").objectReferenceValue = bossBtn.GetComponent<UnityEngine.UI.Button>();
            mapSO.FindProperty("closeButton").objectReferenceValue = closeBtn;
            mapSO.FindProperty("chapterDropdownButton").objectReferenceValue = dropdownBtn;
            mapSO.FindProperty("chapterDropdownLabel").objectReferenceValue = dropdownLabel;
            mapSO.FindProperty("chapterListRoot").objectReferenceValue = listRoot;
            mapSO.FindProperty("chapterListContent").objectReferenceValue = listContent.transform;
            mapSO.FindProperty("dropdownItemNormalSprite").objectReferenceValue = dropdownNormal;
            mapSO.FindProperty("dropdownItemSelectedSprite").objectReferenceValue = dropdownSelected;
            mapSO.FindProperty("stageNormalSprite").objectReferenceValue = stageNormal;
            mapSO.FindProperty("stageSelectedSprite").objectReferenceValue = stageSelected;
            mapSO.FindProperty("stageLockedSprite").objectReferenceValue = stageLock;

            SerializedProperty nodesProp = mapSO.FindProperty("stageNodes");
            nodesProp.arraySize = nodes.Count;
            for (int i = 0; i < nodes.Count; i++)
            {
                nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
            }

            mapSO.ApplyModifiedProperties();

            // Scene-authored chapter rows (Edit Mode editable).
            EnsureAdventureChapterOptions(mapUI, listContent.transform, dropdownNormal, dropdownSelected);
            EnsureAdventureLayerBossNode(mapUI, card.transform);
            return panel;
        }

        /// <summary>
        /// Creates Stage_LayerBoss under StagesRoot (Edit Mode editable). Does not move existing node.
        /// </summary>
        private static void EnsureAdventureLayerBossNode(Game.UI.MapSelectUI mapUI, Transform card)
        {
            if (mapUI == null || card == null)
            {
                return;
            }

            Transform stagesRoot = card.Find("StagesRoot");
            if (stagesRoot == null)
            {
                return;
            }

            Sprite stageNormal = LoadAdventureSprite("Stage_Normal.png");
            Sprite stageSelected = LoadAdventureSprite("Stage_Selected.png");
            Sprite stageLock = LoadAdventureSprite("Stage_Lock.png");

            Transform existing = stagesRoot.Find("Stage_LayerBoss");
            Game.UI.AdventureStageNodeUI bossNode;
            if (existing != null)
            {
                bossNode = existing.GetComponent<Game.UI.AdventureStageNodeUI>()
                           ?? existing.gameObject.AddComponent<Game.UI.AdventureStageNodeUI>();
            }
            else
            {
                // Default spot near path end — user can move freely in Edit Mode.
                bossNode = CreateAdventureStageNode(
                    stagesRoot,
                    100,
                    new Vector2(320f, -90f),
                    stageNormal,
                    stageSelected,
                    stageLock);
                bossNode.gameObject.name = "Stage_LayerBoss";
            }

            UnityEngine.UI.Button btn = bossNode.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image img = bossNode.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Text label = bossNode.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (label != null)
            {
                label.text = "LB";
            }

            bossNode.Configure(
                100,
                stageNormal,
                stageSelected,
                stageLock,
                btn,
                img,
                null,
                label);
            EditorUtility.SetDirty(bossNode);

            SerializedObject mapSO = new SerializedObject(mapUI);
            SerializedProperty layerBossProp = mapSO.FindProperty("layerBossNode");
            if (layerBossProp != null)
            {
                layerBossProp.objectReferenceValue = bossNode;
                mapSO.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[Setup] MapSelectUI.layerBossNode field missing — scripts may need recompile.");
            }

            Transform legacyBtn = card.Find("LayerBossButton");
            if (legacyBtn != null)
            {
                legacyBtn.gameObject.SetActive(false);
            }

            Debug.Log("[Setup] Adventure Stage_LayerBoss node ensured (place it on the map in Edit Mode).");
        }

        private static Game.UI.AdventureStageNodeUI CreateAdventureStageNode(
            Transform parent,
            int index,
            Vector2 anchoredPos,
            Sprite normal,
            Sprite selected,
            Sprite lockSprite)
        {
            GameObject go = new GameObject($"Stage_{index}");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(100f, 100f);
            rt.anchoredPosition = anchoredPos;
            rt.localScale = Vector3.one;

            UnityEngine.UI.Image stageImg = go.AddComponent<UnityEngine.UI.Image>();
            stageImg.sprite = normal;
            stageImg.preserveAspect = true;
            stageImg.raycastTarget = true;
            stageImg.color = Color.white;
            if (normal == null)
            {
                stageImg.color = new Color(0.15f, 0.2f, 0.22f, 1f);
            }

            UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = stageImg;
            btn.transition = UnityEngine.UI.Selectable.Transition.None;

            GameObject numGo = CreateUIText("Number", go.transform, new Vector2(0f, -2f), index.ToString());
            RectTransform numRt = numGo.GetComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0.5f, 0.5f);
            numRt.anchorMax = new Vector2(0.5f, 0.5f);
            numRt.sizeDelta = new Vector2(48f, 36f);
            numRt.localScale = Vector3.one;
            UnityEngine.UI.Text numText = numGo.GetComponent<UnityEngine.UI.Text>();
            numText.fontSize = 20;
            numText.fontStyle = FontStyle.Bold;
            numText.color = new Color(0.85f, 1f, 0.96f, 1f);
            numText.alignment = TextAnchor.MiddleCenter;
            numText.raycastTarget = false;

            // Legacy overlay slot kept inactive — locked art is Stage_Lock on stageImage.
            GameObject lockGo = new GameObject("Lock");
            lockGo.transform.SetParent(go.transform, false);
            RectTransform lockRt = lockGo.AddComponent<RectTransform>();
            lockRt.anchorMin = new Vector2(0.5f, 0.5f);
            lockRt.anchorMax = new Vector2(0.5f, 0.5f);
            lockRt.sizeDelta = new Vector2(34f, 34f);
            lockRt.anchoredPosition = new Vector2(0f, 30f);
            lockRt.localScale = Vector3.one;
            UnityEngine.UI.Image lockImg = lockGo.AddComponent<UnityEngine.UI.Image>();
            lockImg.raycastTarget = false;
            lockGo.SetActive(false);

            Game.UI.AdventureStageNodeUI node = go.AddComponent<Game.UI.AdventureStageNodeUI>();
            SerializedObject so = new SerializedObject(node);
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("stageImage").objectReferenceValue = stageImg;
            so.FindProperty("lockImage").objectReferenceValue = lockImg;
            so.FindProperty("numberText").objectReferenceValue = numText;
            so.FindProperty("normalSprite").objectReferenceValue = normal;
            so.FindProperty("selectedSprite").objectReferenceValue = selected;
            so.FindProperty("lockedSprite").objectReferenceValue = lockSprite;
            so.ApplyModifiedProperties();
            return node;
        }

        private static void BindAdventureMapSprites(Game.UI.MapSelectUI mapUI)
        {
            if (mapUI == null)
            {
                return;
            }

            Sprite stageNormal = LoadAdventureSprite("Stage_Normal.png");
            Sprite stageSelected = LoadAdventureSprite("Stage_Selected.png");
            Sprite stageLocked = LoadAdventureSprite("Stage_Lock.png");
            Sprite dropdownNormal = LoadAdventureSprite("Dropdown_Item_Normal.png");
            Sprite dropdownSelected = LoadAdventureSprite("Dropdown_Item_Selected.png");
            SerializedObject so = new SerializedObject(mapUI);
            if (stageNormal != null)
            {
                so.FindProperty("stageNormalSprite").objectReferenceValue = stageNormal;
            }

            if (stageSelected != null)
            {
                so.FindProperty("stageSelectedSprite").objectReferenceValue = stageSelected;
            }

            if (stageLocked != null)
            {
                so.FindProperty("stageLockedSprite").objectReferenceValue = stageLocked;
            }

            if (dropdownNormal != null)
            {
                so.FindProperty("dropdownItemNormalSprite").objectReferenceValue = dropdownNormal;
            }

            if (dropdownSelected != null)
            {
                so.FindProperty("dropdownItemSelectedSprite").objectReferenceValue = dropdownSelected;
            }

            so.ApplyModifiedProperties();

            EnsureAdventureChapterDropdown(mapUI);

            // Push locked sprite onto existing stage nodes without moving them.
            SerializedProperty nodesProp = so.FindProperty("stageNodes");
            if (nodesProp != null && nodesProp.isArray && stageLocked != null)
            {
                for (int i = 0; i < nodesProp.arraySize; i++)
                {
                    Game.UI.AdventureStageNodeUI node =
                        nodesProp.GetArrayElementAtIndex(i).objectReferenceValue as Game.UI.AdventureStageNodeUI;
                    if (node == null)
                    {
                        continue;
                    }

                    SerializedObject nodeSO = new SerializedObject(node);
                    nodeSO.FindProperty("lockedSprite").objectReferenceValue = stageLocked;
                    if (stageNormal != null)
                    {
                        nodeSO.FindProperty("normalSprite").objectReferenceValue = stageNormal;
                    }

                    if (stageSelected != null)
                    {
                        nodeSO.FindProperty("selectedSprite").objectReferenceValue = stageSelected;
                    }

                    nodeSO.ApplyModifiedProperties();
                }
            }

            Transform card = mapUI.transform.Find("AdventureCard");
            if (card != null)
            {
                UnityEngine.UI.Image frame = card.GetComponent<UnityEngine.UI.Image>();
                Sprite frameSprite = LoadAdventureSprite("Adventure_Frame.png");
                if (frame != null && frame.sprite == null && frameSprite != null)
                {
                    frame.sprite = frameSprite;
                    frame.preserveAspect = true;
                }

                Transform enter = card.Find("EnterButton");
                if (enter != null)
                {
                    UnityEngine.UI.Image enterImg = enter.GetComponent<UnityEngine.UI.Image>();
                    Sprite enterSprite = LoadAdventureSprite("Button_Enter.png");
                    if (enterImg != null && enterImg.sprite == null && enterSprite != null)
                    {
                        enterImg.sprite = enterSprite;
                        enterImg.preserveAspect = true;
                    }
                }

                EnsureAdventureCloseButton(card);
            }
        }

        /// <summary>
        /// Wires chapter dropdown art + list layout without destroying stage positions.
        /// </summary>
        private static void EnsureAdventureChapterDropdown(Game.UI.MapSelectUI mapUI)
        {
            if (mapUI == null)
            {
                return;
            }

            Transform card = mapUI.transform.Find("AdventureCard");
            if (card == null)
            {
                return;
            }

            Transform dropdownRoot = card.Find("ChapterDropdown");
            if (dropdownRoot == null)
            {
                return;
            }

            Sprite dropdownNormal = LoadAdventureSprite("Dropdown_Item_Normal.png");
            Sprite dropdownSelected = LoadAdventureSprite("Dropdown_Item_Selected.png");
            Sprite dropdownFallback = LoadAdventureSprite("Chapter_Dropdown.png");

            Transform btnTf = dropdownRoot.Find("DropdownButton");
            UnityEngine.UI.Button dropdownBtn = btnTf != null ? btnTf.GetComponent<UnityEngine.UI.Button>() : null;
            UnityEngine.UI.Text dropdownLabel = btnTf != null ? btnTf.GetComponentInChildren<UnityEngine.UI.Text>(true) : null;
            if (btnTf != null)
            {
                UnityEngine.UI.Image dropdownImg = btnTf.GetComponent<UnityEngine.UI.Image>();
                if (dropdownImg != null)
                {
                    Sprite use = dropdownSelected != null
                        ? dropdownSelected
                        : (dropdownNormal != null ? dropdownNormal : dropdownFallback);
                    if (use != null)
                    {
                        dropdownImg.sprite = use;
                        dropdownImg.type = UnityEngine.UI.Image.Type.Simple;
                        dropdownImg.preserveAspect = false;
                        dropdownImg.color = Color.white;
                    }
                }

                if (dropdownBtn != null)
                {
                    dropdownBtn.transition = UnityEngine.UI.Selectable.Transition.None;
                }

                RectTransform dropdownRootRt = dropdownRoot.GetComponent<RectTransform>();
                if (dropdownRootRt != null)
                {
                    // Widen only if still on the old compact size; keep manual moves.
                    if (dropdownRootRt.sizeDelta.x < 330f)
                    {
                        dropdownRootRt.sizeDelta = new Vector2(360f, Mathf.Max(48f, dropdownRootRt.sizeDelta.y));
                    }
                }

                if (dropdownLabel != null)
                {
                    dropdownLabel.alignment = TextAnchor.MiddleCenter;
                    dropdownLabel.fontStyle = FontStyle.Bold;
                    RectTransform labelRt = dropdownLabel.GetComponent<RectTransform>();
                    if (labelRt != null)
                    {
                        labelRt.offsetMin = new Vector2(28f, 4f);
                        labelRt.offsetMax = new Vector2(-28f, -4f);
                    }
                }
            }

            Transform listRoot = dropdownRoot.Find("ChapterList");
            Transform listContent = listRoot != null ? listRoot.Find("Content") : null;
            if (listRoot != null)
            {
                RectTransform listRt = listRoot.GetComponent<RectTransform>();
                if (listRt != null)
                {
                    listRt.anchorMin = new Vector2(0.5f, 0f);
                    listRt.anchorMax = new Vector2(0.5f, 0f);
                    listRt.pivot = new Vector2(0.5f, 1f);
                    listRt.anchoredPosition = new Vector2(0f, -4f);
                    float width = dropdownRoot.GetComponent<RectTransform>() != null
                        ? dropdownRoot.GetComponent<RectTransform>().sizeDelta.x
                        : 360f;
                    listRt.sizeDelta = new Vector2(width, Mathf.Max(180f, listRt.sizeDelta.y));
                }

                UnityEngine.UI.Image listBg = listRoot.GetComponent<UnityEngine.UI.Image>();
                if (listBg != null)
                {
                    listBg.sprite = null;
                    listBg.color = new Color(0f, 0f, 0f, 0.01f);
                    listBg.raycastTarget = true;
                }

                Canvas listCanvas = listRoot.GetComponent<Canvas>();
                if (listCanvas == null)
                {
                    listCanvas = listRoot.gameObject.AddComponent<Canvas>();
                }

                listCanvas.overrideSorting = true;
                listCanvas.sortingOrder = 40;
                if (listRoot.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                {
                    listRoot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
            }

            if (listContent != null)
            {
                RectTransform contentRt = listContent.GetComponent<RectTransform>();
                if (contentRt != null)
                {
                    contentRt.offsetMin = Vector2.zero;
                    contentRt.offsetMax = Vector2.zero;
                }

                // Remove auto-layout so Edit Mode position/size edits stick.
                UnityEngine.UI.VerticalLayoutGroup vlg = listContent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (vlg != null)
                {
                    UnityEngine.Object.DestroyImmediate(vlg);
                }
            }

            SerializedObject mapSO = new SerializedObject(mapUI);
            if (dropdownBtn != null)
            {
                mapSO.FindProperty("chapterDropdownButton").objectReferenceValue = dropdownBtn;
            }

            if (dropdownLabel != null)
            {
                mapSO.FindProperty("chapterDropdownLabel").objectReferenceValue = dropdownLabel;
            }

            if (listRoot != null)
            {
                mapSO.FindProperty("chapterListRoot").objectReferenceValue = listRoot.gameObject;
            }

            if (listContent != null)
            {
                mapSO.FindProperty("chapterListContent").objectReferenceValue = listContent;
            }

            if (dropdownNormal != null)
            {
                mapSO.FindProperty("dropdownItemNormalSprite").objectReferenceValue = dropdownNormal;
            }

            if (dropdownSelected != null)
            {
                mapSO.FindProperty("dropdownItemSelectedSprite").objectReferenceValue = dropdownSelected;
            }

            mapSO.ApplyModifiedProperties();

            if (listContent != null)
            {
                EnsureAdventureChapterOptions(mapUI, listContent, dropdownNormal, dropdownSelected);
            }
        }

        /// <summary>
        /// Creates chapter dropdown rows in the scene (Edit Mode). Runtime only refreshes them.
        /// Does not move existing ChapterOption_* RectTransforms.
        /// </summary>
        private static void EnsureAdventureChapterOptions(
            Game.UI.MapSelectUI mapUI,
            Transform listContent,
            Sprite normalSprite,
            Sprite selectedSprite)
        {
            if (mapUI == null || listContent == null)
            {
                return;
            }

            Game.Data.MapCatalog catalog =
                AssetDatabase.LoadAssetAtPath<Game.Data.MapCatalog>(SCRIPTABLE_PATH + "/Map/MapCatalog.asset");
            int chapterCount = 3;
            if (catalog != null && catalog.Layers != null && catalog.Layers.Count > 0)
            {
                Game.Data.LayerData layer = catalog.Layers[0];
                if (layer != null && layer.ChapterCount > 0)
                {
                    chapterCount = layer.ChapterCount;
                }
            }

            const float optionHeight = 56f;
            const float optionSpacing = 6f;
            float optionWidth = 360f;
            Transform dropdownRoot = listContent.parent != null ? listContent.parent.parent : null;
            if (dropdownRoot != null)
            {
                RectTransform dropdownRt = dropdownRoot.GetComponent<RectTransform>();
                if (dropdownRt != null && dropdownRt.sizeDelta.x > 1f)
                {
                    optionWidth = dropdownRt.sizeDelta.x;
                }
            }

            var options = new List<Game.UI.AdventureChapterOptionUI>();
            for (int c = 1; c <= chapterCount; c++)
            {
                string name = "ChapterOption_" + c;
                Transform existing = listContent.Find(name);
                Game.UI.AdventureChapterOptionUI option;
                if (existing != null)
                {
                    option = existing.GetComponent<Game.UI.AdventureChapterOptionUI>()
                             ?? existing.gameObject.AddComponent<Game.UI.AdventureChapterOptionUI>();
                    // Keep RectTransform — do not move/resize existing rows.
                }
                else
                {
                    option = CreateAdventureChapterOption(
                        listContent,
                        c,
                        new Vector2(0f, -((c - 1) * (optionHeight + optionSpacing) + optionHeight * 0.5f + 4f)),
                        new Vector2(optionWidth, optionHeight),
                        normalSprite,
                        selectedSprite);
                }

                if (option != null)
                {
                    SerializedObject optSO = new SerializedObject(option);
                    optSO.FindProperty("chapterIndex").intValue = c;
                    UnityEngine.UI.Button btn = option.GetComponent<UnityEngine.UI.Button>();
                    UnityEngine.UI.Image bg = option.GetComponent<UnityEngine.UI.Image>();
                    UnityEngine.UI.Text label = option.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    if (btn != null) optSO.FindProperty("button").objectReferenceValue = btn;
                    if (bg != null) optSO.FindProperty("backgroundImage").objectReferenceValue = bg;
                    if (label != null)
                    {
                        optSO.FindProperty("labelText").objectReferenceValue = label;
                        string display = "Chapter " + c;
                        if (catalog != null)
                        {
                            Game.Data.LayerData layer = catalog.GetLayer(1);
                            Game.Data.ChapterData chapter = layer != null ? layer.GetChapter(c) : null;
                            if (chapter != null && !string.IsNullOrEmpty(chapter.displayName))
                            {
                                display = chapter.displayName;
                            }
                        }

                        label.text = display;
                        if (bg != null)
                        {
                            bool selected = c == 1;
                            Sprite use = selected && selectedSprite != null ? selectedSprite : normalSprite;
                            if (use != null)
                            {
                                bg.sprite = use;
                                bg.color = Color.white;
                            }
                        }
                    }

                    optSO.ApplyModifiedProperties();
                    options.Add(option);
                }
            }

            // Resize list root to fit rows (only if still near default height).
            Transform listRoot = listContent.parent;
            if (listRoot != null)
            {
                RectTransform listRt = listRoot.GetComponent<RectTransform>();
                if (listRt != null)
                {
                    float height = (optionHeight * chapterCount) + (optionSpacing * Mathf.Max(0, chapterCount - 1)) + 12f;
                    listRt.sizeDelta = new Vector2(optionWidth, height);
                }
            }

            SerializedObject mapSO = new SerializedObject(mapUI);
            SerializedProperty optionsProp = mapSO.FindProperty("chapterOptions");
            if (optionsProp != null)
            {
                optionsProp.arraySize = options.Count;
                for (int i = 0; i < options.Count; i++)
                {
                    optionsProp.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
                }
            }

            mapSO.ApplyModifiedProperties();
        }

        private static Game.UI.AdventureChapterOptionUI CreateAdventureChapterOption(
            Transform parent,
            int chapterIndex,
            Vector2 anchoredPos,
            Vector2 size,
            Sprite normalSprite,
            Sprite selectedSprite)
        {
            GameObject go = new GameObject("ChapterOption_" + chapterIndex);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            UnityEngine.UI.Image bg = go.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = chapterIndex == 1 && selectedSprite != null ? selectedSprite : normalSprite;
            bg.type = UnityEngine.UI.Image.Type.Simple;
            bg.preserveAspect = false;
            bg.color = Color.white;
            bg.raycastTarget = true;

            UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = bg;
            btn.transition = UnityEngine.UI.Selectable.Transition.None;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(28f, 4f);
            labelRt.offsetMax = new Vector2(-28f, -4f);
            UnityEngine.UI.Text label = labelGo.AddComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = chapterIndex == 1
                ? new Color(0.05f, 0.12f, 0.12f, 1f)
                : new Color(0.78f, 0.98f, 0.94f, 1f);
            label.raycastTarget = false;
            label.text = "Chapter " + chapterIndex;

            Game.UI.AdventureChapterOptionUI option = go.AddComponent<Game.UI.AdventureChapterOptionUI>();
            SerializedObject optSO = new SerializedObject(option);
            optSO.FindProperty("chapterIndex").intValue = chapterIndex;
            optSO.FindProperty("button").objectReferenceValue = btn;
            optSO.FindProperty("backgroundImage").objectReferenceValue = bg;
            optSO.FindProperty("labelText").objectReferenceValue = label;
            optSO.ApplyModifiedProperties();
            return option;
        }

        /// <summary>
        /// Adventure close button using Button_Close.png (drop art into Art/UI/Adventure/).
        /// Preserves RectTransform if the button already exists.
        /// </summary>
        private static UnityEngine.UI.Button EnsureAdventureCloseButton(Transform card)
        {
            if (card == null)
            {
                return null;
            }

            Sprite closeSprite = LoadAdventureSprite("Button_Close.png");
            Transform existing = card.Find("CloseMapButton");
            GameObject closeGo;
            bool created = existing == null;
            if (created)
            {
                closeGo = new GameObject("CloseMapButton");
                closeGo.transform.SetParent(card, false);
                RectTransform rt = closeGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(56f, 56f);
                rt.anchoredPosition = new Vector2(340f, 198f);
            }
            else
            {
                closeGo = existing.gameObject;
            }

            UnityEngine.UI.Image img = closeGo.GetComponent<UnityEngine.UI.Image>();
            if (img == null)
            {
                img = closeGo.AddComponent<UnityEngine.UI.Image>();
            }

            if (closeSprite != null)
            {
                img.sprite = closeSprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else if (created || img.sprite == null)
            {
                img.color = new Color(0.12f, 0.2f, 0.22f, 0.95f);
            }

            UnityEngine.UI.Button btn = closeGo.GetComponent<UnityEngine.UI.Button>();
            if (btn == null)
            {
                btn = closeGo.AddComponent<UnityEngine.UI.Button>();
            }

            btn.targetGraphic = img;
            btn.transition = UnityEngine.UI.Selectable.Transition.None;

            // Hide legacy "X" label when sprite art is present.
            Transform textTf = closeGo.transform.Find("Text");
            if (textTf != null)
            {
                textTf.gameObject.SetActive(closeSprite == null);
                UnityEngine.UI.Text t = textTf.GetComponent<UnityEngine.UI.Text>();
                if (t != null && closeSprite == null)
                {
                    t.text = "X";
                }
            }
            else if (closeSprite == null && closeGo.GetComponentInChildren<UnityEngine.UI.Text>() == null)
            {
                GameObject label = CreateUIText("Text", closeGo.transform, Vector2.zero, "X");
                RectTransform labelRt = label.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                label.GetComponent<UnityEngine.UI.Text>().fontSize = 22;
                label.GetComponent<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;
                label.GetComponent<UnityEngine.UI.Text>().raycastTarget = false;
            }
            else if (closeSprite != null)
            {
                UnityEngine.UI.Text[] labels = closeGo.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].gameObject.SetActive(false);
                }
            }

            EditorUtility.SetDirty(closeGo);
            return btn;
        }

        private static Sprite LoadAdventureSprite(string fileName)
        {
            string path = ART_PATH + "/UI/Adventure/" + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is Sprite s)
                    {
                        return s;
                    }
                }
            }

            return null;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // Project uses New Input System (activeInputHandler = 1)
            var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                eventSystem.AddComponent(inputModuleType);
                Debug.Log("[Setup] Created EventSystem with InputSystemUIInputModule.");
            }
            else
            {
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[Setup] Created EventSystem with StandaloneInputModule.");
            }
        }

        private static void EnsureCameraFollow()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Setup] Main Camera not found for CameraFollow2D.");
                return;
            }

            Game.Environment.CameraFollow2D follow = cam.GetComponent<Game.Environment.CameraFollow2D>();
            if (follow == null)
            {
                follow = cam.gameObject.AddComponent<Game.Environment.CameraFollow2D>();
            }

            GameObject hero = GameObject.Find("Hero");
            SerializedObject so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = hero != null ? hero.transform : null;
            so.FindProperty("followX").boolValue = true;
            so.FindProperty("followY").boolValue = false;
            so.FindProperty("offset").vector3Value = new Vector3(1.2f, 0f, -10f);
            so.FindProperty("smoothTime").floatValue = 0.12f;
            so.ApplyModifiedProperties();

            if (hero != null)
            {
                follow.SetTarget(hero.transform);
            }

            Debug.Log("[Setup] CameraFollow2D ensured on Main Camera.");
        }

        private static GameObject CreateUIText(string name, Transform parent, Vector2 anchoredPos, string text)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200, 30);
            rect.anchoredPosition = anchoredPos;
            rect.localScale = Vector3.one;

            UnityEngine.UI.Text uiText = textObj.AddComponent<UnityEngine.UI.Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = 18;
            uiText.color = Color.white;
            uiText.alignment = TextAnchor.MiddleCenter;

            return textObj;
        }

        private static GameObject CreateButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string text)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            UnityEngine.UI.Image btnImage = btnObj.AddComponent<UnityEngine.UI.Image>();
            btnImage.color = new Color(0.2f, 0.5f, 0.7f);

            UnityEngine.UI.Button button = btnObj.AddComponent<UnityEngine.UI.Button>();
            
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.5f, 0.7f);
            colors.highlightedColor = new Color(0.3f, 0.6f, 0.8f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.6f);
            button.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Text btnText = textObj.AddComponent<UnityEngine.UI.Text>();
            btnText.text = text;
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 18;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;

            return btnObj;
        }

        private static void CreateSystemsHierarchy(Transform root)
        {
            GameObject systems = FindOrCreate("Systems", root);

            FindOrCreate("GameManager", systems.transform);
            GameObject battleController = FindOrCreate("BattleController", systems.transform);
            GameObject rewardController = FindOrCreate("RewardController", systems.transform);
            GameObject saveController = FindOrCreate("SaveController", systems.transform);

            if (battleController.GetComponent<Game.Stage.StageBattleController>() == null)
            {
                battleController.AddComponent<Game.Stage.StageBattleController>();
            }

            if (rewardController.GetComponent<Game.Rewards.RewardService>() == null)
            {
                rewardController.AddComponent<Game.Rewards.RewardService>();
            }

            if (saveController.GetComponent<Game.Save.SaveService>() == null)
            {
                saveController.AddComponent<Game.Save.SaveService>();
            }

            if (saveController.GetComponent<Game.Save.GameSaveController>() == null)
            {
                saveController.AddComponent<Game.Save.GameSaveController>();
            }

            GameObject parallaxRoot = GameObject.Find("ParallaxRoot");
            if (parallaxRoot != null && parallaxRoot.GetComponent<Game.Environment.ParallaxScroller>() == null)
            {
                Game.Environment.ParallaxScroller scroller = parallaxRoot.AddComponent<Game.Environment.ParallaxScroller>();
                SerializedObject scrollerSO = new SerializedObject(scroller);
                scrollerSO.FindProperty("farLayer.root").objectReferenceValue = GameObject.Find("FarLayer")?.transform;
                scrollerSO.FindProperty("farLayer.loop").boolValue = true;
                scrollerSO.FindProperty("midLayer.root").objectReferenceValue = GameObject.Find("MidLayer")?.transform;
                scrollerSO.FindProperty("midLayer.loop").boolValue = true;
                scrollerSO.FindProperty("foregroundLayer.root").objectReferenceValue = GameObject.Find("ForegroundLayer")?.transform;
                scrollerSO.FindProperty("foregroundLayer.loop").boolValue = true;
                scrollerSO.FindProperty("tilesPerLayer").intValue = 2;
                scrollerSO.ApplyModifiedProperties();
            }

            BindBattleControllerReferences();
        }

        private static void BindBattleControllerReferences()
        {
            GameObject battleController = GameObject.Find("BattleController");
            if (battleController == null)
            {
                Debug.LogWarning("[Setup] BattleController not found.");
                return;
            }

            Game.Stage.StageBattleController controller = battleController.GetComponent<Game.Stage.StageBattleController>();
            if (controller == null)
            {
                Debug.LogWarning("[Setup] StageBattleController component missing.");
                return;
            }

            SerializedObject so = new SerializedObject(controller);

            GameObject hero = GameObject.Find("Hero");
            if (hero != null)
            {
                so.FindProperty("hero").objectReferenceValue = hero.GetComponent<Game.Units.HeroUnit>();
            }

            so.FindProperty("heroStartPoint").objectReferenceValue = GameObject.Find("HeroStartPoint")?.transform;
            so.FindProperty("heroCombatPoint").objectReferenceValue = GameObject.Find("HeroCombatPoint")?.transform;
            so.FindProperty("enemySpawnPoint").objectReferenceValue = GameObject.Find("EnemySpawnPoint")?.transform;
            so.FindProperty("enemyCombatPoint").objectReferenceValue = GameObject.Find("EnemyCombatPoint")?.transform;
            so.FindProperty("runtimeEnemiesRoot").objectReferenceValue = GameObject.Find("RuntimeEnemies")?.transform;

            GameObject parallaxRoot = GameObject.Find("ParallaxRoot");
            if (parallaxRoot != null)
            {
                so.FindProperty("parallaxScroller").objectReferenceValue = parallaxRoot.GetComponent<Game.Environment.ParallaxScroller>();
            }

            GameObject rewardController = GameObject.Find("RewardController");
            Game.Rewards.RewardService rewardService = rewardController != null
                ? rewardController.GetComponent<Game.Rewards.RewardService>()
                : null;
            so.FindProperty("rewardService").objectReferenceValue = rewardService;

            Game.Data.MapCatalog mapCatalog = AssetDatabase.LoadAssetAtPath<Game.Data.MapCatalog>(SCRIPTABLE_PATH + "/Map/MapCatalog.asset");
            SerializedProperty mapCatalogProp = so.FindProperty("mapCatalog");
            if (mapCatalogProp != null)
            {
                mapCatalogProp.objectReferenceValue = mapCatalog;
            }

            if (rewardService != null)
            {
                SerializedObject rewardSO = new SerializedObject(rewardService);
                GameObject goldText = GameObject.Find("GoldText");
                GameObject experienceText = GameObject.Find("ExperienceText");

                if (goldText != null)
                {
                    rewardSO.FindProperty("goldText").objectReferenceValue = goldText.GetComponent<UnityEngine.UI.Text>();
                }

                if (experienceText != null)
                {
                    rewardSO.FindProperty("experienceText").objectReferenceValue = experienceText.GetComponent<UnityEngine.UI.Text>();
                }

                rewardSO.ApplyModifiedProperties();
            }

            GameObject stageText = GameObject.Find("StageText");
            GameObject waveText = GameObject.Find("WaveText");

            if (stageText != null)
            {
                so.FindProperty("stageText").objectReferenceValue = stageText.GetComponent<UnityEngine.UI.Text>();
            }

            if (waveText != null)
            {
                so.FindProperty("waveText").objectReferenceValue = waveText.GetComponent<UnityEngine.UI.Text>();
            }

            // GameObject.Find cannot find inactive objects; panel starts inactive.
            Game.UI.BattleResultPanel panelComponent = UnityEngine.Object.FindFirstObjectByType<Game.UI.BattleResultPanel>(FindObjectsInactive.Include);
            so.FindProperty("battleResultPanel").objectReferenceValue = panelComponent;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            Debug.Log(panelComponent != null
                ? "[Setup] BattleResultPanel bound to StageBattleController."
                : "[Setup] WARNING: BattleResultPanel not found in scene.");

            BindBattleResultPanelHubScene();
        }

        private static GameObject FindOrCreate(string name, Transform parent)
        {
            if (parent == null)
            {
                GameObject existingRoot = GameObject.Find(name);
                if (existingRoot != null)
                {
                    return existingRoot;
                }

                return new GameObject(name);
            }

            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject newObj = new GameObject(name);
            newObj.transform.SetParent(parent);
            newObj.transform.localPosition = Vector3.zero;
            return newObj;
        }

        private static bool BindHeroAssets()
        {
            string basePath = ART_PATH + "/Characters/Hero/";
            string[] animNames = { "Idle", "Walk", "Attack", "Death" };
            int[] frameRates = { 6, 6, 8, 6 }; // Lowered walk from 8 to 6
            bool[] loops = { true, true, false, false };

            bool allSuccess = true;

            for (int i = 0; i < animNames.Length; i++)
            {
                string fileName = $"Hero_{animNames[i]}.png";
                string filePath = basePath + fileName;

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[Setup] Hero asset not found: {filePath}");
                    allSuccess = false;
                    continue;
                }

                // Load sprites from the texture
                Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath);
                List<Sprite> spriteList = new List<Sprite>();
                
                foreach (Object obj in sprites)
                {
                    if (obj is Sprite sprite)
                    {
                        spriteList.Add(sprite);
                    }
                }

                if (spriteList.Count == 0)
                {
                    Debug.LogWarning($"[Setup] No sprites found in {fileName}. Make sure Sprite Mode is set to Multiple and sprites are sliced.");
                    allSuccess = false;
                    continue;
                }

                // Create or update animation clip
                string clipPath = ANIMATION_PATH + $"/Hero/Hero_{animNames[i]}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                
                if (clip == null)
                {
                    clip = new AnimationClip();
                    clip.frameRate = frameRates[i];
                    AssetDatabase.CreateAsset(clip, clipPath);
                }

                // Set loop setting
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loops[i];
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                // Create sprite keyframes
                EditorCurveBinding spriteBinding = new EditorCurveBinding();
                spriteBinding.type = typeof(SpriteRenderer);
                spriteBinding.path = "";
                spriteBinding.propertyName = "m_Sprite";

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[spriteList.Count];
                for (int j = 0; j < spriteList.Count; j++)
                {
                    keyframes[j] = new ObjectReferenceKeyframe();
                    keyframes[j].time = j / (float)frameRates[i];
                    keyframes[j].value = spriteList[j];
                }

                AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
                EditorUtility.SetDirty(clip);

                Debug.Log($"[Setup] Created/Updated animation: {clipPath} with {spriteList.Count} frames");

                // Bind to animator controller
                string controllerPath = ANIMATION_PATH + "/Hero/HeroController.controller";
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                
                if (controller != null)
                {
                    AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                    AnimatorState state = FindStateByName(stateMachine, animNames[i]);
                    
                    if (state != null)
                    {
                        state.motion = clip;
                        Debug.Log($"[Setup] Bound {animNames[i]} clip to animator state");
                    }
                }
            }

            // Update hero prefab sprite renderer
            UpdatePrefabSpriteRenderer(PREFAB_PATH + "/Hero/Hero.prefab", basePath + "Hero_Idle.png");

            return allSuccess;
        }

        private static bool BindEnemyAssets()
        {
            string basePath = ART_PATH + "/Characters/Enemies/";
            string[] animNames = { "Idle", "Walk", "Attack", "Death" };
            int[] frameRates = { 6, 6, 8, 6 }; // Lowered walk from 8 to 6
            bool[] loops = { true, true, false, false };

            // Check if we should use Dog folder or Enemy_ prefix
            string dogPath = basePath + "Dog/";
            bool useDogFolder = Directory.Exists(dogPath);

            bool allSuccess = true;

            for (int i = 0; i < animNames.Length; i++)
            {
                string fileName = useDogFolder ? $"Dog_{animNames[i]}.png" : $"Enemy_{animNames[i]}.png";
                string filePath = useDogFolder ? dogPath + fileName : basePath + fileName;

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[Setup] Enemy asset not found: {filePath}");
                    allSuccess = false;
                    continue;
                }

                // Load sprites from the texture
                Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath);
                List<Sprite> spriteList = new List<Sprite>();
                
                foreach (Object obj in sprites)
                {
                    if (obj is Sprite sprite)
                    {
                        spriteList.Add(sprite);
                    }
                }

                if (spriteList.Count == 0)
                {
                    Debug.LogWarning($"[Setup] No sprites found in {fileName}. Make sure Sprite Mode is set to Multiple and sprites are sliced.");
                    allSuccess = false;
                    continue;
                }

                // Create or update animation clip
                string clipPath = ANIMATION_PATH + $"/Enemies/Enemy_{animNames[i]}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                
                if (clip == null)
                {
                    clip = new AnimationClip();
                    clip.frameRate = frameRates[i];
                    AssetDatabase.CreateAsset(clip, clipPath);
                }

                // Set loop setting
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loops[i];
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                // Create sprite keyframes
                EditorCurveBinding spriteBinding = new EditorCurveBinding();
                spriteBinding.type = typeof(SpriteRenderer);
                spriteBinding.path = "";
                spriteBinding.propertyName = "m_Sprite";

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[spriteList.Count];
                for (int j = 0; j < spriteList.Count; j++)
                {
                    keyframes[j] = new ObjectReferenceKeyframe();
                    keyframes[j].time = j / (float)frameRates[i];
                    keyframes[j].value = spriteList[j];
                }

                AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
                EditorUtility.SetDirty(clip);

                Debug.Log($"[Setup] Created/Updated animation: {clipPath} with {spriteList.Count} frames");

                // Bind to animator controller
                string controllerPath = ANIMATION_PATH + "/Enemies/EnemyController.controller";
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                
                if (controller != null)
                {
                    AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                    AnimatorState state = FindStateByName(stateMachine, animNames[i]);
                    
                    if (state != null)
                    {
                        state.motion = clip;
                        Debug.Log($"[Setup] Bound {animNames[i]} clip to animator state");
                    }
                }
            }

            // Update enemy prefab sprite renderer
            string idleSpritePath = useDogFolder ? dogPath + "Dog_Idle.png" : basePath + "Enemy_Idle.png";
            string prefabPathToUpdate = useDogFolder ? PREFAB_PATH + "/Enemies/Enemy_Dog.prefab" : PREFAB_PATH + "/Enemies/Enemy_MossSkeleton.prefab";
            UpdatePrefabSpriteRenderer(prefabPathToUpdate, idleSpritePath);

            return allSuccess;
        }

        private static bool BindBossAssets()
        {
            EnsureFolder(ANIMATION_PATH + "/Bosses");
            EnsureFolder(PREFAB_PATH + "/Bosses");

            CreateBossAnimatorController();
            CreateBossPrefab();

            string basePath = ART_PATH + "/Characters/Bosses/";
            string[] animNames = { "Idle", "Walk", "Attack", "Death" };
            string[] fileNames = { "boss1Idle.png", "boss1Walk.png", "boss1Attack.png", "boss1Death.png" };
            int[] frameRates = { 6, 6, 8, 6 };
            bool[] loops = { true, true, false, false };

            bool allSuccess = true;

            for (int i = 0; i < animNames.Length; i++)
            {
                string filePath = basePath + fileNames[i];

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[Setup] Boss asset not found: {filePath}");
                    allSuccess = false;
                    continue;
                }

                Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath);
                List<Sprite> spriteList = new List<Sprite>();

                foreach (Object obj in sprites)
                {
                    if (obj is Sprite sprite)
                    {
                        spriteList.Add(sprite);
                    }
                }

                if (spriteList.Count == 0)
                {
                    Debug.LogWarning($"[Setup] No sprites found in {fileNames[i]}. Set Sprite Mode to Multiple and slice frames.");
                    allSuccess = false;
                    continue;
                }

                string clipPath = ANIMATION_PATH + $"/Bosses/Boss_{animNames[i]}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

                if (clip == null)
                {
                    clip = new AnimationClip();
                    clip.frameRate = frameRates[i];
                    AssetDatabase.CreateAsset(clip, clipPath);
                }

                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loops[i];
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                EditorCurveBinding spriteBinding = new EditorCurveBinding();
                spriteBinding.type = typeof(SpriteRenderer);
                spriteBinding.path = "";
                spriteBinding.propertyName = "m_Sprite";

                ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[spriteList.Count];
                for (int j = 0; j < spriteList.Count; j++)
                {
                    keyframes[j] = new ObjectReferenceKeyframe();
                    keyframes[j].time = j / (float)frameRates[i];
                    keyframes[j].value = spriteList[j];
                }

                AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
                EditorUtility.SetDirty(clip);

                Debug.Log($"[Setup] Created/Updated boss animation: {clipPath} with {spriteList.Count} frames");

                string controllerPath = ANIMATION_PATH + "/Bosses/BossController.controller";
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

                if (controller != null)
                {
                    AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                    AnimatorState state = FindStateByName(stateMachine, animNames[i]);

                    if (state != null)
                    {
                        state.motion = clip;
                        Debug.Log($"[Setup] Bound boss {animNames[i]} clip to animator state");
                    }
                }
            }

            UpdatePrefabSpriteRenderer(PREFAB_PATH + "/Bosses/Enemy_Boss1.prefab", basePath + "boss1Idle.png", flipX: true);
            return allSuccess;
        }

        private static void CreateSampleEnemyData()
        {
            string normalPath = SCRIPTABLE_PATH + "/Enemies/Enemy_Dog.asset";
            string bossPath = SCRIPTABLE_PATH + "/Enemies/Enemy_Boss1.asset";

            string dogPrefabPath = PREFAB_PATH + "/Enemies/Enemy_Dog.prefab";
            string bossPrefabPath = PREFAB_PATH + "/Bosses/Enemy_Boss1.prefab";

            GameObject dogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dogPrefabPath);
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bossPrefabPath);

            if (bossPrefab == null)
            {
                CreateBossAnimatorController();
                CreateBossPrefab();
                bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bossPrefabPath);
            }

            if (!File.Exists(normalPath))
            {
                Game.Data.EnemyData normalEnemy = ScriptableObject.CreateInstance<Game.Data.EnemyData>();
                normalEnemy.enemyId = "dog";
                normalEnemy.displayName = "Swamp Dog";
                normalEnemy.maxHealth = 40;
                normalEnemy.attackDamage = 4;
                normalEnemy.moveSpeed = 1.5f;
                normalEnemy.attackRange = 1.5f;
                normalEnemy.attackCooldown = 1.6f;
                normalEnemy.attackHitDelay = 0.35f;
                normalEnemy.deathAnimationDuration = 1f;
                normalEnemy.goldReward = 5;
                normalEnemy.experienceReward = 15;
                normalEnemy.chestDropChance = 0.1f;
                normalEnemy.isBoss = false;

                if (dogPrefab != null)
                {
                    normalEnemy.prefab = dogPrefab.GetComponent<Game.Units.EnemyUnit>();
                }

                AssetDatabase.CreateAsset(normalEnemy, normalPath);
                Debug.Log($"[Setup] Created enemy data: {normalPath}");
            }

            Game.Data.EnemyData bossEnemy = AssetDatabase.LoadAssetAtPath<Game.Data.EnemyData>(bossPath);
            bool isNewBoss = bossEnemy == null;

            if (isNewBoss)
            {
                bossEnemy = ScriptableObject.CreateInstance<Game.Data.EnemyData>();
            }

            bossEnemy.enemyId = "boss_1";
            bossEnemy.displayName = "Swamp Boss";
            bossEnemy.maxHealth = 180;
            bossEnemy.attackDamage = 10;
            bossEnemy.moveSpeed = 1.1f;
            bossEnemy.attackRange = 1.7f;
            bossEnemy.attackCooldown = 2.0f;
            bossEnemy.attackHitDelay = 0.5f;
            bossEnemy.deathAnimationDuration = 1.5f;
            bossEnemy.goldReward = 50;
            bossEnemy.experienceReward = 100;
            bossEnemy.chestDropChance = 1f;
            bossEnemy.isBoss = true;

            if (bossPrefab != null)
            {
                bossEnemy.prefab = bossPrefab.GetComponent<Game.Units.EnemyUnit>();
            }

            if (isNewBoss)
            {
                AssetDatabase.CreateAsset(bossEnemy, bossPath);
                Debug.Log($"[Setup] Created boss data: {bossPath}");
            }
            else
            {
                EditorUtility.SetDirty(bossEnemy);
                Debug.Log($"[Setup] Updated boss data: {bossPath}");
            }
        }

        private static void CreateSampleStageDataAsset()
        {
            string stagePath = SCRIPTABLE_PATH + "/Stages/Stage_01.asset";

            Game.Data.EnemyData normalEnemy = AssetDatabase.LoadAssetAtPath<Game.Data.EnemyData>(SCRIPTABLE_PATH + "/Enemies/Enemy_Dog.asset");
            Game.Data.EnemyData bossEnemy = AssetDatabase.LoadAssetAtPath<Game.Data.EnemyData>(SCRIPTABLE_PATH + "/Enemies/Enemy_Boss1.asset");

            if (bossEnemy == null)
            {
                bossEnemy = AssetDatabase.LoadAssetAtPath<Game.Data.EnemyData>(SCRIPTABLE_PATH + "/Enemies/Enemy_DogBoss.asset");
            }

            if (normalEnemy == null)
            {
                Debug.LogError("[Setup] Enemy_Dog.asset not found. Create sample enemy data first.");
                return;
            }

            Game.Data.WaveData wave1 = new Game.Data.WaveData
            {
                waveName = "Wave 1",
                delayBeforeWave = 0.5f,
                delayBetweenEnemies = 1f,
                simultaneousSpawnCount = 1,
                enemies = new List<Game.Data.EnemyData> { normalEnemy, normalEnemy, normalEnemy, normalEnemy }
            };

            Game.Data.WaveData wave2 = new Game.Data.WaveData
            {
                waveName = "Wave 2",
                delayBeforeWave = 0.5f,
                delayBetweenEnemies = 0.8f,
                simultaneousSpawnCount = 1,
                enemies = new List<Game.Data.EnemyData> { normalEnemy, normalEnemy, normalEnemy, normalEnemy, normalEnemy }
            };

            Game.Data.WaveData wave3 = new Game.Data.WaveData
            {
                waveName = "Wave 3",
                delayBeforeWave = 0.5f,
                delayBetweenEnemies = 0.8f,
                simultaneousSpawnCount = 2,
                enemies = new List<Game.Data.EnemyData> { normalEnemy, normalEnemy, normalEnemy, normalEnemy, normalEnemy, normalEnemy }
            };

            Game.Data.WaveData bossWave = new Game.Data.WaveData
            {
                waveName = "Boss Wave",
                delayBeforeWave = 1.5f,
                delayBetweenEnemies = 0f,
                simultaneousSpawnCount = 1,
                enemies = bossEnemy != null
                    ? new List<Game.Data.EnemyData> { bossEnemy }
                    : new List<Game.Data.EnemyData> { normalEnemy }
            };

            Game.Data.StageData stage = AssetDatabase.LoadAssetAtPath<Game.Data.StageData>(stagePath);
            bool isNew = stage == null;

            if (isNew)
            {
                stage = ScriptableObject.CreateInstance<Game.Data.StageData>();
            }

            stage.stageId = "stage_01";
            stage.displayName = "Swamp Outskirts";
            stage.stageIndex = 1;
            stage.travelDuration = 3f;
            stage.delayAfterTravel = 1f;
            stage.completionGoldBonus = 50;
            stage.completionExperienceBonus = 100;
            stage.waves = new List<Game.Data.WaveData> { wave1, wave2, wave3, bossWave };

            if (isNew)
            {
                AssetDatabase.CreateAsset(stage, stagePath);
                Debug.Log($"[Setup] Created stage data: {stagePath}");
            }
            else
            {
                EditorUtility.SetDirty(stage);
                Debug.Log($"[Setup] Updated stage data with 3 waves + boss: {stagePath}");
            }

            // Single-wave boss encounter used by chapter bosses + layer boss.
            string bossOnlyPath = SCRIPTABLE_PATH + "/Stages/Stage_BossOnly.asset";
            Game.Data.StageData bossOnly = AssetDatabase.LoadAssetAtPath<Game.Data.StageData>(bossOnlyPath);
            bool bossOnlyNew = bossOnly == null;
            if (bossOnlyNew)
            {
                bossOnly = ScriptableObject.CreateInstance<Game.Data.StageData>();
            }

            bossOnly.stageId = "stage_boss_only";
            bossOnly.displayName = "Boss Encounter";
            bossOnly.stageIndex = 99;
            bossOnly.travelDuration = 2f;
            bossOnly.delayAfterTravel = 0.5f;
            bossOnly.completionGoldBonus = 80;
            bossOnly.completionExperienceBonus = 150;
            bossOnly.waves = new List<Game.Data.WaveData> { bossWave };

            if (bossOnlyNew)
            {
                AssetDatabase.CreateAsset(bossOnly, bossOnlyPath);
                Debug.Log($"[Setup] Created boss-only stage: {bossOnlyPath}");
            }
            else
            {
                EditorUtility.SetDirty(bossOnly);
                Debug.Log($"[Setup] Updated boss-only stage: {bossOnlyPath}");
            }
        }

        private static void ValidateScene(List<string> errors, List<string> warnings)
        {
            Debug.Log("\n--- Validating Scene ---");

            string hubPath = SCENE_PATH + "/VillageHub.unity";
            if (!File.Exists(hubPath))
            {
                warnings.Add("VillageHub scene not found (run Tools/Idle RPG/Create Village Hub)");
            }

            string scenePath = SCENE_PATH + "/BattleDemo.unity";
            if (!File.Exists(scenePath))
            {
                errors.Add("BattleDemo scene not found");
                return;
            }

            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            Camera cam = Camera.main;
            if (cam == null)
            {
                errors.Add("Main Camera not found");
            }
            else if (!cam.orthographic)
            {
                warnings.Add("Main Camera is not orthographic");
            }

            GameObject battleRoot = GameObject.Find("BattleRoot");
            if (battleRoot == null)
            {
                errors.Add("BattleRoot not found");
            }

            ValidateGameObject("HeroStartPoint", errors);
            ValidateGameObject("HeroCombatPoint", errors);
            ValidateGameObject("EnemySpawnPoint", errors);
            ValidateGameObject("EnemyCombatPoint", errors);

            GameObject hero = GameObject.Find("Hero");
            if (hero == null)
            {
                errors.Add("Hero not found in scene");
            }
            else
            {
                ValidateComponent<Game.Units.HeroUnit>(hero, "Hero", errors);
                ValidateComponent<Game.Combat.Health>(hero, "Hero", errors);
                ValidateComponent<Animator>(hero, "Hero", errors);
            }

            Debug.Log("✓ Scene validation complete");
        }

        private static void ValidatePrefabs(List<string> errors, List<string> warnings)
        {
            Debug.Log("\n--- Validating Prefabs ---");

            string heroPrefabPath = PREFAB_PATH + "/Hero/Hero.prefab";
            if (!File.Exists(heroPrefabPath))
            {
                errors.Add("Hero prefab not found");
            }
            else
            {
                GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(heroPrefabPath);
                if (heroPrefab != null)
                {
                    ValidateComponent<Game.Units.HeroUnit>(heroPrefab, "Hero Prefab", errors);
                    ValidateComponent<Game.Combat.Health>(heroPrefab, "Hero Prefab", errors);
                    ValidateComponent<Animator>(heroPrefab, "Hero Prefab", errors);
                }
            }

            string enemyPrefabPath = PREFAB_PATH + "/Enemies/Enemy_Dog.prefab";
            if (!File.Exists(enemyPrefabPath))
            {
                enemyPrefabPath = PREFAB_PATH + "/Enemies/Enemy_MossSkeleton.prefab";
            }
            
            if (!File.Exists(enemyPrefabPath))
            {
                errors.Add("Enemy prefab not found (neither Dog nor MossSkeleton)");
            }
            else
            {
                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
                if (enemyPrefab != null)
                {
                    ValidateComponent<Game.Units.EnemyUnit>(enemyPrefab, "Enemy Prefab", errors);
                    ValidateComponent<Game.Combat.Health>(enemyPrefab, "Enemy Prefab", errors);
                    ValidateComponent<Animator>(enemyPrefab, "Enemy Prefab", errors);
                }
            }

            Debug.Log("✓ Prefab validation complete");
        }

        private static void ValidateAnimators(List<string> errors, List<string> warnings)
        {
            Debug.Log("\n--- Validating Animators ---");

            string heroControllerPath = ANIMATION_PATH + "/Hero/HeroController.controller";
            if (!File.Exists(heroControllerPath))
            {
                errors.Add("Hero AnimatorController not found");
            }
            else
            {
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(heroControllerPath);
                if (controller != null)
                {
                    ValidateAnimatorParameter(controller, "IsWalking", AnimatorControllerParameterType.Bool, errors);
                    ValidateAnimatorParameter(controller, "Attack", AnimatorControllerParameterType.Trigger, errors);
                    ValidateAnimatorParameter(controller, "IsDead", AnimatorControllerParameterType.Bool, errors);
                }
            }

            string enemyControllerPath = ANIMATION_PATH + "/Enemies/EnemyController.controller";
            if (!File.Exists(enemyControllerPath))
            {
                errors.Add("Enemy AnimatorController not found");
            }
            else
            {
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(enemyControllerPath);
                if (controller != null)
                {
                    ValidateAnimatorParameter(controller, "IsWalking", AnimatorControllerParameterType.Bool, errors);
                    ValidateAnimatorParameter(controller, "Attack", AnimatorControllerParameterType.Trigger, errors);
                    ValidateAnimatorParameter(controller, "IsDead", AnimatorControllerParameterType.Bool, errors);
                }
            }

            Debug.Log("✓ Animator validation complete");
        }

        private static void ValidateData(List<string> errors, List<string> warnings)
        {
            Debug.Log("\n--- Validating Data ---");

            GameObject battleController = GameObject.Find("BattleController");
            if (battleController != null)
            {
                Game.Stage.StageBattleController controller = battleController.GetComponent<Game.Stage.StageBattleController>();
                if (controller != null)
                {
                    SerializedObject so = new SerializedObject(controller);

                    if (so.FindProperty("stageData").objectReferenceValue == null)
                    {
                        warnings.Add("StageBattleController: stageData not assigned");
                    }

                    if (so.FindProperty("hero").objectReferenceValue == null)
                    {
                        errors.Add("StageBattleController: hero not assigned");
                    }

                    if (so.FindProperty("parallaxScroller").objectReferenceValue == null)
                    {
                        warnings.Add("StageBattleController: parallaxScroller not assigned");
                    }

                    if (so.FindProperty("rewardService").objectReferenceValue == null)
                    {
                        warnings.Add("StageBattleController: rewardService not assigned");
                    }
                }
                else
                {
                    errors.Add("BattleController does not have StageBattleController component");
                }
            }
            else
            {
                errors.Add("BattleController not found in scene");
            }

            Debug.Log("✓ Data validation complete");
        }

        private static void ValidateGameObject(string name, List<string> errors)
        {
            if (GameObject.Find(name) == null)
            {
                errors.Add($"GameObject '{name}' not found");
            }
        }

        private static void ValidateComponent<T>(GameObject obj, string context, List<string> errors) where T : Component
        {
            if (obj.GetComponent<T>() == null)
            {
                errors.Add($"{context}: Missing {typeof(T).Name} component");
            }
        }

        private static void ValidateAnimatorParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType expectedType, List<string> errors)
        {
            bool found = false;
            foreach (var param in controller.parameters)
            {
                if (param.name == paramName)
                {
                    found = true;
                    if (param.type != expectedType)
                    {
                        errors.Add($"Animator parameter '{paramName}' has wrong type. Expected: {expectedType}, Found: {param.type}");
                    }
                    break;
                }
            }

            if (!found)
            {
                errors.Add($"Animator parameter '{paramName}' not found");
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[Setup] Deleted: {path}");
            }
        }

        private static AnimatorState FindStateByName(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState state in stateMachine.states)
            {
                if (state.state.name == stateName)
                {
                    return state.state;
                }
            }
            return null;
        }

        private static void UpdatePrefabSpriteRenderer(string prefabPath, string spritePath, bool flipX = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Setup] Prefab not found: {prefabPath}");
                return;
            }

            // Load the first sprite from the sprite sheet
            Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            Sprite firstSprite = null;
            
            foreach (Object obj in sprites)
            {
                if (obj is Sprite sprite)
                {
                    firstSprite = sprite;
                    break;
                }
            }

            if (firstSprite == null)
            {
                Debug.LogWarning($"[Setup] No sprite found in: {spritePath}");
                return;
            }

            // Update prefab
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
            
            if (sr != null)
            {
                sr.sprite = firstSprite;
                sr.color = Color.white;
                sr.flipX = flipX;
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                Debug.Log($"[Setup] Updated sprite in prefab: {prefabPath} (flipX={flipX})");
            }

            DestroyImmediate(instance);
        }
    }
}
