-- Each callback receives the one-based stage number.
return {
    stage_spawn_multiplier = function(stage) return 1.1 end,
    stage_enemy_health_multiplier = function(stage) return 1.1 end,
    stage_enemy_speed_multiplier = function(stage) return 1.0 end,
    stage_enemy_damage_multiplier = function(stage) return 1.0 end,
}
