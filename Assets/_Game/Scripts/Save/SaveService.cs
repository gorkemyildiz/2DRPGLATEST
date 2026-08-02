using System;
using System.IO;
using UnityEngine;

namespace Game.Save
{
    public class SaveService : MonoBehaviour
    {
        private const string SAVE_FILE_NAME = "savegame.json";

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        public void Save(SaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveService] Save called with null data.");
                return;
            }

            try
            {
                data.lastSaveTime = DateTime.UtcNow.ToString("o");

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);

                Debug.Log($"[SaveService] Game saved to {SaveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to save game: {ex.Message}");
            }
        }

        public SaveData Load()
        {
            if (!HasSave())
            {
                Debug.Log("[SaveService] No save file found. Creating new save data.");
                return new SaveData();
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning("[SaveService] Failed to deserialize save data. Creating new save data.");
                    return new SaveData();
                }

                Debug.Log($"[SaveService] Game loaded from {SaveFilePath}");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to load game: {ex.Message}. Creating new save data.");
                return new SaveData();
            }
        }

        public bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        public void DeleteSave()
        {
            if (HasSave())
            {
                try
                {
                    File.Delete(SaveFilePath);
                    Debug.Log($"[SaveService] Save file deleted: {SaveFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveService] Failed to delete save file: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[SaveService] No save file to delete.");
            }
        }

        public DateTime GetLastSaveTime()
        {
            if (!HasSave())
            {
                return DateTime.MinValue;
            }

            try
            {
                SaveData data = Load();
                if (data != null && !string.IsNullOrEmpty(data.lastSaveTime))
                {
                    return DateTime.Parse(data.lastSaveTime);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to get last save time: {ex.Message}");
            }

            return DateTime.MinValue;
        }
    }
}
