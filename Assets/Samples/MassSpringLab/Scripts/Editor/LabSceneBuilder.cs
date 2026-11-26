using System.Collections.Generic;
using MassSpringLab.Interaction;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Builds the Phase 1 Mass–Spring VR Lab scene with clean modular zones,
    /// URP dark-charcoal/cyan/lime-green styling, XR Origin, and Desktop Play Mode support.
    /// </summary>
    public static class LabSceneBuilder
    {
        private const string ScenePath = "Assets/MassSpringLab/Scenes/MassSpringLab.unity";
        private const string MaterialsDir = "Assets/MassSpringLab/Materials";
        private const string XROriginHandsPrefabPath =
            "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Hands Variant.prefab";
        private const string XROriginFallbackPrefabPath =
            "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";

        [MenuItem("MassSpringLab/Build Phase 1 Lab Scene")]
        public static void BuildPhase1Scene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Materials
            Material floorMat = GetOrCreateMaterial("Mat_LabFloor", new Color(0.10f, 0.12f, 0.15f), 0.1f, 0.25f);
            Material wallMat = GetOrCreateMaterial("Mat_LabWall", new Color(0.13f, 0.15f, 0.19f), 0.05f, 0.2f);
            Material tableTopMat = GetOrCreateMaterial("Mat_TableTop", new Color(0.15f, 0.17f, 0.21f), 0.15f, 0.4f);
            Material tableFrameMat = GetOrCreateMaterial("Mat_TableFrame", new Color(0.08f, 0.09f, 0.11f), 0.5f, 0.5f);
            Material cyanAccentMat = GetOrCreateMaterial("Mat_AccentCyan", new Color(0.0f, 0.83f, 0.85f), 0.2f, 0.6f, new Color(0.0f, 0.45f, 0.48f));
            Material limeAccentMat = GetOrCreateMaterial("Mat_AccentLime", new Color(0.46f, 1.0f, 0.01f), 0.2f, 0.6f, new Color(0.20f, 0.45f, 0.0f));

            // Lighting
            SetupLighting();

            // Lab Environment Root
            GameObject envRoot = new GameObject("LabEnvironment");
            BuildRoom(envRoot.transform, floorMat, wallMat);
            BuildLabTable(envRoot.transform, tableTopMat, tableFrameMat, cyanAccentMat);
            BuildTheoryBoard(envRoot.transform, tableFrameMat, cyanAccentMat, limeAccentMat);

            // Non-overlapping Layout Zones for Phases 2-11
            BuildLabZones();

            // XR Origin + Interaction Manager + EventSystem + DesktopTestController
            SetupXRAndDesktop();

            // Save Scene & Register in Build Settings
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LabSceneBuilder] Phase 1 scene created at {ScenePath}");
        }

        private static void SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.30f);

            GameObject dirLightGo = new GameObject("Directional Light");
            Light dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.95f, 0.97f, 1.0f);
            dirLight.intensity = 1.15f;
            dirLight.shadows = LightShadows.Soft;
            dirLightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject benchLightGo = new GameObject("LabBenchLight");
            Light benchLight = benchLightGo.AddComponent<Light>();
            benchLight.type = LightType.Point;
            benchLight.color = new Color(0.85f, 0.96f, 1.0f);
            benchLight.intensity = 2.4f;
            benchLight.range = 6.5f;
            benchLightGo.transform.position = new Vector3(0f, 2.5f, 1.0f);
        }

        private static void BuildRoom(Transform parent, Material floorMat, Material wallMat)
        {
            GameObject room = new GameObject("Room");
            room.transform.SetParent(parent, false);

            CreatePrimitiveBox("Floor", room.transform, new Vector3(0f, -0.05f, 0.5f), new Vector3(8f, 0.1f, 8f), floorMat);
            CreatePrimitiveBox("Ceiling", room.transform, new Vector3(0f, 3.55f, 0.5f), new Vector3(8f, 0.1f, 8f), wallMat);
            CreatePrimitiveBox("Wall_Front", room.transform, new Vector3(0f, 1.75f, 3.0f), new Vector3(8f, 3.5f, 0.1f), wallMat);
            CreatePrimitiveBox("Wall_Back", room.transform, new Vector3(0f, 1.75f, -3.5f), new Vector3(8f, 3.5f, 0.1f), wallMat);
            CreatePrimitiveBox("Wall_Left", room.transform, new Vector3(-4.0f, 1.75f, -0.25f), new Vector3(0.1f, 3.5f, 8f), wallMat);
            CreatePrimitiveBox("Wall_Right", room.transform, new Vector3(4.0f, 1.75f, -0.25f), new Vector3(0.1f, 3.5f, 8f), wallMat);
        }

        private static void BuildLabTable(Transform parent, Material topMat, Material frameMat, Material accentMat)
        {
            // Tabletop top surface sits cleanly at y = 0.85m, centered at z = 1.0m
            GameObject table = new GameObject("LabTable");
            table.transform.SetParent(parent, false);
            table.transform.position = new Vector3(0f, 0f, 1.0f);

            const float width = 2.20f;
            const float depth = 0.90f;
            const float topThickness = 0.05f;
            const float topY = 0.85f - topThickness * 0.5f;

            CreatePrimitiveBox("TableTop", table.transform, new Vector3(0f, topY, 0f), new Vector3(width, topThickness, depth), topMat);
            CreatePrimitiveBox("TableFrontTrim", table.transform, new Vector3(0f, topY, -depth * 0.5f - 0.005f), new Vector3(width, 0.015f, 0.01f), accentMat, false);

            float legHeight = 0.80f;
            float legY = legHeight * 0.5f;
            float legX = width * 0.5f - 0.08f;
            float legZ = depth * 0.5f - 0.08f;
            Vector3 legScale = new Vector3(0.06f, legHeight, 0.06f);

            CreatePrimitiveBox("Leg_FL", table.transform, new Vector3(-legX, legY, -legZ), legScale, frameMat);
            CreatePrimitiveBox("Leg_FR", table.transform, new Vector3(legX, legY, -legZ), legScale, frameMat);
            CreatePrimitiveBox("Leg_BL", table.transform, new Vector3(-legX, legY, legZ), legScale, frameMat);
            CreatePrimitiveBox("Leg_BR", table.transform, new Vector3(legX, legY, legZ), legScale, frameMat);
        }

        private static void BuildTheoryBoard(Transform parent, Material frameMat, Material cyanMat, Material limeMat)
        {
            GameObject boardRoot = new GameObject("WallTheoryBoard");
            boardRoot.transform.SetParent(parent, false);
            boardRoot.transform.position = new Vector3(0f, 2.05f, 2.90f);

            CreatePrimitiveBox("BoardBacking", boardRoot.transform, Vector3.zero, new Vector3(2.40f, 1.15f, 0.04f), frameMat, false);
            CreatePrimitiveBox("BoardTopAccent", boardRoot.transform, new Vector3(0f, 0.58f, -0.01f), new Vector3(2.40f, 0.02f, 0.03f), cyanMat, false);

            GameObject canvasGo = new GameObject("TheoryCanvas");
            canvasGo.transform.SetParent(boardRoot.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.025f);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2300f, 1080f);

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

            CreateTMPText(
                "TitleText",
                canvasGo.transform,
                new Vector2(0f, 400f),
                new Vector2(2150f, 130f),
                "HOOKE'S LAW & MASS-SPRING OSCILLATIONS",
                72f,
                FontStyles.Bold,
                new Color(0.0f, 0.90f, 0.92f),
                TextAlignmentOptions.Center);

            CreateTMPText(
                "BodyText",
                canvasGo.transform,
                new Vector2(0f, -60f),
                new Vector2(2100f, 740f),
                "OBJECTIVE: Experimentally determine the spring constant (k) using 50 g - 250 g masses.\n\n" +
                "FORMULAS:\n" +
                "  Extension:        x = L - L0   (convert cm to m)\n" +
                "  Restoring Force:  F = m * g    (g = 9.81 m/s^2, m in kg)\n" +
                "  Spring Constant:  k = F / x    (N/m)\n" +
                "  Time Period:      T = t / 10   (for 10 oscillations)\n\n" +
                "WORKFLOW: Measure L0 -> Attach Mass -> Measure L -> Pull & Release -> Time 10 Oscillations -> Proceed",
                42f,
                FontStyles.Normal,
                Color.white,
                TextAlignmentOptions.TopLeft);
        }

        private static void BuildLabZones()
        {
            GameObject zonesRoot = new GameObject("LabZones");
            CreateZoneAnchor("ApparatusZone", zonesRoot.transform, new Vector3(-0.58f, 0.85f, 0.98f));
            CreateZoneAnchor("MassesZone", zonesRoot.transform, new Vector3(0.00f, 0.85f, 0.78f));
            CreateZoneAnchor("ObservationTableZone", zonesRoot.transform, new Vector3(0.72f, 0.85f, 1.02f));
            CreateZoneAnchor("CalculationDisplayZone", zonesRoot.transform, new Vector3(0.00f, 1.55f, 1.38f));
            CreateZoneAnchor("RobotAssistantZone", zonesRoot.transform, new Vector3(-1.35f, 0.00f, 1.15f));
        }

        private static void SetupXRAndDesktop()
        {
            GameObject xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XROriginHandsPrefabPath);
            if (xrPrefab == null)
            {
                xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XROriginFallbackPrefabPath);
            }

            GameObject xrInstance = null;
            if (xrPrefab != null)
            {
                xrInstance = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab);
                xrInstance.transform.position = new Vector3(0f, 0f, 0.05f);
                xrInstance.transform.rotation = Quaternion.identity;

                if (xrInstance.GetComponent<DesktopTestController>() == null)
                {
                    xrInstance.AddComponent<DesktopTestController>();
                }
            }
            else
            {
                Debug.LogWarning("[LabSceneBuilder] Could not find XR Origin prefab in VRTemplateAssets. Creating fallback Main Camera.");
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                camGo.transform.position = new Vector3(0f, 1.45f, 0.05f);
                camGo.AddComponent<DesktopTestController>();
            }

            if (Object.FindFirstObjectByType<XRInteractionManager>() == null)
            {
                GameObject managerGo = new GameObject("XR Interaction Manager");
                managerGo.AddComponent<XRInteractionManager>();
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<XRUIInputModule>();
            }
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (EditorBuildSettingsScene s in scenes)
            {
                if (s.path == scenePath)
                {
                    s.enabled = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Material GetOrCreateMaterial(
            string name,
            Color baseColor,
            float metallic,
            float smoothness,
            Color? emission = null)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject CreatePrimitiveBox(
            string name,
            Transform parent,
            Vector3 localPos,
            Vector3 scale,
            Material mat,
            bool keepCollider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && mat != null)
            {
                renderer.sharedMaterial = mat;
            }

            if (!keepCollider)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }

            return go;
        }

        private static void CreateZoneAnchor(string name, Transform parent, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
        }

        private static void CreateTMPText(
            string name,
            Transform parent,
            Vector2 anchoredPos,
            Vector2 size,
            string text,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }
    }
}
