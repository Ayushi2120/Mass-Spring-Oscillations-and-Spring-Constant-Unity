using MassSpringLab.Apparatus;
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
    /// Wires Phase 5 measurement functionality onto the EXISTING MeasureLengthButton,
    /// SpringTopAnchor, SpringBottomHook, ObservationConsoleStation, and WallTheoryBoard
    /// without altering mass grabbing, attachment, spring physics, or scene layout.
    /// </summary>
    public static class Phase5MeasurementBuilder
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";

        [MenuItem("MassSpringLab/Apply Phase 5 Length Measurement")]
        public static void ApplyPhase5Measurement()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject measureBtnGo = GameObject.Find("MeasureButtonStation/MeasureLengthButton");
            TextMeshPro measureBtnLabel = GameObject.Find("MeasureButtonStation/Label")?.GetComponent<TextMeshPro>();
            if (measureBtnGo == null)
            {
                Debug.LogError("[Phase5MeasurementBuilder] MeasureLengthButton not found!");
                return;
            }

            XRSimpleInteractable simple = measureBtnGo.GetComponent<XRSimpleInteractable>();
            if (simple == null) simple = measureBtnGo.AddComponent<XRSimpleInteractable>();
            simple.interactionLayers = InteractionLayerMask.GetMask("Default");

            Collider btnCol = measureBtnGo.GetComponent<Collider>();
            if (btnCol != null)
            {
                simple.colliders.Clear();
                simple.colliders.Add(btnCol);
            }

            LabActionButton actionBtn = measureBtnGo.GetComponent<LabActionButton>();
            if (actionBtn == null) actionBtn = measureBtnGo.AddComponent<LabActionButton>();

            SerializedObject btnSo = new SerializedObject(actionBtn);
            btnSo.FindProperty("buttonRenderer").objectReferenceValue = measureBtnGo.GetComponent<Renderer>();
            btnSo.FindProperty("buttonLabel").objectReferenceValue = measureBtnLabel;
            btnSo.FindProperty("enabledColor").colorValue = new Color(0.0f, 0.85f, 0.88f);
            btnSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject theoryCanvas = GameObject.Find("WallTheoryBoard/TheoryCanvas");
            ObservationTableUI obsTable = null;
            if (theoryCanvas != null)
            {
                obsTable = theoryCanvas.GetComponent<ObservationTableUI>();
                if (obsTable == null) obsTable = theoryCanvas.AddComponent<ObservationTableUI>();
            }

            GameObject measureStation = GameObject.Find("MeasureButtonStation");
            SpringLengthMeasurementController measCtrl = measureStation.GetComponent<SpringLengthMeasurementController>();
            if (measCtrl == null) measCtrl = measureStation.AddComponent<SpringLengthMeasurementController>();

            GameObject topAnchor = GameObject.Find("SpringApparatus/SpringTopAnchor");
            GameObject bottomHook = GameObject.Find("SpringApparatus/SpringBottomHook");
            SpringAttachmentPoint socket = Object.FindFirstObjectByType<SpringAttachmentPoint>();
            SpringPhysicsController physics = Object.FindFirstObjectByType<SpringPhysicsController>();
            TextMeshPro statusTmp = GameObject.Find("ObservationConsoleStation/ConsoleTitle")?.GetComponent<TextMeshPro>();

            SerializedObject measSo = new SerializedObject(measCtrl);
            measSo.FindProperty("springTopAnchor").objectReferenceValue = topAnchor != null ? topAnchor.transform : null;
            measSo.FindProperty("springBottomHook").objectReferenceValue = bottomHook != null ? bottomHook.transform : null;
            measSo.FindProperty("springSocket").objectReferenceValue = socket;
            measSo.FindProperty("springPhysics").objectReferenceValue = physics;
            measSo.FindProperty("measureLengthButton").objectReferenceValue = actionBtn;
            measSo.FindProperty("statusDisplayText").objectReferenceValue = statusTmp;
            measSo.FindProperty("observationTable").objectReferenceValue = obsTable;
            measSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(measureBtnGo);
            EditorUtility.SetDirty(measureStation);
            if (theoryCanvas != null) EditorUtility.SetDirty(theoryCanvas);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase5MeasurementBuilder] Phase 5 length measurement wired into existing scene.");
        }
    }
}
