/* Read-only Android diagnostic for whether MediaExtractor recognizes a file. */
#include <fcntl.h>
#include <inttypes.h>
#include <media/NdkMediaExtractor.h>
#include <media/NdkMediaFormat.h>
#include <stdio.h>
#include <sys/stat.h>
#include <unistd.h>

int main(int argc, char **argv) {
    if (argc != 2) { fprintf(stderr, "usage: probe-video-extractor FILE\n"); return 2; }
    int fd = open(argv[1], O_RDONLY | O_CLOEXEC);
    struct stat st;
    if (fd < 0 || fstat(fd, &st) || st.st_size <= 0) { perror("open video"); if (fd >= 0) close(fd); return 1; }
    AMediaExtractor *extractor = AMediaExtractor_new();
    if (!extractor) { close(fd); return 1; }
    media_status_t status = AMediaExtractor_setDataSourceFd(extractor, fd, 0, st.st_size);
    size_t count = status == AMEDIA_OK ? AMediaExtractor_getTrackCount(extractor) : 0;
    printf("extractor_status=%d bytes=%jd tracks=%zu\n", status, (intmax_t)st.st_size, count);
    for (size_t i = 0; i < count; i++) {
        AMediaFormat *format = AMediaExtractor_getTrackFormat(extractor, i);
        if (!format) continue;
        printf("track_%zu=%s\n", i, AMediaFormat_toString(format));
        AMediaFormat_delete(format);
    }
    AMediaExtractor_delete(extractor); close(fd);
    return status == AMEDIA_OK && count > 0 ? 0 : 1;
}
