using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabs.UI
{
    /// <summary>"&lt; Name &gt;" picker that cycles through a list of options.</summary>
    public sealed class SelectorView : MonoBehaviour
    {
        [SerializeField] Text valueText;
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;

        IReadOnlyList<string> options;
        Action<int> onChanged;
        int index;

        void Awake()
        {
            previousButton.onClick.AddListener(() => Step(-1));
            nextButton.onClick.AddListener(() => Step(+1));
        }

        public void Bind(IReadOnlyList<string> names, int selectedIndex, Action<int> changed)
        {
            options = names;
            onChanged = changed;
            index = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, names.Count - 1));
            Refresh();
        }

        void Step(int direction)
        {
            if (options == null || options.Count == 0) return;
            index = (index + direction + options.Count) % options.Count;
            Refresh();
            onChanged?.Invoke(index);
        }

        void Refresh() => valueText.text = options != null && options.Count > 0 ? options[index] : "-";
    }
}
