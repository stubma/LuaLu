import("UnityEngine")

PickUpController = class("PickUpController", function() return LuaLu.LuaComponent.new() end)

function PickUpController:Update()
  local v = Vector3.new(15, 30, 45) * Time.deltaTime
  self.transform:Rotate(v)
end