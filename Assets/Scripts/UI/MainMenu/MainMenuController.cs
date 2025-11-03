using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject panelMain;
    [SerializeField] private GameObject panelSettings; // şimdilik boş kalabilir

    public void ShowMain()
    {
        panelMain.SetActive(true);
        if (panelSettings) panelSettings.SetActive(false);
    }

    public void ShowSettings()
    {
        panelMain.SetActive(false);
        if (panelSettings) panelSettings.SetActive(true);
    }
}