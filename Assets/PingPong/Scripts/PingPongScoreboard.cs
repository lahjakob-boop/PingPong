using UnityEngine;

namespace PingPong
{
    /// <summary>Flip scoreboard next to the table (3D text) showing the score and who serves.</summary>
    public class PingPongScoreboard : MonoBehaviour
    {
        public PingPongGame game;
        public TextMesh playerScore;
        public TextMesh aiScore;
        [Tooltip("Light that glows next to the name of whoever is serving.")]
        public GameObject playerServeLight;
        public GameObject aiServeLight;

        int lastPlayer = -1, lastAI = -1;
        int lastServe = -1;

        void Update()
        {
            if (game == null) return;
            int p = game.PlayerScore, a = game.AIScore;
            int serve = game.PlayerServing ? 0 : 1;
            if (p == lastPlayer && a == lastAI && serve == lastServe) return;
            lastPlayer = p;
            lastAI = a;
            lastServe = serve;
            if (playerScore != null) playerScore.text = p.ToString();
            if (aiScore != null) aiScore.text = a.ToString();
            if (playerServeLight != null) playerServeLight.SetActive(serve == 0);
            if (aiServeLight != null) aiServeLight.SetActive(serve == 1);
        }
    }
}
