using System.Collections.Generic;
using BlockAtelier.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAtelier.Game
{
    /// <summary>
    /// 게임 밖 화면: 홈(이어하기 + 앨범 책장), 마이페이지(닉네임, 대표 그림, 기록), 설정.
    /// 게임 화면(top/middle/bottom 묶음)과 같은 카메라를 쓰고, 보이는 동안 게임 묶음은 꺼 둔다.
    /// 화면 내용은 가운데 원점 기준으로 짓고, 안전 영역 안에 여백을 두고 통째로 맞춘다(FitScreen).
    /// </summary>
    public sealed partial class GameRoot
    {
        enum Page { Game, Home, MyPage, Settings, Avatar }

        Page screen = Page.Game;
        Transform screenRoot;
        Canvas screenCanvas;
        readonly List<WorldButton> screenButtons = new List<WorldButton>();
        float screenW = 10f, screenH = 20f;

        PlayerProfile profile;
        GameSettings settings;
        TouchScreenKeyboard keyboard;
        string nickMessage;
        bool nickMessageIsError;

        const string ProfileKey = "ba_profile", SettingsKey = "ba_settings";
        const float Gap = 0.22f;
        static readonly Color CardColor = Gfx.Hex("#242944");
        static readonly Color CardDim = Gfx.Hex("#1C2033");

        // ---------------- 준비와 저장 ----------------

        void InitScreens()
        {
            screenRoot = Group("Screen");
            screenCanvas = UI.WorldCanvas("ScreenText", screenRoot, 30);

            profile = PlayerProfile.FromJson(PlayerPrefs.GetString(ProfileKey, ""), Random.Range(1, 1000000));
            settings = GameSettings.FromJson(PlayerPrefs.GetString(SettingsKey, ""));
            if (!PlayerPrefs.HasKey(SettingsKey) && PlayerPrefs.HasKey("ba_mute"))
                settings.Sound = PlayerPrefs.GetInt("ba_mute", 0) == 0;   // 예전 소리 설정 옮기기
            ApplySettings();
            SaveProfile();
        }

        void SaveProfile()
        {
            PlayerPrefs.SetString(ProfileKey, profile.ToJson());
            PlayerPrefs.Save();
        }

        void SaveSettings()
        {
            PlayerPrefs.SetString(SettingsKey, settings.ToJson());
            PlayerPrefs.Save();
        }

        void ApplySettings()
        {
            Sfx.Muted = !settings.Sound;
            Fx.ReduceMotion = settings.ReduceMotion;
        }

        // ---------------- 화면 전환 ----------------

        void ShowScreen(Page s)
        {
            HideOverlay();
            CancelDrag();
            HideHand();
            demo = false;
            screen = s;
            bool game = s == Page.Game;
            top.gameObject.SetActive(game);
            middle.gameObject.SetActive(game);
            bottom.gameObject.SetActive(game);
            BuildScreen();
            if (!game)
            {
                // 살짝 떠오르며 등장
                var r = screenRoot;
                var baseScale = r.localScale;
                Tween.Run(0.22f, k => r.localScale = baseScale * Mathf.LerpUnclamped(0.97f, 1f, Ease.OutCubic(k)));
            }
        }

        void ClearScreen()
        {
            for (int i = screenRoot.childCount - 1; i >= 0; i--)
            {
                var ch = screenRoot.GetChild(i);
                if (ch == screenCanvas.transform) continue;
                ch.gameObject.SetActive(false);
                Destroy(ch.gameObject);
            }
            for (int i = screenCanvas.transform.childCount - 1; i >= 0; i--)
            {
                var ch = screenCanvas.transform.GetChild(i).gameObject;
                ch.SetActive(false);
                Destroy(ch);
            }
            screenButtons.Clear();
        }

        void BuildScreen()
        {
            ClearScreen();
            switch (screen)
            {
                case Page.Home: BuildHome(); break;
                case Page.MyPage: BuildMyPage(); break;
                case Page.Settings: BuildSettings(); break;
                case Page.Avatar: BuildAvatarEditor(); break;
            }
            FitScreen();
        }

        /// <summary>화면 내용이 안전 영역 안에 여백을 두고 들어가도록 크기를 맞춘다 (창과 같은 방식).</summary>
        void FitScreen()
        {
            if (screenRoot == null) return;
            float size = cam.orthographicSize;
            var ins = SafeInsets();
            float safeH = 2f * size * (1f - ins.x - ins.y), safeW = 2f * size * cam.aspect;
            float padV = Mathf.Max(0.5f, safeH * 0.03f), padH = Mathf.Max(0.4f, safeW * 0.03f);
            float s = Mathf.Min(1.25f, (safeH - 2f * padV) / screenH, (safeW - 2f * padH) / screenW);
            screenRoot.localScale = Vector3.one * s;
            // 위쪽 정렬: 머리(제목, 뒤로)가 항상 같은 자리에 오도록 안전 영역 맨 위에 붙인다
            float safeTop = size - 2f * size * ins.x;
            screenRoot.localPosition = new Vector3(0f, safeTop - padV - screenH * s * 0.5f, 0f);
        }

        WorldButton ScreenButton(string text, Vector2 pos, Vector2 size, bool primary, int order, System.Action onClick)
        {
            var b = new WorldButton(screenRoot, screenCanvas, text, pos, size, primary, order);
            b.OnClick = onClick;
            screenButtons.Add(b);
            return b;
        }

        Text ScreenLabel(string text, Vector2 pos, float size, Color color, TextAnchor align = TextAnchor.MiddleCenter, float width = 8f, bool bold = false)
        {
            return UI.Label(screenCanvas, text, pos, size, color, align, width, bold);
        }

        SpriteRenderer ScreenSprite(string name, Sprite sprite, Vector2 pos, float scale, Color color, int order)
        {
            var sr = Gfx.MakeSprite(name, screenRoot, sprite, color, order);
            sr.transform.localPosition = pos;
            sr.transform.localScale = Vector3.one * scale;
            return sr;
        }

        void StarRow(Vector2 left, float size, int stars, int order)
        {
            for (int i = 0; i < 3; i++)
                ScreenSprite("Star", Gfx.Star, left + new Vector2(i * size * 1.15f, 0f), size, i < stars ? StarOn : StarOff, order);
        }

        /// <summary>뒤로 버튼 + 화면 제목</summary>
        void ScreenHeader(string title, float y)
        {
            float x0 = -screenW * 0.5f;
            ScreenButton("<", new Vector2(x0 + 0.55f, y), new Vector2(1.1f, 1.1f), false, 22, () => ShowScreen(Page.Home));
            ScreenLabel(title, new Vector2(x0 + 1.45f, y), 0.52f, UI.Ink, TextAnchor.MiddleLeft, 7f, true);
        }

        void ScreenToast(string text, float y)
        {
            var bg = UI.Panel("Toast", screenRoot, new Vector2(0f, y), new Vector2(Mathf.Min(screenW, 8f), 0.95f), Gfx.Hex("#151827"), 28);
            var t = ScreenLabel(text, new Vector2(0f, y), 0.3f, UI.Accent, TextAnchor.MiddleCenter, 8f, true);
            var bgc = bg.color;
            Tween.Run(1.8f, k =>
            {
                if (t == null || bg == null) return;
                float a = k < 0.75f ? 1f : 1f - (k - 0.75f) / 0.25f;
                var c = t.color; c.a = a; t.color = c;
                bg.color = new Color(bgc.r, bgc.g, bgc.b, 0.95f * a);
            }, () => { if (t != null) Destroy(t.gameObject); if (bg != null) Destroy(bg.gameObject); });
        }

        // ---------------- 홈 ----------------

        /// <summary>
        /// 홈: 제목과 별 합계, 이어서 그릴 그림 카드, 앨범 책장.
        /// 폰은 책장 3열, 가로로 넓은 화면은 5열로 한눈에 보인다.
        /// </summary>
        void BuildHome()
        {
            var albums = AlbumSummary.Build(levels, progress);
            int cols = wideLayout ? 5 : 3;
            int rows = (albums.Count + cols - 1) / cols;
            const float cw = 3.05f, ch = 3.1f;
            screenW = cols * cw + (cols - 1) * Gap;
            float gridTop = -5.9f;
            float contentBottom = gridTop - rows * ch - (rows - 1) * Gap;
            screenH = -contentBottom + 0.3f;
            float yTop = screenH * 0.5f;       // 내용 맨 위
            float x0 = -screenW * 0.5f, x1 = screenW * 0.5f;

            // 머리: 제목, 마이페이지, 설정
            ScreenLabel("블록 아틀리에", new Vector2(x0, yTop - 0.65f), 0.62f, UI.Ink, TextAnchor.MiddleLeft, 7f, true);
            int cleared = progress.ClearedCount;
            ScreenLabel("별 " + progress.TotalStars + " / " + levels.Count * 3 + "   ·   그림 " + cleared + " / " + levels.Count,
                        new Vector2(x0, yTop - 1.4f), 0.28f, UI.Muted, TextAnchor.MiddleLeft, 7f);
            var setBtn = ScreenButton("", new Vector2(x1 - 0.55f, yTop - 0.7f), new Vector2(1.1f, 1.1f), false, 22, () => ShowScreen(Page.Settings));
            ScreenSprite("Gear", Gfx.Gear, setBtn.Bg.transform.localPosition, 0.62f, UI.Ink, 24);
            var meBtn = ScreenButton("", new Vector2(x1 - 1.85f, yTop - 0.7f), new Vector2(1.1f, 1.1f), false, 22, () => ShowScreen(Page.MyPage));
            AvatarBadge(meBtn.Bg.transform.localPosition, 0.9f, 24);

            // 이어서 그리기 카드
            int front = Mathf.Clamp(progress.FrontierLevel(levels.Count) - 1, 0, levels.Count - 1);
            bool allDone = cleared >= levels.Count;
            var lv = levels[front];
            var rec = progress.Get(lv.Id);
            float cy = yTop - 3.5f, cardH = 3.0f;
            var card = ScreenButton("", new Vector2(0f, cy), new Vector2(screenW, cardH), false, 20, ContinueFromHome);
            card.Bg.color = Gfx.Hex("#2E3560");
            UI.Panel("ThumbBg", screenRoot, new Vector2(x0 + 1.45f, cy), new Vector2(2.45f, 2.45f), UI.Canvas, 21);
            ScreenSprite("Thumb", Gfx.PictureSprite(lv, rec.Cleared), new Vector2(x0 + 1.45f, cy), 2.1f, Color.white, 22);
            bool resuming = CanResume(front);
            ScreenLabel(allDone ? "모든 그림을 완성했어요" : resuming ? "그리던 그림" : "이어서 그리기",
                        new Vector2(x0 + 3.0f, cy + 0.85f), 0.26f, UI.Accent, TextAnchor.MiddleLeft, 6f, true);
            ScreenLabel(lv.Id + "  " + lv.PictureName, new Vector2(x0 + 3.0f, cy + 0.22f), 0.5f, UI.Ink, TextAnchor.MiddleLeft, 6f, true);
            ScreenLabel(lv.AlbumTitle + (lv.Difficulty == "hard" ? "  ·  <color=#FF6B6B>어려움</color>" : ""),
                        new Vector2(x0 + 3.0f, cy - 0.38f), 0.26f, UI.Muted, TextAnchor.MiddleLeft, 6f).supportRichText = true;
            StarRow(new Vector2(x0 + 3.2f, cy - 0.95f), 0.36f, rec.Stars, 23);
            ScreenButton(resuming ? "계속" : "시작", new Vector2(x1 - 1.35f, cy - 0.7f), new Vector2(2.2f, 0.95f), true, 23, ContinueFromHome);

            // 앨범 책장
            ScreenLabel("앨범", new Vector2(x0, yTop - 5.45f), 0.34f, UI.Ink, TextAnchor.MiddleLeft, 4f, true);
            for (int i = 0; i < albums.Count; i++)
            {
                int c = i % cols, r = i / cols;
                var pos = new Vector2(x0 + cw * 0.5f + c * (cw + Gap), yTop + gridTop - ch * 0.5f - r * (ch + Gap));
                AlbumCard(albums[i], i, pos, cw, ch);
            }
        }

        void AlbumCard(AlbumSummary a, int index, Vector2 pos, float w, float h)
        {
            int page = index;
            var b = ScreenButton("", pos, new Vector2(w, h), false, 20, null);
            if (!a.Unlocked)
            {
                b.Bg.color = CardDim;
                var bt = b.Bg.transform;
                b.OnClick = () =>
                {
                    var p0 = bt.localPosition;
                    Tween.Run(0.3f, q => bt.localPosition = p0 + Vector3.right * Mathf.Sin(q * 30f) * 0.06f * (1f - q), () => bt.localPosition = p0);
                    ScreenToast("앞 앨범을 완성하면 열려요", pos.y);
                };
                ScreenSprite("Lock", Gfx.Lock, pos + new Vector2(0f, 0.35f), 0.95f, new Color(1f, 1f, 1f, 0.28f), 22);
                ScreenLabel(a.Title, pos + new Vector2(0f, -0.95f), 0.26f, UI.Muted, TextAnchor.MiddleCenter, w, true);
                return;
            }
            b.OnClick = () => { albumPage = page; ShowLevels(); };
            if (a.Complete) b.Bg.color = Gfx.Hex("#3A3A28");
            var cover = levels[a.CoverIndex];
            UI.Panel("CoverBg", screenRoot, pos + new Vector2(0f, 0.4f), new Vector2(1.95f, 1.95f), UI.Canvas, 21);
            ScreenSprite("Cover", Gfx.PictureSprite(cover, a.CoverCleared), pos + new Vector2(0f, 0.4f), 1.7f, Color.white, 22);
            ScreenLabel(a.Title, pos + new Vector2(0f, -0.85f), 0.26f, UI.Ink, TextAnchor.MiddleCenter, w, true);
            ScreenLabel(a.Cleared + "/" + a.Count, pos + new Vector2(-w * 0.5f + 0.25f, -1.28f), 0.22f, a.Complete ? UI.Accent : UI.Muted, TextAnchor.MiddleLeft, 1.4f, a.Complete);
            ScreenSprite("Star", Gfx.Star, pos + new Vector2(w * 0.5f - 1.05f, -1.28f), 0.3f, StarOn, 22);
            ScreenLabel(a.Stars.ToString(), pos + new Vector2(w * 0.5f - 0.8f, -1.28f), 0.22f, UI.Muted, TextAnchor.MiddleLeft, 0.8f);
        }

        /// <summary>프로필: 픽셀 캐릭터를 캔버스색 둥근 판 위에</summary>
        void AvatarBadge(Vector3 pos, float size, int order)
        {
            UI.Panel("AvatarBg", screenRoot, pos, new Vector2(size, size), UI.Canvas, order);
            ScreenSprite("Avatar", Gfx.AvatarSprite(profile.Look), pos, size * 0.86f, Color.white, order + 1);
        }

        /// <summary>그리던 판(같은 레벨, 한 수 이상 둠, 아직 진행 중)이 있으면 그대로 돌아간다.</summary>
        bool CanResume(int index)
        {
            return game != null && levelIndex == index && game.State == GameState.Playing && game.MovesUsed > 0;
        }

        void ContinueFromHome()
        {
            int front = Mathf.Clamp(progress.FrontierLevel(levels.Count) - 1, 0, levels.Count - 1);
            if (CanResume(front)) { ShowScreen(Page.Game); RenderAll(); return; }
            StartLevel(front);
        }

        void GoHome()
        {
            ShowScreen(Page.Home);
        }

        // ---------------- 마이페이지 ----------------

        void BuildMyPage()
        {
            screenW = 9.6f;
            screenH = 15.2f;
            float yTop = screenH * 0.5f, x0 = -screenW * 0.5f;
            ScreenHeader("마이페이지", yTop - 0.6f);

            // 픽셀 캐릭터
            float ay = yTop - 3.3f;
            AvatarBadge(new Vector3(0f, ay, 0f), 3.3f, 22);
            ScreenButton("꾸미기", new Vector2(0f, ay - 2.2f), new Vector2(2.4f, 0.8f), false, 22, () => ShowScreen(Page.Avatar));

            // 닉네임
            float ny = yTop - 6.55f;
            ScreenLabel(profile.Nickname, new Vector2(0f, ny), 0.62f, UI.Ink, TextAnchor.MiddleCenter, 9f, true);
            ScreenButton("이름 바꾸기", new Vector2(-1.5f, ny - 1.05f), new Vector2(2.8f, 0.9f), true, 22, OpenNicknameKeyboard);
            ScreenButton("추천 이름", new Vector2(1.5f, ny - 1.05f), new Vector2(2.8f, 0.9f), false, 22, () =>
            {
                profile.Nickname = Nickname.Suggest(Random.Range(1, 1000000));
                SaveProfile();
                nickMessage = "새 이름: " + profile.Nickname;
                nickMessageIsError = false;
                BuildScreen();
            });
            string hint = nickMessage ?? "2~10자, 한글·영문·숫자";
            ScreenLabel(hint, new Vector2(0f, ny - 1.85f), 0.24f, nickMessageIsError ? UI.Hard : UI.Muted);

            // 기록
            var albums = AlbumSummary.Build(levels, progress);
            int doneAlbums = 0;
            foreach (var a in albums) if (a.Complete) doneAlbums++;
            int three = 0;
            foreach (var kv in progress.All) if (kv.Value.Stars >= 3) three++;
            float sy = yTop - 10.05f, bw = (screenW - 2 * Gap) / 3f;
            StatBox(new Vector2(x0 + bw * 0.5f, sy), bw, progress.ClearedCount + "/" + levels.Count, "완성한 그림");
            StatBox(new Vector2(0f, sy), bw, progress.TotalStars + "/" + levels.Count * 3, "모은 별");
            StatBox(new Vector2(-x0 - bw * 0.5f, sy), bw, doneAlbums + "/" + albums.Count, "완성한 앨범");
            ScreenLabel("별 3개로 완성한 그림 " + three + "장", new Vector2(0f, sy - 1.2f), 0.24f, UI.Muted);

            // 계정
            float gy = yTop - 13.35f;
            UI.Panel("Account", screenRoot, new Vector2(0f, gy), new Vector2(screenW, 1.9f), CardColor, 20);
            ScreenLabel("게스트로 플레이 중", new Vector2(x0 + 0.4f, gy + 0.4f), 0.32f, UI.Ink, TextAnchor.MiddleLeft, 6f, true);
            ScreenLabel("기록은 이 기기에만 저장돼요.", new Vector2(x0 + 0.4f, gy - 0.1f), 0.22f, UI.Muted, TextAnchor.MiddleLeft, 6f);
            ScreenLabel("계정 연결은 곧 지원돼요.", new Vector2(x0 + 0.4f, gy - 0.5f), 0.22f, UI.Muted, TextAnchor.MiddleLeft, 6f);
            var link = ScreenButton("연결 (준비 중)", new Vector2(-x0 - 1.55f, gy), new Vector2(2.7f, 0.85f), false, 22, null);
            link.SetEnabled(false);
        }

        void StatBox(Vector2 pos, float w, string value, string label)
        {
            UI.Panel("Stat", screenRoot, pos, new Vector2(w, 1.7f), CardColor, 20);
            ScreenLabel(value, pos + new Vector2(0f, 0.25f), 0.46f, UI.Ink, TextAnchor.MiddleCenter, w, true);
            ScreenLabel(label, pos + new Vector2(0f, -0.45f), 0.22f, UI.Muted, TextAnchor.MiddleCenter, w);
        }

        /// <summary>폰에서는 시스템 키보드를 연다. 에디터는 키보드가 없어서 개발 명령(nick 이름)으로 시험한다.</summary>
        void OpenNicknameKeyboard()
        {
            if (!TouchScreenKeyboard.isSupported)
            {
                nickMessage = "기기에서는 키보드가 열려요 (에디터: nick 이름)";
                nickMessageIsError = false;
                BuildScreen();
                return;
            }
            keyboard = TouchScreenKeyboard.Open(profile.Nickname, TouchScreenKeyboardType.Default, false, false, false, false,
                                                "닉네임 (2~10자)", Nickname.MaxLength);
        }

        /// <summary>Update에서 부른다: 키보드 입력이 끝나면 규칙 검사 후 저장</summary>
        void PollKeyboard()
        {
            if (keyboard == null) return;
            var st = keyboard.status;
            if (st == TouchScreenKeyboard.Status.Visible) return;
            if (st == TouchScreenKeyboard.Status.Done) ApplyNickname(keyboard.text);
            keyboard = null;
        }

        void ApplyNickname(string text)
        {
            string err = profile.TrySetNickname(text);
            if (err == null) { SaveProfile(); nickMessage = "이름을 바꿨어요"; nickMessageIsError = false; }
            else { nickMessage = err; nickMessageIsError = true; }
            if (screen == Page.MyPage) BuildScreen();
        }

        // ---------------- 캐릭터 꾸미기 ----------------

        static readonly string[] PartLabels = { "피부", "머리 모양", "머리 색", "눈", "입", "옷", "옷 색", "장식" };

        /// <summary>큰 미리보기 + 부위마다 &lt; 값 &gt; 한 줄. 바꾸는 즉시 저장.</summary>
        void BuildAvatarEditor()
        {
            screenW = 9.6f;
            const float rowH = 0.95f;
            screenH = 7.4f + PartLabels.Length * (rowH + 0.14f) + 1.3f;
            float yTop = screenH * 0.5f, x0 = -screenW * 0.5f;
            ScreenButton("<", new Vector2(x0 + 0.55f, yTop - 0.6f), new Vector2(1.1f, 1.1f), false, 22, () => ShowScreen(Page.MyPage));
            ScreenLabel("캐릭터 꾸미기", new Vector2(x0 + 1.45f, yTop - 0.6f), 0.52f, UI.Ink, TextAnchor.MiddleLeft, 7f, true);
            AvatarBadge(new Vector3(0f, yTop - 3.9f, 0f), 4.6f, 22);

            float y = yTop - 7.0f;
            var look = profile.Look;
            for (int part = 0; part < PartLabels.Length; part++)
            {
                int pp = part;
                UI.Panel("Row", screenRoot, new Vector2(0f, y), new Vector2(screenW, rowH), CardColor, 20);
                ScreenLabel(PartLabels[part], new Vector2(x0 + 0.35f, y), 0.3f, UI.Ink, TextAnchor.MiddleLeft, 3f, true);
                int v = look.Get(part), n = look.PartCount(part);
                float cx = 1.3f;
                ScreenButton("<", new Vector2(cx - 2.25f, y), new Vector2(0.8f, 0.72f), false, 22, () => ChangePart(pp, -1));
                ScreenButton(">", new Vector2(cx + 2.25f, y), new Vector2(0.8f, 0.72f), false, 22, () => ChangePart(pp, +1));
                string[] colors = part == 0 ? AvatarSpec.SkinColors : part == 2 ? AvatarSpec.HairColors : part == 6 ? AvatarSpec.OutfitColors : null;
                if (colors != null)
                {
                    UI.Panel("Swatch", screenRoot, new Vector2(cx - 0.45f, y), new Vector2(1.3f, 0.52f), Gfx.Hex(colors[v]), 21);
                    ScreenLabel((v + 1) + "/" + n, new Vector2(cx + 0.95f, y), 0.24f, UI.Muted, TextAnchor.MiddleCenter, 1.2f);
                }
                else
                {
                    string[] names = part == 1 ? AvatarSpec.HairNames : part == 3 ? AvatarSpec.EyeNames : part == 4 ? AvatarSpec.MouthNames
                                   : part == 5 ? AvatarSpec.OutfitNames : AvatarSpec.AccessoryNames;
                    ScreenLabel(names[v], new Vector2(cx, y), 0.28f, UI.Ink, TextAnchor.MiddleCenter, 3.4f, true);
                }
                y -= rowH + 0.14f;
            }
            ScreenButton("무작위", new Vector2(-1.5f, y - 0.35f), new Vector2(2.8f, 0.9f), false, 22, () =>
            {
                profile.Look = AvatarSpec.Random(Random.Range(1, 1000000));
                SaveProfile();
                BuildScreen();
            });
            ScreenButton("완료", new Vector2(1.5f, y - 0.35f), new Vector2(2.8f, 0.9f), true, 22, () => ShowScreen(Page.MyPage));
        }

        void ChangePart(int part, int dir)
        {
            profile.Look = profile.Look.With(part, profile.Look.Get(part) + dir);
            SaveProfile();
            BuildScreen();
        }

        // ---------------- 설정 ----------------

        void BuildSettings()
        {
            screenW = 9.6f;
            screenH = 12.4f;
            float yTop = screenH * 0.5f;
            ScreenHeader("설정", yTop - 0.6f);
            float y = yTop - 2.0f, rh = 1.35f;
            SettingRow(y, "소리", "효과음", settings.Sound, () => { settings.Sound = !settings.Sound; ApplySettings(); SaveSettings(); BuildScreen(); });
            y -= rh + Gap;
            SettingRow(y, "효과 줄이기", "화면 흔들림과 번쩍임을 꺼요", settings.ReduceMotion,
                       () => { settings.ReduceMotion = !settings.ReduceMotion; ApplySettings(); SaveSettings(); BuildScreen(); });
            y -= rh + Gap;
            ActionRow(y, "튜토리얼 다시 보기", "1레벨에서 놓는 법을 다시 보여 줘요", "보기", false, () =>
            {
                StartLevel(0);
                Tween.After(0.9f, ShowHand);
            });
            y -= rh + Gap;
            ActionRow(y, "진행 초기화", "모든 별과 기록이 지워져요", "초기화", false, ConfirmReset);
            y -= rh + Gap;
            ScreenLabel("블록 아틀리에  ·  버전 " + Application.version, new Vector2(0f, y - 0.2f), 0.24f, UI.Muted);
        }

        void SettingRow(float y, string title, string sub, bool on, System.Action toggle)
        {
            float x0 = -screenW * 0.5f;
            UI.Panel("Row", screenRoot, new Vector2(0f, y), new Vector2(screenW, 1.35f), CardColor, 20);
            ScreenLabel(title, new Vector2(x0 + 0.4f, y + 0.2f), 0.34f, UI.Ink, TextAnchor.MiddleLeft, 6f, true);
            ScreenLabel(sub, new Vector2(x0 + 0.4f, y - 0.3f), 0.22f, UI.Muted, TextAnchor.MiddleLeft, 6f);
            ScreenButton(on ? "켜짐" : "꺼짐", new Vector2(-x0 - 1.25f, y), new Vector2(2.0f, 0.8f), on, 22, toggle);
        }

        void ActionRow(float y, string title, string sub, string button, bool primary, System.Action act)
        {
            float x0 = -screenW * 0.5f;
            UI.Panel("Row", screenRoot, new Vector2(0f, y), new Vector2(screenW, 1.35f), CardColor, 20);
            ScreenLabel(title, new Vector2(x0 + 0.4f, y + 0.2f), 0.34f, UI.Ink, TextAnchor.MiddleLeft, 6f, true);
            ScreenLabel(sub, new Vector2(x0 + 0.4f, y - 0.3f), 0.22f, UI.Muted, TextAnchor.MiddleLeft, 6f);
            ScreenButton(button, new Vector2(-x0 - 1.25f, y), new Vector2(2.0f, 0.8f), primary, 22, act);
        }

        /// <summary>지우기 전에 한 번 더 묻는다 (되돌릴 수 없음)</summary>
        void ConfirmReset()
        {
            OpenOverlay(6.4f);
            UI.Label(overlayCanvas, "진행을 초기화할까요?", new Vector2(0f, 2.0f), 0.5f, UI.Ink, TextAnchor.MiddleCenter, 8f, true);
            UI.Label(overlayCanvas, "별 " + progress.TotalStars + "개와 모든 기록이 지워지고 되돌릴 수 없어요.", new Vector2(0f, 1.1f), 0.26f, UI.Muted);
            var del = OverlayButton("지우기", -0.4f, false, () =>
            {
                progress = new Progress();
                SaveProgress();
                StartLevel(0);
                ShowScreen(Page.Settings);
                ScreenToast("초기화했어요", 0f);
            });
            del.Bg.color = UI.Hard;
            OverlayButton("취소", -1.8f, false, HideOverlay);
        }
    }
}
