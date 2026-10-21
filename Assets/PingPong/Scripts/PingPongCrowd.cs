using UnityEngine;

namespace PingPong
{
    /// <summary>
    /// The crowd: plays a cheer when you score, applause when the computer scores and a big
    /// reaction at the end of the game. Spectators on the bleachers jump up when they cheer.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PingPongCrowd : MonoBehaviour
    {
        [Header("References (set by the scene builder)")]
        public PingPongGame game;
        public Transform[] spectators;

        [Header("Sounds")]
        public AudioClip playerPoint;   // loud cheer
        public AudioClip cpuPoint;      // polite applause
        public AudioClip playerWins;    // big, long cheer
        public AudioClip playerLoses;   // "aww" + applause
        [Range(0f, 1f)] public float volume = 0.75f;

        [Header("Spectators")]
        public float jumpHeight = 0.16f;
        public float jumpTime = 0.42f;

        AudioSource source;
        Vector3[] basePositions;
        float[] jumpStart;
        int[] jumpsLeft;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            int n = spectators != null ? spectators.Length : 0;
            basePositions = new Vector3[n];
            jumpStart = new float[n];
            jumpsLeft = new int[n];
            for (int i = 0; i < n; i++)
                if (spectators[i] != null) basePositions[i] = spectators[i].localPosition;
        }

        void OnEnable()
        {
            if (game != null) game.PointScored += OnPointScored;
        }

        void OnDisable()
        {
            if (game != null) game.PointScored -= OnPointScored;
        }

        void OnPointScored(Side winner, bool gameOver)
        {
            AudioClip clip;
            float excitement; // share of spectators that jump, and how many times
            if (gameOver)
            {
                clip = winner == Side.Player ? playerWins : playerLoses;
                excitement = winner == Side.Player ? 1f : 0.15f;
            }
            else
            {
                clip = winner == Side.Player ? playerPoint : cpuPoint;
                excitement = winner == Side.Player ? 0.8f : 0.25f;
            }

            if (clip != null)
            {
                source.Stop();
                source.clip = clip;
                source.volume = volume;
                source.pitch = Random.Range(0.96f, 1.04f);
                source.Play();
            }

            Cheer(excitement, gameOver ? 6 : 3);
        }

        /// <summary>Makes a share of the spectators jump a few times.</summary>
        public void Cheer(float share, int maxJumps)
        {
            if (spectators == null) return;
            for (int i = 0; i < spectators.Length; i++)
            {
                if (spectators[i] == null || Random.value > share) continue;
                jumpStart[i] = Time.time + Random.Range(0f, 0.35f);
                jumpsLeft[i] = Random.Range(1, maxJumps + 1);
            }
        }

        void Update()
        {
            if (spectators == null) return;
            float now = Time.time;
            for (int i = 0; i < spectators.Length; i++)
            {
                if (jumpsLeft[i] <= 0 || spectators[i] == null) continue;
                float t = now - jumpStart[i];
                if (t < 0f) continue;

                int hop = Mathf.FloorToInt(t / jumpTime);
                if (hop >= jumpsLeft[i])
                {
                    jumpsLeft[i] = 0;
                    spectators[i].localPosition = basePositions[i];
                    continue;
                }
                float phase = (t - hop * jumpTime) / jumpTime;
                float y = Mathf.Sin(phase * Mathf.PI) * jumpHeight;
                spectators[i].localPosition = basePositions[i] + new Vector3(0f, y, 0f);
            }
        }
    }
}
