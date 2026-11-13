// Local ETC2 encoder compatibility probe. The etcpak source is an external
// dependency and this executable is not an OpenBonk release component.
#include <cstdint>
#include <cstdlib>
#include <fstream>
#include <iostream>
#include <vector>
#include "ProcessRGB.hpp"

int main(int argc,char** argv){
    if(argc!=6){std::cerr<<"input output width height rgb|rgba\n";return 2;}
    long w=std::strtol(argv[3],nullptr,10),h=std::strtol(argv[4],nullptr,10);std::string mode=argv[5];
    if(w<=0||h<=0||w>4096||h>4096||w%4||h%4||(mode!="rgb"&&mode!="rgba")){std::cerr<<"invalid dimensions or mode\n";return 2;}
    std::ifstream in(argv[1],std::ios::binary|std::ios::ate);if(!in||in.tellg()!=w*h*4){std::cerr<<"invalid input\n";return 2;}
    in.seekg(0);std::vector<uint32_t> pixels(static_cast<size_t>(w*h));in.read(reinterpret_cast<char*>(pixels.data()),w*h*4);if(!in)return 2;
    for(auto& p:pixels)p=(p&0xff00ff00u)|((p&0x00ff0000u)>>16)|((p&0x000000ffu)<<16);
    size_t bytes=static_cast<size_t>(w*h)/(mode=="rgb"?2:1);std::vector<uint64_t> result(bytes/8);
    if(mode=="rgb")CompressEtc2Rgb(pixels.data(),result.data(),static_cast<uint32_t>(w*h/16),static_cast<size_t>(w),true);
    else CompressEtc2Rgba(pixels.data(),result.data(),static_cast<uint32_t>(w*h/16),static_cast<size_t>(w),true);
    std::ofstream out(argv[2],std::ios::binary|std::ios::trunc);out.write(reinterpret_cast<const char*>(result.data()),bytes);if(!out)return 2;
    std::cout<<"encoded "<<w<<"x"<<h<<" "<<mode<<" bytes="<<bytes<<"\n";
}
