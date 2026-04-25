using UnityEngine;
using UnityEngine.UI;

public class GiftActionConfigButton : MonoBehaviour
{
    [SerializeField] private GiftActionConfigEditor configEditor;
    [SerializeField] private Button openButton;

    private void Start()
    {
        if (openButton != null)
            openButton.onClick.AddListener(OpenConfigEditor);
    }

    private void OpenConfigEditor()
    {
        if (configEditor != null)
            configEditor.OpenEditor();
    }
}
