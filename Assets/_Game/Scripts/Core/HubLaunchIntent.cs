namespace Game.Core
{
    /// <summary>
    /// One-shot intent when leaving BattleDemo into VillageHub
    /// (e.g. Adventure button should open map select on arrival).
    /// </summary>
    public static class HubLaunchIntent
    {
        public enum PendingAction
        {
            None = 0,
            OpenAdventure = 1,
            OpenInventory = 2
        }

        public static PendingAction Pending { get; private set; }

        public static void Request(PendingAction action)
        {
            Pending = action;
        }

        public static PendingAction Consume()
        {
            PendingAction action = Pending;
            Pending = PendingAction.None;
            return action;
        }
    }
}
