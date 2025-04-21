#include "core/graph.hpp"
#include "core/spanning_tree.hpp"
#include <memory>
#include <vector>

extern "C" {

Graph* CreateGraph(int vertices) {
    try {
        return new Graph(vertices);
    } catch (...) {
        return nullptr;
    }
}

void DeleteGraph(Graph* graph) {
    delete graph;
}

void AddEdge(Graph* graph, int v1, int v2) {
    if (graph) {
        graph->addEdge(v1, v2);
    }
}

bool IsConnected(Graph* graph) {
    return graph ? graph->isConnected() : false;
}

Graph* GenerateRandomGraph(int vertices, double probability) {
    try {
        return new Graph(Graph::generateRandom(vertices, probability));
    } catch (...) {
        return nullptr;
    }
}

Graph* LoadGraphFromFile(const char* filename) {
    try {
        return new Graph(Graph::fromFile(filename));
    } catch (...) {
        return nullptr;
    }
}

void SaveGraphToFile(Graph* graph, const char* filename) {
    if (graph) {
        graph->saveToFile(filename);
    }
}

int GetVertexCount(Graph* graph) {
    return graph ? graph->getVertexCount() : 0;
}

int* GetAdjacencyList(Graph* graph, int vertex) {
    if (!graph) return nullptr;

    try {
        const auto& list = graph->getAdjacencyList()[vertex];
        auto result = new int[list.size()];
        std::copy(list.begin(), list.end(), result);
        return result;
    } catch (...) {
        return nullptr;
    }
}

SpanningTree* CreateSpanningTree(Graph* graph) {
    try {
        return new SpanningTree(*graph);
    } catch (...) {
        return nullptr;
    }
}

void DeleteSpanningTree(SpanningTree* spanningTree) {
    delete spanningTree;
}

int* FindMaxLeafSpanningTree(SpanningTree* spanningTree, int iterations, int numThreads) {
    if (!spanningTree) return nullptr;

    try {
        auto tree = spanningTree->findMaxLeafSpanningTree(iterations, numThreads);
        auto result = new int[tree.size()];
        std::copy(tree.begin(), tree.end(), result);
        return result;
    } catch (...) {
        return nullptr;
    }
}

void SaveTreeToFile(SpanningTree* spanningTree, int* tree, const char* filename) {
    if (spanningTree && tree) {
        std::vector<int> treeVec(tree, tree + spanningTree->getGraph().getVertexCount());
        spanningTree->visualizeTree(treeVec, filename);
    }
}

int CountLeaves(int* tree, int size) {
    if (!tree || size <= 0) return 0;
    return SpanningTree::countLeaves(std::vector<int>(tree, tree + size));
}

} 