// Experimental JNI adapter used by opt-in private texture research builds.
#include "steam_raw_texture_probe.cpp"
#include "Etc.h"

static bool convert_quality(const std::vector<uint8_t>& source, int width, int height, int sourceFormat, std::vector<uint8_t>& result) {
    if (width < 1 || height < 1 || width > 2048 || height > 2048 || source.empty() || source.size() > 64 * 1024 * 1024) return false;
    std::vector<uint8_t> rgba;
    if (!convert(source, width, height, sourceFormat, 4, rgba)) return false;
    std::vector<float> pixels(rgba.size());
    for (size_t i = 0; i < rgba.size(); ++i) pixels[i] = rgba[i] / 255.0f;
    unsigned char* raw = nullptr; unsigned int size = 0, w = 0, h = 0; int elapsed = 0;
    Etc::Encode(pixels.data(), width, height, Etc::Image::Format::RGBA8, Etc::ErrorMetric::BT709,
                40.0f, 1, 1, &raw, &size, &w, &h, &elapsed);
    std::unique_ptr<unsigned char[]> output(raw);
    if (!output || w != unsigned((width + 3) & ~3) || h != unsigned((height + 3) & ~3) || size != w * h) return false;
    result.assign(output.get(), output.get() + size);
    return true;
}

extern "C" int openbonk_quality_mip(const uint8_t* input, size_t length, int width, int height, int sourceFormat,
                                   uint8_t* output, size_t capacity) {
    if (!input || !output || length == 0 || length > 64 * 1024 * 1024) return -1;
    try {
        std::vector<uint8_t> source(input, input + length), result;
        if (!convert_quality(source, width, height, sourceFormat, result) || result.size() > capacity) return -1;
        std::copy(result.begin(), result.end(), output);
        return static_cast<int>(result.size());
    } catch (...) { return -1; }
}

extern "C" JNIEXPORT jbyteArray JNICALL
Java_com_openbonk_dataprep_SteamConvertedTextures_convertQualityMip(
        JNIEnv* env, jclass, jbyteArray input, jint width, jint height, jint sourceFormat) {
    if (!input) return nullptr;
    try {
        const jsize length = env->GetArrayLength(input);
        if (length <= 0 || length > 64 * 1024 * 1024) return nullptr;
        std::vector<uint8_t> source(length), output;
        env->GetByteArrayRegion(input, 0, length, reinterpret_cast<jbyte*>(source.data()));
        if (env->ExceptionCheck() || !convert_quality(source, width, height, sourceFormat, output)) return nullptr;
        jbyteArray result = env->NewByteArray(output.size());
        if (!result) return nullptr;
        env->SetByteArrayRegion(result, 0, output.size(), reinterpret_cast<const jbyte*>(output.data()));
        return result;
    } catch (...) { return nullptr; }
}
