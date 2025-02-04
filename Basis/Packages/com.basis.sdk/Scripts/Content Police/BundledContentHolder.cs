using UnityEngine;

public class BundledContentHolder : MonoBehaviour
{
    public ContentPoliceSelector Selector;
    public BasisLoadableBundle DefaultScene;
    public BasisLoadableBundle DefaultAvatar;
    public static BundledContentHolder Instance;
    public bool UseAddressablesToLoadScene = false;
    public bool UseSceneProvidedHere = false;
    public void Awake()
    {
        Instance = this;
    }
}
