#pragma once

#ifdef CPPDLL_EXPORTS
#define CPPDLL_API __declspec(dllexport)
#else
#define CPPDLL_API __declspec(dllimport)
#endif

extern "C" CPPDLL_API void passImageToCpp(unsigned char* inputArray, unsigned char* outputArray, int width, int height, int start, int stop);