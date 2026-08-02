namespace Game.Data
{
    /// <summary>
    /// Paper-doll slots around the character (8 fixed UI boxes).
    /// Left column: Helmet, Armor, Legs, Accessory(Ring).
    /// Right column: Shoulders, Weapon, Boots, Ring2.
    /// </summary>
    public enum EquipmentSlot
    {
        Weapon = 0,
        Helmet = 1,
        Armor = 2,
        Accessory = 3,
        Shoulders = 4,
        Legs = 5,
        Boots = 6,
        Ring2 = 7
    }
}
