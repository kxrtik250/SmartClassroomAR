using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SmartClassroomAR.Mapping;

namespace SmartClassroomAR.UI
{
    /// <summary>
    /// Displays a searchable, floor-grouped directory of destinations in a ScrollView.
    /// Manages button selection and triggers pathfinding workflow.
    /// Compatible with Unity 2022.3 LTS and TextMeshPro.
    /// </summary>
    public class DestinationSelectionUI : MonoBehaviour
    {
        [Header("Scroll View Container")]
        [Tooltip("Content transform of the ScrollRect where items are instantiated.")]
        [SerializeField] private Transform contentArea;

        [Header("Prefab Elements")]
        [Tooltip("Prefab for destination selection buttons.")]
        [SerializeField] private GameObject destinationButtonPrefab;

        [Tooltip("Prefab for floor section header labels.")]
        [SerializeField] private GameObject floorHeaderPrefab;

        [Header("Search / Filter Input")]
        [Tooltip("Search field for filtering destinations by title.")]
        [SerializeField] private TMP_InputField searchInputField;

        /// <summary>
        /// Event fired when a user selects a destination from the list.
        /// </summary>
        public event Action<DestinationData> OnDestinationSelected;

        /// <summary>
        /// The starting node id for path computation.
        /// </summary>
        public string SelectedStartNodeId { get; private set; } = string.Empty;

        /// <summary>
        /// The currently chosen destination.
        /// </summary>
        public DestinationData SelectedDestination { get; private set; }

        private readonly List<DestinationData> _destinations = new List<DestinationData>();
        private readonly List<FloorData> _floors = new List<FloorData>();
        private readonly List<GameObject> _instantiatedItems = new List<GameObject>();
        private string _activeSearchFilter = string.Empty;

        private void Awake()
        {
            if (searchInputField != null)
            {
                searchInputField.onValueChanged.AddListener(HandleSearchFilterChanged);
            }
        }

        private void OnDestroy()
        {
            if (searchInputField != null)
            {
                searchInputField.onValueChanged.RemoveListener(HandleSearchFilterChanged);
            }
            ClearInstantiatedList();
        }

        /// <summary>
        /// Sets the origin navigation node for route calculation.
        /// </summary>
        public void SetStartNode(string nodeId)
        {
            SelectedStartNodeId = nodeId ?? string.Empty;
            Debug.Log($"[DestinationSelectionUI] Start node updated to: '{SelectedStartNodeId}'");
        }

        /// <summary>
        /// Caches destination and floor records and builds the UI hierarchy.
        /// </summary>
        public void PopulateDestinations(List<DestinationData> destinations, List<FloorData> floors)
        {
            _destinations.Clear();
            if (destinations != null)
            {
                _destinations.AddRange(destinations);
            }

            _floors.Clear();
            if (floors != null)
            {
                _floors.AddRange(floors);
            }

            RefreshList();
        }

        /// <summary>
        /// Rebuilds the UI list items respecting current search filter and floor groupings.
        /// </summary>
        private void RefreshList()
        {
            ClearInstantiatedList();

            if (contentArea == null)
            {
                Debug.LogWarning("[DestinationSelectionUI] contentArea is null; unable to instantiate UI elements.");
                return;
            }

            // Group destinations by floorId
            var floorMap = new Dictionary<string, FloorData>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _floors.Count; i++)
            {
                if (_floors[i] != null && !string.IsNullOrEmpty(_floors[i].floorId))
                {
                    floorMap[_floors[i].floorId] = _floors[i];
                }
            }

            var groupedDestinations = new Dictionary<string, List<DestinationData>>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _destinations.Count; i++)
            {
                var dest = _destinations[i];
                if (dest == null) continue;

                // Apply search filter if present
                if (!string.IsNullOrEmpty(_activeSearchFilter))
                {
                    bool nameMatches = dest.destinationName != null &&
                        dest.destinationName.IndexOf(_activeSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool deptMatches = dest.department != null &&
                        dest.department.IndexOf(_activeSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!nameMatches && !deptMatches)
                    {
                        continue;
                    }
                }

                string floorKey = string.IsNullOrEmpty(dest.floorId) ? "Unknown" : dest.floorId;
                if (!groupedDestinations.TryGetValue(floorKey, out var list))
                {
                    list = new List<DestinationData>();
                    groupedDestinations[floorKey] = list;
                }
                list.Add(dest);
            }

            // Instantiate floor headers and destination buttons
            foreach (var group in groupedDestinations)
            {
                string floorId = group.Key;
                List<DestinationData> itemsOnFloor = group.Value;

                if (itemsOnFloor.Count == 0) continue;

                string floorTitle = floorMap.TryGetValue(floorId, out var floorObj)
                    ? $"{floorObj.floorName} (Floor {floorObj.floorNumber})"
                    : $"Floor {floorId}";

                // 1. Create floor header
                CreateFloorHeader(floorTitle);

                // 2. Create destination buttons
                for (int i = 0; i < itemsOnFloor.Count; i++)
                {
                    CreateDestinationButton(itemsOnFloor[i]);
                }
            }

            Debug.Log($"[DestinationSelectionUI] Rendered {groupedDestinations.Count} floor groups with {_instantiatedItems.Count} total elements.");
        }

        private void CreateFloorHeader(string headerText)
        {
            GameObject headerObj;
            if (floorHeaderPrefab != null)
            {
                headerObj = Instantiate(floorHeaderPrefab, contentArea);
            }
            else
            {
                headerObj = CreateDefaultHeaderItem();
                headerObj.transform.SetParent(contentArea, false);
            }

            var tmpText = headerObj.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                if (tmpText.font == null)
                {
                    tmpText.font = GetDefaultFont();
                }
                tmpText.text = $"<color=#38BDF8>//</color>  <b>{headerText.ToUpper()}</b>";
            }

            _instantiatedItems.Add(headerObj);
        }

        private void CreateDestinationButton(DestinationData destination)
        {
            GameObject buttonObj;
            if (destinationButtonPrefab != null)
            {
                buttonObj = Instantiate(destinationButtonPrefab, contentArea);
            }
            else
            {
                buttonObj = CreateDefaultButtonItem();
                buttonObj.transform.SetParent(contentArea, false);
            }

            var tmpText = buttonObj.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                if (tmpText.font == null)
                {
                    tmpText.font = GetDefaultFont();
                }
                string subDetail = !string.IsNullOrEmpty(destination.description) 
                    ? destination.description 
                    : destination.department;
                if (subDetail.Length > 38) subDetail = subDetail.Substring(0, 36) + "...";
                tmpText.text = $"<b>{destination.destinationName}</b>\n<size=75%><color=#94A3B8>{subDetail}</color></size>";
            }

            var accentImg = buttonObj.transform.Find("CardAccent")?.GetComponent<Image>();
            if (accentImg != null)
            {
                switch (destination.destinationType)
                {
                    case DestinationType.Classroom:
                        accentImg.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Sky Blue
                        break;
                    case DestinationType.Laboratory:
                        accentImg.color = new Color(0.06f, 0.80f, 0.95f, 1f); // Cyan
                        break;
                    case DestinationType.FacultyRoom:
                        accentImg.color = new Color(0.16f, 0.80f, 0.45f, 1f); // Mint Green
                        break;
                    case DestinationType.Staircase:
                        accentImg.color = new Color(0.98f, 0.65f, 0.15f, 1f); // Amber
                        break;
                    case DestinationType.Entrance:
                        accentImg.color = new Color(0.20f, 0.90f, 0.55f, 1f); // Bright Green
                        break;
                    default:
                        accentImg.color = new Color(0.65f, 0.45f, 0.95f, 1f); // Purple
                        break;
                }
            }

            var button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.AddComponent<Button>();
            }

            button.onClick.AddListener(() =>
            {
                SelectedDestination = destination;
                Debug.Log($"[DestinationSelectionUI] Destination chosen: '{destination.destinationName}' (Node: {destination.navigationNodeId})");
                OnDestinationSelected?.Invoke(destination);
            });

            _instantiatedItems.Add(buttonObj);
        }

        private void HandleSearchFilterChanged(string text)
        {
            _activeSearchFilter = text != null ? text.Trim() : string.Empty;
            RefreshList();
        }

        /// <summary>
        /// Selects a destination programmatically by destination ID or human-readable room name.
        /// </summary>
        public void SelectDestinationById(string destId)
        {
            if (string.IsNullOrEmpty(destId)) return;
            var dest = _destinations.Find(d => string.Equals(d.destinationId, destId, StringComparison.OrdinalIgnoreCase) ||
                                               string.Equals(d.destinationName, destId, StringComparison.OrdinalIgnoreCase));
            if (dest != null)
            {
                SelectedDestination = dest;
                Debug.Log($"[DestinationSelectionUI] Programmatic destination chosen: '{dest.destinationName}' (Node: {dest.navigationNodeId})");
                OnDestinationSelected?.Invoke(dest);
            }
            else
            {
                Debug.LogWarning($"[DestinationSelectionUI] Destination ID '{destId}' not found in directory.");
            }
        }

        /// <summary>
        /// Sets the active search filter text programmatically.
        /// </summary>
        public void SetSearchFilter(string text)
        {
            if (searchInputField != null)
            {
                searchInputField.text = text;
            }
            else
            {
                HandleSearchFilterChanged(text);
            }
        }

        private void ClearInstantiatedList()
        {
            for (int i = 0; i < _instantiatedItems.Count; i++)
            {
                if (_instantiatedItems[i] != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(_instantiatedItems[i]);
                    }
                    else
                    {
                        Destroy(_instantiatedItems[i]);
                    }
#else
                    Destroy(_instantiatedItems[i]);
#endif
                }
            }
            _instantiatedItems.Clear();
        }

        /// <summary>
        /// Retrieves the default TextMeshPro font asset from TMP_Settings or Resources.
        /// </summary>
        private TMP_FontAsset GetDefaultFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }
            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        /// <summary>
        /// Fallback UI generator for floor headers if prefab is unassigned.
        /// </summary>
        private GameObject CreateDefaultHeaderItem()
        {
            var root = new GameObject("FloorHeader_Default", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 38f);

            var img = root.GetComponent<Image>();
            img.color = new Color(0.04f, 0.07f, 0.13f, 0.98f); // Deep navy header
            img.raycastTarget = false;

            var layout = root.GetComponent<LayoutElement>();
            layout.minHeight = 38f;
            layout.preferredHeight = 38f;
            layout.flexibleWidth = 1f;

            var textObj = new GameObject("HeaderText", typeof(RectTransform));
            textObj.transform.SetParent(root.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 0f);
            textRect.offsetMax = new Vector2(-14f, 0f);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = GetDefaultFont();
            tmp.fontSize = 14f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.90f, 0.95f, 1f, 1f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;

            return root;
        }

        /// <summary>
        /// Fallback UI generator for destination buttons if prefab is unassigned.
        /// </summary>
        private GameObject CreateDefaultButtonItem()
        {
            var root = new GameObject("DestinationBtn_Default", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 56f);

            var img = root.GetComponent<Image>();
            img.color = new Color(0.07f, 0.10f, 0.17f, 0.98f); // Deep dark slate card
            img.raycastTarget = true;

            var btn = root.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.07f, 0.10f, 0.17f, 0.98f);
            colors.highlightedColor = new Color(0.12f, 0.18f, 0.28f, 1f);
            colors.pressedColor = new Color(0.01f, 0.52f, 0.82f, 1f);
            colors.selectedColor = new Color(0.06f, 0.24f, 0.40f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.targetGraphic = img;

            var layout = root.GetComponent<LayoutElement>();
            layout.minHeight = 56f;
            layout.preferredHeight = 56f;
            layout.flexibleWidth = 1f;

            // Decorative left accent indicator bar
            var accentObj = new GameObject("CardAccent", typeof(RectTransform), typeof(Image));
            accentObj.transform.SetParent(root.transform, false);
            var accentRect = accentObj.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0.15f);
            accentRect.anchorMax = new Vector2(0f, 0.85f);
            accentRect.sizeDelta = new Vector2(3.5f, 0f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = new Vector2(4f, 0f);
            var accentImg = accentObj.GetComponent<Image>();
            accentImg.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Vibrant sky blue
            accentImg.raycastTarget = false;

            var textObj = new GameObject("BtnText", typeof(RectTransform));
            textObj.transform.SetParent(root.transform, false);

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = GetDefaultFont();
            tmp.fontSize = 16f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;

            return root;
        }
    }
}
