using System.Collections.Generic;
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
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Wires ATOM-8 voice narration, AudioClips, synchronized world-space subtitles,
    /// Replay button, and VoiceGuidanceCoordinator into MassSpringLab.unity.
    /// </summary>
    public static class Phase9VoiceGuidanceBuilder
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";
        private const string AudioDir = "Assets/Samples/MassSpringLab/Resources/Narration";
        private const string FrameMatPath = "Assets/Samples/MassSpringLab/Materials/Mat_TableFrame.mat";

        [MenuItem("MassSpringLab/Wire ATOM-8 Voice Narration & Subtitles")]
        public static void WireVoiceGuidance()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject atom8 = GameObject.Find("ATOM-8");
            if (atom8 == null)
            {
                Debug.LogError("[Phase9VoiceGuidanceBuilder] ATOM-8 not found!");
                return;
            }

            // Position ATOM-8 beside left-front of workstation, visible & non-obstructive
            atom8.transform.position = new Vector3(-1.24f, 0f, 0.82f);
            atom8.transform.rotation = Quaternion.Euler(0f, 58f, 0f);

            // 1. AudioSource on ATOM-8 (headset-audible VR settings)
            AudioSource audioSrc = atom8.GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = atom8.AddComponent<AudioSource>();
            audioSrc.enabled = true;
            audioSrc.mute = false;
            audioSrc.playOnAwake = false;
            audioSrc.loop = false;
            audioSrc.spatialBlend = 0.0f; // Clear, consistent headset audio regardless of head rotation
            audioSrc.volume = 1.0f;

            // 2. World-Space Subtitle Panel above ATOM-8's head
            Transform oldSub = atom8.transform.Find("Atom8SubtitlePanel");
            if (oldSub != null) Object.DestroyImmediate(oldSub.gameObject);

            GameObject subRoot = new GameObject("Atom8SubtitlePanel");
            subRoot.transform.SetParent(atom8.transform, false);
            subRoot.transform.localPosition = new Vector3(0f, 1.66f, -0.05f);
            subRoot.transform.localRotation = Quaternion.identity;

            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Backdrop";
            bg.transform.SetParent(subRoot.transform, false);
            bg.transform.localScale = new Vector3(0.86f, 0.22f, 0.012f);
            Collider bgCol = bg.GetComponent<Collider>();
            if (bgCol != null) Object.DestroyImmediate(bgCol);
            Material frameMat = AssetDatabase.LoadAssetAtPath<Material>(FrameMatPath);
            if (frameMat != null) bg.GetComponent<Renderer>().sharedMaterial = frameMat;

            GameObject textGo = new GameObject("SubtitleText");
            textGo.transform.SetParent(subRoot.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.010f);
            TextMeshPro subTmp = textGo.AddComponent<TextMeshPro>();
            subTmp.rectTransform.sizeDelta = new Vector2(0.80f, 0.19f);
            subTmp.fontSize = 0.24f;
            subTmp.color = Color.white;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.textWrappingMode = TextWrappingModes.Normal;

            // 3. Atom8VoiceNarrator with all 22 WAV clips
            Atom8VoiceNarrator narrator = atom8.GetComponent<Atom8VoiceNarrator>();
            if (narrator == null) narrator = atom8.AddComponent<Atom8VoiceNarrator>();

            var cueDefs = new (string id, string sub)[]
            {
                ("01_welcome",             "Welcome to the Mass-Spring Lab! Let's explore how mass affects spring extension and oscillation."),
                ("02a_natural_length",     "First, let's measure the spring's natural length."),
                ("02a_look_ruler",         "Look at the ruler and measure its length."),
                ("02b_natural_recorded",   "Perfect! Natural length recorded."),
                ("03_pick_50g",            "Pick up the fifty-gram mass and attach it to the spring."),
                ("03_pick_100g",           "Pick up the one-hundred-gram mass and attach it to the spring."),
                ("03_pick_150g",           "Pick up the one-hundred-fifty-gram mass and attach it to the spring."),
                ("03_pick_200g",           "Pick up the two-hundred-gram mass and attach it to the spring."),
                ("03_pick_250g",           "Pick up the two-hundred-fifty-gram mass and attach it to the spring."),
                ("03b_attach_point",       "Attach the mass right here at the bottom of the spring."),
                ("04a_extended_length",    "Now measure the extended length."),
                ("04a_click_measure",      "Click the measurement button when you're ready."),
                ("04b_extension_recorded", "Extension recorded! Next, let's observe its motion."),
                ("05a_osc_controller",     "Grab the mass, pull it gently downward, and release it."),
                ("05b_osc_hands",          "Pinch the mass, pull it gently downward, and release."),
                ("06a_oscillating",        "Watch the mass as it oscillates. I'll record the timing automatically."),
                ("06b_osc_done",           "Done! Your time period has been calculated."),
                ("07_calculations",        "Let's calculate the spring constant using your measurements."),
                ("08_observation_table",   "Your results are recorded here. Let's review them together."),
                ("09_ready_proceed",       "Ready for the next mass? Click Proceed to continue."),
                ("09_next_100g",           "Let's try the one-hundred-gram mass next. Pick it up and attach it here."),
                ("09_next_150g",           "Let's try the one-hundred-fifty-gram mass next. Pick it up and attach it here."),
                ("09_next_200g",           "Let's try the two-hundred-gram mass next. Pick it up and attach it here."),
                ("09_next_250g",           "Let's try the two-hundred-fifty-gram mass next. Pick it up and attach it here."),
                ("10_complete",            "Excellent work! You've completed the experiment. Let's review your results.")
            };

            SerializedObject narSo = new SerializedObject(narrator);
            narSo.FindProperty("audioSource").objectReferenceValue = audioSrc;
            narSo.FindProperty("subtitleText").objectReferenceValue = subTmp;
            narSo.FindProperty("subtitleBackdrop").objectReferenceValue = subRoot;

            SerializedProperty cuesProp = narSo.FindProperty("cues");
            cuesProp.arraySize = cueDefs.Length;
            for (int i = 0; i < cueDefs.Length; i++)
            {
                SerializedProperty elem = cuesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("id").stringValue = cueDefs[i].id;
                elem.FindPropertyRelative("subtitle").stringValue = cueDefs[i].sub;
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{cueDefs[i].id}.wav");
                elem.FindPropertyRelative("clip").objectReferenceValue = clip;
            }
            narSo.ApplyModifiedPropertiesWithoutUndo();

            // 4. Replay Button on ObservationConsoleStation
            GameObject obsConsole = GameObject.Find("ObservationConsoleStation");
            LabActionButton replayBtn = null;
            if (obsConsole != null)
            {
                replayBtn = CreateReplayButton(obsConsole.transform);
            }

            // 5. VoiceGuidanceCoordinator
            GameObject vgcGo = GameObject.Find("VoiceGuidanceCoordinator");
            if (vgcGo == null) vgcGo = new GameObject("VoiceGuidanceCoordinator");
            VoiceGuidanceCoordinator vgc = vgcGo.GetComponent<VoiceGuidanceCoordinator>();
            if (vgc == null) vgc = vgcGo.AddComponent<VoiceGuidanceCoordinator>();

            SerializedObject vgcSo = new SerializedObject(vgc);
            vgcSo.FindProperty("narrator").objectReferenceValue = narrator;
            vgcSo.FindProperty("atom8").objectReferenceValue = atom8.GetComponent<Atom8GuidanceController>();
            vgcSo.FindProperty("measurementController").objectReferenceValue = Object.FindFirstObjectByType<SpringLengthMeasurementController>();
            vgcSo.FindProperty("springSocket").objectReferenceValue = Object.FindFirstObjectByType<SpringAttachmentPoint>();
            vgcSo.FindProperty("springPhysics").objectReferenceValue = Object.FindFirstObjectByType<SpringPhysicsController>();
            vgcSo.FindProperty("oscillationController").objectReferenceValue = Object.FindFirstObjectByType<SpringOscillationController>();
            vgcSo.FindProperty("stopwatch").objectReferenceValue = Object.FindFirstObjectByType<StopwatchController>();
            vgcSo.FindProperty("spotlight").objectReferenceValue = Object.FindFirstObjectByType<CalculationSpotlightController>();
            vgcSo.FindProperty("modalityManager").objectReferenceValue = Object.FindFirstObjectByType<XRInputModalityManager>();
            vgcSo.FindProperty("replayButton").objectReferenceValue = replayBtn;

            vgcSo.FindProperty("workstationTarget").objectReferenceValue = GameObject.Find("LabTable")?.transform;
            vgcSo.FindProperty("rulerTarget").objectReferenceValue = GameObject.Find("MeasurementRuler")?.transform;
            vgcSo.FindProperty("measureButtonTarget").objectReferenceValue = GameObject.Find("MeasureButtonStation/MeasureLengthButton")?.transform;
            vgcSo.FindProperty("springHookTarget").objectReferenceValue = GameObject.Find("SpringApparatus/SpringBottomHook")?.transform;
            vgcSo.FindProperty("stopwatchTarget").objectReferenceValue = GameObject.Find("StopwatchDisplayStation")?.transform;
            vgcSo.FindProperty("calculationsTarget").objectReferenceValue = GameObject.Find("CalculationPanelStation")?.transform;
            vgcSo.FindProperty("observationTableTarget").objectReferenceValue = GameObject.Find("WallTheoryBoard")?.transform;
            vgcSo.FindProperty("proceedButtonTarget").objectReferenceValue = GameObject.Find("ObservationConsoleStation")?.transform.Find("ProceedButton");
            vgcSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(atom8);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase9VoiceGuidanceBuilder] Voice narration, subtitles, and Replay wired into scene.");
        }

        private static LabActionButton CreateReplayButton(Transform consoleParent)
        {
            Transform old = consoleParent.Find("ReplayVoiceButton");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject btnGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btnGo.name = "ReplayVoiceButton";
            btnGo.transform.SetParent(consoleParent, false);
            btnGo.transform.localPosition = new Vector3(0f, 0.025f, -0.105f);
            btnGo.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            btnGo.transform.localScale = new Vector3(0.14f, 0.020f, 0.045f);

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            lblGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            lblGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lblGo.transform.localScale = new Vector3(7.14f, 22.2f, 50f);

            TextMeshPro lblTmp = lblGo.AddComponent<TextMeshPro>();
            lblTmp.rectTransform.sizeDelta = new Vector2(0.13f, 0.04f);
            lblTmp.text = "REPLAY VOICE";
            lblTmp.fontSize = 0.055f;
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
            so.FindProperty("enabledColor").colorValue = new Color(0.0f, 0.72f, 0.88f);
            so.ApplyModifiedPropertiesWithoutUndo();

            return btn;
        }
    }
}
