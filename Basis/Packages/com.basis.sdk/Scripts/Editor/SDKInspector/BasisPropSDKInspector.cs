using Basis.Scripts.BasisSdk.Helpers.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(BasisProp))]
public class BasisPropSDKInspector : Editor
{
    public VisualTreeAsset visualTree;
    public BasisProp BasisProp;
    public VisualElement rootElement;
    public VisualElement uiElementsRoot;
    public void OnEnable()
    {
        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BasisPathConstants.PropuxmlPath);
        BasisProp = (BasisProp)target;
    }
    public override VisualElement CreateInspectorGUI()
    {
        BasisProp = (BasisProp)target;
        rootElement = new VisualElement();

        // Draw default inspector elements first
        InspectorElement.FillDefaultInspector(rootElement, serializedObject, this);

        if (visualTree != null)
        {
            uiElementsRoot = visualTree.CloneTree();
            rootElement.Add(uiElementsRoot);

            Button BuildButton = BasisHelpersGizmo.Button(uiElementsRoot, BasisPathConstants.BuildButton);
            BuildButton.clicked +=  () =>  Build(BuildButton);
        }
        else
        {
            Debug.LogError("VisualTree is null. Make sure the UXML file is assigned correctly.");
        }

        return rootElement;
    }

    private async void Build(Button buildButton)
    {
        Debug.Log("Building Gameobject Bundle");
        await BasisBundleBuild.GameObjectBundleBuild(BasisProp);
    }
}
