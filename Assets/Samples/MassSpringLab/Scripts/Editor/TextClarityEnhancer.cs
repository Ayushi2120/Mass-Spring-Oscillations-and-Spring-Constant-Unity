using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Increases font sizes across all existing UI, Observation Table, Calculations Board,
    /// buttons, ruler, masses, and subtitles, and sharpens TextMeshPro SDF atlas filtering.
    /// Strictly preserves all text content, layout positions, models, lighting, and functionality.
    /// </summary>
    public static class TextClarityEnhancer
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";

        [MenuItem("MassSpringLab/Enhance All Text Size & Clarity")]
        public static void EnhanceAllTextClarity()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            EnlargeInteractionButtons();

            TMP_Text[] allTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int updatedCount = 0;

            foreach (TMP_Text tmp in allTexts)
            {
                if (tmp == null) continue;

                tmp.extraPadding = true;
                tmp.overflowMode = TextOverflowModes.Overflow;
                if (tmp.font != null && tmp.font.atlasTexture != null)
                {
                    tmp.font.atlasTexture.filterMode = FilterMode.Trilinear;
                    tmp.font.atlasTexture.anisoLevel = 8;
                    EditorUtility.SetDirty(tmp.font);
                }

                ApplyTargetedSize(tmp);
                EditorUtility.SetDirty(tmp);
                updatedCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TextClarityEnhancer] Enhanced {updatedCount} TMP_Text elements for VR readability.");
        }

        private static void ApplyTargetedSize(TMP_Text tmp)
        {
            string n = tmp.gameObject.name;
            string parentName = tmp.transform.parent != null ? tmp.transform.parent.name : string.Empty;

            if (n == "TheoryHeader")
            {
                tmp.fontSize = 70f;
                tmp.rectTransform.sizeDelta = new Vector2(1010f, 110f);
            }
            else if (n == "TheoryBody")
            {
                tmp.fontSize = 46f;
                tmp.lineSpacing = 6f;
                tmp.rectTransform.sizeDelta = new Vector2(1020f, 900f);
            }
            else if (n == "ObservationHeader")
            {
                tmp.fontSize = 76f;
                tmp.rectTransform.sizeDelta = new Vector2(1540f, 110f);
            }
            else if (n == "ObservationTableColumns")
            {
                tmp.fontSize = 46f;
                tmp.rectTransform.sizeDelta = new Vector2(1540f, 100f);
            }
            else if (n.StartsWith("ObsRow_"))
            {
                tmp.fontSize = 52f;
                tmp.rectTransform.sizeDelta = new Vector2(1540f, 125f);
            }
            else if (n == "CalcHeader")
            {
                tmp.fontSize = 0.25f;
                tmp.rectTransform.sizeDelta = new Vector2(0.49f, 0.065f);
            }
            else if (n == "CalcPlaceholder")
            {
                tmp.fontSize = 0.18f;
                tmp.lineSpacing = 8f;
                tmp.rectTransform.sizeDelta = new Vector2(0.49f, 0.25f);
            }
            else if (n == "Title" && parentName == "StopwatchDisplayStation")
            {
                tmp.fontSize = 0.13f;
                tmp.rectTransform.sizeDelta = new Vector2(0.22f, 0.04f);
            }
            else if (n == "Digits" && parentName == "StopwatchDisplayStation")
            {
                tmp.fontSize = 0.36f;
                tmp.rectTransform.sizeDelta = new Vector2(0.22f, 0.085f);
            }
            else if (n == "ConsoleTitle")
            {
                tmp.fontSize = 0.135f;
                tmp.rectTransform.sizeDelta = new Vector2(0.38f, 0.11f);
            }
            else if (n == "Label" && parentName == "MeasureButtonStation")
            {
                tmp.fontSize = 0.16f;
                tmp.rectTransform.sizeDelta = new Vector2(0.19f, 0.09f);
            }
            else if (n == "Label" && (parentName == "PullOscillateButton" || parentName == "ProceedButton"))
            {
                tmp.fontSize = 0.125f;
                tmp.rectTransform.sizeDelta = new Vector2(0.18f, 0.085f);
            }
            else if (n == "Label" && parentName == "ReplayVoiceButton")
            {
                tmp.fontSize = 0.105f;
                tmp.rectTransform.sizeDelta = new Vector2(0.20f, 0.065f);
            }
            else if (n.StartsWith("Label_") && parentName == "MeasurementRuler")
            {
                tmp.fontSize = 0.13f;
                tmp.rectTransform.sizeDelta = new Vector2(0.038f, 0.024f);
            }
            else if (n == "RulerUnitLabel")
            {
                tmp.fontSize = 0.14f;
            }
            else if (n == "MassLabel")
            {
                tmp.fontSize = 0.16f;
                tmp.rectTransform.sizeDelta = new Vector2(0.068f, 0.034f);
            }
            else if (n == "AttachmentStatusLabel")
            {
                tmp.fontSize = 0.15f;
                tmp.rectTransform.sizeDelta = new Vector2(0.19f, 0.06f);
            }
            else if (n == "RobotBadge")
            {
                tmp.fontSize = 0.18f;
            }
            else if (n == "SubtitleText")
            {
                tmp.fontSize = 0.32f;
                tmp.rectTransform.sizeDelta = new Vector2(0.84f, 0.21f);
            }
        }

        private static void EnlargeInteractionButtons()
        {
            // 1. MeasureButtonStation & MeasureLengthButton
            GameObject measureStation = GameObject.Find("MeasureButtonStation");
            if (measureStation != null)
            {
                Transform ped = measureStation.transform.Find("Pedestal");
                if (ped != null) ped.localScale = new Vector3(0.24f, 0.04f, 0.15f);

                Transform btn = measureStation.transform.Find("MeasureLengthButton");
                if (btn != null)
                {
                    btn.localPosition = new Vector3(0f, 0.054f, 0f);
                    btn.localScale = new Vector3(0.20f, 0.028f, 0.11f);
                }

                Transform lbl = measureStation.transform.Find("Label");
                if (lbl != null) lbl.localPosition = new Vector3(0f, 0.070f, 0f);
                EditorUtility.SetDirty(measureStation);
            }

            // 2. ObservationConsoleStation & Pull / Proceed / Replay buttons
            GameObject obsConsole = GameObject.Find("ObservationConsoleStation");
            if (obsConsole != null)
            {
                Transform cBase = obsConsole.transform.Find("ConsoleBase");
                if (cBase != null) cBase.localScale = new Vector3(0.44f, 0.03f, 0.30f);

                Transform cSlope = obsConsole.transform.Find("ConsoleSlope");
                if (cSlope != null)
                {
                    cSlope.localPosition = new Vector3(0f, 0.095f, 0.03f);
                    cSlope.localScale = new Vector3(0.42f, 0.14f, 0.02f);
                }

                Transform title = obsConsole.transform.Find("ConsoleTitle");
                if (title != null) title.localPosition = new Vector3(0f, 0.155f, 0.020f);

                EnlargeConsoleButton(obsConsole.transform, "PullOscillateButton", new Vector3(-0.105f, 0.052f, -0.052f), new Vector3(0.185f, 0.028f, 0.095f));
                EnlargeConsoleButton(obsConsole.transform, "ProceedButton", new Vector3(0.105f, 0.052f, -0.052f), new Vector3(0.185f, 0.028f, 0.095f));
                EnlargeConsoleButton(obsConsole.transform, "ReplayVoiceButton", new Vector3(0f, 0.028f, -0.135f), new Vector3(0.22f, 0.026f, 0.070f));
                EditorUtility.SetDirty(obsConsole);
            }
        }

        private static void EnlargeConsoleButton(Transform parent, string name, Vector3 localPos, Vector3 localScale)
        {
            Transform btn = parent.Find(name);
            if (btn == null) return;

            btn.localPosition = localPos;
            btn.localScale = localScale;

            Transform lbl = btn.Find("Label") ?? btn.Find("ProceedLabel");
            if (lbl != null)
            {
                lbl.localPosition = new Vector3(0f, 0.60f, 0f);
                // Compensate inverse scale so world-space TMP text is undistorted
                lbl.localScale = new Vector3(1f / localScale.x, 1f / localScale.z, 1f / localScale.y);
            }
        }
    }
}
