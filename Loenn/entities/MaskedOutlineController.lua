local drawableRectangle = require("structs.drawable_rectangle")

local maskedOutline = {}

maskedOutline.name = "lollyde_sj/MaskedOutline"
maskedOutline.depth = -10501

maskedOutline.placements = {
    name = "lollyde_sj_maskedoutline"
}

function maskedOutline.draw(room, entity, viewport)
	drawableRectangle.fromRectangle("fill", entity.x, entity.y, 8, 8, {255,0,0,1}):draw()
end

return maskedOutline