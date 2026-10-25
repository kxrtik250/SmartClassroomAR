using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SmartClassroomAR.Mapping;
using SmartClassroomAR.Navigation;

namespace SmartClassroomAR.Visualization
{
    /// <summary>
    /// Generates and manages the authentic 3D St. Paul's Block (CSE Department) architectural digital twin
    /// for Sathyabama Institute of Science and Technology, Chennai.
    /// 
    /// Features:
    /// - Continuous realistic exterior walls in warm off-white plaster finish.
    /// - Distinct horizontal reddish-brown / terracotta facade bands between all floor levels.
    /// - Repeating rows of architectural windows with white frames and dark tinted glass.
    /// - Finished flat roof with parapet coping and roof penthouse structures.
    /// - Grand entrance portico with canopy, supporting columns, and "DEPARTMENT OF COMPUTER SCIENCE & ENGINEERING" marquee.
    /// - Landscaped front area with paved approach, green lawn berms, and decorative shrubs.
    /// - Prominent "ST. PAUL'S BLOCK" & "SATHYABAMA INSTITUTE OF SCIENCE AND TECHNOLOGY" signage.
    /// - Architectural cutaway system: Complete solid exterior in Full Building view; dynamic front/roof cutaway
    ///   during floor inspection or active navigation so internal corridors, rooms, and 3D route paths are visible.
    /// </summary>
    public class BuildingVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool autoInitializeOnStart = false;
        [SerializeField] private float floorHeight = 4.0f;
        [SerializeField] private bool showLabels = true;

        public GameObject BuildingRoot { get; private set; }

        private readonly Dictionary<string, GameObject> _floorObjects = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, List<Renderer>> _floorRenderers = new Dictionary<string, List<Renderer>>();
        private readonly List<Material> _runtimeMaterials = new List<Material>();
        private readonly List<Transform> _billboardLabels = new List<Transform>();

        // Architectural Materials
        private Material _exteriorWallMaterial;
        private Material _terracottaBandMaterial;
        private Material _windowGlassMaterial;
        private Material _windowFrameMaterial;
        private Material _roofSlabMaterial;
        private Material _floorSlabMaterial;
        private Material _pavedApproachMaterial;
        private Material _grassMaterial;
        private Material _shrubMaterial;
        private Material _interiorWallMaterial;
        private Material _corridorMaterial;
        private Material _stairMaterial;
        private Material _entranceMaterial;
        private Material _badgeMaterial;
        private Material _slabTrimMaterial;
        private Material _roomFloorMaterial;
        private Material _roomFloorLabMaterial;
        private Material _roomFloorLoungeMaterial;
        private Material _furnitureWoodMaterial;
        private Material _furnitureMetalMaterial;
        private Material _whiteboardMaterial;
        private Material _labBenchMaterial;
        private Material _monitorMaterial;
        private Material _treeFoliageMaterial;

        // Architectural Cutaway Hierarchy
        private GameObject _exteriorFacadeRoot;
        private GameObject _frontWallGroup;
        private GameObject _roofGroup;
        private GameObject _landscapingRoot;
        private readonly GameObject[] _exteriorFloorLevels = new GameObject[3];
        private bool _isCutawayActive = false;

        private BuildingData _cachedData;
        private string _currentActiveFloorId = null;
        private Camera _cachedMainCamera;

        public bool IsCutawayActive => _isCutawayActive;

        private void Start()
        {
            if (autoInitializeOnStart && _cachedData == null)
            {
                var data = CSEBlockData.CreateDefaultBuildingData();
                Initialize(data);
            }
        }

        private void LateUpdate()
        {
            // Billboard all labels to face the main camera from any orbit angle
            if (_billboardLabels.Count == 0) return;
            if (_cachedMainCamera == null) _cachedMainCamera = Camera.main;
            if (_cachedMainCamera == null) return;

            Quaternion camRot = _cachedMainCamera.transform.rotation;
            for (int i = 0; i < _billboardLabels.Count; i++)
            {
                if (_billboardLabels[i] != null)
                {
                    _billboardLabels[i].rotation = camRot;
                }
            }
        }

        private void OnDestroy()
        {
            ClearVisualization();
            foreach (var mat in _runtimeMaterials)
            {
                if (mat != null) Destroy(mat);
            }
            _runtimeMaterials.Clear();
        }

        /// <summary>
        /// Clears any existing building geometry and labels.
        /// </summary>
        public void ClearVisualization()
        {
            if (BuildingRoot != null)
            {
                Destroy(BuildingRoot);
                BuildingRoot = null;
            }
            _floorObjects.Clear();
            _floorRenderers.Clear();
            _billboardLabels.Clear();
            _exteriorFacadeRoot = null;
            _frontWallGroup = null;
            _roofGroup = null;
            _landscapingRoot = null;
            for (int i = 0; i < _exteriorFloorLevels.Length; i++) _exteriorFloorLevels[i] = null;
            _isCutawayActive = false;
            _currentActiveFloorId = null;
        }

        /// <summary>
        /// Initializes and builds the complete realistic 3D architectural model from the BuildingData.
        /// </summary>
        public void Initialize(BuildingData data)
        {
            if (data == null)
            {
                Debug.LogError("[BuildingVisualizer] BuildingData is null. Cannot build visualization.");
                return;
            }

            _cachedData = data;
            ClearVisualization();
            CreateMaterials();

            BuildingRoot = new GameObject("StPauls_CSE_Building_3D");
            BuildingRoot.transform.SetParent(transform, false);

            // 1. Create Floor Hierarchies (Internal floor slabs, corridors, and level signs)
            foreach (var floor in data.floors)
            {
                var floorGO = new GameObject($"Floor_{floor.floorId}_{floor.floorName}");
                floorGO.transform.SetParent(BuildingRoot.transform, false);
                _floorObjects[floor.floorId] = floorGO;
                _floorRenderers[floor.floorId] = new List<Renderer>();

                float floorY = floor.floorNumber * floorHeight;

                // Base floor slab with perimeter edge trim
                CreateFloorSlab(floorGO, floor.floorId, floorY);

                // Central corridor strip along X axis (Z = 0)
                CreateCorridorStrip(floorGO, floor.floorId, floorY);

                // Exterior level title signpost
                CreateFloorLevelSign(floorGO, floor.floorId, floor.floorName, floor.floorNumber, floorY);
            }

            // 2. Create Rooms at Destination Nodes (Realistic interior walls & portals instead of bright cubes)
            var destinationMap = new Dictionary<string, DestinationData>();
            foreach (var dest in data.destinations)
            {
                if (!string.IsNullOrEmpty(dest.navigationNodeId))
                {
                    destinationMap[dest.navigationNodeId] = dest;
                }
            }

            foreach (var node in data.nodes)
            {
                if (!_floorObjects.TryGetValue(node.floorId, out var floorGO))
                {
                    continue;
                }

                if (node.nodeType == NodeType.Destination)
                {
                    destinationMap.TryGetValue(node.nodeId, out var destData);
                    CreateRoomBox(floorGO, node, destData);
                }
                else if (node.nodeType == NodeType.Staircase)
                {
                    BuildStaircaseForFloor(floorGO, node);
                }
                else if (node.nodeType == NodeType.Entrance)
                {
                    CreateEntranceStructure(floorGO, node);
                }
            }

            // 3. Create Complete St. Paul's Block Exterior Enclosure (Facade, Terracotta Bands, Windows, Roof)
            CreateExteriorFacade(BuildingRoot);

            // 5. Create Landscaping (Paved approach plaza, lawn berms, decorative shrubs)
            BuildLandscaping(BuildingRoot);

            // Default state: Full Building view (Exterior fully solid and visible)
            SetCutawayMode(false);

            Debug.Log($"[BuildingVisualizer] Successfully generated realistic St. Paul's Block (CSE Building) with {data.floors.Count} floors and {data.destinations.Count} rooms.");
        }

        private Shader GetSafeShader(string preferredName = "Standard")
        {
            Shader s = null;
            if (!string.IsNullOrEmpty(preferredName))
            {
                s = Shader.Find(preferredName);
            }
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Diffuse");
            if (s == null) s = Shader.Find("Mobile/Diffuse");
            if (s == null) s = Shader.Find("UI/Default");
            if (s == null) s = Shader.Find("Hidden/InternalErrorShader");

            if (s == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var rend = temp.GetComponent<MeshRenderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    s = rend.sharedMaterial.shader;
                }
                Destroy(temp);
            }
            return s;
        }

        private void CreateMaterials()
        {
            if (_exteriorWallMaterial != null) return;

            Shader standardShader = GetSafeShader("Standard");

            // Authentic St. Paul's Block architectural materials palette
            // 1. Exterior Walls: Warm off-white / light-grey architectural plaster
            _exteriorWallMaterial = CreateColorMaterial(standardShader, new Color(0.82f, 0.83f, 0.84f, 1f), false);
            // 2. Terracotta Bands: Distinct horizontal reddish-brown architectural bands
            _terracottaBandMaterial = CreateColorMaterial(standardShader, new Color(0.60f, 0.22f, 0.15f, 1f), false);
            // 3. Windows: Dark blue-grey tinted glass
            _windowGlassMaterial = CreateColorMaterial(standardShader, new Color(0.10f, 0.16f, 0.26f, 1f), false);
            // 4. Window Frames: Crisp white/cream mullions
            _windowFrameMaterial = CreateColorMaterial(standardShader, new Color(0.95f, 0.95f, 0.96f, 1f), false);
            // 5. Roof: Flat roof gravel/concrete
            _roofSlabMaterial = CreateColorMaterial(standardShader, new Color(0.42f, 0.44f, 0.48f, 1f), false);
            // 5b. Base Floor Slabs: Architectural structural concrete grey - provides strong, grounded contrast against walls and furniture
            _floorSlabMaterial = CreateColorMaterial(standardShader, new Color(0.68f, 0.70f, 0.73f, 1f), false);
            // 6. Landscaping: Stone pavement, grass, trees, and shrubs
            _pavedApproachMaterial = CreateColorMaterial(standardShader, new Color(0.65f, 0.67f, 0.70f, 1f), false);
            _grassMaterial = CreateColorMaterial(standardShader, new Color(0.28f, 0.58f, 0.25f, 1f), false);
            _shrubMaterial = CreateColorMaterial(standardShader, new Color(0.18f, 0.44f, 0.18f, 1f), false);
            _treeFoliageMaterial = CreateColorMaterial(standardShader, new Color(0.20f, 0.48f, 0.22f, 1f), false);

            // Interior Finishes: Warm institutional ivory walls, polished stone-grey terrazzo corridor, dark structural trim
            _interiorWallMaterial = CreateColorMaterial(standardShader, new Color(0.93f, 0.91f, 0.86f, 1f), false); // warm ivory off-white
            _corridorMaterial = CreateColorMaterial(standardShader, new Color(0.70f, 0.72f, 0.74f, 1f), false); // polished terrazzo grey
            _stairMaterial = CreateColorMaterial(standardShader, new Color(0.88f, 0.52f, 0.14f, 1f), false);
            _entranceMaterial = CreateColorMaterial(standardShader, new Color(0.10f, 0.68f, 0.46f, 1f), false);
            _badgeMaterial = CreateColorMaterial(standardShader, new Color(0.04f, 0.07f, 0.14f, 0.92f), true);
            _slabTrimMaterial = CreateColorMaterial(standardShader, new Color(0.24f, 0.28f, 0.35f, 1f), false);

            // Realistic Room Interior & Furniture Materials with high contrast
            _roomFloorMaterial = CreateColorMaterial(standardShader, new Color(0.78f, 0.74f, 0.68f, 1f), false); // warm beige-cream classroom tile
            _roomFloorLabMaterial = CreateColorMaterial(standardShader, new Color(0.60f, 0.65f, 0.70f, 1f), false); // cool slate ESD lab floor
            _roomFloorLoungeMaterial = CreateColorMaterial(standardShader, new Color(0.72f, 0.62f, 0.48f, 1f), false); // warm timber lounge
            _furnitureWoodMaterial = CreateColorMaterial(standardShader, new Color(0.55f, 0.36f, 0.20f, 1f), false); // Rich warm walnut
            _furnitureMetalMaterial = CreateColorMaterial(standardShader, new Color(0.18f, 0.20f, 0.24f, 1f), false); // Dark charcoal steel
            _whiteboardMaterial = CreateColorMaterial(standardShader, new Color(0.95f, 0.96f, 0.97f, 1f), false);
            _labBenchMaterial = CreateColorMaterial(standardShader, new Color(0.16f, 0.20f, 0.25f, 1f), false);
            _monitorMaterial = CreateColorMaterial(standardShader, new Color(0.08f, 0.09f, 0.12f, 1f), false);
        }

        private Material CreateColorMaterial(Shader shader, Color color, bool transparent)
        {
            if (shader == null)
            {
                shader = GetSafeShader("Standard");
            }

            Material mat = null;
            if (shader != null)
            {
                try
                {
                    mat = new Material(shader);
                }
                catch (Exception)
                {
                    mat = null;
                }
            }

            if (mat == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var rend = temp.GetComponent<MeshRenderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    mat = new Material(rend.sharedMaterial);
                }
                Destroy(temp);
            }

            if (mat != null)
            {
                if (mat.HasProperty("_Color")) mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

                if (transparent && mat.HasProperty("_Mode"))
                {
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                }

                _runtimeMaterials.Add(mat);
            }
            return mat;
        }

        private void RegisterRenderer(string floorId, Renderer r)
        {
            if (r != null && _floorRenderers.TryGetValue(floorId, out var list))
            {
                list.Add(r);
            }
        }

        private void CreateFloorSlab(GameObject parent, string floorId, float floorY)
        {
            // Main Floor Slab
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"Slab_{floorId}";
            slab.transform.SetParent(parent.transform, false);
            slab.transform.localPosition = new Vector3(24.0f, floorY - 0.1f, 0.0f);
            slab.transform.localScale = new Vector3(62.0f, 0.2f, 22.0f);

            var rend = slab.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sharedMaterial = _floorSlabMaterial;
                RegisterRenderer(floorId, rend);
            }
            var col = slab.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Outer Perimeter Border Trim
            CreateSlabTrimBar(parent, floorId, new Vector3(24f, floorY + 0.02f, 11.0f), new Vector3(62.4f, 0.08f, 0.35f));
            CreateSlabTrimBar(parent, floorId, new Vector3(24f, floorY + 0.02f, -11.0f), new Vector3(62.4f, 0.08f, 0.35f));
            CreateSlabTrimBar(parent, floorId, new Vector3(-7.0f, floorY + 0.02f, 0.0f), new Vector3(0.35f, 0.08f, 22.4f));
            CreateSlabTrimBar(parent, floorId, new Vector3(55.0f, floorY + 0.02f, 0.0f), new Vector3(0.35f, 0.08f, 22.4f));
        }

        private void CreateSlabTrimBar(GameObject parent, string floorId, Vector3 pos, Vector3 scale)
        {
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "SlabTrim";
            trim.transform.SetParent(parent.transform, false);
            trim.transform.localPosition = pos;
            trim.transform.localScale = scale;
            var r = trim.GetComponent<MeshRenderer>();
            if (r != null)
            {
                r.sharedMaterial = _slabTrimMaterial;
                RegisterRenderer(floorId, r);
            }
            var col = trim.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private void CreateCorridorStrip(GameObject parent, string floorId, float floorY)
        {
            // Main polished corridor walkway slab (barrier-free, open academic corridor)
            var corridor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corridor.name = $"Corridor_{floorId}";
            corridor.transform.SetParent(parent.transform, false);
            corridor.transform.localPosition = new Vector3(25.0f, floorY + 0.03f, 0.0f);
            corridor.transform.localScale = new Vector3(54.0f, 0.06f, 3.4f);

            var rend = corridor.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sharedMaterial = _corridorMaterial;
                RegisterRenderer(floorId, rend);
            }
            var col = corridor.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Subtle polished border inlay strips along north and south corridor edges
            CreateCorridorTrim(parent, floorId, new Vector3(25.0f, floorY + 0.035f, 1.65f), new Vector3(54.0f, 0.02f, 0.10f));
            CreateCorridorTrim(parent, floorId, new Vector3(25.0f, floorY + 0.035f, -1.65f), new Vector3(54.0f, 0.02f, 0.10f));
        }

        private void CreateCorridorTrim(GameObject parent, string floorId, Vector3 pos, Vector3 size)
        {
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "CorridorInlayTrim";
            trim.transform.SetParent(parent.transform, false);
            trim.transform.localPosition = pos;
            trim.transform.localScale = size;
            var r = trim.GetComponent<MeshRenderer>();
            if (r != null)
            {
                r.sharedMaterial = _slabTrimMaterial;
                RegisterRenderer(floorId, r);
            }
            var col = trim.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private void CreateFloorLevelSign(GameObject parent, string floorId, string floorName, int floorNum, float floorY)
        {
            if (!showLabels) return;

            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "FloorSignPost";
            post.transform.SetParent(parent.transform, false);
            post.transform.localPosition = new Vector3(-5.2f, floorY + 0.7f, -6.8f);
            post.transform.localScale = new Vector3(0.14f, 1.4f, 0.14f);
            var postRend = post.GetComponent<MeshRenderer>();
            if (postRend != null)
            {
                postRend.sharedMaterial = _slabTrimMaterial;
                RegisterRenderer(floorId, postRend);
            }
            var postCol = post.GetComponent<Collider>();
            if (postCol != null) Destroy(postCol);

            var signGO = new GameObject($"FloorSign_{floorId}");
            signGO.transform.SetParent(parent.transform, false);
            signGO.transform.localPosition = new Vector3(-5.2f, floorY + 1.75f, -6.8f);
            signGO.transform.localRotation = Quaternion.identity;

            var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "FloorBadgeBacking";
            badge.transform.SetParent(signGO.transform, false);
            badge.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            badge.transform.localScale = new Vector3(5.2f, 1.25f, 0.06f);
            var bRend = badge.GetComponent<MeshRenderer>();
            if (bRend != null)
            {
                bRend.sharedMaterial = _badgeMaterial;
                RegisterRenderer(floorId, bRend);
            }
            var bCol = badge.GetComponent<Collider>();
            if (bCol != null) Destroy(bCol);

            Color accentColor = floorNum == 0 ? new Color(0.10f, 0.85f, 0.55f, 1f)
                              : floorNum == 1 ? new Color(0.15f, 0.70f, 1.00f, 1f)
                              : new Color(1.00f, 0.72f, 0.15f, 1f);
            string hexColor = floorNum == 0 ? "#10B981" : floorNum == 1 ? "#38BDF8" : "#F59E0B";

            var accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            accent.name = "FloorAccentBar";
            accent.transform.SetParent(signGO.transform, false);
            accent.transform.localPosition = new Vector3(-2.50f, 0f, 0.02f);
            accent.transform.localScale = new Vector3(0.18f, 1.20f, 0.08f);
            var aMat = CreateColorMaterial(GetSafeShader("Standard"), accentColor, false);
            var aRend = accent.GetComponent<MeshRenderer>();
            if (aRend != null)
            {
                aRend.sharedMaterial = aMat;
                RegisterRenderer(floorId, aRend);
            }
            var aCol = accent.GetComponent<Collider>();
            if (aCol != null) Destroy(aCol);

            string subtitle = floorNum == 0 ? "ADMIN & LABS"
                            : floorNum == 1 ? "CLASSROOMS & LABS"
                            : "SEMINAR & RESEARCH";

            var tmp = signGO.AddComponent<TextMeshPro>();
            tmp.text = $"<b>{floorName.ToUpper()}</b>\n<size=68%><color={hexColor}>LEVEL {floorNum}</color>  •  <color=#E2E8F0>{subtitle}</color></size>";
            tmp.fontSize = 2.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(5.0f, 1.15f);

            _billboardLabels.Add(signGO.transform);
        }

        private void CreateRoomBox(GameObject parent, NavNode node, DestinationData dest)
        {
            var roomGO = new GameObject($"Room_{node.nodeId}");
            roomGO.transform.SetParent(parent.transform, false);

            float width = 8.5f;
            float depth = 5.5f;
            float wallH = 2.6f;
            float wallThick = 0.14f;
            float doorW = 2.2f;

            // 1. Room Floor Tile Pad with subtle architectural material variation
            string destId = dest != null ? dest.destinationId : "";
            string destName = dest != null ? dest.destinationName : "";
            DestinationType destType = dest != null ? dest.destinationType : DestinationType.Other;

            Material floorMat = _roomFloorMaterial;
            if (destId.Contains("LAB") || destType == DestinationType.Laboratory)
            {
                floorMat = _roomFloorLabMaterial;
            }
            else if (destId == "DEST_F0_LOUNGE" || destId == "DEST_F2_SEMINAR")
            {
                floorMat = _roomFloorLoungeMaterial;
            }

            CreateRoomElement(roomGO, node.floorId, "RoomFloor", new Vector3(node.x, node.y + 0.02f, node.z), new Vector3(width - 0.2f, 0.04f, depth - 0.2f), floorMat);

            // Corridor facing orientation:
            // If node.z > 0, corridor is at Z=0 (South), so front wall is South (-Z relative to room center)
            // If node.z < 0, corridor is at Z=0 (North), so front wall is North (+Z relative to room center)
            float frontZ = node.z > 0 ? (node.z - (depth * 0.5f) + (wallThick * 0.5f)) : (node.z + (depth * 0.5f) - (wallThick * 0.5f));
            float backZ = node.z > 0 ? (node.z + (depth * 0.5f) - (wallThick * 0.5f)) : (node.z - (depth * 0.5f) + (wallThick * 0.5f));
            float wallCenterY = node.y + (wallH * 0.5f);

            // 2. Hollow Perimeter Partition Walls (0.14m Drywall finish)
            // Back Wall
            CreateRoomElement(roomGO, node.floorId, "Wall_Back", new Vector3(node.x, wallCenterY, backZ), new Vector3(width, wallH, wallThick), _interiorWallMaterial);
            // Left Wall
            CreateRoomElement(roomGO, node.floorId, "Wall_Left", new Vector3(node.x - (width * 0.5f) + (wallThick * 0.5f), wallCenterY, node.z), new Vector3(wallThick, wallH, depth), _interiorWallMaterial);
            // Right Wall
            CreateRoomElement(roomGO, node.floorId, "Wall_Right", new Vector3(node.x + (width * 0.5f) - (wallThick * 0.5f), wallCenterY, node.z), new Vector3(wallThick, wallH, depth), _interiorWallMaterial);

            // Front Corridor Wall with 2.2m Open Doorway Portal in Center
            float sideWallW = (width - doorW) * 0.5f; // (8.5 - 2.2) / 2 = 3.15m
            float sideWallOffset = (doorW * 0.5f) + (sideWallW * 0.5f); // 1.1 + 1.575 = 2.675m
            CreateRoomElement(roomGO, node.floorId, "Wall_Front_Left", new Vector3(node.x - sideWallOffset, wallCenterY, frontZ), new Vector3(sideWallW, wallH, wallThick), _interiorWallMaterial);
            CreateRoomElement(roomGO, node.floorId, "Wall_Front_Right", new Vector3(node.x + sideWallOffset, wallCenterY, frontZ), new Vector3(sideWallW, wallH, wallThick), _interiorWallMaterial);

            // Doorway Portal Lintel & Jambs
            float doorH = 2.2f;
            float lintelH = wallH - doorH; // 0.4m
            float lintelY = node.y + doorH + (lintelH * 0.5f);
            CreateRoomElement(roomGO, node.floorId, "DoorLintel", new Vector3(node.x, lintelY, frontZ), new Vector3(doorW + 0.05f, lintelH, wallThick + 0.04f), _slabTrimMaterial);
            CreateRoomElement(roomGO, node.floorId, "DoorJamb_Left", new Vector3(node.x - (doorW * 0.5f), node.y + (doorH * 0.5f), frontZ), new Vector3(0.12f, doorH, wallThick + 0.04f), _slabTrimMaterial);
            CreateRoomElement(roomGO, node.floorId, "DoorJamb_Right", new Vector3(node.x + (doorW * 0.5f), node.y + (doorH * 0.5f), frontZ), new Vector3(0.12f, doorH, wallThick + 0.04f), _slabTrimMaterial);

            // 3. Interior Architectural Furniture Layout (Customized per destination)
            float boardZ = node.z > 0 ? (backZ - 0.06f) : (backZ + 0.06f);
            float credZ = node.z > 0 ? (backZ - 0.28f) : (backZ + 0.28f);

            if (destId == "DEST_F0_OFFICE")
            {
                // CSE Department Office / HOD Suite
                // HOD Executive Desk & Leather Swivel Chair
                CreateRoomElement(roomGO, node.floorId, "HOD_Desk", new Vector3(node.x - 1.4f, node.y + 0.45f, node.z + 0.5f), new Vector3(2.4f, 0.85f, 1.2f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "HOD_Chair", new Vector3(node.x - 1.4f, node.y + 0.35f, node.z + 1.4f), new Vector3(0.65f, 0.70f, 0.65f), _furnitureMetalMaterial);
                CreateRoomElement(roomGO, node.floorId, "VisitorChair_1", new Vector3(node.x - 2.0f, node.y + 0.28f, node.z - 0.4f), new Vector3(0.55f, 0.55f, 0.55f), _furnitureMetalMaterial);
                CreateRoomElement(roomGO, node.floorId, "VisitorChair_2", new Vector3(node.x - 0.8f, node.y + 0.28f, node.z - 0.4f), new Vector3(0.55f, 0.55f, 0.55f), _furnitureMetalMaterial);

                // Small Executive Meeting Round Table
                CreateRoomElement(roomGO, node.floorId, "OfficeMeetingTable", new Vector3(node.x + 2.0f, node.y + 0.40f, node.z), new Vector3(1.8f, 0.78f, 1.8f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "OfficeMeetingChair_N", new Vector3(node.x + 2.0f, node.y + 0.28f, node.z + 1.2f), new Vector3(0.55f, 0.55f, 0.55f), _furnitureMetalMaterial);
                CreateRoomElement(roomGO, node.floorId, "OfficeMeetingChair_S", new Vector3(node.x + 2.0f, node.y + 0.28f, node.z - 1.2f), new Vector3(0.55f, 0.55f, 0.55f), _furnitureMetalMaterial);

                // Tall Executive Bookshelves / File Credenzas
                CreateRoomElement(roomGO, node.floorId, "OfficeCredenza", new Vector3(node.x, node.y + 0.95f, credZ), new Vector3(5.2f, 1.85f, 0.45f), _slabTrimMaterial);
            }
            else if (destId == "DEST_F0_LOUNGE")
            {
                // Student Lounge: Modular sofa seating, coffee table, reading bench
                CreateRoomElement(roomGO, node.floorId, "LoungeSofa_Main", new Vector3(node.x, node.y + 0.30f, node.z + 1.0f), new Vector3(4.8f, 0.60f, 1.1f), _slabTrimMaterial);
                CreateRoomElement(roomGO, node.floorId, "LoungeSofa_Left", new Vector3(node.x - 2.4f, node.y + 0.30f, node.z), new Vector3(1.0f, 0.60f, 2.2f), _slabTrimMaterial);
                CreateRoomElement(roomGO, node.floorId, "CoffeeTable", new Vector3(node.x, node.y + 0.22f, node.z), new Vector3(2.6f, 0.44f, 1.2f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "LoungeBench", new Vector3(node.x, node.y + 0.25f, node.z - 1.4f), new Vector3(4.0f, 0.50f, 0.8f), _furnitureMetalMaterial);
            }
            else if (destId == "DEST_F0_CONF")
            {
                // Conference Room: Boardroom table with 8 chairs, presentation screen
                CreateRoomElement(roomGO, node.floorId, "ConferenceTable", new Vector3(node.x, node.y + 0.42f, node.z), new Vector3(5.4f, 0.82f, 1.8f), _furnitureWoodMaterial);
                float[] chairOffsets = { -1.8f, -0.6f, 0.6f, 1.8f };
                for (int c = 0; c < chairOffsets.Length; c++)
                {
                    CreateRoomElement(roomGO, node.floorId, $"ConfChair_N_{c}", new Vector3(node.x + chairOffsets[c], node.y + 0.28f, node.z + 1.25f), new Vector3(0.55f, 0.55f, 0.55f), _furnitureMetalMaterial);
                    CreateRoomElement(roomGO, node.floorId, $"ConfChair_S_{c}", new Vector3(node.x + chairOffsets[c], node.y + 0.28f, node.z - 1.25f), new Vector3(0.55f, 0.55f, 0.55f), _furnitureMetalMaterial);
                }
                CreateRoomElement(roomGO, node.floorId, "ProjectorScreen", new Vector3(node.x, node.y + 1.6f, boardZ), new Vector3(4.5f, 1.30f, 0.05f), _whiteboardMaterial);
                CreateRoomElement(roomGO, node.floorId, "SideboardCredenza", new Vector3(node.x, node.y + 0.45f, credZ), new Vector3(5.0f, 0.90f, 0.50f), _slabTrimMaterial);
            }
            else if (destId == "DEST_F0_LAB1")
            {
                // CSE Lab 1: Programming & Software Development Laboratory
                // Dual modular computer workbenches with 8 desktop computer monitors
                CreateRoomElement(roomGO, node.floorId, "LabBench_North", new Vector3(node.x, node.y + 0.42f, node.z + 1.15f), new Vector3(6.4f, 0.80f, 0.85f), _labBenchMaterial);
                CreateRoomElement(roomGO, node.floorId, "LabBench_South", new Vector3(node.x, node.y + 0.42f, node.z - 1.15f), new Vector3(6.4f, 0.80f, 0.85f), _labBenchMaterial);
                float[] monX = { -2.1f, -0.7f, 0.7f, 2.1f };
                for (int m = 0; m < monX.Length; m++)
                {
                    // Monitor screens
                    CreateRoomElement(roomGO, node.floorId, $"MonitorN_{m}", new Vector3(node.x + monX[m], node.y + 0.98f, node.z + 1.15f), new Vector3(0.50f, 0.38f, 0.08f), _monitorMaterial);
                    CreateRoomElement(roomGO, node.floorId, $"MonitorS_{m}", new Vector3(node.x + monX[m], node.y + 0.98f, node.z - 1.15f), new Vector3(0.50f, 0.38f, 0.08f), _monitorMaterial);
                    // Monitor stands
                    CreateRoomElement(roomGO, node.floorId, $"StandN_{m}", new Vector3(node.x + monX[m], node.y + 0.85f, node.z + 1.15f), new Vector3(0.10f, 0.12f, 0.10f), _furnitureMetalMaterial);
                    CreateRoomElement(roomGO, node.floorId, $"StandS_{m}", new Vector3(node.x + monX[m], node.y + 0.85f, node.z - 1.15f), new Vector3(0.10f, 0.12f, 0.10f), _furnitureMetalMaterial);
                    // Keyboards
                    CreateRoomElement(roomGO, node.floorId, $"KeyboardN_{m}", new Vector3(node.x + monX[m], node.y + 0.83f, node.z + 1.55f), new Vector3(0.36f, 0.04f, 0.15f), _slabTrimMaterial);
                    CreateRoomElement(roomGO, node.floorId, $"KeyboardS_{m}", new Vector3(node.x + monX[m], node.y + 0.83f, node.z - 1.55f), new Vector3(0.36f, 0.04f, 0.15f), _slabTrimMaterial);
                    // Lab chairs
                    CreateRoomElement(roomGO, node.floorId, $"ChairN_{m}", new Vector3(node.x + monX[m], node.y + 0.24f, node.z + 1.85f), new Vector3(0.44f, 0.48f, 0.44f), _furnitureMetalMaterial);
                    CreateRoomElement(roomGO, node.floorId, $"ChairS_{m}", new Vector3(node.x + monX[m], node.y + 0.24f, node.z - 1.85f), new Vector3(0.44f, 0.48f, 0.44f), _furnitureMetalMaterial);
                }
                // Instructor Station & Smart board
                CreateRoomElement(roomGO, node.floorId, "InstructorStation", new Vector3(node.x - 2.8f, node.y + 0.42f, node.z), new Vector3(1.3f, 0.82f, 0.70f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "LabSmartBoard", new Vector3(node.x, node.y + 1.55f, boardZ), new Vector3(4.5f, 1.25f, 0.05f), _whiteboardMaterial);
                CreateRoomElement(roomGO, node.floorId, "LabBoard_Frame", new Vector3(node.x, node.y + 1.55f, boardZ), new Vector3(4.7f, 1.45f, 0.08f), _slabTrimMaterial);
            }
            else if (destId == "DEST_F1_LAB2")
            {
                // CSE Lab 2: Networks & Operating Systems Laboratory
                // Workbenches with dual monitors
                CreateRoomElement(roomGO, node.floorId, "LabBench_North", new Vector3(node.x, node.y + 0.42f, node.z + 1.15f), new Vector3(6.4f, 0.80f, 0.85f), _labBenchMaterial);
                CreateRoomElement(roomGO, node.floorId, "LabBench_South", new Vector3(node.x, node.y + 0.42f, node.z - 1.15f), new Vector3(6.4f, 0.80f, 0.85f), _labBenchMaterial);
                float[] monX = { -2.1f, -0.7f, 0.7f, 2.1f };
                for (int m = 0; m < monX.Length; m++)
                {
                    CreateRoomElement(roomGO, node.floorId, $"MonitorN_{m}", new Vector3(node.x + monX[m], node.y + 0.95f, node.z + 1.15f), new Vector3(0.50f, 0.35f, 0.10f), _monitorMaterial);
                    CreateRoomElement(roomGO, node.floorId, $"MonitorS_{m}", new Vector3(node.x + monX[m], node.y + 0.95f, node.z - 1.15f), new Vector3(0.50f, 0.35f, 0.10f), _monitorMaterial);
                }
                // Server Networking Rack Cabinet
                CreateRoomElement(roomGO, node.floorId, "ServerRack", new Vector3(node.x + 3.2f, node.y + 1.0f, node.z), new Vector3(0.9f, 2.0f, 1.2f), _slabTrimMaterial);
                CreateRoomElement(roomGO, node.floorId, "InstructorStation", new Vector3(node.x - 2.8f, node.y + 0.42f, node.z), new Vector3(1.3f, 0.82f, 0.70f), _furnitureWoodMaterial);
            }
            else if (destId == "DEST_F1_FAC1")
            {
                // Faculty Room 1: 4 Faculty cubicle cabins with partitions and desks
                float[] cubicleX = { node.x - 2.0f, node.x + 2.0f };
                float[] cubicleZ = { node.z + 1.1f, node.z - 1.1f };
                for (int cx = 0; cx < cubicleX.Length; cx++)
                {
                    for (int cz = 0; cz < cubicleZ.Length; cz++)
                    {
                        CreateRoomElement(roomGO, node.floorId, $"CubicleDesk_{cx}_{cz}", new Vector3(cubicleX[cx], node.y + 0.40f, cubicleZ[cz]), new Vector3(2.4f, 0.78f, 1.1f), _furnitureWoodMaterial);
                        CreateRoomElement(roomGO, node.floorId, $"CubicleScreen_{cx}_{cz}", new Vector3(cubicleX[cx], node.y + 0.75f, cubicleZ[cz] + (cz == 0 ? 0.6f : -0.6f)), new Vector3(2.4f, 0.60f, 0.08f), _slabTrimMaterial);
                        CreateRoomElement(roomGO, node.floorId, $"FacultyLaptop_{cx}_{cz}", new Vector3(cubicleX[cx], node.y + 0.84f, cubicleZ[cz]), new Vector3(0.45f, 0.10f, 0.35f), _monitorMaterial);
                    }
                }
                CreateRoomElement(roomGO, node.floorId, "FacultyBookcase", new Vector3(node.x, node.y + 0.95f, credZ), new Vector3(5.6f, 1.85f, 0.40f), _slabTrimMaterial);
            }
            else if (destId == "DEST_F1_R103")
            {
                // Tutorial Room 103: Interactive seminar tutorial layout
                CreateRoomElement(roomGO, node.floorId, "TutorialTable_N", new Vector3(node.x, node.y + 0.40f, node.z + 1.1f), new Vector3(5.0f, 0.78f, 0.8f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "TutorialTable_S", new Vector3(node.x, node.y + 0.40f, node.z - 1.1f), new Vector3(5.0f, 0.78f, 0.8f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "TutorialTutorDesk", new Vector3(node.x - 2.5f, node.y + 0.42f, node.z), new Vector3(1.4f, 0.82f, 0.75f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "TutorialWhiteboard", new Vector3(node.x, node.y + 1.55f, boardZ), new Vector3(4.2f, 1.20f, 0.04f), _whiteboardMaterial);
            }
            else if (destId == "DEST_F2_LAB3")
            {
                // CSE Lab 3: AI & Cloud Computing Laboratory
                CreateRoomElement(roomGO, node.floorId, "AI_Workbenches_N", new Vector3(node.x, node.y + 0.42f, node.z + 1.15f), new Vector3(6.4f, 0.80f, 0.85f), _labBenchMaterial);
                CreateRoomElement(roomGO, node.floorId, "AI_Workbenches_S", new Vector3(node.x, node.y + 0.42f, node.z - 1.15f), new Vector3(6.4f, 0.80f, 0.85f), _labBenchMaterial);
                float[] aiMonX = { -2.1f, -0.7f, 0.7f, 2.1f };
                for (int m = 0; m < aiMonX.Length; m++)
                {
                    CreateRoomElement(roomGO, node.floorId, $"CurvedMonitorN_{m}", new Vector3(node.x + aiMonX[m], node.y + 0.95f, node.z + 1.15f), new Vector3(0.65f, 0.38f, 0.10f), _monitorMaterial);
                    CreateRoomElement(roomGO, node.floorId, $"CurvedMonitorS_{m}", new Vector3(node.x + aiMonX[m], node.y + 0.95f, node.z - 1.15f), new Vector3(0.65f, 0.38f, 0.10f), _monitorMaterial);
                }
                // Cloud Compute Tower Server
                CreateRoomElement(roomGO, node.floorId, "CloudServerTower", new Vector3(node.x + 3.1f, node.y + 1.1f, node.z), new Vector3(1.0f, 2.2f, 1.3f), _slabTrimMaterial);
                CreateRoomElement(roomGO, node.floorId, "InstructorAIStation", new Vector3(node.x - 2.8f, node.y + 0.42f, node.z), new Vector3(1.3f, 0.82f, 0.70f), _furnitureWoodMaterial);
            }
            else if (destId == "DEST_F2_SEMINAR")
            {
                // Seminar Hall: Raised presentation stage, podium, projection screen, auditorium seating
                CreateRoomElement(roomGO, node.floorId, "SeminarStage", new Vector3(node.x - 2.5f, node.y + 0.20f, node.z), new Vector3(2.5f, 0.40f, 4.4f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "SeminarPodium", new Vector3(node.x - 2.5f, node.y + 0.65f, node.z), new Vector3(0.8f, 1.25f, 0.7f), _slabTrimMaterial);
                CreateRoomElement(roomGO, node.floorId, "SeminarScreen", new Vector3(node.x, node.y + 1.6f, boardZ), new Vector3(5.0f, 1.4f, 0.05f), _whiteboardMaterial);

                // 3 Tiered Rows of Auditorium Seating
                float[] rowX = { node.x + 0.2f, node.x + 1.6f, node.x + 3.0f };
                for (int r = 0; r < rowX.Length; r++)
                {
                    CreateRoomElement(roomGO, node.floorId, $"AuditoriumRow_{r}", new Vector3(rowX[r], node.y + 0.30f, node.z), new Vector3(0.55f, 0.60f, 4.2f), _furnitureMetalMaterial);
                }
            }
            else if (destId == "DEST_F2_PROJECT")
            {
                // Project Lab: Hardware project work islands, test benches, hardware racks
                CreateRoomElement(roomGO, node.floorId, "ProjectIsland_1", new Vector3(node.x - 1.8f, node.y + 0.42f, node.z), new Vector3(2.2f, 0.82f, 2.2f), _labBenchMaterial);
                CreateRoomElement(roomGO, node.floorId, "ProjectIsland_2", new Vector3(node.x + 1.8f, node.y + 0.42f, node.z), new Vector3(2.2f, 0.82f, 2.2f), _labBenchMaterial);
                // Hardware test gear boxes (oscilloscope, power supplies)
                CreateRoomElement(roomGO, node.floorId, "TestGear_1", new Vector3(node.x - 1.8f, node.y + 0.95f, node.z), new Vector3(0.70f, 0.35f, 0.45f), _monitorMaterial);
                CreateRoomElement(roomGO, node.floorId, "TestGear_2", new Vector3(node.x + 1.8f, node.y + 0.95f, node.z), new Vector3(0.70f, 0.35f, 0.45f), _monitorMaterial);
                CreateRoomElement(roomGO, node.floorId, "ComponentsRack", new Vector3(node.x, node.y + 0.95f, credZ), new Vector3(5.6f, 1.85f, 0.45f), _slabTrimMaterial);
            }
            else if (destType == DestinationType.Classroom || destName.StartsWith("Room", StringComparison.OrdinalIgnoreCase))
            {
                // Standard Academic Classrooms (Room 101, 102, 201, 202)
                // Smart board (glowing blue interactive display)
                CreateRoomElement(roomGO, node.floorId, "SmartBoard_Frame", new Vector3(node.x, node.y + 1.65f, boardZ), new Vector3(4.4f, 1.40f, 0.06f), _slabTrimMaterial);
                CreateRoomElement(roomGO, node.floorId, "SmartBoard_Screen", new Vector3(node.x, node.y + 1.65f, boardZ), new Vector3(4.1f, 1.15f, 0.04f), _whiteboardMaterial);
                // Projector mount on ceiling above board
                CreateRoomElement(roomGO, node.floorId, "Projector", new Vector3(node.x, node.y + 2.45f, boardZ + (node.z > 0 ? 0.6f : -0.6f)), new Vector3(0.38f, 0.18f, 0.52f), _monitorMaterial);

                float podiumZ = node.z > 0 ? (backZ - 1.2f) : (backZ + 1.2f);
                CreateRoomElement(roomGO, node.floorId, "Podium", new Vector3(node.x - 2.0f, node.y + 0.45f, podiumZ), new Vector3(1.6f, 0.85f, 0.70f), _furnitureWoodMaterial);
                // Teacher's laptop on podium
                CreateRoomElement(roomGO, node.floorId, "TeacherLaptop", new Vector3(node.x - 2.0f, node.y + 0.90f, podiumZ), new Vector3(0.40f, 0.06f, 0.30f), _monitorMaterial);

                // Two rows of student desks and benches
                float row1Z = node.z > 0 ? (node.z + 0.1f) : (node.z - 0.1f);
                float row2Z = node.z > 0 ? (node.z - 1.1f) : (node.z + 1.1f);

                // Row 1
                CreateRoomElement(roomGO, node.floorId, "StudentDesk_1", new Vector3(node.x, node.y + 0.38f, row1Z), new Vector3(5.6f, 0.74f, 0.60f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "StudentBench_1", new Vector3(node.x, node.y + 0.22f, row1Z + (node.z > 0 ? -0.48f : 0.48f)), new Vector3(5.4f, 0.44f, 0.30f), _furnitureMetalMaterial);
                // Individual books/notebooks on desk 1
                CreateRoomElement(roomGO, node.floorId, "DeskBook_1A", new Vector3(node.x - 1.5f, node.y + 0.77f, row1Z), new Vector3(0.28f, 0.04f, 0.22f), _whiteboardMaterial);
                CreateRoomElement(roomGO, node.floorId, "DeskBook_1B", new Vector3(node.x + 1.5f, node.y + 0.77f, row1Z), new Vector3(0.28f, 0.04f, 0.22f), _whiteboardMaterial);

                // Row 2
                CreateRoomElement(roomGO, node.floorId, "StudentDesk_2", new Vector3(node.x, node.y + 0.38f, row2Z), new Vector3(5.6f, 0.74f, 0.60f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "StudentBench_2", new Vector3(node.x, node.y + 0.22f, row2Z + (node.z > 0 ? -0.48f : 0.48f)), new Vector3(5.4f, 0.44f, 0.30f), _furnitureMetalMaterial);
                CreateRoomElement(roomGO, node.floorId, "DeskBook_2A", new Vector3(node.x - 1.5f, node.y + 0.77f, row2Z), new Vector3(0.28f, 0.04f, 0.22f), _whiteboardMaterial);
                CreateRoomElement(roomGO, node.floorId, "DeskBook_2B", new Vector3(node.x + 1.5f, node.y + 0.77f, row2Z), new Vector3(0.28f, 0.04f, 0.22f), _whiteboardMaterial);

                // Fan on ceiling (symbolic)
                CreateRoomElement(roomGO, node.floorId, "CeilingFan", new Vector3(node.x, node.y + 2.55f, node.z), new Vector3(1.40f, 0.06f, 0.22f), _slabTrimMaterial);
            }
            else
            {
                // General purpose / fallback lab or meeting space
                CreateRoomElement(roomGO, node.floorId, "WorkTable", new Vector3(node.x, node.y + 0.40f, node.z), new Vector3(4.2f, 0.78f, 1.6f), _furnitureWoodMaterial);
                CreateRoomElement(roomGO, node.floorId, "WorkChairs_N", new Vector3(node.x, node.y + 0.28f, node.z + 1.10f), new Vector3(3.6f, 0.55f, 0.35f), _furnitureMetalMaterial);
                CreateRoomElement(roomGO, node.floorId, "WorkChairs_S", new Vector3(node.x, node.y + 0.28f, node.z - 1.10f), new Vector3(3.6f, 0.55f, 0.35f), _furnitureMetalMaterial);
            }

            // 4. Compact Architectural Door Nameplate (Mounted above Door Lintel in Corridor Air)
            if (showLabels && dest != null)
            {
                var labelGO = new GameObject("RoomNameplate");
                labelGO.transform.SetParent(roomGO.transform, false);

                // Offset 0.32m forward into corridor open air so billboarding never intersects doorway lintel or partition walls
                float labelZ = node.z > 0 ? (frontZ - 0.32f) : (frontZ + 0.32f);
                labelGO.transform.localPosition = new Vector3(node.x, node.y + 3.05f, labelZ);

                string roomText = dest.destinationName;
                float badgeWidth = Mathf.Clamp(roomText.Length * 0.24f + 1.4f, 3.8f, 6.8f);

                // Dark backing badge
                var badge = CreateRoomElement(labelGO, node.floorId, "BadgeBacking", new Vector3(0f, 0f, 0.04f), new Vector3(badgeWidth, 0.86f, 0.04f), _badgeMaterial);

                // Floor-specific accent strip on the left edge of the badge
                Color accentColor = node.floorId == "F0" ? new Color(0.10f, 0.85f, 0.55f, 1f)
                                  : node.floorId == "F1" ? new Color(0.15f, 0.70f, 1.00f, 1f)
                                  : new Color(1.00f, 0.72f, 0.15f, 1f);
                var accentMat = CreateColorMaterial(GetSafeShader("Standard"), accentColor, false);
                CreateRoomElement(labelGO, node.floorId, "BadgeAccent", new Vector3(-(badgeWidth * 0.5f) + 0.12f, 0f, 0.02f), new Vector3(0.18f, 0.82f, 0.06f), accentMat);

                var tmp = labelGO.AddComponent<TextMeshPro>();
                tmp.text = roomText;
                tmp.fontSize = roomText.Length > 16 ? 2.2f : 2.5f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.outlineWidth = 0.28f;
                tmp.outlineColor = new Color(0.02f, 0.04f, 0.08f, 1f);
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
                tmp.rectTransform.sizeDelta = new Vector2(badgeWidth - 0.40f, 0.80f);

                _billboardLabels.Add(labelGO.transform);
            }

            // 5. Populate room with low-poly character figures (teacher/students/faculty)
            PopulateRoomWithPeople(roomGO, node, dest);
        }

        private GameObject CreateRoomElement(GameObject parent, string floorId, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var elem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            elem.name = name;
            elem.transform.SetParent(parent.transform, false);
            elem.transform.localPosition = pos;
            elem.transform.localScale = scale;

            var r = elem.GetComponent<MeshRenderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
                RegisterRenderer(floorId, r);
            }
            var col = elem.GetComponent<Collider>();
            if (col != null) Destroy(col);

            return elem;
        }

        private GameObject CreateStairElement(GameObject parent, string floorId, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var elem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            elem.name = name;
            elem.transform.SetParent(parent.transform, false);
            elem.transform.localPosition = pos;
            elem.transform.localScale = scale;

            var r = elem.GetComponent<MeshRenderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
                RegisterRenderer(floorId, r);
            }
            var col = elem.GetComponent<Collider>();
            if (col != null) Destroy(col);

            return elem;
        }

        private GameObject CreateAngledStairElement(GameObject parent, string floorId, string name, Vector3 pos, Vector3 rot, Vector3 scale, Material mat)
        {
            var elem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            elem.name = name;
            elem.transform.SetParent(parent.transform, false);
            elem.transform.localPosition = pos;
            elem.transform.localRotation = Quaternion.Euler(rot);
            elem.transform.localScale = scale;

            var r = elem.GetComponent<MeshRenderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
                RegisterRenderer(floorId, r);
            }
            var col = elem.GetComponent<Collider>();
            if (col != null) Destroy(col);

            return elem;
        }

        private void BuildStaircaseForFloor(GameObject parent, NavNode node)
        {
            var stairRoot = new GameObject($"StaircaseSystem_{node.floorId}");
            stairRoot.transform.SetParent(parent.transform, false);

            float floorY = node.y;
            string fId = node.floorId;

            if (fId == "F0")
            {
                // -------------------------------------------------------------
                // GROUND FLOOR (F0): Lower approach landing + Flight 1 (F0 -> F1)
                // -------------------------------------------------------------
                // Lower Approach Landing (from corridor Z=1.4m to flight start Z=2.4m)
                CreateStairElement(stairRoot, fId, "Landing_Approach_F0", new Vector3(25.0f, floorY + 0.03f, 1.90f), new Vector3(3.2f, 0.06f, 1.0f), _stairMaterial);

                // Flight 1 Solid Structural Ramp & 16 Steps (Ascending from Z=2.4m to Z=7.6m, Y=0.06m to 4.06m)
                BuildStairFlight(stairRoot, fId, floorY);

                // Architectural Staircase Signage Badge facing corridor
                CreateStairSignBadge(stairRoot, fId, floorY, "GROUND TO FIRST FLOOR");
            }
            else if (fId == "F1")
            {
                // -------------------------------------------------------------
                // FIRST FLOOR (F1): Upper arrival landing from F0, side walkways,
                //                   void safety guardrails, lower approach landing,
                //                   and Flight 2 (F1 -> F2)
                // -------------------------------------------------------------
                // Upper Arrival Landing from Flight 1 (Z=7.6m to Z=9.2m)
                CreateStairElement(stairRoot, fId, "Landing_Arrival_F1", new Vector3(25.0f, floorY + 0.03f, 8.40f), new Vector3(3.6f, 0.06f, 1.6f), _stairMaterial);

                // Connecting Walkways from Arrival Landing back to Corridor (along X=23.1m and X=26.9m)
                CreateStairElement(stairRoot, fId, "Walkway_Left_F1", new Vector3(23.10f, floorY + 0.03f, 4.90f), new Vector3(1.2f, 0.06f, 5.8f), _corridorMaterial);
                CreateStairElement(stairRoot, fId, "Walkway_Right_F1", new Vector3(26.90f, floorY + 0.03f, 4.90f), new Vector3(1.2f, 0.06f, 5.8f), _corridorMaterial);

                // Stairwell Void Safety Balustrades / Railings
                BuildStairwellGuardrails(stairRoot, fId, floorY);

                // Lower Approach Landing for Flight 2 (Z=1.4m to Z=2.4m)
                CreateStairElement(stairRoot, fId, "Landing_Approach_F1", new Vector3(25.0f, floorY + 0.03f, 1.90f), new Vector3(3.2f, 0.06f, 1.0f), _stairMaterial);

                // Flight 2 Solid Structural Ramp & 16 Steps (Ascending from Z=2.4m to Z=7.6m, Y=4.06m to 8.06m)
                BuildStairFlight(stairRoot, fId, floorY);

                // Architectural Staircase Signage Badge facing corridor
                CreateStairSignBadge(stairRoot, fId, floorY, "FIRST TO SECOND FLOOR");
            }
            else if (fId == "F2")
            {
                // -------------------------------------------------------------
                // SECOND FLOOR (F2): Upper arrival landing from Flight 2, side walkways,
                //                    and void safety guardrails
                // -------------------------------------------------------------
                // Upper Arrival Landing from Flight 2 (Z=7.6m to Z=9.2m)
                CreateStairElement(stairRoot, fId, "Landing_Arrival_F2", new Vector3(25.0f, floorY + 0.03f, 8.40f), new Vector3(3.6f, 0.06f, 1.6f), _stairMaterial);

                // Connecting Walkways back to Corridor
                CreateStairElement(stairRoot, fId, "Walkway_Left_F2", new Vector3(23.10f, floorY + 0.03f, 4.90f), new Vector3(1.2f, 0.06f, 5.8f), _corridorMaterial);
                CreateStairElement(stairRoot, fId, "Walkway_Right_F2", new Vector3(26.90f, floorY + 0.03f, 4.90f), new Vector3(1.2f, 0.06f, 5.8f), _corridorMaterial);

                // Stairwell Void Safety Balustrades
                BuildStairwellGuardrails(stairRoot, fId, floorY);

                // Architectural Staircase Signage Badge facing corridor
                CreateStairSignBadge(stairRoot, fId, floorY, "SECOND FLOOR LANDING");
            }
        }

        private void CreateEntranceStructure(GameObject parent, NavNode node)
        {
            var entrance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entrance.name = $"Entrance_{node.nodeId}";
            entrance.transform.SetParent(parent.transform, false);
            entrance.transform.localPosition = new Vector3(node.x, node.y + 0.2f, node.z);
            entrance.transform.localScale = new Vector3(4.5f, 0.4f, 5.5f);

            var rend = entrance.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sharedMaterial = _entranceMaterial;
                RegisterRenderer(node.floorId, rend);
            }
            var col = entrance.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Gateway pillars
            CreatePillar(parent, node.floorId, new Vector3(node.x - 1.8f, node.y + 1.5f, node.z + 2.2f));
            CreatePillar(parent, node.floorId, new Vector3(node.x - 1.8f, node.y + 1.5f, node.z - 2.2f));

            if (showLabels)
            {
                var labelGO = new GameObject("EntranceLabel");
                labelGO.transform.SetParent(parent.transform, false);
                labelGO.transform.localPosition = new Vector3(node.x, node.y + 2.85f, node.z);

                float badgeWidth = 5.2f;
                CreateRoomElement(labelGO, node.floorId, "BadgeBacking", new Vector3(0f, 0f, 0.04f), new Vector3(badgeWidth, 0.78f, 0.04f), _badgeMaterial);

                var accentMat = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.10f, 0.85f, 0.55f, 1f), false);
                CreateRoomElement(labelGO, node.floorId, "BadgeAccent", new Vector3(-(badgeWidth * 0.5f) + 0.10f, 0f, 0.02f), new Vector3(0.16f, 0.74f, 0.06f), accentMat);

                var tmp = labelGO.AddComponent<TextMeshPro>();
                tmp.text = "<b>MAIN ENTRANCE</b>\n<size=65%><color=#10B981>RECEPTION & SECURITY</color></size>";
                tmp.fontSize = 1.95f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.outlineWidth = 0.25f;
                tmp.outlineColor = new Color(0.02f, 0.04f, 0.08f, 1f);
                tmp.enableWordWrapping = false;
                tmp.rectTransform.sizeDelta = new Vector2(badgeWidth - 0.35f, 0.72f);

                _billboardLabels.Add(labelGO.transform);
            }
        }

        private void CreatePillar(GameObject parent, string floorId, Vector3 pos)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Pillar";
            pillar.transform.SetParent(parent.transform, false);
            pillar.transform.localPosition = pos;
            pillar.transform.localScale = new Vector3(0.55f, 3.0f, 0.55f);
            var rend = pillar.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sharedMaterial = _entranceMaterial;
                RegisterRenderer(floorId, rend);
            }
            var col = pillar.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private void BuildStairFlight(GameObject parent, string floorId, float baseY)
        {
            float flightRise = 4.0f;
            float flightRun = 5.2f;
            float startZ = 2.40f;
            float endZ = 7.60f;
            float startY = baseY + 0.06f;
            float endY = baseY + flightRise + 0.06f;
            float slopeLength = Mathf.Sqrt(flightRise * flightRise + flightRun * flightRun); // ~6.56m
            float pitchAngle = -Mathf.Atan2(flightRise, flightRun) * Mathf.Rad2Deg; // -37.56 deg

            Vector3 midpoint = new Vector3(25.0f, baseY + (flightRise * 0.5f) + 0.06f, (startZ + endZ) * 0.5f);

            // 1. Solid Structural Concrete Soffit / Stringer Slab underneath
            CreateAngledStairElement(parent, floorId, "StairSoffitSlab", midpoint - new Vector3(0f, 0.10f, 0f), new Vector3(pitchAngle, 0f, 0f), new Vector3(2.58f, 0.20f, slopeLength), _floorSlabMaterial);

            // 2. Solid Steps (16 Treads & Risers)
            int stepCount = 16;
            float stepRun = flightRun / stepCount; // 0.325m
            float stepRise = flightRise / stepCount; // 0.25m

            for (int s = 0; s < stepCount; s++)
            {
                float t = (s + 0.5f) / stepCount;
                float sZ = startZ + t * flightRun;
                float sY = startY + t * flightRise;

                // Tread Cap (Terracotta/Amber)
                CreateStairElement(parent, floorId, $"Tread_{s}", new Vector3(25.0f, sY + 0.04f, sZ), new Vector3(2.54f, 0.08f, stepRun + 0.03f), _stairMaterial);

                // Vertical Closed Riser (Structural Dark Trim)
                CreateStairElement(parent, floorId, $"Riser_{s}", new Vector3(25.0f, sY - (stepRise * 0.40f), sZ - (stepRun * 0.45f)), new Vector3(2.54f, stepRise, 0.06f), _slabTrimMaterial);
            }

            // 3. Sturdy Architectural Handrails (Left & Right)
            float[] railX = { -1.32f, 1.32f };
            for (int r = 0; r < railX.Length; r++)
            {
                float xOffset = railX[r];

                // Sloping Top Handrail Bar
                CreateAngledStairElement(parent, floorId, $"HandrailTop_{r}", midpoint + new Vector3(xOffset, 0.90f, 0f), new Vector3(pitchAngle, 0f, 0f), new Vector3(0.08f, 0.06f, slopeLength + 0.10f), _slabTrimMaterial);

                // Sloping Mid Guardrail Bar
                CreateAngledStairElement(parent, floorId, $"HandrailMid_{r}", midpoint + new Vector3(xOffset, 0.45f, 0f), new Vector3(pitchAngle, 0f, 0f), new Vector3(0.05f, 0.04f, slopeLength + 0.10f), _slabTrimMaterial);

                // Vertical Support Baluster Posts (Firmly anchored into steps/landings)
                float[] postT = { 0.0f, 0.33f, 0.67f, 1.0f };
                for (int p = 0; p < postT.Length; p++)
                {
                    float pt = postT[p];
                    float pZ = startZ + pt * flightRun;
                    float pY = startY + pt * flightRise + 0.45f;
                    CreateStairElement(parent, floorId, $"HandrailPost_{r}_{p}", new Vector3(25.0f + xOffset, pY, pZ), new Vector3(0.08f, 0.95f, 0.08f), _slabTrimMaterial);
                }
            }
        }

        private void BuildStairwellGuardrails(GameObject parent, string floorId, float floorY)
        {
            float[] railX = { 23.68f, 26.32f };
            for (int r = 0; r < railX.Length; r++)
            {
                float x = railX[r];
                // Horizontal Top Safety Rail along stairwell void
                CreateStairElement(parent, floorId, $"VoidRailTop_{r}", new Vector3(x, floorY + 0.95f, 4.50f), new Vector3(0.08f, 0.06f, 6.0f), _slabTrimMaterial);
                // Horizontal Mid Safety Rail
                CreateStairElement(parent, floorId, $"VoidRailMid_{r}", new Vector3(x, floorY + 0.50f, 4.50f), new Vector3(0.05f, 0.04f, 6.0f), _slabTrimMaterial);

                // Vertical Posts
                float[] postZ = { 1.50f, 3.50f, 5.50f, 7.50f };
                for (int p = 0; p < postZ.Length; p++)
                {
                    CreateStairElement(parent, floorId, $"VoidPost_{r}_{p}", new Vector3(x, floorY + 0.50f, postZ[p]), new Vector3(0.08f, 1.0f, 0.08f), _slabTrimMaterial);
                }
            }
        }

        private void CreateStairSignBadge(GameObject parent, string floorId, float floorY, string subtitle)
        {
            if (!showLabels) return;

            var signGO = new GameObject($"StairSign_{floorId}");
            signGO.transform.SetParent(parent.transform, false);
            signGO.transform.localPosition = new Vector3(25.0f, floorY + 2.85f, 1.80f);

            float badgeWidth = 5.6f;
            CreateStairElement(signGO, floorId, "BadgeBacking", new Vector3(0f, 0f, 0.04f), new Vector3(badgeWidth, 0.82f, 0.04f), _badgeMaterial);

            var accentMat = CreateColorMaterial(GetSafeShader("Standard"), new Color(1.00f, 0.72f, 0.15f, 1f), false);
            CreateStairElement(signGO, floorId, "BadgeAccent", new Vector3(-(badgeWidth * 0.5f) + 0.12f, 0f, 0.02f), new Vector3(0.18f, 0.78f, 0.06f), accentMat);

            var tmp = signGO.AddComponent<TextMeshPro>();
            tmp.text = $"<b>CENTRAL STAIRCASE</b>\n<size=65%><color=#F59E0B>{subtitle}</color></size>";
            tmp.fontSize = 2.1f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.26f;
            tmp.outlineColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(badgeWidth - 0.40f, 0.76f);

            _billboardLabels.Add(signGO.transform);
        }

        // =========================================================================================
        // REALISTIC ST. PAUL'S BLOCK EXTERIOR ARCHITECTURE
        // =========================================================================================

        private void CreateExteriorFacade(GameObject parent)
        {
            _exteriorFacadeRoot = new GameObject("StPauls_ExteriorFacade");
            _exteriorFacadeRoot.transform.SetParent(parent.transform, false);

            _frontWallGroup = new GameObject("FrontFacade_South");
            _frontWallGroup.transform.SetParent(_exteriorFacadeRoot.transform, false);

            _roofGroup = new GameObject("RoofAssembly");
            _roofGroup.transform.SetParent(_exteriorFacadeRoot.transform, false);

            for (int f = 0; f < 3; f++)
            {
                _exteriorFloorLevels[f] = new GameObject($"ExteriorWalls_Level{f}");
                _exteriorFloorLevels[f].transform.SetParent(_exteriorFacadeRoot.transform, false);
            }

            // 1. Build Front (South) Facade at Z = -10.9m
            BuildFacadeWall(_frontWallGroup, -10.9f, true);

            // 2. Build Rear (North) Facade at Z = +10.9m (Split by floor into _exteriorFloorLevels)
            BuildFacadeWall(_exteriorFacadeRoot, 10.9f, false);

            // 3. Build Gable / Side Walls (West at X = -6.8m, East at X = 54.8m, Split by floor)
            BuildEndWall(-6.8f, 21.8f);
            BuildEndWall(54.8f, 21.8f);

            // 4. Build Finished Flat Roof, Parapets & Penthouse
            BuildRoofAssembly(_roofGroup);

            // 5. Build Grand Entrance Portico & Canopy (Front Facade Entrance)
            BuildEntrancePortico(_frontWallGroup);

            // 6. Build Official St. Paul's Block Signage on Roof Parapet
            BuildStPaulsSignage(_roofGroup);
        }

        private void BuildFacadeWall(GameObject parent, float zPos, bool isFront)
        {
            float wallThickness = 0.35f;

            GameObject p0 = isFront ? parent : _exteriorFloorLevels[0];
            GameObject p1 = isFront ? parent : _exteriorFloorLevels[1];
            GameObject p2 = isFront ? parent : _exteriorFloorLevels[2];

            // 1. Horizontal Terracotta Facade Bands (Sathyabama St. Paul's Block signature feature)
            // Plinth Base Band (Y = 0.0 to 0.5m)
            CreateBox(p0, "Band_Plinth", new Vector3(24.0f, 0.25f, zPos), new Vector3(61.6f, 0.50f, wallThickness + 0.12f), _terracottaBandMaterial);
            // Band between Ground & First Floor (Y = 3.52 to 4.18m)
            CreateBox(p0, "Band_Floor1", new Vector3(24.0f, 3.85f, zPos), new Vector3(61.6f, 0.66f, wallThickness + 0.12f), _terracottaBandMaterial);
            // Band between First & Second Floor (Y = 7.52 to 8.18m)
            CreateBox(p1, "Band_Floor2", new Vector3(24.0f, 7.85f, zPos), new Vector3(61.6f, 0.66f, wallThickness + 0.12f), _terracottaBandMaterial);
            // Roof Parapet Terracotta Band (Y = 11.52 to 12.20m)
            CreateBox(p2, "Band_Parapet", new Vector3(24.0f, 11.86f, zPos), new Vector3(61.6f, 0.68f, wallThickness + 0.12f), _terracottaBandMaterial);
            // Top Parapet Concrete Coping (Y = 12.20 to 12.50m)
            CreateBox(p2, "Parapet_Coping", new Vector3(24.0f, 12.35f, zPos), new Vector3(61.6f, 0.30f, wallThickness + 0.16f), _exteriorWallMaterial);

            // 2. Continuous Solid Spandrel Panels (Above and below window rows - completely closes facade)
            // Ground Floor (F0)
            CreateBox(p0, "Spandrel_F0_Low", new Vector3(24.0f, 0.75f, zPos), new Vector3(61.6f, 0.50f, wallThickness), _exteriorWallMaterial);
            CreateBox(p0, "Spandrel_F0_High", new Vector3(24.0f, 3.26f, zPos), new Vector3(61.6f, 0.52f, wallThickness), _exteriorWallMaterial);
            // First Floor (F1)
            CreateBox(p1, "Spandrel_F1_Low", new Vector3(24.0f, 4.51f, zPos), new Vector3(61.6f, 0.66f, wallThickness), _exteriorWallMaterial);
            CreateBox(p1, "Spandrel_F1_High", new Vector3(24.0f, 7.19f, zPos), new Vector3(61.6f, 0.66f, wallThickness), _exteriorWallMaterial);
            // Second Floor (F2)
            CreateBox(p2, "Spandrel_F2_Low", new Vector3(24.0f, 8.51f, zPos), new Vector3(61.6f, 0.66f, wallThickness), _exteriorWallMaterial);
            CreateBox(p2, "Spandrel_F2_High", new Vector3(24.0f, 11.19f, zPos), new Vector3(61.6f, 0.66f, wallThickness), _exteriorWallMaterial);

            // 3. Repeating Window Bays and Inter-Window Concrete Piers across all 3 floors
            float[] bayX = { -1.5f, 4.0f, 9.5f, 15.0f, 20.5f, 26.0f, 31.5f, 37.0f, 42.5f, 48.0f };
            float[] floorY = { 2.0f, 5.85f, 9.85f };

            for (int f = 0; f < floorY.Length; f++)
            {
                float y = floorY[f];
                GameObject floorTarget = isFront ? parent : _exteriorFloorLevels[f];

                // Solid end piers at window height
                CreateBox(floorTarget, $"EndPier_West_F{f}", new Vector3(-5.05f, y, zPos), new Vector3(3.5f, 2.0f, wallThickness), _exteriorWallMaterial);
                CreateBox(floorTarget, $"EndPier_East_F{f}", new Vector3(52.30f, y, zPos), new Vector3(5.0f, 2.0f, wallThickness), _exteriorWallMaterial);

                for (int b = 0; b < bayX.Length; b++)
                {
                    float x = bayX[b];

                    // Front ground floor entrance portal
                    if (isFront && f == 0 && (x >= 20.0f && x <= 27.0f))
                    {
                        CreateBox(floorTarget, "DoorFrame_Outer", new Vector3(x, y, zPos), new Vector3(4.5f, 2.0f, wallThickness), _exteriorWallMaterial);
                        CreateBox(floorTarget, "GlassDoor_Double", new Vector3(x, y - 0.1f, zPos), new Vector3(3.4f, 1.8f, 0.10f), _windowGlassMaterial);
                    }
                    else
                    {
                        CreateWindowAssembly(floorTarget, new Vector3(x, y, zPos), new Vector2(3.6f, 2.0f), zPos < 0);
                    }

                    // Pier between adjacent windows
                    if (b < bayX.Length - 1)
                    {
                        float midX = (bayX[b] + bayX[b + 1]) * 0.5f;
                        CreateBox(floorTarget, $"Pier_F{f}_{b}", new Vector3(midX, y, zPos), new Vector3(1.9f, 2.0f, wallThickness), _exteriorWallMaterial);
                    }
                }
            }
        }

        private void CreateWindowAssembly(GameObject parent, Vector3 center, Vector2 size, bool facingSouth)
        {
            var winGO = new GameObject("WindowAssembly");
            winGO.transform.SetParent(parent.transform, false);
            winGO.transform.localPosition = center;

            float glassThickness = 0.08f;
            float frameThickness = 0.12f;
            float mullionDepth = 0.06f;
            float borderW = 0.12f;
            float zOffset = facingSouth ? -0.04f : 0.04f;

            // 1. Dark Blue-Grey Tinted Glass Pane (The actual glass)
            CreateBox(winGO, "Glass_Pane", Vector3.zero, new Vector3(size.x, size.y, glassThickness), _windowGlassMaterial);

            // 2. Crisp White Outer Frame Borders (top, bottom, left, right border bars)
            CreateBox(winGO, "Frame_Left", new Vector3(-(size.x * 0.5f - borderW * 0.5f), 0f, zOffset), new Vector3(borderW, size.y, frameThickness), _windowFrameMaterial);
            CreateBox(winGO, "Frame_Right", new Vector3((size.x * 0.5f - borderW * 0.5f), 0f, zOffset), new Vector3(borderW, size.y, frameThickness), _windowFrameMaterial);
            CreateBox(winGO, "Frame_Top", new Vector3(0f, (size.y * 0.5f - borderW * 0.5f), zOffset), new Vector3(size.x, borderW, frameThickness), _windowFrameMaterial);
            CreateBox(winGO, "Frame_Bottom", new Vector3(0f, -(size.y * 0.5f - borderW * 0.5f), zOffset), new Vector3(size.x, borderW, frameThickness), _windowFrameMaterial);

            // 3. Central Vertical Mullion Divider
            CreateBox(winGO, "Mullion_Vert", new Vector3(0f, 0f, zOffset), new Vector3(0.08f, size.y - borderW * 2f, mullionDepth), _windowFrameMaterial);

            // 4. Horizontal Transom Divider
            CreateBox(winGO, "Mullion_Horiz", new Vector3(0f, 0.25f, zOffset), new Vector3(size.x - borderW * 2f, 0.08f, mullionDepth), _windowFrameMaterial);

            // 5. Exterior Window Sill (Architectural drip sill)
            float sillZ = facingSouth ? -0.15f : 0.15f;
            CreateBox(winGO, "WindowSill", new Vector3(0f, -(size.y * 0.5f) - 0.06f, sillZ), new Vector3(size.x + 0.30f, 0.12f, 0.30f), _exteriorWallMaterial);
        }

        private void BuildEndWall(float xPos, float zLength)
        {
            float wallThickness = 0.40f;

            // Level 0 (Ground Floor, Y = 0 to 4.0m)
            GameObject p0 = _exteriorFloorLevels[0];
            CreateBox(p0, "EndWall_F0", new Vector3(xPos, 2.0f, 0.0f), new Vector3(wallThickness, 4.0f, zLength), _exteriorWallMaterial);
            CreateBox(p0, "EndBand_Plinth", new Vector3(xPos, 0.25f, 0.0f), new Vector3(wallThickness + 0.12f, 0.50f, zLength + 0.12f), _terracottaBandMaterial);
            CreateBox(p0, "EndBand_F1", new Vector3(xPos, 3.85f, 0.0f), new Vector3(wallThickness + 0.12f, 0.65f, zLength + 0.12f), _terracottaBandMaterial);
            CreateBox(p0, "Pilaster_S_F0", new Vector3(xPos, 2.0f, -(zLength * 0.5f)), new Vector3(0.65f, 4.0f, 0.65f), _slabTrimMaterial);
            CreateBox(p0, "Pilaster_N_F0", new Vector3(xPos, 2.0f, (zLength * 0.5f)), new Vector3(0.65f, 4.0f, 0.65f), _slabTrimMaterial);

            // Level 1 (First Floor, Y = 4.0 to 8.0m)
            GameObject p1 = _exteriorFloorLevels[1];
            CreateBox(p1, "EndWall_F1", new Vector3(xPos, 6.0f, 0.0f), new Vector3(wallThickness, 4.0f, zLength), _exteriorWallMaterial);
            CreateBox(p1, "EndBand_F2", new Vector3(xPos, 7.85f, 0.0f), new Vector3(wallThickness + 0.12f, 0.65f, zLength + 0.12f), _terracottaBandMaterial);
            CreateBox(p1, "Pilaster_S_F1", new Vector3(xPos, 6.0f, -(zLength * 0.5f)), new Vector3(0.65f, 4.0f, 0.65f), _slabTrimMaterial);
            CreateBox(p1, "Pilaster_N_F1", new Vector3(xPos, 6.0f, (zLength * 0.5f)), new Vector3(0.65f, 4.0f, 0.65f), _slabTrimMaterial);

            // Level 2 (Second Floor, Y = 8.0 to 12.2m)
            GameObject p2 = _exteriorFloorLevels[2];
            CreateBox(p2, "EndWall_F2", new Vector3(xPos, 10.1f, 0.0f), new Vector3(wallThickness, 4.2f, zLength), _exteriorWallMaterial);
            CreateBox(p2, "EndBand_Parapet", new Vector3(xPos, 11.85f, 0.0f), new Vector3(wallThickness + 0.12f, 0.70f, zLength + 0.12f), _terracottaBandMaterial);
            CreateBox(p2, "Pilaster_S_F2", new Vector3(xPos, 10.1f, -(zLength * 0.5f)), new Vector3(0.65f, 4.2f, 0.65f), _slabTrimMaterial);
            CreateBox(p2, "Pilaster_N_F2", new Vector3(xPos, 10.1f, (zLength * 0.5f)), new Vector3(0.65f, 4.2f, 0.65f), _slabTrimMaterial);
        }

        private void BuildRoofAssembly(GameObject parent)
        {
            float roofY = 12.0f;
            float roofLength = 62.2f;
            float roofWidth = 22.2f;

            // Flat Main Roof Slab
            CreateBox(parent, "RoofSlab_Main", new Vector3(24.0f, roofY, 0.0f), new Vector3(roofLength, 0.35f, roofWidth), _roofSlabMaterial);

            // Perimeter Parapet Wall with finished terracotta coping
            float parapetHeight = 0.85f;
            float py = roofY + (parapetHeight * 0.5f);

            CreateBox(parent, "Parapet_Front", new Vector3(24.0f, py, -11.0f), new Vector3(roofLength, parapetHeight, 0.40f), _exteriorWallMaterial);
            CreateBox(parent, "Parapet_Rear", new Vector3(24.0f, py, 11.0f), new Vector3(roofLength, parapetHeight, 0.40f), _exteriorWallMaterial);
            CreateBox(parent, "Parapet_West", new Vector3(-6.9f, py, 0.0f), new Vector3(0.40f, parapetHeight, roofWidth), _exteriorWallMaterial);
            CreateBox(parent, "Parapet_East", new Vector3(54.9f, py, 0.0f), new Vector3(0.40f, parapetHeight, roofWidth), _exteriorWallMaterial);

            // Finished Parapet Coping Trim Cap
            float copingY = roofY + parapetHeight + 0.04f;
            CreateBox(parent, "Coping_Front", new Vector3(24.0f, copingY, -11.0f), new Vector3(roofLength + 0.3f, 0.10f, 0.55f), _terracottaBandMaterial);
            CreateBox(parent, "Coping_Rear", new Vector3(24.0f, copingY, 11.0f), new Vector3(roofLength + 0.3f, 0.10f, 0.55f), _terracottaBandMaterial);
            CreateBox(parent, "Coping_West", new Vector3(-6.9f, copingY, 0.0f), new Vector3(0.55f, 0.10f, roofWidth + 0.3f), _terracottaBandMaterial);
            CreateBox(parent, "Coping_East", new Vector3(54.9f, copingY, 0.0f), new Vector3(0.55f, 0.10f, roofWidth + 0.3f), _terracottaBandMaterial);

            // Architectural Roof Penthouses (Stairway headhouse & elevator equipment housing)
            var penthouse = new GameObject("RoofPenthouse");
            penthouse.transform.SetParent(parent.transform, false);
            penthouse.transform.localPosition = new Vector3(25.0f, roofY + 1.6f, 0.0f);

            CreateBox(penthouse, "Penthouse_Block", Vector3.zero, new Vector3(9.0f, 3.2f, 7.5f), _exteriorWallMaterial);
            CreateBox(penthouse, "Penthouse_Roof", new Vector3(0f, 1.7f, 0f), new Vector3(9.6f, 0.25f, 8.1f), _terracottaBandMaterial);
        }

        private void BuildEntrancePortico(GameObject parent)
        {
            var portico = new GameObject("MainEntrancePortico");
            portico.transform.SetParent(parent.transform, false);
            portico.transform.localPosition = new Vector3(23.5f, 0.0f, -11.0f);

            float porticoWidth = 9.5f;
            float porticoDepth = 4.8f;
            float canopyHeight = 4.3f;
            float canopyThickness = 0.45f;
            float frontZ = -(porticoDepth);

            // 1. Entrance Canopy Roof
            CreateBox(portico, "Canopy_Slab", new Vector3(0f, canopyHeight, frontZ * 0.5f), new Vector3(porticoWidth, canopyThickness, porticoDepth), _exteriorWallMaterial);
            CreateBox(portico, "Canopy_Fascia", new Vector3(0f, canopyHeight + 0.05f, frontZ * 0.5f), new Vector3(porticoWidth + 0.3f, 0.18f, porticoDepth + 0.3f), _terracottaBandMaterial);

            // 2. Supporting Architectural Columns (Sturdy square pillars)
            float colOffset = (porticoWidth * 0.5f) - 0.75f;
            float colZ = frontZ + 0.55f;
            CreateBox(portico, "PorticoColumn_L", new Vector3(-colOffset, canopyHeight * 0.5f, colZ), new Vector3(0.65f, canopyHeight, 0.65f), _exteriorWallMaterial);
            CreateBox(portico, "PorticoColumn_R", new Vector3(colOffset, canopyHeight * 0.5f, colZ), new Vector3(0.65f, canopyHeight, 0.65f), _exteriorWallMaterial);

            // Column bases & capitals
            CreateBox(portico, "ColumnBase_L", new Vector3(-colOffset, 0.25f, colZ), new Vector3(0.90f, 0.50f, 0.90f), _terracottaBandMaterial);
            CreateBox(portico, "ColumnBase_R", new Vector3(colOffset, 0.25f, colZ), new Vector3(0.90f, 0.50f, 0.90f), _terracottaBandMaterial);

            // 3. Paved Entrance Porch Slab
            CreateBox(portico, "Porch_Slab", new Vector3(0f, 0.08f, frontZ * 0.5f), new Vector3(porticoWidth + 1.2f, 0.16f, porticoDepth + 1.0f), _pavedApproachMaterial);

            // 4. Entrance Marquee Signboard mounted on Canopy Fascia
            var entSignGO = new GameObject("EntranceMarquee_CSE");
            entSignGO.transform.SetParent(portico.transform, false);
            entSignGO.transform.localPosition = new Vector3(0f, canopyHeight + 0.55f, frontZ - 0.05f);
            entSignGO.transform.localRotation = Quaternion.identity;

            var eBacking = CreateBox(entSignGO, "SignBacking", Vector3.zero, new Vector3(9.2f, 0.85f, 0.12f), _badgeMaterial);

            var tmp = entSignGO.AddComponent<TextMeshPro>();
            tmp.text = "<b>DEPARTMENT OF COMPUTER SCIENCE & ENGINEERING</b>\n<size=65%><color=#38BDF8>MAIN ENTRANCE</color></size>";
            tmp.fontSize = 2.0f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(9.0f, 0.80f);

            // 5. Main Entrance Pillars Signboard: "ST. PAUL'S BLOCK"
            // Prominently mounted across the front entrance pillars below the canopy beam
            var pillarSignGO = new GameObject("EntrancePillarSign_StPauls");
            pillarSignGO.transform.SetParent(portico.transform, false);
            pillarSignGO.transform.localPosition = new Vector3(0f, 3.82f, colZ - 0.12f);
            pillarSignGO.transform.localRotation = Quaternion.identity;

            var pBacking = CreateBox(pillarSignGO, "PillarSignBacking", Vector3.zero, new Vector3(7.4f, 0.68f, 0.12f), _badgeMaterial);
            CreateBox(pillarSignGO, "PillarSignAccent_L", new Vector3(-3.55f, 0f, 0.02f), new Vector3(0.20f, 0.64f, 0.14f), _terracottaBandMaterial);
            CreateBox(pillarSignGO, "PillarSignAccent_R", new Vector3( 3.55f, 0f, 0.02f), new Vector3(0.20f, 0.64f, 0.14f), _terracottaBandMaterial);

            var pTmp = pillarSignGO.AddComponent<TextMeshPro>();
            pTmp.text = "<b><color=#F59E0B>ST. PAUL'S BLOCK</color></b>\n<size=60%><color=#F8FAFC>DEPARTMENT OF COMPUTER SCIENCE & ENGINEERING</color></size>";
            pTmp.fontSize = 2.1f;
            pTmp.fontStyle = FontStyles.Bold;
            pTmp.alignment = TextAlignmentOptions.Center;
            pTmp.color = Color.white;
            pTmp.outlineWidth = 0.28f;
            pTmp.outlineColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            pTmp.enableWordWrapping = false;
            pTmp.rectTransform.sizeDelta = new Vector2(7.2f, 0.64f);
        }

        private void BuildLandscaping(GameObject parent)
        {
            _landscapingRoot = new GameObject("StPauls_Landscaping");
            _landscapingRoot.transform.SetParent(parent.transform, false);

            // 1. Vast Campus Turf Ground Plane (160m x 140m lush green campus grounds)
            CreateBox(_landscapingRoot, "CampusGroundTurf", new Vector3(25.0f, -0.15f, -5.0f), new Vector3(160.0f, 0.20f, 140.0f), _grassMaterial);

            // 2. Continuous Solid Concrete Foundation Plinth beneath St. Paul's Block footprint
            CreateBox(_landscapingRoot, "BuildingFoundationPlinth", new Vector3(24.0f, -0.05f, 0.0f), new Vector3(64.0f, 0.12f, 24.0f), _exteriorWallMaterial);

            // 3. Wide Stone Paved Entrance Plaza & Forecourt
            CreateBox(_landscapingRoot, "PavedApproach_Plaza", new Vector3(24.0f, -0.02f, -22.0f), new Vector3(48.0f, 0.10f, 22.0f), _pavedApproachMaterial);

            // 4. Perimeter Campus Walkway Paths
            CreateBox(_landscapingRoot, "Walkway_MainSouth", new Vector3(25.0f, -0.03f, -34.0f), new Vector3(120.0f, 0.08f, 3.6f), _pavedApproachMaterial);
            CreateBox(_landscapingRoot, "Walkway_WestConnector", new Vector3(-16.0f, -0.03f, -16.0f), new Vector3(3.6f, 0.08f, 34.0f), _pavedApproachMaterial);
            CreateBox(_landscapingRoot, "Walkway_EastConnector", new Vector3(64.0f, -0.03f, -16.0f), new Vector3(3.6f, 0.08f, 34.0f), _pavedApproachMaterial);

            // 5. Manicured Decorative Landscaping Shrubs along front foundation
            float[] shrubX = { -3.5f, 2.5f, 8.5f, 14.5f, 33.5f, 39.5f, 45.5f, 51.5f };
            for (int i = 0; i < shrubX.Length; i++)
            {
                var shrub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shrub.name = $"Shrub_{i}";
                shrub.transform.SetParent(_landscapingRoot.transform, false);
                shrub.transform.localPosition = new Vector3(shrubX[i], 0.45f, -11.8f);
                shrub.transform.localScale = new Vector3(1.1f, 0.9f, 1.1f);

                var r = shrub.GetComponent<MeshRenderer>();
                if (r != null) r.sharedMaterial = _shrubMaterial;
                var c = shrub.GetComponent<Collider>();
                if (c != null) Destroy(c);
            }

            // 6. Lush Campus Shade Trees along perimeter and walkways
            float[] treeX = { -12f, 4f, 20f, 36f, 52f, 68f };
            for (int t = 0; t < treeX.Length; t++)
            {
                CreateCampusTree(_landscapingRoot, new Vector3(treeX[t], 0f, -40.0f), 1.0f);
            }

            // West campus trees
            CreateCampusTree(_landscapingRoot, new Vector3(-28.0f, 0f, -12.0f), 1.1f);
            CreateCampusTree(_landscapingRoot, new Vector3(-28.0f, 0f, 6.0f), 1.0f);
            CreateCampusTree(_landscapingRoot, new Vector3(-36.0f, 0f, -24.0f), 1.15f);

            // East campus trees — framed around the parking bay with clear clearance
            CreateCampusTree(_landscapingRoot, new Vector3(68.0f, 0f, -28.0f), 1.10f);
            CreateCampusTree(_landscapingRoot, new Vector3(96.0f, 0f, -18.0f), 1.15f);
            CreateCampusTree(_landscapingRoot, new Vector3(96.0f, 0f,  14.0f), 1.10f);
            CreateCampusTree(_landscapingRoot, new Vector3(68.0f, 0f,  24.0f), 1.05f);

            // North / Rear campus trees
            CreateCampusTree(_landscapingRoot, new Vector3( 5.0f, 0f, 22.0f), 1.05f);
            CreateCampusTree(_landscapingRoot, new Vector3(24.0f, 0f, 22.0f), 1.10f);
            CreateCampusTree(_landscapingRoot, new Vector3(43.0f, 0f, 22.0f), 1.05f);

            // 7. Dedicated East Campus Bus Parking Pad (Clean asphalt parking apron with stall markings)
            CreateBox(_landscapingRoot, "BusParkingPad", new Vector3(82.0f, -0.02f, 0.0f), new Vector3(18.0f, 0.08f, 34.0f), _slabTrimMaterial);
            // White stall divider demarcations
            CreateBox(_landscapingRoot, "ParkingStallLine_1", new Vector3(82.0f, 0.025f,  0.0f), new Vector3(16.0f, 0.02f, 0.25f), _windowFrameMaterial);
            CreateBox(_landscapingRoot, "ParkingStallLine_2", new Vector3(82.0f, 0.025f, 16.0f), new Vector3(16.0f, 0.02f, 0.25f), _windowFrameMaterial);
            CreateBox(_landscapingRoot, "ParkingStallLine_3", new Vector3(82.0f, 0.025f, -16.0f), new Vector3(16.0f, 0.02f, 0.25f), _windowFrameMaterial);

            // Authentic bright yellow Sathyabama University buses sitting securely in parking bays
            CreateCampusBus(_landscapingRoot, new Vector3(82.0f, 0f, -8.0f), 0f);
            CreateCampusBus(_landscapingRoot, new Vector3(82.0f, 0f,  8.0f), 0f);

            // 8. Celestial Sun in the sky (Positioned high East-South-East in celestial distance, clear of building & signage)
            var sunGO = new GameObject("SunDisc");
            sunGO.transform.SetParent(_landscapingRoot.transform, false);
            sunGO.transform.localPosition = new Vector3(95f, 85f, -55f);

            var sunSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sunSphere.name = "SunCore";
            sunSphere.transform.SetParent(sunGO.transform, false);
            sunSphere.transform.localScale = new Vector3(7.6f, 7.6f, 7.6f);
            var sunMat = CreateColorMaterial(GetSafeShader("Standard"), new Color(1.0f, 0.98f, 0.90f, 1f), false);
            if (sunMat != null)
            {
                sunMat.EnableKeyword("_EMISSION");
                sunMat.SetColor("_EmissionColor", new Color(1.0f, 0.96f, 0.85f) * 1.5f);
            }
            var sunRend = sunSphere.GetComponent<MeshRenderer>();
            if (sunRend != null) sunRend.sharedMaterial = sunMat;
            var sunCol = sunSphere.GetComponent<Collider>();
            if (sunCol != null) Destroy(sunCol);

            // Soft atmospheric warm glow corona (subtle, non-distracting)
            var coronaGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coronaGO.name = "SunCorona";
            coronaGO.transform.SetParent(sunGO.transform, false);
            coronaGO.transform.localScale = new Vector3(13.5f, 13.5f, 3.0f);
            var coronaMat = CreateColorMaterial(GetSafeShader("Standard"), new Color(1.0f, 0.94f, 0.78f, 0.10f), true);
            var coronaRend = coronaGO.GetComponent<MeshRenderer>();
            if (coronaRend != null) coronaRend.sharedMaterial = coronaMat;
            var coronaCol = coronaGO.GetComponent<Collider>();
            if (coronaCol != null) Destroy(coronaCol);
        }

        /// <summary>
        /// Creates a realistic bright yellow Sathyabama University bus with body, windows, frames,
        /// passenger doors, mirrors, headlights, indicators, bumpers, grille, wheel arches,
        /// dark tyres, silver rims, and legible "SATHYABAMA" university fleet branding.
        /// </summary>
        private void CreateCampusBus(GameObject parent, Vector3 pos, float yRotation)
        {
            var busRoot = new GameObject("SathyabamaBus");
            busRoot.transform.SetParent(parent.transform, false);
            busRoot.transform.localPosition = pos;
            busRoot.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

            // Palette matching actual Sathyabama university fleet
            var busYellowMat    = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.98f, 0.73f, 0.05f, 1f), false); // Sathyabama golden yellow
            var busDarkSkirtMat = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.18f, 0.18f, 0.20f, 1f), false); // Charcoal lower skirt / bumper
            var busWhiteRoofMat = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.94f, 0.95f, 0.96f, 1f), false); // White roof cap
            var busGlassMat     = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.08f, 0.10f, 0.14f, 0.92f), true);  // Deep black tint
            var tyreMat         = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.12f, 0.12f, 0.13f, 1f), false); // Dark rubber
            var rimMat          = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.80f, 0.82f, 0.85f, 1f), false); // Silver metallic rim
            var headlightMat    = CreateColorMaterial(GetSafeShader("Standard"), new Color(1.0f, 0.98f, 0.90f, 1f), false);
            var amberMat        = CreateColorMaterial(GetSafeShader("Standard"), new Color(1.0f, 0.65f, 0.05f, 1f), false);
            var taillightRedMat = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.85f, 0.10f, 0.10f, 1f), false);

            float busLength = 10.6f;
            float busWidth  = 2.50f;
            float halfLen   = busLength * 0.5f; // 5.3m

            // 1. Main Yellow Body Shell (height from Y=0.70m to 2.80m)
            CreateBox(busRoot, "BodyShell", new Vector3(0f, 1.75f, 0f), new Vector3(busWidth, 2.10f, busLength), busYellowMat);

            // 2. Charcoal Protective Lower Skirt / Rocker Trim (Y=0.35m to 0.70m)
            CreateBox(busRoot, "LowerSkirt", new Vector3(0f, 0.52f, 0f), new Vector3(busWidth + 0.02f, 0.35f, busLength), busDarkSkirtMat);

            // 3. Aerodynamic White Roof Cap
            CreateBox(busRoot, "WhiteRoof", new Vector3(0f, 2.92f, 0f), new Vector3(busWidth - 0.06f, 0.25f, busLength - 0.2f), busWhiteRoofMat);
            // Roof AC Unit & Ventilation Housings
            CreateBox(busRoot, "RoofAC_Unit", new Vector3(0f, 3.12f, -0.6f), new Vector3(1.60f, 0.22f, 2.80f), _exteriorWallMaterial);
            CreateBox(busRoot, "RoofVent_1", new Vector3(0f, 3.08f, -3.2f), new Vector3(0.80f, 0.14f, 0.80f), busWhiteRoofMat);
            CreateBox(busRoot, "RoofVent_2", new Vector3(0f, 3.08f,  2.2f), new Vector3(0.80f, 0.14f, 0.80f), busWhiteRoofMat);

            // 4. Windows — Continuous Tinted Glass Ribbon with Mullion Frames
            // Side window bands on Left & Right
            CreateBox(busRoot, "WindowRibbon_L", new Vector3(-(busWidth * 0.5f) - 0.01f, 2.22f, 0.1f), new Vector3(0.04f, 0.82f, 8.8f), busGlassMat);
            CreateBox(busRoot, "WindowRibbon_R", new Vector3( (busWidth * 0.5f) + 0.01f, 2.22f, 0.1f), new Vector3(0.04f, 0.82f, 8.8f), busGlassMat);

            // Window Mullions (vertical black dividing pillars between windows)
            float[] mullionZ = { -3.8f, -2.4f, -1.0f, 0.4f, 1.8f, 3.2f, 4.4f };
            for (int m = 0; m < mullionZ.Length; m++)
            {
                CreateBox(busRoot, $"Mullion_L_{m}", new Vector3(-(busWidth * 0.5f) - 0.02f, 2.22f, mullionZ[m]), new Vector3(0.06f, 0.84f, 0.10f), busDarkSkirtMat);
                CreateBox(busRoot, $"Mullion_R_{m}", new Vector3( (busWidth * 0.5f) + 0.02f, 2.22f, mullionZ[m]), new Vector3(0.06f, 0.84f, 0.10f), busDarkSkirtMat);
            }

            // 5. Front Cab & Fascia
            // Front Windshield (tilted black glass)
            var wShield = CreateBox(busRoot, "Windshield", new Vector3(0f, 2.20f, -halfLen - 0.02f), new Vector3(busWidth - 0.14f, 0.95f, 0.10f), busGlassMat);
            wShield.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            // Windshield wiper center divider
            CreateBox(busRoot, "WiperDivider", new Vector3(0f, 2.20f, -halfLen - 0.04f), new Vector3(0.05f, 0.85f, 0.08f), busDarkSkirtMat);

            // Destination Display Box above windshield
            CreateBox(busRoot, "DestDisplayBox", new Vector3(0f, 2.76f, -halfLen - 0.02f), new Vector3(1.70f, 0.28f, 0.12f), _badgeMaterial);
            var destTmpGO = new GameObject("DestText");
            destTmpGO.transform.SetParent(busRoot.transform, false);
            destTmpGO.transform.localPosition = new Vector3(0f, 2.76f, -halfLen - 0.09f);
            var dTmp = destTmpGO.AddComponent<TextMeshPro>();
            dTmp.text = "<b><color=#FBBF24>SATHYABAMA</color></b>";
            dTmp.fontSize = 1.3f;
            dTmp.fontStyle = FontStyles.Bold;
            dTmp.alignment = TextAlignmentOptions.Center;
            dTmp.enableWordWrapping = false;
            dTmp.rectTransform.sizeDelta = new Vector2(1.6f, 0.26f);

            // Front Radiator Grille
            CreateBox(busRoot, "FrontGrille", new Vector3(0f, 1.25f, -halfLen - 0.02f), new Vector3(1.50f, 0.55f, 0.08f), busDarkSkirtMat);
            // Grille horizontal chrome slats
            CreateBox(busRoot, "GrilleSlat_1", new Vector3(0f, 1.35f, -halfLen - 0.04f), new Vector3(1.40f, 0.04f, 0.06f), rimMat);
            CreateBox(busRoot, "GrilleSlat_2", new Vector3(0f, 1.20f, -halfLen - 0.04f), new Vector3(1.40f, 0.04f, 0.06f), rimMat);

            // Heavy Front Bumper
            CreateBox(busRoot, "FrontBumper", new Vector3(0f, 0.65f, -halfLen - 0.08f), new Vector3(busWidth + 0.08f, 0.38f, 0.28f), busDarkSkirtMat);

            // Headlight Clusters (Left & Right)
            float hlX = (busWidth * 0.5f) - 0.40f;
            CreateBox(busRoot, "Headlight_L", new Vector3(-hlX, 1.10f, -halfLen - 0.03f), new Vector3(0.32f, 0.22f, 0.06f), headlightMat);
            CreateBox(busRoot, "Headlight_R", new Vector3( hlX, 1.10f, -halfLen - 0.03f), new Vector3(0.32f, 0.22f, 0.06f), headlightMat);
            CreateBox(busRoot, "Indicator_L", new Vector3(-hlX - 0.22f, 1.10f, -halfLen - 0.03f), new Vector3(0.12f, 0.20f, 0.06f), amberMat);
            CreateBox(busRoot, "Indicator_R", new Vector3( hlX + 0.22f, 1.10f, -halfLen - 0.03f), new Vector3(0.12f, 0.20f, 0.06f), amberMat);

            // Side Mirrors (Left & Right extending brackets + mirror pods)
            float mirrorX = (busWidth * 0.5f) + 0.28f;
            CreateBox(busRoot, "MirrorBracket_L", new Vector3(-mirrorX * 0.75f, 2.25f, -halfLen + 0.35f), new Vector3(0.40f, 0.06f, 0.06f), busDarkSkirtMat);
            CreateBox(busRoot, "MirrorPod_L", new Vector3(-mirrorX, 2.20f, -halfLen + 0.35f), new Vector3(0.12f, 0.36f, 0.18f), busDarkSkirtMat);
            CreateBox(busRoot, "MirrorBracket_R", new Vector3( mirrorX * 0.75f, 2.25f, -halfLen + 0.35f), new Vector3(0.40f, 0.06f, 0.06f), busDarkSkirtMat);
            CreateBox(busRoot, "MirrorPod_R", new Vector3( mirrorX, 2.20f, -halfLen + 0.35f), new Vector3(0.12f, 0.36f, 0.18f), busDarkSkirtMat);

            // 6. Passenger Door (Left curb side near front)
            float doorZ = -halfLen + 1.25f;
            CreateBox(busRoot, "PassengerDoorFrame", new Vector3(-(busWidth * 0.5f) - 0.01f, 1.45f, doorZ), new Vector3(0.04f, 1.85f, 0.95f), busDarkSkirtMat);
            CreateBox(busRoot, "PassengerDoorGlass_Top", new Vector3(-(busWidth * 0.5f) - 0.02f, 1.85f, doorZ), new Vector3(0.04f, 0.70f, 0.75f), busGlassMat);
            CreateBox(busRoot, "PassengerDoorGlass_Btm", new Vector3(-(busWidth * 0.5f) - 0.02f, 0.95f, doorZ), new Vector3(0.04f, 0.55f, 0.75f), busGlassMat);

            // 7. Rear Section
            CreateBox(busRoot, "RearWindow", new Vector3(0f, 2.20f, halfLen + 0.02f), new Vector3(busWidth - 0.40f, 0.75f, 0.08f), busGlassMat);
            CreateBox(busRoot, "RearBumper", new Vector3(0f, 0.65f, halfLen + 0.08f), new Vector3(busWidth + 0.08f, 0.38f, 0.28f), busDarkSkirtMat);
            CreateBox(busRoot, "Taillight_L", new Vector3(-hlX, 1.15f, halfLen + 0.03f), new Vector3(0.18f, 0.42f, 0.06f), taillightRedMat);
            CreateBox(busRoot, "Taillight_R", new Vector3( hlX, 1.15f, halfLen + 0.03f), new Vector3(0.18f, 0.42f, 0.06f), taillightRedMat);
            CreateBox(busRoot, "SafetyReflector", new Vector3(0f, 0.62f, halfLen + 0.16f), new Vector3(1.60f, 0.08f, 0.04f), taillightRedMat);

            // 8. Wheels & Tyres (Grounded at Y=0.50m with radius 0.50m)
            float frontAxleZ = -halfLen + 2.5f; // Z = -2.8m
            float rearAxleZ  =  halfLen - 2.6f; // Z = +2.7m

            // Front Axle (Single Tyres on each side)
            CreateWheel(busRoot, new Vector3(-(busWidth * 0.5f), 0.50f, frontAxleZ), tyreMat, rimMat);
            CreateWheel(busRoot, new Vector3( (busWidth * 0.5f), 0.50f, frontAxleZ), tyreMat, rimMat);

            // Rear Axle (Dual Tyres on each side for heavy university coach)
            CreateWheel(busRoot, new Vector3(-(busWidth * 0.5f) + 0.08f, 0.50f, rearAxleZ), tyreMat, rimMat);
            CreateWheel(busRoot, new Vector3(-(busWidth * 0.5f) - 0.12f, 0.50f, rearAxleZ), tyreMat, rimMat);
            CreateWheel(busRoot, new Vector3( (busWidth * 0.5f) - 0.08f, 0.50f, rearAxleZ), tyreMat, rimMat);
            CreateWheel(busRoot, new Vector3( (busWidth * 0.5f) + 0.12f, 0.50f, rearAxleZ), tyreMat, rimMat);

            // 9. Authentic SATHYABAMA University Fleet Branding (Both sides)
            // Left Side Branding
            var brandLGO = new GameObject("Brand_Left");
            brandLGO.transform.SetParent(busRoot.transform, false);
            brandLGO.transform.localPosition = new Vector3(-(busWidth * 0.5f) - 0.03f, 1.35f, 0.4f);
            brandLGO.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            var bTmpL = brandLGO.AddComponent<TextMeshPro>();
            bTmpL.text = "<b><color=#0F172A>SATHYABAMA</color></b>\n<size=50%><color=#1E293B>INSTITUTE OF SCIENCE & TECHNOLOGY</color></size>";
            bTmpL.fontSize = 1.9f;
            bTmpL.fontStyle = FontStyles.Bold;
            bTmpL.alignment = TextAlignmentOptions.Center;
            bTmpL.enableWordWrapping = false;
            bTmpL.rectTransform.sizeDelta = new Vector2(6.0f, 0.90f);

            // Right Side Branding
            var brandRGO = new GameObject("Brand_Right");
            brandRGO.transform.SetParent(busRoot.transform, false);
            brandRGO.transform.localPosition = new Vector3((busWidth * 0.5f) + 0.03f, 1.35f, 0.4f);
            brandRGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var bTmpR = brandRGO.AddComponent<TextMeshPro>();
            bTmpR.text = "<b><color=#0F172A>SATHYABAMA</color></b>\n<size=50%><color=#1E293B>INSTITUTE OF SCIENCE & TECHNOLOGY</color></size>";
            bTmpR.fontSize = 1.9f;
            bTmpR.fontStyle = FontStyles.Bold;
            bTmpR.alignment = TextAlignmentOptions.Center;
            bTmpR.enableWordWrapping = false;
            bTmpR.rectTransform.sizeDelta = new Vector2(6.0f, 0.90f);
        }

        private void CreateWheel(GameObject parent, Vector3 localPos, Material tyreMat, Material rimMat)
        {
            var wheelRoot = new GameObject("Wheel");
            wheelRoot.transform.SetParent(parent.transform, false);
            wheelRoot.transform.localPosition = localPos;

            // Outer Tyre (Cylinder turned 90 deg along Z)
            var tyre = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tyre.name = "Tyre";
            tyre.transform.SetParent(wheelRoot.transform, false);
            tyre.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tyre.transform.localScale = new Vector3(1.00f, 0.26f, 1.00f);
            var tRend = tyre.GetComponent<MeshRenderer>();
            if (tRend != null) tRend.sharedMaterial = tyreMat;
            var tCol = tyre.GetComponent<Collider>();
            if (tCol != null) Destroy(tCol);

            // Inner Metallic Rim
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(wheelRoot.transform, false);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localScale = new Vector3(0.64f, 0.28f, 0.64f);
            var rRend = rim.GetComponent<MeshRenderer>();
            if (rRend != null) rRend.sharedMaterial = rimMat;
            var rCol = rim.GetComponent<Collider>();
            if (rCol != null) Destroy(rCol);
        }

        private void CreateCampusTree(GameObject parent, Vector3 pos, float scale)
        {
            var treeGO = new GameObject("CampusTree");
            treeGO.transform.SetParent(parent.transform, false);
            treeGO.transform.localPosition = pos;

            // --- Trunk: tapered brown cylinder ---
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeGO.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.40f * scale, 0f);
            trunk.transform.localScale = new Vector3(0.32f * scale, 1.40f * scale, 0.32f * scale);
            var trRend = trunk.GetComponent<MeshRenderer>();
            if (trRend != null) trRend.sharedMaterial = _furnitureWoodMaterial;
            var trCol = trunk.GetComponent<Collider>(); if (trCol != null) Destroy(trCol);

            // --- Lower branch stumps for structural realism ---
            for (int b = 0; b < 3; b++)
            {
                float bAngle = b * 120f * Mathf.Deg2Rad;
                var branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = $"Branch_{b}";
                branch.transform.SetParent(treeGO.transform, false);
                branch.transform.localPosition = new Vector3(Mathf.Sin(bAngle) * 0.40f * scale, 2.30f * scale, Mathf.Cos(bAngle) * 0.40f * scale);
                branch.transform.localRotation = Quaternion.Euler(30f, b * 120f, 0f);
                branch.transform.localScale    = new Vector3(0.10f * scale, 0.45f * scale, 0.10f * scale);
                var bRend = branch.GetComponent<MeshRenderer>();
                if (bRend != null) bRend.sharedMaterial = _furnitureWoodMaterial;
                var bCol = branch.GetComponent<Collider>(); if (bCol != null) Destroy(bCol);
            }

            // --- Three-layer foliage canopy for realistic profile ---
            // Bottom wide skirt
            var f0 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            f0.name = "Canopy_Skirt";
            f0.transform.SetParent(treeGO.transform, false);
            f0.transform.localPosition = new Vector3(0f, 2.75f * scale, 0f);
            f0.transform.localScale = new Vector3(3.2f * scale, 1.60f * scale, 3.2f * scale);
            var f0R = f0.GetComponent<MeshRenderer>(); if (f0R != null) f0R.sharedMaterial = _treeFoliageMaterial;
            var f0C = f0.GetComponent<Collider>(); if (f0C != null) Destroy(f0C);

            // Middle mid canopy (slightly darker tone re-using same mat)
            var f1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            f1.name = "Canopy_Mid";
            f1.transform.SetParent(treeGO.transform, false);
            f1.transform.localPosition = new Vector3(0f, 3.65f * scale, 0f);
            f1.transform.localScale = new Vector3(2.60f * scale, 2.00f * scale, 2.60f * scale);
            var f1R = f1.GetComponent<MeshRenderer>(); if (f1R != null) f1R.sharedMaterial = _treeFoliageMaterial;
            var f1C = f1.GetComponent<Collider>(); if (f1C != null) Destroy(f1C);

            // Top crown (narrower, taller, slightly lighter green)
            var topMat = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.28f, 0.58f, 0.28f, 1f), false);
            var f2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            f2.name = "Canopy_Top";
            f2.transform.SetParent(treeGO.transform, false);
            f2.transform.localPosition = new Vector3(0f, 4.70f * scale, 0f);
            f2.transform.localScale = new Vector3(1.70f * scale, 2.00f * scale, 1.70f * scale);
            var f2R = f2.GetComponent<MeshRenderer>(); if (f2R != null) f2R.sharedMaterial = topMat;
            var f2C = f2.GetComponent<Collider>(); if (f2C != null) Destroy(f2C);
        }

        private void BuildStPaulsSignage(GameObject parent)
        {
            // 1. Prominent Roof Parapet Signboard: "SATHYABAMA INSTITUTE" & St. Paul's Block
            var roofSignGO = new GameObject("RoofSign_StPauls");
            roofSignGO.transform.SetParent(parent.transform, false);
            // Cleanly elevated atop parapet coping (Y=14.15m), anchored with structural mounting posts
            roofSignGO.transform.localPosition = new Vector3(23.5f, 14.15f, -11.0f);
            roofSignGO.transform.localRotation = Quaternion.identity;

            // Structural mounting upright posts anchored down to parapet coping
            CreateBox(roofSignGO, "MountStrut_L", new Vector3(-8.0f, -0.65f, 0f), new Vector3(0.24f, 1.20f, 0.24f), _slabTrimMaterial);
            CreateBox(roofSignGO, "MountStrut_R", new Vector3( 8.0f, -0.65f, 0f), new Vector3(0.24f, 1.20f, 0.24f), _slabTrimMaterial);

            // Main architectural backing panel
            var signBacking = CreateBox(roofSignGO, "SignBacking", new Vector3(0f, 0f, 0.05f), new Vector3(34.0f, 2.15f, 0.20f), _badgeMaterial);

            // Top and bottom terracotta framing trims
            CreateBox(roofSignGO, "SignTrim_Top", new Vector3(0f, 1.08f, 0.06f), new Vector3(34.3f, 0.12f, 0.24f), _terracottaBandMaterial);
            CreateBox(roofSignGO, "SignTrim_Btm", new Vector3(0f, -1.08f, 0.06f), new Vector3(34.3f, 0.12f, 0.24f), _terracottaBandMaterial);

            // Left terracotta decorative accent bar
            var accent = CreateBox(roofSignGO, "SignAccent", new Vector3(-16.7f, 0f, 0.02f), new Vector3(0.50f, 2.10f, 0.24f), _terracottaBandMaterial);

            var tmp = roofSignGO.AddComponent<TextMeshPro>();
            tmp.text = "<b>SATHYABAMA INSTITUTE OF SCIENCE AND TECHNOLOGY</b>\n<size=60%><color=#38BDF8><b>St. Paul's Block</b></color> <color=#E2E8F0>— Department of Computer Science & Engineering</color></size>";
            tmp.fontSize = 3.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.28f;
            tmp.outlineColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(33.0f, 2.0f);

            // 2. West Gable Side Signboard
            var westSignGO = new GameObject("WestSign_StPauls");
            westSignGO.transform.SetParent(parent.transform, false);
            westSignGO.transform.localPosition = new Vector3(-6.95f, 12.8f, 0.0f);
            westSignGO.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            var wBacking = CreateBox(westSignGO, "WestBacking", Vector3.zero, new Vector3(12.0f, 2.0f, 0.18f), _badgeMaterial);

            var wTmp = westSignGO.AddComponent<TextMeshPro>();
            wTmp.text = "<b>SATHYABAMA</b>\n<size=75%><color=#38BDF8>St. Paul's Block</color></size>";
            wTmp.fontSize = 3.4f;
            wTmp.fontStyle = FontStyles.Bold;
            wTmp.alignment = TextAlignmentOptions.Center;
            wTmp.color = Color.white;
            wTmp.outlineWidth = 0.30f;
            wTmp.outlineColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            wTmp.enableWordWrapping = false;
            wTmp.rectTransform.sizeDelta = new Vector2(11.5f, 1.9f);
        }

        private GameObject CreateBox(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent.transform, false);
            cube.transform.localPosition = pos;
            cube.transform.localScale = scale;

            var rend = cube.GetComponent<MeshRenderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;

            var col = cube.GetComponent<Collider>();
            if (col != null) Destroy(col);

            return cube;
        }

        // =========================================================================================
        // LOW-POLY PEOPLE & CHARACTER POPULATION
        // =========================================================================================

        /// <summary>
        /// Creates a simple low-poly humanoid figure (capsule body + sphere head) at the given world position.
        /// bodyColor controls the shirt/body material; all characters share a skin-tone head.
        /// </summary>
        private void CreatePerson(GameObject parent, string floorId, string personName, Vector3 worldPos, Color bodyColor, float facingAngleY = 0f)
        {
            // Varied skin tones for realism — deterministically chosen from name hash
            int nameHash = personName.GetHashCode();
            float skinR = 0.72f + (nameHash & 0x3F) / 255f * 0.28f;
            float skinG = 0.52f + ((nameHash >> 6) & 0x1F) / 255f * 0.22f;
            float skinB = 0.38f + ((nameHash >> 11) & 0x0F) / 255f * 0.14f;
            var skinToneMat = CreateColorMaterial(GetSafeShader("Standard"), new Color(skinR, skinG, skinB, 1f), false);

            var bodyMat     = CreateColorMaterial(GetSafeShader("Standard"), bodyColor, false);
            // Trousers: slightly varied dark tones per person
            float troGrey = 0.14f + ((nameHash >> 4) & 0xF) / 255f * 0.12f;
            var trouserMat  = CreateColorMaterial(GetSafeShader("Standard"), new Color(troGrey, troGrey + 0.02f, troGrey + 0.06f, 1f), false);
            // Shoes: near-black
            var shoesMat    = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.10f, 0.08f, 0.08f, 1f), false);
            // Hair: dark brown / black varied
            float hairDark = 0.06f + ((nameHash >> 8) & 0xF) / 255f * 0.14f;
            var hairMat     = CreateColorMaterial(GetSafeShader("Standard"), new Color(hairDark + 0.08f, hairDark + 0.04f, hairDark, 1f), false);
            // Collar: white shirt collar strip
            var collarMat   = CreateColorMaterial(GetSafeShader("Standard"), new Color(0.95f, 0.95f, 0.96f, 1f), false);

            var personRoot = new GameObject(personName);
            personRoot.transform.SetParent(parent.transform, false);
            personRoot.transform.localPosition = worldPos;
            personRoot.transform.localRotation = Quaternion.Euler(0f, facingAngleY, 0f);

            // --- Torso (shirt): tall capsule, proportional collegiate build ---
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Torso";
            body.transform.SetParent(personRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.50f, 0f);
            body.transform.localScale    = new Vector3(0.22f, 0.22f, 0.20f); // slightly flatter than default capsule
            var bRend = body.GetComponent<MeshRenderer>();
            if (bRend != null) { bRend.sharedMaterial = bodyMat; RegisterRenderer(floorId, bRend); }
            var bCol = body.GetComponent<Collider>(); if (bCol != null) Destroy(bCol);

            // --- Collar strip (white shirt collar visible at neck) ---
            var collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collar.name = "Collar";
            collar.transform.SetParent(personRoot.transform, false);
            collar.transform.localPosition = new Vector3(0f, 0.68f, 0.04f);
            collar.transform.localScale    = new Vector3(0.10f, 0.04f, 0.08f);
            var colRend = collar.GetComponent<MeshRenderer>();
            if (colRend != null) { colRend.sharedMaterial = collarMat; RegisterRenderer(floorId, colRend); }
            var colCol = collar.GetComponent<Collider>(); if (colCol != null) Destroy(colCol);

            // --- Hips / Trousers upper ---
            var hips = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hips.name = "Hips";
            hips.transform.SetParent(personRoot.transform, false);
            hips.transform.localPosition = new Vector3(0f, 0.26f, 0f);
            hips.transform.localScale    = new Vector3(0.20f, 0.10f, 0.16f);
            var hiRend = hips.GetComponent<MeshRenderer>();
            if (hiRend != null) { hiRend.sharedMaterial = trouserMat; RegisterRenderer(floorId, hiRend); }
            var hiCol = hips.GetComponent<Collider>(); if (hiCol != null) Destroy(hiCol);

            // --- Legs (trousers lower) ---
            var legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legs.name = "Legs";
            legs.transform.SetParent(personRoot.transform, false);
            legs.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            legs.transform.localScale    = new Vector3(0.18f, 0.20f, 0.14f);
            var lRend = legs.GetComponent<MeshRenderer>();
            if (lRend != null) { lRend.sharedMaterial = trouserMat; RegisterRenderer(floorId, lRend); }
            var lCol = legs.GetComponent<Collider>(); if (lCol != null) Destroy(lCol);

            // --- Shoes ---
            var shoes = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shoes.name = "Shoes";
            shoes.transform.SetParent(personRoot.transform, false);
            shoes.transform.localPosition = new Vector3(0f, 0.01f, 0.02f);
            shoes.transform.localScale    = new Vector3(0.19f, 0.06f, 0.18f);
            var shRend = shoes.GetComponent<MeshRenderer>();
            if (shRend != null) { shRend.sharedMaterial = shoesMat; RegisterRenderer(floorId, shRend); }
            var shCol = shoes.GetComponent<Collider>(); if (shCol != null) Destroy(shCol);

            // --- Head: slightly oval, not a perfect sphere ---
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(personRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.80f, 0f);
            head.transform.localScale    = new Vector3(0.18f, 0.20f, 0.17f);
            var hRend = head.GetComponent<MeshRenderer>();
            if (hRend != null) { hRend.sharedMaterial = skinToneMat; RegisterRenderer(floorId, hRend); }
            var hCol = head.GetComponent<Collider>(); if (hCol != null) Destroy(hCol);

            // --- Hair cap (top half sphere) ---
            var hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair";
            hair.transform.SetParent(personRoot.transform, false);
            hair.transform.localPosition = new Vector3(0f, 0.87f, -0.01f);
            hair.transform.localScale    = new Vector3(0.185f, 0.13f, 0.175f);
            var haRend = hair.GetComponent<MeshRenderer>();
            if (haRend != null) { haRend.sharedMaterial = hairMat; RegisterRenderer(floorId, haRend); }
            var haCol = hair.GetComponent<Collider>(); if (haCol != null) Destroy(haCol);

            // --- Left arm ---
            var lArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            lArm.name = "Arm_L";
            lArm.transform.SetParent(personRoot.transform, false);
            lArm.transform.localPosition = new Vector3(-0.155f, 0.50f, 0f);
            lArm.transform.localRotation = Quaternion.Euler(0f, 0f, 18f); // slight natural hang
            lArm.transform.localScale    = new Vector3(0.07f, 0.16f, 0.07f);
            var laRend = lArm.GetComponent<MeshRenderer>();
            if (laRend != null) { laRend.sharedMaterial = bodyMat; RegisterRenderer(floorId, laRend); }
            var laCol = lArm.GetComponent<Collider>(); if (laCol != null) Destroy(laCol);

            // --- Right arm ---
            var rArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rArm.name = "Arm_R";
            rArm.transform.SetParent(personRoot.transform, false);
            rArm.transform.localPosition = new Vector3(0.155f, 0.50f, 0f);
            rArm.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            rArm.transform.localScale    = new Vector3(0.07f, 0.16f, 0.07f);
            var raRend = rArm.GetComponent<MeshRenderer>();
            if (raRend != null) { raRend.sharedMaterial = bodyMat; RegisterRenderer(floorId, raRend); }
            var raCol = rArm.GetComponent<Collider>(); if (raCol != null) Destroy(raCol);
        }

        /// <summary>
        /// Places appropriate low-poly character figures (teacher + students, or faculty)
        /// inside the room based on destination type and ID. Guards against duplicate spawning
        /// via the existing floorId renderer registration pattern.
        /// Characters are seated (Y offset is applied) or standing at their natural positions.
        /// </summary>
        private void PopulateRoomWithPeople(GameObject roomGO, NavNode node, DestinationData dest)
        {
            if (dest == null) return;

            string destId   = dest.destinationId;
            DestinationType dtype = dest.destinationType;

            // Teacher shirt: dark navy; Student shirts: varied muted tones
            Color teacherColor  = new Color(0.10f, 0.18f, 0.36f, 1f);
            Color student1Color = new Color(0.55f, 0.25f, 0.20f, 1f); // terracotta-red
            Color student2Color = new Color(0.20f, 0.40f, 0.25f, 1f); // muted green
            Color student3Color = new Color(0.30f, 0.20f, 0.50f, 1f); // muted purple
            Color student4Color = new Color(0.45f, 0.38f, 0.20f, 1f); // tan khaki
            Color facultyColor  = new Color(0.14f, 0.28f, 0.48f, 1f); // smart blue

            float depth  = 5.5f;
            float frontZ = node.z > 0 ? (node.z - depth * 0.5f) : (node.z + depth * 0.5f);
            float backZ  = node.z > 0 ? (node.z + depth * 0.5f) : (node.z - depth * 0.5f);
            // Teacher faces away from back wall (toward class): angle depends on which side corridor is
            float teacherFacingAngle = node.z > 0 ? 180f : 0f;
            float studentFacingAngle = node.z > 0 ? 0f   : 180f; // students face front

            if (dtype == DestinationType.Classroom || destId.StartsWith("DEST_F") && destId.Contains("_R"))
            {
                // Teacher at podium near back wall (standing, Y=0 relative to floor)
                float podiumZ = node.z > 0 ? (backZ - 1.5f) : (backZ + 1.5f);
                CreatePerson(roomGO, node.floorId, "Teacher", new Vector3(node.x - 0.5f, node.y, podiumZ), teacherColor, teacherFacingAngle);

                // Row 1 students seated at desk 1 (lifted 0.28m for seated height on bench)
                float row1Z = node.z > 0 ? (node.z + 0.05f) : (node.z - 0.05f);
                CreatePerson(roomGO, node.floorId, "Student_1A", new Vector3(node.x - 1.8f, node.y + 0.28f, row1Z), student1Color, studentFacingAngle);
                CreatePerson(roomGO, node.floorId, "Student_1B", new Vector3(node.x + 0.0f, node.y + 0.28f, row1Z), student2Color, studentFacingAngle);
                CreatePerson(roomGO, node.floorId, "Student_1C", new Vector3(node.x + 1.8f, node.y + 0.28f, row1Z), student3Color, studentFacingAngle);

                // Row 2 students
                float row2Z = node.z > 0 ? (node.z - 1.05f) : (node.z + 1.05f);
                CreatePerson(roomGO, node.floorId, "Student_2A", new Vector3(node.x - 1.8f, node.y + 0.28f, row2Z), student4Color, studentFacingAngle);
                CreatePerson(roomGO, node.floorId, "Student_2B", new Vector3(node.x + 0.0f, node.y + 0.28f, row2Z), student1Color, studentFacingAngle);
                CreatePerson(roomGO, node.floorId, "Student_2C", new Vector3(node.x + 1.8f, node.y + 0.28f, row2Z), student2Color, studentFacingAngle);
            }
            else if (dtype == DestinationType.Laboratory)
            {
                // Lab instructor standing at instructor station
                CreatePerson(roomGO, node.floorId, "LabInstructor", new Vector3(node.x - 2.5f, node.y, node.z), teacherColor, teacherFacingAngle);

                // Students seated at workbenches
                float[] monX = { -2.1f, -0.7f, 0.7f, 2.1f };
                Color[] sColors = { student1Color, student2Color, student3Color, student4Color };
                float benchNorthZ = node.z + 1.15f;
                float benchSouthZ = node.z - 1.15f;
                for (int m = 0; m < Mathf.Min(monX.Length, 2); m++)
                {
                    // Students at north bench, seated Y offset 0.28
                    CreatePerson(roomGO, node.floorId, $"LabStudentN_{m}", new Vector3(node.x + monX[m], node.y + 0.28f, benchNorthZ + (node.z > 0 ? -0.55f : 0.55f)), sColors[m], studentFacingAngle);
                    CreatePerson(roomGO, node.floorId, $"LabStudentS_{m}", new Vector3(node.x + monX[m], node.y + 0.28f, benchSouthZ + (node.z > 0 ? 0.55f : -0.55f)), sColors[m + 2 < sColors.Length ? m + 2 : m], node.z > 0 ? 180f : 0f);
                }
            }
            else if (dtype == DestinationType.FacultyRoom)
            {
                // 4 faculty members at their cubicle desks
                float[] cubX = { node.x - 2.0f, node.x + 2.0f };
                float[] cubZ = { node.z + 1.1f, node.z - 1.1f };
                Color[] fColors = { facultyColor, new Color(0.20f, 0.28f, 0.20f, 1f), new Color(0.40f, 0.22f, 0.12f, 1f), new Color(0.28f, 0.20f, 0.40f, 1f) };
                int ci = 0;
                for (int cx = 0; cx < cubX.Length; cx++)
                    for (int cz = 0; cz < cubZ.Length; cz++)
                    {
                        float angle = cz == 0 ? 180f : 0f;
                        CreatePerson(roomGO, node.floorId, $"Faculty_{ci}", new Vector3(cubX[cx], node.y + 0.28f, cubZ[cz]), fColors[ci % fColors.Length], angle);
                        ci++;
                    }
            }
            else if (destId == "DEST_F0_OFFICE")
            {
                // HOD at desk + 2 visitors
                CreatePerson(roomGO, node.floorId, "HOD_Staff", new Vector3(node.x - 1.4f, node.y + 0.28f, node.z + 1.0f), facultyColor, 180f);
                CreatePerson(roomGO, node.floorId, "Visitor_1",  new Vector3(node.x - 2.0f, node.y + 0.28f, node.z - 0.4f), student1Color, 90f);
                CreatePerson(roomGO, node.floorId, "Visitor_2",  new Vector3(node.x - 0.8f, node.y + 0.28f, node.z - 0.4f), student2Color, 90f);
            }
            else if (destId == "DEST_F0_CONF")
            {
                // Chair occupants around conference table
                float[] cOffsets = { -1.8f, -0.6f, 0.6f, 1.8f };
                Color[] cColors  = { facultyColor, student1Color, student2Color, student3Color };
                for (int c = 0; c < cOffsets.Length; c++)
                {
                    CreatePerson(roomGO, node.floorId, $"ConfPerson_N{c}", new Vector3(node.x + cOffsets[c], node.y + 0.28f, node.z + 1.25f), cColors[c], 0f);
                    CreatePerson(roomGO, node.floorId, $"ConfPerson_S{c}", new Vector3(node.x + cOffsets[c], node.y + 0.28f, node.z - 1.25f), cColors[(c + 2) % cColors.Length], 180f);
                }
            }
            else if (destId == "DEST_F2_SEMINAR")
            {
                // Presenter at podium + audience rows
                CreatePerson(roomGO, node.floorId, "Presenter", new Vector3(node.x - 2.5f, node.y + 0.55f, node.z), teacherColor, teacherFacingAngle);
                float[] rowX = { node.x + 0.2f, node.x + 1.6f, node.x + 3.0f };
                Color[] rowColors = { student1Color, student2Color, student3Color, student4Color };
                for (int r = 0; r < rowX.Length; r++)
                {
                    for (int s = 0; s < 2; s++)
                    {
                        CreatePerson(roomGO, node.floorId, $"Audience_{r}_{s}", new Vector3(rowX[r], node.y + 0.28f + r * 0.12f, node.z + (s == 0 ? -0.9f : 0.9f)), rowColors[(r * 2 + s) % rowColors.Length], teacherFacingAngle);
                    }
                }
            }
        }

        // =========================================================================================
        // ARCHITECTURAL CUTAWAY & FLOOR ISOLATION SYSTEM
        // =========================================================================================

        /// <summary>
        /// Controls the architectural cutaway state.
        /// When cutawayActive is false: The entire exterior (front facade, roof, entrance portico) is solid and sealed.
        /// When cutawayActive is true: The front facade and roof cut away so internal corridors, rooms, and 3D navigation paths are visible.
        /// </summary>
        public void SetCutawayMode(bool cutawayActive, int targetFloor = -1)
        {
            _isCutawayActive = cutawayActive;

            if (_frontWallGroup != null)
            {
                _frontWallGroup.SetActive(!cutawayActive);
            }

            if (_roofGroup != null)
            {
                _roofGroup.SetActive(!cutawayActive);
            }

            if (cutawayActive && targetFloor >= 0)
            {
                // Tiered Floor Cutaway:
                // targetFloor == 0: Show F0, hide F1 and F2 so upper floor slabs & walls never occlude Ground Floor
                // targetFloor == 1: Show F0 (foundation) and F1 (active), hide F2 so F2 slab & walls never occlude First Floor
                // targetFloor == 2: Show F0, F1, and F2 (with roof removed so F2 is fully visible)
                for (int f = 0; f < 3; f++)
                {
                    string fId = $"F{f}";
                    bool shouldBeVisible = f <= targetFloor;
                    SetFloorVisibility(fId, shouldBeVisible);
                    if (_exteriorFloorLevels[f] != null)
                    {
                        _exteriorFloorLevels[f].SetActive(shouldBeVisible);
                    }
                }
                SetActiveFloor($"F{targetFloor}");
            }
            else if (!cutawayActive)
            {
                for (int f = 0; f < 3; f++)
                {
                    if (_exteriorFloorLevels[f] != null)
                    {
                        _exteriorFloorLevels[f].SetActive(true);
                    }
                }
                ResetAllFloorsVisibility();
            }
        }

        /// <summary>
        /// Shows or hides an entire floor.
        /// </summary>
        public void SetFloorVisibility(string floorId, bool visible)
        {
            if (_floorObjects.TryGetValue(floorId, out var floorGO) && floorGO != null)
            {
                floorGO.SetActive(visible);
            }
        }

        /// <summary>
        /// Highlights the active floor by making its materials fully opaque while dimming other floors.
        /// </summary>
        public void SetActiveFloor(string floorId)
        {
            _currentActiveFloorId = floorId;

            foreach (var kvp in _floorRenderers)
            {
                bool isCurrent = string.Equals(kvp.Key, floorId, StringComparison.OrdinalIgnoreCase);

                foreach (var rend in kvp.Value)
                {
                    if (rend == null) continue;
                    var mat = rend.material;
                    if (mat != null && mat.HasProperty("_Color"))
                    {
                        var c = mat.color;
                        bool isSlab = rend.gameObject.name.StartsWith("Slab");
                        if (isCurrent)
                        {
                            c.a = 1.0f;
                        }
                        else
                        {
                            c.a = isSlab ? 0.40f : 0.60f;
                        }
                        mat.color = c;
                    }
                }
            }
        }

        /// <summary>
        /// Resets all floor materials to default visibility and opacities.
        /// </summary>
        public void ResetAllFloorsVisibility()
        {
            for (int f = 0; f < 3; f++)
            {
                SetFloorVisibility($"F{f}", true);
                if (_exteriorFloorLevels[f] != null)
                {
                    _exteriorFloorLevels[f].SetActive(true);
                }
            }

            foreach (var kvp in _floorObjects)
            {
                if (kvp.Value != null) kvp.Value.SetActive(true);
            }

            foreach (var kvp in _floorRenderers)
            {
                foreach (var rend in kvp.Value)
                {
                    if (rend == null) continue;
                    var mat = rend.material;
                    if (mat != null && mat.HasProperty("_Color"))
                    {
                        var c = mat.color;
                        c.a = 1.0f;
                        mat.color = c;
                    }
                }
            }
            _currentActiveFloorId = null;
        }
    }
}
