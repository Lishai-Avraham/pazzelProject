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
    [SerializeField] private ScreenManager screenManager;

    int myPhotosIndex = 2;
    int aiIndex = 3;
    int regularIndex = 4;
    void Start()
    {
        MyPhotosButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClickMyPhotos);
        AIButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClickAI);
        RegularButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClickRegular);
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
        screenManager.ShowScreen(regularIndex);
    }
}