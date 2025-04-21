using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SpanningTree.Core;

namespace SpanningTree.GUI
{
    public partial class MainForm : Form, IDisposable
    {
        private Graph? currentGraph;
        private int[]? currentTree;
        private readonly GraphPanel graphPanel;
        private readonly SplitContainer splitContainer;
        private readonly Panel controlPanel;
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLabel;

        public GroupBox? graphGroup;
        public NumericUpDown? numVertices;
        public NumericUpDown? numProbability;
        public Button? btnGenerateGraph;
        public Button? btnToggleEdit;

        public GroupBox? algorithmGroup;
        public NumericUpDown? numIterations;
        public NumericUpDown? numThreads;
        public Button? btnFindTree;

        public GroupBox? fileGroup;
        public Button? btnLoadGraph;
        public Button? btnSaveGraph;
        public Button? btnSaveTree;

        public MainForm()
        {
            Text = "Maximum Leaf Spanning Tree";
            MinimumSize = new Size(900, 600);
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;

            // Графическая панель
            graphPanel = new GraphPanel { Dock = DockStyle.Fill };
            // Панель с настройками
            controlPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };

            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel2,
                Orientation = Orientation.Vertical
            };

            // Статус-бар
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel { Spring = true };
            statusStrip.Items.Add(statusLabel);

            // Создаем группы контролов
            CreateControlGroups();

            // Собираем форму
            Controls.Add(splitContainer);
            Controls.Add(statusStrip);

            splitContainer.Panel1.Controls.Add(graphPanel);
            splitContainer.Panel2.Controls.Add(controlPanel);

            controlPanel.Controls.AddRange(new Control[] { graphGroup!, algorithmGroup!, fileGroup! });

            // События формы
            Load += MainForm_Load;
            Shown += MainForm_Shown;
            FormClosing += MainForm_FormClosing;
        }

        private void CreateControlGroups()
        {
            // --- Генерация графа ---
            graphGroup = new GroupBox
            {
                Text = "Генерация графа",
                Dock = DockStyle.Top,
                Height = 150,
                Padding = new Padding(10)
            };
            var lblV = new Label { Text = "Количество вершин:", Location = new Point(10, 30), AutoSize = true };
            numVertices = new NumericUpDown { Location = new Point(150, 28), Minimum = 2, Maximum = 100, Value = 5 };
            var lblP = new Label { Text = "Вероятность ребра:", Location = new Point(10, 60), AutoSize = true };
            numProbability = new NumericUpDown
            {
                Location = new Point(150, 58),
                DecimalPlaces = 2,
                Increment = 0.1m,
                Minimum = 0,
                Maximum = 1,
                Value = 0.3m
            };
            btnGenerateGraph = new Button { Text = "Сгенерировать случайный граф", Location = new Point(10, 90), Width = 200 };
            btnToggleEdit = new Button { Text = "Режим редактирования", Location = new Point(10, 120), Width = 200 };
            
            btnGenerateGraph.Click += BtnGenerateGraph_Click;
            btnToggleEdit.Click += BtnToggleEdit_Click;
            
            graphGroup.Controls.AddRange(new Control[] { 
                lblV, numVertices, lblP, numProbability, btnGenerateGraph, btnToggleEdit 
            });

            // --- Параметры алгоритма ---
            algorithmGroup = new GroupBox
            {
                Text = "Параметры алгоритма",
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(10)
            };
            
            // Добавляем опцию фиксированного seed
            var chkFixedSeed = new CheckBox { 
                Text = "Постоянное остовное дерево", 
                Location = new Point(10, 30), 
                AutoSize = true,
                Checked = true // По умолчанию включено
            };
            chkFixedSeed.CheckedChanged += (s, e) => {
                SpanningTree.Core.SpanningTree.SetUseFixedSeed(chkFixedSeed.Checked);
            };
            
            // Добавляем опцию параллельного выполнения
            var chkParallel = new CheckBox { 
                Text = "Использовать параллельные вычисления", 
                Location = new Point(10, 60), 
                AutoSize = true,
                Checked = true // По умолчанию включено
            };
            chkParallel.CheckedChanged += (s, e) => {
                SpanningTree.Core.SpanningTree.SetUseParallelComputation(chkParallel.Checked);
            };
            
            btnFindTree = new Button { Text = "Найти остовное дерево", Dock = DockStyle.Bottom, Enabled = false };
            btnFindTree.Click += BtnFindTree_Click;
            algorithmGroup.Controls.AddRange(new Control[] { chkFixedSeed, chkParallel, btnFindTree });

            // --- Файловые операции ---
            fileGroup = new GroupBox
            {
                Text = "Файловые операции",
                Dock = DockStyle.Top,
                Height = 130,
                Padding = new Padding(10)
            };
            btnLoadGraph = new Button { Text = "Загрузить граф", Dock = DockStyle.Top };
            btnLoadGraph.Click += BtnLoadGraph_Click;
            btnSaveGraph = new Button { Text = "Сохранить граф", Dock = DockStyle.Top, Enabled = false };
            btnSaveGraph.Click += BtnSaveGraph_Click;
            btnSaveTree = new Button { Text = "Сохранить дерево", Dock = DockStyle.Top, Enabled = false };
            btnSaveTree.Click += BtnSaveTree_Click;
            fileGroup.Controls.AddRange(new Control[] { btnLoadGraph, btnSaveGraph, btnSaveTree });
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            statusLabel.Text = "Готово. Сгенерируйте граф или загрузите из файла.";
        }

        private void MainForm_Shown(object? sender, EventArgs e)
        {
            splitContainer.Panel1MinSize = 200;
            splitContainer.Panel2MinSize = 300;
            SafeSetSplitterDistance();
        }

        private void SafeSetSplitterDistance()
        {
            int min = splitContainer.Panel1MinSize;
            int max = splitContainer.Width - splitContainer.Panel2MinSize;

            if (splitContainer.Width <= 0 || max <= min)
            {
                splitContainer.SplitterDistance = splitContainer.Width / 2;
                return;
            }

            int mid = min + (max - min) / 2;
            splitContainer.SplitterDistance = Math.Clamp(mid, min, max);
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            currentGraph?.Dispose();
        }

        private void BtnToggleEdit_Click(object? sender, EventArgs e)
        {
            if (currentGraph == null) return;

            bool isEditing = btnToggleEdit!.Text == "Выйти из режима редактирования";
            btnToggleEdit.Text = isEditing ? "Режим редактирования" : "Выйти из режима редактирования";
            
            // Отключаем другие кнопки во время редактирования
            btnGenerateGraph!.Enabled = isEditing;
            btnFindTree!.Enabled = isEditing;
            btnLoadGraph!.Enabled = isEditing;
            btnSaveGraph!.Enabled = isEditing;
            btnSaveTree!.Enabled = isEditing && currentTree != null;
            
            graphPanel.SetEditMode(!isEditing);
            statusLabel.Text = isEditing 
                ? "Готово" 
                : "Режим редактирования: ЛКМ - добавить ребро, ПКМ - удалить ребро, Двойной клик - добавить вершину, Shift+ПКМ - удалить вершину";
        }

        private void BtnGenerateGraph_Click(object? sender, EventArgs e)
        {
            try
            {
                int v = (int)numVertices!.Value;
                double p = (double)numProbability!.Value;

                currentGraph?.Dispose();
                currentGraph = Graph.GenerateRandom(v, p);

                graphPanel.SetGraph(currentGraph);
                btnToggleEdit!.Enabled = true;
                btnFindTree!.Enabled = true;
                btnSaveGraph!.Enabled = true;
                btnSaveTree!.Enabled = false;
                statusLabel.Text = $"Граф: {v} вершин";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации: {ex.Message}", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnFindTree_Click(object? sender, EventArgs e)
        {
            if (currentGraph == null) return;

            try
            {
                btnFindTree!.Enabled = false;
                statusLabel.Text = "Ищу...";
                
                // Если мы в режиме редактирования, сначала выходим из него
                bool wasInEditMode = btnToggleEdit!.Text == "Выйти из режима редактирования";
                if (wasInEditMode)
                {
                    BtnToggleEdit_Click(btnToggleEdit, EventArgs.Empty);
                }
                
                // Блокируем перерисовку на время поиска
                graphPanel.SuspendLayout();
                this.UseWaitCursor = true;

                if (!currentGraph.IsConnected())
                {
                    MessageBox.Show("Граф не является связным. Добавьте больше рёбер.", "Ошибка", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var tree = new SpanningTree.Core.SpanningTree(currentGraph);
                var result = tree.FindMaxLeafSpanningTree();

                // Сохраняем результат в переменной класса
                currentTree = result;
                graphPanel.SetSpanningTree(result);
                btnSaveTree!.Enabled = true;
                
                int leaves = SpanningTree.Core.SpanningTree.CountLeaves(result);
                statusLabel.Text = $"Найдено остовное дерево с {leaves} листьями";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска дерева: {ex.Message}", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally 
            {
                graphPanel.ResumeLayout();
                this.UseWaitCursor = false;
                btnFindTree!.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void BtnLoadGraph_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "Граф (*.graph)|*.graph" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                currentGraph?.Dispose();
                currentGraph = Graph.fromFile(dlg.FileName);
                graphPanel.SetGraph(currentGraph);
                currentTree = null;
                graphPanel.ClearSpanningTree();

                btnToggleEdit!.Enabled = true;
                btnFindTree!.Enabled = true;
                btnSaveGraph!.Enabled = true;
                btnSaveTree!.Enabled = false;
                statusLabel.Text = $"Загружен: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSaveGraph_Click(object? sender, EventArgs e)
        {
            if (currentGraph == null) return;

            using var dlg = new SaveFileDialog { Filter = "Граф (*.graph)|*.graph", DefaultExt = "graph" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                currentGraph.SaveToFile(dlg.FileName);
                statusLabel.Text = $"Сохранён: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSaveTree_Click(object? sender, EventArgs e)
        {
            if (currentTree == null || currentTree.Length == 0) return;

            using var dlg = new SaveFileDialog { Filter = "Текст (*.txt)|*.txt", DefaultExt = "txt" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var lines = new string[currentTree.Length];
                for (int i = 0; i < currentTree.Length; i++)
                    lines[i] = $"{i}: {currentTree[i]}";
                File.WriteAllLines(dlg.FileName, lines);
                statusLabel.Text = $"Дерево сохранено: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения дерева: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) currentGraph?.Dispose();
            base.Dispose(disposing);
        }
    }
}
