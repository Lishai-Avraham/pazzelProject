using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class ModePanel : MonoBehaviour
{
    [SerializeField] private GameObject MyPhotosButton;
    [SerializeField] private GameObject AIButton;
    [SerializeField] private GameObject RegularButton;
    [SerializeField] private GameObject settingButton;
    [SerializeField] private ScreenManager screenManager;
    [SerializeField] private GameManager gameManager;

    int myPhotosIndex = 2;
    int aiIndex = 3;
    int regularIndex = 4;
    int settingsIndex=5;
    void Start()
    {
        MyPhotosButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClickMyPhotos);
        AIButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClickAI);
        RegularButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClickRegular);
        settingButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClicksettings);
    }
    public void OnClickMyPhotos()
    {
        screenManager.ShowScreen(myPhotosIndex);
    }
    public void OnClickAI()
    {
        screenManager.ShowScreen(aiIndex);
    }
    public void OnClickRegular()
    {
        gameManager.ShowLevelSelect();
        screenManager.ShowScreen(regularIndex);

    }
    public void OnClicksettings()
    {
        gameManager.ShowLevelSelect();
        screenManager.ShowScreen(settingsIndex);
        
    }
}