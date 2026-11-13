#include <stddef.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include "lua.h"
#include "lauxlib.h"
#include "lualib.h"

/* Experimental, isolated expression runner. No game state is exposed yet. */
#define MEMORY_LIMIT (8u * 1024u * 1024u)
#define INSTRUCTION_LIMIT 100000
struct budget { size_t bytes; };

static void *limited_alloc(void *context, void *ptr, size_t old_size, size_t size) {
    struct budget *b = context;
    if (!ptr) old_size = 0;
    if (size == 0) { free(ptr); b->bytes -= old_size; return NULL; }
    if (size > MEMORY_LIMIT || b->bytes - old_size > MEMORY_LIMIT - size) return NULL;
    void *result = realloc(ptr, size);
    if (result) b->bytes = b->bytes - old_size + size;
    return result;
}

static void instruction_limit(lua_State *state, lua_Debug *debug) {
    (void)debug;
    luaL_error(state, "instruction limit exceeded");
}

/* Keep the base helpers useful to mods without exposing loaders, IO or the
 * protected-call functions that could repeatedly catch the instruction hook. */
static void open_mod_libraries(lua_State *state) {
    static const char *const removed[] = {
        "collectgarbage", "dofile", "load", "loadfile", "pcall", "print",
        "warn", "xpcall", "getmetatable", "setmetatable", "rawequal",
        "rawget", "rawlen", "rawset", "_G", NULL
    };
    luaL_requiref(state, LUA_GNAME, luaopen_base, 1); lua_pop(state, 1);
    for (const char *const *name = removed; *name; ++name) {
        lua_pushnil(state); lua_setglobal(state, *name);
    }
    luaL_requiref(state, LUA_MATHLIBNAME, luaopen_math, 1); lua_pop(state, 1);
    luaL_requiref(state, LUA_TABLIBNAME, luaopen_table, 1); lua_pop(state, 1);
    luaL_requiref(state, LUA_STRLIBNAME, luaopen_string, 1); lua_pop(state, 1);
    luaL_requiref(state, LUA_UTF8LIBNAME, luaopen_utf8, 1); lua_pop(state, 1);
}

/* Returns 0 for a number; nonzero is an error. Caller owns the error buffer. */
__attribute__((visibility("default")))
int megabonk_lua_eval(const char *source, double *value, char *error, size_t error_size) {
    if (!source || !value || !error || !error_size) return 1;
    error[0] = '\0';
    struct budget b = { 0 };
    lua_State *state = lua_newstate(limited_alloc, &b);
    if (!state) { strncpy(error, "cannot create Lua state", error_size - 1); error[error_size - 1] = 0; return 1; }
    open_mod_libraries(state);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int status = luaL_loadbufferx(state, source, strlen(source), "mod", "t");
    if (!status) status = lua_pcall(state, 0, 1, 0);
    if (!status && lua_type(state, -1) != LUA_TNUMBER) { lua_pushliteral(state, "script must return a number"); status = 1; }
    if (status) {
        const char *message = lua_tostring(state, -1);
        if (!message) message = "Lua error";
        strncpy(error, message, error_size - 1); error[error_size - 1] = 0;
    } else *value = lua_tonumber(state, -1);
    lua_close(state);
    return status ? 1 : 0;
}

/* API 2: at most one flat and one multiplicative value per supported stat. */
__attribute__((visibility("default")))
int megabonk_lua_eval_values(const char *source, int count, double *values, char *error, size_t error_size) {
    if (!source || !values || !error || !error_size || count < 1 || count > 112) return 1;
    error[0] = '\0';
    struct budget b = { 0 };
    lua_State *state = lua_newstate(limited_alloc, &b);
    if (!state) { strncpy(error, "cannot create Lua state", error_size - 1); error[error_size - 1] = 0; return 1; }
    open_mod_libraries(state);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int status = luaL_loadbufferx(state, source, strlen(source), "mod", "t");
    if (!status) status = lua_pcall(state, 0, 1, 0);
    if (!status && (lua_type(state, -1) != LUA_TTABLE || lua_rawlen(state, -1) != (size_t)count)) {
        lua_pushliteral(state, "script must return a list matching the hook count"); status = 1;
    }
    double result[112];
    if (!status) for (int i = 0; i < count; ++i) {
        lua_rawgeti(state, -1, i + 1);
        if (lua_type(state, -1) != LUA_TNUMBER) {
            lua_pop(state, 1); lua_pushliteral(state, "all hook values must be numbers"); status = 1; break;
        }
        result[i] = lua_tonumber(state, -1); lua_pop(state, 1);
    }
    if (status) {
        const char *message = lua_tostring(state, -1);
        if (!message) message = "Lua error";
        strncpy(error, message, error_size - 1); error[error_size - 1] = 0;
    } else memcpy(values, result, (size_t)count * sizeof(double));
    lua_close(state);
    return status ? 1 : 0;
}

/* Persistent callback state, groundwork for event-driven game hooks. The
 * callback table is retained below each protected invocation on the stack. */
struct megabonk_lua_session { struct budget budget; lua_State *state; };

static void copy_error(lua_State *state, char *error, size_t size) {
    const char *message = lua_tostring(state, -1);
    if (!message) message = "Lua error";
    strncpy(error, message, size - 1); error[size - 1] = 0;
}

__attribute__((visibility("default")))
void *megabonk_lua_session_create(const char *source, char *error, size_t error_size) {
    if (!source || !error || !error_size) return NULL;
    error[0] = 0;
    struct megabonk_lua_session *session = calloc(1, sizeof *session);
    if (!session) { strncpy(error, "cannot allocate session", error_size - 1); error[error_size - 1] = 0; return NULL; }
    lua_State *state = session->state = lua_newstate(limited_alloc, &session->budget);
    if (!state) { free(session); strncpy(error, "cannot create Lua state", error_size - 1); error[error_size - 1] = 0; return NULL; }
    open_mod_libraries(state);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int status = luaL_loadbufferx(state, source, strlen(source), "mod", "t");
    if (!status) status = lua_pcall(state, 0, 1, 0);
    lua_sethook(state, NULL, 0, 0);
    if (!status && lua_type(state, -1) != LUA_TTABLE) {
        strncpy(error, "script must return a callback table", error_size - 1); error[error_size - 1] = 0;
        lua_close(state); free(session); return NULL;
    }
    if (status) { copy_error(state, error, error_size); lua_close(state); free(session); return NULL; }
    return session;
}

/* The dispatcher runs under lua_pcall, including table lookup and script
 * execution. The event name is pushed inside that protected call so even an
 * allocation failure during lookup cannot escape into the host process. */
static int dispatch_event(lua_State *state) {
    const char *event = lua_touserdata(state, 2);
    lua_pushstring(state, event); lua_rawget(state, 1);
    if (lua_isnil(state, -1)) { lua_pushnil(state); return 1; }
    if (!lua_isfunction(state, -1)) return luaL_error(state, "event handler must be a function");
    lua_pushvalue(state, 3); lua_call(state, 1, 1); return 1;
}

struct map_enemy_context { const char *event; const char *enemy; int stage; };

struct weapon_target_context { const char *enemy; int boss, elite; double hp, distance; };

/* The extra table is copied data; old attack(level) callbacks ignore it. */
static int dispatch_weapon_target(lua_State *state) {
    const struct weapon_target_context *ctx = lua_touserdata(state, 3);
    lua_pushliteral(state, "attack"); lua_rawget(state, 1);
    if (lua_isnil(state, -1)) { lua_pushnil(state); return 1; }
    if (!lua_isfunction(state, -1)) return luaL_error(state, "event handler must be a function");
    lua_pushvalue(state, 2);
    lua_createtable(state, 0, 5);
    lua_pushstring(state, ctx->enemy); lua_setfield(state, -2, "enemy");
    lua_pushboolean(state, ctx->boss); lua_setfield(state, -2, "boss");
    lua_pushboolean(state, ctx->elite); lua_setfield(state, -2, "elite");
    lua_pushnumber(state, ctx->hp); lua_setfield(state, -2, "target_health");
    lua_pushnumber(state, ctx->distance); lua_setfield(state, -2, "distance");
    lua_call(state, 2, 1); return 1;
}

struct tick_context { int available; double hp, max_hp, shield, max_shield, time; int extended, level_available, level, manager_available; double manager_time; int xp_actions; };

/* Numeric tick results remain supported; tables add bounded player actions. */
static int dispatch_tick_actions(lua_State *state) {
    const struct tick_context *ctx = lua_touserdata(state, 3);
    lua_pushliteral(state, "tick"); lua_rawget(state, 1);
    if (lua_isnil(state, -1)) { lua_pushnil(state); return 1; }
    if (!lua_isfunction(state, -1)) return luaL_error(state, "tick handler must be a function");
    lua_pushvalue(state, 2);
    if (ctx) {
        lua_createtable(state, 0, 6);
        lua_pushboolean(state, ctx->available); lua_setfield(state, -2, "health_available");
        lua_pushnumber(state, ctx->hp); lua_setfield(state, -2, "hp");
        lua_pushnumber(state, ctx->max_hp); lua_setfield(state, -2, "max_hp");
        lua_pushnumber(state, ctx->shield); lua_setfield(state, -2, "shield");
        lua_pushnumber(state, ctx->max_shield); lua_setfield(state, -2, "max_shield");
        lua_pushnumber(state, ctx->time); lua_setfield(state, -2, "time");
        if (ctx->extended) {
            lua_pushboolean(state, ctx->level_available); lua_setfield(state, -2, "level_available");
            lua_pushinteger(state, ctx->level); lua_setfield(state, -2, "level");
            lua_pushboolean(state, ctx->manager_available); lua_setfield(state, -2, "manager_time_available");
            lua_pushnumber(state, ctx->manager_time); lua_setfield(state, -2, "manager_time");
        }
    } else lua_pushnil(state);
    lua_call(state, 2, 1);
    if (lua_isnil(state, -1)) return 1;
    double values[3] = {1, 0, 0};
    if (lua_type(state, -1) == LUA_TNUMBER) values[0] = lua_tonumber(state, -1);
    else if (lua_type(state, -1) == LUA_TTABLE) {
        const char *fields[] = {"scale", "heal", "xp"};
        for (int i = 0; i < (ctx && ctx->xp_actions ? 3 : 2); i++) {
            lua_pushstring(state, fields[i]); lua_rawget(state, -2);
            if (!lua_isnil(state, -1)) {
                if (lua_type(state, -1) != LUA_TNUMBER) return luaL_error(state, "tick action must be a number");
                values[i] = lua_tonumber(state, -1);
            }
            lua_pop(state, 1);
        }
    } else return luaL_error(state, "tick must return a number or action table");
    if (!isfinite(values[0]) || values[0] < 0 || values[0] > 10 || !isfinite(values[1]) || values[1] < 0 || values[1] > 100 ||
        !isfinite(values[2]) || values[2] < 0 || values[2] > 1000 || floor(values[2]) != values[2])
        return luaL_error(state, "tick action out of range");
    lua_pushnumber(state, values[0]); lua_pushnumber(state, values[1]);
    if (ctx && ctx->xp_actions) { lua_pushnumber(state, values[2]); return 3; }
    return 2;
}

static int tick_actions(void *handle, double stacks, const struct tick_context *ctx, double *out, char *error, size_t error_size) {
    struct megabonk_lua_session *session = handle;
    if (!session || !out || !error || !error_size || !isfinite(stacks) || stacks < 0) return 1;
    error[0] = 0;
    lua_State *state = session->state;
    lua_settop(state, 1);
    lua_pushcfunction(state, dispatch_tick_actions); lua_pushvalue(state, 1); lua_pushnumber(state, stacks); lua_pushlightuserdata(state, (void *)ctx);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int count = ctx && ctx->xp_actions ? 3 : 2;
    int status = lua_pcall(state, 3, count, 0);
    lua_sethook(state, NULL, 0, 0);
    if (status) { copy_error(state, error, error_size); lua_settop(state, 1); return 1; }
    if (lua_isnil(state, -count)) { lua_settop(state, 1); return 2; }
    double values[3] = {lua_tonumber(state, -count), lua_tonumber(state, -count + 1), 0};
    if (count == 3) values[2] = lua_tonumber(state, -1);
    lua_settop(state, 1); out[0] = values[0]; out[1] = values[1]; if (count == 3) out[2] = values[2]; return 0;
}

__attribute__((visibility("default")))
int megabonk_lua_session_tick_actions(void *handle, double stacks, double *out, char *error, size_t error_size) {
    return tick_actions(handle, stacks, NULL, out, error, error_size);
}

__attribute__((visibility("default")))
int megabonk_lua_session_tick_context(void *handle, double stacks, int available, double hp, double max_hp,
                                     double shield, double max_shield, double time, double *out, char *error, size_t error_size) {
    if ((available != 0 && available != 1) || !isfinite(hp) || !isfinite(max_hp) || !isfinite(shield) || !isfinite(max_shield) || !isfinite(time)) {
        if (error && error_size) { strncpy(error, "invalid tick context", error_size - 1); error[error_size - 1] = 0; }
        return 1;
    }
    struct tick_context ctx = {available, hp, max_hp, shield, max_shield, time, 0, 0, 0, 0, 0, 0};
    return tick_actions(handle, stacks, &ctx, out, error, error_size);
}

__attribute__((visibility("default")))
int megabonk_lua_session_tick_context_v2(void *handle, double stacks, int available, double hp, double max_hp,
                                        double shield, double max_shield, double time, int level_available, int level,
                                        int manager_available, double manager_time, double *out, char *error, size_t error_size) {
    if ((available != 0 && available != 1) || (level_available != 0 && level_available != 1) ||
        (manager_available != 0 && manager_available != 1) || level < 0 || (!level_available && level != 0) ||
        !isfinite(hp) || !isfinite(max_hp) || !isfinite(shield) || !isfinite(max_shield) || !isfinite(time) ||
        !isfinite(manager_time) || manager_time < 0 || (!manager_available && manager_time != 0)) {
        if (error && error_size) { strncpy(error, "invalid tick context", error_size - 1); error[error_size - 1] = 0; }
        return 1;
    }
    struct tick_context ctx = {available, hp, max_hp, shield, max_shield, time, 1, level_available, level, manager_available, manager_time, 0};
    return tick_actions(handle, stacks, &ctx, out, error, error_size);
}

__attribute__((visibility("default")))
int megabonk_lua_session_tick_context_v3(void *handle, double stacks, int available, double hp, double max_hp,
                                        double shield, double max_shield, double time, int level_available, int level,
                                        int manager_available, double manager_time, double *out, char *error, size_t error_size) {
    if ((available != 0 && available != 1) || (level_available != 0 && level_available != 1) ||
        (manager_available != 0 && manager_available != 1) || level < 0 || (!level_available && level != 0) ||
        !isfinite(hp) || !isfinite(max_hp) || !isfinite(shield) || !isfinite(max_shield) || !isfinite(time) ||
        !isfinite(manager_time) || manager_time < 0 || (!manager_available && manager_time != 0)) {
        if (error && error_size) { strncpy(error, "invalid tick context", error_size - 1); error[error_size - 1] = 0; }
        return 1;
    }
    struct tick_context ctx = {available, hp, max_hp, shield, max_shield, time, 1, level_available, level, manager_available, manager_time, 1};
    return tick_actions(handle, stacks, &ctx, out, error, error_size);
}

/* Both the lookup and context allocation remain within the protected call. */
static int dispatch_map_enemy(lua_State *state) {
    const struct map_enemy_context *ctx = lua_touserdata(state, 2);
    lua_pushstring(state, ctx->event); lua_rawget(state, 1);
    if (lua_isnil(state, -1)) { lua_pushnil(state); return 1; }
    if (!lua_isfunction(state, -1)) return luaL_error(state, "event handler must be a function");
    lua_newtable(state);
    lua_pushinteger(state, ctx->stage); lua_setfield(state, -2, "stage");
    lua_pushstring(state, ctx->enemy); lua_setfield(state, -2, "enemy");
    lua_call(state, 1, 1); return 1;
}

struct attack_context {
    int has_target, boss, elite, element, critical;
    double target_health, proc_coefficient;
    const char *event;
    double damage;
    int post_hit;
};

/* The context table is created inside the protected call: allocation failures
 * cannot longjmp past the host. Lua receives no engine pointers or handles. */
static int dispatch_attack(lua_State *state) {
    const struct attack_context *context = lua_touserdata(state, 3);
    lua_pushstring(state, context->event); lua_rawget(state, 1);
    if (lua_isnil(state, -1)) { lua_pushnil(state); return 1; }
    if (!lua_isfunction(state, -1)) return luaL_error(state, "event handler must be a function");
    lua_pushvalue(state, 2);
    lua_createtable(state, 0, 7);
    lua_pushboolean(state, context->has_target); lua_setfield(state, -2, "has_target");
    lua_pushboolean(state, context->boss); lua_setfield(state, -2, "boss");
    lua_pushboolean(state, context->elite); lua_setfield(state, -2, "elite");
    lua_pushnumber(state, context->target_health); lua_setfield(state, -2, "target_health");
    lua_pushinteger(state, context->element); lua_setfield(state, -2, "element");
    lua_pushboolean(state, context->critical); lua_setfield(state, -2, "critical");
    lua_pushnumber(state, context->proc_coefficient); lua_setfield(state, -2, "proc_coefficient");
    if (context->post_hit) { lua_pushnumber(state, context->damage); lua_setfield(state, -2, "damage"); }
    lua_call(state, 2, 1); return 1;
}

/* Same return contract as session_event; adds a read-only-by-copy context. */
static int session_attack_event(void *handle, double stacks, const struct attack_context *context,
        double *out, char *error, size_t error_size) {
    struct megabonk_lua_session *session = handle;
    if (!session || !out || !error || !error_size) return 1;
    error[0] = 0;
    lua_State *state = session->state;
    lua_settop(state, 1);
    lua_pushcfunction(state, dispatch_attack); lua_pushvalue(state, 1); lua_pushnumber(state, stacks); lua_pushlightuserdata(state, (void *)context);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int status = lua_pcall(state, 3, 1, 0);
    lua_sethook(state, NULL, 0, 0);
    if (status) { copy_error(state, error, error_size); lua_settop(state, 1); return 1; }
    if (lua_isnil(state, -1)) { lua_settop(state, 1); return 2; }
    if (lua_type(state, -1) != LUA_TNUMBER) {
        strncpy(error, "event handler must return a number or nil", error_size - 1); error[error_size - 1] = 0;
        lua_settop(state, 1); return 1;
    }
    *out = lua_tonumber(state, -1); lua_settop(state, 1); return 0;
}

__attribute__((visibility("default")))
int megabonk_lua_session_pre_attack(void *handle, double stacks, int has_target, int boss, int elite,
        double target_health, int element, int critical, double proc_coefficient,
        double *out, char *error, size_t error_size) {
    const struct attack_context context = { has_target, boss, elite, element, critical, target_health, proc_coefficient, "pre_attack", 0, 0 };
    return session_attack_event(handle, stacks, &context, out, error, error_size);
}

__attribute__((visibility("default")))
int megabonk_lua_session_on_hit(void *handle, double stacks, int has_target, int boss, int elite,
        double target_health, int element, int critical, double proc_coefficient, double damage,
        double *out, char *error, size_t error_size) {
    const struct attack_context context = { has_target, boss, elite, element, critical, target_health, proc_coefficient, "on_hit", damage, 1 };
    return session_attack_event(handle, stacks, &context, out, error, error_size);
}

/* On-hit may return the legacy damage multiplier or a bounded action table.
 * Inspect the result inside lua_pcall, including all potentially allocating
 * field lookups. Nothing is copied to the host unless every field is valid. */
static int dispatch_on_hit_actions(lua_State *state) {
    dispatch_attack(state);
    if (lua_isnil(state, -1)) return 1;
    double values[5] = {0, 0, 0, 0, 0};
    if (lua_type(state, -1) == LUA_TNUMBER) values[0] = lua_tonumber(state, -1);
    else if (lua_type(state, -1) == LUA_TTABLE) {
        const char *fields[] = {"damage", "freeze", "stun", "burn", "poison"};
        for (int i = 0; i < 5; ++i) {
            lua_pushstring(state, fields[i]); lua_rawget(state, -2);
            if (!lua_isnil(state, -1)) {
                if (lua_type(state, -1) != LUA_TNUMBER) return luaL_error(state, "on_hit action must be a number");
                values[i] = lua_tonumber(state, -1);
            }
            lua_pop(state, 1);
        }
    } else return luaL_error(state, "on_hit must return a number or action table");
    for (int i = 0; i < 5; ++i)
        if (!isfinite(values[i]) || values[i] < 0 || values[i] > (i == 0 ? 10 : 30))
            return luaL_error(state, "on_hit action out of range");
    for (int i = 0; i < 5; ++i) lua_pushnumber(state, values[i]);
    return 5;
}

__attribute__((visibility("default")))
int megabonk_lua_session_on_hit_actions(void *handle, double stacks, int has_target, int boss, int elite,
        double target_health, int element, int critical, double proc_coefficient, double damage,
        double *out, char *error, size_t error_size) {
    struct megabonk_lua_session *session = handle;
    if (!session || !out || !error || !error_size) return 1;
    const struct attack_context context = { has_target, boss, elite, element, critical, target_health, proc_coefficient, "on_hit", damage, 1 };
    error[0] = 0;
    lua_State *state = session->state;
    lua_settop(state, 1);
    lua_pushcfunction(state, dispatch_on_hit_actions); lua_pushvalue(state, 1); lua_pushnumber(state, stacks); lua_pushlightuserdata(state, (void *)&context);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int status = lua_pcall(state, 3, 5, 0);
    lua_sethook(state, NULL, 0, 0);
    if (status) { copy_error(state, error, error_size); lua_settop(state, 1); return 1; }
    if (lua_isnil(state, -5)) { lua_settop(state, 1); return 2; }
    double values[5]; for (int i = 0; i < 5; ++i) values[i] = lua_tonumber(state, -5 + i);
    memcpy(out, values, sizeof values); lua_settop(state, 1); return 0;
}

__attribute__((visibility("default")))
int megabonk_lua_session_enemy_killed(void *handle, double stacks, int has_target, int boss, int elite,
        double target_health, int element, int critical, double proc_coefficient, double damage,
        double *out, char *error, size_t error_size) {
    const struct attack_context context = { has_target, boss, elite, element, critical, target_health, proc_coefficient, "enemy_killed", damage, 1 };
    return session_attack_event(handle, stacks, &context, out, error, error_size);
}

/* Preserve numeric callbacks while allowing bounded, atomic actions on death. */
static int dispatch_enemy_killed_actions_impl(lua_State *state, int with_gold) {
    dispatch_attack(state);
    if (lua_isnil(state, -1)) return 1;
    double values[4] = {1, 0, 0, 0};
    if (lua_type(state, -1) == LUA_TNUMBER) values[0] = lua_tonumber(state, -1);
    else if (lua_type(state, -1) == LUA_TTABLE) {
        const char *fields[] = {"scale", "heal", "xp", "gold"};
        for (int i = 0; i < (with_gold ? 4 : 3); ++i) {
            lua_pushstring(state, fields[i]); lua_rawget(state, -2);
            if (!lua_isnil(state, -1)) {
                if (lua_type(state, -1) != LUA_TNUMBER) return luaL_error(state, "enemy_killed action must be a number");
                values[i] = lua_tonumber(state, -1);
            }
            lua_pop(state, 1);
        }
    } else return luaL_error(state, "enemy_killed must return a number or action table");
    for (int i = 0; i < (with_gold ? 4 : 3); ++i)
        if (!isfinite(values[i]) || values[i] < 0 || values[i] > (i == 0 ? 10 : 100) || (i >= 2 && floor(values[i]) != values[i]))
            return luaL_error(state, "enemy_killed action out of range");
    for (int i = 0; i < (with_gold ? 4 : 3); ++i) lua_pushnumber(state, values[i]);
    return with_gold ? 4 : 3;
}
static int dispatch_enemy_killed_actions(lua_State *state) { return dispatch_enemy_killed_actions_impl(state, 0); }
static int dispatch_enemy_killed_actions_v2(lua_State *state) { return dispatch_enemy_killed_actions_impl(state, 1); }

static int enemy_killed_actions(void *handle, double stacks, int has_target, int boss, int elite,
        double target_health, int element, int critical, double proc_coefficient, double damage,
        double *out, char *error, size_t error_size, int with_gold) {
    struct megabonk_lua_session *session = handle;
    if (!session || !out || !error || !error_size) return 1;
    const struct attack_context context = { has_target, boss, elite, element, critical, target_health, proc_coefficient, "enemy_killed", damage, 1 };
    error[0] = 0;
    lua_State *state = session->state;
    lua_settop(state, 1);
    lua_pushcfunction(state, with_gold ? dispatch_enemy_killed_actions_v2 : dispatch_enemy_killed_actions); lua_pushvalue(state, 1); lua_pushnumber(state, stacks); lua_pushlightuserdata(state, (void *)&context);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int count = with_gold ? 4 : 3;
    int status = lua_pcall(state, 3, count, 0);
    lua_sethook(state, NULL, 0, 0);
    if (status) { copy_error(state, error, error_size); lua_settop(state, 1); return 1; }
    if (lua_isnil(state, -count)) { lua_settop(state, 1); return 2; }
    double values[4]; for (int i = 0; i < count; ++i) values[i] = lua_tonumber(state, -count + i);
    memcpy(out, values, (size_t)count * sizeof *values); lua_settop(state, 1); return 0;
}

__attribute__((visibility("default")))
int megabonk_lua_session_enemy_killed_actions(void *handle, double stacks, int has_target, int boss, int elite,
        double target_health, int element, int critical, double proc_coefficient, double damage,
        double *out, char *error, size_t error_size) {
    return enemy_killed_actions(handle, stacks, has_target, boss, elite, target_health, element, critical, proc_coefficient, damage, out, error, error_size, 0);
}

__attribute__((visibility("default")))
int megabonk_lua_session_enemy_killed_actions_v2(void *handle, double stacks, int has_target, int boss, int elite,
        double target_health, int element, int critical, double proc_coefficient, double damage,
        double *out, char *error, size_t error_size) {
    return enemy_killed_actions(handle, stacks, has_target, boss, elite, target_health, element, critical, proc_coefficient, damage, out, error, error_size, 1);
}

struct player_damage_context {
    const char *event;
    double stacks, hp, max_hp, shield, max_shield;
    int shield_hit_known, shield_hit, has_enemy, boss, elite, element, critical;
};

/* All Lua allocation and table access stays inside the protected call. */
static int dispatch_player_damage(lua_State *state) {
    const struct player_damage_context *c = lua_touserdata(state, 3);
    lua_pushstring(state, c->event); lua_rawget(state, 1);
    if (lua_isnil(state, -1)) { lua_pushnil(state); return 1; }
    if (!lua_isfunction(state, -1)) return luaL_error(state, "event handler must be a function");
    lua_pushvalue(state, 2);
    lua_createtable(state, 0, 12);
    lua_pushnumber(state, c->stacks); lua_setfield(state, -2, "stacks");
    lua_pushnumber(state, c->hp); lua_setfield(state, -2, "hp");
    lua_pushnumber(state, c->max_hp); lua_setfield(state, -2, "max_hp");
    lua_pushnumber(state, c->shield); lua_setfield(state, -2, "shield");
    lua_pushnumber(state, c->max_shield); lua_setfield(state, -2, "max_shield");
    if (c->shield_hit_known) { lua_pushboolean(state, c->shield_hit); lua_setfield(state, -2, "shield_hit"); }
    lua_pushboolean(state, c->has_enemy); lua_setfield(state, -2, "has_enemy");
    lua_pushboolean(state, c->boss); lua_setfield(state, -2, "boss");
    lua_pushboolean(state, c->elite); lua_setfield(state, -2, "elite");
    lua_pushinteger(state, c->element); lua_setfield(state, -2, "element");
    lua_pushboolean(state, c->critical); lua_setfield(state, -2, "critical");
    lua_call(state, 2, 1); return 1;
}

__attribute__((visibility("default")))
int megabonk_lua_session_player_damage(void *handle, int after, double damage, double stacks,
        double hp, double max_hp, double shield, double max_shield, int shield_hit,
        int has_enemy, int boss, int elite, int element, int critical,
        double *out, char *error, size_t error_size) {
    struct megabonk_lua_session *session = handle;
    if (!session || !out || !error || !error_size) return 1;
    const struct player_damage_context context = { after ? "player_damaged" : "before_player_damage",
        stacks, hp, max_hp, shield, max_shield, after, shield_hit, has_enemy, boss, elite, element, critical };
    error[0] = 0;
    lua_State *state = session->state;
    lua_settop(state, 1);
    lua_pushcfunction(state, dispatch_player_damage); lua_pushvalue(state, 1); lua_pushnumber(state, damage); lua_pushlightuserdata(state, (void *)&context);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int status = lua_pcall(state, 3, 1, 0);
    lua_sethook(state, NULL, 0, 0);
    if (status) { copy_error(state, error, error_size); lua_settop(state, 1); return 1; }
    if (lua_isnil(state, -1)) { lua_settop(state, 1); return 2; }
    if (lua_type(state, -1) != LUA_TNUMBER) {
        strncpy(error, "event handler must return a number or nil", error_size - 1); error[error_size - 1] = 0;
        lua_settop(state, 1); return 1;
    }
    *out = lua_tonumber(state, -1); lua_settop(state, 1); return 0;
}

/* Returns 0 for a numeric result, 2 for a missing/nil callback, 1 on error.
 * Out is changed only on success; the state remains usable after a Lua error. */
__attribute__((visibility("default")))
int megabonk_lua_session_event(void *handle, const char *event, double input, double *out, char *error, size_t error_size) {
    struct megabonk_lua_session *session = handle;
    if (!session || !event || !*event || strlen(event) > 64 || !out || !error || !error_size) return 1;
    error[0] = 0;
    lua_State *state = session->state;
    lua_settop(state, 1);
    lua_pushcfunction(state, dispatch_event); lua_pushvalue(state, 1); lua_pushlightuserdata(state, (void *)event); lua_pushnumber(state, input);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int status = lua_pcall(state, 3, 1, 0);
    lua_sethook(state, NULL, 0, 0);
    if (status) { copy_error(state, error, error_size); lua_settop(state, 1); return 1; }
    if (lua_isnil(state, -1)) { lua_settop(state, 1); return 2; }
    if (lua_type(state, -1) != LUA_TNUMBER) {
        strncpy(error, "event handler must return a number or nil", error_size - 1); error[error_size - 1] = 0;
        lua_settop(state, 1); return 1;
    }
    *out = lua_tonumber(state, -1); lua_settop(state, 1); return 0;
}

__attribute__((visibility("default")))
int megabonk_lua_session_weapon_attack(void *handle, double level, const char *enemy, int boss, int elite,
        double hp, double distance, double *out, char *error, size_t error_size) {
    struct megabonk_lua_session *session = handle;
    if (!session || !enemy || !*enemy || strlen(enemy) > 120 || !isfinite(level) || !isfinite(hp) ||
        !isfinite(distance) || distance < 0 || !out || !error || !error_size) return 1;
    const struct weapon_target_context ctx = { enemy, boss, elite, hp, distance };
    error[0] = 0;
    lua_State *state = session->state;
    lua_settop(state, 1);
    lua_pushcfunction(state, dispatch_weapon_target); lua_pushvalue(state, 1); lua_pushnumber(state, level); lua_pushlightuserdata(state, (void *)&ctx);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int status = lua_pcall(state, 3, 1, 0);
    lua_sethook(state, NULL, 0, 0);
    if (status) { copy_error(state, error, error_size); lua_settop(state, 1); return 1; }
    if (lua_isnil(state, -1)) { lua_settop(state, 1); return 2; }
    if (lua_type(state, -1) != LUA_TNUMBER) {
        strncpy(error, "event handler must return a number or nil", error_size - 1); error[error_size - 1] = 0;
        lua_settop(state, 1); return 1;
    }
    *out = lua_tonumber(state, -1); lua_settop(state, 1); return 0;
}

__attribute__((visibility("default")))
int megabonk_lua_session_map_enemy(void *handle, const char *event, int stage, const char *enemy, double *out, char *error, size_t error_size) {
    struct megabonk_lua_session *session = handle;
    if (!session || !event || !*event || strlen(event) > 64 || !enemy || !*enemy || strlen(enemy) > 120 || stage < 1 || !out || !error || !error_size) return 1;
    struct map_enemy_context ctx = { event, enemy, stage };
    error[0] = 0;
    lua_State *state = session->state;
    lua_settop(state, 1);
    lua_pushcfunction(state, dispatch_map_enemy); lua_pushvalue(state, 1); lua_pushlightuserdata(state, &ctx);
    lua_sethook(state, instruction_limit, LUA_MASKCOUNT, INSTRUCTION_LIMIT);
    int status = lua_pcall(state, 2, 1, 0);
    lua_sethook(state, NULL, 0, 0);
    if (status) { copy_error(state, error, error_size); lua_settop(state, 1); return 1; }
    if (lua_isnil(state, -1)) { lua_settop(state, 1); return 2; }
    if (lua_type(state, -1) != LUA_TNUMBER) {
        strncpy(error, "event handler must return a number or nil", error_size - 1); error[error_size - 1] = 0;
        lua_settop(state, 1); return 1;
    }
    *out = lua_tonumber(state, -1); lua_settop(state, 1); return 0;
}

__attribute__((visibility("default")))
void megabonk_lua_session_destroy(void *handle) {
    struct megabonk_lua_session *session = handle;
    if (!session) return;
    lua_close(session->state); free(session);
}
