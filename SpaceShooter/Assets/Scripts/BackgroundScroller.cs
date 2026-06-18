using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    public float scrollSpeed = 0.1f;

    void Update()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.mainTextureOffset += new Vector2(0, scrollSpeed * Time.deltaTime);
        }
    }
}
