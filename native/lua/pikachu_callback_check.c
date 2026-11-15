/* Exercise the actual Pikachu attack script through the Android Lua bridge. */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

void *megabonk_lua_session_create(const char *, char *, unsigned long);
int megabonk_lua_session_weapon_attack(void *, double, const char *, int, int,
                                       double, double, double *, char *, unsigned long);
void megabonk_lua_session_destroy(void *);

int main(int argc, char **argv)
{
    if (argc != 2) { fprintf(stderr, "usage: pikachu_callback_check main.lua\n"); return 2; }
    FILE *file = fopen(argv[1], "rb");
    if (!file) { perror("script"); return 2; }
    char source[65537];
    size_t size = fread(source, 1, sizeof(source), file);
    int failed = ferror(file) || size == 0 || size >= sizeof(source) || memchr(source, '\0', size) != NULL;
    fclose(file);
    if (failed) { fprintf(stderr, "invalid script\n"); return 2; }
    source[size] = '\0';
    char error[256] = {0};
    void *session = megabonk_lua_session_create(source, error, sizeof(error));
    if (!session) { fprintf(stderr, "session failed: %s\n", error); return 1; }
    for (int index = 0; index < 6; index++) {
        double value = -1;
        int status = megabonk_lua_session_weapon_attack(session, 1 + index / 2, "Goblin", 0, 0, 100, 5,
                                                        &value, error, sizeof(error));
        double expected = index % 2 ? 1.5 : 1.0;
        if (status != 0 || value != expected) {
            fprintf(stderr, "attack %d failed: status=%d value=%g error=%s\n", index + 1, status, value, error);
            megabonk_lua_session_destroy(session);
            return 1;
        }
    }
    megabonk_lua_session_destroy(session);
    puts("PASS: Pikachu callback alternates 1.0 and 1.5 over six attacks");
    return 0;
}
