using UnityEngine;

namespace PingPong
{
    /// <summary>
    /// Arena effects: the animated LED boards around the court, the big video screen with the live
    /// score, and camera flashes in the crowd. The LED strip and the screen are each drawn by a small
    /// offscreen camera into a RenderTexture, which the boards and the screen then display.
    /// </summary>
    public class PingPongArena : MonoBehaviour
    {
        [Header("References (set by the scene builder)")]
        public PingPongGame game;
        public Camera mainCamera;

        [Header("LED boards")]
        public Camera ledCamera;
        public PingPongLedPanel[] ledPanels;
        public TextMesh[] ledItems;
        public Light[] ledSpill;
        [Tooltip("Length of the LED strip in rig units (one unit = the strip height).")]
        public float stripLength = 32f;
        public float scrollSpeed = 0.8f;
        public string[] idleTexts = { "PING PONG", "CUP 2026", "PING PONG", "FIRST TO 11", "PING PONG", "CUP 2026" };

        [Header("Video screen")]
        public Camera screenCamera;
        public Renderer screenFace;
        public Renderer screenBar;
        public TextMesh screenBarText;
        public TextMesh screenScore;
        public TextMesh screenInfo;
        public string screenTitle = "PING PONG CUP 2026  ·  FIRST TO 11";

        [Header("Camera flashes")]
        public Transform[] spectators;
        public Transform[] flashes;
        public float flashesPerSecond = 1.2f;

        static readonly Color Orange = new Color(1f, 0.5f, 0.08f);
        static readonly Color LedWhite = new Color(0.95f, 0.96f, 1f);
        static readonly Color LedDark = new Color(0.08f, 0.03f, 0f);
        static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
        static readonly Color YouRed = new Color(0.86f, 0.13f, 0.11f);
        static readonly Color CpuBlue = new Color(0.16f, 0.4f, 0.95f);

        enum Mode { Idle, Point, GameStart, GameOver }

        Mode mode = Mode.Idle;
        Mode appliedMode = (Mode)(-1);
        float modeTimer;
        Side lastWinner;
        float scroll;
        float[] baseX;
        RenderTexture ledTexture, screenTexture;

        int shownPlayer = -1, shownAi = -1, shownRally = -1, shownLongest = -1;
        float scorePop;
        bool wasActive;
        float flashBoost;
        float[] flashTimers;
        MaterialPropertyBlock barBlock;

        // ================================================================== setup

        void Awake()
        {
            ledTexture = MakeTexture("LED Strip", 4096, 128);
            screenTexture = MakeTexture("Video Screen", 1024, 320);

            if (ledCamera != null) ledCamera.targetTexture = ledTexture;
            if (screenCamera != null) screenCamera.targetTexture = screenTexture;
            if (ledPanels != null)
                foreach (PingPongLedPanel p in ledPanels)
                    if (p != null) p.Apply(ledTexture);
            if (screenFace != null)
            {
                var block = new MaterialPropertyBlock();
                screenFace.GetPropertyBlock(block);
                block.SetTexture("_MainTex", screenTexture);
                screenFace.SetPropertyBlock(block);
            }

            int n = ledItems != null ? ledItems.Length : 0;
            baseX = new float[n];
            for (int i = 0; i < n; i++) baseX[i] = stripLength * i / Mathf.Max(1, n);

            flashTimers = new float[flashes != null ? flashes.Length : 0];
            if (flashes != null)
                foreach (Transform f in flashes)
                    if (f != null) f.gameObject.SetActive(false);

            barBlock = new MaterialPropertyBlock();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        static RenderTexture MakeTexture(string name, int w, int h)
        {
            var rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
            rt.name = name;
            rt.useMipMap = true;
            rt.autoGenerateMips = true;
            rt.filterMode = FilterMode.Trilinear;
            rt.anisoLevel = 4;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.Create();
            return rt;
        }

        void OnEnable()
        {
            if (game != null) game.PointScored += OnPointScored;
        }

        void OnDisable()
        {
            if (game != null) game.PointScored -= OnPointScored;
        }

        void OnDestroy()
        {
            if (ledTexture != null) ledTexture.Release();
            if (screenTexture != null) screenTexture.Release();
        }

        void OnPointScored(Side winner, bool gameOver)
        {
            lastWinner = winner;
            mode = gameOver ? Mode.GameOver : Mode.Point;
            modeTimer = gameOver ? 999f : 2.4f;
            scorePop = 1f;
            flashBoost = gameOver ? 4f : 1.6f;
        }

        // ================================================================== frame update

        void Update()
        {
            float dt = Time.deltaTime;

            bool active = game != null && game.IsActive;
            if (active && !wasActive)
            {
                mode = Mode.GameStart;
                modeTimer = 1.8f;
            }
            if (!active && wasActive) mode = Mode.Idle;
            wasActive = active;

            if (mode == Mode.GameOver && game != null && !game.IsGameOver) mode = Mode.Idle; // restarted
            if (mode == Mode.Point || mode == Mode.GameStart)
            {
                modeTimer -= dt;
                if (modeTimer <= 0f) mode = Mode.Idle;
            }

            UpdateLed(dt);
            UpdateScreen(dt);
            UpdateFlashes(dt);
        }

        void UpdateLed(float dt)
        {
            if (ledItems == null || ledItems.Length == 0) return;

            if (mode != appliedMode)
            {
                appliedMode = mode;
                for (int i = 0; i < ledItems.Length; i++)
                {
                    string t;
                    switch (mode)
                    {
                        case Mode.Point: t = i % 2 == 0 ? "POINT" : (lastWinner == Side.Player ? "YOU" : "CPU"); break;
                        case Mode.GameStart: t = i % 2 == 0 ? "GAME ON" : "PING PONG"; break;
                        case Mode.GameOver: t = lastWinner == Side.Player ? "YOU WIN" : "CPU WINS"; break;
                        default: t = idleTexts[i % idleTexts.Length]; break;
                    }
                    ledItems[i].text = t;
                }
            }

            float speed = mode == Mode.GameOver ? scrollSpeed * 2f : scrollSpeed;
            scroll += speed * dt;

            // blinking orange background on a point, steady black otherwise
            bool blinkOn = mode == Mode.Point && Mathf.Repeat(modeTimer, 0.5f) > 0.17f;
            bool whiteFlash = mode == Mode.GameStart && Mathf.Repeat(modeTimer, 0.6f) > 0.3f;
            Color bg = blinkOn ? Orange : (whiteFlash ? new Color(0.9f, 0.92f, 1f) : Color.black);
            if (ledCamera != null) ledCamera.backgroundColor = bg;

            for (int i = 0; i < ledItems.Length; i++)
            {
                TextMesh item = ledItems[i];
                Vector3 p = item.transform.localPosition;
                p.x = Mathf.Repeat(baseX[i] - scroll, stripLength);
                item.transform.localPosition = p;

                Color c;
                if (blinkOn || whiteFlash) c = LedDark;
                else if (mode == Mode.Point) c = Orange;
                else if (mode == Mode.GameOver) c = lastWinner == Side.Player ? Gold : LedWhite;
                else c = i % 2 == 0 ? Orange : LedWhite;
                item.color = c;
            }

            if (ledSpill != null)
            {
                float target = blinkOn ? 1.6f : (whiteFlash ? 1.2f : 0.35f);
                Color spill = whiteFlash ? new Color(0.85f, 0.9f, 1f) : Orange;
                foreach (Light l in ledSpill)
                {
                    if (l == null) continue;
                    l.intensity = Mathf.MoveTowards(l.intensity, target, dt * 8f);
                    l.color = spill;
                }
            }
        }

        void UpdateScreen(float dt)
        {
            if (game == null) return;

            if (screenScore != null && (game.PlayerScore != shownPlayer || game.AIScore != shownAi))
            {
                shownPlayer = game.PlayerScore;
                shownAi = game.AIScore;
                screenScore.text = shownPlayer + " : " + shownAi;
            }

            int rally = game.RallyHits;
            if (screenInfo != null && (rally != shownRally || game.LongestRally != shownLongest))
            {
                shownRally = rally;
                shownLongest = game.LongestRally;
                screenInfo.text = "RALLY " + shownRally + "   ·   LONGEST " + shownLongest;
            }

            if (scorePop > 0f) scorePop = Mathf.Max(0f, scorePop - dt * 2.5f);
            if (screenScore != null) screenScore.transform.localScale = Vector3.one * (1f + 0.22f * Mathf.Sin(scorePop * Mathf.PI));

            Color bar = Orange;
            string text = screenTitle;
            Color textColor = LedDark;
            switch (mode)
            {
                case Mode.Point:
                    bar = lastWinner == Side.Player ? YouRed : CpuBlue;
                    text = lastWinner == Side.Player ? "POINT  ·  YOU" : "POINT  ·  CPU";
                    textColor = Color.white;
                    break;
                case Mode.GameStart:
                    bar = new Color(0.92f, 0.94f, 1f);
                    text = "GAME ON";
                    break;
                case Mode.GameOver:
                    bar = lastWinner == Side.Player ? Gold : new Color(0.55f, 0.58f, 0.65f);
                    text = lastWinner == Side.Player ? "YOU WIN!" : "CPU WINS";
                    break;
            }
            if (screenBar != null)
            {
                if (barBlock == null) barBlock = new MaterialPropertyBlock(); // also after a script reload in Play mode
                screenBar.GetPropertyBlock(barBlock);
                barBlock.SetColor("_Color", bar);
                screenBar.SetPropertyBlock(barBlock);
            }
            if (screenBarText != null)
            {
                if (screenBarText.text != text) screenBarText.text = text;
                screenBarText.color = textColor;
            }
        }

        void UpdateFlashes(float dt)
        {
            if (flashes == null || flashes.Length == 0 || spectators == null || spectators.Length == 0) return;
            if (mainCamera == null) mainCamera = Camera.main;

            flashBoost = Mathf.Max(0f, flashBoost - dt);
            float rate = flashesPerSecond * (1f + flashBoost * 3f);

            for (int i = 0; i < flashes.Length; i++)
            {
                Transform f = flashes[i];
                if (f == null) continue;
                if (flashTimers[i] > 0f)
                {
                    flashTimers[i] -= dt;
                    if (mainCamera != null) f.rotation = Quaternion.LookRotation(f.position - mainCamera.transform.position);
                    if (flashTimers[i] <= 0f) f.gameObject.SetActive(false);
                }
                else if (Random.value < rate * dt / flashes.Length)
                {
                    Transform who = spectators[Random.Range(0, spectators.Length)];
                    if (who == null) continue;
                    f.position = who.position + Vector3.up * Random.Range(0.75f, 1.05f) + who.right * Random.Range(-0.15f, 0.15f);
                    f.gameObject.SetActive(true);
                    flashTimers[i] = Random.Range(0.05f, 0.09f);
                }
            }
        }
    }
}
