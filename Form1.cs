// Updated Form1.cs with corrected animation and async MakeMove
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectFourGame
{
    public partial class Form1 : Form
    {
        private int[,] board = new int[6, 7];
        private int currentPlayer = 1;
        private PictureBox[,] cells;
        private int player1Score = 0, player2Score = 0;
        private Label labelScores;
        private ComboBox modeSelector;
        private bool isVsComputer = false;
        private Random random = new Random();

        public Form1()
        {
            InitializeComponent();
            this.BackColor = Color.LightSkyBlue;
            AddModeSelector();
            AddScoreLabel();
            AddResetButton();
            InitializeBoard();
        }

        private void AddModeSelector()
        {
            modeSelector = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Height = 30
            };
            modeSelector.Items.AddRange(new string[] { "Player vs Player", "Player vs Computer" });
            modeSelector.SelectedIndex = 0;
            modeSelector.SelectedIndexChanged += (s, e) =>
            {
                isVsComputer = modeSelector.SelectedIndex == 1;
                btnReset_Click(null, null);
            };
            Controls.Add(modeSelector);
            Controls.SetChildIndex(modeSelector, 0);
        }

        private void AddScoreLabel()
        {
            labelScores = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F),
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(labelScores);
            Controls.SetChildIndex(labelScores, 1);
            UpdateScoreLabel();
        }

        private void AddResetButton()
        {
            var btnReset = new Button
            {
                Dock = DockStyle.Bottom,
                Text = "Restart Game",
                Font = new Font("Segoe UI", 10F),
                Height = 35
            };
            btnReset.Click += btnReset_Click;
            Controls.Add(btnReset);
        }

        private void UpdateScoreLabel()
        {
            labelScores.Text = $"Player 1: {player1Score}  |  Player 2: {player2Score}";
        }

        private void InitializeBoard()
        {
            cells = new PictureBox[6, 7];
            tableLayoutPanel1.Controls.Clear();

            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    var pic = new PictureBox
                    {
                        Dock = DockStyle.Fill,
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.White,
                        Tag = col,
                        Margin = new Padding(1),
                        Padding = new Padding(5)
                    };
                    pic.Paint += (s, e) =>
                    {
                        var g = e.Graphics;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.Clear(pic.BackColor);
                        using (Brush b = new SolidBrush(pic.BackColor))
                        {
                            g.FillEllipse(b, 5, 5, pic.Width - 10, pic.Height - 10);
                        }
                    };
                    pic.Click += async (s, e) => await Column_Click(s, e);
                    tableLayoutPanel1.Controls.Add(pic, col, row);
                    cells[row, col] = pic;
                    board[row, col] = 0;
                }
            }
        }

        private async Task Column_Click(object sender, EventArgs e)
        {
            if (sender is PictureBox clicked)
            {
                int col = (int)clicked.Tag;
                if (await MakeMove(col))
                {
                    if (isVsComputer && currentPlayer == 2)
                    {
                        await Task.Delay(300);
                        int aiCol;
                        do { aiCol = random.Next(0, 7); } while (!IsColumnAvailable(aiCol));
                        await MakeMove(aiCol);
                    }
                }
            }
        }

        private bool IsColumnAvailable(int col)
        {
            for (int row = 5; row >= 0; row--)
                if (board[row, col] == 0)
                    return true;
            return false;
        }

        private async Task<bool> MakeMove(int col)
        {
            for (int row = 5; row >= 0; row--)
            {
                if (board[row, col] == 0)
                {
                    await AnimateDrop(row, col);

                    board[row, col] = currentPlayer;
                    cells[row, col].BackColor = currentPlayer == 1 ? Color.Red : Color.Yellow;
                    cells[row, col].Invalidate();

                    var winCells = GetWinningCells(row, col);
                    if (winCells.Count > 0)
                    {
                        HighlightWinningCells(winCells);
                        labelStatus.Text = $"Player {currentPlayer} wins!";
                        if (currentPlayer == 1) player1Score++; else player2Score++;
                        UpdateScoreLabel();
                        DisableBoard();
                        return false;
                    }
                    else if (IsBoardFull())
                    {
                        MessageBox.Show("Match Tie! No more valid moves.", "Tie", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        labelStatus.Text = "Match Tied!";
                        DisableBoard();
                        return false;
                    }
                    else
                    {
                        currentPlayer = 3 - currentPlayer;
                        labelStatus.Text = $"Player {currentPlayer}'s turn";
                    }
                    return true;
                }
            }
            return false;
        }

        private bool IsBoardFull()
        {
            for (int row = 0; row < 6; row++)
                for (int col = 0; col < 7; col++)
                    if (board[row, col] == 0)
                        return false;
            return true;
        }

        private async Task AnimateDrop(int targetRow, int col)
        {
            for (int row = 0; row <= targetRow; row++)
            {
                cells[row, col].BackColor = currentPlayer == 1 ? Color.Red : Color.Yellow;
                cells[row, col].Invalidate();
                await Task.Delay(50);
                if (row != targetRow)
                {
                    cells[row, col].BackColor = Color.White;
                    cells[row, col].Invalidate();
                }
            }
        }

        private List<(int, int)> GetWinningCells(int row, int col)
        {
            int player = board[row, col];
            foreach (var (dr, dc) in new[] { (1, 0), (0, 1), (1, 1), (1, -1) })
            {
                var winCells = new List<(int, int)> { (row, col) };
                for (int i = 1; i < 4; i++)
                {
                    int r = row + dr * i, c = col + dc * i;
                    if (r < 0 || r >= 6 || c < 0 || c >= 7 || board[r, c] != player) break;
                    winCells.Add((r, c));
                }
                for (int i = 1; i < 4; i++)
                {
                    int r = row - dr * i, c = col - dc * i;
                    if (r < 0 || r >= 6 || c < 0 || c >= 7 || board[r, c] != player) break;
                    winCells.Add((r, c));
                }
                if (winCells.Count >= 4)
                    return winCells;
            }
            return new List<(int, int)>();
        }

        private void HighlightWinningCells(List<(int, int)> cellsToHighlight)
        {
            foreach (var (r, c) in cellsToHighlight)
            {
                cells[r, c].BackColor = Color.LimeGreen;
                cells[r, c].Invalidate();
            }
        }

        private void DisableBoard()
        {
            foreach (var pic in cells)
                pic.Enabled = false;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            currentPlayer = 1;
            labelStatus.Text = "Player 1's turn";
            InitializeBoard();
        }
    }
}
    