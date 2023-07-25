#include "pch.h"
#include "JACpp.h"


void passImageToCpp(unsigned char* inputArray, unsigned char* outputArray, int width, int height, int start, int stop) {
	int filter[3][3] = { {-1, 0, 1}, {-1, 0, 1}, {-1, 0, 1} };
	int realInputWidth = ((width + 1) * 3) & ~3;
	int realOutputWidth = ((width - 1) * 3) & ~3;

	for (int y = start; y < stop; y++) {
		for (int x = 0; x < width - 2; x++) {
			int sum = 0;
			for (int y1 = 0; y1 < 3; y1++) {
				for (int x1 = 0; x1 < 3; x1++) {
					int B = inputArray[(y + y1) * realInputWidth + (x + x1) * 3 + 0];
					int G = inputArray[(y + y1) * realInputWidth + (x + x1) * 3 + 1];
					int R = inputArray[(y + y1) * realInputWidth + (x + x1) * 3 + 2];
					int S = (R + G + B) / 9;
					sum += S * filter[y1][x1];
				}
			}
			if (sum < 0) { sum = 0; }
			outputArray[y * realOutputWidth + x * 3 + 0] = (unsigned char) sum;
			outputArray[y * realOutputWidth + x * 3 + 1] = (unsigned char) sum;
			outputArray[y * realOutputWidth + x * 3 + 2] = (unsigned char) sum;
		}
	}
}