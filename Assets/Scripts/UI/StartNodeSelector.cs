using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SmartClassroomAR.Navigation;

namespace SmartClassroomAR.UI
{
    /// <summary>
    /// Dropdown selector enabling users to designate their current physical starting node
    /// (e.g., ground floor main entrance, north stairs, secondary entry).
    /// Compatible with Unity 2022.3 LTS and TextMeshPro.
    /// </summary>
    public class StartNodeSelector : MonoBehaviour
    {
        [Header("UI Component References")]
        [Tooltip("TextMeshPro dropdown component for selecting starting location.")]
        [SerializeField] private TMP_Dropdown startNodeDropdown;

        /// <summary>
        /// Event invoked whenever the selected start node changes, passing the nodeId.
        /// </summary>
        public event Action<string> OnStartNodeSelected;

        /// <summary>
        /// The currently selected start node identifier.
        /// </summary>
        public string SelectedNodeId { get; private set; } = string.Empty;

        private readonly List<NavNode> _cachedStartNodes = new List<NavNode>();

        private void Awake()
        {
            if (startNodeDropdown == null)
            {
                startNodeDropdown = GetComponent<TMP_Dropdown>();
            }

            if (startNodeDropdown != null)
            {
                EnsureDropdownFonts();
                startNodeDropdown.onValueChanged.AddListener(HandleDropdownIndexChanged);
            }
        }

        private void EnsureDropdownFonts()
        {
            if (startNodeDropdown == null) return;

            var defaultFont = TMP_Settings.defaultFontAsset ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont == null) return;

            if (startNodeDropdown.captionText != null && startNodeDropdown.captionText.font == null)
            {
                startNodeDropdown.captionText.font = defaultFont;
                startNodeDropdown.captionText.color = Color.white;
            }

            if (startNodeDropdown.itemText != null && startNodeDropdown.itemText.font == null)
            {
                startNodeDropdown.itemText.font = defaultFont;
                startNodeDropdown.itemText.color = Color.white;
            }
        }

        private void OnDestroy()
        {
            if (startNodeDropdown != null)
            {
                startNodeDropdown.onValueChanged.RemoveListener(HandleDropdownIndexChanged);
            }
        }

        /// <summary>
        /// Populates the dropdown options with available entrance, staircase, or corridor nodes.
        /// Defaults selection to the primary building entrance node.
        /// </summary>
        public void PopulateStartNodes(List<NavNode> entranceNodes)
        {
            _cachedStartNodes.Clear();

            if (startNodeDropdown == null)
            {
                Debug.LogWarning("[StartNodeSelector] Cannot populate: startNodeDropdown reference is null.");
                return;
            }

            EnsureDropdownFonts();
            startNodeDropdown.ClearOptions();

            if (entranceNodes == null || entranceNodes.Count == 0)
            {
                SelectedNodeId = string.Empty;
                var emptyOption = new List<TMP_Dropdown.OptionData>
                {
                    new TMP_Dropdown.OptionData("No Entrance Nodes Available")
                };
                startNodeDropdown.AddOptions(emptyOption);
                return;
            }

            var options = new List<TMP_Dropdown.OptionData>();
            int defaultIndex = 0;

            for (int i = 0; i < entranceNodes.Count; i++)
            {
                var node = entranceNodes[i];
                if (node == null) continue;

                _cachedStartNodes.Add(node);

                string displayText = !string.IsNullOrEmpty(node.label) 
                    ? $"{node.label} [{node.floorId}]" 
                    : $"Node {node.nodeId} [{node.floorId}]";

                options.Add(new TMP_Dropdown.OptionData(displayText));

                // Prefer main entrance as the default selection
                if (node.nodeType == NodeType.Entrance && 
                    (node.label.IndexOf("Main", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     node.nodeId.IndexOf("MAIN", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    defaultIndex = _cachedStartNodes.Count - 1;
                }
            }

            startNodeDropdown.AddOptions(options);

            if (_cachedStartNodes.Count > 0)
            {
                startNodeDropdown.SetValueWithoutNotify(defaultIndex);
                SelectedNodeId = _cachedStartNodes[defaultIndex].nodeId;
                OnStartNodeSelected?.Invoke(SelectedNodeId);
            }
            else
            {
                SelectedNodeId = string.Empty;
            }

            Debug.Log($"[StartNodeSelector] Populated {_cachedStartNodes.Count} start nodes. Selected: '{SelectedNodeId}'.");
        }

        /// <summary>
        /// Sets the active start node selection programmatically.
        /// </summary>
        public void SetSelectedNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return;

            for (int i = 0; i < _cachedStartNodes.Count; i++)
            {
                if (string.Equals(_cachedStartNodes[i].nodeId, nodeId, StringComparison.OrdinalIgnoreCase))
                {
                    if (startNodeDropdown != null)
                    {
                        startNodeDropdown.SetValueWithoutNotify(i);
                    }
                    SelectedNodeId = nodeId;
                    OnStartNodeSelected?.Invoke(SelectedNodeId);
                    return;
                }
            }
        }

        private void HandleDropdownIndexChanged(int index)
        {
            if (index >= 0 && index < _cachedStartNodes.Count)
            {
                SelectedNodeId = _cachedStartNodes[index].nodeId;
                Debug.Log($"[StartNodeSelector] User selected start node: {SelectedNodeId} ({_cachedStartNodes[index].label})");
                OnStartNodeSelected?.Invoke(SelectedNodeId);
            }
        }
    }
}
