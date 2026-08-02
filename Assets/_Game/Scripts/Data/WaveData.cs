using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class WaveData
    {
        [Header("Wave Info")]
        public string waveName = "Wave 1";

        [Header("Enemies")]
        public List<EnemyData> enemies = new List<EnemyData>();

        [Header("Timing")]
        public float delayBeforeWave = 0.5f;
        public float delayBetweenEnemies = 0.5f;

        [Header("Spawn Settings")]
        [Tooltip("How many enemies to spawn at the same time (1 = one at a time)")]
        public int simultaneousSpawnCount = 1;
    }
}
