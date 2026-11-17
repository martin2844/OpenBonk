#define _FILE_OFFSET_BITS 64
#include <jni.h>
#include <fcntl.h>
#include <stdint.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <unistd.h>
#include <zstd.h>

// Java validates the source and target hashes. The output is a temporary file;
// neither a failed decode nor a process death may modify the original OBB.
JNIEXPORT jint JNICALL
Java_com_megabonk_launcher_LauncherObbMigration_applyNative(
    JNIEnv *env, jclass cls, jstring old_path, jstring patch_path,
    jstring output_path, jlong expected_size) {
    (void)cls;
    const char *old = NULL, *patch = NULL, *output = NULL;
    int old_fd = -1, patch_fd = -1, output_fd = -1, result = 1;
    void *old_map = MAP_FAILED, *patch_map = MAP_FAILED, *output_map = MAP_FAILED;
    struct stat old_stat = {0}, patch_stat = {0}; ZSTD_DCtx *ctx = NULL;
    if (expected_size <= 0 || expected_size > 1024LL * 1024 * 1024) goto done;
    old = (*env)->GetStringUTFChars(env, old_path, NULL);
    patch = (*env)->GetStringUTFChars(env, patch_path, NULL);
    output = (*env)->GetStringUTFChars(env, output_path, NULL);
    if (!old || !patch || !output) goto done;
    old_fd = open(old, O_RDONLY | O_CLOEXEC);
    patch_fd = open(patch, O_RDONLY | O_CLOEXEC);
    output_fd = open(output, O_RDWR | O_TRUNC | O_CLOEXEC);
    if (old_fd < 0 || patch_fd < 0 || output_fd < 0 || fstat(old_fd, &old_stat) ||
        fstat(patch_fd, &patch_stat) || !S_ISREG(old_stat.st_mode) || !S_ISREG(patch_stat.st_mode) ||
        old_stat.st_size <= 0 || old_stat.st_size > 1024LL * 1024 * 1024 ||
        patch_stat.st_size <= 0 || patch_stat.st_size > 64LL * 1024 * 1024 ||
        ftruncate(output_fd, (off_t)expected_size) ||
        posix_fallocate(output_fd, 0, (off_t)expected_size)) goto done;
    old_map = mmap(NULL, (size_t)old_stat.st_size, PROT_READ, MAP_PRIVATE, old_fd, 0);
    patch_map = mmap(NULL, (size_t)patch_stat.st_size, PROT_READ, MAP_PRIVATE, patch_fd, 0);
    output_map = mmap(NULL, (size_t)expected_size, PROT_READ | PROT_WRITE, MAP_SHARED, output_fd, 0);
    if (old_map == MAP_FAILED || patch_map == MAP_FAILED || output_map == MAP_FAILED) goto done;
    ctx = ZSTD_createDCtx(); if (!ctx) goto done;
    size_t size = ZSTD_DCtx_refPrefix(ctx, old_map, (size_t)old_stat.st_size);
    if (ZSTD_isError(size)) goto done;
    size = ZSTD_decompressDCtx(ctx, output_map, (size_t)expected_size, patch_map, (size_t)patch_stat.st_size);
    if (ZSTD_isError(size) || size != (size_t)expected_size || msync(output_map, size, MS_SYNC) || fsync(output_fd)) goto done;
    result = 0;
done:
    if (ctx) ZSTD_freeDCtx(ctx);
    if (old_map != MAP_FAILED) munmap(old_map, (size_t)old_stat.st_size);
    if (patch_map != MAP_FAILED) munmap(patch_map, (size_t)patch_stat.st_size);
    if (output_map != MAP_FAILED) munmap(output_map, (size_t)expected_size);
    if (old_fd >= 0) close(old_fd);
    if (patch_fd >= 0) close(patch_fd);
    if (output_fd >= 0) close(output_fd);
    if (old) (*env)->ReleaseStringUTFChars(env, old_path, old);
    if (patch) (*env)->ReleaseStringUTFChars(env, patch_path, patch);
    if (output) (*env)->ReleaseStringUTFChars(env, output_path, output);
    return result;
}
