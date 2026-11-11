using System.Collections.Generic;
using UnityEngine;

namespace PingPong
{
    /// <summary>
    /// Start menu, "How to play", settings, pause menu and game-over screen.
    /// While the menu is open the camera slowly circles the hall; PLAY flies it down to the game view.
    /// Drawn with Unity's built-in IMGUI, so no extra UI packages are needed.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PingPongMenu : MonoBehaviour
    {
        [Header("References (set by the scene builder)")]
        public PingPongGame game;
        public PingPongCrowd crowd;
        public Camera cam;

        [Header("Text")]
        public string title = "PING PONG";
        public string subtitle = "3D TABLE TENNIS";
        public string credits = "Made by Jakob Lah";
        public string version = "v1.0";

        [Header("Menu camera")]
        public Vector3 orbitCenter = new Vector3(0f, 0.8f, 0.6f);
        public Vector2 orbitRadius = new Vector2(3.1f, 4.8f);
        public float orbitHeight = 2.3f;
        [Tooltip("Degrees per second.")]
        public float orbitSpeed = 6f;
        public float transitionTime = 1.4f;

        enum Page { Main, HowTo, Settings, ToGame, Playing, Paused, PauseSettings, GameOver, ToMenu }

        static readonly string[] DifficultyNames = { "EASY", "MEDIUM", "HARD" };
        static readonly float[] DifficultyValues = { 0.25f, 0.55f, 0.85f };
        static readonly Color[] DifficultyColors =
        {
            new Color(0.45f, 0.9f, 0.45f), new Color(1f, 0.75f, 0.25f), new Color(1f, 0.38f, 0.32f)
        };

        static readonly Color Accent = new Color(1f, 0.55f, 0.12f);
        static readonly Color AccentHover = new Color(1f, 0.68f, 0.32f);
        static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.09f);
        static readonly Color ButtonHover = new Color(1f, 1f, 1f, 0.2f);
        static readonly Color PanelColor = new Color(0.05f, 0.07f, 0.11f, 0.92f);
        static readonly Color Muted = new Color(1f, 1f, 1f, 0.55f);

        Page page = Page.Main;
        int difficulty = 1;
        float masterVolume = 1f;
        float crowdVolume = 0.75f;

        Vector3 gamePos;
        Quaternion gameRot;
        float orbitAngle = 35f;
        Vector3 fromPos;
        Quaternion fromRot;
        float transT;
        float gameOverTimer;
        float fadeIn;
        float pageFade = 1f;
        Page lastPage;

        AudioSource uiAudio;
        AudioClip hoverClip, clickClip;

        Texture2D gradientTex;
        GUIStyle textStyle;
        readonly Dictionary<string, float> hoverAnim = new Dictionary<string, float>();
        string lastHovered;
        string activeSlider;
        float s;     // UI scale (1 at 1080p)
        float alpha; // alpha of what is being drawn right now

        // ================================================================== lifecycle

        void Start()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                gamePos = cam.transform.position;
                gameRot = cam.transform.rotation;
            }

            uiAudio = GetComponent<AudioSource>();
            uiAudio.playOnAwake = false;
            uiAudio.spatialBlend = 0f;
            uiAudio.ignoreListenerPause = true;
            hoverClip = MakeTone("ui_hover", 2200f, 0.025f, 0.18f);
            clickClip = MakeTone("ui_click", 1100f, 0.06f, 0.45f);

            difficulty = Mathf.Clamp(PlayerPrefs.GetInt("pp_difficulty", 1), 0, 2);
            masterVolume = PlayerPrefs.GetFloat("pp_master", 1f);
            crowdVolume = PlayerPrefs.GetFloat("pp_crowd", 0.75f);
            ApplyVolumes();

            PlaceOrbitCamera();
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fadeIn = Mathf.Min(1f, fadeIn + dt / 0.8f);
            if (page != lastPage)
            {
                lastPage = page;
                pageFade = 0f;
            }
            pageFade = Mathf.Min(1f, pageFade + dt / 0.25f);

            switch (page)
            {
                case Page.Main:
                case Page.HowTo:
                case Page.Settings:
                    orbitAngle += orbitSpeed * dt;
                    PlaceOrbitCamera();
                    if (PPInput.EscapePressed && page != Page.Main) Go(Page.Main);
                    break;

                case Page.ToGame:
                {
                    transT += dt / transitionTime;
                    float e = Smooth(transT);
                    cam.transform.position = Vector3.Lerp(fromPos, gamePos, e);
                    cam.transform.rotation = Quaternion.Slerp(fromRot, gameRot, e);
                    if (transT >= 1f)
                    {
                        page = Page.Playing;
                        game.StartMatch(DifficultyValues[difficulty]);
                    }
                    break;
                }

                case Page.ToMenu:
                {
                    orbitAngle += orbitSpeed * dt;
                    transT += dt / transitionTime;
                    Vector3 op;
                    Quaternion orr;
                    OrbitPose(orbitAngle, out op, out orr);
                    float e = Smooth(transT);
                    cam.transform.position = Vector3.Lerp(fromPos, op, e);
                    cam.transform.rotation = Quaternion.Slerp(fromRot, orr, e);
                    if (transT >= 1f) page = Page.Main;
                    break;
                }

                case Page.Playing:
                    if (PPInput.EscapePressed) SetPaused(true);
                    else if (game.IsGameOver)
                    {
                        gameOverTimer += dt;
                        if (gameOverTimer > 1.8f) page = Page.GameOver; // let the crowd react first
                    }
                    else gameOverTimer = 0f;
                    break;

                case Page.Paused:
                    if (PPInput.EscapePressed) SetPaused(false);
                    break;

                case Page.PauseSettings:
                    if (PPInput.EscapePressed) Go(Page.Paused);
                    break;
            }
        }

        // ================================================================== actions

        void Go(Page p)
        {
            page = p;
            activeSlider = null;
        }

        void Play()
        {
            SavePrefs();
            fromPos = cam.transform.position;
            fromRot = cam.transform.rotation;
            transT = 0f;
            gameOverTimer = 0f;
            Go(Page.ToGame);
            if (crowd != null) crowd.PlayIntro();
        }

        void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            game.Paused = paused;
            Go(paused ? Page.Paused : Page.Playing);
        }

        void Restart()
        {
            SetPaused(false);
            game.RestartMatch();
            gameOverTimer = 0f;
            if (crowd != null) crowd.PlayIntro();
        }

        void BackToMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            game.Paused = false;
            game.StopMatch();
            fromPos = cam.transform.position;
            fromRot = cam.transform.rotation;
            transT = 0f;
            gameOverTimer = 0f;
            OrbitAngleFacing(fromPos);
            Go(Page.ToMenu);
        }

        void Quit()
        {
            SavePrefs();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ApplyVolumes()
        {
            AudioListener.volume = masterVolume;
            if (crowd != null) crowd.volume = crowdVolume;
        }

        void SavePrefs()
        {
            PlayerPrefs.SetInt("pp_difficulty", difficulty);
            PlayerPrefs.SetFloat("pp_master", masterVolume);
            PlayerPrefs.SetFloat("pp_crowd", crowdVolume);
            PlayerPrefs.Save();
        }

        // ================================================================== camera

        void OrbitPose(float angleDeg, out Vector3 pos, out Quaternion rot)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            pos = new Vector3(
                orbitCenter.x + Mathf.Sin(a) * orbitRadius.x,
                orbitHeight + 0.25f * Mathf.Sin(a * 2f),
                orbitCenter.z - Mathf.Cos(a) * orbitRadius.y);
            rot = Quaternion.LookRotation(orbitCenter - pos);
        }

        /// <summary>Picks the orbit angle closest to where the camera is, so the return flight is short.</summary>
        void OrbitAngleFacing(Vector3 p)
        {
            float a = Mathf.Atan2((p.x - orbitCenter.x) / orbitRadius.x, -(p.z - orbitCenter.z) / orbitRadius.y) * Mathf.Rad2Deg;
            orbitAngle = a + 25f;
        }

        void PlaceOrbitCamera()
        {
            if (cam == null) return;
            Vector3 p;
            Quaternion r;
            OrbitPose(orbitAngle, out p, out r);
            cam.transform.position = p;
            cam.transform.rotation = r;
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        // ================================================================== drawing

        void OnGUI()
        {
            EnsureGui();
            GUI.depth = -10; // in front of the in-game HUD
            s = Screen.height / 1080f;
            float W = Screen.width, H = Screen.height;

            switch (page)
            {
                case Page.Main:
                case Page.HowTo:
                case Page.Settings:
                    DrawMainMenu(W, H, fadeIn);
                    break;
                case Page.ToGame:
                    DrawMainMenu(W, H, 1f - Smooth(transT * 1.8f));
                    break;
                case Page.ToMenu:
                    DrawMainMenu(W, H, Smooth(transT * 1.4f - 0.4f));
                    break;
                case Page.Paused:
                case Page.PauseSettings:
                    DrawPause(W, H);
                    break;
                case Page.GameOver:
                    DrawGameOver(W, H);
                    break;
            }
            GUI.color = Color.white;
        }

        void DrawMainMenu(float W, float H, float a)
        {
            if (a <= 0.001f) return;
            alpha = a;
            bool interactive = page == Page.Main || page == Page.HowTo || page == Page.Settings;

            // dark gradient on the left so text reads well over the moving 3D scene
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(0, 0, W * 0.62f, H), gradientTex);
            GUI.color = Color.white;
            Fill(new Rect(0, 0, W, 3f * s), Accent);

            float x0 = 110f * s;
            float top = H * 0.12f;
            DrawLogo(x0, top);

            float tx = x0 + 150f * s;
            Text(new Rect(tx, top - 10f * s, 900f * s, 130f * s), title, 108, Color.white, TextAnchor.MiddleLeft, true, true);
            Text(new Rect(tx + 4f * s, top + 112f * s, 900f * s, 34f * s), Spaced(subtitle), 22, Accent, TextAnchor.MiddleLeft, true, false);
            Fill(new Rect(tx + 4f * s, top + 152f * s, 90f * s, 4f * s), Accent);

            float bw = 420f * s, bh = 62f * s, gap = 14f * s;
            float y = H * 0.42f;
            float pa = interactive ? Smooth(pageFade) : 1f;

            if (page == Page.Main || !interactive)
            {
                alpha = a * pa;
                if (Button(new Rect(x0, y, bw, bh), "play", "PLAY", true, interactive)) Play();
                y += bh + gap;
                DifficultySelector(new Rect(x0, y, bw, bh), interactive);
                y += bh + gap;
                if (Button(new Rect(x0, y, bw, bh), "howto", "HOW TO PLAY", false, interactive)) Go(Page.HowTo);
                y += bh + gap;
                if (Button(new Rect(x0, y, bw, bh), "settings", "SETTINGS", false, interactive)) Go(Page.Settings);
                y += bh + gap;
                if (Button(new Rect(x0, y, bw, bh), "quit", "QUIT", false, interactive)) Quit();
            }
            else if (page == Page.HowTo)
            {
                alpha = a * pa;
                DrawHowTo(new Rect(x0, y, 620f * s, 470f * s));
            }
            else if (page == Page.Settings)
            {
                alpha = a * pa;
                DrawSettings(new Rect(x0, y, 520f * s, 380f * s), Page.Main);
            }

            alpha = a;
            Text(new Rect(x0, H - 60f * s, 800f * s, 30f * s), version + "   ·   " + credits, 18, Muted, TextAnchor.MiddleLeft, false, false);
        }

        void DrawLogo(float x, float y)
        {
            // a paddle with a bouncing ball
            float r = 52f * s;
            Vector2 c = new Vector2(x + r, y + r + 8f * s);
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(-28f, c);
            Rounded(new Rect(c.x - 11f * s, c.y + r * 0.7f, 22f * s, r * 1.1f), new Color(0.78f, 0.56f, 0.34f), 6f * s);
            Rounded(new Rect(c.x - r - 3f * s, c.y - r - 3f * s, (r + 3f * s) * 2f, (r + 3f * s) * 2f), new Color(0.08f, 0.08f, 0.09f), r + 3f * s);
            Rounded(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), new Color(0.86f, 0.1f, 0.1f), r);
            GUI.matrix = m;

            float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f));
            float br = 13f * s;
            Vector2 b = new Vector2(c.x + r * 1.05f, c.y - r * 0.9f - bounce * 34f * s);
            Rounded(new Rect(b.x - br, b.y - br, br * 2f, br * 2f), Accent, br);
        }

        void DifficultySelector(Rect r, bool interactive)
        {
            Rounded(r, ButtonColor, 10f * s);
            Text(new Rect(r.x + 24f * s, r.y, r.width, r.height), "DIFFICULTY", 24, Color.white, TextAnchor.MiddleLeft, true, false);

            float aw = 44f * s;
            Rect right = new Rect(r.xMax - aw - 10f * s, r.y + (r.height - aw) * 0.5f, aw, aw);
            Rect left = new Rect(right.x - 150f * s - aw, right.y, aw, aw);
            Rect val = new Rect(left.xMax, r.y, right.x - left.xMax, r.height);

            if (SmallButton(left, "diff_l", "<", interactive)) difficulty = (difficulty + 2) % 3;
            if (SmallButton(right, "diff_r", ">", interactive)) difficulty = (difficulty + 1) % 3;
            Text(val, DifficultyNames[difficulty], 24, DifficultyColors[difficulty], TextAnchor.MiddleCenter, true, false);
        }

        void DrawHowTo(Rect r)
        {
            Rounded(r, PanelColor, 14f * s);
            float x = r.x + 34f * s, w = r.width - 68f * s, y = r.y + 26f * s;
            Text(new Rect(x, y, w, 44f * s), "HOW TO PLAY", 32, Color.white, TextAnchor.MiddleLeft, true, false);
            y += 50f * s;
            Fill(new Rect(x, y, 60f * s, 3f * s), Accent);
            y += 20f * s;

            string[] lines =
            {
                "Move the <b>mouse</b> to move your paddle.",
                "<b>Click</b> or press <b>SPACE</b> to serve.",
                "Where the ball hits your paddle and how you swing decide where it goes.",
                "The ball must bounce once on your side before you can hit it.",
                "First to <b>11</b> points wins – you need a 2 point lead.",
            };
            foreach (string line in lines)
            {
                Rounded(new Rect(x, y + 11f * s, 8f * s, 8f * s), Accent, 4f * s);
                Text(new Rect(x + 22f * s, y, w - 22f * s, 52f * s), line, 21, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperLeft, false, false, true);
                y += 52f * s;
            }
            y += 6f * s;
            Text(new Rect(x, y, w, 30f * s), "1 / 2 / 3  difficulty     R  restart     ESC  pause", 18, Muted, TextAnchor.MiddleLeft, true, false);

            if (Button(new Rect(x, r.yMax - 82f * s, 200f * s, 54f * s), "howto_back", "BACK", false, true)) Go(Page.Main);
        }

        void DrawSettings(Rect r, Page backTo)
        {
            Rounded(r, PanelColor, 14f * s);
            float x = r.x + 34f * s, w = r.width - 68f * s, y = r.y + 26f * s;
            Text(new Rect(x, y, w, 44f * s), "SETTINGS", 32, Color.white, TextAnchor.MiddleLeft, true, false);
            y += 50f * s;
            Fill(new Rect(x, y, 60f * s, 3f * s), Accent);
            y += 30f * s;

            float before = masterVolume + crowdVolume;
            masterVolume = Slider(new Rect(x, y, w, 60f * s), "master", "MASTER VOLUME", masterVolume);
            y += 76f * s;
            crowdVolume = Slider(new Rect(x, y, w, 60f * s), "crowd", "CROWD VOLUME", crowdVolume);
            y += 80f * s;
            if (!Mathf.Approximately(before, masterVolume + crowdVolume)) ApplyVolumes();

            if (Button(new Rect(x, r.yMax - 82f * s, 200f * s, 54f * s), "settings_back", "BACK", false, true))
            {
                SavePrefs();
                Go(backTo);
            }
        }

        void DrawPause(float W, float H)
        {
            alpha = Smooth(pageFade * 1.5f);
            GUI.color = Color.white;
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));

            if (page == Page.PauseSettings)
            {
                DrawSettings(new Rect((W - 520f * s) * 0.5f, (H - 380f * s) * 0.5f, 520f * s, 380f * s), Page.Paused);
                return;
            }

            Rect p = new Rect((W - 480f * s) * 0.5f, (H - 470f * s) * 0.5f, 480f * s, 470f * s);
            Rounded(p, PanelColor, 16f * s);
            Text(new Rect(p.x, p.y + 30f * s, p.width, 54f * s), "PAUSED", 44, Color.white, TextAnchor.MiddleCenter, true, true);
            Fill(new Rect(p.center.x - 30f * s, p.y + 92f * s, 60f * s, 3f * s), Accent);

            float bw = 360f * s, bh = 58f * s, gap = 14f * s;
            float x = p.center.x - bw * 0.5f, y = p.y + 124f * s;
            if (Button(new Rect(x, y, bw, bh), "resume", "RESUME", true, true)) SetPaused(false);
            y += bh + gap;
            if (Button(new Rect(x, y, bw, bh), "restart", "RESTART MATCH", false, true)) Restart();
            y += bh + gap;
            if (Button(new Rect(x, y, bw, bh), "p_settings", "SETTINGS", false, true)) Go(Page.PauseSettings);
            y += bh + gap;
            if (Button(new Rect(x, y, bw, bh), "p_menu", "MAIN MENU", false, true)) BackToMenu();
        }

        void DrawGameOver(float W, float H)
        {
            alpha = Smooth(pageFade * 1.2f);
            GUI.color = Color.white;
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.55f));

            bool won = game.PlayerWon;
            Color headline = won ? new Color(1f, 0.82f, 0.25f) : new Color(1f, 0.42f, 0.38f);
            Rect p = new Rect((W - 520f * s) * 0.5f, (H - 480f * s) * 0.5f, 520f * s, 480f * s);
            Rounded(p, PanelColor, 16f * s);
            Fill(new Rect(p.x, p.y, p.width, 6f * s), headline);

            Text(new Rect(p.x, p.y + 34f * s, p.width, 70f * s), won ? "YOU WIN!" : "YOU LOSE", 58, headline, TextAnchor.MiddleCenter, true, true);
            Text(new Rect(p.x, p.y + 108f * s, p.width, 30f * s), "FINAL SCORE", 18, Muted, TextAnchor.MiddleCenter, true, false);
            Text(new Rect(p.x, p.y + 136f * s, p.width, 90f * s), game.PlayerScore + "  :  " + game.AIScore, 72, Color.white, TextAnchor.MiddleCenter, true, false);
            Text(new Rect(p.x, p.y + 226f * s, p.width, 30f * s), "YOU  vs  CPU   ·   " + game.DifficultyLabel.ToUpperInvariant(), 18, Muted, TextAnchor.MiddleCenter, true, false);

            float bw = 360f * s, bh = 58f * s, gap = 14f * s;
            float x = p.center.x - bw * 0.5f, y = p.y + 290f * s;
            if (Button(new Rect(x, y, bw, bh), "again", "PLAY AGAIN", true, true)) Restart();
            y += bh + gap;
            if (Button(new Rect(x, y, bw, bh), "go_menu", "MAIN MENU", false, true)) BackToMenu();
        }

        // ================================================================== widgets

        bool Button(Rect r, string id, string label, bool primary, bool enabled)
        {
            float h = Hover(r, id, enabled);
            Rect rr = Expand(r, h * 3f * s);
            Color baseC = primary ? Accent : ButtonColor;
            Color hovC = primary ? AccentHover : ButtonHover;
            Rounded(rr, Color.Lerp(baseC, hovC, h), 10f * s);
            if (!primary && h > 0.01f)
                Rounded(new Rect(rr.x, rr.y + 10f * s, 5f * s, rr.height - 20f * s), Accent * new Color(1f, 1f, 1f, h), 2.5f * s);
            Color tc = primary ? new Color(0.1f, 0.06f, 0.02f) : Color.white;
            Text(new Rect(rr.x + h * 8f * s, rr.y, rr.width, rr.height), label, 25, tc, TextAnchor.MiddleCenter, true, false);
            return Clicked(r, enabled);
        }

        bool SmallButton(Rect r, string id, string label, bool enabled)
        {
            float h = Hover(r, id, enabled);
            Rounded(r, Color.Lerp(new Color(1f, 1f, 1f, 0.08f), new Color(1f, 1f, 1f, 0.25f), h), 8f * s);
            Text(r, label, 24, Color.white, TextAnchor.MiddleCenter, true, false);
            return Clicked(r, enabled);
        }

        float Slider(Rect r, string id, string label, float value)
        {
            Text(new Rect(r.x, r.y, r.width, 26f * s), label, 19, Color.white, TextAnchor.MiddleLeft, true, false);
            Text(new Rect(r.x, r.y, r.width, 26f * s), Mathf.RoundToInt(value * 100f) + "%", 19, Accent, TextAnchor.MiddleRight, true, false);

            Rect track = new Rect(r.x, r.y + 40f * s, r.width, 8f * s);
            Rect hit = new Rect(track.x - 10f * s, track.y - 16f * s, track.width + 20f * s, track.height + 32f * s);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && hit.Contains(e.mousePosition))
            {
                activeSlider = id;
                value = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                PlayUi(clickClip, 0.5f);
                e.Use();
            }
            else if (activeSlider == id && e.type == EventType.MouseDrag)
            {
                value = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                e.Use();
            }
            else if (activeSlider == id && e.type == EventType.MouseUp)
            {
                activeSlider = null;
                e.Use();
            }

            float hov = Hover(hit, id, true);
            Rounded(track, new Color(1f, 1f, 1f, 0.15f), 4f * s);
            Rounded(new Rect(track.x, track.y, track.width * value, track.height), Accent, 4f * s);
            float kr = (11f + hov * 3f) * s;
            Vector2 k = new Vector2(track.x + track.width * value, track.center.y);
            Rounded(new Rect(k.x - kr, k.y - kr, kr * 2f, kr * 2f), Color.white, kr);
            return value;
        }

        float Hover(Rect r, string id, bool enabled)
        {
            Event e = Event.current;
            bool hover = enabled && r.Contains(e.mousePosition);
            float h;
            hoverAnim.TryGetValue(id, out h);
            if (e.type == EventType.Repaint)
            {
                h = Mathf.MoveTowards(h, hover ? 1f : 0f, Time.unscaledDeltaTime * 9f);
                hoverAnim[id] = h;
                if (hover && lastHovered != id)
                {
                    lastHovered = id;
                    PlayUi(hoverClip, 0.35f);
                }
                else if (!hover && lastHovered == id) lastHovered = null;
            }
            return h;
        }

        bool Clicked(Rect r, bool enabled)
        {
            Event e = Event.current;
            if (!enabled || alpha < 0.5f) return false;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                e.Use();
                PlayUi(clickClip, 0.7f);
                return true;
            }
            return false;
        }

        // ================================================================== drawing helpers

        void Rounded(Rect r, Color c, float radius)
        {
            c.a *= alpha;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, radius);
        }

        void Fill(Rect r, Color c)
        {
            c.a *= alpha;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        }

        void Text(Rect r, string t, int size, Color c, TextAnchor anchor, bool bold, bool shadow, bool wrap = false)
        {
            GUI.color = Color.white;
            textStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(size * s));
            textStyle.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            textStyle.alignment = anchor;
            textStyle.wordWrap = wrap;
            textStyle.richText = true;
            if (shadow)
            {
                textStyle.normal.textColor = new Color(0f, 0f, 0f, 0.55f * c.a * alpha);
                GUI.Label(new Rect(r.x + 3f * s, r.y + 3f * s, r.width, r.height), t, textStyle);
            }
            textStyle.normal.textColor = new Color(c.r, c.g, c.b, c.a * alpha);
            GUI.Label(r, t, textStyle);
        }

        static Rect Expand(Rect r, float d)
        {
            return new Rect(r.x - d, r.y - d, r.width + d * 2f, r.height + d * 2f);
        }

        static string Spaced(string t)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char ch in t)
            {
                sb.Append(ch);
                sb.Append(ch == ' ' ? "  " : " ");
            }
            return sb.ToString().TrimEnd();
        }

        void EnsureGui()
        {
            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label);
                textStyle.padding = new RectOffset(0, 0, 0, 0);
                textStyle.margin = new RectOffset(0, 0, 0, 0);
                textStyle.clipping = TextClipping.Overflow;
            }
            if (gradientTex == null)
            {
                gradientTex = new Texture2D(256, 1, TextureFormat.RGBA32, false);
                gradientTex.wrapMode = TextureWrapMode.Clamp;
                for (int i = 0; i < 256; i++)
                {
                    float t = i / 255f;
                    float a = 0.9f * (1f - t * t * (3f - 2f * t));
                    gradientTex.SetPixel(i, 0, new Color(0.02f, 0.03f, 0.06f, a));
                }
                gradientTex.Apply();
            }
        }

        // ================================================================== sounds

        void PlayUi(AudioClip clip, float volume)
        {
            if (uiAudio != null && clip != null) uiAudio.PlayOneShot(clip, volume);
        }

        static AudioClip MakeTone(string name, float freq, float duration, float volume)
        {
            const int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * duration));
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 60f) * Mathf.Clamp01(t * 2000f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * freq * t)) * env * volume;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
