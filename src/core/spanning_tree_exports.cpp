#include "../../include/core/spanning_tree.hpp"
#include <cstdlib>
#include <vector>
#include <string>
#include <stdexcept>

#ifdef _WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API
#endif

extern "C" {

// Создание и уничтожение SpanningTree
EXPORT_API SpanningTree* SpanningTree_Create(Graph* graph) {
    if (!graph) return nullptr;
    
    try {
        // Создаем копию графа, чтобы SpanningTree владел своей копией
        // Либо убедитесь, что Graph передается по константной ссылке и живет дольше SpanningTree
        return new SpanningTree(*graph); 
    } catch (const std::exception& e) {
        // Можно добавить логирование ошибки e.what()
        return nullptr;
    }
}

EXPORT_API void SpanningTree_Destroy(SpanningTree* tree) {
    if (tree) {
        delete tree;
    }
}

// Поиск остовного дерева с максимальным числом листьев
EXPORT_API int* SpanningTree_FindMaxLeafSpanningTree(SpanningTree* tree, int iterations, int threads) {
    if (!tree) return nullptr;
    
    try {
        std::vector<int> result = tree->findMaxLeafSpanningTree(iterations, threads);
        
        // Копируем результат в память, которую должен будет освободить вызывающий код C# через FreeArray
        int* resultArray = static_cast<int*>(malloc(result.size() * sizeof(int)));
        if (!resultArray) {
            return nullptr; // Ошибка выделения памяти
        }
        if (!result.empty()) {
            memcpy(resultArray, result.data(), result.size() * sizeof(int));
        }
        return resultArray;
    } catch (const std::exception& e) {
        // Можно добавить логирование ошибки e.what()
        return nullptr;
    }
}

// Подсчет числа листьев - принимает размер, но логика подсчета остается в C#
EXPORT_API int SpanningTree_CountLeaves(SpanningTree* tree, int* spanningTreeArray, int size) {
    // Эта функция больше не используется напрямую из C# в текущей реализации
    // Оставлена для возможного будущего использования или для совместимости
    if (!tree || !spanningTreeArray || size <= 0) return 0;
    
    try {
        std::vector<int> treeVector(spanningTreeArray, spanningTreeArray + size);
        return SpanningTree::countLeaves(treeVector);
    } catch (const std::exception& e) {
        // Можно добавить логирование ошибки e.what()
        return 0;
    }
}

// Освобождение памяти, выделенной для массива FindMaxLeafSpanningTreeNative
EXPORT_API void FreeArray(void* array) {
    if (array) {
        free(array);
    }
}

} // extern "C" 