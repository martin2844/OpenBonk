// Experimental standalone RGBA-to-ETC2 probe; not used by the APK pipeline.
#include "Etc.h"
#include <cstdint>
#include <cstdlib>
#include <fstream>
#include <vector>
int main(int argc, char **argv) {
    if (argc != 5) return 2;
    const int width = std::atoi(argv[3]), height = std::atoi(argv[4]);
    if (width <= 0 || height <= 0 || width > 4096 || height > 4096) return 2;
    const size_t size = size_t(width) * size_t(height) * 4;
    std::vector<uint8_t> input(size);
    std::ifstream in(argv[1], std::ios::binary);
    if (!in.read(reinterpret_cast<char*>(input.data()), size) || in.peek() != std::char_traits<char>::eof()) return 2;
    std::vector<float> rgba(size);
    for (size_t i = 0; i < size; i++) rgba[i] = input[i] / 255.0f;
    unsigned char *output = nullptr; unsigned int length = 0, w = 0, h = 0; int elapsed = 0;
    Etc::Encode(rgba.data(), width, height, Etc::Image::Format::RGBA8, Etc::ErrorMetric::BT709,
                40.0f, 1, 1, &output, &length, &w, &h, &elapsed);
    if (!output || w != unsigned((width+3)&~3) || h != unsigned((height+3)&~3) || length != w*h) { delete[] output; return 3; }
    std::ofstream out(argv[2], std::ios::binary | std::ios::trunc);
    out.write(reinterpret_cast<char*>(output), length); delete[] output;
    return out ? 0 : 4;
}
