#pragma once

#include "graph.hpp"
#include <vector>
#include <string>
#include <random>
#include <functional>

class SpanningTree {
public:
    // Конструктор принимает граф
    explicit SpanningTree(const Graph& graph);
    
    // Находит остовное дерево с максимальным числом листьев
    std::vector<int> findMaxLeafSpanningTree(int iterations = 10000, int numThreads = 0) const;
    
    // Подсчет числа листьев в дереве
    static int countLeaves(const std::vector<int>& parent);
    
    // Визуализация дерева
    void visualizeTree(const std::vector<int>& parent, const std::string& filename) const;

    const Graph& getGraph() const { return graph; }

private:
    const Graph& graph;  // Безопасная ссылка на граф
    
    // Генерация случайного остовного дерева
    std::vector<int> generateRandomSpanningTree(std::mt19937& rng) const;
    
    // Параллельный поиск лучшего дерева
    std::vector<int> findBestTreeParallel(int iterations, int numThreads) const;

    std::vector<std::pair<int, int>> findMaximumLeafSpanningTree() const;
}; 