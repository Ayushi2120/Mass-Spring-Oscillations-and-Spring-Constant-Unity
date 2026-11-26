using MassSpringLab.Apparatus;
using MassSpringLab.Core;
using MassSpringLab.Interaction;
using MassSpringLab.Robot;
using MassSpringLab.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Wires the complete 7-column Observation Table, Oscillation &amp; Stopwatch,
    /// large-font Calculations Panel, Calculation Spotlight, ATOM-8 Next-Mass Guidance,
    /// and Proceed workflow into the existing MassSpringLab.unity scene without
    /// modifying grabbing, attachment, spring extension, or measurement logic.
    /// </summary>
    public static class Phase7CompleteWorkflowBuilder
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";

        [MenuItem("MassSpringLab/Build Complete Observation & Calculation Workflow")]
        public static void BuildCompleteObservationAndCalculationWorkflow()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Upgrade 7-column Observation Table on WallTheoryBoard
            ObservationTableUI obsTable = UpgradeWhiteboardObservationTable();

            // 2. Upgrade Calculations Panel text & add Calculation Spotlight
            CalculationDisplayUI calcUI = UpgradeCalculationsPanel();
            CalculationSpotlightController spotlightCtrl = SetupCalculationSpotlight();

            // 3. Wire StopwatchController on StopwatchDisplayStation
            StopwatchController stopwatchCtrl = SetupStopwatch();

            // 4. Wire SpringOscillationController on SpringApparatus & SpringPullHandle on SpringBottomHook
            SpringOscillationController oscCtrl = SetupOscillation(stopwatchCtrl);
            SetupSpringPullHandle(oscCtrl);

            // 5. Wire Atom8GuidanceController on ATOM-8
            Atom8GuidanceController atom8Ctrl = SetupAtom8Guidance();

            // 6. Wire Pull & Proceed buttons on ObservationConsoleStation + CompleteWorkflowCoordinator
            SetupCoordinatorAndConsole(obsTable, calcUI, spotlightCtrl, stopwatchCtrl, oscCtrl, atom8Ctrl);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase7CompleteWorkflowBuilder] Complete observation & calculation workflow wired and saved.");
        }

        private static ObservationTableUI UpgradeWhiteboardObservationTable()
        {
            GameObject theoryCanvas = GameObject.Find("WallTheoryBoard/TheoryCanvas");
            if (theoryCanvas == null) return null;

            const float centerX = 555f;
            const float width = 1520f;

            TextMeshProUGUI header = theoryCanvas.transform.Find("ObservationHeader")?.GetComponent<TextMeshProUGUI>();
            if (header != null)
            {
                header.rectTransform.anchoredPosition = new Vector2(centerX, 475f);
                header.rectTransform.sizeDelta = new Vector2(width, 100f);
                header.text = "OBSERVATION TABLE";
                header.fontSize = 66f;
                header.fontStyle = FontStyles.Bold;
                header.color = new Color(0.46f, 1.0f, 0.01f);
            }

            TextMeshProUGUI cols = theoryCanvas.transform.Find("ObservationTableColumns")?.GetComponent<TextMeshProUGUI>();
            if (cols != null)
            {
                cols.rectTransform.anchoredPosition = new Vector2(centerX, 345f);
                cols.rectTransform.sizeDelta = new Vector2(width, 95f);
                cols.text = "Mass<pos=14%>L0(cm)<pos=30%>L(cm)<pos=45%>x(cm)<pos=59%>N<pos=69%>t(s)<pos=85%>T(s)";
                cols.fontSize = 36f;
                cols.fontStyle = FontStyles.Bold;
                cols.color = new Color(0.0f, 0.90f, 0.92f);
                cols.textWrappingMode = TextWrappingModes.NoWrap;
            }

            int[] masses = { 50, 100, 150, 200, 250 };
            for (int i = 0; i < masses.Length; i++)
            {
                int m = masses[i];
                TextMeshProUGUI row = theoryCanvas.transform.Find($"ObsRow_{m}g")?.GetComponent<TextMeshProUGUI>();
                if (row != null)
                {
                    row.rectTransform.anchoredPosition = new Vector2(centerX, 210f - i * 145f);
                    row.rectTransform.sizeDelta = new Vector2(width, 115f);
                    row.text = ObservationTableUI.FormatRow(m, "—", "—", "—", "—", "—", "—");
                    row.fontSize = 42f;
                    row.color = Color.white;
                    row.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }

            ObservationTableUI tableUI = theoryCanvas.GetComponent<ObservationTableUI>();
            if (tableUI == null) tableUI = theoryCanvas.AddComponent<ObservationTableUI>();
            EditorUtility.SetDirty(theoryCanvas);
            return tableUI;
        }

        private static CalculationDisplayUI UpgradeCalculationsPanel()
        {
            GameObject calcStation = GameObject.Find("CalculationPanelStation");
            if (calcStation == null) return null;

            TextMeshPro header = calcStation.transform.Find("CalcHeader")?.GetComponent<TextMeshPro>();
            if (header != null)
            {
                header.fontSize = 0.18f;
                header.rectTransform.sizeDelta = new Vector2(0.50f, 0.06f);
                header.text = "CALCULATIONS";
            }

            TextMeshPro body = calcStation.transform.Find("CalcPlaceholder")?.GetComponent<TextMeshPro>();
            if (body != null)
            {
                body.fontSize = 0.13f;
                body.rectTransform.anchoredPosition = new Vector2(0f, -0.025f);
                body.rectTransform.sizeDelta = new Vector2(0.49f, 0.25f);
                body.alignment = TextAlignmentOptions.TopLeft;
                body.text = "Measure L0, attach a mass,\nmeasure L, then pull & release\nto time 10 oscillations.";
            }

            CalculationDisplayUI ui = calcStation.GetComponent<CalculationDisplayUI>();
            if (ui == null) ui = calcStation.AddComponent<CalculationDisplayUI>();
            EditorUtility.SetDirty(calcStation);
            return ui;
        }

        private static CalculationSpotlightController SetupCalculationSpotlight()
        {
            GameObject calcStation = GameObject.Find("CalculationPanelStation");
            if (calcStation == null) return null;

            GameObject spotGo = GameObject.Find("CalculationSpotlight");
            if (spotGo == null) spotGo = new GameObject("CalculationSpotlight");

            spotGo.transform.position = calcStation.transform.position + new Vector3(-0.15f, 0.55f, -0.75f);
            spotGo.transform.LookAt(calcStation.transform.position);

            Light spot = spotGo.GetComponent<Light>();
            if (spot == null) spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(0.75f, 1.0f, 0.88f);
            spot.range = 3.5f;
            spot.spotAngle = 42f;
            spot.intensity = 0f;

            CalculationSpotlightController ctrl = spotGo.GetComponent<CalculationSpotlightController>();
            if (ctrl == null) ctrl = spotGo.AddComponent<CalculationSpotlightController>();
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("spotlight").objectReferenceValue = spot;
            so.ApplyModifiedPropertiesWithoutUndo();

            return ctrl;
        }

        private static StopwatchController SetupStopwatch()
        {
            GameObject sw = GameObject.Find("StopwatchDisplayStation");
            if (sw == null) return null;

            TextMeshPro title = sw.transform.Find("Title")?.GetComponent<TextMeshPro>();
            if (title != null) title.fontSize = 0.095f;

            TextMeshPro digits = sw.transform.Find("Digits")?.GetComponent<TextMeshPro>();
            if (digits != null) digits.fontSize = 0.28f;

            StopwatchController ctrl = sw.GetComponent<StopwatchController>();
            if (ctrl == null) ctrl = sw.AddComponent<StopwatchController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("digitsText").objectReferenceValue = digits;
            so.FindProperty("targetOscillations").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sw);
            return ctrl;
        }

        private static SpringOscillationController SetupOscillation(StopwatchController stopwatch)
        {
            GameObject apparatus = GameObject.Find("SpringApparatus");
            if (apparatus == null) return null;

            SpringOscillationController ctrl = apparatus.GetComponent<SpringOscillationController>();
            if (ctrl == null) ctrl = apparatus.AddComponent<SpringOscillationController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("springPhysics").objectReferenceValue = apparatus.GetComponent<SpringPhysicsController>();
            so.FindProperty("springSocket").objectReferenceValue = Object.FindFirstObjectByType<SpringAttachmentPoint>();
            so.FindProperty("measurementController").objectReferenceValue = Object.FindFirstObjectByType<SpringLengthMeasurementController>();
            so.FindProperty("stopwatch").objectReferenceValue = stopwatch;
            so.FindProperty("targetOscillations").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(apparatus);
            return ctrl;
        }

        private static void SetupSpringPullHandle(SpringOscillationController oscCtrl)
        {
            GameObject bottomHook = GameObject.Find("SpringApparatus/SpringBottomHook");
            if (bottomHook == null) return;

            SphereCollider col = bottomHook.GetComponent<SphereCollider>();
            if (col == null) col = bottomHook.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.08f;
            col.center = new Vector3(0f, -0.04f, 0f);

            XRSimpleInteractable simple = bottomHook.GetComponent<XRSimpleInteractable>();
            if (simple == null) simple = bottomHook.AddComponent<XRSimpleInteractable>();
            simple.interactionLayers = InteractionLayerMask.GetMask("Default");
            simple.colliders.Clear();
            simple.colliders.Add(col);
            simple.enabled = false;

            SpringPullHandle handle = bottomHook.GetComponent<SpringPullHandle>();
            if (handle == null) handle = bottomHook.AddComponent<SpringPullHandle>();
            SerializedObject so = new SerializedObject(handle);
            so.FindProperty("oscillationController").objectReferenceValue = oscCtrl;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bottomHook);
        }

        private static Atom8GuidanceController SetupAtom8Guidance()
        {
            GameObject atom8 = GameObject.Find("ATOM-8");
            if (atom8 == null) return null;

            Atom8GuidanceController ctrl = atom8.GetComponent<Atom8GuidanceController>();
            if (ctrl == null) ctrl = atom8.AddComponent<Atom8GuidanceController>();
            EditorUtility.SetDirty(atom8);
            return ctrl;
        }

        private static void SetupCoordinatorAndConsole(
            ObservationTableUI obsTable,
            CalculationDisplayUI calcUI,
            CalculationSpotlightController spotlight,
            StopwatchController stopwatch,
            SpringOscillationController oscCtrl,
            Atom8GuidanceController atom8)
        {
            GameObject obsConsole = GameObject.Find("ObservationConsoleStation");
            LabActionButton pullBtn = null;
            LabActionButton proceedBtn = null;
            TextMeshPro consoleStatus = null;

            if (obsConsole != null)
            {
                consoleStatus = obsConsole.transform.Find("ConsoleTitle")?.GetComponent<TextMeshPro>();
                if (consoleStatus != null)
                {
                    consoleStatus.transform.localPosition = new Vector3(0f, 0.135f, 0.015f);
                    consoleStatus.fontSize = 0.082f;
                }

                pullBtn = CreateOrUpdateConsoleButton(obsConsole.transform, "PullOscillateButton", new Vector3(-0.075f, 0.042f, -0.052f), "PULL &\nOSCILLATE", new Color(0.0f, 0.85f, 0.88f));
                proceedBtn = CreateOrUpdateConsoleButton(obsConsole.transform, "ProceedButton", new Vector3(0.075f, 0.042f, -0.052f), "PROCEED", new Color(0.46f, 1.0f, 0.01f));
            }

            GameObject coordGo = GameObject.Find("CompleteWorkflowCoordinator");
            if (coordGo == null) coordGo = new GameObject("CompleteWorkflowCoordinator");
            CompleteWorkflowCoordinator coord = coordGo.GetComponent<CompleteWorkflowCoordinator>();
            if (coord == null) coord = coordGo.AddComponent<CompleteWorkflowCoordinator>();

            SerializedObject so = new SerializedObject(coord);
            so.FindProperty("springSocket").objectReferenceValue = Object.FindFirstObjectByType<SpringAttachmentPoint>();
            so.FindProperty("springPhysics").objectReferenceValue = Object.FindFirstObjectByType<SpringPhysicsController>();
            so.FindProperty("measurementController").objectReferenceValue = Object.FindFirstObjectByType<SpringLengthMeasurementController>();
            so.FindProperty("oscillationController").objectReferenceValue = oscCtrl;
            so.FindProperty("stopwatch").objectReferenceValue = stopwatch;
            so.FindProperty("observationTable").objectReferenceValue = obsTable;
            so.FindProperty("calculationDisplay").objectReferenceValue = calcUI;
            so.FindProperty("calculationSpotlight").objectReferenceValue = spotlight;
            so.FindProperty("atom8Guidance").objectReferenceValue = atom8;
            so.FindProperty("pullOscillateButton").objectReferenceValue = pullBtn;
            so.FindProperty("proceedButton").objectReferenceValue = proceedBtn;
            so.FindProperty("consoleStatusText").objectReferenceValue = consoleStatus;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LabActionButton CreateOrUpdateConsoleButton(
            Transform parent,
            string name,
            Vector3 localPos,
            string labelText,
            Color enabledColor)
        {
            Transform old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject btnGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btnGo.name = name;
            btnGo.transform.SetParent(parent, false);
            btnGo.transform.localPosition = localPos;
            btnGo.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            btnGo.transform.localScale = new Vector3(0.13f, 0.022f, 0.065f);

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            lblGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            lblGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lblGo.transform.localScale = new Vector3(7.69f, 15.38f, 45.45f);

            TextMeshPro lblTmp = lblGo.AddComponent<TextMeshPro>();
            lblTmp.rectTransform.sizeDelta = new Vector2(0.12f, 0.055f);
            lblTmp.text = labelText;
            lblTmp.fontSize = 0.068f;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.color = Color.black;
            lblTmp.alignment = TextAlignmentOptions.Center;

            XRSimpleInteractable simple = btnGo.AddComponent<XRSimpleInteractable>();
            simple.interactionLayers = InteractionLayerMask.GetMask("Default");
            Collider col = btnGo.GetComponent<Collider>();
            if (col != null)
            {
                simple.colliders.Clear();
                simple.colliders.Add(col);
            }

            LabActionButton btn = btnGo.AddComponent<LabActionButton>();
            SerializedObject so = new SerializedObject(btn);
            so.FindProperty("buttonRenderer").objectReferenceValue = btnGo.GetComponent<Renderer>();
            so.FindProperty("buttonLabel").objectReferenceValue = lblTmp;
            so.FindProperty("enabledColor").colorValue = enabledColor;
            so.ApplyModifiedPropertiesWithoutUndo();

            return btn;
        }
    }
}
