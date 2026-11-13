-- Combine small bonuses from a list. This example returns 1.15 (+15%).
local bonuses = {0.05, 0.10}
local multiplier = 1
for _, bonus in ipairs(bonuses) do
    multiplier = multiplier + bonus
end
return multiplier
