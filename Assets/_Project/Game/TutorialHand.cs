using UnityEngine;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 첫 레벨 안내: 손가락이 블록을 집어 보드로 옮기는 동작을 반복해서 보여 준다.
    /// 글을 읽지 않아도 1초 안에 조작을 알게 하는 게 목표.
    /// </summary>
    public sealed class TutorialHand : MonoBehaviour
    {
        Transform piece;
        SpriteRenderer finger, ring;
        Vector3 from, to;
        float t;
        const float Cycle = 1.9f;

        public static TutorialHand Show(Transform parent, Transform ghostPiece, Vector3 fromWorld, Vector3 toWorld)
        {
            var go = new GameObject("TutorialHand");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<TutorialHand>();
            h.piece = ghostPiece;
            h.piece.SetParent(go.transform, true);
            foreach (var sr in h.piece.GetComponentsInChildren<SpriteRenderer>())
            {
                var c = sr.color; c.a *= 0.55f;
                sr.color = c;
            }
            h.from = fromWorld;
            h.to = toWorld;
            h.finger = Gfx.MakeSprite("Finger", go.transform, Gfx.Circle, new Color(1f, 1f, 1f, 0.9f), 36);
            h.finger.transform.localScale = Vector3.one * 0.55f;
            h.ring = Gfx.MakeSprite("Ring", go.transform, Gfx.Ring, new Color(1f, 1f, 1f, 0.8f), 35);
            return h;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Repeat(t, Cycle) / Cycle;
            // 0~0.15 누르기, 0.15~0.65 끌기, 0.65~0.8 놓기, 나머지 쉬기
            Vector3 p;
            float press;
            if (k < 0.15f) { p = from; press = k / 0.15f; }
            else if (k < 0.65f) { p = Vector3.Lerp(from, to, Ease.InOutCubic((k - 0.15f) / 0.5f)); press = 1f; }
            else if (k < 0.8f) { p = to; press = 1f - (k - 0.65f) / 0.15f; }
            else { p = to; press = 0f; }

            bool visible = k < 0.9f;
            finger.enabled = visible;
            ring.enabled = visible && press > 0.05f;
            finger.transform.position = p + new Vector3(0.25f, -0.35f, 0f);
            finger.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 0.45f, press);
            ring.transform.position = finger.transform.position;
            ring.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.3f, Mathf.Repeat(t * 1.6f, 1f));
            var rc = ring.color; rc.a = 0.7f * (1f - Mathf.Repeat(t * 1.6f, 1f));
            ring.color = rc;

            piece.gameObject.SetActive(k > 0.1f && k < 0.9f);
            piece.position = p;
        }
    }
}
