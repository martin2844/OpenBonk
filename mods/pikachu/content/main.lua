-- Thunderbolt prototype: every second successful attack is amplified.
-- The host supplies the weapon level and applies the returned multiplier to
-- the weapon's own damage before the normal player/item/critical calculation.
local attacks = 0
return {
    attack = function(level)
        attacks = attacks + 1
        if attacks % 2 == 0 then return 1.5 end
        return 1.0
    end
}
