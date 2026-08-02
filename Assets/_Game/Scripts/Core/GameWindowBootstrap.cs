using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    /// <summary>
    /// Locks the player to a fixed 960×960 windowed resolution.
    /// </summary>
    public static class GameWindowBootstrap
    {
        public const int WindowWidth = 960;
        public const int WindowHeight = 960;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void ApplyBeforeSplash()
        {
            ForceWindowed(WindowWidth, WindowHeight);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void HookSceneLoads()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ForceWindowed(WindowWidth, WindowHeight);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ForceWindowed(WindowWidth, WindowHeight);
        }

        public static void ForceWindowed(int width, int height)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            width = Mathf.Clamp(width, 640, 3840);
            height = Mathf.Clamp(height, 640, 2160);

            Screen.fullScreen = false;
            Screen.fullScreenMode = FullScreenMode.Windowed;

            if (Screen.width != width || Screen.height != height || Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
            }
#endif
        }
    }
}
