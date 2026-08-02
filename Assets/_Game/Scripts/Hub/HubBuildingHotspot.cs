using UnityEngine;

namespace Game.Hub
{
    /// <summary>
    /// World-space clickable plaza building (Library / Mine / Blacksmith).
    /// Clicks are handled by HubPlazaClickRouter (New Input System safe).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HubBuildingHotspot : MonoBehaviour
    {
        [SerializeField] private Game.Data.VillageBuildingType buildingType = Game.Data.VillageBuildingType.Library;
        [SerializeField] private HubController hubController;

        public Game.Data.VillageBuildingType BuildingType => buildingType;

        public void Configure(Game.Data.VillageBuildingType type, HubController hub = null)
        {
            buildingType = type;
            if (hub != null)
            {
                hubController = hub;
            }
        }

        public void Activate(HubController hub = null)
        {
            if (hub != null)
            {
                hubController = hub;
            }

            if (hubController == null)
            {
                hubController = FindFirstObjectByType<HubController>();
            }

            if (hubController == null)
            {
                Debug.LogWarning("[HubBuildingHotspot] HubController missing.");
                return;
            }

            Debug.Log($"[HubBuildingHotspot] Open {buildingType}");
            hubController.OpenBuilding(buildingType);
        }
    }
}
