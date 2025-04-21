#pragma once

#include <vector>
#include <queue>
#include <string>
#include <stdexcept>

class Graph {
public:
    explicit Graph(int vertices);
    
    // Основные операции с графом
    void addEdge(int u, int v);
    void removeEdge(int u, int v);
    bool isConnected() const;
    int getVertexCount() const { return V; }
    const std::vector<std::vector<int>>& getAdjacencyList() const { return adj; }
    
    // Загрузка графа из файла
    static Graph fromFile(const std::string& filename);
    void saveToFile(const std::string& filename) const;
    
    // Генерация случайного графа
    static Graph generateRandom(int vertices, double edgeProbability);

private:
    int V;  // количество вершин
    std::vector<std::vector<int>> adj;  // список смежности
}; 