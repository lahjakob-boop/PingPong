using UnityEngine;

namespace PingPong
{
    /// <summary>
    /// One LED board around the court. All boards together show one long LED strip;
    /// this board shows the slice of the strip between u0 and u1 (0..1).
    /// </summary>
    public class PingPongLedPanel : MonoBehaviour
    {
        public Renderer face;
        [Range(0f, 1f)] public float u0;
        [Range(0f, 1f)] public float u1 = 1f;

        /// <summary>Shows the given strip texture on this board (set at runtime, nothing is saved into the material).</summary>
        public void Apply(Texture strip)
        {
            if (face == null) return;
            var block = new MaterialPropertyBlock();
            face.GetPropertyBlock(block);
            block.SetTexture("_MainTex", strip);
            block.SetVector("_MainTex_ST", new Vector4(u1 - u0, 1f, u0, 0f));
            face.SetPropertyBlock(block);
        }
    }
}
