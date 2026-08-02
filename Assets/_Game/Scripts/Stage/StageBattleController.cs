using System.Collections;
using UnityEngine;

namespace Game.Stage
{
    public class StageBattleController : MonoBehaviour
    {
        [Header("Stage Data")]
        [SerializeField] private Game.Data.StageData stageData;
        [SerializeField] private Game.Data.MapCatalog mapCatalog;

        [Header("Hero")]
        [SerializeField] private Game.Units.HeroUnit hero;
        [SerializeField] private float partyMemberSpacing = 1.35f;

        [Header("Battle Points")]
        [SerializeField] private Transform heroStartPoint;
        [SerializeField] private Transform heroCombatPoint;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Transform enemyCombatPoint;

        [Header("Runtime Containers")]
        [SerializeField] private Transform runtimeEnemiesRoot;
        [SerializeField] private Transform runtimePartyRoot;

        [Header("Systems")]
        [SerializeField] private Game.Environment.ParallaxScroller parallaxScroller;
        [SerializeField] private Game.Rewards.RewardService rewardService;

        [Header("UI")]
        [SerializeField] private UnityEngine.UI.Text stageText;
        [SerializeField] private UnityEngine.UI.Text waveText;
        [SerializeField] private Game.UI.BattleResultPanel battleResultPanel;
        [SerializeField] private float victoryAutoContinueDelay = 1.8f;
        [SerializeField] private string hubSceneName = Game.Core.GameScenes.Hub;

        private BattleState currentState = BattleState.None;
        private int currentWaveIndex = 0;
        private System.Collections.Generic.List<Game.Units.EnemyUnit> activeEnemies = new System.Collections.Generic.List<Game.Units.EnemyUnit>();
        private readonly System.Collections.Generic.List<Game.Units.HeroUnit> partyHeroes = new System.Collections.Generic.List<Game.Units.HeroUnit>();
        private Coroutine battleFlowCoroutine;
        private bool isHeroDead = false;
        private float enemySpawnAheadX = 7f;

        private void Start()
        {
            Game.UI.Faz6RuntimeBootstrap.EnsureRosterService();
            Game.UI.Faz6RuntimeBootstrap.EnsureWeeklyService();
            ResolveBattleContentFromSession();

            if (stageData == null)
            {
                Debug.LogError("[StageBattleController] StageData is not assigned.", this);
                return;
            }

            if (hero == null)
            {
                Debug.LogError("[StageBattleController] Hero is not assigned.", this);
                return;
            }

            SetupPartyFromRoster();

            for (int i = 0; i < partyHeroes.Count; i++)
            {
                Game.Units.HeroUnit member = partyHeroes[i];
                if (member != null && member.Health != null)
                {
                    member.Health.Died += OnPartyMemberDied;
                }
            }

            if (battleResultPanel == null)
            {
                battleResultPanel = FindFirstObjectByType<Game.UI.BattleResultPanel>(FindObjectsInactive.Include);
                if (battleResultPanel == null)
                {
                    Debug.LogError("[StageBattleController] BattleResultPanel is not assigned and could not be found in the scene.", this);
                }
            }

            if (battleResultPanel != null)
            {
                battleResultPanel.gameObject.SetActive(false);
            }

            CacheSpawnOffset();
            BindCameraFollow();
            StartBattle();
        }

        private void ResolveBattleContentFromSession()
        {
            if (mapCatalog == null)
            {
                mapCatalog = Game.Core.BattleSession.ActiveCatalog;
            }

            if (mapCatalog == null)
            {
                Game.Inventory.ChestService chest = FindFirstObjectByType<Game.Inventory.ChestService>();
                if (chest != null)
                {
                    mapCatalog = chest.MapCatalog;
                }
            }

            if (mapCatalog != null)
            {
                Game.Core.BattleSession.SetCatalog(mapCatalog);
            }

            // Recover map node if static BattleSession was wiped across scene load.
            if (!Game.Core.BattleSession.IsWeeklyDungeonBattle
                && Game.Core.BattleSession.SelectedLevel == null
                && !Game.Core.BattleSession.IsLayerBossBattle)
            {
                if (Game.Core.BattleSession.TryRestoreFromProgress(mapCatalog))
                {
                    Debug.Log("[StageBattleController] Restored SelectedLevel from MapProgress.active*.");
                }
            }

            Game.Data.StageData resolved = Game.Core.BattleSession.ResolveBattleContent(stageData);
            if (resolved != null)
            {
                stageData = resolved;
            }

            // Chapter / layer boss fights must be single-wave boss encounters.
            EnsureBossOnlyWavesIfNeeded();
            ApplyWeeklyDungeonBossOverride();

            Debug.Log(
                $"[StageBattleController] Battle content: {Game.Core.BattleSession.ResolveDisplayName(stageData != null ? stageData.displayName : "null")} " +
                $"| SelectedLevel={(Game.Core.BattleSession.SelectedLevel != null ? Game.Core.BattleSession.SelectedLevel.levelId : "null")} " +
                $"| Weekly={Game.Core.BattleSession.IsWeeklyDungeonBattle} " +
                $"| Catalog={(mapCatalog != null ? mapCatalog.name : "null")} " +
                $"| Waves={(stageData != null && stageData.waves != null ? stageData.waves.Count : 0)}");
        }

        private void ApplyWeeklyDungeonBossOverride()
        {
            if (!Game.Core.BattleSession.IsWeeklyDungeonBattle || stageData == null)
            {
                return;
            }

            Game.Data.EnemyData boss = Game.Core.BattleSession.SelectedWeeklyBoss;
            if (boss == null)
            {
                return;
            }

            Game.Data.WaveData bossWave = new Game.Data.WaveData
            {
                waveName = "Weekly Boss",
                enemies = new System.Collections.Generic.List<Game.Data.EnemyData> { boss },
                delayBeforeWave = 0.5f,
                delayBetweenEnemies = 0.5f,
                simultaneousSpawnCount = 1
            };

            Game.Data.StageData runtimeStage = ScriptableObject.CreateInstance<Game.Data.StageData>();
            runtimeStage.stageId = (stageData.stageId ?? "weekly") + "_runtime";
            runtimeStage.displayName = Game.Core.BattleSession.ResolveDisplayName(stageData.displayName);
            runtimeStage.stageIndex = stageData.stageIndex;
            runtimeStage.travelDuration = stageData.travelDuration;
            runtimeStage.delayAfterTravel = stageData.delayAfterTravel;
            runtimeStage.completionGoldBonus = Mathf.Max(stageData.completionGoldBonus, 80);
            runtimeStage.completionExperienceBonus = Mathf.Max(stageData.completionExperienceBonus, 120);
            runtimeStage.waves = new System.Collections.Generic.List<Game.Data.WaveData> { bossWave };
            stageData = runtimeStage;
            Debug.Log($"[StageBattleController] Weekly dungeon boss override: {boss.displayName}");
        }

        private void EnsureBossOnlyWavesIfNeeded()
        {
            if (stageData == null || stageData.waves == null || stageData.waves.Count <= 1)
            {
                return;
            }

            bool isChapterBoss = Game.Core.BattleSession.SelectedLevel != null
                && Game.Core.BattleSession.SelectedLevel.isChapterBoss;
            bool isLayerBoss = Game.Core.BattleSession.IsLayerBossBattle;
            if (!isChapterBoss && !isLayerBoss)
            {
                return;
            }

            // Keep only the last wave (boss wave) for this run — does not mutate the asset.
            Game.Data.WaveData bossWave = stageData.waves[stageData.waves.Count - 1];
            Game.Data.StageData runtimeStage = ScriptableObject.CreateInstance<Game.Data.StageData>();
            runtimeStage.stageId = stageData.stageId + "_boss_only";
            runtimeStage.displayName = stageData.displayName;
            runtimeStage.stageIndex = stageData.stageIndex;
            runtimeStage.travelDuration = stageData.travelDuration;
            runtimeStage.delayAfterTravel = stageData.delayAfterTravel;
            runtimeStage.completionGoldBonus = stageData.completionGoldBonus;
            runtimeStage.completionExperienceBonus = stageData.completionExperienceBonus;
            runtimeStage.waves = new System.Collections.Generic.List<Game.Data.WaveData> { bossWave };
            stageData = runtimeStage;
            Debug.Log("[StageBattleController] Boss fight trimmed to single wave.");
        }

        private void BindCameraFollow()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[StageBattleController] Main Camera missing; cannot bind follow.");
                return;
            }

            Game.Environment.CameraFollow2D follow = cam.GetComponent<Game.Environment.CameraFollow2D>();
            if (follow == null)
            {
                follow = cam.gameObject.AddComponent<Game.Environment.CameraFollow2D>();
            }

            follow.SetTarget(hero.transform);
        }

        private void SetupPartyFromRoster()
        {
            partyHeroes.Clear();
            Game.Roster.RosterService roster = FindFirstObjectByType<Game.Roster.RosterService>();
            System.Collections.Generic.List<string> classIds = roster != null
                ? roster.GetFilledPartyClassIds()
                : new System.Collections.Generic.List<string>();

            if (classIds.Count == 0)
            {
                classIds.Add("fighter");
            }

            // Lead = scene hero
            ConfigurePartyMember(hero, 0, classIds[0], isLead: true);
            partyHeroes.Add(hero);

            Transform parent = runtimePartyRoot != null
                ? runtimePartyRoot
                : (hero != null ? hero.transform.parent : transform);

            for (int i = 1; i < classIds.Count; i++)
            {
                Game.Units.HeroUnit clone = Instantiate(hero, parent);
                clone.name = $"Hero_Party_{i}";
                Vector3 pos = hero.transform.position;
                pos.x -= partyMemberSpacing * i;
                clone.transform.position = pos;
                ConfigurePartyMember(clone, i, classIds[i], isLead: false);
                partyHeroes.Add(clone);
            }

            Debug.Log($"[StageBattleController] Party size={partyHeroes.Count}");
        }

        private void ConfigurePartyMember(Game.Units.HeroUnit unit, int slot, string classId, bool isLead)
        {
            if (unit == null)
            {
                return;
            }

            Game.Units.PartyMember member = unit.GetComponent<Game.Units.PartyMember>();
            if (member == null)
            {
                member = unit.gameObject.AddComponent<Game.Units.PartyMember>();
            }

            member.Configure(slot, classId, isLead);

            Game.Units.HeroProgression progression = unit.GetComponent<Game.Units.HeroProgression>();
            if (progression != null)
            {
                progression.enabled = true;
                int level = 1;
                if (rewardService != null && rewardService.Progress != null)
                {
                    level = Mathf.Max(1, rewardService.Progress.level);
                }
                else
                {
                    Game.Core.RuntimePlayerState.EnsureInitialized();
                    level = Mathf.Max(1, Game.Core.RuntimePlayerState.Progress.level);
                }

                progression.ApplyLevel(level, isLevelUp: false, restoreHealthOnApply: true);
            }
        }

        private Game.Units.HeroUnit GetLeadHero()
        {
            for (int i = 0; i < partyHeroes.Count; i++)
            {
                Game.Units.HeroUnit h = partyHeroes[i];
                if (h != null && h.Health != null && !h.Health.IsDead)
                {
                    return h;
                }
            }

            return hero;
        }

        private bool IsEntirePartyDead()
        {
            for (int i = 0; i < partyHeroes.Count; i++)
            {
                Game.Units.HeroUnit h = partyHeroes[i];
                if (h != null && h.Health != null && !h.Health.IsDead)
                {
                    return false;
                }
            }

            return partyHeroes.Count > 0;
        }

        private void CacheSpawnOffset()
        {
            if (enemySpawnPoint == null)
            {
                enemySpawnAheadX = 7f;
                return;
            }

            // Prefer distance from combat stance → spawn so later waves stay ahead of the hero
            if (heroCombatPoint != null)
            {
                enemySpawnAheadX = Mathf.Abs(enemySpawnPoint.position.x - heroCombatPoint.position.x);
            }
            else if (heroStartPoint != null)
            {
                enemySpawnAheadX = Mathf.Abs(enemySpawnPoint.position.x - heroStartPoint.position.x);
            }
            else
            {
                enemySpawnAheadX = 7f;
            }

            enemySpawnAheadX = Mathf.Max(3f, enemySpawnAheadX);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < partyHeroes.Count; i++)
            {
                Game.Units.HeroUnit member = partyHeroes[i];
                if (member != null && member.Health != null)
                {
                    member.Health.Died -= OnPartyMemberDied;
                }
            }

            CleanupAllEnemies();
        }

        private void StartBattle()
        {
            currentWaveIndex = 0;
            isHeroDead = false;

            for (int i = 0; i < partyHeroes.Count; i++)
            {
                Game.Units.HeroUnit member = partyHeroes[i];
                if (member == null)
                {
                    continue;
                }

                member.LockGroundY(member.transform.position.y);
                member.SetMovementEnabled(true);
                member.ClearTarget();
            }

            Game.Environment.CameraFollow2D follow = Camera.main != null
                ? Camera.main.GetComponent<Game.Environment.CameraFollow2D>()
                : null;
            if (follow != null)
            {
                follow.SnapToTarget();
            }

            UpdateUI();

            battleFlowCoroutine = StartCoroutine(BattleFlowCoroutine());
        }

        private IEnumerator BattleFlowCoroutine()
        {
            for (currentWaveIndex = 0; currentWaveIndex < stageData.waves.Count; currentWaveIndex++)
            {
                if (isHeroDead)
                {
                    yield break;
                }

                Game.Data.WaveData wave = stageData.waves[currentWaveIndex];

                currentState = BattleState.Preparing;
                UpdateUI();

                if (wave.delayBeforeWave > 0)
                {
                    yield return new WaitForSeconds(wave.delayBeforeWave);
                }

                // Spawn enemies in batches
                int enemiesSpawned = 0;
                int spawnCount = Mathf.Max(1, wave.simultaneousSpawnCount);

                while (enemiesSpawned < wave.enemies.Count && !isHeroDead)
                {
                    // Spawn batch
                    int enemiesToSpawnNow = Mathf.Min(spawnCount, wave.enemies.Count - enemiesSpawned);
                    
                    for (int i = 0; i < enemiesToSpawnNow; i++)
                    {
                        if (isHeroDead) break;

                        Game.Data.EnemyData enemyData = wave.enemies[enemiesSpawned + i];

                        if (enemyData == null || enemyData.prefab == null)
                        {
                            Debug.LogError($"[StageBattleController] Enemy data or prefab is null at wave {currentWaveIndex}, enemy {enemiesSpawned + i}.", this);
                            continue;
                        }

                        SpawnEnemy(enemyData, i, enemiesToSpawnNow);
                    }

                    enemiesSpawned += enemiesToSpawnNow;

                    // Wait for all active enemies to die
                    yield return new WaitUntil(() => AreAllEnemiesDead() || isHeroDead);

                    if (isHeroDead)
                    {
                        yield break;
                    }

                    // Cleanup dead enemies
                    CleanupDeadEnemies();

                    // Delay before next batch
                    if (enemiesSpawned < wave.enemies.Count && wave.delayBetweenEnemies > 0)
                    {
                        yield return new WaitForSeconds(wave.delayBetweenEnemies);
                    }
                }

                currentState = BattleState.WaveCompleted;

                if (currentWaveIndex < stageData.waves.Count - 1)
                {
                    yield return StartCoroutine(TravelToNextWave());
                }
            }

            currentState = BattleState.StageCompleted;
            OnStageCompleted();
        }

        private void SpawnEnemy(Game.Data.EnemyData enemyData, int indexInBatch, int totalInBatch)
        {
            currentState = BattleState.Spawning;

            // Spawn ahead of the hero's current X so wave transitions never yank the hero backward
            Vector3 spawnPos = GetEnemySpawnPosition();
            if (totalInBatch > 1)
            {
                float horizontalOffset = (indexInBatch - (totalInBatch - 1) * 0.5f) * 1.5f;
                spawnPos.x += horizontalOffset;

                float verticalOffset = (indexInBatch % 2 == 0) ? 0.2f : -0.2f;
                spawnPos.y += verticalOffset;
            }

            Game.Units.EnemyUnit enemy = Instantiate(enemyData.prefab, spawnPos, Quaternion.identity, runtimeEnemiesRoot);
            enemy.Configure(enemyData);
            enemy.SetTarget(GetLeadHero());

            activeEnemies.Add(enemy);

            if (enemy.Health != null)
            {
                enemy.Health.Died += OnAnyEnemyDied;
            }

            UpdateHeroTarget();

            currentState = BattleState.Combat;
        }

        private Vector3 GetEnemySpawnPosition()
        {
            Game.Units.HeroUnit lead = GetLeadHero();
            float spawnY = enemySpawnPoint != null
                ? enemySpawnPoint.position.y
                : (lead != null ? lead.transform.position.y : 0f);

            float spawnZ = enemySpawnPoint != null
                ? enemySpawnPoint.position.z
                : 0f;

            float heroX = lead != null ? lead.transform.position.x : 0f;
            return new Vector3(heroX + enemySpawnAheadX, spawnY, spawnZ);
        }

        private bool AreAllEnemiesDead()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && enemy.Health != null && !enemy.Health.IsDead)
                {
                    return false;
                }
            }
            return true;
        }

        private void CleanupDeadEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = activeEnemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    if (enemy != null)
                    {
                        // Unsubscribe from death event
                        if (enemy.Health != null)
                        {
                            enemy.Health.Died -= OnAnyEnemyDied;
                        }

                        if (rewardService != null && enemy.EnemyData != null)
                        {
                            rewardService.GrantEnemyRewards(enemy.EnemyData);
                        }

                        Destroy(enemy.gameObject);
                    }
                    activeEnemies.RemoveAt(i);
                }
            }

            UpdateHeroTarget();
        }

        private void OnAnyEnemyDied()
        {
            // Update hero target immediately when any enemy dies
            UpdateHeroTarget();
        }

        private void UpdateHeroTarget()
        {
            for (int i = 0; i < partyHeroes.Count; i++)
            {
                Game.Units.HeroUnit member = partyHeroes[i];
                if (member == null || member.Health == null || member.Health.IsDead)
                {
                    continue;
                }

                Game.Units.EnemyUnit closest = GetClosestEnemyTo(member);
                if (closest != null)
                {
                    member.SetTarget(closest);
                }
                else
                {
                    member.ClearTarget();
                }
            }

            RetargetEnemiesToLivingHeroes();
        }

        private Game.Units.EnemyUnit GetClosestEnemyTo(Game.Units.HeroUnit fromHero)
        {
            if (fromHero == null)
            {
                return null;
            }

            Game.Units.EnemyUnit closest = null;
            float closestDist = float.MaxValue;

            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && enemy.Health != null && !enemy.Health.IsDead)
                {
                    float dist = Mathf.Abs(fromHero.transform.position.x - enemy.transform.position.x);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = enemy;
                    }
                }
            }

            return closest;
        }

        private void RetargetEnemiesToLivingHeroes()
        {
            for (int e = 0; e < activeEnemies.Count; e++)
            {
                Game.Units.EnemyUnit enemy = activeEnemies[e];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                Game.Units.HeroUnit closestHero = null;
                float best = float.MaxValue;
                for (int i = 0; i < partyHeroes.Count; i++)
                {
                    Game.Units.HeroUnit member = partyHeroes[i];
                    if (member == null || member.Health == null || member.Health.IsDead)
                    {
                        continue;
                    }

                    float dist = Mathf.Abs(enemy.transform.position.x - member.transform.position.x);
                    if (dist < best)
                    {
                        best = dist;
                        closestHero = member;
                    }
                }

                if (closestHero != null)
                {
                    enemy.SetTarget(closestHero);
                }
                else
                {
                    enemy.ClearTarget();
                }
            }
        }

        private void CleanupAllEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    if (enemy.Health != null)
                    {
                        enemy.Health.Died -= OnAnyEnemyDied;
                    }

                    Destroy(enemy.gameObject);
                }
            }
            activeEnemies.Clear();

            for (int i = 0; i < partyHeroes.Count; i++)
            {
                if (partyHeroes[i] != null)
                {
                    partyHeroes[i].ClearTarget();
                }
            }
        }

        private IEnumerator TravelToNextWave()
        {
            currentState = BattleState.Traveling;

            for (int i = 0; i < partyHeroes.Count; i++)
            {
                Game.Units.HeroUnit member = partyHeroes[i];
                if (member == null || member.Health == null || member.Health.IsDead)
                {
                    continue;
                }

                member.ClearTarget();
                member.SetMovementEnabled(false);
                member.SetWalking(true);
            }

            if (parallaxScroller != null)
            {
                parallaxScroller.StartScrolling();
            }

            yield return new WaitForSeconds(stageData.travelDuration);

            if (parallaxScroller != null)
            {
                parallaxScroller.StopScrolling();
            }

            for (int i = 0; i < partyHeroes.Count; i++)
            {
                Game.Units.HeroUnit member = partyHeroes[i];
                if (member == null || member.Health == null || member.Health.IsDead)
                {
                    continue;
                }

                member.Health.RestoreFullHealth();
            }

            Debug.Log("[StageBattleController] Party healed between waves.");

            if (stageData.delayAfterTravel > 0)
            {
                for (int i = 0; i < partyHeroes.Count; i++)
                {
                    Game.Units.HeroUnit member = partyHeroes[i];
                    if (member == null || member.Health == null || member.Health.IsDead)
                    {
                        continue;
                    }

                    member.SetWalking(false);
                    member.SetMovementEnabled(true);
                }

                yield return new WaitForSeconds(stageData.delayAfterTravel);
            }
            else
            {
                for (int i = 0; i < partyHeroes.Count; i++)
                {
                    Game.Units.HeroUnit member = partyHeroes[i];
                    if (member == null || member.Health == null || member.Health.IsDead)
                    {
                        continue;
                    }

                    member.SetMovementEnabled(true);
                }
            }
        }

        private void OnPartyMemberDied()
        {
            UpdateHeroTarget();

            if (!IsEntirePartyDead())
            {
                Game.Units.HeroUnit lead = GetLeadHero();
                if (lead != null)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Game.Environment.CameraFollow2D follow = cam.GetComponent<Game.Environment.CameraFollow2D>();
                        if (follow != null)
                        {
                            follow.SetTarget(lead.transform);
                        }
                    }
                }

                return;
            }

            isHeroDead = true;
            currentState = BattleState.HeroDead;

            if (battleFlowCoroutine != null)
            {
                StopCoroutine(battleFlowCoroutine);
                battleFlowCoroutine = null;
            }

            if (parallaxScroller != null)
            {
                parallaxScroller.StopScrolling();
            }

            CleanupAllEnemies();

            Debug.Log("[StageBattleController] Entire party died. Battle lost.");

            if (battleResultPanel != null)
            {
                battleResultPanel.ShowDefeat(stageData.displayName);
            }
        }

        private void OnStageCompleted()
        {
            string displayName = Game.Core.BattleSession.ResolveDisplayName(stageData.displayName);
            Debug.Log($"[StageBattleController] Stage '{displayName}' completed!");

            int totalGold = stageData.completionGoldBonus;
            int totalExp = stageData.completionExperienceBonus;

            if (rewardService != null)
            {
                rewardService.AddGold(totalGold);
                rewardService.AddExperience(totalExp);

                if (rewardService.Progress != null)
                {
                    int stageIndex = Mathf.Max(1, stageData.stageIndex);
                    if (stageIndex > rewardService.Progress.highestCompletedStage)
                    {
                        rewardService.Progress.highestCompletedStage = stageIndex;
                    }
                }
            }

            GrantCompletionChest();
            ApplyMapProgressOnClear();

            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (stageData != null)
            {
                Game.Core.RuntimePlayerState.ActiveStageIndex = Mathf.Max(
                    Game.Core.RuntimePlayerState.ActiveStageIndex,
                    stageData.stageIndex);
            }

            Game.Save.GameSaveController.SaveGame();

            if (battleResultPanel != null)
            {
                battleResultPanel.ShowVictory(displayName, totalGold, totalExp);
            }

            StartCoroutine(VictoryAutoContinueCoroutine());
        }

        private System.Collections.IEnumerator VictoryAutoContinueCoroutine()
        {
            yield return new WaitForSecondsRealtime(victoryAutoContinueDelay);

            // Don't leave loot popup blocking the next stage
            Game.UI.LootPopupUI lootPopup = FindFirstObjectByType<Game.UI.LootPopupUI>(FindObjectsInactive.Include);
            if (lootPopup != null)
            {
                lootPopup.gameObject.SetActive(false);
            }

            if (mapCatalog == null)
            {
                mapCatalog = Game.Core.BattleSession.ActiveCatalog;
            }

            if (mapCatalog == null)
            {
                Game.Inventory.ChestService chest = FindFirstObjectByType<Game.Inventory.ChestService>();
                if (chest != null)
                {
                    mapCatalog = chest.MapCatalog;
                }
            }

            Debug.Log(
                $"[StageBattleController] Auto-continue check. Catalog={(mapCatalog != null ? mapCatalog.name : "NULL")} " +
                $"Level={(Game.Core.BattleSession.SelectedLevel != null ? Game.Core.BattleSession.SelectedLevel.levelId : "null")} " +
                $"LayerBoss={Game.Core.BattleSession.IsLayerBossBattle} " +
                $"Weekly={Game.Core.BattleSession.IsWeeklyDungeonBattle}");

            if (Game.Core.BattleSession.IsWeeklyDungeonBattle)
            {
                Debug.Log("[StageBattleController] Weekly dungeon cleared — returning to village.");
                Game.Core.BattleSession.Clear(clearActiveProgressNode: true);
                Game.Save.GameSaveController.SaveGame();
                UnityEngine.SceneManagement.SceneManager.LoadScene(hubSceneName);
                yield break;
            }

            if (Game.Map.MapProgressService.TryGetAutoContinueTarget(
                    mapCatalog,
                    out Game.Data.LevelData nextLevel,
                    out Game.Data.LayerData nextLayerBoss))
            {
                if (nextLayerBoss != null)
                {
                    Game.Core.BattleSession.SelectLayerBoss(nextLayerBoss, mapCatalog);
                    Debug.Log($"[StageBattleController] Auto-continue → Layer Boss {nextLayerBoss.displayName}");
                }
                else
                {
                    Game.Core.BattleSession.SelectLevel(nextLevel, mapCatalog);
                    Debug.Log($"[StageBattleController] Auto-continue → {nextLevel.displayName}");
                }

                Game.Save.GameSaveController.SaveGame();
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                yield break;
            }

            Debug.LogWarning("[StageBattleController] No next stage — returning to village.");
            Game.Core.BattleSession.Clear(clearActiveProgressNode: true);
            Game.Save.GameSaveController.SaveGame();
            UnityEngine.SceneManagement.SceneManager.LoadScene(hubSceneName);
        }

        private void GrantCompletionChest()
        {
            Game.Inventory.ChestService chestService = FindFirstObjectByType<Game.Inventory.ChestService>();
            if (chestService == null)
            {
                return;
            }

            if (Game.Core.BattleSession.IsWeeklyDungeonBattle)
            {
                Game.Data.ChestData dungeonChest = Game.Core.BattleSession.WeeklyDungeonCatalog != null
                    ? Game.Core.BattleSession.WeeklyDungeonCatalog.dungeonChest
                    : null;
                if (dungeonChest == null && mapCatalog != null)
                {
                    dungeonChest = mapCatalog.dungeonBossChest;
                }

                chestService.TryOpenTierChest(Game.Data.ChestTier.DungeonBoss, dungeonChest);
                return;
            }

            Game.Data.LayerData layerBoss = Game.Map.MapProgressService.ResolveClearedLayerBoss(mapCatalog);
            if (layerBoss != null && Game.Core.BattleSession.IsLayerBossBattle)
            {
                chestService.TryOpenTierChest(Game.Data.ChestTier.LayerBoss, layerBoss.layerBossChest);
                return;
            }

            Game.Data.LevelData level = Game.Map.MapProgressService.ResolveClearedLevel(mapCatalog);
            if (level == null)
            {
                chestService.TryOpenTierChest(Game.Data.ChestTier.Normal);
                return;
            }

            Game.Data.ChestTier tier = level.isChapterBoss
                ? Game.Data.ChestTier.ChapterBoss
                : Game.Data.ChestTier.LevelBoss;

            chestService.TryOpenTierChest(tier, level.completionChest);
        }

        private void ApplyMapProgressOnClear()
        {
            if (Game.Core.BattleSession.IsWeeklyDungeonBattle)
            {
                // Weekly dungeon does not advance map unlocks.
                return;
            }

            Game.Data.LayerData layerBoss = Game.Map.MapProgressService.ResolveClearedLayerBoss(mapCatalog);
            if (layerBoss != null && Game.Core.BattleSession.IsLayerBossBattle)
            {
                Game.Map.MapProgressService.RegisterLayerBossCleared(layerBoss, mapCatalog);
                Game.Dungeon.WeeklyDungeonService weekly =
                    FindFirstObjectByType<Game.Dungeon.WeeklyDungeonService>();
                if (weekly != null)
                {
                    weekly.TryDropCourageStoneFromLayerBoss();
                }

                return;
            }

            Game.Data.LevelData level = Game.Map.MapProgressService.ResolveClearedLevel(mapCatalog);
            if (level != null)
            {
                Game.Map.MapProgressService.RegisterLevelCleared(level, mapCatalog);
                return;
            }

            Debug.LogError(
                "[StageBattleController] Clear with no SelectedLevel and no MapProgress.active* — " +
                "map unlock skipped. Enter battles via Adventure → ENTER.");
        }

        private void UpdateUI()
        {
            if (stageText != null)
            {
                stageText.text = $"Stage: {Game.Core.BattleSession.ResolveDisplayName(stageData.displayName)}";
            }

            if (waveText != null)
            {
                waveText.text = $"Wave: {currentWaveIndex + 1}/{stageData.waves.Count}";
            }
        }
    }
}
