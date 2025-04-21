using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using SpanningTree.Core;

namespace SpanningTree.GUI
{
    public class GraphPanel : Panel
    {
        private Graph? graph;
        private int[]? spanningTree;
        private int[]? savedSpanningTree = null; // Для сохранения дерева во время редактирования
        private readonly Random random = new Random();
        private readonly Dictionary<int, Point> vertexPositions = new Dictionary<int, Point>();
        private const float VertexRadius = 20f;
        private const float PanelPadding = 50f;
        
        private int? selectedVertex = null;
        private bool isEditing = false;

        public GraphPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            
            // Добавляем обработчики событий мыши
            MouseClick += GraphPanel_MouseClick;
            MouseMove += GraphPanel_MouseMove;
            MouseDoubleClick += GraphPanel_MouseDoubleClick;
            
            // Добавляем обработку нажатия клавиш для отладки
            KeyDown += GraphPanel_KeyDown;
        }

        public void SetGraph(Graph? newGraph)
        {
            graph = newGraph;
            spanningTree = null;
            if (graph != null)
            {
                GenerateVertexPositions();
            }
            Invalidate();
        }

        public void SetSpanningTree(int[]? tree)
        {
            spanningTree = tree;
            Invalidate();
        }

        public void ClearSpanningTree()
        {
            spanningTree = null;
            Invalidate();
        }

        public int[] GetCurrentTree()
        {
            return spanningTree ?? Array.Empty<int>();
        }

        public void SetEditMode(bool enabled)
        {
            isEditing = enabled;
            selectedVertex = null;
            
            if (enabled)
            {
                // При входе в режим редактирования сохраняем остовное дерево и скрываем его
                if (spanningTree != null)
                {
                    savedSpanningTree = (int[])spanningTree.Clone();
                    spanningTree = null;
                }
            }
            else
            {
                // При выходе из режима редактирования восстанавливаем остовное дерево
                if (savedSpanningTree != null)
                {
                    spanningTree = savedSpanningTree;
                    savedSpanningTree = null;
                }
            }
            
            Invalidate();
        }

        private int? GetVertexAtPoint(Point point)
        {
            if (graph == null) return null;

            foreach (var kvp in vertexPositions)
            {
                int v = kvp.Key;
                Point pos = kvp.Value;
                if (Math.Pow(point.X - pos.X, 2) + Math.Pow(point.Y - pos.Y, 2) <= Math.Pow(VertexRadius, 2))
                {
                    return v;
                }
            }
            return null;
        }

        private void GraphPanel_MouseClick(object? sender, MouseEventArgs e)
        {
            if (!isEditing || graph == null) return;

            var clickedVertex = GetVertexAtPoint(e.Location);
            
            // Проверяем, если нажата правая кнопка мыши + Shift - удаляем вершину
            if (e.Button == MouseButtons.Right && ModifierKeys == Keys.Shift && clickedVertex.HasValue)
            {
                // Удаляем вершину
                try
                {
                    graph.RemoveVertex(clickedVertex.Value);
                    selectedVertex = null;
                    GenerateVertexPositions();
                    Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении вершины: {ex.Message}", "Ошибка", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            
            // Обработка клика по пустому месту
            if (clickedVertex == null)
            {
                selectedVertex = null;
                Invalidate();
                return;
            }

            // Обработка выбора вершины и создания/удаления рёбер
            if (selectedVertex == null)
            {
                selectedVertex = clickedVertex;
            }
            else
            {
                if (selectedVertex != clickedVertex)
                {
                    try
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            // Добавляем ребро
                            graph.AddEdge((int)selectedVertex, (int)clickedVertex);
                        }
                        else if (e.Button == MouseButtons.Right)
                        {
                            // Удаляем ребро
                            graph.RemoveEdge((int)selectedVertex, (int)clickedVertex);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при редактировании рёбер: {ex.Message}", "Ошибка", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                selectedVertex = null;
            }
            Invalidate();
        }

        private void GraphPanel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!isEditing || graph == null) return;

            var hoveredVertex = GetVertexAtPoint(e.Location);
            Cursor = hoveredVertex != null ? Cursors.Hand : Cursors.Default;
        }

        private void GraphPanel_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (!isEditing || graph == null) return;

            // Проверяем, что клик произошел не на существующей вершине
            var clickedVertex = GetVertexAtPoint(e.Location);
            if (clickedVertex != null) return;

            // Добавляем новую вершину
            graph.AddVertex();
            
            // Обновляем позиции вершин
            GenerateVertexPositions();
            
            // Обновляем отображение
            Invalidate();
        }

        private void GraphPanel_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12 && graph != null)
            {
                // Вывод диагностической информации
                var adjList = graph.GetAdjacencyList();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Граф: {graph.VertexCount} вершин, {adjList.Sum(list => list.Count) / 2} рёбер");
                
                sb.AppendLine("Список смежности:");
                for (int i = 0; i < adjList.Count; i++)
                {
                    sb.Append($"{i}: ");
                    sb.AppendLine(string.Join(", ", adjList[i]));
                }
                
                MessageBox.Show(sb.ToString(), "Диагностика графа", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void GenerateVertexPositions()
        {
            if (graph == null) return;

            vertexPositions.Clear();
            
            int vertices = graph.VertexCount;
            if (vertices == 0) return;
            
            int margin = 50;
            int width = Math.Max(ClientSize.Width - 2 * margin, 10);
            int height = Math.Max(ClientSize.Height - 2 * margin, 10);
            
            // Для большего количества вершин используем спиральное расположение
            if (vertices <= 10)
            {
                // Для малого числа вершин - по кругу
                double angle = 0;
                double angleStep = 2 * Math.PI / vertices;
                double radius = Math.Min(width, height) * 0.4;
                
                int centerX = margin + width / 2;
                int centerY = margin + height / 2;

                for (int i = 0; i < vertices; i++)
                {
                    int x = centerX + (int)(radius * Math.Cos(angle));
                    int y = centerY + (int)(radius * Math.Sin(angle));
                    vertexPositions[i] = new Point(x, y);
                    angle += angleStep;
                }
            }
            else
            {
                // Для большого числа вершин - сетка
                int cols = (int)Math.Ceiling(Math.Sqrt(vertices));
                int rows = (int)Math.Ceiling((double)vertices / cols);
                
                int cellWidth = width / cols;
                int cellHeight = height / rows;
                
                for (int i = 0; i < vertices; i++)
                {
                    int row = i / cols;
                    int col = i % cols;
                    
                    int x = margin + col * cellWidth + cellWidth / 2;
                    int y = margin + row * cellHeight + cellHeight / 2;
                    
                    vertexPositions[i] = new Point(x, y);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (graph == null || vertexPositions.Count == 0) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            if (spanningTree == null)
            {
                // Рисуем рёбра обычного графа
                using var penEdge = new Pen(Color.LightGray, 1);
                var adjacencyLists = graph.GetAdjacencyList();
                
                // Отслеживаем, какие рёбра уже нарисованы
                var drawnEdges = new HashSet<(int, int)>();
                
                for (int i = 0; i < adjacencyLists.Count; i++)
                {
                    foreach (int j in adjacencyLists[i])
                    {
                        // Нормализуем индексы для хэша
                        int minVertex = Math.Min(i, j);
                        int maxVertex = Math.Max(i, j);
                        var edge = (minVertex, maxVertex);
                        
                        // Проверяем, что ребро ещё не нарисовано
                        if (!drawnEdges.Contains(edge))
                        {
                            e.Graphics.DrawLine(penEdge, vertexPositions[i], vertexPositions[j]);
                            drawnEdges.Add(edge);
                        }
                    }
                }
            }
            else
            {
                // Рисуем рёбра остовного дерева
                using var penTree = new Pen(Color.Blue, 2);
                for (int i = 0; i < spanningTree.Length; i++)
                {
                    if (spanningTree[i] != -1)
                    {
                        if (vertexPositions.ContainsKey(i) && vertexPositions.ContainsKey(spanningTree[i]))
                        {
                            e.Graphics.DrawLine(penTree, vertexPositions[i], vertexPositions[spanningTree[i]]);
                        }
                    }
                }
            }

            // Рисуем временное ребро от выбранной вершины к курсору
            if (isEditing && selectedVertex != null)
            {
                using var penTemp = new Pen(Color.Red, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                Point mousePos = PointToClient(MousePosition);
                e.Graphics.DrawLine(penTemp, vertexPositions[(int)selectedVertex], mousePos);
            }

            using var brushVertex = new SolidBrush(Color.White);
            using var brushSelected = new SolidBrush(Color.LightYellow);
            using var penVertex = new Pen(Color.Black, 2);
            using var brushText = new SolidBrush(Color.Black);
            using var font = new Font("Arial", 10);

            foreach (var kvp in vertexPositions)
            {
                int v = kvp.Key;
                Point pos = kvp.Value;
                
                var brush = selectedVertex.HasValue && v == selectedVertex.Value ? brushSelected : brushVertex;
                e.Graphics.FillEllipse(brush, pos.X - 15, pos.Y - 15, 30, 30);
                e.Graphics.DrawEllipse(penVertex, pos.X - 15, pos.Y - 15, 30, 30);
                
                var size = e.Graphics.MeasureString(v.ToString(), font);
                e.Graphics.DrawString(v.ToString(), font, brushText, 
                    pos.X - size.Width / 2, pos.Y - size.Height / 2);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GenerateVertexPositions();
        }
    }
} 