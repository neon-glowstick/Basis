using UnityEngine;

public interface BasisISettingHandler<T>
{
    // Provides the default value of the setting
    T GetDefaultValue();

    // Retrieves the current value of the setting
    T GetCurrentValue();

    // Sets the new value for the setting
    void SetValue(T value);
}
