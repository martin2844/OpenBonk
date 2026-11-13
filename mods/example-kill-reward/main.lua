-- Grants a small reward for each enemy death while this item is held.
return {
    enemy_killed = function(stacks, context)
        if not context.has_target then return {} end
        return { heal = math.min(stacks, 100), xp = math.min(stacks, 100) }
    end
}
