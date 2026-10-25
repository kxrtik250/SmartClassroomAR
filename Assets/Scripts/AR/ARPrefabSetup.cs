using System;
using UnityEngine;
using TMPro;

namespace SmartClassroomAR.AR
{
    /// <summary>
    /// Utility component that programmatically instantiates default fallback 3D prefabs
    /// for AR arrows, waypoint markers, and destination markers when none are assigned in the Inspector.
    /// Compatible with Unity 2022.3 LTS and TextMeshPro.
    /// </summary>
    public class ARPrefabSetup : MonoBehaviour
    {
        private Material _arrowMaterial;
        private Material _markerMaterial;
        private Material _destinationMaterial;

        /// <summary>
        /// Creates a 3D green directional arrow prefab oriented along the positive Z forward axis.
        /// Total length ~0.35m suitable for AR floor guidance.
        /// </summary>
        public GameObject CreateDefaultArrowPrefab()
        {
            var root = new GameObject("Default_AR_Arrow");

            // Shaft: Stretched cube along Z axis
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.03f, -0.06f);
            shaft.transform.localScale = new Vector3(0.06f, 0.03f, 0.16f);

            // Head: Triangular/wedge arrowhead formed by rotated cube
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "ArrowHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.03f, 0.06f);
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            head.transform.localScale = new Vector3(0.12f, 0.035f, 0.12f);

            // Apply vibrant green unlit material
            Material mat = GetOrCreateMaterial(ref _arrowMaterial, new Color(0.15f, 0.85f, 0.25f, 1f), "AR_Arrow_Green");
            ApplyMaterialToRenderers(shaft, mat);
            ApplyMaterialToRenderers(head, mat);

            // Add box collider encompassing the full arrow volume
            var boxCol = root.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.03f, 0f);
            boxCol.size = new Vector3(0.2f, 0.08f, 0.32f);

            return root;
        }

        /// <summary>
        /// Creates a small spherical waypoint marker prefab with blue unlit material.
        /// Scale ~0.15m.
        /// </summary>
        public GameObject CreateDefaultMarkerPrefab()
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Default_AR_WaypointMarker";
            marker.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            Material mat = GetOrCreateMaterial(ref _markerMaterial, new Color(0.2f, 0.55f, 1f, 1f), "AR_Marker_Blue");
            ApplyMaterialToRenderers(marker, mat);

            // Ensure sphere collider exists
            if (marker.GetComponent<SphereCollider>() == null)
            {
                marker.AddComponent<SphereCollider>();
            }

            return marker;
        }

        /// <summary>
        /// Creates a prominent destination marker prefab with red material and a floating 3D TextMeshPro label.
        /// Sphere scale ~0.3m.
        /// </summary>
        public GameObject CreateDefaultDestinationMarkerPrefab()
        {
            var root = new GameObject("Default_AR_DestinationMarker");

            // Main sphere beacon
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "BeaconSphere";
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            sphere.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            Material mat = GetOrCreateMaterial(ref _destinationMaterial, new Color(0.95f, 0.2f, 0.2f, 1f), "AR_Destination_Red");
            ApplyMaterialToRenderers(sphere, mat);

            // Floating TextMeshPro label above the beacon
            var textObj = new GameObject("DestinationLabel");
            textObj.transform.SetParent(root.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = "Destination";
            tmp.fontSize = 4.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);

            // Ensure collider on root for hit testing
            var col = root.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.2f, 0f);
            col.radius = 0.25f;

            return root;
        }

        /// <summary>
        /// Safely finds or creates an unlit/standard shader material with the given color.
        /// </summary>
        private Material GetOrCreateMaterial(ref Material cachedMaterial, Color color, string matName)
        {
            if (cachedMaterial != null)
            {
                return cachedMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }
            if (shader == null)
            {
                shader = Shader.Find("Hidden/InternalErrorShader");
            }

            if (shader == null)
            {
                Debug.LogError("[ARPrefabSetup] Unable to find suitable shader for AR materials.");
                return null;
            }

            cachedMaterial = new Material(shader)
            {
                name = matName,
                color = color
            };

            // Set URP BaseColor property if present
            if (cachedMaterial.HasProperty("_BaseColor"))
            {
                cachedMaterial.SetColor("_BaseColor", color);
            }

            return cachedMaterial;
        }

        /// <summary>
        /// Applies material to all MeshRenderers in target GameObject hierarchy.
        /// </summary>
        private void ApplyMaterialToRenderers(GameObject target, Material material)
        {
            if (target == null || material == null) return;

            var renderers = target.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }
    }
}
