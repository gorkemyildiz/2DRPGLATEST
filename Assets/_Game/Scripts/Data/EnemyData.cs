using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Game Data/Enemy")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = "enemy_001";
        public string displayName = "Enemy";

        [Header("Prefab")]
        public Game.Units.EnemyUnit prefab;

        [Header("Stats — edit THESE for combat (EnemyUnit prefab fields are overwritten on spawn)")]
        public int maxHealth = 50;
        public int attackDamage = 10;
        public float moveSpeed = 1.5f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.4f;
        public float attackHitDelay = 0.35f;
        public float deathAnimationDuration = 1f;

        [Header("Rewards")]
        public int goldReward = 5;
        public int experienceReward = 10;
        [Range(0f, 1f)]
        public float chestDropChance = 0.1f;

        [Header("Type")]
        public bool isBoss = false;
    }
}
