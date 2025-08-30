using UnityEngine;


public class ScreenManager : MonoBehaviour
{
    public GameObject[] screens;
    private int activeIndex = 0;

    void Start()
    {
        ShowScreen(activeIndex);
    }

    public void ShowScreen(int index)
    {
        for (int i = 0; i < screens.Length; i++)
            screens[i].SetActive(i == index);
        activeIndex = index;
    }
}