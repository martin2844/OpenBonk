-- Runs once each unpaused game second while the item is held.
return {
  tick = function(stacks, player)
    if not player.health_available or player.hp <= 0 then return {xp = 0} end
    return {xp = math.min(3 * stacks, 1000)}
  end
}
