using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TripleTriadApp
{
    public enum CardOwner { None, Player, AI }

    public class Card
    {
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public int Left { get; set; }
        public CardOwner Owner { get; set; }

        public Card(int t, int r, int b, int l, CardOwner owner)
        {
            Top = t; Right = r; Bottom = b; Left = l; Owner = owner;
        }
    }

    public static class GameStats
    {
        public static int Wins = 0;
        public static int Losses = 0;
        public static int Draws = 0;
        private static readonly string FilePath = "triple_triad_stats.txt";

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                string[] lines = File.ReadAllLines(FilePath);
                if (lines.Length >= 3)
                {
                    int.TryParse(lines[0], out Wins);
                    int.TryParse(lines[1], out Losses);
                    int.TryParse(lines[2], out Draws);
                }
            }
            catch { }
        }

        public static void Save()
        {
            try { File.WriteAllLines(FilePath, new[] { Wins.ToString(), Losses.ToString(), Draws.ToString() }); }
            catch { }
        }

        public static void Reset() { Wins = Losses = Draws = 0; Save(); }
    }

    public partial class Form1 : Form
    {
        // Палитра
        private static readonly Color C_BG = Color.FromArgb(13, 13, 18);
        private static readonly Color C_SURFACE = Color.FromArgb(22, 22, 30);
        private static readonly Color C_GOLD = Color.FromArgb(255, 200, 50);
        private static readonly Color C_GOLD_DARK = Color.FromArgb(180, 130, 20);
        private static readonly Color C_BLUE = Color.FromArgb(52, 140, 220);
        private static readonly Color C_BLUE_DARK = Color.FromArgb(25, 80, 155);
        private static readonly Color C_RED = Color.FromArgb(220, 55, 55);
        private static readonly Color C_RED_DARK = Color.FromArgb(140, 25, 25);
        private static readonly Color C_GREEN = Color.FromArgb(40, 200, 110);
        private static readonly Color C_BORDER = Color.FromArgb(55, 55, 75);
        private static readonly Color C_TEXT = Color.FromArgb(230, 230, 240);
        private static readonly Color C_TEXT_DIM = Color.FromArgb(130, 130, 150);

        private const int CARD_W = 110;
        private const int CARD_H = 110;

        private Panel pnlMenu;
        private Panel pnlGame;

        private Label lblWins, lblLosses, lblDraws, lblWinRate;

        private Button[,] boardButtons = new Button[3, 3];
        private Card[,] boardCards = new Card[3, 3];
        private List<Card> playerHand = new List<Card>();
        private List<Card> aiHand = new List<Card>();
        private List<Button> playerButtons = new List<Button>();
        private List<Button> aiButtons = new List<Button>();

        private Card selectedCard = null;
        private Button selectedCardButton = null;
        private bool isPlayerTurn = true;

        private Label lblStatus;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            GameStats.Load();
            BuildMenuPanel();
            ShowMainMenu();
        }

        // ГОЛОВНЕ МЕНЮ
        private void BuildMenuPanel()
        {
            pnlMenu = new Panel { Dock = DockStyle.Fill, BackColor = C_BG };
            pnlMenu.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
            };
            this.Controls.Add(pnlMenu);

            var lblTitle = new Label
            {
                Text = "TRIPLE TRIAD",
                Font = new Font("Megrim", 48, FontStyle.Italic),
                ForeColor = C_GOLD,
                Location = new Point(55, 45),
                Size = new Size(500, 70),
                BackColor = Color.Transparent
            };
            pnlMenu.Controls.Add(lblTitle);

            // Блок статистики
            var pnlStats = new Panel { Location = new Point(55, 165), Size = new Size(380, 200), BackColor = C_SURFACE };
            pnlStats.Paint += (s, e) => DrawPanelBorder(e.Graphics, pnlStats, C_BLUE);
            pnlMenu.Controls.Add(pnlStats);

            pnlStats.Controls.Add(MakeLabel("СТАТИСТИКА", C_GOLD, new Font("Segoe UI", 10, FontStyle.Bold), new Point(18, 14)));
            lblWins = MakeLabel("Перемоги:   0", C_GREEN, new Font("Consolas", 12), new Point(18, 48));
            lblLosses = MakeLabel("Поразки:     0", C_RED, new Font("Consolas", 12), new Point(18, 82));
            lblDraws = MakeLabel("Нічиї:       0", C_TEXT_DIM, new Font("Consolas", 12), new Point(18, 116));
            lblWinRate = MakeLabel("Вінрейт: 0.0%", C_GOLD, new Font("Consolas", 13, FontStyle.Bold), new Point(18, 155));
            pnlStats.Controls.AddRange(new Control[] { lblWins, lblLosses, lblDraws, lblWinRate });

            var btnPlay = MakeMenuBtn("▶   ГРАТИ", new Point(55, 395), C_BLUE, C_BLUE_DARK);
            var btnReset = MakeMenuBtn("↺   СКИНУТИ СТАТИСТИКУ", new Point(55, 465), C_RED, C_RED_DARK);
            var btnExit = MakeMenuBtn("✕   ВИЙТИ", new Point(55, 535), Color.FromArgb(50, 50, 65), Color.FromArgb(35, 35, 50));

            btnPlay.Click += (s, e) => StartNewMatch();
            btnReset.Click += (s, e) =>
            {
                if (MessageBox.Show("Скинути всю статистику?", "Підтвердження",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                { GameStats.Reset(); UpdateStatsUI(); }
            };
            btnExit.Click += (s, e) => Application.Exit();
            pnlMenu.Controls.AddRange(new Control[] { btnPlay, btnReset, btnExit });

            // Правила
            var pnlRules = new Panel { Location = new Point(490, 165), Size = new Size(440, 395), BackColor = C_SURFACE };
            pnlRules.Paint += (s, e) =>
            {
                DrawPanelBorder(e.Graphics, pnlRules, C_BLUE);
                using (var lb = new LinearGradientBrush(new Point(0, 0), new Point(pnlRules.Width, 0), C_BLUE_DARK, C_BLUE))
                    e.Graphics.FillRectangle(lb, 0, 0, pnlRules.Width, 4);
            };
            pnlMenu.Controls.Add(pnlRules);

            pnlRules.Controls.Add(MakeLabel("ПРАВИЛА ГРИ", C_BLUE, new Font("Segoe UI", 10, FontStyle.Bold), new Point(18, 18)));

            string[] rules = {
                "1.  Кожен гравець має 5 карт.",
                "2.  Перший хід визначається випадково.",
                "3.  По черзі розміщуйте карти на полі 3×3.",
                "4.  Якщо число вашої сторони більше числа",
                "     сусідньої карти суперника — ви захоплюєте її.",
                "5.  Порівняння: верх↔низ та ліво↔право.",
                "6.  Після заповнення поля рахуємо всі карти",
                "     свого кольору — хто більше, той переміг.",
                "7.  Максимальне значение картки — 10."
            };
            int ry = 52;
            foreach (var rule in rules)
            {
                pnlRules.Controls.Add(MakeLabel(rule, C_TEXT, new Font("Segoe UI", 10.5f), new Point(16, ry)));
                ry += 36;
            }

            pnlGame = new Panel { Dock = DockStyle.Fill, BackColor = C_BG, Visible = false };
            this.Controls.Add(pnlGame);
        }

        private void UpdateStatsUI()
        {
            lblWins.Text = $"Перемоги:   {GameStats.Wins}";
            lblLosses.Text = $"Поразки:    {GameStats.Losses}";
            lblDraws.Text = $"Нічиї:      {GameStats.Draws}";
            int total = GameStats.Wins + GameStats.Losses + GameStats.Draws;
            double rate = total > 0 ? (double)GameStats.Wins / total * 100.0 : 0;
            lblWinRate.Text = $"Вінрейт: {rate:F1}%";
        }

        private void ShowMainMenu()
        {
            UpdateStatsUI();
            pnlGame.Visible = false;
            pnlMenu.Visible = true;
        }

        // ПОЧАТОК МАТЧУ
        private void StartNewMatch()
        {
            pnlMenu.Visible = false;
            pnlGame.Visible = true;
            pnlGame.Controls.Clear();

            playerHand.Clear(); aiHand.Clear();
            playerButtons.Clear(); aiButtons.Clear();
            boardCards = new Card[3, 3];
            selectedCard = null;
            selectedCardButton = null;

            isPlayerTurn = new Random().Next(2) == 0;

            GenerateDecks();
            CreateGameUI();

            if (!isPlayerTurn)
            {
                lblStatus.Text = "Суперник ходить першим...";
                ExecuteAITurn();
            }
        }

        private void GenerateDecks()
        {
            var rand = new Random();
            for (int i = 0; i < 5; i++)
            {
                playerHand.Add(new Card(rand.Next(1, 11), rand.Next(1, 11), rand.Next(1, 11), rand.Next(1, 11), CardOwner.Player));
                aiHand.Add(new Card(rand.Next(1, 11), rand.Next(1, 11), rand.Next(1, 11), rand.Next(1, 11), CardOwner.AI));
            }
        }

        // UI ГРИ
        private void CreateGameUI()
        {
            int formW = this.ClientSize.Width;

            var topBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(formW, 65),
                BackColor = C_SURFACE
            };
            topBar.Paint += (s, e) =>
            {
                using (var lb = new LinearGradientBrush(new Point(0, 63), new Point(0, 65),
                    Color.FromArgb(80, C_BLUE), Color.Transparent))
                    e.Graphics.FillRectangle(lb, 0, 63, topBar.Width, 2);
            };
            pnlGame.Controls.Add(topBar);

            var btnQuit = new Button
            {
                Text = "← Меню",
                Location = new Point(14, 16),
                Size = new Size(95, 32),
                FlatStyle = FlatStyle.Flat,
                ForeColor = C_TEXT,
                BackColor = Color.FromArgb(40, 40, 55),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnQuit.FlatAppearance.BorderSize = 1;
            btnQuit.FlatAppearance.BorderColor = C_BORDER;
            btnQuit.Click += (s, e) =>
            {
                if (MessageBox.Show("Покинути гру? Результат не збережеться.", "Вихід",
                        MessageBoxButtons.YesNo) == DialogResult.Yes) ShowMainMenu();
            };
            topBar.Controls.Add(btnQuit);

            lblStatus = new Label
            {
                Text = isPlayerTurn ? "Ваш хід! Оберіть карту." : "Суперник ходить першим...",
                ForeColor = C_TEXT,
                Location = new Point(0, 0),
                Size = new Size(formW, 65),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            topBar.Controls.Add(lblStatus);

            int boardW = 3 * 120 + 2 * 6;
            int gap = 28;
            int totalW = CARD_W + gap + boardW + gap + CARD_W;
            int startX = (formW - totalW) / 2;

            int playerX = startX;
            int boardX = startX + CARD_W + gap;
            int aiX = boardX + boardW + gap;
            int topY = 75;

            AddColLabel("ВАША РУКА", C_BLUE, new Point(playerX, topY));
            AddColLabel("ПОЛЕ БОЮ", C_GOLD, new Point(boardX + boardW / 2 - 45, topY));
            AddColLabel("РУКА СУПЕРНИКА", C_RED, new Point(aiX, topY));

            for (int i = 0; i < 5; i++)
            {
                var btn = MakeCardButton(playerHand[i], new Point(playerX, topY + 22 + i * (CARD_H + 5)));
                btn.Click += PlayerHandCard_Click;
                playerButtons.Add(btn);
                pnlGame.Controls.Add(btn);
            }

            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                {
                    var btn = new Button
                    {
                        Size = new Size(120, 120),
                        Location = new Point(boardX + c * 126, topY + 22 + r * 126),
                        FlatStyle = FlatStyle.Flat,
                        Tag = new Point(r, c),
                        Cursor = Cursors.Hand
                    };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Paint += BoardCell_Paint;
                    btn.Click += BoardCell_Click;
                    SetDoubleBuffer(btn);
                    boardButtons[r, c] = btn;
                    pnlGame.Controls.Add(btn);
                }

            for (int i = 0; i < 5; i++)
            {
                var btn = MakeCardButton(aiHand[i], new Point(aiX, topY + 22 + i * (CARD_H + 5)));
                btn.Enabled = false;
                aiButtons.Add(btn);
                pnlGame.Controls.Add(btn);
            }
        }

        // МАЛЮВАННЯ КАРТОК
        private void DrawCardLayout(Graphics g, Rectangle rect, Card card, bool selected)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool isPlayer = card.Owner == CardOwner.Player;
            Color cMain = selected ? C_GOLD : (isPlayer ? C_BLUE : C_RED);
            Color cDark = selected ? C_GOLD_DARK : (isPlayer ? C_BLUE_DARK : C_RED_DARK);

            int w = rect.Width, h = rect.Height;

            using (var lb = new LinearGradientBrush(rect, cDark, cMain, 135f))
                g.FillRectangle(lb, rect);

            var inner = new Rectangle(6, 6, w - 12, h - 12);
            using (var lb2 = new LinearGradientBrush(inner,
                Color.FromArgb(175, 0, 0, 0), Color.FromArgb(75, 0, 0, 0), 90f))
                g.FillRectangle(lb2, inner);

            using (var gloss = new LinearGradientBrush(new Rectangle(0, 0, w, h / 2),
                Color.FromArgb(40, 255, 255, 255), Color.Transparent, 90f))
                g.FillRectangle(gloss, 0, 0, w, h / 2);

            Color borderCol = selected ? Color.White : Color.FromArgb(65, 255, 255, 255);
            float borderW = selected ? 2.5f : 1.5f;
            using (Pen bp = new Pen(borderCol, borderW))
                g.DrawRectangle(bp, 1, 1, w - 3, h - 3);

            DrawValue(g, card.Top, w / 2, 17, w, h);
            DrawValue(g, card.Bottom, w / 2, h - 17, w, h);
            DrawValue(g, card.Left, 16, h / 2, w, h);
            DrawValue(g, card.Right, w - 16, h / 2, w, h);
        }

        private void DrawValue(Graphics g, int val, int cx, int cy, int w, int h)
        {
            string text = val.ToString();
            bool isMax = val == 10;

            using (Font f = new Font("Consolas", isMax ? 15f : 14f, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(text, f);
                float px = cx - sz.Width / 2f;
                float py = cy - sz.Height / 2f;

                using (SolidBrush sh = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
                    g.DrawString(text, f, sh, px + 1, py + 1);

                Color c = isMax ? C_GOLD : Color.White;
                using (SolidBrush tb = new SolidBrush(c))
                    g.DrawString(text, f, tb, px, py);
            }
        }

        private void CardButton_Paint(object sender, PaintEventArgs e)
        {
            var btn = (Button)sender;
            var card = (Card)btn.Tag;
            if (card != null)
                DrawCardLayout(e.Graphics, btn.ClientRectangle, card, btn == selectedCardButton);
        }

        private void BoardCell_Paint(object sender, PaintEventArgs e)
        {
            var btn = (Button)sender;
            var coord = (Point)btn.Tag;
            var card = boardCards[coord.X, coord.Y];
            var g = e.Graphics;

            if (card != null)
            {
                DrawCardLayout(g, btn.ClientRectangle, card, false);
            }
            else
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(22, 22, 30));
                using (Pen dp = new Pen(Color.FromArgb(55, 55, 80), 1.5f))
                {
                    dp.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(dp, 5, 5, btn.Width - 11, btn.Height - 11);
                }
                using (Font pf = new Font("Segoe UI", 22))
                using (SolidBrush pb = new SolidBrush(Color.FromArgb(35, 255, 255, 255)))
                {
                    SizeF sz = g.MeasureString("+", pf);
                    g.DrawString("+", pf, pb,
                        btn.Width / 2f - sz.Width / 2f,
                        btn.Height / 2f - sz.Height / 2f);
                }
            }
        }

        // ІГРОВА ЛОГІКА
        private void PlayerHandCard_Click(object sender, EventArgs e)
        {
            if (!isPlayerTurn) return;

            var clicked = (Button)sender;

            if (selectedCardButton == clicked)
            {
                selectedCardButton = null;
                selectedCard = null;
                clicked.Invalidate();
                lblStatus.Text = "Оберіть карту зі своєї руки.";
                return;
            }

            var old = selectedCardButton;
            selectedCardButton = clicked;
            selectedCard = (Card)clicked.Tag;
            old?.Invalidate();
            clicked.Invalidate();
            lblStatus.Text = "Карта обрана. Розмістіть на полі.";
        }

        private void BoardCell_Click(object sender, EventArgs e)
        {
            if (!isPlayerTurn || selectedCard == null) return;

            var cell = (Button)sender;
            var coord = (Point)cell.Tag;
            if (boardCards[coord.X, coord.Y] != null) return;

            boardCards[coord.X, coord.Y] = selectedCard;
            cell.Invalidate();

            pnlGame.Controls.Remove(selectedCardButton);
            playerButtons.Remove(selectedCardButton);
            selectedCard = null;
            selectedCardButton = null;

            BattleMechanic(coord.X, coord.Y, boardCards[coord.X, coord.Y]);
            UpdateScore();
            if (CheckMatchResult()) return;

            isPlayerTurn = false;
            lblStatus.Text = "Хід violent суперника...";
            ExecuteAITurn();
        }

        private void BattleMechanic(int r, int c, Card placed)
        {
            if (r > 0 && boardCards[r - 1, c] != null && boardCards[r - 1, c].Owner != placed.Owner)
                if (placed.Top > boardCards[r - 1, c].Bottom) FlipCard(r - 1, c);
            if (r < 2 && boardCards[r + 1, c] != null && boardCards[r + 1, c].Owner != placed.Owner)
                if (placed.Bottom > boardCards[r + 1, c].Top) FlipCard(r + 1, c);
            if (c > 0 && boardCards[r, c - 1] != null && boardCards[r, c - 1].Owner != placed.Owner)
                if (placed.Left > boardCards[r, c - 1].Right) FlipCard(r, c - 1);
            if (c < 2 && boardCards[r, c + 1] != null && boardCards[r, c + 1].Owner != placed.Owner)
                if (placed.Right > boardCards[r, c + 1].Left) FlipCard(r, c + 1);
        }

        private void FlipCard(int r, int c)
        {
            boardCards[r, c].Owner = boardCards[r, c].Owner == CardOwner.Player
                                     ? CardOwner.AI : CardOwner.Player;
            boardButtons[r, c].Invalidate();
        }

        private async void ExecuteAITurn()
        {
            await Task.Delay(1100);
            if (aiButtons.Count == 0) goto Done;

            var rand = new Random();
            var emptyCells = new List<Point>();
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (boardCards[r, c] == null) emptyCells.Add(new Point(r, c));

            if (emptyCells.Count == 0) goto Done;

            int bestScore = -1;
            Button bestBtn = null;
            Point bestCell = emptyCells[0];

            foreach (Button ab in aiButtons)
            {
                var ac = (Card)ab.Tag;
                foreach (Point cell in emptyCells)
                {
                    int score = SimulateCaptures(ac, cell.X, cell.Y);
                    if (score > bestScore) { bestScore = score; bestBtn = ab; bestCell = cell; }
                }
            }

            if (bestBtn == null)
            {
                bestBtn = aiButtons[rand.Next(aiButtons.Count)];
                bestCell = emptyCells[rand.Next(emptyCells.Count)];
            }

            Card aiCard = (Card)bestBtn.Tag;
            boardCards[bestCell.X, bestCell.Y] = aiCard;
            boardButtons[bestCell.X, bestCell.Y].Invalidate();

            pnlGame.Controls.Remove(bestBtn);
            aiButtons.Remove(bestBtn);

            BattleMechanic(bestCell.X, bestCell.Y, aiCard);
            UpdateScore();

        Done:
            if (CheckMatchResult()) return;
            isPlayerTurn = true;
            lblStatus.Text = "Ваш хід! Оберіть карту.";
        }

        private int SimulateCaptures(Card card, int r, int c)
        {
            int n = 0;
            if (r > 0 && boardCards[r - 1, c]?.Owner == CardOwner.Player && card.Top > boardCards[r - 1, c].Bottom) n++;
            if (r < 2 && boardCards[r + 1, c]?.Owner == CardOwner.Player && card.Bottom > boardCards[r + 1, c].Top) n++;
            if (c > 0 && boardCards[r, c - 1]?.Owner == CardOwner.Player && card.Left > boardCards[r, c - 1].Right) n++;
            if (c < 2 && boardCards[r, c + 1]?.Owner == CardOwner.Player && card.Right > boardCards[r, c + 1].Left) n++;
            return n;
        }

        private void UpdateScore()
        {
            // Метод залишено порожнім, щоб не ламати виклики у коді логіки
        }

        private bool CheckMatchResult()
        {
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (boardCards[r, c] == null) return false;

            int p = 0, a = 0;
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                {
                    if (boardCards[r, c].Owner == CardOwner.Player) p++;
                    else a++;
                }

            string msg;
            if (p > a) { msg = "🏆 Вітаємо! Ви перемогли!"; GameStats.Wins++; }
            else if (a > p) { msg = "💀 Поразка! Штучний інтелект виявився сильнішим."; GameStats.Losses++; }
            else { msg = "🤝 Нічия! Рівна гра."; GameStats.Draws++; }

            GameStats.Save();
            MessageBox.Show($"{msg}\n\nРахунок — Ви: {p}  |  Суперник: {a}",
                "Кінець матчу", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowMainMenu();
            return true;
        }

        // УТИЛІТИ
        private Button MakeCardButton(Card card, Point loc)
        {
            var btn = new Button
            {
                Size = new Size(CARD_W, CARD_H),
                Location = loc,
                FlatStyle = FlatStyle.Flat,
                Tag = card,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += CardButton_Paint;
            SetDoubleBuffer(btn);
            return btn;
        }

        private Button MakeMenuBtn(string text, Point loc, Color hover, Color hoverDark)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(260, 52),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = C_TEXT,
                BackColor = Color.FromArgb(35, 35, 50),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = C_BORDER;
            btn.MouseEnter += (s, e) => { btn.BackColor = hover; btn.FlatAppearance.BorderColor = hover; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.FromArgb(35, 35, 50); btn.FlatAppearance.BorderColor = C_BORDER; };
            return btn;
        }

        private Label MakeLabel(string text, Color color, Font font, Point loc)
        {
            return new Label
            {
                Text = text,
                ForeColor = color,
                Font = font,
                Location = loc,
                AutoSize = true,
                BackColor = Color.Transparent
            };
        }

        private void AddColLabel(string text, Color color, Point loc)
        {
            pnlGame.Controls.Add(new Label
            {
                Text = text,
                ForeColor = color,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = loc,
                AutoSize = true,
                BackColor = Color.Transparent
            });
        }

        private static void DrawPanelBorder(Graphics g, Panel p, Color accent)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Color.FromArgb(60, accent), 1.5f))
                g.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }

        private static void SetDoubleBuffer(Control ctrl)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, true, null);
        }
    }
}