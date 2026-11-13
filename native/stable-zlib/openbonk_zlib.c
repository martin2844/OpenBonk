#include <jni.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include "zlib.h"

typedef struct { z_stream z; unsigned char *input; int finished; } State;

static void fail(JNIEnv *env, const char *message) {
    jclass type = (*env)->FindClass(env, "java/lang/IllegalStateException");
    if (type) (*env)->ThrowNew(env, type, message);
}
static State *get(JNIEnv *env, jlong handle) {
    State *s = (State *)(intptr_t)handle;
    if (!s) fail(env, "closed stable deflater");
    return s;
}
JNIEXPORT jlong JNICALL Java_com_openbonk_dataprep_SteamStableDeflater_init(JNIEnv *env, jclass cls, jint level) {
    (void)cls;
    State *s = calloc(1, sizeof *s);
    if (!s) { fail(env, "cannot allocate deflater"); return 0; }
    if (deflateInit2(&s->z, level, Z_DEFLATED, -15, 8, Z_DEFAULT_STRATEGY) != Z_OK) {
        free(s); fail(env, "cannot initialize deflater"); return 0;
    }
    return (jlong)(intptr_t)s;
}
JNIEXPORT void JNICALL Java_com_openbonk_dataprep_SteamStableDeflater_input(JNIEnv *env, jclass cls, jlong handle, jbyteArray bytes, jint offset, jint length) {
    (void)cls;
    State *s = get(env, handle); if (!s) return;
    if (s->z.avail_in || s->finished || length < 0 || offset < 0 || offset > (*env)->GetArrayLength(env, bytes) - length) { fail(env, "invalid deflater input"); return; }
    free(s->input); s->input = NULL;
    if (!length) return;
    s->input = malloc((size_t)length);
    if (!s->input) { fail(env, "cannot allocate input"); return; }
    (*env)->GetByteArrayRegion(env, bytes, offset, length, (jbyte *)s->input);
    if ((*env)->ExceptionCheck(env)) { free(s->input); s->input = NULL; return; }
    s->z.next_in = s->input; s->z.avail_in = (uInt)length;
}
JNIEXPORT jint JNICALL Java_com_openbonk_dataprep_SteamStableDeflater_compress(JNIEnv *env, jclass cls, jlong handle, jbyteArray bytes, jint offset, jint length, jboolean finish) {
    (void)cls;
    State *s = get(env, handle); if (!s) return 0;
    if (length < 0 || offset < 0 || offset > (*env)->GetArrayLength(env, bytes) - length) { fail(env, "invalid deflater output"); return 0; }
    if (!length || s->finished) return 0;
    unsigned char *out = malloc((size_t)length);
    if (!out) { fail(env, "cannot allocate output"); return 0; }
    s->z.next_out = out; s->z.avail_out = (uInt)length;
    int rc = deflate(&s->z, finish ? Z_FINISH : Z_NO_FLUSH);
    jint written = length - (jint)s->z.avail_out;
    if (rc == Z_STREAM_END) s->finished = 1;
    if (rc != Z_OK && rc != Z_STREAM_END && rc != Z_BUF_ERROR) { free(out); fail(env, "deflate failed"); return 0; }
    if (written) (*env)->SetByteArrayRegion(env, bytes, offset, written, (jbyte *)out);
    free(out);
    if (!s->z.avail_in) { free(s->input); s->input = NULL; s->z.next_in = Z_NULL; }
    return written;
}
JNIEXPORT jlong JNICALL Java_com_openbonk_dataprep_SteamStableDeflater_metric(JNIEnv *env, jclass cls, jlong handle, jint which) {
    (void)cls;
    State *s = get(env, handle); if (!s) return 0;
    switch (which) {
      case 0: return s->z.avail_in == 0;
      case 1: return s->finished;
      case 2: return (jlong)s->z.total_in;
      case 3: return (jlong)s->z.total_out;
      default: fail(env, "invalid deflater metric"); return 0;
    }
}
JNIEXPORT void JNICALL Java_com_openbonk_dataprep_SteamStableDeflater_resetNative(JNIEnv *env, jclass cls, jlong handle) {
    (void)cls;
    State *s = get(env, handle); if (!s) return;
    free(s->input); s->input = NULL; s->finished = 0;
    if (deflateReset(&s->z) != Z_OK) fail(env, "deflater reset failed");
}
JNIEXPORT void JNICALL Java_com_openbonk_dataprep_SteamStableDeflater_closeNative(JNIEnv *env, jclass cls, jlong handle) {
    (void)cls;
    State *s = get(env, handle); if (!s) return;
    deflateEnd(&s->z); free(s->input); free(s);
}
