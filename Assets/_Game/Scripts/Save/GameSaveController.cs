using UnityEngine;

namespace Game.Save
{
    /// <summary>
    /// Loads disk save once per Play session and auto-saves on key transitions.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GameSaveController : MonoBehaviour
    {
        [SerializeField] private SaveService saveService;
        [SerializeField] private bool loadOnAwake = true;
        [SerializeField] private bool saveOnQuit = true;
        [SerializeField] private bool saveOnPause = true;

        private static GameSaveController instance;

        public static GameSaveController Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                // Keep one controller; scene duplicates can still call SaveNow via static/instance
                if (saveService == null)
                {
                    saveService = GetComponent<SaveService>();
                }

                return;
            }

            instance = this;

            if (saveService == null)
            {
                saveService = GetComponent<SaveService>();
            }

            if (saveService == null)
            {
                saveService = gameObject.AddComponent<SaveService>();
            }

            if (loadOnAwake)
            {
                TryLoadOnce();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            if (saveOnQuit)
            {
                SaveNow();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && saveOnPause)
            {
                SaveNow();
            }
        }

        public void TryLoadOnce()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();

            if (Game.Core.RuntimePlayerState.HasAppliedDiskSave)
            {
                return;
            }

            if (saveService == null)
            {
                saveService = GetComponent<SaveService>() ?? gameObject.AddComponent<SaveService>();
            }

            if (saveService.HasSave())
            {
                SaveData data = saveService.Load();
                Game.Core.RuntimePlayerState.ApplySaveData(data);
                Debug.Log("[GameSaveController] Disk save applied to runtime session.");
            }
            else
            {
                Game.Core.RuntimePlayerState.MarkDiskLoadComplete();
                Debug.Log("[GameSaveController] No disk save — starting fresh session.");
            }
        }

        public void SaveNow()
        {
            Game.Core.RuntimePlayerState.EnsureInitialized();

            Game.Inventory.EquipmentService equipment = FindFirstObjectByType<Game.Inventory.EquipmentService>();
            if (equipment != null)
            {
                equipment.CaptureToRuntime();
            }

            if (saveService == null)
            {
                saveService = GetComponent<SaveService>() ?? FindFirstObjectByType<SaveService>();
            }

            if (saveService == null)
            {
                Debug.LogWarning("[GameSaveController] SaveService missing; cannot save.");
                return;
            }

            SaveData data = Game.Core.RuntimePlayerState.CaptureSaveData();
            saveService.Save(data);
        }

        public static void SaveGame()
        {
            if (instance != null)
            {
                instance.SaveNow();
                return;
            }

            GameSaveController found = Object.FindFirstObjectByType<GameSaveController>();
            if (found != null)
            {
                found.SaveNow();
                return;
            }

            // Fallback without creating a loader instance (avoids accidental disk reload)
            Game.Inventory.EquipmentService equipment = Object.FindFirstObjectByType<Game.Inventory.EquipmentService>();
            if (equipment != null)
            {
                equipment.CaptureToRuntime();
            }

            SaveService service = Object.FindFirstObjectByType<SaveService>();
            if (service == null)
            {
                GameObject go = new GameObject("TempSaveService");
                service = go.AddComponent<SaveService>();
                service.Save(Game.Core.RuntimePlayerState.CaptureSaveData());
                Object.Destroy(go);
                return;
            }

            service.Save(Game.Core.RuntimePlayerState.CaptureSaveData());
        }

        public static void LoadGameIfNeeded()
        {
            if (instance != null)
            {
                instance.TryLoadOnce();
                return;
            }

            GameSaveController found = Object.FindFirstObjectByType<GameSaveController>();
            if (found != null)
            {
                found.TryLoadOnce();
                return;
            }

            Game.Core.RuntimePlayerState.EnsureInitialized();
            if (Game.Core.RuntimePlayerState.HasAppliedDiskSave)
            {
                return;
            }

            SaveService service = Object.FindFirstObjectByType<SaveService>();
            if (service != null && service.HasSave())
            {
                Game.Core.RuntimePlayerState.ApplySaveData(service.Load());
            }
            else
            {
                Game.Core.RuntimePlayerState.MarkDiskLoadComplete();
            }
        }
    }
}
