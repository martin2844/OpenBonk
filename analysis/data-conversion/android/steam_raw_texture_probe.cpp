// Experimental per-mip desktop RGB24/RGBA32/BC1/BC3/BC7 converter.
// ETC encoder is an external dependency; this is not a release component.
// BC1/BC3 block decoding follows Pillow 13.0.0 BcnDecode.c, CC0/public domain.
#include <algorithm>
#include <cstdint>
#include <cmath>
#include <cstdlib>
#include <fstream>
#include <iostream>
#include <vector>
#include "ProcessRGB.hpp"
#define BCDEC_STATIC
#define BCDEC_IMPLEMENTATION
#include "bcdec.h"
#ifdef OPENBONK_JNI
#include <jni.h>
#endif

static void decode_block(const uint8_t *input, bool bc3, uint32_t *out) {
    const uint8_t *p = input + (bc3 ? 8 : 0);
    unsigned c0 = p[0] | unsigned(p[1]) << 8, c1 = p[2] | unsigned(p[3]) << 8;
    uint8_t palette[4][4] = {};
    auto set565 = [&](unsigned c, int i) {
        unsigned r=(c>>11)&31, g=(c>>5)&63, b=c&31;
        palette[i][0]=(b<<3)|(b>>2); palette[i][1]=(g<<2)|(g>>4);
        palette[i][2]=(r<<3)|(r>>2); palette[i][3]=255;
    };
    set565(c0, 0); set565(c1, 1);
    if (c0 > c1 || bc3) {
        for (int j=0; j<3; ++j) {
            palette[2][j]=(2*palette[0][j]+palette[1][j])/3;
            palette[3][j]=(palette[0][j]+2*palette[1][j])/3;
        }
        palette[2][3]=palette[3][3]=255;
    } else {
        for (int j=0; j<3; ++j) palette[2][j]=(palette[0][j]+palette[1][j])/2;
        palette[2][3]=255;
    }
    uint8_t alpha[8]={input[0],input[1]};
    if (bc3) {
        if (alpha[0]>alpha[1]) for(int i=2;i<8;++i) alpha[i]=((8-i)*alpha[0]+(i-1)*alpha[1])/7;
        else { for(int i=2;i<6;++i) alpha[i]=((6-i)*alpha[0]+(i-1)*alpha[1])/5; alpha[6]=0; alpha[7]=255; }
    }
    uint64_t bits=0; if(bc3) for(int i=0;i<6;++i) bits|=uint64_t(input[i+2])<<(8*i);
    for (int i=0;i<16;++i) {
        const uint8_t *v=palette[(p[4+i/4]>>(2*(i%4)))&3];
        unsigned a=bc3 ? alpha[(bits>>(3*i))&7] : v[3];
        out[i]=uint32_t(v[0])|(uint32_t(v[1])<<8)|(uint32_t(v[2])<<16)|(a<<24);
    }
}

static bool convert(const std::vector<uint8_t>& source, long w, long h, long fmt, long target, std::vector<uint8_t>& result) {
    if (w < 1 || h < 1 || w > 4096 || h > 4096 || (fmt != 1 && fmt != 3 && fmt != 4 && fmt != 10 && fmt != 12 && fmt != 25) || (target != 1 && target != 4 && target != 34 && target != 45 && target != 47)) return false;
    const size_t pw = (w + 3) & ~size_t(3), ph = (h + 3) & ~size_t(3);
    const size_t source_size = fmt <= 4 ? size_t(w) * h * fmt : pw*ph/(fmt == 10 ? 2 : 1);
    if(source.size()!=source_size) return false;
    if(target==1) { if(fmt!=1) return false; result=source; return true; }
    std::vector<uint32_t> bgra(pw * ph);
    if (fmt >= 10) {
        for (size_t y=0; y<ph/4; ++y) for (size_t x=0; x<pw/4; ++x) {
            uint32_t block[16];const uint8_t* encoded=source.data()+(y*pw/4+x)*(fmt==10?8:16);
            if(fmt==25){bcdec_bc7(encoded,block,16);for(auto &p:block)p=(p&0xff00ff00u)|((p&0x00ff0000u)>>16)|((p&0x000000ffu)<<16);}
            else decode_block(encoded,fmt==12,block);
            for(size_t row=0;row<4;++row) for(size_t col=0;col<4;++col) bgra[(y*4+row)*pw+x*4+col]=block[row*4+col];
        }
    }
    for (size_t y = 0; y < ph; ++y) for (size_t x = 0; x < pw; ++x) {
        size_t xx=std::min(x,size_t(w-1)), yy=std::min(y,size_t(h-1));
        if(fmt>=10) { if(x>=size_t(w)||y>=size_t(h)) bgra[y*pw+x]=bgra[yy*pw+xx]; }
        else { size_t s = (yy * w + xx) * fmt;
            if(fmt==1) bgra[y*pw+x]=uint32_t(source[s])<<24;
            else bgra[y*pw+x] = uint32_t(source[s+2]) | (uint32_t(source[s+1]) << 8) |
                                (uint32_t(source[s]) << 16) | (uint32_t(fmt == 4 ? source[s+3] : 255) << 24); }
    }
    if (target == 4) {
        result.reserve(size_t(w)*h*4);
        for(long y=0;y<h;++y) for(long x=0;x<w;++x) {
            uint32_t p=bgra[y*pw+x]; result.push_back(p>>16); result.push_back(p>>8); result.push_back(p); result.push_back(p>>24);
        }
        return true;
    }
    const size_t bytes = pw * ph / (target == 34 || target == 45 ? 2 : 1);
    std::vector<uint64_t> encoded(bytes / 8);
    if (target == 34) CompressEtc1Rgb(bgra.data(), encoded.data(), pw*ph/16, pw);
    else if (target == 45) CompressEtc2Rgb(bgra.data(), encoded.data(), pw*ph/16, pw, true);
    else CompressEtc2Rgba(bgra.data(), encoded.data(), pw*ph/16, pw, true);
    const uint8_t* first = reinterpret_cast<const uint8_t*>(encoded.data());
    result.assign(first,first+bytes); return true;
}

#ifdef OPENBONK_JNI
extern "C" JNIEXPORT jstring JNICALL Java_com_openbonk_dataprep_ArmTextureTestActivity_nativeAbi(JNIEnv* env, jclass) {
#if defined(__aarch64__)
    return env->NewStringUTF("arm64");
#elif defined(__x86_64__)
    return env->NewStringUTF("x86_64");
#else
    return env->NewStringUTF("other");
#endif
}
extern "C" JNIEXPORT jbyteArray JNICALL Java_com_openbonk_dataprep_SteamConvertedTextures_convertMip(JNIEnv* env, jclass, jbyteArray input, jint w, jint h, jint fmt, jint target) {
    if (!input) return nullptr;
    try {
        jsize n=env->GetArrayLength(input); if(n<0 || n>64*1024*1024) return nullptr;
        std::vector<uint8_t> source(n),result;
        env->GetByteArrayRegion(input,0,n,reinterpret_cast<jbyte*>(source.data()));
        if(env->ExceptionCheck() || !convert(source,w,h,fmt,target,result)) return nullptr;
        jbyteArray output=env->NewByteArray(result.size()); if(!output) return nullptr;
        env->SetByteArrayRegion(output,0,result.size(),reinterpret_cast<const jbyte*>(result.data()));
        return output;
    } catch (...) { return nullptr; }
}
extern "C" JNIEXPORT jbyteArray JNICALL Java_com_openbonk_dataprep_SteamConvertedTextures_convertNormalMip(JNIEnv* env, jclass, jbyteArray input, jint w, jint h, jint target) {
    if (!input || (target!=4 && target!=34)) return nullptr;
    try {
        jsize n=env->GetArrayLength(input);if(n<0 || n>64*1024*1024)return nullptr;
        std::vector<uint8_t> source(n),rgba,result;env->GetByteArrayRegion(input,0,n,reinterpret_cast<jbyte*>(source.data()));
        if(env->ExceptionCheck() || !convert(source,w,h,12,4,rgba))return nullptr;
        for(size_t i=0;i<rgba.size();i+=4){
            const unsigned x=rgba[i+3],y=rgba[i+1];const double dx=x/127.5-1.0,dy=y/127.5-1.0;
            const double magnitude=std::max(0.0,1.0-dx*dx-dy*dy);const unsigned z=static_cast<unsigned>(std::nearbyint(std::sqrt(magnitude)*127.5+127.5));
            rgba[i]=x;rgba[i+1]=y;rgba[i+2]=static_cast<uint8_t>(z);rgba[i+3]=255;
        }
        if(!convert(rgba,w,h,4,target,result))return nullptr;
        jbyteArray output=env->NewByteArray(result.size());if(!output)return nullptr;env->SetByteArrayRegion(output,0,result.size(),reinterpret_cast<const jbyte*>(result.data()));return output;
    } catch (...) { return nullptr; }
}
#else
int main(int argc, char **argv) {
    if (argc != 7) { std::cerr << "input output width height 3|4|10|12 rgb|rgba|raw\n"; return 2; }
    char *end = nullptr;
    auto number = [&](const char *v) -> long { long n = std::strtol(v, &end, 10); return *end ? -1 : n; };
    const long w=number(argv[3]),h=number(argv[4]),fmt=number(argv[5]); const std::string mode=argv[6];
    long target=mode=="raw"?4:mode=="rgb"?45:mode=="rgba"?47:-1;
    std::ifstream in(argv[1],std::ios::binary|std::ios::ate); if(!in || in.tellg()<0 || in.tellg()>64*1024*1024) return 2;
    size_t n=in.tellg(); in.seekg(0); std::vector<uint8_t> source(n),result;
    if(!in.read(reinterpret_cast<char*>(source.data()),n) || !convert(source,w,h,fmt,target,result)) return 2;
    std::ofstream out(argv[2], std::ios::binary | std::ios::trunc);
    return out.write(reinterpret_cast<const char*>(result.data()), result.size()) ? 0 : 2;
}
#endif
