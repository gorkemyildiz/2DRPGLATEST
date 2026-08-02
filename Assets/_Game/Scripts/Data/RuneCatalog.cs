using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "RuneCatalog", menuName = "Game Data/Build/Rune Catalog")]
    public class RuneCatalog : ScriptableObject
    {
        public List<RuneNodeData> nodes = new List<RuneNodeData>();

        public RuneNodeData GetById(string runeId)
        {
            if (nodes == null || string.IsNullOrEmpty(runeId))
            {
                return null;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].runeId == runeId)
                {
                    return nodes[i];
                }
            }

            return null;
        }

        public List<RuneNodeData> GetByBranch(RuneBranch branch)
        {
            List<RuneNodeData> result = new List<RuneNodeData>();
            if (nodes == null)
            {
                return result;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].branch == branch)
                {
                    result.Add(nodes[i]);
                }
            }

            return result;
        }
    }
}
