using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace SpanningTree.Core
{
    // Обертка для нативного указателя Graph
    internal class GraphSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private GraphSafeHandle() : base(true) { }

        // Импортируем функцию уничтожения
        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_Destroy")]
        private static extern void DestroyGraph(IntPtr graph);

        protected override bool ReleaseHandle()
        {
            DestroyGraph(handle);
            return true;
        }
    }

    public class Graph : IDisposable
    {
        private GraphSafeHandle? _handle; // Используем SafeHandle
        private HashSet<(int, int)> _edges = new HashSet<(int, int)>(); // Кэш рёбер

        // Импорт нативных методов
        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_Create")]
        private static extern GraphSafeHandle CreateGraph(int vertices);

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_AddEdge")]
        private static extern void AddEdgeNative(GraphSafeHandle graph, int u, int v);

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_RemoveEdge")]
        private static extern void RemoveEdgeNative(GraphSafeHandle graph, int u, int v);

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_IsConnected")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool IsConnectedNative(GraphSafeHandle graph);

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_GetVertexCount")]
        private static extern int GetVertexCountNative(GraphSafeHandle graph);

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_GenerateRandom")]
        private static extern GraphSafeHandle GenerateRandomNative(int vertices, double probability);

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_FromFile")]
        private static extern GraphSafeHandle FromFileNative([MarshalAs(UnmanagedType.LPStr)] string filename);

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_SaveToFile")]
        private static extern void SaveToFileNative(GraphSafeHandle graph, [MarshalAs(UnmanagedType.LPStr)] string filename);
        
        // TODO: Реализовать правильный экспорт/импорт списка смежности из C++
        // [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Graph_GetAdjacencyList")]
        // private static extern IntPtr GetAdjacencyListNative(GraphSafeHandle graph, ref int size);

        // Конструктор
        public Graph(int vertices)
        {
            if (vertices < 0)
                throw new ArgumentException("Количество вершин не может быть отрицательным", nameof(vertices));

            _handle = CreateGraph(vertices);
            if (_handle.IsInvalid)
                throw new InvalidOperationException("Не удалось создать нативный объект графа.");
        }

        // Приватный конструктор для статических методов
        private Graph(GraphSafeHandle handle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            if (_handle.IsInvalid)
                 throw new ArgumentException("Предоставлен невалидный указатель на граф.", nameof(handle));
        }

        // Статические методы для создания графа
        public static Graph GenerateRandom(int vertices, double probability)
        {
            if (vertices < 0)
                throw new ArgumentException("Количество вершин должно быть положительным числом", nameof(vertices));
            if (probability < 0.0 || probability > 1.0)
                throw new ArgumentException("Вероятность должна быть в диапазоне [0, 1]", nameof(probability));

            Graph graph = new Graph(vertices);
            
            // Сначала генерируем случайный граф
            var random = new Random();
            for (int i = 0; i < vertices; i++)
            {
                for (int j = i + 1; j < vertices; j++)
                {
                    if (random.NextDouble() < probability)
                    {
                        graph.AddEdge(i, j);
                    }
                }
            }
            
            // Проверяем связность и делаем граф связным, если нужно
            if (!graph.IsConnected() && vertices > 1)
            {
                // Используем списки смежности из кэша для нахождения компонент
                var adjList = graph.GetAdjacencyList();
                var visited = new bool[vertices];
                var components = new List<List<int>>();
                
                for (int v = 0; v < vertices; v++)
                {
                    if (!visited[v])
                    {
                        var component = new List<int>();
                        var queue = new Queue<int>();
                        queue.Enqueue(v);
                        visited[v] = true;
                        
                        while (queue.Count > 0)
                        {
                            int current = queue.Dequeue();
                            component.Add(current);
                            
                            foreach (int neighbor in adjList[current])
                            {
                                if (!visited[neighbor])
                                {
                                    visited[neighbor] = true;
                                    queue.Enqueue(neighbor);
                                }
                            }
                        }
                        
                        components.Add(component);
                    }
                }
                
                // Соединяем компоненты
                if (components.Count > 1)
                {
                    for (int i = 1; i < components.Count; i++)
                    {
                        int v1 = components[0][0];  // Вершина из первой компоненты
                        int v2 = components[i][0];  // Вершина из текущей компоненты
                        graph.AddEdge(v1, v2);
                    }
                }
            }
            
            return graph;
        }
        
        // Вспомогательный метод для поиска компонент связности
        private static List<List<int>> FindComponents(Graph graph)
        {
            var result = new List<List<int>>();
            int vertices = graph.VertexCount;
            bool[] visited = new bool[vertices];
            
            for (int v = 0; v < vertices; v++)
            {
                if (!visited[v])
                {
                    // Новая компонента связности
                    var component = new List<int>();
                    
                    // BFS для поиска всех вершин в этой компоненте
                    var queue = new Queue<int>();
                    queue.Enqueue(v);
                    visited[v] = true;
                    
                    while (queue.Count > 0)
                    {
                        int current = queue.Dequeue();
                        component.Add(current);
                        
                        // Проверяем соседей
                        var adjList = graph.GetAdjacencyList();
                        foreach (int neighbor in adjList[current])
                        {
                            if (!visited[neighbor])
                            {
                                visited[neighbor] = true;
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                    
                    result.Add(component);
                }
            }
            
            return result;
        }

        public static Graph fromFile(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Файл графа не найден", filename);
                
            var handle = FromFileNative(filename);
            if (handle.IsInvalid)
                throw new InvalidOperationException($"Не удалось загрузить граф из файла: {filename}");
            return new Graph(handle);
        }

        // Методы экземпляра
        public void AddEdge(int u, int v)
        {
            EnsureNotDisposed();
            if (u == v) throw new ArgumentException("Нельзя создать петлю (ребро из вершины в себя)");
            
            // Проверяем, что вершины существуют
            int count = VertexCount;
            if (u < 0 || u >= count || v < 0 || v >= count)
                throw new ArgumentOutOfRangeException("Индексы вершин вне допустимого диапазона");
            
            // Нормализуем индексы вершин для хранения в кэше
            int minVertex = Math.Min(u, v);
            int maxVertex = Math.Max(u, v);
            
            // Если ребро уже существует, ничего не делаем
            if (_edges.Contains((minVertex, maxVertex)))
                return;
            
            try
            {
                // Добавляем в кэш
                _edges.Add((minVertex, maxVertex));
                
                // Вызываем нативный метод (обернем в try-catch, чтобы изолировать проблемы с DLL)
                AddEdgeNative(_handle!, u, v);
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, удаляем ребро из кэша
                _edges.Remove((minVertex, maxVertex));
                throw new InvalidOperationException($"Ошибка при добавлении ребра: {ex.Message}", ex);
            }
        }

        public void RemoveEdge(int u, int v)
        {
            EnsureNotDisposed();
            
            // Нормализуем индексы вершин для хранения в кэше
            int minVertex = Math.Min(u, v);
            int maxVertex = Math.Max(u, v);
            
            // Если ребра нет в кэше, ничего не делаем
            if (!_edges.Contains((minVertex, maxVertex)))
                return;
            
            // Удаляем из кэша
            _edges.Remove((minVertex, maxVertex));
                
            // Затем пересоздаем граф без этого ребра
            int vertices = VertexCount;
            
            // Сохраняем рёбра
            var edgesToKeep = new HashSet<(int, int)>(_edges);
            
            // Освобождаем текущий handle
            _handle?.Dispose();
            
            // Создаем новый пустой граф
            _handle = CreateGraph(vertices);
            
            // Добавляем все рёбра из кэша, кроме удаленного
            foreach (var edge in edgesToKeep)
            {
                AddEdgeNative(_handle!, edge.Item1, edge.Item2);
            }
        }

        public bool IsConnected()
        {
            EnsureNotDisposed();
            return IsConnectedNative(_handle!);
        }

        public int VertexCount
        {
            get
            {
                EnsureNotDisposed();
                return GetVertexCountNative(_handle!);
            }
        }

        public void SaveToFile(string filename)
        {
            EnsureNotDisposed();
            SaveToFileNative(_handle!, filename);
        }

        // Временная заглушка для GetAdjacencyList
        public List<List<int>> GetAdjacencyList()
        {
            EnsureNotDisposed();
            int count = VertexCount;
            
            var result = new List<List<int>>();
            for (int i = 0; i < count; i++)
            {
                result.Add(new List<int>());
            }
            
            // Заполняем список рёбрами из кэша
            foreach (var edge in _edges)
            {
                int u = edge.Item1;
                int v = edge.Item2;
                result[u].Add(v);
                result[v].Add(u);
            }
            
            return result;
        }

        public void AddVertex()
        {
            EnsureNotDisposed();
            
            int currentVertices = VertexCount;
            int newVertexCount = currentVertices + 1;
            
            // Сохраняем текущие рёбра
            var edgesToKeep = new HashSet<(int, int)>(_edges);
            
            // Освобождаем текущий handle
            _handle?.Dispose();
            
            // Создаем новый граф с большим количеством вершин
            _handle = CreateGraph(newVertexCount);
            
            // Восстанавливаем рёбра
            foreach (var edge in edgesToKeep)
            {
                AddEdgeNative(_handle!, edge.Item1, edge.Item2);
            }
        }

        public void RemoveVertex(int vertex)
        {
            EnsureNotDisposed();
            
            int currentVertices = VertexCount;
            
            // Проверяем, что вершина существует
            if (vertex < 0 || vertex >= currentVertices)
                throw new ArgumentOutOfRangeException(nameof(vertex), "Индекс вершины вне допустимого диапазона");
            
            // Если это единственная вершина, просто пересоздаем граф
            if (currentVertices == 1)
            {
                _handle?.Dispose();
                _handle = CreateGraph(0);
                _edges.Clear();
                return;
            }
            
            // Новое количество вершин
            int newVertexCount = currentVertices - 1;
            
            // Находим рёбра, которые нужно сохранить (не содержащие удаляемую вершину)
            var edgesToKeep = new HashSet<(int, int)>();
            foreach (var edge in _edges)
            {
                if (edge.Item1 != vertex && edge.Item2 != vertex)
                {
                    // Корректируем индексы вершин, которые идут после удаляемой
                    int u = edge.Item1 > vertex ? edge.Item1 - 1 : edge.Item1;
                    int v = edge.Item2 > vertex ? edge.Item2 - 1 : edge.Item2;
                    edgesToKeep.Add((Math.Min(u, v), Math.Max(u, v)));
                }
            }
            
            // Освобождаем текущий handle
            _handle?.Dispose();
            
            // Создаем новый граф с меньшим количеством вершин
            _handle = CreateGraph(newVertexCount);
            
            // Очищаем и обновляем кэш рёбер
            _edges.Clear();
            _edges = edgesToKeep;
            
            // Восстанавливаем рёбра в новом графе
            foreach (var edge in edgesToKeep)
            {
                try
                {
                    AddEdgeNative(_handle!, edge.Item1, edge.Item2);
                }
                catch
                {
                    // Игнорируем ошибки при добавлении рёбер, кэш уже обновлен
                }
            }
        }

        // Реализация IDisposable
        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы (если есть)
                }

                // Освобождаем неуправляемые ресурсы
                _handle?.Dispose();
                _handle = null;
                _disposed = true;
            }
        }

        public void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Финализатор для подстраховки
        ~Graph()
        {
            Dispose(false);
        }
        
        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Graph));
            }
        }

        // Доступ к нативному указателю для SpanningTree
        internal GraphSafeHandle NativeHandle => _handle;
    }
} 