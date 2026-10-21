using UnityEngine;

namespace PingPong
{
    /// <summary>Scoreboard next to the table (3D text) showing the current score.</summary>
    public class PingPongScoreboard : MonoBehaviour
    {
        public PingPongGame game;
        public TextMesh playerScore;
        public TextMesh aiScore;
        public TextMesh serveMark;

        int lastPlayer = -1, lastAI = -1;
        bool lastServe;

        void Update()
        {
            if (game == null) return;
            int p = game.PlayerScore, a = game.AIScore;
            bool serve = game.PlayerServing;
            if (p == lastPlayer && a == lastAI && serve == lastServe) return;
            lastPlayer = p;
            lastAI = a;
            lastServe = serve;
            if (playerScore != null) playerScore.text = p.ToString();
            if (aiScore != null) aiScore.text = a.ToString();
            if (serveMark != null) serveMark.text = serve ? "< serve" : "serve >";
        }
    }
}
