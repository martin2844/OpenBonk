// Local research adapter for texture2ddecoder Unity Crunch crn_decomp.h.
// Uses the library's six-face API; source data remains owned by the user.
// Build against the separately obtained texture2ddecoder Unity Crunch headers.
// The caller must verify the input game version and hash before invoking this
// narrow prototype. It is not a general-purpose untrusted CRN parser.
#include <cstdint>
#include <cstring>
#include <cstdlib>
#include <climits>
#include <cstdio>
#include <fstream>
#include <iterator>
#include <vector>
#include "crn_decomp.h"

int main(int argc, char** argv) {
    if (argc != 3) { std::fprintf(stderr, "usage: unpack INPUT OUTPUT\n"); return 2; }
    std::ifstream input(argv[1], std::ios::binary);
    if (!input) { std::fprintf(stderr, "input open failed\n"); return 1; }
    std::vector<unsigned char> data((std::istreambuf_iterator<char>(input)), {});
    if (data.size() < 32 || data.size() > 4 * 1024 * 1024) { std::fprintf(stderr, "invalid size\n"); return 1; }
    unitycrnd::crn_texture_info info;
    if (!unitycrnd::crnd_get_texture_info(data.data(), static_cast<unitycrnd::uint32>(data.size()), &info)) { std::fprintf(stderr, "invalid header\n"); return 1; }
    if (info.m_width != 512 || info.m_height != 512 || info.m_faces != 6 || info.m_levels != 1 || unitycrnd::crnd_get_bytes_per_dxt_block(info.m_format) != 16 || unitycrnd::crnd_get_fundamental_dxt_format(info.m_format) != cCRNFmtDXT5) {
        std::fprintf(stderr, "unsupported header: %ux%u faces=%u levels=%u format=%d\n", info.m_width, info.m_height, info.m_faces, info.m_levels, int(info.m_format)); return 1;
    }
    constexpr unsigned face_size = 512 * 512;
    std::vector<unsigned char> faces(6 * face_size);
    void* ptrs[6]; for (unsigned i=0; i<6; ++i) ptrs[i] = faces.data() + i*face_size;
    auto ctx = unitycrnd::crnd_unpack_begin(data.data(), static_cast<unitycrnd::uint32>(data.size()));
    if (!ctx) { std::fprintf(stderr, "context failed\n"); return 1; }
    bool ok = unitycrnd::crnd_unpack_level(ctx, ptrs, face_size, (512 / 4) * 16, 0);
    unitycrnd::crnd_unpack_end(ctx);
    if (!ok) { std::fprintf(stderr, "unpack failed\n"); return 1; }
    std::ofstream output(argv[2], std::ios::binary | std::ios::out | std::ios::trunc);
    if (!output.write(reinterpret_cast<const char*>(faces.data()), faces.size())) { std::fprintf(stderr, "output write failed\n"); return 1; }
    std::fprintf(stderr, "decoded %u faces, %u bytes each, format=%d\n", info.m_faces, face_size, int(info.m_format));
    return 0;
}
