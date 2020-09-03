using UnityEngine;
using UnityEngine.UI;

public class SliderUpdater : MonoBehaviour
{
    public Slider slide;

    public void UpdateFromInput(string input)
    {
        if(int.TryParse(input, out int count))
        {
            slide.SetValueWithoutNotify(Mathf.Abs(count));
        }
        else
        {
            throw new System.ArgumentException("number of boids must be a number, please.");
        }
    }
}
