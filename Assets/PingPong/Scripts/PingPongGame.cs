using UnityEngine;

namespace PingPong
{
    /// <summary>
    /// Game rules, serving, scoring, difficulty and the on-screen UI.
    /// Simplifications: the serve goes straight to the opponent's side (no bounce on your own side),
    /// and volleys are not possible – a paddle can only hit the ball after it bounced on its side.
    /// </summary>
    public class PingPongGame : MonoBehaviour
    {
        [Header("References (set by the scene builder)")]
        public PingPongBall ball;
        public PingPongPaddle playerPaddle;
        public PingPongPaddle aiPaddle;

        [Header("Rules")]
        public int pointsToWin = 11;

        [Header("Shot speed (m/s)")]
        public float baseSpeed = 4.0f;
        public float speedPerHit = 0.12f;
        public float maxSpeed = 7.5f;

        [Header("Computer")]
        [Range(0f, 1f)] public float aiDifficulty = 0.55f;

        enum State { WaitingServe, Rally, PointOver, GameOver }

        State state;
        readonly int[] score = new int[2];
        Side server = Side.Player;
        Side lastHitter = Side.Player;
        bool receiverBounced;
        int rallyHits;
        float stateTimer;
        float timeSinceHit;
        string message = "";
        float messageTimer;

        GUIStyle scoreStyle, textStyle, smallStyle;

        void Start()
        {
            ball.TableBounced += OnTableBounced;
            ball.Dead += OnBallDead;
            ApplyDifficulty();
            ResetGame();
        }

        void OnDestroy()
        {
            if (ball == null) return;
            ball.TableBounced -= OnTableBounced;
            ball.Dead -= OnBallDead;
        }

        // ------------------------------------------------------------------ game state

        void ResetGame()
        {
            score[0] = 0;
            score[1] = 0;
            server = Side.Player;
            ShowMessage("First to " + pointsToWin + " points (win by 2)!", 2.5f);
            PrepareServe();
        }

        void PrepareServe()
        {
            state = State.WaitingServe;
            stateTimer = 1.0f;
            ball.Frozen = true;
            ball.Velocity = Vector3.zero;
            ball.ResetFlags();
            HoldBall();
        }

        void HoldBall()
        {
            PingPongPaddle pad = PaddleOf(server);
            ball.transform.position = pad.transform.position + new Vector3(0f, 0.12f, pad.Facing * 0.06f);
        }

        void Serve()
        {
            PingPongPaddle pad = PaddleOf(server);
            Side receiver = Table.Other(server);
            Vector3 from = ball.transform.position;
            if (from.y < Table.Height + 0.12f) from.y = Table.Height + 0.12f;

            float maxX = Table.HalfWidth - 0.15f;
            float tx = server == Side.Player
                ? Mathf.Clamp(pad.transform.position.x * 0.5f + Random.Range(-0.2f, 0.2f), -maxX, maxX)
                : Random.Range(-maxX, maxX);
            float tz = Table.Dir(receiver) * Random.Range(0.6f, 1.1f);

            ball.ResetFlags();
            ball.transform.position = from;
            ball.Velocity = ball.SolveVelocity(from, new Vector3(tx, 0f, tz), baseSpeed * 0.9f);
            ball.Frozen = false;
            ball.PlayServeSound();

            lastHitter = server;
            receiverBounced = false;
            rallyHits = 0;
            timeSinceHit = 0f;
            state = State.Rally;
            aiPaddle.NewAimError(AIAimError());
        }

        void Update()
        {
            int d = PPInput.DifficultyKey();
            if (d >= 0)
            {
                aiDifficulty = d == 0 ? 0.25f : d == 1 ? 0.55f : 0.85f;
                ApplyDifficulty();
                ShowMessage("Difficulty: " + DifficultyName(), 1.5f);
            }
            if (PPInput.RestartPressed)
            {
                ResetGame();
                return;
            }

            if (messageTimer > 0f) messageTimer -= Time.deltaTime;

            switch (state)
            {
                case State.WaitingServe:
                    HoldBall();
                    if (server == Side.Player)
                    {
                        if (PPInput.ActionPressed) Serve();
                    }
                    else
                    {
                        stateTimer -= Time.deltaTime;
                        if (stateTimer <= 0f) Serve();
                    }
                    break;

                case State.Rally:
                    timeSinceHit += Time.deltaTime;
                    if (timeSinceHit > 6f) OnBallDead(); // safety net if the ball ever gets stuck
                    break;

                case State.PointOver:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) PrepareServe();
                    break;

                case State.GameOver:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f && PPInput.ActionPressed) ResetGame();
                    break;
            }
        }

        // ------------------------------------------------------------------ rules

        public bool CanPaddleHit(PingPongPaddle pad)
        {
            return state == State.Rally && pad.side != lastHitter && receiverBounced;
        }

        public bool ShouldAITrackBall(PingPongPaddle pad)
        {
            return IsBallIncoming(pad) && timeSinceHit >= pad.aiReactionTime;
        }

        /// <summary>True when the ball is flying towards this paddle and it has to return it.</summary>
        public bool IsBallIncoming(PingPongPaddle pad)
        {
            if (state != State.Rally || lastHitter == pad.side || ball.Frozen) return false;
            return ball.Velocity.z * Table.Dir(pad.side) > 0f;
        }

        public int PlayerScore { get { return score[0]; } }
        public int AIScore { get { return score[1]; } }
        public bool PlayerServing { get { return server == Side.Player; } }

        public bool IsServing(Side s)
        {
            return state == State.WaitingServe && server == s;
        }

        public void HandlePaddleHit(PingPongPaddle pad, Vector3 contact)
        {
            lastHitter = pad.side;
            receiverBounced = false;
            rallyHits++;
            timeSinceHit = 0f;

            Vector3 from = contact;
            if (from.y < Table.Height + 0.06f) from.y = Table.Height + 0.06f;
            ball.transform.position = from;

            Vector3 target;
            float speed;
            if (pad.controlledByAI) AIShot(out target, out speed);
            else PlayerShot(pad, from, out target, out speed);

            ball.Velocity = ball.SolveVelocity(from, target, speed);

            if (!pad.controlledByAI) aiPaddle.NewAimError(AIAimError());
        }

        void PlayerShot(PingPongPaddle pad, Vector3 from, out Vector3 target, out float speed)
        {
            // Direction: where on the hit area you catch the ball (edge = angle) + sideways swing.
            float aim = Mathf.Clamp((from.x - pad.transform.position.x) / Mathf.Max(0.01f, pad.hitSize.x), -1f, 1f);
            float tx = from.x * 0.2f + aim * 0.5f + Mathf.Clamp(pad.Velocity.x, -4f, 4f) * 0.08f;
            float limit = Table.HalfWidth - 0.07f;
            tx = Mathf.Clamp(tx, -limit, limit);

            // Depth: swinging upwards sends the ball deeper.
            float depth = Mathf.Clamp(0.8f + Mathf.Clamp(pad.Velocity.y, -3f, 3f) * 0.07f + Random.Range(-0.15f, 0.15f), 0.4f, 1.22f);
            target = new Vector3(tx, 0f, Table.Dir(Side.AI) * depth);

            float swing = Mathf.Clamp(Mathf.Abs(pad.Velocity.x) * 0.35f, 0f, 2f);
            speed = Mathf.Min(maxSpeed, baseSpeed + rallyHits * speedPerHit + swing);
        }

        void AIShot(out Vector3 target, out float speed)
        {
            float d = aiDifficulty;
            float maxX = Table.HalfWidth - 0.12f;
            float tx = Random.Range(-maxX, maxX);

            // A stronger computer aims away from your paddle more often.
            if (Random.value < d * 0.7f)
            {
                float px = playerPaddle.transform.position.x;
                tx = (px > 0f ? -1f : 1f) * Random.Range(0.3f, maxX);
            }
            float depth = Random.Range(0.45f, 1.2f);

            // Mistakes: a weaker computer misses the table more often.
            if (Random.value < Mathf.Lerp(0.16f, 0.03f, d))
            {
                if (Random.value < 0.5f) depth = Table.HalfLength + Random.Range(0.08f, 0.3f);
                else tx = Mathf.Sign(tx) * (Table.HalfWidth + Random.Range(0.05f, 0.25f));
            }

            target = new Vector3(tx, 0f, Table.Dir(Side.Player) * depth);
            speed = Mathf.Min(maxSpeed, baseSpeed + rallyHits * speedPerHit + d * 1.2f + Random.Range(-0.3f, 0.5f));
        }

        void OnTableBounced(Side side)
        {
            if (state != State.Rally) return;

            if (side == lastHitter)
            {
                AwardPoint(Table.Other(lastHitter), ball.NetTouched ? "net" : "bounced on own side");
            }
            else if (receiverBounced)
            {
                AwardPoint(lastHitter, "double bounce");
            }
            else
            {
                receiverBounced = true;
            }
        }

        void OnBallDead()
        {
            if (state != State.Rally) return;
            if (receiverBounced) AwardPoint(lastHitter, "not returned");
            else AwardPoint(Table.Other(lastHitter), ball.NetTouched ? "net" : "out");
        }

        void AwardPoint(Side winner, string reason)
        {
            score[(int)winner]++;
            int p = score[0], a = score[1];

            if ((p >= pointsToWin || a >= pointsToWin) && Mathf.Abs(p - a) >= 2)
            {
                state = State.GameOver;
                stateTimer = 1.0f;
                message = "";
                return;
            }

            ShowMessage((winner == Side.Player ? "Your point!" : "CPU's point") + "  (" + reason + ")", 1.6f);
            state = State.PointOver;
            stateTimer = 1.4f;

            // Serve changes every 2 points, and every point at 10:10 (deuce).
            int total = p + a;
            bool deuce = p >= pointsToWin - 1 && a >= pointsToWin - 1;
            int block = deuce ? total : total / 2;
            server = block % 2 == 0 ? Side.Player : Side.AI;
        }

        // ------------------------------------------------------------------ helpers

        PingPongPaddle PaddleOf(Side s)
        {
            return s == Side.Player ? playerPaddle : aiPaddle;
        }

        float AIAimError()
        {
            return Mathf.Lerp(0.2f, 0.04f, aiDifficulty);
        }

        void ApplyDifficulty()
        {
            if (aiPaddle == null) return;
            aiPaddle.aiMaxSpeed = Mathf.Lerp(1.4f, 3.2f, aiDifficulty);
            aiPaddle.aiReactionTime = Mathf.Lerp(0.3f, 0.06f, aiDifficulty);
        }

        string DifficultyName()
        {
            if (aiDifficulty < 0.4f) return "easy";
            if (aiDifficulty < 0.7f) return "medium";
            return "hard";
        }

        void ShowMessage(string text, float seconds)
        {
            message = text;
            messageTimer = seconds;
        }

        // ------------------------------------------------------------------ UI

        void OnGUI()
        {
            float s = Screen.height / 1080f;
            if (scoreStyle == null)
            {
                scoreStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            }
            textStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(38 * s));
            smallStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(22 * s));

            float w = Screen.width;

            // Compact score in the top-left corner (the big score is on the scoreboard next to the table).
            scoreStyle.wordWrap = false;
            textStyle.wordWrap = false;
            smallStyle.wordWrap = false;
            scoreStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(46 * s));
            string scoreText = "YOU  " + score[0] + " : " + score[1] + "  CPU";
            string serveText = server == Side.Player ? "Serve: you" : "Serve: CPU";
            string diffText = "Difficulty: " + DifficultyName();
            Vector2 scoreSize = scoreStyle.CalcSize(new GUIContent(scoreText));
            Vector2 lineSize = smallStyle.CalcSize(new GUIContent(diffText));
            float panelW = Mathf.Max(330 * s, Mathf.Max(scoreSize.x, lineSize.x) + 24f);
            float y0 = 14 * s;
            Rect panel = new Rect(14 * s, y0, panelW, scoreSize.y + lineSize.y * 2f + 16f);
            Color oldBg = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = oldBg;
            Label(new Rect(panel.x, panel.y + 6f, panel.width, scoreSize.y), scoreText, scoreStyle, Color.white);
            Label(new Rect(panel.x, panel.y + 6f + scoreSize.y, panel.width, lineSize.y), serveText, smallStyle, new Color(1f, 1f, 1f, 0.9f));
            Label(new Rect(panel.x, panel.y + 6f + scoreSize.y + lineSize.y, panel.width, lineSize.y), diffText, smallStyle, new Color(1f, 1f, 1f, 0.75f));
            scoreStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(64 * s));

            if (state == State.GameOver)
            {
                bool won = score[0] > score[1];
                Label(new Rect(0, Screen.height * 0.38f, w, 90 * s), won ? "YOU WIN!" : "YOU LOSE", scoreStyle,
                    won ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.4f, 0.4f));
                Label(new Rect(0, Screen.height * 0.38f + 85 * s, w, 50 * s), "Click or press Space for a new game", textStyle, Color.white);
            }
            else if (state == State.WaitingServe && server == Side.Player)
            {
                Label(new Rect(0, Screen.height * 0.42f, w, 50 * s), "Your serve – click or press Space", textStyle, Color.white);
            }
            else if (messageTimer > 0f && !string.IsNullOrEmpty(message))
            {
                Label(new Rect(0, Screen.height * 0.42f, w, 50 * s), message, textStyle, new Color(1f, 0.95f, 0.6f));
            }

            Label(new Rect(0, Screen.height - 40 * s, w, 30 * s),
                "Mouse: move paddle   ·   Click/Space: serve   ·   1/2/3: difficulty   ·   R: restart",
                smallStyle, new Color(1f, 1f, 1f, 0.7f));
        }

        static void Label(Rect r, string text, GUIStyle style, Color color)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.6f * color.a);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }
    }
}
