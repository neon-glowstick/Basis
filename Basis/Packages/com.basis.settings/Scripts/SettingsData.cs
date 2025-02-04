using System;
using System.Collections.Generic;

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
