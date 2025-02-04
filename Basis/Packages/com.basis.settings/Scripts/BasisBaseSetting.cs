public abstract class BasisBaseSetting<T> : BasisISettingHandler<T>
{
    public string Identifier { get; private set; }
    protected T defaultValue;
    protected T currentValue;

    public BasisBaseSetting(string identifier, T defaultValue)
    {
        Identifier = identifier;
        this.defaultValue = defaultValue;
        currentValue = defaultValue; // Initializing with the default value
    }

    // Retrieve the default value
    public T GetDefaultValue() => defaultValue;

    // Retrieve the current value
    public T GetCurrentValue() => currentValue;

    // Set a new value
    public void SetValue(T value)
    {
        currentValue = value;
        // Optionally, trigger an event to notify other systems about the change
    }

    // To be overridden in derived classes if additional save/load logic is needed
    public virtual void ApplySetting()
    {
        // Here you can handle saving the value or triggering other behavior
    }
}
