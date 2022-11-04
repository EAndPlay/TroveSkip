cmake_minimum_required(VERSION 3.17)
project(Dll)

set(CMAKE_CXX_STANDARD 14)

add_library(Dll SHARED main.cpp main.h)