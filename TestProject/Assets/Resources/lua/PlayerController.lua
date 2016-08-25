import("UnityEngine")

PlayerController = class("PlayerController", function() return LuaLu.LuaComponent.new() end)

function PlayerController:Update()
  --local v = Vector3.new(15 * Time.deltaTime, 30 * Time.deltaTime, 45 * Time.deltaTime)
  --self.transform:Rotate(v)
end