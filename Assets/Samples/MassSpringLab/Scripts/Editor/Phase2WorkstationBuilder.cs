using System.Collections.Generic;
using MassSpringLab.Apparatus;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Builds the complete Phase 2 3D workstation, spring apparatus, parallel ruler,
    /// five 3D mass objects, ATOM-8 robot assistant, organized WallTheoryBoard,
    /// and dedicated control/display stations inside the existing MassSpringLab scene.
    /// </summary>
    public static class Phase2WorkstationBuilder
    {
        private const string ScenePath = "Assets/MassSpringLab/Scenes/MassSpringLab.unity";
        private const string MaterialsDir = "Assets/MassSpringLab/Materials";
        private const float TableTopY = 0.85f;

        [MenuItem("MassSpringLab/Build Phase 2 Workstation")]
        public static void BuildPhase2Workstation()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Material frameMat = LoadOrCreateMaterial("Mat_TableFrame", new Color(0.08f, 0.09f, 0.11f), 0.5f, 0.5f);
            Material cyanMat = LoadOrCreateMaterial("Mat_AccentCyan", new Color(0.0f, 0.83f, 0.85f), 0.2f, 0.6f, new Color(0.0f, 0.45f, 0.48f));
            Material limeMat = LoadOrCreateMaterial("Mat_AccentLime", new Color(0.46f, 1.0f, 0.01f), 0.2f, 0.6f, new Color(0.20f, 0.45f, 0.0f));
            Material steelMat = LoadOrCreateMaterial("Mat_BrushedSteel", new Color(0.62f, 0.66f, 0.72f), 0.85f, 0.65f);
            Material goldMassMat = LoadOrCreateMaterial("Mat_MassGold", new Color(0.95f, 0.76f, 0.18f), 0.75f, 0.58f);
            Material rulerMat = LoadOrCreateMaterial("Mat_RulerBody", new Color(0.92f, 0.94f, 0.96f), 0.1f, 0.35f);
            Material robotBodyMat = LoadOrCreateMaterial("Mat_RobotBody", new Color(0.22f, 0.26f, 0.32f), 0.45f, 0.55f);
            Material robotWhiteMat = LoadOrCreateMaterial("Mat_RobotWhite", new Color(0.84f, 0.88f, 0.92f), 0.2f, 0.5f);
            Material glassPanelMat = LoadOrCreateMaterial("Mat_GlassPanel", new Color(0.09f, 0.13f, 0.18f), 0.1f, 0.75f);

            // Remove only Phase 1 zone anchors or prior Phase 2 roots (preserve Room, LabTable, WallTheoryBoard, XR Origin)
            DestroyIfExists("LabZones");
            DestroyIfExists("StandingArea");
            DestroyIfExists("Phase2_Workstation");

            GameObject root = new GameObject("Phase2_Workstation");

            BuildStandingArea(frameMat, cyanMat);
            BuildSpringApparatus(root.transform, frameMat, steelMat, cyanMat, limeMat);
            BuildParallelRuler(root.transform, rulerMat, frameMat, cyanMat);
            BuildMassesTrayAndWeights(root.transform, frameMat, goldMassMat, steelMat, cyanMat);
            BuildWorkstationConsoles(root.transform, frameMat, cyanMat, limeMat, glassPanelMat);
            BuildATOM8Robot(root.transform, robotBodyMat, robotWhiteMat, frameMat, cyanMat, limeMat);
            UpdateExistingWallBoard(cyanMat);

            VerifyUserViewpointAndLayout();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase2WorkstationBuilder] Phase 2 complete 3D workstation built and saved.");
        }

        private static void BuildStandingArea(Material baseMat, Material cyanMat)
        {
            GameObject standing = new GameObject("StandingArea");
            standing.transform.position = new Vector3(0f, 0.004f, 0.05f);

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "StandingPad";
            pad.transform.SetParent(standing.transform, false);
            pad.transform.localScale = new Vector3(0.92f, 0.004f, 0.92f);
            SetMat(pad, baseMat);
            RemoveCollider(pad);

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "StandingRing";
            ring.transform.SetParent(standing.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.001f, 0f);
            ring.transform.localScale = new Vector3(0.98f, 0.003f, 0.98f);
            SetMat(ring, cyanMat);
            RemoveCollider(ring);
        }

        private static void BuildSpringApparatus(
            Transform parent,
            Material baseMat,
            Material steelMat,
            Material cyanMat,
            Material limeMat)
        {
            GameObject apparatus = new GameObject("SpringApparatus");
            apparatus.transform.SetParent(parent, false);
            apparatus.transform.position = new Vector3(-0.46f, TableTopY, 0.88f);

            // Heavy stand base
            CreateBox("StandBase", apparatus.transform, new Vector3(-0.04f, 0.0125f, 0.02f), new Vector3(0.28f, 0.025f, 0.24f), baseMat);

            // Vertical support rod (0.64m tall -> plenty of vertical clearance above tabletop)
            CreateCylinder("VerticalRod", apparatus.transform, new Vector3(-0.12f, 0.3325f, 0.06f), new Vector3(0.022f, 0.32f, 0.022f), steelMat);

            // Top clamp & horizontal arm
            CreateBox("TopClamp", apparatus.transform, new Vector3(-0.12f, 0.59f, 0.06f), new Vector3(0.045f, 0.045f, 0.045f), cyanMat);
            CreateCylinder("SupportArm", apparatus.transform, new Vector3(-0.06f, 0.59f, 0.03f), new Vector3(0.016f, 0.08f, 0.016f), steelMat, Quaternion.Euler(0f, -26f, 90f));
            CreateBox("FixedSupportBlock", apparatus.transform, new Vector3(0f, 0.59f, 0f), new Vector3(0.045f, 0.024f, 0.045f), baseMat);

            // Spring Top Anchor (0 cm reference at local y = 0.58m -> world y = 1.43m)
            GameObject topAnchor = new GameObject("SpringTopAnchor");
            topAnchor.transform.SetParent(apparatus.transform, false);
            topAnchor.transform.localPosition = new Vector3(0f, 0.58f, 0f);

            // Spring Bottom Hook (natural length L0 = 0.12m -> local y = 0.46m -> world y = 1.31m)
            GameObject bottomHook = new GameObject("SpringBottomHook");
            bottomHook.transform.SetParent(apparatus.transform, false);
            bottomHook.transform.localPosition = new Vector3(0f, 0.46f, 0f);

            // Hook visual + pointer needle toward ruler
            CreateCylinder("HookStem", bottomHook.transform, new Vector3(0f, -0.01f, 0f), new Vector3(0.008f, 0.012f, 0.008f), steelMat);
            CreateBox("HookCollar", bottomHook.transform, new Vector3(0f, -0.022f, 0f), new Vector3(0.024f, 0.012f, 0.024f), cyanMat);
            CreateBox("RulerPointerNeedle", bottomHook.transform, new Vector3(0.045f, 0f, 0f), new Vector3(0.07f, 0.004f, 0.004f), limeMat, false);

            // Attachment socket at bottom of hook
            GameObject attachPoint = new GameObject("SpringAttachmentPoint");
            attachPoint.transform.SetParent(bottomHook.transform, false);
            attachPoint.transform.localPosition = new Vector3(0f, -0.032f, 0f);
            attachPoint.AddComponent<SpringAttachmentPoint>();

            // 3D Procedural Helical Spring Mesh
            GameObject helixGo = new GameObject("HelicalSpring3D");
            helixGo.transform.SetParent(topAnchor.transform, false);
            MeshRenderer mr = helixGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = steelMat;
            HelicalSpringMesh helix = helixGo.AddComponent<HelicalSpringMesh>();

            SerializedObject so = new SerializedObject(helix);
            so.FindProperty("bottomHook").objectReferenceValue = bottomHook.transform;
            so.FindProperty("defaultLength").floatValue = 0.12f;
            so.ApplyModifiedPropertiesWithoutUndo();
            helix.RebuildIfNeeded(force: true);
        }

        private static void BuildParallelRuler(Transform parent, Material rulerMat, Material frameMat, Material cyanMat)
        {
            GameObject rulerRoot = new GameObject("MeasurementRuler");
            rulerRoot.transform.SetParent(parent, false);
            // Parallel to spring at x = -0.34m, z = 0.88m; 0 cm aligns with SpringTopAnchor (local y = 0.58m -> world y = 1.43m)
            rulerRoot.transform.position = new Vector3(-0.34f, TableTopY, 0.88f);

            CreateBox("RulerFoot", rulerRoot.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.08f, 0.02f, 0.08f), frameMat);
            CreateCylinder("RulerPost", rulerRoot.transform, new Vector3(0.02f, 0.14f, 0.02f), new Vector3(0.012f, 0.13f, 0.012f), frameMat);

            // Vertical scale bar: covers 0 cm (y=0.58) down to 32 cm (y=0.26)
            CreateBox("RulerBar", rulerRoot.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.048f, 0.34f, 0.012f), rulerMat);
            CreateBox("RulerAccentEdge", rulerRoot.transform, new Vector3(-0.025f, 0.42f, 0f), new Vector3(0.004f, 0.34f, 0.013f), cyanMat, false);

            for (int cm = 0; cm <= 30; cm++)
            {
                bool isMajor = (cm % 5 == 0);
                float tickY = 0.58f - (cm * 0.01f);
                float tickW = isMajor ? 0.018f : 0.009f;
                float tickH = isMajor ? 0.0022f : 0.0012f;

                CreateBox(
                    $"Tick_{cm}cm",
                    rulerRoot.transform,
                    new Vector3(-0.024f + tickW * 0.5f, tickY, -0.007f),
                    new Vector3(tickW, tickH, 0.002f),
                    frameMat,
                    false);

                if (isMajor)
                {
                    CreateWorldTMP(
                        $"Label_{cm}cm",
                        rulerRoot.transform,
                        new Vector3(0.009f, tickY, -0.008f),
                        new Vector2(0.032f, 0.018f),
                        $"{cm}",
                        0.09f,
                        new Color(0.08f, 0.10f, 0.14f),
                        TextAlignmentOptions.Right);
                }
            }

            CreateWorldTMP(
                "RulerUnitLabel",
                rulerRoot.transform,
                new Vector3(0f, 0.605f, -0.008f),
                new Vector2(0.06f, 0.022f),
                "cm",
                0.10f,
                new Color(0.0f, 0.85f, 0.88f),
                TextAlignmentOptions.Center);
        }

        private static void BuildMassesTrayAndWeights(
            Transform parent,
            Material trayMat,
            Material goldMat,
            Material steelMat,
            Material cyanMat)
        {
            GameObject massesRoot = new GameObject("MassesTrayAndWeights");
            massesRoot.transform.SetParent(parent, false);
            massesRoot.transform.position = new Vector3(0.04f, TableTopY, 0.72f);

            CreateBox("MassTrayBase", massesRoot.transform, new Vector3(0f, 0.008f, 0f), new Vector3(0.54f, 0.016f, 0.14f), trayMat);
            CreateBox("MassTrayTrim", massesRoot.transform, new Vector3(0f, 0.008f, -0.072f), new Vector3(0.54f, 0.008f, 0.004f), cyanMat, false);

            int[] massValues = { 50, 100, 150, 200, 250 };
            for (int i = 0; i < massValues.Length; i++)
            {
                int grams = massValues[i];
                float slotX = -0.20f + i * 0.10f;

                CreateCylinder($"SlotPad_{grams}g", massesRoot.transform, new Vector3(slotX, 0.017f, 0f), new Vector3(0.065f, 0.002f, 0.065f), cyanMat, Quaternion.identity, false);

                float radius = 0.020f + i * 0.0022f;
                float height = 0.036f + i * 0.0065f;

                GameObject massGo = new GameObject($"Mass_{grams}g");
                massGo.transform.SetParent(massesRoot.transform, false);
                massGo.transform.localPosition = new Vector3(slotX, 0.018f + height * 0.5f, 0f);

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "Body";
                body.transform.SetParent(massGo.transform, false);
                body.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
                SetMat(body, goldMat);
                RemoveCollider(body);

                CreateCylinder("HookStem", massGo.transform, new Vector3(0f, height * 0.5f + 0.008f, 0f), new Vector3(0.007f, 0.008f, 0.007f), steelMat, Quaternion.identity, false);
                CreateBox("HookLoop", massGo.transform, new Vector3(0f, height * 0.5f + 0.018f, 0f), new Vector3(0.016f, 0.008f, 0.006f), steelMat, false);

                GameObject hookPoint = new GameObject("MassHookPoint");
                hookPoint.transform.SetParent(massGo.transform, false);
                hookPoint.transform.localPosition = new Vector3(0f, height * 0.5f + 0.020f, 0f);

                CapsuleCollider col = massGo.AddComponent<CapsuleCollider>();
                col.radius = radius;
                col.height = height + 0.02f;
                col.center = new Vector3(0f, 0.005f, 0f);

                Rigidbody rb = massGo.AddComponent<Rigidbody>();
                rb.mass = grams / 1000f;
                rb.isKinematic = true;
                rb.useGravity = false;

                MassObject massComp = massGo.AddComponent<MassObject>();
                massComp.Configure(grams, hookPoint.transform);

                CreateWorldTMP(
                    "MassLabel",
                    massGo.transform,
                    new Vector3(0f, 0f, -radius - 0.002f),
                    new Vector2(0.055f, 0.025f),
                    $"{grams} g",
                    0.11f,
                    new Color(0.08f, 0.09f, 0.11f),
                    TextAlignmentOptions.Center,
                    FontStyles.Bold);
            }
        }

        private static void BuildWorkstationConsoles(
            Transform parent,
            Material frameMat,
            Material cyanMat,
            Material limeMat,
            Material glassMat)
        {
            GameObject stations = new GameObject("WorkstationStations");
            stations.transform.SetParent(parent, false);

            // 1. Measure Length Button Station
            GameObject measureStation = new GameObject("MeasureButtonStation");
            measureStation.transform.SetParent(stations.transform, false);
            measureStation.transform.position = new Vector3(-0.46f, TableTopY, 0.66f);
            CreateBox("Pedestal", measureStation.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.16f, 0.04f, 0.11f), frameMat);
            CreateBox("MeasureLengthButton", measureStation.transform, new Vector3(0f, 0.048f, 0f), new Vector3(0.12f, 0.018f, 0.07f), cyanMat);
            CreateWorldTMP("Label", measureStation.transform, new Vector3(0f, 0.059f, 0f), new Vector2(0.14f, 0.05f), "MEASURE\nLENGTH", 0.09f, Color.black, TextAlignmentOptions.Center, FontStyles.Bold, Quaternion.Euler(90f, 0f, 0f));

            // 2. Stopwatch Display Station
            GameObject stopwatchStation = new GameObject("StopwatchDisplayStation");
            stopwatchStation.transform.SetParent(stations.transform, false);
            stopwatchStation.transform.position = new Vector3(0.46f, TableTopY, 0.76f);
            stopwatchStation.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
            CreateBox("StandBase", stopwatchStation.transform, new Vector3(0f, 0.012f, 0f), new Vector3(0.22f, 0.024f, 0.14f), frameMat);
            CreateBox("Bezel", stopwatchStation.transform, new Vector3(0f, 0.095f, 0.01f), new Vector3(0.24f, 0.14f, 0.02f), frameMat);
            CreateBox("Screen", stopwatchStation.transform, new Vector3(0f, 0.095f, -0.002f), new Vector3(0.22f, 0.12f, 0.005f), glassMat, false);
            CreateWorldTMP("Title", stopwatchStation.transform, new Vector3(0f, 0.135f, -0.006f), new Vector2(0.20f, 0.03f), "STOPWATCH (10 OSC)", 0.08f, new Color(0f, 0.9f, 0.92f), TextAlignmentOptions.Center, FontStyles.Bold);
            CreateWorldTMP("Digits", stopwatchStation.transform, new Vector3(0f, 0.085f, -0.006f), new Vector2(0.20f, 0.06f), "00.00 s", 0.22f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

            // 3. Future Calculation Panel Station
            GameObject calcStation = new GameObject("CalculationPanelStation");
            calcStation.transform.SetParent(stations.transform, false);
            calcStation.transform.position = new Vector3(0.48f, 1.24f, 1.18f);
            calcStation.transform.rotation = Quaternion.Euler(6f, -18f, 0f);
            CreateBox("PanelFrame", calcStation.transform, Vector3.zero, new Vector3(0.54f, 0.36f, 0.016f), frameMat, false);
            CreateBox("PanelGlass", calcStation.transform, new Vector3(0f, 0f, -0.009f), new Vector3(0.51f, 0.33f, 0.004f), glassMat, false);
            CreateBox("TopAccent", calcStation.transform, new Vector3(0f, 0.184f, -0.005f), new Vector3(0.54f, 0.01f, 0.018f), limeMat, false);
            CreateWorldTMP("CalcHeader", calcStation.transform, new Vector3(0f, 0.135f, -0.013f), new Vector2(0.48f, 0.05f), "CALCULATION DISPLAY", 0.14f, new Color(0.46f, 1.0f, 0.01f), TextAlignmentOptions.Center, FontStyles.Bold);
            CreateWorldTMP("CalcPlaceholder", calcStation.transform, new Vector3(0f, -0.02f, -0.013f), new Vector2(0.46f, 0.22f), "Step-by-step calculation\n(k = F / x)\nwill appear here after timing.", 0.11f, new Color(0.85f, 0.90f, 0.95f), TextAlignmentOptions.Center);

            // 4. Right-Side Tabletop Observation Console / Proceed Dock
            GameObject obsConsole = new GameObject("ObservationConsoleStation");
            obsConsole.transform.SetParent(stations.transform, false);
            obsConsole.transform.position = new Vector3(0.84f, TableTopY, 0.96f);
            obsConsole.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
            CreateBox("ConsoleBase", obsConsole.transform, new Vector3(0f, 0.015f, 0f), new Vector3(0.30f, 0.03f, 0.22f), frameMat);
            CreateBox("ConsoleSlope", obsConsole.transform, new Vector3(0f, 0.08f, 0.02f), new Vector3(0.28f, 0.12f, 0.02f), glassMat, false, Quaternion.Euler(-25f, 0f, 0f));
            CreateWorldTMP("ConsoleTitle", obsConsole.transform, new Vector3(0f, 0.10f, 0.005f), new Vector2(0.26f, 0.08f), "TRIAL CONTROL\n& STATUS", 0.10f, new Color(0f, 0.9f, 0.92f), TextAlignmentOptions.Center, FontStyles.Bold, Quaternion.Euler(-25f, 0f, 0f));
        }

        private static void BuildATOM8Robot(
            Transform parent,
            Material bodyMat,
            Material whiteMat,
            Material frameMat,
            Material cyanMat,
            Material limeMat)
        {
            GameObject robot = new GameObject("ATOM-8");
            robot.transform.SetParent(parent, false);
            robot.transform.position = new Vector3(-1.28f, 0f, 1.05f);
            robot.transform.rotation = Quaternion.Euler(0f, 42f, 0f);

            CreateCylinder("HoverBase", robot.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.42f, 0.03f, 0.42f), frameMat);
            CreateCylinder("HoverRing", robot.transform, new Vector3(0f, 0.012f, 0f), new Vector3(0.45f, 0.008f, 0.45f), cyanMat, Quaternion.identity, false);
            CreateCylinder("LowerColumn", robot.transform, new Vector3(0f, 0.44f, 0f), new Vector3(0.14f, 0.38f, 0.14f), bodyMat);

            GameObject torso = new GameObject("Torso");
            torso.transform.SetParent(robot.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            CreateBox("TorsoShell", torso.transform, Vector3.zero, new Vector3(0.34f, 0.40f, 0.24f), whiteMat);
            CreateBox("ChestPanel", torso.transform, new Vector3(0f, 0.02f, -0.125f), new Vector3(0.24f, 0.18f, 0.015f), frameMat, false);
            CreateCylinder("PowerCore", torso.transform, new Vector3(0f, 0.03f, -0.135f), new Vector3(0.06f, 0.008f, 0.06f), cyanMat, Quaternion.Euler(90f, 0f, 0f), false);
            CreateWorldTMP("RobotBadge", torso.transform, new Vector3(0f, -0.045f, -0.138f), new Vector2(0.22f, 0.05f), "ATOM-8", 0.13f, new Color(0.46f, 1.0f, 0.01f), TextAlignmentOptions.Center, FontStyles.Bold);

            GameObject head = new GameObject("Head");
            head.transform.SetParent(torso.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            CreateCylinder("Neck", head.transform, new Vector3(0f, -0.09f, 0f), new Vector3(0.08f, 0.04f, 0.08f), frameMat, Quaternion.identity, false);
            CreateBox("HeadShell", head.transform, Vector3.zero, new Vector3(0.24f, 0.18f, 0.20f), whiteMat);
            CreateBox("Visor", head.transform, new Vector3(0f, 0f, -0.105f), new Vector3(0.20f, 0.08f, 0.015f), frameMat, false);
            CreateCylinder("EyeLeft", head.transform, new Vector3(-0.045f, 0f, -0.115f), new Vector3(0.035f, 0.005f, 0.035f), cyanMat, Quaternion.Euler(90f, 0f, 0f), false);
            CreateCylinder("EyeRight", head.transform, new Vector3(0.045f, 0f, -0.115f), new Vector3(0.035f, 0.005f, 0.035f), cyanMat, Quaternion.Euler(90f, 0f, 0f), false);

            BuildRobotArm("LeftArm", torso.transform, new Vector3(-0.22f, 0.12f, 0f), bodyMat, cyanMat, false);
            BuildRobotArm("RightArm", torso.transform, new Vector3(0.22f, 0.12f, 0f), bodyMat, limeMat, true);
        }

        private static void BuildRobotArm(string name, Transform torso, Vector3 shoulderPos, Material armMat, Material tipMat, bool isPointerArm)
        {
            GameObject shoulder = new GameObject(name);
            shoulder.transform.SetParent(torso, false);
            shoulder.transform.localPosition = shoulderPos;

            CreateCylinder("ShoulderJoint", shoulder.transform, Vector3.zero, new Vector3(0.08f, 0.04f, 0.08f), armMat, Quaternion.Euler(0f, 0f, 90f), false);
            CreateBox("UpperArm", shoulder.transform, new Vector3(0f, -0.11f, 0f), new Vector3(0.06f, 0.18f, 0.06f), armMat, false);

            GameObject elbow = new GameObject("Elbow");
            elbow.transform.SetParent(shoulder.transform, false);
            elbow.transform.localPosition = new Vector3(0f, -0.21f, 0f);
            if (isPointerArm)
            {
                elbow.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            }

            CreateBox("Forearm", elbow.transform, new Vector3(0f, -0.09f, 0f), new Vector3(0.05f, 0.16f, 0.05f), armMat, false);
            GameObject pointerTip = new GameObject("PointerTip");
            pointerTip.transform.SetParent(elbow.transform, false);
            pointerTip.transform.localPosition = new Vector3(0f, -0.19f, 0f);
            CreateCylinder("HandTip", pointerTip.transform, Vector3.zero, new Vector3(0.045f, 0.025f, 0.045f), tipMat, Quaternion.identity, false);
        }

        private static void UpdateExistingWallBoard(Material cyanMat)
        {
            GameObject board = GameObject.Find("WallTheoryBoard");
            if (board == null)
            {
                Debug.LogWarning("[Phase2WorkstationBuilder] WallTheoryBoard not found; skipping board update.");
                return;
            }

            // Resize backing cleanly in-place to hold both Theory (left) and Observation Table (right)
            Transform backing = board.transform.Find("BoardBacking");
            if (backing != null) backing.localScale = new Vector3(2.80f, 1.24f, 0.04f);

            Transform topAccent = board.transform.Find("BoardTopAccent");
            if (topAccent != null)
            {
                topAccent.localPosition = new Vector3(0f, 0.625f, -0.01f);
                topAccent.localScale = new Vector3(2.80f, 0.02f, 0.03f);
            }

            Transform oldDivider = board.transform.Find("CenterDivider");
            if (oldDivider != null) Object.DestroyImmediate(oldDivider.gameObject);
            CreateBox("CenterDivider", board.transform, new Vector3(-0.25f, 0f, -0.022f), new Vector3(0.015f, 1.12f, 0.008f), cyanMat, false);

            Transform oldCanvas = board.transform.Find("TheoryCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);

            GameObject canvasGo = new GameObject("TheoryCanvas");
            canvasGo.transform.SetParent(board.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, -0.026f);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(2720f, 1180f);
            canvasGo.AddComponent<CanvasScaler>();

            // Left Section: THEORY
            CreateCanvasTMP(
                "TheoryHeader",
                canvasGo.transform,
                new Vector2(-820f, 470f),
                new Vector2(980f, 90f),
                "THEORY & HOOKE'S LAW",
                54f,
                FontStyles.Bold,
                new Color(0f, 0.90f, 0.92f),
                TextAlignmentOptions.Left);

            CreateCanvasTMP(
                "TheoryBody",
                canvasGo.transform,
                new Vector2(-820f, -40f),
                new Vector2(980f, 840f),
                "Objective:\nDetermine the spring constant (k) of a helical spring.\n\n" +
                "Formulas:\n" +
                "  x = L - L0   (Extension, convert cm to m)\n" +
                "  F = m * g    (g = 9.81 m/s^2, m in kg)\n" +
                "  k = F / x    (Spring constant in N/m)\n" +
                "  T = t / 10   (Time period of 1 oscillation)\n\n" +
                "Procedure:\n" +
                "1. Measure natural length L0\n" +
                "2. Attach mass (50g - 250g)\n" +
                "3. Measure stretched length L\n" +
                "4. Pull & release; time 10 oscillations\n" +
                "5. Review k and click PROCEED",
                36f,
                FontStyles.Normal,
                Color.white,
                TextAlignmentOptions.TopLeft);

            // Right Section: OBSERVATION TABLE
            CreateCanvasTMP(
                "ObservationHeader",
                canvasGo.transform,
                new Vector2(540f, 470f),
                new Vector2(1500f, 90f),
                "OBSERVATION TABLE",
                54f,
                FontStyles.Bold,
                new Color(0.46f, 1.0f, 0.01f),
                TextAlignmentOptions.Left);

            CreateCanvasTMP(
                "ObservationTableColumns",
                canvasGo.transform,
                new Vector2(540f, 320f),
                new Vector2(1520f, 80f),
                "S.No | Mass(g) |  L0(cm)  |  L(cm)   |  x(cm)  | t(10osc) |  T(s)  |  k(N/m)",
                32f,
                FontStyles.Bold,
                new Color(0f, 0.90f, 0.92f),
                TextAlignmentOptions.Left);

            int[] masses = { 50, 100, 150, 200, 250 };
            for (int i = 0; i < masses.Length; i++)
            {
                float rowY = 210f - i * 115f;
                CreateCanvasTMP(
                    $"ObsRow_{masses[i]}g",
                    canvasGo.transform,
                    new Vector2(540f, rowY),
                    new Vector2(1520f, 80f),
                    $"  {i + 1}   |   {masses[i],3}   |    —     |    —     |    —    |    —     |   —    |    —",
                    34f,
                    FontStyles.Normal,
                    Color.white,
                    TextAlignmentOptions.Left);
            }
        }

        private static void VerifyUserViewpointAndLayout()
        {
            Vector3 userEye = new Vector3(0f, 1.45f, 0.05f);
            string[] keyObjects =
            {
                "SpringApparatus",
                "MeasurementRuler",
                "MassesTrayAndWeights",
                "Mass_50g",
                "Mass_100g",
                "Mass_150g",
                "Mass_200g",
                "Mass_250g",
                "MeasureButtonStation",
                "StopwatchDisplayStation",
                "CalculationPanelStation",
                "ObservationConsoleStation",
                "ATOM-8",
                "WallTheoryBoard"
            };

            foreach (string objName in keyObjects)
            {
                GameObject go = GameObject.Find(objName);
                if (go == null)
                {
                    Debug.LogError($"[Phase2Verify] Missing expected object: {objName}");
                    continue;
                }

                Vector3 toObj = go.transform.position - userEye;
                float dist = toObj.magnitude;
                float horizAngle = Mathf.Atan2(toObj.x, toObj.z) * Mathf.Rad2Deg;
                Debug.Log($"[Phase2Verify] {objName}: pos={go.transform.position:F2}, dist={dist:F2}m, yawFromCenter={horizAngle:F1}deg");
            }
        }

        // --- Helpers ---

        private static void DestroyIfExists(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static Material LoadOrCreateMaterial(
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
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
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

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 localPos,
            Vector3 scale,
            Material mat,
            bool keepCollider = true,
            Quaternion? localRot = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = scale;
            SetMat(go, mat);
            if (!keepCollider) RemoveCollider(go);
            return go;
        }

        private static GameObject CreateCylinder(
            string name,
            Transform parent,
            Vector3 localPos,
            Vector3 scale,
            Material mat,
            Quaternion? localRot = null,
            bool keepCollider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = scale;
            SetMat(go, mat);
            if (!keepCollider) RemoveCollider(go);
            return go;
        }

        private static void SetMat(GameObject go, Material mat)
        {
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        private static void CreateWorldTMP(
            string name,
            Transform parent,
            Vector3 localPos,
            Vector2 size,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            FontStyles style = FontStyles.Normal,
            Quaternion? localRot = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = size;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }

        private static void CreateCanvasTMP(
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
