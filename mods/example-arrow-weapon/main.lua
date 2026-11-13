-- The host calls this once per attack after finding a target.
local attacks = 0
return {
  attack = function(level, target)
    attacks = attacks + 1
    if attacks % 3 == 0 then return 1.5 end
    return 1
  end
}
