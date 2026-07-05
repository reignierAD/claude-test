using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Tiny coroutine-based tween helper (no external dependencies).
/// </summary>
public class Tween : MonoBehaviour
{
    static Tween _runner;

    static Tween Runner
    {
        get
        {
            if (_runner == null)
            {
                var go = new GameObject("~Tween");
                DontDestroyOnLoad(go);
                _runner = go.AddComponent<Tween>();
            }
            return _runner;
        }
    }

    public static void MoveTo(RectTransform rt, Vector2 target, float duration, Action onDone = null)
    {
        Runner.StartCoroutine(Runner.MoveRoutine(rt, target, duration, onDone));
    }

    public static void PopAndDestroy(GameObject go, float duration, Action onDone = null)
    {
        Runner.StartCoroutine(Runner.PopRoutine(go, duration, onDone));
    }

    public static void Delay(float seconds, Action action)
    {
        Runner.StartCoroutine(Runner.DelayRoutine(seconds, action));
    }

    IEnumerator MoveRoutine(RectTransform rt, Vector2 target, float duration, Action onDone)
    {
        if (rt == null) yield break;
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            if (rt == null) yield break;
            t += Time.deltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f); // ease-out cubic
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, e);
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = target;
        onDone?.Invoke();
    }

    IEnumerator PopRoutine(GameObject go, float duration, Action onDone)
    {
        if (go == null) yield break;
        Transform tr = go.transform;
        Vector3 baseScale = tr.localScale;
        float t = 0f;
        while (t < duration)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            // grow to 125% for the first third, then shrink to zero
            float s = p < 0.33f ? Mathf.Lerp(1f, 1.25f, p / 0.33f)
                                : Mathf.Lerp(1.25f, 0f, (p - 0.33f) / 0.67f);
            tr.localScale = baseScale * s;
            yield return null;
        }
        if (go != null) Destroy(go);
        onDone?.Invoke();
    }

    IEnumerator DelayRoutine(float seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }
}
