using UnityEngine;
using UnityEngine.UI;

public class InputUpdater : MonoBehaviour
{
    public InputField input;

    public void UpdateFromSlider(float count)
    {
        input.text = count.ToString();
    }
}
