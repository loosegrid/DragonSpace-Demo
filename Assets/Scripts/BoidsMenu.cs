using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BoidsMenu : MonoBehaviour
{
    public BoidSettingsTemp settings;
    public Dropdown options;
    public Toggle showFPS;
    public Slider boidCount;

    void Start()
    {
        List<string> types = new List<string>(System.Enum.GetNames(typeof(QtTestType)));
        options.AddOptions(types);
        showFPS.isOn = settings.showFPS;
        options.value = (int)settings.type;
        options.RefreshShownValue();
        boidCount.value = settings.testElements;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void ShowFPS(bool b)
    {
        settings.showFPS = b;
    }

    public void SetType(int t)
    {
        settings.type = (QtTestType)t;
    }

    public void LoadBoids(int scene)
    {
        settings.testElements = (int)boidCount.value;
        BoidController.useMenuSettings = true;
        SceneManager.LoadScene(scene);
    }
}