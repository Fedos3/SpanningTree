#include "core/graph.hpp"
#include <fstream>
#include <sstream>
#include <random>
#include <stdexcept>
#include <algorithm>

Graph::Graph(int vertices) {
    if (vertices < 0) {
        throw std::invalid_argument("Количество вершин должно быть неотрицательным");
    }
    V = vertices;
    adj.resize(vertices);
}

void Graph::addEdge(int v1, int v2) {
    if (v1 < 0 || v1 >= static_cast<int>(adj.size()) ||
        v2 < 0 || v2 >= static_cast<int>(adj.size())) {
        throw std::out_of_range("Vertex index out of range");
    }
    adj[v1].push_back(v2);
    adj[v2].push_back(v1);
}

void Graph::removeEdge(int v1, int v2) {
    if (v1 < 0 || v1 >= static_cast<int>(adj.size()) ||
        v2 < 0 || v2 >= static_cast<int>(adj.size())) {
        throw std::out_of_range("Vertex index out of range");
    }
    
    // Удаляем v2 из списка смежности v1
    auto& adj1 = adj[v1];
    adj1.erase(std::remove(adj1.begin(), adj1.end(), v2), adj1.end());
    
    // Удаляем v1 из списка смежности v2
    auto& adj2 = adj[v2];
    adj2.erase(std::remove(adj2.begin(), adj2.end(), v1), adj2.end());
}

bool Graph::isConnected() const {
    if (adj.empty()) return true;
    
    std::vector<bool> visited(adj.size(), false);
    std::queue<int> queue;
    queue.push(0);
    visited[0] = true;
    int visitedCount = 1;
    
    while (!queue.empty()) {
        int v = queue.front();
        queue.pop();
        
        for (int neighbor : adj[v]) {
            if (!visited[neighbor]) {
                visited[neighbor] = true;
                queue.push(neighbor);
                visitedCount++;
            }
        }
    }
    
    return visitedCount == static_cast<int>(adj.size());
}

Graph Graph::fromFile(const std::string& filename) {
    std::ifstream file(filename);
    if (!file.is_open()) {
        throw std::runtime_error("Не удалось открыть файл: " + filename);
    }
    
    int n, m;
    file >> n >> m;
    
    Graph graph(n);
    for (int i = 0; i < m; ++i) {
        int u, v;
        file >> u >> v;
        graph.addEdge(u, v);
    }
    
    return graph;
}

void Graph::saveToFile(const std::string& filename) const {
    std::ofstream file(filename);
    if (!file.is_open()) {
        throw std::runtime_error("Не удалось открыть файл для записи: " + filename);
    }
    
    int edges = 0;
    for (const auto& neighbors : adj) {
        edges += static_cast<int>(neighbors.size());
    }
    edges /= 2;  // каждое ребро учтено дважды
    
    file << V << " " << edges << "\n";
    
    for (int u = 0; u < V; ++u) {
        for (int v : adj[u]) {
            if (u < v) {  // записываем каждое ребро только один раз
                file << u << " " << v << "\n";
            }
        }
    }
}

Graph Graph::generateRandom(int vertices, double edgeProbability) {
    if (vertices < 0) {
        throw std::invalid_argument("Количество вершин должно быть неотрицательным");
    }
    if (edgeProbability < 0.0 || edgeProbability > 1.0) {
        throw std::invalid_argument("Вероятность должна быть в диапазоне [0, 1]");
    }
    
    Graph graph(vertices);
    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_real_distribution<> dis(0.0, 1.0);
    
    for (int u = 0; u < vertices; ++u) {
        for (int v = u + 1; v < vertices; ++v) {
            if (dis(gen) < edgeProbability) {
                graph.addEdge(u, v);
            }
        }
    }
    
    return graph;
} 