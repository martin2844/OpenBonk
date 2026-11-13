// Private pinned-version adapter for Unity Crunch six-face DXT5 cubemaps.
// The caller must validate the Steam source hash before passing input here.
#include <cstdint>
#include <vector>
#include <jni.h>
#include "crn_decomp.h"

extern "C" JNIEXPORT jbyteArray JNICALL Java_com_openbonk_dataprep_SteamCubemapResources_unpackCrunch(JNIEnv* env,jclass,jbyteArray input){
    if(!input)return nullptr;
    try{
        const jsize size=env->GetArrayLength(input);if(size<32||size>4*1024*1024)return nullptr;
        std::vector<unsigned char> data(size);env->GetByteArrayRegion(input,0,size,reinterpret_cast<jbyte*>(data.data()));if(env->ExceptionCheck())return nullptr;
        unitycrnd::crn_texture_info info;
        if(!unitycrnd::crnd_get_texture_info(data.data(),static_cast<unitycrnd::uint32>(data.size()),&info)||info.m_width!=512||info.m_height!=512||info.m_faces!=6||info.m_levels!=1||unitycrnd::crnd_get_bytes_per_dxt_block(info.m_format)!=16||unitycrnd::crnd_get_fundamental_dxt_format(info.m_format)!=cCRNFmtDXT5)return nullptr;
        constexpr unsigned faceSize=512*512;std::vector<unsigned char> faces(6*faceSize);void* ptrs[6];for(unsigned i=0;i<6;i++)ptrs[i]=faces.data()+i*faceSize;
        auto context=unitycrnd::crnd_unpack_begin(data.data(),static_cast<unitycrnd::uint32>(data.size()));if(!context)return nullptr;
        bool ok=unitycrnd::crnd_unpack_level(context,ptrs,faceSize,(512/4)*16,0);unitycrnd::crnd_unpack_end(context);if(!ok)return nullptr;
        jbyteArray out=env->NewByteArray(faces.size());if(!out)return nullptr;env->SetByteArrayRegion(out,0,faces.size(),reinterpret_cast<const jbyte*>(faces.data()));return out;
    }catch(...){return nullptr;}
}
