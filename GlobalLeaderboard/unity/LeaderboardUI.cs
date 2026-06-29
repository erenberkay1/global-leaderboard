using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GlobalLeaderboard
{
    /// <summary>
    /// Ready-to-use on-screen leaderboard panel. Builds its entire UI from code,
    /// using TextMeshPro so the text stays crisp at any resolution.
    ///
    /// FIRST-TIME SETUP: TextMeshPro needs its essential resources. The first time
    /// you press Play, Unity may show a popup "Import TMP Essentials" -> click Import.
    /// (Or do it manually: Window > TextMeshPro > Import TMP Essential Resources.)
    ///
    /// USAGE: Add this script to an empty GameObject (LeaderboardClient is added
    /// automatically). Fill in Base Url / Api Key / Hmac Secret on LeaderboardClient,
    /// then press Play. Remove the LeaderboardDemo script from the same object so the
    /// score is not submitted twice.
    /// </summary>
    [RequireComponent(typeof(LeaderboardClient))]
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Player name for this device (use your real player name in a real game).")]
        public string playerName = "Player";

        [Header("Appearance")]
        [Tooltip("How many scores to display.")]
        public int topCount = 10;
        public Color panelColor = new Color(0.10f, 0.11f, 0.15f, 0.96f);
        public Color accentColor = new Color(0.27f, 0.55f, 1f, 1f);
        public Color textColor = new Color(0.92f, 0.93f, 0.96f, 1f);
        public Color highlightColor = new Color(1f, 0.78f, 0.25f, 1f);

        private LeaderboardClient _client;
        private string _playerId;

        private Transform _listContainer;
        private TextMeshProUGUI _statusText;

        private void Awake()
        {
            _client = GetComponent<LeaderboardClient>();
            _playerId = SystemInfo.deviceUniqueIdentifier;
            EnsureEventSystem();
            BuildUI();
        }

        private void Start()
        {
            RefreshAll();
        }

        // ------------------------------------------------------------------
        //  MAIN ACTIONS
        // ------------------------------------------------------------------

        private void RefreshAll()
        {
            SetStatus("Loading...");
            StartCoroutine(_client.GetTopScores(topCount, OnScoresLoaded, OnError));
        }

        private void SubmitRandomScore()
        {
            long score = Random.Range(100, 9999);
            SetStatus("Submitting: " + score + " ...");
            StartCoroutine(_client.SubmitScore(_playerId, playerName, score,
                resp =>
                {
                    SetStatus($"Your score: {score}  |  Rank: #{resp.rank}");
                    RefreshAll();
                },
                OnError));
        }

        private void OnScoresLoaded(ScoreView[] scores)
        {
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_listContainer.GetChild(i).gameObject);
            }

            if (scores == null || scores.Length == 0)
            {
                SetStatus("No scores yet. Tap 'Submit Score'.");
                return;
            }

            foreach (var s in scores)
            {
                bool isMe = s.playerId == _playerId;
                CreateRow(s.rank.ToString(), s.playerName, s.score.ToString(), isMe);
            }

            SetStatus("Showing top " + scores.Length + " scores.");
        }

        private void OnError(string err)
        {
            SetStatus("Error: " + err);
            Debug.LogError("[LeaderboardUI] " + err);
        }

        private void SetStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        // ------------------------------------------------------------------
        //  UI CONSTRUCTION
        // ------------------------------------------------------------------

        private void BuildUI()
        {
            var canvasGo = new GameObject("LeaderboardCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = CreateUIObject("Panel", canvasGo.transform);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(460, 620);
            panelRt.anchoredPosition = Vector2.zero;
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = panelColor;

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(18, 18, 18, 18);
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var title = CreateText(panel.transform, "LEADERBOARD", 34, FontStyles.Bold, TextAlignmentOptions.Center, accentColor);
            SetHeight(title.gameObject, 46);

            var header = CreateUIObject("Header", panel.transform);
            SetHeight(header, 28);
            AddRowLayout(header);
            CreateCell(header.transform, "#", 20, FontStyles.Bold, TextAlignmentOptions.Left, accentColor, 0.18f);
            CreateCell(header.transform, "PLAYER", 20, FontStyles.Bold, TextAlignmentOptions.Left, accentColor, 0.52f);
            CreateCell(header.transform, "SCORE", 20, FontStyles.Bold, TextAlignmentOptions.Right, accentColor, 0.30f);

            var list = CreateUIObject("List", panel.transform);
            var listVlg = list.AddComponent<VerticalLayoutGroup>();
            listVlg.spacing = 4;
            listVlg.childControlWidth = true;
            listVlg.childControlHeight = false;
            listVlg.childForceExpandWidth = true;
            listVlg.childForceExpandHeight = false;
            var listLe = list.AddComponent<LayoutElement>();
            listLe.flexibleHeight = 1;
            listLe.minHeight = 360;
            _listContainer = list.transform;

            _statusText = CreateText(panel.transform, "Loading...", 20, FontStyles.Normal, TextAlignmentOptions.Left, textColor);
            SetHeight(_statusText.gameObject, 30);

            var buttons = CreateUIObject("Buttons", panel.transform);
            SetHeight(buttons, 56);
            AddRowLayout(buttons, 10);
            CreateButton(buttons.transform, "Refresh", accentColor, RefreshAll);
            CreateButton(buttons.transform, "Submit Score", new Color(0.20f, 0.65f, 0.36f, 1f), SubmitRandomScore);
        }

        private void CreateRow(string rank, string name, string score, bool highlight)
        {
            var row = CreateUIObject("Row", _listContainer);
            SetHeight(row, 38);
            var bg = row.AddComponent<Image>();
            bg.color = highlight ? new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.18f)
                                 : new Color(1f, 1f, 1f, 0.04f);
            AddRowLayout(row, 4, 8);

            Color c = highlight ? highlightColor : textColor;
            CreateCell(row.transform, "#" + rank, 20, FontStyles.Bold, TextAlignmentOptions.Left, c, 0.18f);
            CreateCell(row.transform, name, 20, FontStyles.Normal, TextAlignmentOptions.Left, c, 0.52f);
            CreateCell(row.transform, score, 20, FontStyles.Bold, TextAlignmentOptions.Right, c, 0.30f);
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private TextMeshProUGUI CreateText(Transform parent, string content, float size, FontStyles style,
            TextAlignmentOptions anchor, Color color)
        {
            var go = CreateUIObject("Text", parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private void CreateCell(Transform parent, string content, float size, FontStyles style,
            TextAlignmentOptions anchor, Color color, float widthWeight)
        {
            var t = CreateText(parent, content, size, style, anchor, color);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = widthWeight;
        }

        private void CreateButton(Transform parent, string label, Color color, System.Action onClick)
        {
            var go = CreateUIObject("Button_" + label, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            var label2 = CreateText(go.transform, label, 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            var lrt = label2.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
        }

        private void AddRowLayout(GameObject go, float spacing = 6, int sidePadding = 4)
        {
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.padding = new RectOffset(sidePadding, sidePadding, 0, 0);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
        }

        private void SetHeight(GameObject go, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        // Create an EventSystem if none exists (required for buttons to work)
        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
