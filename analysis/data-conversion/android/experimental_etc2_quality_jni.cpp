// Experimental JNI adapter. This is not yet used by the launcher or plans.
#include "steam_raw_texture_probe.cpp"
#include "Etc.h"

extern "C" JNIEXPORT jbyteArray JNICALL
Java_com_openbonk_dataprep_SteamConvertedTextures_convertQualityMip(
        JNIEnv* env, jclass, jbyteArray input, jint width, jint height, jint sourceFormat) {
    if (!input || width < 1 || height < 1 || width > 2048 || height > 2048) return nullptr;
    try {
        const jsize length = env->GetArrayLength(input);
        if (length <= 0 || length > 64 * 1024 * 1024) return nullptr;
        std::vector<uint8_t> source(length), rgba;
        env->GetByteArrayRegion(input, 0, length, reinterpret_cast<jbyte*>(source.data()));
        if (env->ExceptionCheck() || !convert(source, width, height, sourceFormat, 4, rgba)) return nullptr;
        std::vector<float> pixels(rgba.size());
        for (size_t i = 0; i < rgba.size(); ++i) pixels[i] = rgba[i] / 255.0f;
        unsigned char* raw = nullptr; unsigned int size = 0, w = 0, h = 0; int elapsed = 0;
        Etc::Encode(pixels.data(), width, height, Etc::Image::Format::RGBA8, Etc::ErrorMetric::BT709,
                    40.0f, 1, 1, &raw, &size, &w, &h, &elapsed);
        std::unique_ptr<unsigned char[]> output(raw);
        if (!output || w != unsigned((width + 3) & ~3) || h != unsigned((height + 3) & ~3) || size != w * h) return nullptr;
        jbyteArray result = env->NewByteArray(size);
        if (!result) return nullptr;
        env->SetByteArrayRegion(result, 0, size, reinterpret_cast<const jbyte*>(output.get()));
        return result;
    } catch (...) { return nullptr; }
}
