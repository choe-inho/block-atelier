using System.Collections;
using System.Collections.Generic;
using System.IO;
using BlockAtelier.Core;
using BlockAtelier.Core.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 게임 전체를 코드로 조립하고 진행한다. 씬에 아무것도 없어도 Play하면 자동으로 생긴다.
    /// 화면 배치 기준: 가로 10, 세로 20.6 월드 단위 (폰 세로 화면).
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        public static float BaseTimeScale = 1f;
        const float DesignW = 10.2f, DesignH = 20.8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<GameRoot>() == null) new GameObject("GameRoot").AddComponent<GameRoot>();
        }

        Camera cam;
        Fx fx;
        Transform top, middle, bottom;
        Canvas topCanvas, bottomCanvas, overlayCanvas;
        BoardView board;
        PictureView picture;
        TrayView tray;
        SpriteRenderer background;
        Text titleText, movesText, pctText, pctLabel;
        readonly List<Text> chipTexts = new List<Text>();
        readonly List<SpriteRenderer> chipDots = new List<SpriteRenderer>();
        Transform chipRoot;
        WorldButton undoBtn, restartBtn, levelsBtn;
        readonly List<WorldButton> overlayButtons = new List<WorldButton>();
        Transform overlayRoot;

        List<LevelData> levels;
        int levelIndex;
        GameSession game;
        Color[] palette;
        int undoLeft;
        int combo;
        bool busy;
        HashSet<int> cleared = new HashSet<int>();
        float lastAspect = -1f;

        // 끌어서 놓기
        Transform floatPiece;
        BlockSource dragSrc;
        bool dragging;
        Vector2Int? dragCell;
        bool dragOverHold;
        bool touchInput;

        // 자동 데모 (개발용)
        bool demo;
        float demoTimer;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Gfx.Hex("#141722");

            Sfx.Init(transform);
            fx = new GameObject("Fx").AddComponent<Fx>();
            fx.transform.SetParent(transform, false);
            fx.Init(cam);
            fx.SetCameraBase(cam.transform.position);

            background = Gfx.MakeSprite("Background", transform, Gfx.Gradient, Color.white, -10);
            SetupPost();

            top = Group("Top");
            middle = Group("Middle");
            bottom = Group("Bottom");
            topCanvas = UI.WorldCanvas("TopText", top, 50);
            bottomCanvas = UI.WorldCanvas("BottomText", bottom, 50);

            board = new BoardView(middle);
            board.Root.localPosition = new Vector3(0f, -0.35f, 0f);
            picture = new PictureView(top);
            tray = new TrayView(bottom, bottomCanvas, board);
            tray.Root.localPosition = new Vector3(0f, 2.75f, 0f);
            // HoldLabel은 bottomCanvas 기준이라 트레이 위치만큼 옮긴다
            tray.HoldLabel.rectTransform.anchoredPosition += new Vector2(0f, 275f);

            titleText = UI.Label(topCanvas, "", new Vector2(-4.7f, -0.62f), 0.46f, UI.Ink, TextAnchor.MiddleLeft, 7f, true);
            movesText = UI.Label(topCanvas, "", new Vector2(4.7f, -0.62f), 0.3f, UI.Muted, TextAnchor.MiddleRight, 3f);
            pctText = UI.Label(topCanvas, "0%", new Vector2(2.95f, -2.55f), 0.95f, UI.Ink, TextAnchor.MiddleCenter, 3.5f, true);
            pctLabel = UI.Label(topCanvas, "그림 완성도", new Vector2(2.95f, -3.35f), 0.24f, UI.Muted, TextAnchor.MiddleCenter, 3.5f);
            chipRoot = new GameObject("Chips").transform;
            chipRoot.SetParent(top, false);

            undoBtn = new WorldButton(bottom, bottomCanvas, "되돌리기", new Vector2(-3.25f, 0.85f), new Vector2(3.0f, 0.95f), false, 45);
            restartBtn = new WorldButton(bottom, bottomCanvas, "처음부터", new Vector2(0f, 0.85f), new Vector2(3.0f, 0.95f), false, 45);
            levelsBtn = new WorldButton(bottom, bottomCanvas, "레벨", new Vector2(3.25f, 0.85f), new Vector2(3.0f, 0.95f), false, 45);
            undoBtn.OnClick = DoUndo;
            restartBtn.OnClick = () => StartLevel(levelIndex);
            levelsBtn.OnClick = ShowLevels;

            overlayRoot = Group("Overlay");
            overlayCanvas = UI.WorldCanvas("OverlayText", overlayRoot, 90);

            LoadLevels();
            LoadProgress();
            Layout(cam.aspect);
            StartLevel(Mathf.Clamp(PlayerPrefs.GetInt("ba_level", 0), 0, levels.Count - 1));
        }

        /// <summary>빛 번짐(블룸)과 가장자리 어둡게. 흰색 이상 밝은 것만 번진다.</summary>
        void SetupPost()
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
            var go = new GameObject("PostFx");
            go.transform.SetParent(transform, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.96f);
            bloom.intensity.Override(1.3f);
            bloom.scatter.Override(0.7f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.3f);
            vig.smoothness.Override(0.55f);
            vol.sharedProfile = profile;
        }

        Transform Group(string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(transform, false);
            return t;
        }

        // ---------------- 배치 ----------------

        void Layout(float aspect)
        {
            lastAspect = aspect;
            float size = Mathf.Max(DesignH * 0.5f, DesignW * 0.5f / aspect);
            cam.orthographicSize = size;
            float halfW = size * aspect;
            top.localPosition = new Vector3(0f, size, 0f);
            bottom.localPosition = new Vector3(0f, -size, 0f);
            // 세로가 남으면 보드를 가운데 쪽으로, 위아래 요소는 가장자리에 붙인다
            middle.localPosition = new Vector3(0f, Mathf.Clamp((size - DesignH * 0.5f) * -0.15f, -1f, 0f), 0f);
            overlayRoot.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(halfW * 2f / (4f / 4f) + 1f, size * 2f / (128f / 4f) + 0.1f, 1f);
        }

        // ---------------- 레벨 ----------------

        void LoadLevels()
        {
            levels = new List<LevelData>();
            var assets = Resources.LoadAll<TextAsset>("Levels");
            System.Array.Sort(assets, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (var a in assets)
            {
                try { levels.Add(LevelData.FromJson(a.text)); }
                catch (System.Exception e) { Debug.LogError("레벨 읽기 실패 " + a.name + ": " + e.Message); }
            }
        }

        void LoadProgress()
        {
            cleared.Clear();
            var s = PlayerPrefs.GetString("ba_cleared", "");
            foreach (var p in s.Split(','))
            {
                int id;
                if (int.TryParse(p, out id)) cleared.Add(id);
            }
        }

        void SaveProgress()
        {
            PlayerPrefs.SetString("ba_cleared", string.Join(",", cleared));
            PlayerPrefs.SetInt("ba_level", levelIndex);
            PlayerPrefs.Save();
        }

        void StartLevel(int index)
        {
            Tween.StopAll();
            StopAllCoroutines();
            fx.Clear();
            Time.timeScale = BaseTimeScale;
            CancelDrag();
            HideOverlay();
            busy = false;
            combo = 0;
            levelIndex = Mathf.Clamp(index, 0, levels.Count - 1);
            PlayerPrefs.SetInt("ba_level", levelIndex);
            var lv = levels[levelIndex];
            game = new GameSession(lv);
            undoLeft = 3;

            palette = new Color[lv.Palette.Count];
            for (int i = 0; i < palette.Length; i++) palette[i] = Gfx.Hex(lv.Palette[i]);
            board.SetPalette(palette);
            picture.Setup(lv, palette, 4.3f);
            picture.Root.localPosition = new Vector3(-2.15f, -3.35f, 0f);
            picture.Root.localScale = Vector3.one;

            titleText.text = lv.Id + "  " + lv.PictureName + (lv.Difficulty == "hard" ? "  <color=#FF6B6B>어려움</color>" : "");
            titleText.supportRichText = true;
            BuildChips();
            RenderAll();

            // 등장 연출: 보드 칸이 대각선으로 톡톡
            for (int i = 0; i < 64; i++)
            {
                var w = board.Root.GetChild(1 + i * 3);
                int x = i % 8, y = i / 8;
                float d = (x + y) * 0.018f;
                w.localScale = Vector3.zero;
                Tween.Run(0.28f, k => w.localScale = Vector3.one * 0.96f * Ease.OutBack(k), null, d);
            }
            var pr = picture.Root;
            Tween.Run(0.4f, k => pr.localScale = Vector3.one * Ease.OutBack(k), null, 0.1f);
        }

        void BuildChips()
        {
            for (int i = chipRoot.childCount - 1; i >= 0; i--) Destroy(chipRoot.GetChild(i).gameObject);
            foreach (var t in chipTexts) if (t != null) Destroy(t.gameObject);
            chipTexts.Clear();
            chipDots.Clear();
            int n = 0;
            for (int c = 1; c <= game.Level.ColorCount; c++) if (game.Picture.Total(c) > 0) n++;
            float spacing = 1.2f;
            float x0 = 3.0f - (n - 1) * spacing * 0.5f;
            int k = 0;
            for (int c = 1; c <= game.Level.ColorCount; c++)
            {
                if (game.Picture.Total(c) == 0) { chipTexts.Add(null); chipDots.Add(null); continue; }
                var pos = new Vector2(x0 + k * spacing, -4.45f);
                var bg = UI.Panel("Chip", chipRoot, pos, new Vector2(1.1f, 0.62f), UI.Button, 40);
                var dot = Gfx.MakeSprite("Dot", chipRoot, Gfx.Block, palette[c], 41);
                dot.transform.localPosition = pos + new Vector2(-0.3f, 0f);
                dot.transform.localScale = Vector3.one * 0.38f;
                var label = UI.Label(topCanvas, "", pos + new Vector2(0.18f, 0f), 0.28f, UI.Ink, TextAnchor.MiddleCenter, 1f, true);
                chipTexts.Add(label);
                chipDots.Add(dot);
                k++;
            }
        }

        int ShownRemaining(int color)
        {
            int left = game.Picture.Remaining(color);
            foreach (int idx in picture.Pending)
                if (game.Picture.TargetAt(idx % game.Picture.Width, idx / game.Picture.Width) == color) left++;
            return left;
        }

        void RenderHud()
        {
            int total = game.Picture.TotalPixels;
            int shownLeft = game.Picture.RemainingPixels + picture.Pending.Count;
            int pct = total == 0 ? 100 : Mathf.RoundToInt((1f - shownLeft / (float)total) * 100f);
            pctText.text = pct + "%";
            movesText.text = game.MovesUsed + "수";
            for (int c = 1; c <= game.Level.ColorCount; c++)
            {
                var t = chipTexts[c - 1];
                if (t == null) continue;
                int left = ShownRemaining(c);
                t.text = left == 0 ? "✓" : left.ToString();
                t.color = left == 0 ? UI.Good : UI.Ink;
            }
            undoBtn.Label.text = "되돌리기 " + undoLeft;
            undoBtn.SetEnabled(!busy && undoLeft > 0 && game.CanUndo && game.State == GameState.Playing);
        }

        void RenderAll()
        {
            board.Render(game.Board);
            picture.Render(game.Picture);
            tray.Refresh(game);
            RenderHud();
        }

        // ---------------- 입력 ----------------

        void Update()
        {
            if (!Mathf.Approximately(cam.aspect, lastAspect)) Layout(cam.aspect);
            if (game != null) board.Tick(game.Board);
#if UNITY_EDITOR
            PollDevCommands();
#endif
            if (demo) { DemoTick(); return; }

            var p = Pointer.current;
            if (p == null) return;
            var sp = p.position.ReadValue();
            var wp = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, -cam.transform.position.z));
            wp.z = 0f;
            touchInput = p is Touchscreen;

            if (p.press.wasPressedThisFrame) OnDown(wp);
            else if (p.press.isPressed) { if (dragging) OnDrag(wp); }
            else if (p.press.wasReleasedThisFrame) OnUp(wp);
        }

        void OnDown(Vector3 wp)
        {
            if (overlayRoot.childCount > 1)
            {
                foreach (var b in overlayButtons)
                    if (b.Hit(wp)) { b.Press(); if (b.OnClick != null) b.OnClick(); return; }
                return;
            }
            foreach (var b in new[] { undoBtn, restartBtn, levelsBtn })
                if (b.Hit(wp)) { b.Press(); if (b.OnClick != null) b.OnClick(); return; }
            if (busy || game.State != GameState.Playing) return;

            BlockSource src;
            if (!tray.HitSource(wp, game, out src)) return;
            BeginDrag(src, wp);
        }

        void BeginDrag(BlockSource src, Vector3 wp)
        {
            var block = game.GetBlock(src);
            if (block.IsNone) return;
            dragSrc = src;
            dragging = true;
            floatPiece = BuildPiece(block, 20);
            floatPiece.position = tray.SlotWorld(src);
            floatPiece.localScale = Vector3.one * 0.55f;
            var fp = floatPiece;
            Tween.Run(0.12f, k => { if (fp != null) fp.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, Ease.OutBack(k)); });
            tray.SetHidden(src, true);
            tray.Nudge(src);
            Sfx.Play(Sfx.Pick, 0.7f);
            OnDrag(wp);
        }

        Transform BuildPiece(Block block, int order)
        {
            var root = new GameObject("FloatPiece").transform;
            root.SetParent(transform, false);
            var shape = block.Shape;
            var color = board.ColorOf(block.Color);
            for (int k = 0; k < shape.Size; k++)
            {
                var p = new Vector3(shape.Xs[k] * BoardView.Pitch, -shape.Ys[k] * BoardView.Pitch, 0f);
                var sh = Gfx.MakeSprite("shadow", root, Gfx.Well, new Color(0f, 0f, 0.05f, 0.35f), order - 1);
                sh.transform.localPosition = p + new Vector3(0.1f, -0.2f, 0f);
                sh.transform.localScale = Vector3.one * BoardView.BlockSize;
                var sr = Gfx.MakeSprite("c", root, Gfx.Block, color, order);
                sr.transform.localPosition = p;
                sr.transform.localScale = Vector3.one * BoardView.BlockSize;
            }
            return root;
        }

        /// <summary>floatPiece의 원점 = 모양의 (0,0) 칸 중심</summary>
        Vector3 PieceOriginFor(Vector3 pointer, Shape shape)
        {
            float w = (shape.Width - 1) * BoardView.Pitch, h = (shape.Height - 1) * BoardView.Pitch;
            float lift = touchInput ? 1.6f + h * 0.5f : 0f;
            return new Vector3(pointer.x - w * 0.5f, pointer.y + h * 0.5f + lift, 0f);
        }

        void OnDrag(Vector3 wp)
        {
            if (!dragging || floatPiece == null) return;
            var block = game.GetBlock(dragSrc);
            var origin = PieceOriginFor(wp, block.Shape);
            floatPiece.position = Vector3.Lerp(floatPiece.position, origin, 0.65f);

            dragOverHold = !dragSrc.FromHold && tray.HitHold(wp);
            tray.HighlightHold(dragOverHold);
            Vector2Int? cell = null;
            if (!dragOverHold)
            {
                var g = board.GridFromWorld(origin);
                if (game.CanPlace(dragSrc, g.x, g.y)) cell = g;
            }
            if (cell != dragCell)
            {
                dragCell = cell;
                if (cell.HasValue)
                {
                    board.ShowGhost(game.Board, block.Shape, block.Color, cell.Value.x, cell.Value.y);
                    Sfx.Play(Sfx.Tick, 0.25f, 1.4f);
                }
                else board.ClearGhost(game.Board);
            }
        }

        void OnUp(Vector3 wp)
        {
            if (!dragging) return;
            if (dragOverHold)
            {
                var fp = floatPiece;
                floatPiece = null;
                dragging = false;
                tray.HighlightHold(false);
                board.ClearGhost(game.Board);
                Destroy(fp.gameObject);
                game.TryStash(dragSrc.Index);
                Sfx.Play(Sfx.Pick, 0.6f, 0.8f);
                tray.Refresh(game, dragSrc.Index);
                AfterState(null);
                RenderHud();
                return;
            }
            if (dragCell.HasValue) Commit(dragSrc, dragCell.Value);
            else ReturnPiece();
        }

        void ReturnPiece()
        {
            var fp = floatPiece;
            var src = dragSrc;
            floatPiece = null;
            dragging = false;
            dragCell = null;
            board.ClearGhost(game.Board);
            if (fp == null) return;
            var from = fp.position;
            var to = tray.SlotWorld(src);
            Tween.Run(0.16f, k =>
            {
                fp.position = Vector3.Lerp(from, to, Ease.OutCubic(k));
                fp.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, k);
            }, () => { Destroy(fp.gameObject); tray.SetHidden(src, false); });
        }

        void CancelDrag()
        {
            if (floatPiece != null) Destroy(floatPiece.gameObject);
            floatPiece = null;
            dragging = false;
            dragCell = null;
            if (tray != null) tray.HighlightHold(false);
        }

        void Commit(BlockSource src, Vector2Int cell)
        {
            var fp = floatPiece;
            floatPiece = null;
            dragging = false;
            dragCell = null;
            busy = true;
            var target = board.CellWorld(BoardModel.Index(cell.x, cell.y));
            var from = fp.position;
            Tween.Run(0.06f, k => fp.position = Vector3.Lerp(from, target, k), () =>
            {
                Destroy(fp.gameObject);
                var r = game.TryPlace(src, cell.x, cell.y);
                if (r == null) { busy = false; RenderAll(); return; }
                StartCoroutine(PlayMove(r, src));
            });
        }

        // ---------------- 한 수 연출 ----------------

        IEnumerator PlayMove(MoveResult r, BlockSource src)
        {
            busy = true;
            foreach (var p in r.Paints) if (p.Pixel >= 0) picture.Pending.Add(p.Pixel);

            // 지우기 직전 모습: 지워질 칸은 원래 색으로 남겨 둔다
            var overrides = new Dictionary<int, int>();
            foreach (var c in r.Cleared) overrides[c.Index] = c.Color;
            board.ClearGhost(game.Board);
            board.Render(game.Board, overrides);
            tray.Refresh(game, src.FromHold ? -1 : src.Index);
            RenderHud();

            // 1) 놓기: 톡 눌리며 자리 잡기
            Sfx.Play(Sfx.Place, 0.9f, UnityEngine.Random.Range(0.95f, 1.05f));
            foreach (int i in r.Placed)
            {
                fx.Puff(board.CellWorld(i) + Vector3.down * 0.35f, new Color(1f, 1f, 1f, 0.35f), 2, 1.2f, 0.45f);
                var t = board.BlockAt(i).transform;
                Tween.Run(0.16f, k =>
                {
                    float s = 1f + 0.16f * Ease.Bump(k);
                    t.localScale = new Vector3(BoardView.BlockSize * s, BoardView.BlockSize * (2f - s), 1f);
                });
            }

            if (r.LinesCleared == 0)
            {
                combo = 0;
                yield return new WaitForSeconds(0.08f);
                Finish(r);
                yield break;
            }

            combo++;
            yield return new WaitForSeconds(0.1f);

            // 2) 지워질 줄이 하얗게 번쩍
            foreach (var c in r.Cleared)
            {
                var sr = board.BlockAt(c.Index);
                var baseC = sr.color;
                Tween.Run(0.1f, k => sr.color = Color.Lerp(baseC, Color.white, k * 0.85f));
            }
            int lines = r.LinesCleared;
            var br = board.Root;
            Tween.Run(0.22f, k => br.localScale = Vector3.one * (1f + 0.025f * lines * Ease.Bump(k)));
            Sfx.Play(Sfx.Clear, 0.8f, 1f + 0.06f * Mathf.Min(combo - 1, 6) + 0.04f * (lines - 1));
            yield return new WaitForSeconds(0.08f);

            // 3) 붓질·폭발 연출
            var mono = r.MonochromeRows.Count + r.MonochromeCols.Count;
            foreach (int y in r.MonochromeRows)
            {
                var a = board.CellWorld(BoardModel.Index(0, y));
                var b = board.CellWorld(BoardModel.Index(7, y));
                fx.Sweep(a, b, board.ColorOf(overrides[BoardModel.Index(0, y)]), 1.1f);
            }
            foreach (int x in r.MonochromeCols)
            {
                var a = board.CellWorld(BoardModel.Index(x, 0));
                var b = board.CellWorld(BoardModel.Index(x, 7));
                fx.Sweep(a, b, board.ColorOf(overrides[BoardModel.Index(x, 0)]), 1.1f);
            }
            if (mono > 0) Sfx.Play(Sfx.Whoosh, 0.8f);
            if (r.Exploded)
            {
                foreach (int y in r.ClearedRows)
                    foreach (int x in r.ClearedCols)
                        fx.Shockwave(board.CellWorld(BoardModel.Index(x, y)), new Color(1f, 0.85f, 0.5f, 1f), 6.5f);
                Sfx.Play(Sfx.Boom, 0.9f);
                fx.Shake(Fx.ShakeExplode + Fx.ShakePerLine * lines, 0.4f);
                fx.HitStop(0.07f);
                fx.ScreenFlash(new Color(1f, 0.95f, 0.85f), 0.2f, 0.18f);
            }
            else fx.Shake(Fx.ShakePerLine * lines, 0.25f);

            // 떠오르는 글자
            string msg = null;
            if (mono > 0 && r.Exploded) msg = "붓질 + 십자 폭발!";
            else if (r.Exploded) msg = "십자 폭발!";
            else if (mono > 0) msg = "붓질 ×2";
            else if (lines >= 2) msg = lines + "줄!";
            if (combo >= 2) msg = (msg == null ? "" : msg + "\n") + "콤보 " + combo;
            if (msg != null)
            {
                var at = Vector3.zero;
                foreach (var c in r.Cleared) at += board.CellWorld(c.Index);
                at /= Mathf.Max(1, r.Cleared.Count);
                var bc = board.Root.position;
                at = new Vector3(bc.x, at.y < bc.y ? bc.y + 2.1f : bc.y - 2.1f, 0f);
                fx.Text(msg, at, mono > 0 || r.Exploded ? UI.Accent : UI.Ink, 0.95f, 1.1f);
            }

            // 4) 칸이 부서지면서 그 칸의 페인트가 바로 튀어 오른다 (놓은 자리에서 멀수록 늦게, 물결처럼)
            var byCell = new Dictionary<int, List<PaintEvent>>();
            foreach (var p in r.Paints)
            {
                List<PaintEvent> list;
                if (!byCell.TryGetValue(p.FromCell, out list)) byCell[p.FromCell] = list = new List<PaintEvent>();
                list.Add(p);
            }
            var placedCenter = Vector3.zero;
            foreach (int i in r.Placed) placedCenter += board.CellWorld(i);
            placedCenter /= Mathf.Max(1, r.Placed.Count);
            int n = r.Paints.Count, arrived = 0, note = 0;
            foreach (var c in r.Cleared)
            {
                var pos = board.CellWorld(c.Index);
                float d = Vector3.Distance(pos, placedCenter) * 0.03f + (c.FromExplosion ? 0.1f : 0f);
                var sr = board.BlockAt(c.Index);
                var color = board.ColorOf(c.Color);
                var t = sr.transform;
                float power = c.Multiplier > 1 ? 1.4f : 1f;
                List<PaintEvent> paints;
                byCell.TryGetValue(c.Index, out paints);
                Tween.Run(0.12f, k =>
                {
                    t.localScale = Vector3.one * BoardView.BlockSize * Mathf.Lerp(1.12f, 0f, Ease.InCubic(k));
                    t.localRotation = Quaternion.Euler(0f, 0f, k * 25f);
                }, () =>
                {
                    sr.enabled = false;
                    t.localScale = Vector3.one * BoardView.BlockSize;
                    t.localRotation = Quaternion.identity;
                    fx.Shatter(pos, color, power);
                    if (paints == null) return;
                    for (int k = 0; k < paints.Count; k++)
                    {
                        var p = paints[k];
                        var from = pos + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.2f);
                        int colIdx = p.Color;
                        if (p.Color == Cell.Wild && p.Pixel >= 0) colIdx = game.Picture.TargetAt(p.Pixel % game.Picture.Width, p.Pixel / game.Picture.Width);
                        var dc = board.ColorOf(colIdx);
                        float delay = k * 0.06f;
                        if (p.Pixel < 0) { fx.Fizzle(from, dc, delay); arrived++; continue; }
                        int px = p.Pixel;
                        var to = picture.PixelWorld(px);
                        fx.Drop(from, to, dc, delay, () =>
                        {
                            picture.Reveal(px, game.Picture);
                            fx.Splash(to, dc);
                            PulsePicture();
                            Sfx.PlayNote(note++, 0.55f);
                            arrived++;
                            RenderHud();
                        });
                    }
                }, d);
            }

            float timeout = Time.time + 5f;
            while (arrived < n && Time.time < timeout) yield return null;
            picture.Pending.Clear();
            picture.Render(game.Picture);

            // 6) 한 색을 다 칠했다
            foreach (int c in r.CompletedColors)
            {
                if (c - 1 < chipDots.Count && chipDots[c - 1] != null)
                {
                    var dot = chipDots[c - 1].transform;
                    Tween.Run(0.4f, k => dot.localScale = Vector3.one * 0.38f * (1f + 0.6f * Ease.Bump(k)));
                    fx.Puff(dot.position, palette[c], 10, 3f, 0.4f);
                }
                if (game.State == GameState.Playing)
                {
                    Sfx.Play(Sfx.Pop, 0.7f);
                    fx.Text("한 색 완성!", picture.Root.position + new Vector3(0f, -2.3f, 0f), UI.Good, 0.42f, 0.9f);
                }
            }
            if (r.Grayed.Count > 0) board.Render(game.Board);
            if (r.ConveyorRecolored && game.State == GameState.Playing)
            {
                tray.Refresh(game);
                for (int s = 0; s < 3; s++)
                    fx.Puff(tray.SlotWorld(BlockSource.Conveyor(s)), new Color(1f, 0.95f, 0.7f, 0.9f), 8, 2.2f, 0.35f);
                fx.Text("남은 블록이 필요한 색으로!", tray.Root.position + new Vector3(0f, 1.55f, 0f), UI.Accent, 0.36f, 1.2f);
                Sfx.Play(Sfx.Pop, 0.6f, 1.3f);
            }
            Finish(r);
        }

        float lastPulse;
        void PulsePicture()
        {
            if (Time.time - lastPulse < 0.07f) return;
            lastPulse = Time.time;
            var pr = picture.Root;
            Tween.Run(0.12f, k => pr.localScale = Vector3.one * (1f + 0.018f * Ease.Bump(k)));
            var pt = pctText.rectTransform;
            Tween.Run(0.14f, k => pt.localScale = Vector3.one * (1f + 0.12f * Ease.Bump(k)));
        }

        void Finish(MoveResult r)
        {
            busy = false;
            board.Render(game.Board);
            tray.Refresh(game);
            RenderHud();
            AfterState(r);
        }

        void AfterState(MoveResult r)
        {
            if (game.State == GameState.Won) StartCoroutine(WinSequence());
            else if (game.State == GameState.Lost) StartCoroutine(LoseSequence());
        }

        IEnumerator WinSequence()
        {
            busy = true;
            cleared.Add(game.Level.Id);
            SaveProgress();
            yield return new WaitForSeconds(0.25f);
            float d = picture.Celebrate();
            Sfx.Play(Sfx.Win, 0.9f);
            var colors = new List<Color>();
            for (int c = 1; c < palette.Length; c++) colors.Add(palette[c]);
            colors.Add(UI.Accent);
            fx.Confetti(new Vector3(0f, -cam.orthographicSize - 0.5f, 0f), colors.ToArray(), 90);
            yield return new WaitForSeconds(d + 0.5f);
            ShowWin();
        }

        IEnumerator LoseSequence()
        {
            busy = true;
            yield return new WaitForSeconds(0.2f);
            Sfx.Play(Sfx.Fail, 0.7f);
            Tween.Run(0.5f, k => board.Desaturate(k * 0.1f));
            fx.Shake(0.08f, 0.3f);
            yield return new WaitForSeconds(0.7f);
            ShowLose();
        }

        void DoUndo()
        {
            if (busy || undoLeft <= 0 || !game.CanUndo) return;
            if (game.Undo())
            {
                undoLeft--;
                combo = 0;
                RenderAll();
                Sfx.Play(Sfx.Pick, 0.6f, 0.7f);
            }
        }

        // ---------------- 오버레이 ----------------

        void HideOverlay()
        {
            for (int i = overlayRoot.childCount - 1; i >= 0; i--)
            {
                var ch = overlayRoot.GetChild(i);
                if (ch == overlayCanvas.transform) continue;
                Destroy(ch.gameObject);
            }
            for (int i = overlayCanvas.transform.childCount - 1; i >= 0; i--) Destroy(overlayCanvas.transform.GetChild(i).gameObject);
            overlayButtons.Clear();
        }

        Transform OpenOverlay(float cardH)
        {
            HideOverlay();
            float size = cam.orthographicSize;
            var dim = Gfx.MakeSprite("Dim", overlayRoot, Gfx.Pixel, UI.Dim, 80);
            dim.transform.localScale = new Vector3(size * cam.aspect * 2f + 2f, size * 2f + 2f, 1f);
            var card = UI.Panel("Card", overlayRoot, Vector2.zero, new Vector2(8.6f, cardH), Gfx.Hex("#242944"), 81);
            var ct = card.transform;
            Tween.Run(0.25f, k => ct.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, Ease.OutBack(k)));
            return overlayRoot;
        }

        WorldButton OverlayButton(string text, float y, bool primary, System.Action onClick)
        {
            var b = new WorldButton(overlayRoot, overlayCanvas, text, new Vector2(0f, y), new Vector2(6.6f, 1.1f), primary, 85);
            b.OnClick = onClick;
            overlayButtons.Add(b);
            return b;
        }

        void BigPicture(float y, float side, bool colored)
        {
            var sr = Gfx.MakeSprite("Thumb", overlayRoot, Gfx.PictureSprite(game.Level, colored), Color.white, 83);
            sr.transform.localPosition = new Vector3(0f, y, 0f);
            sr.transform.localScale = Vector3.one * side;
            UI.Panel("ThumbBg", overlayRoot, new Vector2(0f, y), new Vector2(side + 0.5f, side + 0.5f), UI.Canvas, 82);
            if (!colored)
            {
                // 칠한 만큼 색 입히기
                var lv = game.Level;
                float ps = side / Mathf.Max(lv.PictureWidth, lv.PictureHeight);
                for (int i = 0; i < lv.PictureWidth * lv.PictureHeight; i++)
                {
                    if (!game.Picture.IsPaintedIndex(i)) continue;
                    int c = lv.PixelAt(i % lv.PictureWidth, i / lv.PictureWidth);
                    var p = Gfx.MakeSprite("p", overlayRoot, Gfx.Pixel, palette[c], 84);
                    p.transform.localPosition = new Vector3(((i % lv.PictureWidth) - (lv.PictureWidth - 1) * 0.5f) * ps, y + ((lv.PictureHeight - 1) * 0.5f - i / lv.PictureWidth) * ps, 0f);
                    p.transform.localScale = Vector3.one * ps * 1.02f;
                }
            }
        }

        void ShowWin()
        {
            OpenOverlay(12.6f);
            UI.Label(overlayCanvas, game.Level.PictureName + " 완성!", new Vector2(0f, 5.3f), 0.8f, UI.Accent, TextAnchor.MiddleCenter, 8f, true);
            BigPicture(2.1f, 4.6f, true);
            UI.Label(overlayCanvas, game.MovesUsed + "수  ·  되돌리기 " + (3 - undoLeft) + "  ·  이어하기 " + game.ContinuesUsed,
                     new Vector2(0f, -0.9f), 0.3f, UI.Muted);
            bool hasNext = levelIndex < levels.Count - 1;
            if (hasNext) OverlayButton("다음 레벨", -2.6f, true, () => StartLevel(levelIndex + 1));
            else UI.Label(overlayCanvas, "앨범 1의 그림을 모두 완성했어요", new Vector2(0f, -2.6f), 0.34f, UI.Ink);
            OverlayButton("레벨 목록", -4.0f, false, ShowLevels);
            busy = false;
        }

        void ShowLose()
        {
            int total = game.Picture.TotalPixels;
            int pct = Mathf.RoundToInt((1f - game.Picture.RemainingPixels / (float)total) * 100f);
            OpenOverlay(13.2f);
            UI.Label(overlayCanvas, pct + "% 완성에서 멈췄어요", new Vector2(0f, 5.6f), 0.62f, UI.Ink, TextAnchor.MiddleCenter, 8f, true);
            BigPicture(2.5f, 4.2f, false);
            UI.Label(overlayCanvas, "이어하면 회색 칸 3개를 지우고 작은 블록 3개를 받아요", new Vector2(0f, -0.35f), 0.26f, UI.Muted);
            OverlayButton("이어하기", -1.9f, true, () =>
            {
                HideOverlay();
                game.Continue();
                busy = false;
                RenderAll();
                Sfx.Play(Sfx.Pop, 0.7f);
            });
            OverlayButton("처음부터", -3.3f, false, () => StartLevel(levelIndex));
            OverlayButton("레벨 목록", -4.7f, false, ShowLevels);
        }

        void ShowLevels()
        {
            if (busy && game.State == GameState.Playing) return;
            CancelDrag();
            OpenOverlay(14.4f);
            UI.Label(overlayCanvas, "동물 친구들", new Vector2(0f, 6.3f), 0.7f, UI.Ink, TextAnchor.MiddleCenter, 8f, true);
            UI.Label(overlayCanvas, "앨범 1 · 그림 " + cleared.Count + " / " + levels.Count, new Vector2(0f, 5.5f), 0.3f, UI.Muted);
            for (int i = 0; i < levels.Count; i++)
            {
                int col = i % 3, row = i / 3;
                if (i == 9) col = 1;
                var pos = new Vector2(-2.6f + col * 2.6f, 3.6f - row * 2.55f);
                var lv = levels[i];
                bool done = cleared.Contains(lv.Id);
                int idx = i;
                var b = new WorldButton(overlayRoot, overlayCanvas, "", pos, new Vector2(2.3f, 2.3f), false, 85);
                b.Bg.color = i == levelIndex ? Gfx.Hex("#3A4170") : UI.Button;
                b.OnClick = () => StartLevel(idx);
                overlayButtons.Add(b);
                var th = Gfx.MakeSprite("Thumb", overlayRoot, Gfx.PictureSprite(lv, done), Color.white, 87);
                th.transform.localPosition = new Vector3(pos.x, pos.y + 0.18f, 0f);
                th.transform.localScale = Vector3.one * 1.45f;
                var tb = UI.Panel("ThumbBg", overlayRoot, new Vector2(pos.x, pos.y + 0.18f), new Vector2(1.65f, 1.65f), UI.Canvas, 86);
                UI.Label(overlayCanvas, lv.Id + (lv.Difficulty == "hard" ? " 어려움" : ""), new Vector2(pos.x, pos.y - 0.86f), 0.24f,
                         lv.Difficulty == "hard" ? UI.Hard : UI.Ink);
            }
            OverlayButton("닫기", -6.3f, false, HideOverlay);
        }

        // ---------------- 캡처 (개발용) ----------------

        public void Capture(string path, int width = 1080, int height = 2340)
        {
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            float aspect = width / (float)height;
            Layout(aspect);
            cam.aspect = aspect;
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            cam.ResetAspect();
            RenderTexture.ReleaseTemporary(rt);
            Layout(cam.aspect);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToJPG(88));
            Destroy(tex);
        }

        // ---------------- 자동 데모 ----------------

        void DemoTick()
        {
            if (busy || game.State != GameState.Playing) return;
            demoTimer -= Time.deltaTime;
            if (demoTimer > 0f) return;
            demoTimer = 0.55f;
            var bot = new AutoSolver(Random.Range(0, 99999), 0);
            AutoSolver.Action a;
            if (!bot.ChooseAction(game, out a)) return;
            if (a.IsStash)
            {
                game.TryStash(a.StashIndex);
                tray.Refresh(game, a.StashIndex);
                AfterState(null);
                return;
            }
            busy = true;
            var block = game.GetBlock(a.Source);
            var fp = BuildPiece(block, 20);
            var from = tray.SlotWorld(a.Source);
            var to = board.CellWorld(BoardModel.Index(a.X, a.Y));
            tray.SetHidden(a.Source, true);
            board.ShowGhost(game.Board, block.Shape, block.Color, a.X, a.Y);
            var src = a.Source;
            int gx = a.X, gy = a.Y;
            Tween.Run(0.35f, k =>
            {
                fp.position = Vector3.Lerp(from, to, Ease.InOutCubic(k)) + Vector3.up * Ease.Bump(k) * 0.8f;
                fp.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, k);
            }, () =>
            {
                Destroy(fp.gameObject);
                var r = game.TryPlace(src, gx, gy);
                if (r == null) { busy = false; RenderAll(); return; }
                StartCoroutine(PlayMove(r, src));
            });
        }

#if UNITY_EDITOR
        // ---------------- Claude 개발 명령 (에디터 전용) ----------------
        float nextPoll;
        DevRunner devRunner;
        static string LogsDir { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs")); } }

        void PollDevCommands()
        {
            if (Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + 0.25f;
            var path = Path.Combine(LogsDir, "claude_game.txt");
            if (!File.Exists(path)) return;
            string[] lines;
            try { lines = File.ReadAllLines(path); File.Delete(path); }
            catch (System.Exception) { return; }
            if (devRunner == null) devRunner = new GameObject("DevRunner").AddComponent<DevRunner>();
            devRunner.StartCoroutine(RunDev(lines));
        }

        void Ack(string s)
        {
            try { File.AppendAllText(Path.Combine(LogsDir, "claude_game_ack.txt"), System.DateTime.Now.ToString("HH:mm:ss.f") + " " + s + "\n"); }
            catch (System.Exception) { }
        }

        IEnumerator RunDev(string[] lines)
        {
            foreach (var raw in lines)
            {
                var parts = raw.Trim().Split(' ');
                if (parts.Length == 0 || parts[0].Length == 0) continue;
                var cmd = parts[0];
                switch (cmd)
                {
                    case "level":
                        StartLevel(int.Parse(parts[1]) - 1);
                        break;
                    case "demo":
                        demo = parts.Length < 2 || parts[1] != "off";
                        demoTimer = 0.3f;
                        break;
                    case "speed":
                        BaseTimeScale = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                        Time.timeScale = BaseTimeScale;
                        break;
                    case "wait":
                        yield return new WaitForSecondsRealtime(float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case "shot":
                        yield return new WaitForEndOfFrame();
                        Capture(Path.Combine(LogsDir, "shots", parts[1] + ".jpg"));
                        break;
                    case "shots":
                    {
                        // shots 이름 개수 간격
                        int count = int.Parse(parts[2]);
                        float gap = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                        for (int i = 0; i < count; i++)
                        {
                            yield return new WaitForEndOfFrame();
                            Capture(Path.Combine(LogsDir, "shots", parts[1] + "_" + i.ToString("00") + ".jpg"));
                            yield return new WaitForSecondsRealtime(gap);
                        }
                        break;
                    }
                    case "place":
                    {
                        // place 슬롯 x y  (슬롯 h = 보관함)
                        var src = parts[1] == "h" ? BlockSource.Held(0) : BlockSource.Conveyor(int.Parse(parts[1]));
                        int x = int.Parse(parts[2]), y = int.Parse(parts[3]);
                        if (!busy && game.CanPlace(src, x, y))
                        {
                            var r = game.TryPlace(src, x, y);
                            StartCoroutine(PlayMove(r, src));
                        }
                        else Ack("놓을 수 없음");
                        break;
                    }
                    case "undo": DoUndo(); break;
                    case "fillrow":
                    {
                        int y = int.Parse(parts[1]), col = int.Parse(parts[2]), gap = int.Parse(parts[3]);
                        for (int x = 0; x < 8; x++) if (x != gap) game.Board[x, y] = col;
                        board.Render(game.Board);
                        break;
                    }
                    case "fillcol":
                    {
                        int x = int.Parse(parts[1]), col = int.Parse(parts[2]), gap = int.Parse(parts[3]);
                        for (int y = 0; y < 8; y++) if (y != gap) game.Board[x, y] = col;
                        board.Render(game.Board);
                        break;
                    }
                    case "give":
                    {
                        var list = new List<Block>();
                        for (int i = 1; i + 1 < parts.Length; i += 2) list.Add(new Block(parts[i], int.Parse(parts[i + 1])));
                        game.Conveyor.ReplaceFront(list);
                        tray.Refresh(game);
                        break;
                    }
                    case "ghost":
                    {
                        var src = parts[1] == "h" ? BlockSource.Held(0) : BlockSource.Conveyor(int.Parse(parts[1]));
                        var b = game.GetBlock(src);
                        board.ShowGhost(game.Board, b.Shape, b.Color, int.Parse(parts[2]), int.Parse(parts[3]));
                        break;
                    }
                    case "menu": ShowLevels(); break;
                    case "close": HideOverlay(); break;
                    case "continue":
                        if (game.State == GameState.Lost) { HideOverlay(); game.Continue(); busy = false; RenderAll(); }
                        break;
                    case "until":
                    {
                        // until Won|Lost 최대초
                        float limit = Time.realtimeSinceStartup + float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                        while (game.State.ToString() != parts[1] && Time.realtimeSinceStartup < limit) yield return null;
                        break;
                    }
                    case "state":
                        Ack("level " + game.Level.Id + " state " + game.State + " moves " + game.MovesUsed + " left " + game.Picture.RemainingPixels + " busy " + busy);
                        break;
                    case "reset":
                        PlayerPrefs.DeleteAll();
                        LoadProgress();
                        break;
                }
                Ack("완료 " + raw.Trim());
            }
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>개발 명령 코루틴 전용 (레벨 재시작 때 같이 멈추지 않도록 분리)</summary>
    public sealed class DevRunner : MonoBehaviour { }
#endif
}
