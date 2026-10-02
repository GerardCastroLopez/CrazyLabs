using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public static class CollectionExtensions
{
    public static void Foreach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
        {
            action(item);
        }
    }

    public static T GetRandomFlag<T>(this T flags) where T : Enum
    {
        long current = Convert.ToInt64(flags);
        var possibleFlags = new List<T>();
        foreach (T value in Enum.GetValues(typeof(T)))
        {
            long flag = Convert.ToInt64(value);

            if (flag != 0 && (current & flag) == flag)
            {
                possibleFlags.Add(value);
            }
        }

        return possibleFlags.GetRandom();
    }

    public static T GetRandom<T>(this List<T> list)
    {
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static T GetOrAdd<T>(this ICollection<T> list, Predicate<T> predicate, Func<T> add)
    {
        if (!list.TryFind(predicate, out var result))
        {
            result = add();
            list.Add(result);
        }

        return result;
    }

    public static bool TryFind<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, out T result)
    {
        foreach (var item in enumerable)
        {
            if (predicate(item))
            {
                result = item;
                return true;
            }
        }

        Debug.LogWarning($"No element of type {typeof(T).Name} was found");
        result = default;
        return false;
    }

    public static bool FindAllNonAlloc<T0, T1>(this IEnumerable<T0> enumerable, ICollection<T1> result,
        Predicate<T1> predicate) where T1 : T0
    {
        bool found = false;
        foreach (var item in enumerable)
        {
            if (item is not T1 casted || !predicate(casted)) continue;

            found = true;
            result.Add(casted);
        }

        return found;
    }

    public static bool IsNullOrEmpty(this ICollection list)
    {
        return list == null || list.IsEmpty();
    }

    public static bool IsEmpty(this ICollection list)
    {
        return list.Count == 0;
    }

    /// <summary>
    /// Returns true if we just added the element.
    /// </summary>
    public static bool AddIfMissing<T>(this ICollection<T> list, T element)
    {
        if (!list.Contains(element))
        {
            list.Add(element);
            return true;
        }

        return false;
    }

    public static bool TryRemove<T>(this ICollection<T> list, Predicate<T> predicate)
    {
        if (list.TryFind(predicate, out T item))
        {
            list.Remove(item);
            return true;
        }

        return false;
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (!dict.TryGetValue(key, out TValue value))
        {
            value = new TValue();
            dict.Add(key, value);
        }

        return value;
    }
    
    public static bool Any<T>(this ICollection<T> c, Predicate<T> match)
    {
        foreach (var element in c)
        {
            if (match(element))
            {
                return true;
            }
        }
        return false;
    }

    public static bool All<T>(this ICollection<T> c, Predicate<T> match)
    {
        foreach (var element in c)
        {
            if (!match(element))
            {
                return false;
            }
        }
        return true;
    }
}

public static class UnityExtensions
{
    public static T AddOrGetComponent<T>(this GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }

        return component;
    }

    public static void SetPosX(this RectTransform tr, float x, bool isLocal)
    {
        Vector3 pos = isLocal ? tr.localPosition : tr.position;
        pos.x = x;
        if (isLocal)
        {
            tr.localPosition = pos;
        }
        else
        {
            tr.position = pos;
        }

    }
    
    public static void SetPosY(this RectTransform tr, float y, bool isLocal)
    {
        Vector3 pos = isLocal ? tr.localPosition : tr.position;
        pos.y = y;
        if (isLocal)
        {
            tr.localPosition = pos;
        }
        else
        {
            tr.position = pos;
        }
    }

    public static void SetPivotX(this RectTransform tr, float x)
    {
        Vector2 pivot = tr.pivot;
        pivot.x = x;
        tr.pivot = pivot;
    }

    public static void SetPivotY(this RectTransform tr, float y)
    {
        Vector2 pivot = tr.pivot;
        pivot.y = y;
        tr.pivot = pivot;
    }

    public static void SetHeight(this RectTransform rectTr, float h)
    {
        var size = rectTr.sizeDelta;
        size.y = h;
        rectTr.sizeDelta = size;
    }

    public static void SetAnchorY(this RectTransform tr, float y)
    {
        Vector3 anchor = tr.anchorMin;
        anchor.y = y;
        tr.anchorMin = anchor;
        anchor = tr.anchorMax;
        anchor.y = y;
        tr.anchorMax = anchor;
    }

    public static void SetAnchoredPosX(this RectTransform transform, float value)
    {
        transform.anchoredPosition = new(value, transform.anchoredPosition.y);
    }

    public static void SetAnchoredPosY(this RectTransform transform, float value)
    {
        transform.anchoredPosition = new(transform.anchoredPosition.x, value);
    }

    public static void SetLocalPosX(this Transform transform, float value)
    {
        transform.localPosition = new(value, transform.localPosition.y, transform.localPosition.z);
    }

    public static void SetLocalPosY(this Transform transform, float value)
    {
        transform.localPosition = new(transform.localPosition.x, value, transform.localPosition.z);
    }

    public static void SetLocalPosZ(this Transform transform, float value)
    {
        transform.localPosition = new(transform.localPosition.x, transform.localPosition.y, value);
    }

    public static void SetFullScreen(this Transform tr)
    {
        SetFullScreen(tr as RectTransform);
    }

    public static void SetFullScreen(this RectTransform tr)
    {
        tr.localScale = Vector3.one;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;
        tr.anchoredPosition = Vector2.zero;
    }

    public static void SetSafeArea(this RectTransform tr)
    {
        tr.offsetMin += Screen.safeArea.position;
        tr.offsetMax += Screen.safeArea.position + Screen.safeArea.size - new Vector2(Screen.width, Screen.height);
    }

    public static T AddReusedObject<T>(this List<T> list, T defaultPrefab, Transform parent, Action<int, T> onInit)
        where T : Component
    {
        var index = list.FindIndex(t => !t.gameObject.activeSelf);
        if (index < 0)
        {
            index = list.Count;
            list.Add(GameObject.Instantiate(defaultPrefab, parent));
        }

        var instance = list[index];
        instance.gameObject.SetActive(true);
        instance.transform.SetAsLastSibling();

        onInit?.Invoke(index, instance);

        return instance;
    }

    public static void HideReusedObjects<T>(this List<T> list) where T : Component
    {
        foreach (var instance in list)
        {
            instance.gameObject.SetActive(false);
        }
    }
    
    public static void CreateReusedObjects<T>(this List<T> list, T defaultPrefab, Transform parent, int count,
        Action<int, T> onInit) where T : Component
    {
        list.CreateReusedObjects(defaultPrefab, defaultPrefab.transform.parent == parent, parent, count, onInit);
    }

    public static void CreateReusedObjects<T>(this List<T> list, T defaultPrefab, bool isPreinstantiated,
        Transform parent, int count, Action<int, T> onInit) where T : Component
    {
        var prevCount = list.Count;

        if (prevCount == 0 && isPreinstantiated)
        {
            prevCount++;
            list.Add(defaultPrefab);
        }

        for (int i = 0; i < count; ++i)
        {
            T instance;

            if (i >= prevCount)
            {
                instance = GameObject.Instantiate(defaultPrefab, parent);
                list.Add(instance);
            }
            else
            {
                instance = list[i];
            }

            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();

            onInit?.Invoke(i, instance);
        }

        for (int i = count; i < prevCount; ++i)
        {
            list[i].gameObject.SetActive(false);
        }
    }

    public static float GetRandom(this AnimationCurve curve)
    {
        return curve.Evaluate(Random.value);
    }
}

public static class UIExtensions
{
    public static void SetAlpha(this Graphic g, float a)
    {
        var color = g.color;
        color.a = a;
        g.color = color;
    }

    public static void SetOnlyListener(this Button.ButtonClickedEvent evt, Action callback)
    {
        evt.RemoveAllListeners();
        evt.AddListener(new(callback));
    }
}

public static class NoNamespaceExtensions
{
    public static bool IsNullOrEmpty(this string str)
	{
		return string.IsNullOrEmpty(str);
	}
}

public static class gSdkExtensions
{
    public static Vector2 GetRandom(this Limit<Vector2> limit)
    {
        return new(Random.Range(limit.Min.x, limit.Max.x),
            Random.Range(limit.Min.y, limit.Max.y));
    }
    
    public static Vector3 GetRandom(this Limit<Vector3> limit)
    {
        return new(Random.Range(limit.Min.x, limit.Max.x),
            Random.Range(limit.Min.y, limit.Max.y),
            Random.Range(limit.Min.z, limit.Max.z));
    }
    
    public static float GetRandom(this Limit<float> limit)
    {
        return Random.Range(limit.Min, limit.Max);
    }
    
    public static int GetRandom(this Limit<int> limit)
    {
        return Random.Range(limit.Min, limit.Max);
    }
}