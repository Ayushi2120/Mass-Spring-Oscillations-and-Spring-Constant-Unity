using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MassSpringLab.Robot
{
    /// <summary>
    /// Plays queued, non-overlapping English voice narration clips on ATOM-8,
    /// synchronizes world-space subtitles, prevents duplicate triggers for the same
    /// active state, and supports replaying the current instruction.
    /// </summary>
    public class Atom8VoiceNarrator : MonoBehaviour
    {
        [Serializable]
        public struct NarrationCue
        {
            public string id;
            [TextArea(2, 4)] public string subtitle;
            public AudioClip clip;
        }

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private TextMeshPro subtitleText;
        [SerializeField] private GameObject subtitleBackdrop;
        [SerializeField] private List<NarrationCue> cues = new List<NarrationCue>();

        private readonly Dictionary<string, NarrationCue> _cueMap = new Dictionary<string, NarrationCue>();
        private readonly Queue<(string cueId, Action onDone)> _queue = new Queue<(string, Action)>();

        private string _currentCueId;
        private string _lastPlayedStateKey;
        private Action _currentOnDone;
        private float _fallbackTimer;
        private bool _isPlayingCue;

        public string CurrentCueId => _currentCueId;
        public string CurrentSubtitle => subtitleText != null ? subtitleText.text : string.Empty;
        public bool IsSpeaking => _isPlayingCue;

        public int CueCount => _cueMap.Count;

        public bool HasValidClip(string cueId)
        {
            return _cueMap.TryGetValue(cueId, out NarrationCue c) && c.clip != null && c.clip.length > 0.1f;
        }

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.enabled = true;
            audioSource.mute = false;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0.0f;
            audioSource.volume = 1.0f;

            _cueMap.Clear();
            if (cues == null || cues.Count == 0)
            {
                PopulateDefaultCues();
            }

            foreach (NarrationCue c in cues)
            {
                if (!string.IsNullOrEmpty(c.id))
                {
                    NarrationCue loaded = c;
                    if (loaded.clip == null)
                    {
                        loaded.clip = Resources.Load<AudioClip>($"Narration/{loaded.id}");
                    }
                    _cueMap[loaded.id] = loaded;
                }
            }
            HideSubtitle();
        }

        public void ConfigureSubtitleUI(TextMeshPro text, GameObject backdrop)
        {
            subtitleText = text;
            subtitleBackdrop = backdrop;
            HideSubtitle();
        }

        private void PopulateDefaultCues()
        {
            cues = new List<NarrationCue>();
            var defs = new (string id, string sub)[]
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

            foreach (var d in defs)
            {
                cues.Add(new NarrationCue
                {
                    id = d.id,
                    subtitle = d.sub,
                    clip = Resources.Load<AudioClip>($"Narration/{d.id}")
                });
            }
        }

        private void Update()
        {
            if (!_isPlayingCue) return;

            bool clipFinished = audioSource != null && audioSource.clip != null
                ? !audioSource.isPlaying
                : (_fallbackTimer -= Time.deltaTime) <= 0f;

            if (clipFinished)
            {
                _isPlayingCue = false;
                Action done = _currentOnDone;
                _currentOnDone = null;
                done?.Invoke();

                if (_queue.Count > 0)
                {
                    var next = _queue.Dequeue();
                    StartCueImmediate(next.cueId, next.onDone);
                }
            }
        }

        public void PlayStateCue(string stateKey, string cueId, bool interruptQueue = true, Action onDone = null)
        {
            if (string.IsNullOrEmpty(cueId)) return;
            if (!string.IsNullOrEmpty(stateKey) && _lastPlayedStateKey == stateKey)
            {
                return;
            }

            _lastPlayedStateKey = stateKey;

            if (interruptQueue)
            {
                _queue.Clear();
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                _isPlayingCue = false;
                _currentOnDone = null;
                StartCueImmediate(cueId, onDone);
            }
            else if (_isPlayingCue)
            {
                _queue.Enqueue((cueId, onDone));
            }
            else
            {
                StartCueImmediate(cueId, onDone);
            }
        }

        public void EnqueueCue(string cueId, Action onDone = null)
        {
            if (string.IsNullOrEmpty(cueId)) return;
            if (_isPlayingCue)
            {
                _queue.Enqueue((cueId, onDone));
            }
            else
            {
                StartCueImmediate(cueId, onDone);
            }
        }

        public void ReplayCurrentInstruction()
        {
            if (string.IsNullOrEmpty(_currentCueId)) return;
            _queue.Clear();
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            _isPlayingCue = false;
            StartCueImmediate(_currentCueId, null);
        }

        public void HideSubtitle()
        {
            if (subtitleText != null) subtitleText.text = string.Empty;
            if (subtitleBackdrop != null) subtitleBackdrop.SetActive(false);
        }

        private void StartCueImmediate(string cueId, Action onDone)
        {
            if (!_cueMap.TryGetValue(cueId, out NarrationCue cue))
            {
                onDone?.Invoke();
                return;
            }

            _currentCueId = cueId;
            _currentOnDone = onDone;
            _isPlayingCue = true;

            if (subtitleBackdrop != null) subtitleBackdrop.SetActive(true);
            if (subtitleText != null) subtitleText.text = cue.subtitle;

            if (audioSource != null && cue.clip != null)
            {
                audioSource.enabled = true;
                audioSource.mute = false;
                audioSource.volume = 1.0f;
                audioSource.clip = cue.clip;
                audioSource.Play();
                Debug.Log($"[AudioDebug] Playing cue '{cueId}' clip='{cue.clip.name}' ({cue.clip.length:F2}s), isPlaying={audioSource.isPlaying}");
            }
            else
            {
                Debug.LogWarning($"[AudioDebug] Missing AudioSource or AudioClip for cue '{cueId}'");
                _fallbackTimer = Mathf.Clamp(cue.subtitle.Length * 0.055f, 1.5f, 5.0f);
            }
        }
    }
}
