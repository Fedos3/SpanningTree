cmake_minimum_required(VERSION 3.10)
project(SpanningTree)

# Опции компиляции
option(USE_OPENMP "Use OpenMP" ON)

# Настройка C++ стандарта
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Поиск OpenMP
if(USE_OPENMP)
    find_package(OpenMP)
    if(OpenMP_CXX_FOUND)
        set(OpenMP_LIBS OpenMP::OpenMP_CXX)
    endif()
endif()

# Исходные файлы
set(SOURCES
    src/core/graph.cpp
    src/core/spanning_tree.cpp
    src/core/graph_exports.cpp
    src/core/spanning_tree_exports.cpp
)

# Включаемые директории
include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

# Создаем динамическую библиотеку
add_library(SpanningTreeCore SHARED ${SOURCES})

# Настраиваем экспорт символов в Windows
if(WIN32)
    target_compile_definitions(SpanningTreeCore PRIVATE -DBUILD_DLL)
endif()

# Линкуем с OpenMP, если найден
if(OpenMP_CXX_FOUND)
    target_link_libraries(SpanningTreeCore PRIVATE ${OpenMP_LIBS})
endif()

# Копируем библиотеку в выходную директорию C# проекта
set(CSHARP_OUTPUT_DIR ${CMAKE_SOURCE_DIR}/bin/$<CONFIG>/net9.0-windows)
add_custom_command(
    TARGET SpanningTreeCore POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E make_directory ${CSHARP_OUTPUT_DIR}
    COMMAND ${CMAKE_COMMAND} -E copy
    $<TARGET_FILE:SpanningTreeCore>
    ${CSHARP_OUTPUT_DIR}/
) 