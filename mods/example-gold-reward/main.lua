-- Request gold through the game's normal gold handling for each enemy death.
return {
    enemy_killed = function(stacks, context)
        if not context.has_target then return {} end
        return { gold = math.min(2 * stacks, 100) }
    end
}
