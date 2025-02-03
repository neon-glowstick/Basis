using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class DataItem
{
    public string identifier; // Unique identifier for the data item
    public string value; // Value stored as a string
}

[Serializable]
public class SettingsData
{
    // Using Dictionary for O(1) fast lookups
    public Dictionary<string, DataItem> itemsDictionary = new Dictionary<string, DataItem>();

    // Find item by identifier with O(1) lookup time
    public DataItem FindItemByIdentifier(string identifier)
    {
        itemsDictionary.TryGetValue(identifier, out var item);
        return item;
    }
}

public static class BasisSettingsSaver
{
    private const string FileName = "BasisSettings.json";
    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, FileName);

    public static SettingsData SettingsData = new SettingsData();
    private static bool dataLoaded = false;

    // Events
    public static event Action OnDataSaved;
    public static event Action OnDataLoaded;
    public static event Action<DataItem> OnOptionLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static async void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (!dataLoaded)
        {
            await LoadDataAsync();
            dataLoaded = true;
        }
    }

    private static void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        // You can handle scene-specific logic here if needed
    }

    // Add new method to load custom settings
    public static async Task LoadCustomSettings(IEnumerable<DataItem> customItems)
    {
        bool isDataModified = false;

        foreach (var item in customItems)
        {
            var existingItem = SettingsData.FindItemByIdentifier(item.identifier);
            if (existingItem == null)
            {
                // Add the custom item if not already in the data
                SettingsData.itemsDictionary.Add(item.identifier, item);
                isDataModified = true;

                OnOptionLoaded?.Invoke(item);
            }
            else
            {
                // Optionally update existing items with new values
                existingItem.value = item.value;
                OnOptionLoaded?.Invoke(existingItem);
            }
        }

        if (isDataModified)
        {
            await SaveDataAsync(); // Save the custom items only if modified
            Debug.Log("Custom data successfully added.");
        }
    }

    public static async Task SaveDataAsync()
    {
        try
        {
            // Convert dictionary back to list for serialization
            var itemsList = new List<DataItem>(SettingsData.itemsDictionary.Values);
            SettingsData.itemsDictionary.Clear();
            foreach (var item in itemsList)
            {
                SettingsData.itemsDictionary.Add(item.identifier, item);
            }

            string json = JsonUtility.ToJson(SettingsData, true);
            await Task.Run(() => File.WriteAllText(FilePath, json));
            Debug.Log($"Data successfully saved to {FilePath}");

            OnDataSaved?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving data: {ex.Message}");
        }
    }

    public static async Task LoadDataAsync()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = await Task.Run(() => File.ReadAllText(FilePath));
                SettingsData = JsonUtility.FromJson<SettingsData>(json);
                Debug.Log($"Data successfully loaded from {FilePath}");

                OnDataLoaded?.Invoke();

                // Invoke loading events concurrently
                await Task.Run(() =>
                {
                    foreach (DataItem item in SettingsData.itemsDictionary.Values)
                    {
                        OnOptionLoaded?.Invoke(item);
                    }
                });
            }
            else
            {
                Debug.LogWarning("No save file found. Generating default data.");
                await SaveDataAsync(); // Save the default data if file doesn't exist
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading data: {ex.Message}");
        }
    }
}
