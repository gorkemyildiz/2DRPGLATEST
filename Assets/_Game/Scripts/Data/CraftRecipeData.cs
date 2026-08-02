using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class CraftIngredient
    {
        public MaterialData material;
        public int amount = 1;
    }

    [CreateAssetMenu(fileName = "NewCraftRecipe", menuName = "Game Data/Crafting/Recipe")]
    public class CraftRecipeData : ScriptableObject
    {
        [Header("Identity")]
        public string recipeId = "recipe_rusty_sword";
        public string displayName = "Craft Rusty Sword";

        [Header("Output")]
        public ItemData outputItem;
        public int outputCount = 1;

        [Header("Cost")]
        public List<CraftIngredient> ingredients = new List<CraftIngredient>();
        public int goldCost;

        [Header("Requirements")]
        public int requiredBlacksmithLevel = 1;
    }
}
