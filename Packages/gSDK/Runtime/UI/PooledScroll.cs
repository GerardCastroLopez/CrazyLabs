using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace gSDK.UI
{
   public class PooledScroll<T> where T : Component
   {
       public int InstancesCount { get; private set; }

       private readonly ScrollRect _scroll;
       private readonly Action<T, int> _onUpdate;
       private readonly LayoutGroup _layoutGrp;
       private readonly ContentSizeFitter _contentSizeFitter;
       private readonly int[] _indexArray;

       private T[] _instances;
       private Vector2 _instanceSize, _itemSpace;
       private int _instancesPerRowsOrColumns, _itemsCount;
       private float _allInstancesSize;


       public PooledScroll(T prefab, ScrollRect scroll, Action<T, int> onUpdate)
       {
          _scroll = scroll;
          _onUpdate = onUpdate;

          _layoutGrp = scroll.content.GetComponent<LayoutGroup>();
          _contentSizeFitter = scroll.content.GetComponent<ContentSizeFitter>();

          SetSizesAndCounts(prefab.transform as RectTransform);
          _indexArray = new int[InstancesCount];
          Instantiate(prefab);

          scroll.onValueChanged.AddListener(OnScroll);
       }
       
       public void ForEach(Action<T, int> callback)
       {
          ForEach<T>(callback);
       }

       public void ForEach<T0>(Action<T0, int> callback) where T0 : T
       {
          for (int i = 0, max = Mathf.Min(InstancesCount, _itemsCount); i < max; ++i)
          {
             if (_instances[i] is T0 instance)
             {
                callback(instance, _indexArray[i]);
             }
          }
       }

       public T Find(Func<T, bool> match)
       {
          return Find<T>(match);
       }

       public T0 Find<T0>(Func<T0, bool> match) where T0 : T
       {
          foreach (var instance in _instances)
          {
             if (instance is T0 castedInstance && match(castedInstance))
             {
                return castedInstance;
             }
          }
          return null;
       }

       public bool TryFind(Func<T, bool> match, out T value)
       {
          return TryFind<T>(match, out value);
       }

       public bool TryFind<T0>(Func<T0, bool> match, out T0 value) where T0 : T
       {
          value = Find(match);
          return value != null;
       }

       // Call only if the scroll content should be destroyed but not the scroll itself.
       public void Unload()
       {
          _scroll.onValueChanged.RemoveListener(OnScroll);

          for (var i = _instances.Length - 1; i >= 0; --i)
          {
             Object.DestroyImmediate(_instances[i]);
          }
       }

       public void SetItemsCount(int maxItemsCount, bool scrollToBeginning)
       {
          _itemsCount = maxItemsCount;

          Vector2 position = _scroll.content.anchoredPosition;
          if (scrollToBeginning)
          {
             if (_scroll.horizontal)
             {
                position.x = 0f;
             }
             else
             {
                position.y = 0f;
             }
          }

          for (var i = 0; i < InstancesCount; ++i)
          {
             _indexArray[i] = i;
             _instances[i].gameObject.SetActive(i < _itemsCount);
          }

          _contentSizeFitter.enabled = _layoutGrp.enabled = true;
          
          LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);

          _contentSizeFitter.enabled = _layoutGrp.enabled = false;

          ModifyContentSize();
          
          _scroll.content.anchoredPosition = position;
          UpdateItems(true);
       }

       private void SetSizesAndCounts(RectTransform prefabTr)
       {
          if (_layoutGrp is HorizontalOrVerticalLayoutGroup group)
          {
             _instancesPerRowsOrColumns = 1;
             _instanceSize = prefabTr.rect.size;
             _itemSpace = Vector2.zero;

             if (_scroll.horizontal)
             {
                _itemSpace.x = group.spacing;
             }
             else
             {
                _itemSpace.y = group.spacing;
             }
          }
          else if (_layoutGrp is GridLayoutGroup gridLayout)
          {
             _instanceSize = gridLayout.cellSize;
             _itemSpace = gridLayout.spacing;

             _scroll.content.anchorMax = _scroll.content.anchorMin = (_scroll.content.anchorMax + _scroll.content.anchorMin) * 0.5f;

             if (gridLayout.constraint == GridLayoutGroup.Constraint.Flexible)
             {
                float secondaryItemSizeWithSpacing = _scroll.horizontal
                   ? (gridLayout.cellSize.y + gridLayout.spacing.y)
                   : (gridLayout.cellSize.x + gridLayout.spacing.x);
                float viewportOtherSize;

                if (_scroll.horizontal)
                {
                   viewportOtherSize = _scroll.viewport.rect.height;
                   gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                }
                else
                {
                   viewportOtherSize = _scroll.viewport.rect.width;
                   gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                }

                _instancesPerRowsOrColumns = gridLayout.constraintCount = Mathf.FloorToInt(viewportOtherSize / secondaryItemSizeWithSpacing);
             }
             else
             {
                _instancesPerRowsOrColumns = gridLayout.constraintCount;
             }
          }

          var viewportRect = _scroll.viewport.rect;
          float viewportSize = _scroll.horizontal ? viewportRect.width : viewportRect.height;
          float instanceSizeWithSpacing = _scroll.horizontal ? _instanceSize.x + _itemSpace.x : _instanceSize.y + _itemSpace.y;
          int visibleRowsOrColumns = Mathf.CeilToInt(viewportSize / instanceSizeWithSpacing) +1;

          _allInstancesSize = instanceSizeWithSpacing * visibleRowsOrColumns;
          InstancesCount = visibleRowsOrColumns * _instancesPerRowsOrColumns;
       }

       private void Instantiate(T prefab)
       {
          _instances = new T[InstancesCount];

          var startFromIndex = 0;

          if (prefab.transform.parent == _scroll.content)
          {
             _instances[0] = prefab;
             prefab.gameObject.SetActive(false);
             _indexArray[0] = 0;
             startFromIndex = 1;
          }

          for (var i = startFromIndex; i < InstancesCount; ++i)
          {
             var instance = Object.Instantiate(prefab, _scroll.content);
             instance.gameObject.SetActive(false);
             _instances[i] = instance;
             _indexArray[i] = i;
          }
       }

       private void OnScroll(Vector2 delta)
       {
          UpdateItems(false);
       }

       private void ModifyContentSize()
       {
          var contentSize = Vector2.zero;
          int itemsPerEntry = Mathf.CeilToInt((float)_itemsCount / _instancesPerRowsOrColumns);

          if (_scroll.horizontal)
          {
             contentSize.x = (_layoutGrp.padding.left + _layoutGrp.padding.right) + (_instanceSize.x * itemsPerEntry) + (_itemSpace.x * (itemsPerEntry -1));

             if (_layoutGrp is GridLayoutGroup)
             {
                contentSize.y = (_layoutGrp.padding.top + _layoutGrp.padding.bottom) + (_instanceSize.y * _instancesPerRowsOrColumns) + (_itemSpace.y * (_instancesPerRowsOrColumns - 1));
             }
          }
          else
          {
             contentSize.y = (_layoutGrp.padding.top + _layoutGrp.padding.bottom) + (_instanceSize.y * itemsPerEntry) + (_itemSpace.y * (itemsPerEntry - 1));

             if (_layoutGrp is GridLayoutGroup)
             {
                contentSize.x = (_layoutGrp.padding.left + _layoutGrp.padding.right) + (_instanceSize.x * _instancesPerRowsOrColumns) + (_itemSpace.x * (_instancesPerRowsOrColumns -1));
             }
          }

          _scroll.content.sizeDelta = contentSize;
       }

       private void UpdateItems(bool force)
       {
          for (var i = 0; i < InstancesCount; ++i)
          {
             var item = _instances[i];

             if (!item.gameObject.activeSelf)
             {
                return;
             }

             var itemRect = item.transform as RectTransform;

             if (!itemRect)
             {
                continue;
             }

             int prevIndex = _indexArray[i];
             int newIndex = prevIndex;
             var itemPosition = _scroll.viewport.InverseTransformPoint(itemRect.position);
             int movesCount;

             if (_scroll.horizontal)
             {
                movesCount = Mathf.RoundToInt(Mathf.Abs(itemPosition.x) / _allInstancesSize) * Math.Sign(itemPosition.x);
             }
             else
             {
                movesCount = -Mathf.RoundToInt(Mathf.Abs(itemPosition.y) / _allInstancesSize) * Math.Sign(itemPosition.y);
             }

             if (movesCount == 0 && !force)
             {
                continue;
             }

             movesCount = GetClampedMovesCount(newIndex, movesCount);
             newIndex -= (movesCount * InstancesCount);

             if (prevIndex == newIndex && !force)
             {
                continue;
             }

             itemPosition = itemRect.localPosition;

             if (_scroll.horizontal)
             {
                itemPosition.x -= (movesCount * _allInstancesSize);
             }
             else
             {
                itemPosition.y += (movesCount * _allInstancesSize);
             }

             itemRect.localPosition = itemPosition;

             _indexArray[i] = newIndex;

             _onUpdate?.Invoke(item, newIndex);
          }
       }

       private int GetClampedMovesCount(int index, int moves)
       {
          index -= (moves * InstancesCount);

          if (index < 0)
          {
             return (moves - Mathf.CeilToInt(Mathf.Abs((float)index) / InstancesCount));
          }

          if (index >= _itemsCount)
          {
             return (moves + Mathf.CeilToInt((float)(index - _itemsCount +1) / InstancesCount));
          }

          return moves;
       }
   }
}