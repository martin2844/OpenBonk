-- Runs once per in-game second while at least one copy is held.
-- The context is a snapshot, so this can inspect health but cannot write it.
return {
  tick = function(stacks, player)
    if player.health_available and player.hp > 0 and player.hp < player.max_hp * 0.5 then
      return {heal = math.min(2 * stacks, 100)}
    end
    return {heal = 0}
  end
}
