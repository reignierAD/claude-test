using UnityEngine;

/// <summary>Slow constant rotation for the cartoon sunburst background.</summary>
public class SpinBackground : MonoBehaviour
{
    public float degreesPerSecond = -10f;

    void Update()
    {
        transform.localEulerAngles = new Vector3(0f, 0f, Time.time * degreesPerSecond % 360f);
    }
}
