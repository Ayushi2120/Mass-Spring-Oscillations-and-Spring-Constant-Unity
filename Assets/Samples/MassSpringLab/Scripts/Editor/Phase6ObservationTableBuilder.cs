using MassSpringLab.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MassSpringLab.Editor
{
    /// <summary>
    /// Upgrades ONLY the existing Observation Table UI on WallTheoryBoard/TheoryCanvas
    /// for high VR legibility (larger fonts, generous row spacing, clean 4-column layout).
    /// Does not move the whiteboard or modify grabbing, attachment, physics, or measurement.
    /// </summary>
    public static class Phase6ObservationTableBuilder
    {
        private const string ScenePath = "Assets/Samples/MassSpringLab/Scenes/MassSpringLab.unity";

        [MenuItem("MassSpringLab/Upgrade Observation Table UI")]
        public static void UpgradeObservationTableUI()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject theoryCanvas = GameObject.Find("WallTheoryBoard/TheoryCanvas");
            if (theoryCanvas == null)
            {
                Debug.LogError("[Phase6ObservationTableBuilder] WallTheoryBoard/TheoryCanvas not found!");
                return;
            }

            const float centerX = 555f;
            const float width = 1500f;

            // Header
            TextMeshProUGUI header = theoryCanvas.transform.Find("ObservationHeader")?.GetComponent<TextMeshProUGUI>();
            if (header != null)
            {
                RectTransform r = header.rectTransform;
                r.anchoredPosition = new Vector2(centerX, 475f);
                r.sizeDelta = new Vector2(width, 100f);
                header.text = "OBSERVATION TABLE";
                header.fontSize = 68f;
                header.fontStyle = FontStyles.Bold;
                header.color = new Color(0.46f, 1.0f, 0.01f);
                header.alignment = TextAlignmentOptions.Left;
            }

            // Column Headers (4 clean, well-spaced columns)
            TextMeshProUGUI cols = theoryCanvas.transform.Find("ObservationTableColumns")?.GetComponent<TextMeshProUGUI>();
            if (cols != null)
            {
                RectTransform r = cols.rectTransform;
                r.anchoredPosition = new Vector2(centerX, 345f);
                r.sizeDelta = new Vector2(width, 95f);
                cols.text = "Mass<pos=24%>L0 (cm)<pos=52%>L (cm)<pos=79%>x (cm)";
                cols.fontSize = 46f;
                cols.fontStyle = FontStyles.Bold;
                cols.color = new Color(0.0f, 0.90f, 0.92f);
                cols.alignment = TextAlignmentOptions.Left;
                cols.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // 5 Mass Rows (50g - 250g)
            int[] masses = { 50, 100, 150, 200, 250 };
            for (int i = 0; i < masses.Length; i++)
            {
                int m = masses[i];
                TextMeshProUGUI row = theoryCanvas.transform.Find($"ObsRow_{m}g")?.GetComponent<TextMeshProUGUI>();
                if (row != null)
                {
                    RectTransform r = row.rectTransform;
                    r.anchoredPosition = new Vector2(centerX, 210f - i * 145f);
                    r.sizeDelta = new Vector2(width, 115f);
                    row.text = ObservationTableUI.FormatRow(m, "—", "—", "—");
                    row.fontSize = 52f;
                    row.fontStyle = FontStyles.Normal;
                    row.color = Color.white;
                    row.alignment = TextAlignmentOptions.Left;
                    row.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }

            ObservationTableUI tableUI = theoryCanvas.GetComponent<ObservationTableUI>();
            if (tableUI == null) tableUI = theoryCanvas.AddComponent<ObservationTableUI>();

            EditorUtility.SetDirty(theoryCanvas);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase6ObservationTableBuilder] Observation Table UI upgraded and saved.");
        }
    }
}
