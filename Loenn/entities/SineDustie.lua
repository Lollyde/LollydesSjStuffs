local drawableSpriteStruct = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")

local sineDustie = {}

sineDustie.name = "lollyde_sj/SineDustie"
sineDustie.depth = -10501
sineDustie.fillColor = {210/255,25/255,90/255,120/255}
sineDustie.borderColor = {25/255,25/255,25/255,200/255}

sineDustie.placements = {
    name = "lollyde_sj_sinedustie",
    data = {
        width = 16,
        height = 16,
		xPeriod = 1,
		xPhase = 0,
		yPeriod = 1,
		yPhase = 1,
		xLinear = true,
		yLinear = false
    }
}

function sineDustie.draw(room, entity, viewport)
	local spriteData = {
        x = entity.x + entity.width/2,
        y = entity.y + entity.height/2
    }

	drawableRectangle.fromRectangle("bordered", entity.x, entity.y, entity.width, entity.height, {210/255,25/255,90/255,120/255}, {25/255,25/255,25/255,200/255}):draw()
	drawableSpriteStruct.fromTexture("danger/dustcreature/base00", spriteData):draw()
	drawableSpriteStruct.fromTexture("danger/dustcreature/center00", spriteData):draw()
end

return sineDustie