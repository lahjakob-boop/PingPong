using UnityEngine;

namespace PingPong
{
    /// <summary>
    /// A paddle. The player's paddle follows the mouse; the computer's paddle predicts where
    /// the ball will arrive and moves there.
    /// </summary>
    public class PingPongPaddle : MonoBehaviour
    {
        public Side side = Side.Player;
        public bool controlledByAI;

        [Header("Hitbox")]
        [Tooltip("Hit area radius in width (x) and height (y), in metres. Bigger = easier to hit.")]
        public Vector2 hitSize = new Vector2(0.4f, 0.45f);

        [Header("Movement")]
        public Vector2 xRange = new Vector2(-1.15f, 1.15f);
        public Vector2 yRange = new Vector2(0.82f, 1.4f);
        public float followSharpness = 30f;
        public float maxTilt = 25f;

        [Header("Computer (AI)")]
        public float aiMaxSpeed = 2.4f;
        public float aiReactionTime = 0.15f;
        public Vector2 aiHome = new Vector2(0f, 1.0f);

        [Header("References (set by the scene builder)")]
        public PingPongGame game;
        public PingPongBall ball;

        /// <summary>Paddle velocity in m/s (used for shot power and direction).</summary>
        public Vector3 Velocity { get; private set; }
        /// <summary>Direction the paddle face points (+1 = towards +Z).</summary>
        public float Facing { get { return side == Side.Player ? 1f : -1f; } }

        Vector2 aiAimError;
        Vector3 lastPos;
        Camera cam;

        void Start()
        {
            cam = Camera.main;
            lastPos = transform.position;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (controlledByAI) UpdateAI(dt);
            else UpdatePlayer(dt);

            Vector3 pos = transform.position;
            Velocity = (pos - lastPos) / dt;
            lastPos = pos;

            // Slight tilt – visual only.
            float roll = -Mathf.Clamp(pos.x / Mathf.Max(0.01f, xRange.y), -1f, 1f) * maxTilt * Facing;
            float pitch = -10f * Facing;
            transform.rotation = Quaternion.Euler(pitch, side == Side.Player ? 0f : 180f, roll);
        }

        void UpdatePlayer(float dt)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(PPInput.MousePosition);
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
            float enter;
            if (!plane.Raycast(ray, out enter)) return;

            Vector3 hit = ray.GetPoint(enter);
            Vector3 target = new Vector3(
                Mathf.Clamp(hit.x, xRange.x, xRange.y),
                Mathf.Clamp(hit.y, yRange.x, yRange.y),
                transform.position.z);
            float k = 1f - Mathf.Exp(-followSharpness * dt);
            transform.position = Vector3.Lerp(transform.position, target, k);
        }

        void UpdateAI(float dt)
        {
            Vector3 pos = transform.position;
            Vector2 target = aiHome;

            Vector3 p;
            if (game != null && ball != null && game.ShouldAITrackBall(this) &&
                ball.PredictCrossing(pos.z + Facing * ball.radius, 3f, out p))
            {
                target = new Vector2(p.x, p.y) + aiAimError;
            }
            else if (game != null && game.IsServing(side))
            {
                target = new Vector2(0.25f, 1.0f);
            }

            target.x = Mathf.Clamp(target.x, xRange.x, xRange.y);
            target.y = Mathf.Clamp(target.y, yRange.x, yRange.y);
            Vector2 cur = new Vector2(pos.x, pos.y);
            Vector2 next = Vector2.MoveTowards(cur, target, aiMaxSpeed * dt);
            transform.position = new Vector3(next.x, next.y, pos.z);
        }

        /// <summary>New random aiming error for the computer (smaller on higher difficulty).</summary>
        public void NewAimError(float amount)
        {
            aiAimError = Random.insideUnitCircle * amount;
        }

        /// <summary>
        /// Checks whether the ball travelling p0 → p1 passed through the paddle's hit area (ellipse hitSize).
        /// It is a swept test, so a fast ball can't tunnel through the paddle.
        /// </summary>
        public bool TrySweep(Vector3 p0, Vector3 p1, float ballRadius, out Vector3 contact)
        {
            contact = Vector3.zero;
            Vector3 pos = transform.position;
            float face = pos.z + Facing * ballRadius;
            float d0 = (p0.z - face) * Facing;
            float d1 = (p1.z - face) * Facing;
            if (!(d0 >= 0f && d1 < 0f)) return false;

            float f = d0 / (d0 - d1);
            Vector3 c = Vector3.Lerp(p0, p1, f);
            float nx = (c.x - pos.x) / Mathf.Max(0.01f, hitSize.x);
            float ny = (c.y - pos.y) / Mathf.Max(0.01f, hitSize.y);
            if (nx * nx + ny * ny > 1f) return false;

            contact = c;
            return true;
        }
    }
}
