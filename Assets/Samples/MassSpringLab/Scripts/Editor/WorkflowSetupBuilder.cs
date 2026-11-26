using MassSpringLab.Apparatus;
using MassSpringLab.Core;
using MassSpringLab.Interaction;
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
    /// Wires the complete practical workflow into MassSpringLab.unity:
    /// compact spring hook + attachment point, Hooke's Law physics,
    /// Measure Length &amp; Proceed buttons, ObservationTableUI, CalculationDisplayUI,
    /// and ExperimentController.
    /// </summary>
    public static class WorkflowSetupBuilder
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";

        [MenuItem("MassSpringLab/Build Complete Practical Workflow")]
        public static void BuildCompleteWorkflow()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Compact SpringBottomHook & remove floating AttachmentHalo disc
            GameObject apparatus = GameObject.Find("SpringApparatus");
            Transform topAnchor = apparatus.transform.Find("SpringTopAnchor");
            Transform bottomHook = apparatus.transform.Find("SpringBottomHook");

            Transform hookStem = bottomHook.Find("HookStem");
            if (hookStem != null)
            {
                hookStem.localPosition = new Vector3(0f, -0.004f, 0f);
                hookStem.localScale = new Vector3(0.008f, 0.006f, 0.008f);
            }

            Transform hookCollar = bottomHook.Find("HookCollar");
            Renderer collarRenderer = null;
            if (hookCollar != null)
            {
                hookCollar.localPosition = new Vector3(0f, -0.010f, 0f);
                hookCollar.localScale = new Vector3(0.022f, 0.008f, 0.022f);
                collarRenderer = hookCollar.GetComponent<Renderer>();
            }

            SpringAttachmentPoint socket = bottomHook.GetComponentInChildren<SpringAttachmentPoint>();
            if (socket != null)
            {
                socket.transform.localPosition = new Vector3(0f, -0.012f, 0f);
                Transform oldHalo = socket.transform.Find("AttachmentHalo");
                if (oldHalo != null) Object.DestroyImmediate(oldHalo.gameObject);

                SphereCollider trig = socket.GetComponent<SphereCollider>();
                if (trig == null) trig = socket.gameObject.AddComponent<SphereCollider>();
                trig.isTrigger = true;
                trig.radius = 0.16f;
            }

            // 2. Wire SpringPhysicsController on SpringApparatus
            SpringPhysicsController physics = apparatus.GetComponent<SpringPhysicsController>();
            if (physics == null) physics = apparatus.AddComponent<SpringPhysicsController>();

            SerializedObject physicsSo = new SerializedObject(physics);
            physicsSo.FindProperty("springConstant").floatValue = 20.0f;
            physicsSo.FindProperty("gravity").floatValue = 9.81f;
            physicsSo.FindProperty("naturalLength").floatValue = 0.12f;
            physicsSo.FindProperty("dampingRatio").floatValue = 0.24f;
            physicsSo.FindProperty("tableTopY").floatValue = 0.85f;
            physicsSo.FindProperty("minTableClearance").floatValue = 0.05f;
            physicsSo.FindProperty("maxAllowedExtension").floatValue = 0.22f;
            physicsSo.FindProperty("assumedMassDropBelowHook").floatValue = 0.09f;
            physicsSo.FindProperty("springTopAnchor").objectReferenceValue = topAnchor;
            physicsSo.FindProperty("springBottomHook").objectReferenceValue = bottomHook;
            physicsSo.ApplyModifiedPropertiesWithoutUndo();

            if (socket != null)
            {
                SerializedObject sockSo = new SerializedObject(socket);
                sockSo.FindProperty("springPhysics").objectReferenceValue = physics;
                sockSo.FindProperty("haloRenderer").objectReferenceValue = collarRenderer;
                sockSo.FindProperty("snapRadius").floatValue = 0.16f;
                sockSo.FindProperty("highlightRadius").floatValue = 0.28f;
                sockSo.FindProperty("autoSnapWhenGrabbedNearHook").boolValue = true;
                sockSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // 3. Configure MeasureLengthButton as LabActionButton
            LabActionButton measureBtn = ConfigureActionButton(
                GameObject.Find("MeasureLengthButton"),
                GameObject.Find("MeasureButtonStation/Label")?.GetComponent<TextMeshPro>(),
                new Color(0.0f, 0.85f, 0.88f));

            // 4. Configure ProceedButton & Status on ObservationConsoleStation
            GameObject obsConsole = GameObject.Find("ObservationConsoleStation");
            LabActionButton proceedBtn = null;
            TextMeshPro consoleStatus = null;
            if (obsConsole != null)
            {
                consoleStatus = obsConsole.transform.Find("ConsoleTitle")?.GetComponent<TextMeshPro>();
                if (consoleStatus != null)
                {
                    consoleStatus.transform.localPosition = new Vector3(0f, 0.125f, 0.015f);
                    consoleStatus.fontSize = 0.085f;
                }

                Transform oldBtn = obsConsole.transform.Find("ProceedButton");
                if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);

                GameObject btnGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                btnGo.name = "ProceedButton";
                btnGo.transform.SetParent(obsConsole.transform, false);
                btnGo.transform.localPosition = new Vector3(0f, 0.042f, -0.045f);
                btnGo.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                btnGo.transform.localScale = new Vector3(0.20f, 0.022f, 0.075f);

                GameObject lblGo = new GameObject("ProceedLabel");
                lblGo.transform.SetParent(btnGo.transform, false);
                lblGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                lblGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                lblGo.transform.localScale = new Vector3(5f, 13.3f, 45f);
                TextMeshPro lblTmp = lblGo.AddComponent<TextMeshPro>();
                lblTmp.rectTransform.sizeDelta = new Vector2(0.18f, 0.06f);
                lblTmp.text = "PROCEED";
                lblTmp.fontSize = 0.085f;
                lblTmp.fontStyle = FontStyles.Bold;
                lblTmp.color = Color.black;
                lblTmp.alignment = TextAlignmentOptions.Center;

                proceedBtn = ConfigureActionButton(btnGo, lblTmp, new Color(0.46f, 1.0f, 0.01f));
            }

            // 5. Wire ObservationTableUI & CalculationDisplayUI
            GameObject theoryCanvas = GameObject.Find("WallTheoryBoard/TheoryCanvas");
            ObservationTableUI obsTable = null;
            if (theoryCanvas != null)
            {
                obsTable = theoryCanvas.GetComponent<ObservationTableUI>();
                if (obsTable == null) obsTable = theoryCanvas.AddComponent<ObservationTableUI>();
            }

            GameObject calcStation = GameObject.Find("CalculationPanelStation");
            CalculationDisplayUI calcUI = null;
            if (calcStation != null)
            {
                calcUI = calcStation.GetComponent<CalculationDisplayUI>();
                if (calcUI == null) calcUI = calcStation.AddComponent<CalculationDisplayUI>();
            }

            // 6. Wire ExperimentController
            GameObject controllerGo = GameObject.Find("ExperimentController");
            if (controllerGo == null) controllerGo = new GameObject("ExperimentController");
            ExperimentController exp = controllerGo.GetComponent<ExperimentController>();
            if (exp == null) exp = controllerGo.AddComponent<ExperimentController>();

            SerializedObject expSo = new SerializedObject(exp);
            expSo.FindProperty("springPhysics").objectReferenceValue = physics;
            expSo.FindProperty("springSocket").objectReferenceValue = socket;
            expSo.FindProperty("measureLengthButton").objectReferenceValue = measureBtn;
            expSo.FindProperty("proceedButton").objectReferenceValue = proceedBtn;
            expSo.FindProperty("consoleStatusText").objectReferenceValue = consoleStatus;
            expSo.FindProperty("observationTable").objectReferenceValue = obsTable;
            expSo.FindProperty("calculationDisplay").objectReferenceValue = calcUI;
            expSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WorkflowSetupBuilder] Complete practical workflow built and saved.");
        }

        private static LabActionButton ConfigureActionButton(GameObject btnGo, TextMeshPro label, Color enabledColor)
        {
            if (btnGo == null) return null;

            XRSimpleInteractable simple = btnGo.GetComponent<XRSimpleInteractable>();
            if (simple == null) simple = btnGo.AddComponent<XRSimpleInteractable>();
            simple.interactionLayers = InteractionLayerMask.GetMask("Default");

            Collider col = btnGo.GetComponent<Collider>();
            if (col != null)
            {
                simple.colliders.Clear();
                simple.colliders.Add(col);
            }

            LabActionButton btn = btnGo.GetComponent<LabActionButton>();
            if (btn == null) btn = btnGo.AddComponent<LabActionButton>();

            SerializedObject so = new SerializedObject(btn);
            so.FindProperty("buttonRenderer").objectReferenceValue = btnGo.GetComponent<Renderer>();
            so.FindProperty("buttonLabel").objectReferenceValue = label;
            so.FindProperty("enabledColor").colorValue = enabledColor;
            so.ApplyModifiedPropertiesWithoutUndo();

            return btn;
        }
    }
}
