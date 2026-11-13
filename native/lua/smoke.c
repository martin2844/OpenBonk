#include <math.h>
#include <stdio.h>
#include <string.h>

int megabonk_lua_eval(const char *, double *, char *, unsigned long);
int megabonk_lua_eval_values(const char *, int, double *, char *, unsigned long);
void *megabonk_lua_session_create(const char *, char *, unsigned long);
int megabonk_lua_session_event(void *, const char *, double, double *, char *, unsigned long);
int megabonk_lua_session_weapon_attack(void *, double, const char *, int, int, double, double, double *, char *, unsigned long);
int megabonk_lua_session_tick_actions(void *, double, double *, char *, unsigned long);
int megabonk_lua_session_tick_context(void *, double, int, double, double, double, double, double, double *, char *, unsigned long);
int megabonk_lua_session_tick_context_v2(void *, double, int, double, double, double, double, double, int, int, int, double, double *, char *, unsigned long);
int megabonk_lua_session_tick_context_v3(void *, double, int, double, double, double, double, double, int, int, int, double, double *, char *, unsigned long);
int megabonk_lua_session_map_enemy(void *, const char *, int, const char *, double *, char *, unsigned long);
int megabonk_lua_session_pre_attack(void *, double, int, int, int, double, int, int, double, double *, char *, unsigned long);
int megabonk_lua_session_on_hit(void *, double, int, int, int, double, int, int, double, double, double *, char *, unsigned long);
int megabonk_lua_session_on_hit_actions(void *, double, int, int, int, double, int, int, double, double, double *, char *, unsigned long);
int megabonk_lua_session_enemy_killed(void *, double, int, int, int, double, int, int, double, double, double *, char *, unsigned long);
int megabonk_lua_session_enemy_killed_actions(void *, double, int, int, int, double, int, int, double, double, double *, char *, unsigned long);
int megabonk_lua_session_enemy_killed_actions_v2(void *, double, int, int, int, double, int, int, double, double, double *, char *, unsigned long);
int megabonk_lua_session_player_damage(void *, int, double, double, double, double, double, double, int, int, int, int, int, int, double *, char *, unsigned long);
void megabonk_lua_session_destroy(void *);
static int check_tick_xp(void) {
    char error[256]; double values[3] = {-1,-1,-1}; int failed=0;
    const char *script="return {tick=function(n,c) if n==1 then return {scale=2,heal=7,xp=42} elseif n==2 then return {heal=4,xp=-1} elseif n==3 then return {xp=1.5} elseif n==4 then return {xp=1001} elseif n==5 then return {xp=0/0} elseif n==6 then return {xp='3'} else return {heal=2} end end}";
    void *s=megabonk_lua_session_create(script,error,sizeof error); if(!s)return 1;
    int status=megabonk_lua_session_tick_context_v3(s,1,1,20,100,3,10,17,1,7,1,12.5,values,error,sizeof error);
    if(status || values[0]!=2 || values[1]!=7 || values[2]!=42)failed++;
    for(int i=2;i<=6;i++) {
        values[0]=values[1]=values[2]=123;
        status=megabonk_lua_session_tick_context_v3(s,i,1,20,100,3,10,17,1,7,1,12.5,values,error,sizeof error);
        if(status!=1 || values[0]!=123 || values[1]!=123 || values[2]!=123)failed++;
    }
    status=megabonk_lua_session_tick_context_v3(s,7,1,20,100,3,10,17,1,7,1,12.5,values,error,sizeof error);
    if(status || values[0]!=1 || values[1]!=2 || values[2]!=0)failed++;
    values[0]=values[1]=values[2]=123;
    status=megabonk_lua_session_tick_context_v2(s,1,1,20,100,3,10,17,1,7,1,12.5,values,error,sizeof error);
    if(status || values[0]!=2 || values[1]!=7 || values[2]!=123)failed++;
    megabonk_lua_session_destroy(s);printf("%s tick XP compatibility and atomicity failures=%d\n",failed?"FAIL":"PASS",failed);return failed!=0;
}
static int check_tick_context(void) {
    char error[256]; double values[2] = {-1, -1}; int failed = 0;
    const char *script = "return {tick=function(n,c) if not c.health_available then return {scale=1} end if n==2 and c.hp==20 and c.max_hp==100 and c.shield==3 and c.max_shield==10 and c.time==17 then c.hp=99 return {scale=2,heal=4} end return 0 end}";
    void *s = megabonk_lua_session_create(script, error, sizeof error);
    if (!s) return 1;
    int status = megabonk_lua_session_tick_context(s, 2, 1, 20, 100, 3, 10, 17, values, error, sizeof error);
    if (status || values[0] != 2 || values[1] != 4) failed++;
    status = megabonk_lua_session_tick_context(s, 2, 0, 0, 0, 0, 0, 18, values, error, sizeof error);
    if (status || values[0] != 1 || values[1] != 0) failed++;
    values[0] = values[1] = 123;
    status = megabonk_lua_session_tick_context(s, 2, 1, NAN, 100, 3, 10, 17, values, error, sizeof error);
    if (status != 1 || values[0] != 123 || values[1] != 123 || !strstr(error, "invalid tick context")) failed++;
    megabonk_lua_session_destroy(s);
    printf("%s tick context failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check_tick_context_v2(void) {
    char error[256]; double values[2] = {-1, -1}; int failed = 0;
    const char *script = "return {tick=function(n,c) if c.level_available and c.level==7 and c.manager_time_available and c.manager_time==12.5 then c.level=99 return {scale=2,heal=4} elseif c.level_available==false and c.level==0 and c.manager_time_available==false and c.manager_time==0 then return {scale=1} end return 0 end}";
    void *s = megabonk_lua_session_create(script, error, sizeof error);
    if (!s) return 1;
    int status = megabonk_lua_session_tick_context_v2(s, 2, 1, 20, 100, 3, 10, 17, 1, 7, 1, 12.5, values, error, sizeof error);
    if (status || values[0] != 2 || values[1] != 4) failed++;
    status = megabonk_lua_session_tick_context_v2(s, 2, 0, 0, 0, 0, 0, 18, 0, 0, 0, 0, values, error, sizeof error);
    if (status || values[0] != 1 || values[1] != 0) failed++;
    values[0] = values[1] = 123;
    status = megabonk_lua_session_tick_context_v2(s, 2, 1, 20, 100, 3, 10, 17, 1, 7, 1, NAN, values, error, sizeof error);
    if (status != 1 || values[0] != 123 || values[1] != 123 || !strstr(error, "invalid tick context")) failed++;
    status = megabonk_lua_session_tick_context_v2(s, 2, 1, 20, 100, 3, 10, 17, 0, 7, 1, 2, values, error, sizeof error);
    if (status != 1 || values[0] != 123 || values[1] != 123) failed++;
    megabonk_lua_session_destroy(s);
    printf("%s extended tick context failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check_standard_libraries(void) {
    char error[256]; double value = -1, values[2] = {-1, -1}; int failed = 0;
    const char *script = "local t={3,1,2} table.sort(t) local n=0 for _,v in ipairs(t) do n=n+v end return n+tonumber(string.sub('v4',2))+utf8.len('hi')";
    if (megabonk_lua_eval(script, &value, error, sizeof error) || value != 12) failed++;
    if (megabonk_lua_eval_values("return {string.len('abc'),table.concat({'a','b'})=='ab' and 2 or 0}", 2, values, error, sizeof error) || values[0] != 3 || values[1] != 2) failed++;
    void *s = megabonk_lua_session_create("local t={} return {tick=function(n) table.insert(t,n) local sum=0 for _,v in ipairs(t) do sum=sum+v end return sum end}", error, sizeof error);
    if (!s) failed++; else {
        if (megabonk_lua_session_event(s, "tick", 2, &value, error, sizeof error) || value != 2) failed++;
        if (megabonk_lua_session_event(s, "tick", 3, &value, error, sizeof error) || value != 5) failed++;
        megabonk_lua_session_destroy(s);
    }
    const char *blocked = "return (io==nil and os==nil and debug==nil and package==nil and require==nil and load==nil and loadfile==nil and dofile==nil and pcall==nil and xpcall==nil and setmetatable==nil and getmetatable==nil and collectgarbage==nil and _G==nil) and 1 or 0";
    if (megabonk_lua_eval(blocked, &value, error, sizeof error) || value != 1) failed++;
    if (!megabonk_lua_eval("return #string.rep('x',9000000)", &value, error, sizeof error)) failed++;
    if (!megabonk_lua_eval("local t={2,1}; table.sort(t,function() while true do end end); return 1", &value, error, sizeof error) || !strstr(error, "instruction limit")) failed++;
    printf("%s standard libraries failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check_on_hit_actions(void) {
    char error[256]; double values[5] = {-7,-7,-7,-7,-7}; int failed = 0;
    const char *script = "local n=0 return {on_hit=function(stacks,c) n=n+1 if n==1 and stacks==2 and c.damage==5 then return {damage=.25,freeze=3,poison=4} elseif n==2 then return .5 elseif n==3 then return {freeze=31} elseif n==4 then return {stun='bad'} elseif n==5 then return {damage=0/0} end return {} end}";
    void *s = megabonk_lua_session_create(script, error, sizeof error);
    if (!s) return 1;
    int status = megabonk_lua_session_on_hit_actions(s,2,1,0,0,10,0,0,1,5,values,error,sizeof error);
    if (status || values[0]!=.25 || values[1]!=3 || values[2]!=0 || values[3]!=0 || values[4]!=4) failed++;
    status = megabonk_lua_session_on_hit_actions(s,2,1,0,0,10,0,0,1,5,values,error,sizeof error);
    if (status || values[0]!=.5 || values[1]!=0 || values[4]!=0) failed++;
    for (int i=0;i<5;i++) values[i]=123;
    for (int test=0;test<3;test++) {
        status = megabonk_lua_session_on_hit_actions(s,2,1,0,0,10,0,0,1,5,values,error,sizeof error);
        if (status!=1) failed++;
        for (int i=0;i<5;i++) if (values[i]!=123) failed++;
    }
    status = megabonk_lua_session_on_hit_actions(s,2,1,0,0,10,0,0,1,5,values,error,sizeof error);
    if (status) failed++; else for (int i=0;i<5;i++) if (values[i]!=0) failed++;
    megabonk_lua_session_destroy(s);
    printf("%s on-hit action table failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check_player_damage(void) {
    char error[256]; double value = -7; int failed = 0;
    const char *script = "return {before_player_damage=function(d,c) if d==10 and c.stacks==2 and c.hp==80 and c.max_hp==100 and c.shield==25 and c.max_shield==40 and c.shield_hit==nil and c.has_enemy and c.boss and not c.elite and c.element==1 and c.critical then c.hp=0 return .5 end return 'bad' end, player_damaged=function(d,c) if d==5 and c.hp==80 and c.shield==20 and c.shield_hit and not c.has_enemy then return 2 end return 'bad' end}";
    void *s = megabonk_lua_session_create(script, error, sizeof error);
    if (!s) return 1;
    int status = megabonk_lua_session_player_damage(s, 0, 10, 2, 80, 100, 25, 40, 0, 1, 1, 0, 1, 1, &value, error, sizeof error);
    if (status || value != .5) failed++;
    status = megabonk_lua_session_player_damage(s, 1, 5, 2, 80, 100, 20, 40, 1, 0, 0, 0, 0, 0, &value, error, sizeof error);
    if (status || value != 2) failed++;
    megabonk_lua_session_destroy(s);
    s = megabonk_lua_session_create("return {before_player_damage=function(d) return d/10 end}", error, sizeof error);
    if (!s) failed++; else {
        status = megabonk_lua_session_player_damage(s, 0, 5, 1, 80, 100, 0, 0, 0, 0, 0, 0, 0, 0, &value, error, sizeof error);
        if (status || value != .5) failed++;
        value = 123;
        status = megabonk_lua_session_player_damage(s, 1, 5, 1, 80, 100, 0, 0, 0, 0, 0, 0, 0, 0, &value, error, sizeof error);
        if (status != 2 || value != 123) failed++;
        megabonk_lua_session_destroy(s);
    }
    s = megabonk_lua_session_create("return {player_damaged=function() while true do end end}", error, sizeof error);
    if (!s) failed++; else {
        status = megabonk_lua_session_player_damage(s, 1, 5, 1, 80, 100, 0, 0, 0, 0, 0, 0, 0, 0, &value, error, sizeof error);
        if (status != 1 || value != 123 || !strstr(error, "instruction limit")) failed++;
        megabonk_lua_session_destroy(s);
    }
    printf("%s player-damage context failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check_enemy_killed(void) {
    char error[256]; double value = -7; int failed = 0;
    void *s = megabonk_lua_session_create("local n=0 return {enemy_killed=function(stacks,c) n=n+1 if c.has_target and c.boss and not c.elite and c.target_health==-2 and c.damage==7 and c.proc_coefficient==0.5 and c.element==1 and c.critical then return stacks+n end return 'bad' end}", error, sizeof error);
    if (!s) return 1;
    for (int i=0; i<10000; i++) { int status=megabonk_lua_session_enemy_killed(s, 2, 1, 1, 0, -2, 1, 1, .5, 7, &value, error, sizeof error); if (status || value != i+3) { failed++; break; } }
    megabonk_lua_session_destroy(s);
    s = megabonk_lua_session_create("return {enemy_killed=function() while true do end end}", error, sizeof error);
    value = 123;
    if (!s) failed++; else { int status=megabonk_lua_session_enemy_killed(s, 1, 0, 0, 0, 0, 0, 0, 0, 0, &value, error, sizeof error); if (status != 1 || value != 123 || !strstr(error, "instruction limit")) failed++; megabonk_lua_session_destroy(s); }
    printf("%s enemy-killed context failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}

static int check_enemy_killed_actions(void) {
    int failed = 0; char error[256]; double v[3] = {99,99,99};
    void *s = megabonk_lua_session_create("local n=0 return {enemy_killed=function(stacks,c) n=n+1 if n==1 then return 2 end if n==2 then return {heal=stacks,xp=3} end if n==3 then return {heal=5,xp=1.5} end return {scale=11,heal=3} end}", error, sizeof error);
    if (!s) return 1;
    int status = megabonk_lua_session_enemy_killed_actions(s,2,1,0,0,0,0,0,1,5,v,error,sizeof error);
    if (status || v[0]!=2 || v[1]!=0 || v[2]!=0) failed++;
    status = megabonk_lua_session_enemy_killed_actions(s,2,1,0,0,0,0,0,1,5,v,error,sizeof error);
    if (status || v[0]!=1 || v[1]!=2 || v[2]!=3) failed++;
    v[0]=v[1]=v[2]=99;
    status = megabonk_lua_session_enemy_killed_actions(s,2,1,0,0,0,0,0,1,5,v,error,sizeof error);
    if (status!=1 || v[0]!=99 || v[1]!=99 || v[2]!=99) failed++;
    status = megabonk_lua_session_enemy_killed_actions(s,2,1,0,0,0,0,0,1,5,v,error,sizeof error);
    if (status!=1 || v[0]!=99 || v[1]!=99 || v[2]!=99) failed++;
    megabonk_lua_session_destroy(s);
    printf("%s enemy-killed actions compatibility and atomicity failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed;
}
static int check_enemy_killed_gold(void) {
    int failed = 0; char error[256]; double v[4] = {99,99,99,99};
    void *s = megabonk_lua_session_create("local n=0 return {enemy_killed=function() n=n+1 if n==1 then return 2 elseif n==2 then return {heal=3,xp=4,gold=5} elseif n==3 then return {heal=6,gold=1.5} elseif n==4 then return {gold=101} else return {gold=7} end end}", error, sizeof error);
    if (!s) return 1;
    int status = megabonk_lua_session_enemy_killed_actions_v2(s,1,1,0,0,0,0,0,1,5,v,error,sizeof error);
    if (status || v[0]!=2 || v[1]!=0 || v[2]!=0 || v[3]!=0) failed++;
    status = megabonk_lua_session_enemy_killed_actions_v2(s,1,1,0,0,0,0,0,1,5,v,error,sizeof error);
    if (status || v[0]!=1 || v[1]!=3 || v[2]!=4 || v[3]!=5) failed++;
    v[0]=v[1]=v[2]=v[3]=99;
    for (int i=0;i<2;i++) {
        status = megabonk_lua_session_enemy_killed_actions_v2(s,1,1,0,0,0,0,0,1,5,v,error,sizeof error);
        if (status!=1 || v[0]!=99 || v[1]!=99 || v[2]!=99 || v[3]!=99) failed++;
    }
    status = megabonk_lua_session_enemy_killed_actions(s,1,1,0,0,0,0,0,1,5,v,error,sizeof error);
    if (status || v[0]!=1 || v[1]!=0 || v[2]!=0 || v[3]!=99) failed++;
    megabonk_lua_session_destroy(s);
    printf("%s enemy-killed gold compatibility and atomicity failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed;
}
static int check_on_hit(void) {
    char error[256]; double value = -7; int failed = 0;
    const char *script = "local calls=0 return {on_hit=function(n,c) calls=calls+1 if c.has_target and c.boss and c.target_health==35 and c.damage==7 and c.proc_coefficient==0.5 and c.element==1 and c.critical then return n+calls end return 'bad' end}";
    void *s = megabonk_lua_session_create(script, error, sizeof error);
    if (!s) return 1;
    for (int i=0; i<10000; i++) {
        int status = megabonk_lua_session_on_hit(s, 2, 1, 1, 0, 35, 1, 1, .5, 7, &value, error, sizeof error);
        if (status || value != i+3) { failed++; break; }
    }
    value = 123;
    int status = megabonk_lua_session_pre_attack(s, 1, 0, 0, 0, 0, 0, 0, 0, &value, error, sizeof error);
    if (status != 2 || value != 123) failed++;
    megabonk_lua_session_destroy(s);
    s = megabonk_lua_session_create("return {on_hit=function() while true do end end}", error, sizeof error);
    if (!s) failed++;
    else { status = megabonk_lua_session_on_hit(s, 1, 1, 0, 0, 10, 0, 0, 1, 5, &value, error, sizeof error); if (status != 1 || value != 123 || !strstr(error, "instruction limit")) failed++; megabonk_lua_session_destroy(s); }
    printf("%s on-hit context failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check_attack_context(void) {
    char error[256]; double value = -7; int failed = 0;
    const char *script = "local calls=0 return { pre_attack=function(n,c) calls=calls+1 if c.has_target and c.boss and not c.elite and c.target_health==42 and c.element==1 and c.critical and c.proc_coefficient==0.5 then return n+calls end if not c.has_target and not c.boss and not c.elite and c.target_health==0 then return calls end return 'bad context' end }";
    void *s = megabonk_lua_session_create(script, error, sizeof error);
    if (!s) { printf("FAIL context create: %s\n", error); return 1; }
    int status = megabonk_lua_session_pre_attack(s, 2, 1, 1, 0, 42, 1, 1, .5, &value, error, sizeof error);
    if (status || value != 3) failed++;
    status = megabonk_lua_session_pre_attack(s, 2, 0, 0, 0, 0, 0, 0, 1, &value, error, sizeof error);
    if (status || value != 2) failed++;
    status = megabonk_lua_session_pre_attack(s, 2, 1, 0, 1, 10, 0, 0, 1, &value, error, sizeof error);
    if (status != 1 || value != 2 || !strstr(error, "number or nil")) failed++;
    for (int i = 0; i < 10000; i++) {
        status = megabonk_lua_session_pre_attack(s, 2, 1, 1, 0, 42, 1, 1, .5, &value, error, sizeof error);
        if (status || value != i + 6) { failed++; break; }
    }
    value = 2; /* Preserve a sentinel for the following failure-path checks. */
    megabonk_lua_session_destroy(s);
    s = megabonk_lua_session_create("return {}", error, sizeof error);
    if (!s) failed++;
    else { status = megabonk_lua_session_pre_attack(s, 1, 0, 0, 0, 0, 0, 0, 0, &value, error, sizeof error); if (status != 2 || value != 2) failed++; megabonk_lua_session_destroy(s); }
    s = megabonk_lua_session_create("return {pre_attack=function() while true do end end}", error, sizeof error);
    if (!s) failed++;
    else { status = megabonk_lua_session_pre_attack(s, 1, 0, 0, 0, 0, 0, 0, 0, &value, error, sizeof error); if (status != 1 || value != 2 || !strstr(error, "instruction limit")) failed++; megabonk_lua_session_destroy(s); }
    printf("%s pre-attack context failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check_session(void) {
    char error[256]; double value = -7; int failed = 0;
    void *s = megabonk_lua_session_create("local n=0 return { tick=function(x) n=n+x return n end, bad=function() return 'oops' end, loop=function() while true do end end, oom=function() local x='x' while true do x=x..x end end }", error, sizeof error);
    if (!s) { printf("FAIL session create: %s\n", error); return 1; }
    int result = megabonk_lua_session_event(s, "tick", 2, &value, error, sizeof error);
    if (result || value != 2) failed++;
    result = megabonk_lua_session_event(s, "tick", 3, &value, error, sizeof error);
    if (result || value != 5) failed++;
    result = megabonk_lua_session_event(s, "missing", 0, &value, error, sizeof error);
    if (result != 2 || value != 5) failed++;
    result = megabonk_lua_session_event(s, "bad", 0, &value, error, sizeof error);
    if (result != 1 || value != 5 || !strstr(error, "number or nil")) failed++;
    result = megabonk_lua_session_event(s, "loop", 0, &value, error, sizeof error);
    if (result != 1 || value != 5 || !strstr(error, "instruction limit")) failed++;
    result = megabonk_lua_session_event(s, "oom", 0, &value, error, sizeof error);
    if (result != 1 || value != 5 || !strstr(error, "memory")) failed++;
    result = megabonk_lua_session_event(s, "tick", 1, &value, error, sizeof error);
    if (result || value != 6) failed++;
    for (int i = 0; i < 10000; i++) {
        result = megabonk_lua_session_event(s, "tick", 1, &value, error, sizeof error);
        if (result || value != i + 7) { failed++; break; }
    }
    megabonk_lua_session_destroy(s);
    s = megabonk_lua_session_create("return 42", error, sizeof error);
    if (s || !strstr(error, "callback table")) failed++;
    if (s) megabonk_lua_session_destroy(s);
    s = megabonk_lua_session_create("while true do end", error, sizeof error);
    if (s || !strstr(error, "instruction limit")) failed++;
    if (s) megabonk_lua_session_destroy(s);
    printf("%s persistent event state failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check(const char *name, const char *script, int expected, double value, const char *message) {
    double result = -1;
    char error[256];
    int actual = megabonk_lua_eval(script, &result, error, sizeof error);
    int ok = actual == expected && (actual ? strstr(error, message) != NULL : fabs(result - value) < .0001);
    printf("%s %s result=%.2f error=%s\n", ok ? "PASS" : "FAIL", name, result, error);
    return !ok;
}
static int check_values(const char *name, const char *script, int count, int expected, const char *message) {
    double values[6] = {-7,-7,-7,-7,-7,-7}; char error[256];
    int status = megabonk_lua_eval_values(script, count, values, error, sizeof error);
    int ok = status == expected && (status ? strstr(error, message) != NULL && values[0] == -7 : values[0] == 1.25 && values[1] == 0.5);
    printf("%s %s first=%.2f second=%.2f error=%s\n", ok ? "PASS" : "FAIL", name, values[0], values[1], error);
    return !ok;
}
static int check_many_values(void) {
    double values[112]; char error[256] = {0};
    for (int i = 0; i < 112; i++) values[i] = -7;
    int status = megabonk_lua_eval_values("local t={} for i=1,112 do t[i]=i end return t", 112, values, error, sizeof error);
    int ok = status == 0;
    for (int i = 0; i < 112; i++) if (values[i] != i + 1) ok = 0;
    for (int i = 0; i < 112; i++) values[i] = -7;
    status = megabonk_lua_eval_values("local t={} for i=1,112 do t[i]=i end t[112]='bad' return t", 112, values, error, sizeof error);
    if (status != 1 || !strstr(error, "must be numbers")) ok = 0;
    for (int i = 0; i < 112; i++) if (values[i] != -7) ok = 0;
    if (megabonk_lua_eval_values("return {}", 113, values, error, sizeof error) != 1) ok = 0;
    printf("%s 112 values, late failure is atomic, 113 rejected\n", ok ? "PASS" : "FAIL");
    return !ok;
}
static int check_map_enemy(void) {
    char error[256] = {0}; double value = -7; int failed = 0;
    const char *script = "return {hp=function(c) if c.stage==2 and c.enemy=='Goblin' then return 2 end return 0.5 end, bad=function() return 'bad' end, loop=function() while true do end end}";
    void *s = megabonk_lua_session_create(script, error, sizeof error);
    if (!s) { printf("FAIL map enemy create: %s\n", error); return 1; }
    if (megabonk_lua_session_map_enemy(s, "hp", 2, "Goblin", &value, error, sizeof error) || value != 2) failed++;
    if (megabonk_lua_session_map_enemy(s, "hp", 2, "Skeleton", &value, error, sizeof error) || value != .5) failed++;
    value = -7;
    if (megabonk_lua_session_map_enemy(s, "missing", 1, "Goblin", &value, error, sizeof error) != 2 || value != -7) failed++;
    if (megabonk_lua_session_map_enemy(s, "bad", 1, "Goblin", &value, error, sizeof error) != 1 || value != -7) failed++;
    if (megabonk_lua_session_map_enemy(s, "loop", 1, "Goblin", &value, error, sizeof error) != 1 || !strstr(error, "instruction limit") || value != -7) failed++;
    if (megabonk_lua_session_map_enemy(s, "hp", 0, "Goblin", &value, error, sizeof error) != 1 || value != -7) failed++;
    megabonk_lua_session_destroy(s);
    printf("%s map enemy context failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check_tick_actions(void) {
    char error[256] = {0}; double values[2] = {-7,-7}; int failed = 0;
    const char *script = "local n=0 return {tick=function(s) n=n+1 if n==1 then return s elseif n==2 then return {scale=3,heal=5} elseif n==3 then return {heal=7} elseif n==4 then return {scale=2,heal=101} elseif n==5 then return {heal=0/0} elseif n==6 then return {heal='bad'} end return {} end}";
    void *s = megabonk_lua_session_create(script,error,sizeof error);
    if (!s) return 1;
    if (megabonk_lua_session_tick_actions(s,2,values,error,sizeof error) || values[0]!=2 || values[1]!=0) failed++;
    if (megabonk_lua_session_tick_actions(s,2,values,error,sizeof error) || values[0]!=3 || values[1]!=5) failed++;
    if (megabonk_lua_session_tick_actions(s,2,values,error,sizeof error) || values[0]!=1 || values[1]!=7) failed++;
    for (int i=0;i<3;i++) { values[0]=-7; values[1]=-7; if (megabonk_lua_session_tick_actions(s,2,values,error,sizeof error)!=1 || values[0]!=-7 || values[1]!=-7) failed++; }
    if (megabonk_lua_session_tick_actions(s,2,values,error,sizeof error) || values[0]!=1 || values[1]!=0) failed++;
    megabonk_lua_session_destroy(s);
    s=megabonk_lua_session_create("return {tick=function() while true do end end}",error,sizeof error); values[0]=-7; values[1]=-7;
    if (!s || megabonk_lua_session_tick_actions(s,1,values,error,sizeof error)!=1 || !strstr(error,"instruction limit") || values[0]!=-7 || values[1]!=-7) failed++;
    if (s) megabonk_lua_session_destroy(s);
    printf("%s tick action atomicity and compatibility failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
static int check_weapon_target(void) {
    char error[256] = {0}; double value = -7; int failed = 0;
    void *s = megabonk_lua_session_create("local n=0 return {attack=function(level,c) n=n+1 if level==3 and c.enemy=='Goblin' and not c.boss and c.elite and c.target_health==45 and c.distance==6 then return n end return 0 end}", error, sizeof error);
    if (!s) return 1;
    if (megabonk_lua_session_weapon_attack(s,3,"Goblin",0,1,45,6,&value,error,sizeof error) || value != 1) failed++;
    if (megabonk_lua_session_weapon_attack(s,3,"Goblin",0,1,45,6,&value,error,sizeof error) || value != 2) failed++;
    value = -7;
    if (megabonk_lua_session_weapon_attack(s,3,"Goblin",0,1,45,-1,&value,error,sizeof error) != 1 || value != -7) failed++;
    megabonk_lua_session_destroy(s);
    s = megabonk_lua_session_create("return {attack=function(level) return level+1 end}", error, sizeof error);
    if (!s || megabonk_lua_session_weapon_attack(s,3,"Goblin",0,0,10,1,&value,error,sizeof error) || value != 4) failed++;
    if (s) megabonk_lua_session_destroy(s);
    s = megabonk_lua_session_create("return {attack=function() while true do end end}", error, sizeof error); value = -7;
    if (!s || megabonk_lua_session_weapon_attack(s,3,"Goblin",0,0,10,1,&value,error,sizeof error) != 1 || value != -7 || !strstr(error,"instruction limit")) failed++;
    if (s) megabonk_lua_session_destroy(s);
    s = megabonk_lua_session_create("return {}", error, sizeof error); value = -7;
    if (!s || megabonk_lua_session_weapon_attack(s,3,"Goblin",0,0,10,1,&value,error,sizeof error) != 2 || value != -7) failed++;
    if (s) megabonk_lua_session_destroy(s);
    printf("%s weapon target context failures=%d\n", failed ? "FAIL" : "PASS", failed);
    return failed != 0;
}
int main(void) {
    int failed = 0;
    failed += check("math", "return math.max(2, 4) * 3", 0, 12, "");
    failed += check("syntax", "return )", 1, 0, "unexpected");
    failed += check("type", "return '12'", 1, 0, "must return a number");
    failed += check("file access", "return os.execute('id')", 1, 0, "nil value");
    failed += check("instruction limit", "while true do end", 1, 0, "instruction limit");
    failed += check("memory limit", "local s='x' while true do s=s..s end", 1, 0, "memory");
    failed += check("recovery", "return 7", 0, 7, "");
    failed += check_values("multiple", "return {1.25, math.max(0.4, 0.5)}", 2, 0, "");
    failed += check_values("wrong count", "return {1.25}", 2, 1, "matching the hook count");
    failed += check_values("wrong value", "return {1.25, 'bad'}", 2, 1, "must be numbers");
    failed += check_values("table required", "return 1.25", 2, 1, "matching the hook count");
    failed += check_values("multiple instruction limit", "while true do end", 2, 1, "instruction limit");
    failed += check_standard_libraries();
    failed += check_tick_context();
    failed += check_tick_context_v2();
    failed += check_tick_xp();
    failed += check_many_values();
    failed += check_session();
    failed += check_attack_context();
    failed += check_on_hit();
    failed += check_on_hit_actions();
    failed += check_enemy_killed();
    failed += check_enemy_killed_actions();
    failed += check_enemy_killed_gold();
    failed += check_player_damage();
    failed += check_map_enemy();
    failed += check_weapon_target();
    failed += check_tick_actions();
    return failed ? 1 : 0;
}
