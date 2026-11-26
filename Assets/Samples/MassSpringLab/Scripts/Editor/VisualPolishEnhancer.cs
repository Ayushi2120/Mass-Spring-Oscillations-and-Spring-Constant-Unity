using System.Collections.Generic;
using MassSpringLab.Robot;
using MassSpringLab.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Applies a colourful, futuristic educational laboratory palette using:
    /// Deep Slate Blue (#263445), Blue Grey (#354B63), Teal/Cyan (#14B8C4),
    /// Muted Violet (#7564C8), Warm Amber (#F0B45A), and Soft Light Grey (#E6EAF0).
    /// Strictly visual-only: preserves all existing models, positions, scripts, and interactions.
    /// </summary>
    public static class VisualPolishEnhancer
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";
        private const string MaterialsDir = "Assets/Samples/MassSpringLab/Materials";

        [MenuItem("MassSpringLab/Apply Futuristic Lab Visual Polish")]
        public static void ApplyVisualPolish()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Color deepSlateBlue = Hex("#263445");
            Color blueGrey      = Hex("#354B63");
            Color tealCyan      = Hex("#14B8C4");
            Color mutedViolet   = Hex("#7564C8");
            Color warmAmber     = Hex("#F0B45A");
            Color softLightGrey = Hex("#E6EAF0");
            Color darkFloor     = Hex("#1C2633");
            Color graphite      = Hex("#1B232E");
            Color uiDarkSlate   = Hex("#16202C");

            // 1. Update existing materials + create wall colour variants
            UpdateMaterial("Mat_LabFloor", darkFloor, 0.22f, 0.58f);
            UpdateMaterial("Mat_LabWall", softLightGrey, 0.05f, 0.25f);
            UpdateMaterial("Mat_TableTop", deepSlateBlue, 0.25f, 0.52f);
            UpdateMaterial("Mat_TableFrame", graphite, 0.62f, 0.55f);
            UpdateMaterial("Mat_AccentCyan", tealCyan, 0.20f, 0.60f, tealCyan * 0.42f);
            UpdateMaterial("Mat_AccentLime", Hex("#4CD97B"), 0.20f, 0.58f, Hex("#164A28"));
            UpdateMaterial("Mat_GlassPanel", uiDarkSlate, 0.12f, 0.80f);

            Material wallSlateMat = LoadOrCreateLit("Mat_WallSlateBlue", deepSlateBlue, 0.08f, 0.32f);
            Material wallBlueGreyMat = LoadOrCreateLit("Mat_WallBlueGrey", blueGrey, 0.08f, 0.32f);
            Material wallLightGreyMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/Mat_LabWall.mat");
            Material tealMat = LoadOrCreateEmissive("Mat_AccentCyan", tealCyan, tealCyan * 0.42f);
            Material violetMat = LoadOrCreateEmissive("Mat_AccentViolet", mutedViolet, mutedViolet * 0.38f);
            Material amberMat = LoadOrCreateEmissive("Mat_AccentAmber", warmAmber, warmAmber * 0.40f);
            Material ceilingFixtureMat = LoadOrCreateEmissive("Mat_CeilingFixture", Hex("#FFFDF8"), Hex("#FFFBF0") * 1.05f);

            // 2. Apply multi-tone colours directly to existing Room walls without moving/rebuilding
            ApplyRoomWallMaterial("Wall_Front", wallSlateMat);
            ApplyRoomWallMaterial("Wall_Back", wallBlueGreyMat);
            ApplyRoomWallMaterial("Wall_Left", wallBlueGreyMat);
            ApplyRoomWallMaterial("Wall_Right", wallBlueGreyMat);
            ApplyRoomWallMaterial("Ceiling", wallLightGreyMat);

            // 3. Balanced brighter multi-tone lighting
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.46f, 0.50f, 0.56f);

            Light dirLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (dirLight != null)
            {
                dirLight.color = new Color(1.0f, 0.97f, 0.93f);
                dirLight.intensity = 1.35f;
                dirLight.shadows = LightShadows.Soft;
                dirLight.shadowStrength = 0.42f;
            }

            Light benchLight = GameObject.Find("LabBenchLight")?.GetComponent<Light>();
            if (benchLight != null)
            {
                benchLight.color = Hex("#FFF4E2");
                benchLight.intensity = 3.5f;
                benchLight.range = 8.5f;
            }

            // 4. Add tasteful coloured wall panels, ceiling fixtures, accent strips, and fill lights
            GameObject oldGroup = GameObject.Find("VisualEnhancements_Lighting");
            if (oldGroup != null) Object.DestroyImmediate(oldGroup);

            GameObject group = new GameObject("VisualEnhancements_Lighting");

            // Ceiling light panels framed with Teal
            CreateBox("CeilingLightPanel_Left", group.transform, new Vector3(-1.2f, 3.48f, 1.0f), new Vector3(1.5f, 0.03f, 2.3f), ceilingFixtureMat);
            CreateBox("CeilingFrame_Left", group.transform, new Vector3(-1.2f, 3.49f, 1.0f), new Vector3(1.62f, 0.02f, 2.42f), tealMat);
            CreateBox("CeilingLightPanel_Right", group.transform, new Vector3(1.2f, 3.48f, 1.0f), new Vector3(1.5f, 0.03f, 2.3f), ceilingFixtureMat);
            CreateBox("CeilingFrame_Right", group.transform, new Vector3(1.2f, 3.49f, 1.0f), new Vector3(1.62f, 0.02f, 2.42f), tealMat);

            // Front Wall architectural colour blocks & strips (behind/around WallTheoryBoard)
            CreateBox("FrontWall_LowerAmberBand", group.transform, new Vector3(0f, 0.45f, 2.93f), new Vector3(7.6f, 0.05f, 0.03f), amberMat);
            CreateBox("FrontWall_UpperVioletBand", group.transform, new Vector3(0f, 3.05f, 2.93f), new Vector3(7.6f, 0.28f, 0.03f), violetMat);
            CreateBox("FrontWall_LeftLightPanel", group.transform, new Vector3(-2.65f, 1.80f, 2.93f), new Vector3(1.75f, 2.0f, 0.025f), wallLightGreyMat);
            CreateBox("FrontWall_RightLightPanel", group.transform, new Vector3(2.65f, 1.80f, 2.93f), new Vector3(1.75f, 2.0f, 0.025f), wallLightGreyMat);
            CreateBox("FrontWall_TealPillarLeft", group.transform, new Vector3(-1.68f, 1.80f, 2.92f), new Vector3(0.08f, 2.2f, 0.04f), tealMat);
            CreateBox("FrontWall_TealPillarRight", group.transform, new Vector3(1.68f, 1.80f, 2.92f), new Vector3(0.08f, 2.2f, 0.04f), tealMat);

            // Side Wall architectural panels (Soft Light Grey + Deep Slate Blue + Teal/Violet/Amber strips)
            CreateBox("LeftWall_LightPanel", group.transform, new Vector3(-3.93f, 1.80f, 0.5f), new Vector3(0.03f, 1.85f, 4.6f), wallLightGreyMat);
            CreateBox("LeftWall_TealStrip", group.transform, new Vector3(-3.91f, 2.75f, 0.5f), new Vector3(0.03f, 0.06f, 5.4f), tealMat);
            CreateBox("LeftWall_VioletStrip", group.transform, new Vector3(-3.91f, 0.82f, 0.5f), new Vector3(0.03f, 0.08f, 5.4f), violetMat);

            CreateBox("RightWall_LightPanel", group.transform, new Vector3(3.93f, 1.80f, 0.5f), new Vector3(0.03f, 1.85f, 4.6f), wallLightGreyMat);
            CreateBox("RightWall_TealStrip", group.transform, new Vector3(3.91f, 2.75f, 0.5f), new Vector3(0.03f, 0.06f, 5.4f), tealMat);
            CreateBox("RightWall_VioletStrip", group.transform, new Vector3(3.91f, 0.82f, 0.5f), new Vector3(0.03f, 0.08f, 5.4f), violetMat);

            // Multi-tone lights (bright neutral + subtle teal, violet, and warm amber accents)
            List<Light> accentLights = new List<Light>();
            CreateFillLight("RobotFillLight", group.transform, new Vector3(-1.25f, 2.25f, 0.35f), Hex("#FFF6EA"), 2.2f, 4.8f);
            CreateFillLight("WhiteboardFillLight", group.transform, new Vector3(0.0f, 2.65f, 1.85f), Hex("#F6FBFF"), 2.4f, 5.5f);
            accentLights.Add(CreateFillLight("TealAccentLight_Left", group.transform, new Vector3(-2.6f, 2.4f, 0.8f), tealCyan, 1.5f, 5.0f));
            accentLights.Add(CreateFillLight("TealAccentLight_Right", group.transform, new Vector3(2.6f, 2.4f, 0.8f), tealCyan, 1.5f, 5.0f));
            accentLights.Add(CreateFillLight("VioletAccentLight_Front", group.transform, new Vector3(0.0f, 2.95f, 2.1f), mutedViolet, 1.6f, 5.5f));
            accentLights.Add(CreateFillLight("AmberAccentLight_Bench", group.transform, new Vector3(-0.3f, 1.85f, 0.65f), warmAmber, 1.1f, 3.8f));

            // Attach visual-only LabVisualAccentManager directly to the scene group
            LabVisualAccentManager accentMgr = group.AddComponent<LabVisualAccentManager>();
            SerializedObject mgrSo = new SerializedObject(accentMgr);
            SerializedProperty lightsProp = mgrSo.FindProperty("accentLights");
            lightsProp.arraySize = accentLights.Count;
            for (int i = 0; i < accentLights.Count; i++)
            {
                lightsProp.GetArrayElementAtIndex(i).objectReferenceValue = accentLights[i];
            }
            mgrSo.ApplyModifiedPropertiesWithoutUndo();

            // 5. Ensure ATOM-8 hand-originating pointer ray is wired
            SetupAtom8HandPointerRay();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[VisualPolishEnhancer] Colourful futuristic lab palette & LabVisualAccentManager applied.");
        }

        private static void ApplyRoomWallMaterial(string wallName, Material mat)
        {
            if (mat == null) return;
            GameObject go = GameObject.Find($"LabEnvironment/Room/{wallName}") ?? GameObject.Find(wallName);
            if (go == null) return;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                EditorUtility.SetDirty(go);
            }
        }

        private static void SetupAtom8HandPointerRay()
        {
            GameObject atom8 = GameObject.Find("ATOM-8");
            if (atom8 == null) return;

            Transform rightArm = atom8.transform.Find("Torso/RightArm");
            Transform elbow = rightArm != null ? rightArm.Find("Elbow") : null;
            Transform pointerTip = elbow != null ? elbow.Find("PointerTip") : null;
            if (pointerTip == null) return;

            LineRenderer ray = pointerTip.GetComponent<LineRenderer>();
            if (ray == null) ray = pointerTip.gameObject.AddComponent<LineRenderer>();
            ray.useWorldSpace = true;
            ray.positionCount = 2;
            ray.startWidth = 0.007f;
            ray.endWidth = 0.002f;
            ray.numCapVertices = 4;
            ray.enabled = false;

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material rayMat = LoadOrCreateEmissive("Mat_RobotPointerRay", Hex("#14B8C4"), Hex("#14B8C4") * 0.8f);
            if (unlit != null && rayMat.shader != unlit)
            {
                rayMat.shader = unlit;
                rayMat.SetColor("_BaseColor", new Color(0.08f, 0.72f, 0.77f, 0.90f));
            }
            ray.sharedMaterial = rayMat;

            Atom8GuidanceController guidance = atom8.GetComponent<Atom8GuidanceController>();
            if (guidance != null)
            {
                SerializedObject so = new SerializedObject(guidance);
                so.FindProperty("rightShoulder").objectReferenceValue = rightArm;
                so.FindProperty("rightElbow").objectReferenceValue = elbow;
                so.FindProperty("pointerTip").objectReferenceValue = pointerTip;
                so.FindProperty("handPointerRay").objectReferenceValue = ray;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(atom8);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        private static void UpdateMaterial(
            string matName,
            Color baseColor,
            float metallic,
            float smoothness,
            Color? emission = null)
        {
            string path = $"{MaterialsDir}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) return;

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            EditorUtility.SetDirty(mat);
        }

        private static Material LoadOrCreateLit(string name, Color baseColor, float metallic, float smoothness)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreateEmissive(string name, Color baseColor, Color emissionColor)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;
        }

        private static Light CreateFillLight(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }
    }
}
