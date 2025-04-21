#include "core/spanning_tree.hpp"
#include <fstream>
#include <thread>
#include <mutex>
#include <atomic>
#include <algorithm>
#include <queue>
#include <iostream>

#ifdef _OPENMP
#include <omp.h>
#endif

SpanningTree::SpanningTree(const Graph& g) : graph(g) {
    if (!graph.isConnected()) {
        throw std::invalid_argument("Graph must be connected");
    }
}

std::vector<int> SpanningTree::generateRandomSpanningTree(std::mt19937& rng) const {
    const int V = graph.getVertexCount();
    std::vector<int> parent(V, -1);
    if (V <= 1) return parent;

    std::vector<bool> visited(V, false);
    std::uniform_int_distribution<int> dist(0, V - 1);
    int start = dist(rng);
    
    std::queue<int> q;
    q.push(start);
    visited[start] = true;

    while (!q.empty()) {
        int u = q.front();
        q.pop();
        
        auto neighbors = graph.getAdjacencyList()[u];
        std::shuffle(neighbors.begin(), neighbors.end(), rng);
        
        for (int v : neighbors) {
            if (!visited[v]) {
                visited[v] = true;
                parent[v] = u;
                q.push(v);
            }
        }
    }
    
    return parent;
}

int SpanningTree::countLeaves(const std::vector<int>& parent) {
    const int n = static_cast<int>(parent.size());
    if (n == 0) return 0;
    if (n == 1) return 1;

    std::vector<int> degree(n, 0);
    for (int i = 0; i < n; ++i) {
        if (parent[i] != -1) {
            degree[i]++;
            degree[parent[i]]++;
        }
    }

    return static_cast<int>(std::count_if(degree.begin(), degree.end(), 
                        [](int d) { return d == 1; }));
}

std::vector<int> SpanningTree::findMaxLeafSpanningTree(int iterations, int numThreads) const {
    if (numThreads <= 0) {
#ifdef _OPENMP
        numThreads = omp_get_max_threads();
#else
        numThreads = static_cast<int>(std::thread::hardware_concurrency());
#endif
    }
    
    return findBestTreeParallel(iterations, numThreads);
}

std::vector<int> SpanningTree::findBestTreeParallel(int iterations, int numThreads) const {
    std::vector<int> bestTree;
    int bestLeafCount = -1;
    std::mutex mutex;
    std::random_device rd;
    std::mt19937 rng(rd());

#ifdef _OPENMP
    omp_set_num_threads(numThreads);
    #pragma omp parallel
    {
        std::mt19937 localRng(rng() + omp_get_thread_num());
        std::vector<int> localBestTree;
        int localBestLeafCount = -1;
        int iterationsPerThread = iterations / numThreads;

        for (int i = 0; i < iterationsPerThread; ++i) {
            auto tree = generateRandomSpanningTree(localRng);
            int leaves = countLeaves(tree);

            if (leaves > localBestLeafCount) {
                localBestLeafCount = leaves;
                localBestTree = tree;
            }
        }

        std::lock_guard<std::mutex> lock(mutex);
        if (localBestLeafCount > bestLeafCount) {
            bestLeafCount = localBestLeafCount;
            bestTree = localBestTree;
        }
    }
#else
    for (int i = 0; i < iterations; ++i) {
        auto tree = generateRandomSpanningTree(rng);
        int leaves = countLeaves(tree);

        if (leaves > bestLeafCount) {
            bestLeafCount = leaves;
            bestTree = tree;
        }
    }
#endif

    return bestTree;
}

void SpanningTree::visualizeTree(const std::vector<int>& parent, const std::string& filename) const {
    std::ofstream file(filename);
    if (!file.is_open()) {
        throw std::runtime_error("Не удалось открыть файл для записи: " + filename);
    }

    file << "digraph G {\n";
    file << "  node [shape=circle];\n";
    
    const int n = static_cast<int>(parent.size());
    for (int i = 0; i < n; ++i) {
        file << "  " << i << " [label=\"" << i << "\"];\n";
        if (parent[i] != -1) {
            file << "  " << parent[i] << " -> " << i << ";\n";
        }
    }
    
    file << "}\n";
}

std::vector<std::pair<int, int>> SpanningTree::findMaximumLeafSpanningTree() const {
    std::vector<std::pair<int, int>> edges;
    if (graph.getAdjacencyList().empty()) return edges;

    const int n = static_cast<int>(graph.getAdjacencyList().size());
    std::vector<int> degree(n, 0);
    
    // TODO: Реализовать алгоритм поиска остовного дерева с максимальным числом листьев
    return edges;
} 