using UnityEngine;

namespace PingPong
{
    /// <summary>
    /// Ball with its own (precise) physics. Unity's Rigidbody is unreliable for small, fast
    /// balls, so movement is integrated in small sub-steps: gravity, table bounce,
    /// net, floor and paddle hits.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PingPongBall : MonoBehaviour
    {
        [Header("Physics")]
        public float radius = 0.028f;
        public float gravity = 9.81f;
        [Tooltip("How much speed the ball keeps when bouncing off the table (0-1).")]
        public float tableRestitution = 0.86f;
        public float tableFriction = 0.97f;
        public float floorY = 0f;
        [Tooltip("Simulation speed. Lower = slower, easier game.")]
        public float simSpeed = 0.8f;

        [Header("References (set by the scene builder)")]
        public PingPongGame game;
        public PingPongPaddle[] paddles;
        [Tooltip("Soft shadow directly under the ball.")]
        public Transform blobShadow;

        public Vector3 Velocity { get; set; }
        /// <summary>When true the ball is held still (e.g. before a serve).</summary>
        public bool Frozen { get; set; }
        public bool NetTouched { get; private set; }

        public event System.Action<Side> TableBounced;
        public event System.Action NetHit;
        public event System.Action Dead;

        const float SubStep = 1f / 240f;
        bool deadFired;
        AudioSource audioSource;
        AudioClip paddleClip, tableClip, floorClip;

        void Awake()
        {
            Frozen = true;
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            paddleClip = MakeClick("paddle", 1500f, 0.06f, 0.35f, 70f);
            tableClip = MakeClick("table", 2300f, 0.05f, 0.25f, 90f);
            floorClip = MakeClick("floor", 700f, 0.08f, 0.2f, 50f);
        }

        /// <summary>Prepares the ball for a new rally.</summary>
        public void ResetFlags()
        {
            deadFired = false;
            NetTouched = false;
        }

        void Update()
        {
            if (Frozen) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f) * simSpeed;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / SubStep));
            float h = dt / steps;
            for (int i = 0; i < steps && !Frozen; i++) Step(h);
        }

        void LateUpdate()
        {
            if (blobShadow == null) return;
            Vector3 p = transform.position;
            bool overTable = Table.IsOverTable(p) && p.y >= Table.Height;
            float groundY = overTable ? Table.Height + 0.003f : floorY + 0.008f;
            float height = Mathf.Max(0f, p.y - radius - groundY);
            blobShadow.position = new Vector3(p.x, groundY, p.z);
            float size = radius * 2.6f * (1f + height * 1.2f);
            blobShadow.localScale = new Vector3(size, size, 1f);
        }

        void Step(float h)
        {
            Vector3 p0 = transform.position;
            Vector3 v = Velocity;
            v.y -= gravity * h;
            Vector3 p1 = p0 + v * h;

            // 1) Paddle
            if (paddles != null && game != null)
            {
                for (int i = 0; i < paddles.Length; i++)
                {
                    PingPongPaddle pad = paddles[i];
                    if (pad == null) continue;
                    Vector3 contact;
                    if (pad.TrySweep(p0, p1, radius, out contact) && game.CanPaddleHit(pad))
                    {
                        transform.position = contact + new Vector3(0f, 0f, pad.Facing * 0.01f);
                        Velocity = Vector3.zero;
                        NetTouched = false;
                        PlaySound(paddleClip, 0.9f);
                        game.HandlePaddleHit(pad, transform.position); // sets the new velocity
                        return;
                    }
                }
            }

            // 2) Net (plane z = 0)
            if (!NetTouched && (p0.z < 0f) != (p1.z < 0f))
            {
                float f = p0.z / (p0.z - p1.z);
                Vector3 c = Vector3.Lerp(p0, p1, f);
                bool withinX = Mathf.Abs(c.x) <= Table.HalfWidth + Table.NetOverhang;
                bool withinY = c.y - radius < Table.Height + Table.NetHeight && c.y + radius > Table.Height - 0.02f;
                if (withinX && withinY)
                {
                    NetTouched = true;
                    v = new Vector3(v.x * 0.4f, v.y * 0.4f, -v.z * 0.15f);
                    p1 = new Vector3(c.x, c.y, p0.z); // stays on its own side
                    transform.position = p1;
                    Velocity = v;
                    PlaySound(floorClip, 0.6f);
                    if (NetHit != null) NetHit();
                    return;
                }
            }

            // 3) Table
            if (v.y < 0f && p0.y - radius >= Table.Height - 0.001f && p1.y - radius < Table.Height)
            {
                float f = (p0.y - radius - Table.Height) / (p0.y - p1.y);
                Vector3 c = Vector3.Lerp(p0, p1, f);
                if (Table.IsOverTable(c))
                {
                    float impact = -v.y;
                    p1.y = Table.Height + radius;
                    v.y = impact > 0.25f ? impact * tableRestitution : 0f; // tiny bounces are absorbed (rolling)
                    v.x *= tableFriction;
                    v.z *= tableFriction;
                    transform.position = p1;
                    Velocity = v;
                    if (impact > 0.4f) PlaySound(tableClip, Mathf.Clamp01(impact / 4f) + 0.3f);
                    if (TableBounced != null) TableBounced(Table.SideOf(c.z));
                    return;
                }
            }

            // 4) Floor and out of play
            if (p1.y - radius <= floorY)
            {
                p1.y = floorY + radius;
                if (v.y < 0f)
                {
                    if (-v.y > 0.6f) PlaySound(floorClip, 0.5f);
                    v.y = -v.y * 0.55f;
                }
                v.x *= 0.9f;
                v.z *= 0.9f;
                transform.position = p1;
                Velocity = v;
                FireDead();
                return;
            }

            transform.position = p1;
            Velocity = v;

            if (Mathf.Abs(p1.z) > 10f || Mathf.Abs(p1.x) > 10f)
            {
                FireDead();
                Frozen = true;
            }
        }

        void FireDead()
        {
            if (deadFired) return;
            deadFired = true;
            if (Dead != null) Dead();
        }

        /// <summary>
        /// Computes the velocity that makes the ball land on the table at target (x, z) from "from".
        /// If it would hit the net, the flight is lengthened (higher arc) until it clears.
        /// </summary>
        public Vector3 SolveVelocity(Vector3 from, Vector3 target, float horizontalSpeed)
        {
            Vector3 flat = new Vector3(target.x - from.x, 0f, target.z - from.z);
            float dist = flat.magnitude;
            float T = Mathf.Max(0.2f, dist / Mathf.Max(0.5f, horizontalSpeed));
            float yTarget = Table.Height + radius;
            float g = gravity;
            Vector3 vel = Vector3.zero;
            bool crossesNet = (from.z < 0f) != (target.z < 0f);

            for (int i = 0; i < 40; i++)
            {
                vel = flat / T;
                vel.y = (yTarget - from.y + 0.5f * g * T * T) / T;
                if (!crossesNet || Mathf.Abs(vel.z) < 0.0001f) break;
                float tNet = -from.z / vel.z;
                float yNet = from.y + vel.y * tNet - 0.5f * g * tNet * tNet;
                if (yNet - radius > Table.Height + Table.NetHeight + 0.03f) break;
                T *= 1.07f;
            }
            return vel;
        }

        /// <summary>Predicts where the ball will cross the plane z = planeZ (used by the AI).</summary>
        public bool PredictCrossing(float planeZ, float maxTime, out Vector3 point)
        {
            Vector3 p = transform.position;
            Vector3 v = Velocity;
            const float h = 1f / 120f;
            for (float t = 0f; t < maxTime; t += h)
            {
                v.y -= gravity * h;
                Vector3 n = p + v * h;
                if (v.y < 0f && p.y - radius >= Table.Height - 0.001f && n.y - radius < Table.Height && Table.IsOverTable(n))
                {
                    n.y = Table.Height + radius;
                    v.y = -v.y * tableRestitution;
                    v.x *= tableFriction;
                    v.z *= tableFriction;
                }
                if ((p.z - planeZ) * (n.z - planeZ) <= 0f && !Mathf.Approximately(p.z, n.z))
                {
                    float f = (planeZ - p.z) / (n.z - p.z);
                    point = Vector3.Lerp(p, n, f);
                    return true;
                }
                if (n.y < floorY) break;
                p = n;
            }
            point = p;
            return false;
        }

        public void PlayServeSound()
        {
            PlaySound(paddleClip, 0.8f);
        }

        void PlaySound(AudioClip clip, float volume)
        {
            if (audioSource == null || clip == null) return;
            audioSource.pitch = Random.Range(0.93f, 1.07f);
            audioSource.PlayOneShot(clip, volume);
        }

        /// <summary>Generates a short "tock" sound so no external audio files are needed.</summary>
        static AudioClip MakeClick(string name, float freq, float duration, float noise, float decay)
        {
            const int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * duration));
            float[] data = new float[n];
            System.Random rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * decay);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f;
                float hiss = ((float)rng.NextDouble() * 2f - 1f) * noise;
                data[i] = (tone + hiss) * env;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
