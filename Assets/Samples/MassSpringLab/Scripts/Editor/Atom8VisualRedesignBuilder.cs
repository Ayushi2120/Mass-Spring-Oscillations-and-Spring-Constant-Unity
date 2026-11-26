using MassSpringLab.Robot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Visually redesigns the existing ATOM-8 robot in MassSpringLab.unity into a cute,
    /// friendly, rounded white/silver/sky-blue futuristic educational robot matching the
    /// reference image—while preserving all existing scripts, hierarchy, pointing,
    /// narration, and XR functionality.
    /// </summary>
    public static class Atom8VisualRedesignBuilder
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";
        private const string MaterialsDir = "Assets/Samples/MassSpringLab/Materials";

        [MenuItem("MassSpringLab/Redesign ATOM-8 Appearance (Visual Only)")]
        public static void RedesignAtom8Visuals()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject atom8 = GameObject.Find("ATOM-8");
            if (atom8 == null)
            {
                Debug.LogError("[Atom8VisualRedesignBuilder] ATOM-8 not found!");
                return;
            }

            // Friendly toy-like futuristic materials (white, light grey, sky blue, cyan, navy)
            Material smoothWhite   = LoadOrCreateLit("Mat_Atom8SmoothWhite", Hex("#F4F7FB"), 0.08f, 0.72f);
            Material lightSilver   = LoadOrCreateLit("Mat_Atom8Silver", Hex("#DCE4EC"), 0.35f, 0.65f);
            Material skyBlueFace   = LoadOrCreateEmissive("Mat_Atom8SkyFace", Hex("#74BBE3"), Hex("#1E5578") * 0.42f);
            Material softBlue      = LoadOrCreateLit("Mat_Atom8SoftBlue", Hex("#4388C4"), 0.25f, 0.62f);
            Material deepNavy      = LoadOrCreateLit("Mat_Atom8DeepNavy", Hex("#12263F"), 0.20f, 0.85f);
            Material cyanGlow      = LoadOrCreateEmissive("Mat_Atom8EyeCyan", Hex("#4AE0FF"), Hex("#1CC8FF") * 1.15f);
            Material brightWhite   = LoadOrCreateEmissive("Mat_Atom8SmileWhite", Hex("#FFFFFF"), Hex("#FFFFFF") * 0.85f);
            Material handBlue      = LoadOrCreateLit("Mat_Atom8HandBlue", Hex("#2C72B8"), 0.40f, 0.62f);
            Material handTeal      = LoadOrCreateEmissive("Mat_Atom8HandTeal", Hex("#14B8C4"), Hex("#14B8C4") * 0.38f);

            // 1. Large Rounded Head with Light Sky-Blue Face, Large Cute Eyes, Smile & Top Antenna
            Transform head = atom8.transform.Find("Torso/Head");
            if (head != null)
            {
                DisableMeshRenderer(head, "HeadShell");
                DisableMeshRenderer(head, "Visor");
                DisableMeshRenderer(head, "EyeLeft");
                DisableMeshRenderer(head, "EyeRight");
                DestroyChildIfExists(head, "Visual_FriendlyHead");

                GameObject vHead = new GameObject("Visual_FriendlyHead");
                vHead.transform.SetParent(head, false);

                // Smooth large rounded white head + cute ear-pods
                CreatePart(PrimitiveType.Sphere, "HeadOuterShell", vHead.transform, Vector3.zero, new Vector3(0.46f, 0.34f, 0.34f), smoothWhite);
                CreatePart(PrimitiveType.Sphere, "EarPod_L", vHead.transform, new Vector3(-0.222f, -0.005f, 0.005f), new Vector3(0.085f, 0.140f, 0.140f), smoothWhite);
                CreatePart(PrimitiveType.Cylinder, "EarTrim_L", vHead.transform, new Vector3(-0.242f, -0.005f, 0.005f), new Vector3(0.100f, 0.014f, 0.100f), softBlue, Quaternion.Euler(0f, 0f, 90f));
                CreatePart(PrimitiveType.Sphere, "EarPod_R", vHead.transform, new Vector3(0.222f, -0.005f, 0.005f), new Vector3(0.085f, 0.140f, 0.140f), smoothWhite);
                CreatePart(PrimitiveType.Cylinder, "EarTrim_R", vHead.transform, new Vector3(0.242f, -0.005f, 0.005f), new Vector3(0.100f, 0.014f, 0.100f), softBlue, Quaternion.Euler(0f, 0f, 90f));

                // Small cute antenna indicator on top
                CreatePart(PrimitiveType.Cylinder, "AntennaStem", vHead.transform, new Vector3(0f, 0.185f, 0.005f), new Vector3(0.014f, 0.028f, 0.014f), lightSilver);
                CreatePart(PrimitiveType.Sphere, "AntennaOrb", vHead.transform, new Vector3(0f, 0.220f, 0.005f), new Vector3(0.045f, 0.045f, 0.045f), cyanGlow);

                // Clean light sky-blue rounded face panel with subtle navy rim
                CreatePart(PrimitiveType.Sphere, "FaceRim", vHead.transform, new Vector3(0f, -0.005f, -0.108f), new Vector3(0.365f, 0.250f, 0.115f), deepNavy);
                CreatePart(PrimitiveType.Sphere, "FacePanel", vHead.transform, new Vector3(0f, -0.005f, -0.118f), new Vector3(0.340f, 0.228f, 0.110f), skyBlueFace);

                // Two LARGE, simple, friendly cyan-blue eyes with white catchlights
                GameObject eyeL = CreateCuteEye("CuteEye_L", vHead.transform, new Vector3(-0.074f, 0.015f, -0.170f), brightWhite, cyanGlow, deepNavy);
                GameObject eyeR = CreateCuteEye("CuteEye_R", vHead.transform, new Vector3(0.074f, 0.015f, -0.170f), brightWhite, cyanGlow, deepNavy);

                // Small cute smiling mouth
                CreatePart(PrimitiveType.Sphere, "Smile_Center", vHead.transform, new Vector3(0f, -0.044f, -0.177f), new Vector3(0.036f, 0.012f, 0.012f), deepNavy);
                CreatePart(PrimitiveType.Sphere, "Smile_L", vHead.transform, new Vector3(-0.019f, -0.036f, -0.176f), new Vector3(0.012f, 0.022f, 0.012f), deepNavy, Quaternion.Euler(0f, 0f, 32f));
                CreatePart(PrimitiveType.Sphere, "Smile_R", vHead.transform, new Vector3(0.019f, -0.036f, -0.176f), new Vector3(0.012f, 0.022f, 0.012f), deepNavy, Quaternion.Euler(0f, 0f, -32f));

                Atom8FaceBlink blink = head.GetComponent<Atom8FaceBlink>();
                if (blink == null) blink = head.gameObject.AddComponent<Atom8FaceBlink>();
                SerializedObject blinkSo = new SerializedObject(blink);
                blinkSo.FindProperty("eyeLeft").objectReferenceValue = eyeL.transform;
                blinkSo.FindProperty("eyeRight").objectReferenceValue = eyeR.transform;
                blinkSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // 2. Smooth Rounded White/Light-Grey Compact Body + Blue/Cyan Chest Emblem
            Transform torso = atom8.transform.Find("Torso");
            if (torso != null)
            {
                DisableMeshRenderer(torso, "TorsoShell");
                DisableMeshRenderer(torso, "ChestPanel");
                DisableMeshRenderer(torso, "PowerCore");
                DestroyChildIfExists(torso, "Visual_LabCoat");
                DestroyChildIfExists(torso, "Visual_RoundedBody");

                GameObject vBody = new GameObject("Visual_RoundedBody");
                vBody.transform.SetParent(torso, false);

                CreatePart(PrimitiveType.Capsule, "BodyCapsule", vBody.transform, new Vector3(0f, -0.02f, 0f), new Vector3(0.34f, 0.22f, 0.27f), smoothWhite);
                CreatePart(PrimitiveType.Cylinder, "CollarRing", vBody.transform, new Vector3(0f, 0.165f, 0f), new Vector3(0.22f, 0.020f, 0.20f), lightSilver);
                CreatePart(PrimitiveType.Sphere, "LowerPelvis", vBody.transform, new Vector3(0f, -0.195f, 0f), new Vector3(0.27f, 0.13f, 0.23f), softBlue);

                // Circular blue/cyan chest badge
                CreatePart(PrimitiveType.Cylinder, "ChestRingWhite", vBody.transform, new Vector3(0f, 0.025f, -0.128f), new Vector3(0.145f, 0.014f, 0.145f), lightSilver, Quaternion.Euler(90f, 0f, 0f));
                CreatePart(PrimitiveType.Cylinder, "ChestRingCyan", vBody.transform, new Vector3(0f, 0.025f, -0.136f), new Vector3(0.122f, 0.012f, 0.122f), cyanGlow, Quaternion.Euler(90f, 0f, 0f));
                CreatePart(PrimitiveType.Cylinder, "ChestCoreBlue", vBody.transform, new Vector3(0f, 0.025f, -0.142f), new Vector3(0.096f, 0.010f, 0.096f), deepNavy, Quaternion.Euler(90f, 0f, 0f));
                CreatePart(PrimitiveType.Sphere, "ChestDot", vBody.transform, new Vector3(0f, 0.025f, -0.148f), new Vector3(0.036f, 0.036f, 0.010f), brightWhite);

                Transform badge = torso.Find("RobotBadge");
                if (badge != null) badge.localPosition = new Vector3(0f, -0.105f, -0.140f);
            }

            // 3. Cute Rounded Legs/Boots visually replacing the industrial LowerColumn pole
            DisableMeshRenderer(atom8.transform, "LowerColumn");
            DestroyChildIfExists(atom8.transform, "Visual_FriendlyLegs");
            GameObject legs = new GameObject("Visual_FriendlyLegs");
            legs.transform.SetParent(atom8.transform, false);
            CreateFriendlyLeg("Leg_L", legs.transform, -0.105f, smoothWhite, softBlue, lightSilver);
            CreateFriendlyLeg("Leg_R", legs.transform, 0.105f, smoothWhite, softBlue, lightSilver);

            // 4. Rounded Mechanical Arms & Blue/Teal Hands (preserving RightArm pointing & HandPointerRay)
            RedesignArm(atom8.transform.Find("Torso/LeftArm"), false, smoothWhite, lightSilver, softBlue, handBlue, handTeal);
            RedesignArm(atom8.transform.Find("Torso/RightArm"), true, smoothWhite, lightSilver, softBlue, handBlue, handTeal);

            EditorUtility.SetDirty(atom8);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Atom8VisualRedesignBuilder] ATOM-8 redesigned into cute, friendly rounded educational robot.");
        }

        private static GameObject CreateCuteEye(
            string name,
            Transform parent,
            Vector3 localPos,
            Material whiteMat,
            Material cyanMat,
            Material navyMat)
        {
            GameObject eye = new GameObject(name);
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;

            CreatePart(PrimitiveType.Sphere, "OuterWhite", eye.transform, Vector3.zero, new Vector3(0.102f, 0.106f, 0.020f), whiteMat);
            CreatePart(PrimitiveType.Sphere, "CyanIris", eye.transform, new Vector3(0f, 0f, -0.005f), new Vector3(0.088f, 0.092f, 0.020f), cyanMat);
            CreatePart(PrimitiveType.Sphere, "DarkPupil", eye.transform, new Vector3(0f, 0f, -0.010f), new Vector3(0.068f, 0.072f, 0.018f), navyMat);
            CreatePart(PrimitiveType.Sphere, "CatchBig", eye.transform, new Vector3(0.017f, 0.018f, -0.018f), new Vector3(0.020f, 0.020f, 0.008f), whiteMat);
            CreatePart(PrimitiveType.Sphere, "CatchSmall", eye.transform, new Vector3(-0.012f, -0.014f, -0.018f), new Vector3(0.010f, 0.010f, 0.006f), whiteMat);

            return eye;
        }

        private static void CreateFriendlyLeg(string name, Transform parent, float xOffset, Material whiteMat, Material blueMat, Material silverMat)
        {
            GameObject leg = new GameObject(name);
            leg.transform.SetParent(parent, false);
            leg.transform.localPosition = new Vector3(xOffset, 0f, 0f);

            CreatePart(PrimitiveType.Capsule, "Thigh", leg.transform, new Vector3(0f, 0.64f, 0f), new Vector3(0.11f, 0.13f, 0.11f), silverMat);
            CreatePart(PrimitiveType.Capsule, "Shin", leg.transform, new Vector3(0f, 0.36f, 0f), new Vector3(0.14f, 0.16f, 0.15f), whiteMat);
            CreatePart(PrimitiveType.Sphere, "Boot", leg.transform, new Vector3(0f, 0.12f, -0.025f), new Vector3(0.16f, 0.12f, 0.22f), whiteMat);
            CreatePart(PrimitiveType.Cylinder, "Sole", leg.transform, new Vector3(0f, 0.065f, -0.025f), new Vector3(0.17f, 0.015f, 0.23f), blueMat);
        }

        private static void RedesignArm(
            Transform arm,
            bool isPointerArm,
            Material smoothWhite,
            Material silver,
            Material softBlue,
            Material handBlue,
            Material handTeal)
        {
            if (arm == null) return;

            DisableMeshRenderer(arm, "ShoulderJoint");
            DisableMeshRenderer(arm, "UpperArm");
            DestroyChildIfExists(arm, "Visual_SleeveUpper");
            DestroyChildIfExists(arm, "Visual_RoundedUpperArm");

            GameObject upper = new GameObject("Visual_RoundedUpperArm");
            upper.transform.SetParent(arm, false);
            CreatePart(PrimitiveType.Sphere, "ShoulderSphere", upper.transform, Vector3.zero, new Vector3(0.10f, 0.10f, 0.10f), softBlue);
            CreatePart(PrimitiveType.Capsule, "UpperShell", upper.transform, new Vector3(0f, -0.105f, 0f), new Vector3(0.085f, 0.105f, 0.085f), smoothWhite);

            Transform elbow = arm.Find("Elbow");
            if (elbow != null)
            {
                DisableMeshRenderer(elbow, "Forearm");
                DestroyChildIfExists(elbow, "Visual_SleeveLower");
                DestroyChildIfExists(elbow, "Visual_RoundedForearm");

                GameObject lower = new GameObject("Visual_RoundedForearm");
                lower.transform.SetParent(elbow, false);
                CreatePart(PrimitiveType.Sphere, "ElbowSphere", lower.transform, Vector3.zero, new Vector3(0.075f, 0.075f, 0.075f), softBlue);
                CreatePart(PrimitiveType.Capsule, "ForearmShell", lower.transform, new Vector3(0f, -0.09f, 0f), new Vector3(0.082f, 0.095f, 0.082f), smoothWhite);

                Transform pointerTip = elbow.Find("PointerTip");
                if (pointerTip != null)
                {
                    DisableMeshRenderer(pointerTip, "HandTip");
                    DestroyChildIfExists(pointerTip, "Visual_MechanicalHand");

                    GameObject hand = new GameObject("Visual_MechanicalHand");
                    hand.transform.SetParent(pointerTip, false);

                    CreatePart(PrimitiveType.Cylinder, "WristRing", hand.transform, new Vector3(0f, 0.025f, 0f), new Vector3(0.056f, 0.014f, 0.056f), silver);
                    CreatePart(PrimitiveType.Sphere, "Palm", hand.transform, Vector3.zero, new Vector3(0.058f, 0.050f, 0.044f), handBlue);

                    CreatePart(PrimitiveType.Capsule, "Thumb", hand.transform, new Vector3(-0.028f, -0.012f, -0.014f), new Vector3(0.017f, 0.018f, 0.017f), handTeal, Quaternion.Euler(0f, 0f, 35f));
                    float indexLen = isPointerArm ? 0.032f : 0.020f;
                    float indexY = isPointerArm ? -0.042f : -0.028f;
                    CreatePart(PrimitiveType.Capsule, "Finger_Index", hand.transform, new Vector3(0.012f, indexY, -0.008f), new Vector3(0.016f, indexLen, 0.016f), handTeal);
                    CreatePart(PrimitiveType.Capsule, "Finger_Middle", hand.transform, new Vector3(0.002f, -0.028f, 0.006f), new Vector3(0.015f, 0.018f, 0.015f), handBlue);
                    CreatePart(PrimitiveType.Capsule, "Finger_Ring", hand.transform, new Vector3(-0.010f, -0.026f, 0.012f), new Vector3(0.015f, 0.016f, 0.015f), handBlue);
                }
            }
        }

        private static void DisableMeshRenderer(Transform parent, string childName)
        {
            Transform t = parent.Find(childName);
            if (t == null) return;
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        private static void DestroyChildIfExists(Transform parent, string childName)
        {
            Transform t = parent.Find(childName);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static GameObject CreatePart(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPos,
            Vector3 localScale,
            Material mat,
            Quaternion? localRot = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            return go;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
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
    }
}
