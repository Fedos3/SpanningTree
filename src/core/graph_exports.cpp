#include "../../include/core/graph.hpp"
#include <cstdlib>
#include <vector>
#include <string>

#ifdef _WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API
#endif

extern "C" {

// Создание и уничтожение графа
EXPORT_API Graph* Graph_Create(int vertices) {
    try {
        return new Graph(vertices);
    } catch (const std::exception&) {
        return nullptr;
    }
}

EXPORT_API void Graph_Destroy(Graph* graph) {
    if (graph) {
        delete graph;
    }
}

// Добавление ребра
EXPORT_API void Graph_AddEdge(Graph* graph, int u, int v) {
    if (graph) {
        try {
            graph->addEdge(u, v);
        } catch (const std::exception&) {
            // Обработка ошибок
        }
    }
}

// Удаление ребра
EXPORT_API void Graph_RemoveEdge(Graph* graph, int u, int v) {
    if (graph) {
        try {
            graph->removeEdge(u, v);
        } catch (const std::exception&) {
            // Обработка ошибок
        }
    }
}

// Проверка связности
EXPORT_API bool Graph_IsConnected(Graph* graph) {
    if (graph) {
        try {
            return graph->isConnected();
        } catch (const std::exception&) {
            return false;
        }
    }
    return false;
}

// Получение количества вершин
EXPORT_API int Graph_GetVertexCount(Graph* graph) {
    if (graph) {
        return graph->getVertexCount();
    }
    return 0;
}

// Генерация случайного графа
EXPORT_API Graph* Graph_GenerateRandom(int vertices, double probability) {
    try {
        Graph randomGraph = Graph::generateRandom(vertices, probability);
        return new Graph(randomGraph);
    } catch (const std::exception&) {
        return nullptr;
    }
}

// Загрузка графа из файла
EXPORT_API Graph* Graph_FromFile(const char* filename) {
    try {
        Graph loadedGraph = Graph::fromFile(filename);
        return new Graph(loadedGraph);
    } catch (const std::exception&) {
        return nullptr;
    }
}

// Сохранение графа в файл
EXPORT_API void Graph_SaveToFile(Graph* graph, const char* filename) {
    if (graph) {
        try {
            graph->saveToFile(filename);
        } catch (const std::exception&) {
            // Обработка ошибок
        }
    }
}

// Получение списка смежности
EXPORT_API int** Graph_GetAdjacencyList(Graph* graph, int* size) {
    if (graph && size) {
        try {
            const auto& adjList = graph->getAdjacencyList();
            *size = static_cast<int>(adjList.size());
        
            return nullptr;
        } catch (const std::exception&) {
            *size = 0;
            return nullptr;
        }
    }
    if (size) {
        *size = 0;
    }
    return nullptr;
}

} // extern "C" 