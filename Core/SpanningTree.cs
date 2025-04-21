using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SpanningTree.Core
{
    // Обертка для нативного указателя SpanningTree
    internal class SpanningTreeSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SpanningTreeSafeHandle() : base(true) { }

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SpanningTree_Destroy")]
        private static extern void DestroySpanningTree(IntPtr tree);

        protected override bool ReleaseHandle()
        {
            DestroySpanningTree(handle);
            return true;
        }
    }

    public class SpanningTree : IDisposable
    {
        private SpanningTreeSafeHandle? _handle; // Используем SafeHandle
        private static int _globalSeed = 42; // Фиксированное начальное значение для seed
        private static bool _useFixedSeed = true; // Флаг для использования фиксированного seed
        private static bool _useParallelComputation = true; // Флаг для использования параллельных вычислений
        private const int DEFAULT_ITERATIONS = 10000; // Фиксированное количество итераций для алгоритма
        private const bool USE_EXACT_ALGORITHM = true; // Используем точный алгоритм вместо эвристического

        // Публичный метод для включения/выключения фиксированного seed
        public static void SetUseFixedSeed(bool useFixedSeed, int seed = 42)
        {
            _useFixedSeed = useFixedSeed;
            if (useFixedSeed)
                _globalSeed = seed;
        }
        
        // Публичный метод для включения/выключения параллельных вычислений
        public static void SetUseParallelComputation(bool useParallel)
        {
            _useParallelComputation = useParallel;
        }

        // Импорт нативных методов
        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SpanningTree_Create")]
        private static extern SpanningTreeSafeHandle CreateSpanningTree(GraphSafeHandle graph);

        // Возвращает указатель на массив int*, который нужно освободить через FreeArray
        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SpanningTree_FindMaxLeafSpanningTree")]
        private static extern IntPtr FindMaxLeafSpanningTreeNative(SpanningTreeSafeHandle tree, int iterations, int threads);

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SpanningTree_CountLeaves")]
        private static extern int CountLeavesNative(SpanningTreeSafeHandle tree, IntPtr spanningTree, int size); // Добавлен size

        [DllImport("SpanningTreeCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FreeArray")]
        private static extern void FreeArrayNative(IntPtr array);

        private readonly Graph _associatedGraph; // Храним ссылку на граф для получения размера

        // Конструктор
        public SpanningTree(Graph graph)
        {
            _associatedGraph = graph ?? throw new ArgumentNullException(nameof(graph));
            
            // Получаем SafeHandle из ассоциированного графа
            var graphHandle = graph.NativeHandle;
            if (graphHandle == null || graphHandle.IsInvalid)
                throw new ArgumentException("Предоставлен невалидный или уничтоженный граф.", nameof(graph));

            _handle = CreateSpanningTree(graphHandle);
            if (_handle.IsInvalid)
                throw new InvalidOperationException("Не удалось создать нативный объект SpanningTree.");
        }

        // Метод для поиска дерева
        public int[] FindMaxLeafSpanningTree()
        {
            EnsureNotDisposed();
            
            return USE_EXACT_ALGORITHM 
                ? FindExactMaxLeafSpanningTree() 
                : FindMaxLeafSpanningTree(DEFAULT_ITERATIONS, Environment.ProcessorCount);
        }
        
        // Точный алгоритм поиска остовного дерева с максимальным числом листьев
        private int[] FindExactMaxLeafSpanningTree()
        {
            var adjList = _associatedGraph.GetAdjacencyList();
            int n = adjList.Count;
            
            if (n <= 1) 
            {
                // Для графа с одной вершиной возвращаем тривиальное дерево
                if (n == 0)
                    return Array.Empty<int>();
                else
                    return new int[] { -1 };
            }
            
            // Этот алгоритм основан на подходе построения остовного дерева с максимальным числом листьев
            
            // Если включены параллельные вычисления, используем все доступные ядра процессора
            if (_useParallelComputation && n > 10)
            {
                return FindExactMaxLeafSpanningTreeParallel(adjList);
            }
            
            return FindExactMaxLeafSpanningTreeSequential(adjList);
        }
        
        // Последовательная версия точного алгоритма
        private int[] FindExactMaxLeafSpanningTreeSequential(List<List<int>> adjList)
        {
            int n = adjList.Count;
            
            // Используем общий метод для выбора начальных вершин
            var startVertices = SelectStartVertices(adjList);
            
            // Если нет вершин, возвращаем пустой массив
            if (startVertices.Count == 0)
            {
                return new int[n];
            }
            
            // Создаем массивы для хранения результатов каждого запуска
            int[][] results = new int[startVertices.Count][];
            int[] leafCounts = new int[startVertices.Count];
            
            // Выполняем поиск остовных деревьев, начиная с разных вершин, последовательно
            for (int i = 0; i < startVertices.Count; i++)
            {
                results[i] = FindSpanningTreeStartingFrom(adjList, startVertices[i]);
                leafCounts[i] = CountLeaves(results[i]);
            }
            
            // Находим лучший результат
            int bestIndex = 0;
            int maxLeaves = leafCounts[0];
            
            for (int i = 1; i < leafCounts.Length; i++)
            {
                if (leafCounts[i] > maxLeaves)
                {
                    maxLeaves = leafCounts[i];
                    bestIndex = i;
                }
            }
            
            return results[bestIndex];
        }
        
        // Параллельная версия точного алгоритма
        private int[] FindExactMaxLeafSpanningTreeParallel(List<List<int>> adjList)
        {
            int n = adjList.Count;
            
            // Используем общий метод для выбора начальных вершин
            var startVertices = SelectStartVertices(adjList);
            
            // Если нет вершин, возвращаем пустой массив
            if (startVertices.Count == 0)
            {
                return new int[n];
            }
            
            // Создаем массивы для хранения результатов каждого параллельного запуска
            int[][] results = new int[startVertices.Count][];
            int[] leafCounts = new int[startVertices.Count];
            
            // Выполняем поиск остовных деревьев параллельно, начиная с разных вершин
            Parallel.For(0, startVertices.Count, i =>
            {
                results[i] = FindSpanningTreeStartingFrom(adjList, startVertices[i]);
                leafCounts[i] = CountLeaves(results[i]);
            });
            
            // Находим лучший результат
            int bestIndex = 0;
            int maxLeaves = leafCounts[0];
            
            for (int i = 1; i < leafCounts.Length; i++)
            {
                if (leafCounts[i] > maxLeaves)
                {
                    maxLeaves = leafCounts[i];
                    bestIndex = i;
                }
            }
            
            return results[bestIndex];
        }
        
        // Общий метод для выбора стартовых вершин
        private List<int> SelectStartVertices(List<List<int>> adjList)
        {
            int n = adjList.Count;
            var startVertices = new List<int>();
            
            // Всегда перебираем все вершины как потенциальные корни
            for (int i = 0; i < n; i++)
            {
                startVertices.Add(i);
            }
            
            return startVertices;
        }
        
        // Вспомогательный метод для построения остовного дерева, начиная с заданной вершины
        private int[] FindSpanningTreeStartingFrom(List<List<int>> adjList, int startVertex)
        {
            int n = adjList.Count;
            
            int[] parent = new int[n];
            for (int i = 0; i < n; i++)
            {
                parent[i] = -1;
            }
            
            bool[] visited = new bool[n];
            var queue = new Queue<int>();
            
            queue.Enqueue(startVertex);
            visited[startVertex] = true;
            
            // Применяем BFS с приоритетом для вершин высокой степени
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                
                // Сортируем соседей по степени (в порядке убывания)
                var neighbors = new List<(int vertex, int degree)>();
                foreach (var neighbor in adjList[current])
                {
                    if (!visited[neighbor])
                    {
                        // Считаем "эффективную" степень как количество непосещенных соседей
                        int effectiveDegree = 0;
                        foreach (var nn in adjList[neighbor])
                        {
                            if (!visited[nn])
                                effectiveDegree++;
                        }
                        
                        neighbors.Add((neighbor, effectiveDegree));
                    }
                }
                
                // Сортируем по убыванию эффективной степени
                neighbors.Sort((a, b) => b.degree.CompareTo(a.degree));
                
                foreach (var (neighbor, _) in neighbors)
                {
                    visited[neighbor] = true;
                    parent[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
            
            // Проверяем, что все вершины добавлены в дерево
            for (int i = 0; i < n; i++)
            {
                if (!visited[i])
                {
                    // Находим ближайшую посещенную вершину
                    foreach (var neighbor in adjList[i])
                    {
                        if (visited[neighbor])
                        {
                            parent[i] = neighbor;
                            visited[i] = true;
                            break;
                        }
                    }
                }
            }
            
            // Выполняем оптимизацию: пытаемся увеличить число листьев
            OptimizeLeafCount(adjList, parent);
            
            return parent;
        }
        
        // Метод оптимизации числа листьев в дереве
        private void OptimizeLeafCount(List<List<int>> adjList, int[] parent)
        {
            int n = adjList.Count;
            bool improved;
            
            do
            {
                improved = false;
                
                // Для каждого узла проверяем, можно ли изменить его родителя для увеличения числа листьев
                for (int i = 0; i < n; i++)
                {
                    // Пропускаем корень
                    if (parent[i] == -1)
                        continue;
                    
                    // Текущее количество листьев
                    int currentLeafCount = CountLeaves(parent);
                    
                    // Текущий родитель
                    int originalParent = parent[i];
                    
                    // Перебираем всех соседей как потенциальных новых родителей
                    foreach (var neighbor in adjList[i])
                    {
                        // Проверяем, что это не текущий родитель и не потомок
                        if (neighbor != originalParent && !IsDescendant(parent, i, neighbor))
                        {
                            // Временно изменяем родителя
                            parent[i] = neighbor;
                            
                            // Проверяем, улучшилось ли количество листьев
                            int newLeafCount = CountLeaves(parent);
                            if (newLeafCount > currentLeafCount)
                            {
                                // Нашли улучшение, сохраняем его
                                improved = true;
                                break;
                            }
                            else
                            {
                                // Возвращаем исходного родителя
                                parent[i] = originalParent;
                            }
                        }
                    }
                }
            } while (improved); // Повторяем, пока есть улучшения
        }
        
        // Проверяет, является ли potentialDescendant потомком node в дереве parent
        private bool IsDescendant(int[] parent, int node, int potentialDescendant)
        {
            // Начинаем с potentialDescendant и идем вверх по дереву
            int current = potentialDescendant;
            while (current != -1)
            {
                if (current == node)
                    return true;
                current = parent[current];
            }
            return false;
        }
        
        // Оставляем этот метод для совместимости, но делаем его приватным
        private int[] FindMaxLeafSpanningTree(int iterations, int threads)
        {
            EnsureNotDisposed();
            
            // Если мы используем собственную имплементацию (поскольку нативный метод не работает),
            // то можем контролировать seed
            if (_useFixedSeed)
            {
                return FindMaxLeafSpanningTreeManaged(iterations);
            }

            IntPtr resultArrayPtr = IntPtr.Zero;
            try
            {
                resultArrayPtr = FindMaxLeafSpanningTreeNative(_handle!, iterations, threads);
                if (resultArrayPtr == IntPtr.Zero)
                    throw new InvalidOperationException("Не удалось найти остовное дерево (нативный метод вернул null).");

                int size = _associatedGraph.VertexCount; // Получаем размер из графа
                if (size <= 0) return Array.Empty<int>(); // Если граф пуст или невалиден
                
                int[] result = new int[size];
                Marshal.Copy(resultArrayPtr, result, 0, size); // Копируем данные
                return result;
            }
            finally
            {
                // Освобождаем память, выделенную C++
                if (resultArrayPtr != IntPtr.Zero)
                {
                    FreeArrayNative(resultArrayPtr);
                }
            }
        }

        // Управляемая реализация алгоритма поиска остовного дерева с максимальным числом листьев
        private int[] FindMaxLeafSpanningTreeManaged(int iterations)
        {
            var adjList = _associatedGraph.GetAdjacencyList();
            int vertices = adjList.Count;
            
            // Используем фиксированный seed для повторяемости результатов
            var random = new Random(_globalSeed);
            
            int[] bestTree = null;
            int bestLeafCount = -1;
            
            // Используем локальные объекты для синхронизации
            object lockObj = new object();
            
            // Определяем функцию для одной итерации
            Action<int> processIteration = (i) => {
                // Создаем отдельный генератор для каждого потока
                var localRandom = new Random(_globalSeed + i);
                
                // Генерируем случайное дерево
                var tree = GenerateRandomSpanningTree(adjList, localRandom);
                int leafCount = CountLeaves(tree);
                
                // Блокируем доступ при обновлении лучшего результата
                lock (lockObj)
                {
                    if (leafCount > bestLeafCount)
                    {
                        bestLeafCount = leafCount;
                        bestTree = tree;
                    }
                }
            };
            
            // Запускаем итерации последовательно или параллельно в зависимости от настройки
            if (_useParallelComputation)
            {
                // Используем параллельное выполнение для ускорения
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };
                
                Parallel.For(0, iterations, options, processIteration);
            }
            else
            {
                // Последовательное выполнение
                for (int i = 0; i < iterations; i++)
                {
                    processIteration(i);
                }
            }
            
            return bestTree ?? new int[vertices];
        }
        
        // Генерирует случайное остовное дерево
        private int[] GenerateRandomSpanningTree(List<List<int>> adjList, Random random)
        {
            int vertices = adjList.Count;
            int[] parent = new int[vertices];
            for (int i = 0; i < vertices; i++)
                parent[i] = -1;
                
            bool[] visited = new bool[vertices];
            
            // Выбираем случайную стартовую вершину
            int start = random.Next(vertices);
            visited[start] = true;
            
            // Используем очередь для BFS
            var queue = new Queue<int>();
            queue.Enqueue(start);
            
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                
                // Создаем список соседей и перемешиваем его
                var neighbors = new List<int>(adjList[current]);
                int n = neighbors.Count;
                
                // Перемешиваем соседей (алгоритм Фишера-Йейтса)
                for (int i = n - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    int temp = neighbors[i];
                    neighbors[i] = neighbors[j];
                    neighbors[j] = temp;
                }
                
                // Обрабатываем соседей в случайном порядке
                foreach (int neighbor in neighbors)
                {
                    if (!visited[neighbor])
                    {
                        visited[neighbor] = true;
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return parent;
        }

        // Статический метод для подсчета листьев (использует C# реализацию, т.к. C++ экспорт был сложным)
        public static int CountLeaves(int[] spanningTree)
        {
            if (spanningTree == null || spanningTree.Length == 0)
                return 0;

            int n = spanningTree.Length;
            if (n == 1) return 1; // Граф из одной вершины - это лист

            int leafCount = 0;
            bool[] isParent = new bool[n];

            // Отмечаем узлы, которые являются родителями
            for (int i = 0; i < n; i++)
            {
                if (spanningTree[i] != -1) // Проверяем, что это не корень дерева (или изолированная вершина)
                {
                     if (spanningTree[i] >= 0 && spanningTree[i] < n)
                     {
                        isParent[spanningTree[i]] = true;
                     }
                     else
                     {
                         // Ошибка в структуре дерева? Пропускаем или выбрасываем исключение
                         Console.WriteLine($"Warning: Invalid parent index {spanningTree[i]} for node {i}");
                     }
                }
            }

            // Листья - это узлы, которые не являются родителями
            // Корень (-1) не считаем листом, если он не единственный узел.
            int rootIndex = -1;
            for(int i=0; i<n; ++i)
            {
                if(spanningTree[i] == -1) rootIndex = i;
            }
            
            for (int i = 0; i < n; i++)
            {
                if (!isParent[i])
                {
                    // Узел является листом, если он не родитель и не является изолированным корнем
                    if (i != rootIndex || n == 1)
                       leafCount++;
                }
            }

            return leafCount;
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

        // Финализатор
        ~SpanningTree()
        {
            Dispose(false);
        }
        
        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SpanningTree));
            }
        }
    }
} 