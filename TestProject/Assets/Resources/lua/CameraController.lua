import("UnityEngine")

CameraController = class("CameraController", function() return LuaLu.LuaComponent.new() end)

function CameraController:Start()
  self.player = GameObject.Find("Player")
  local spos = self.transform.position
  self.offset = Vector3.new(spos.x, spos.y, spos.z)
  local p = self.player.transform.position
  self.offset.x = self.offset.x - p.x
  self.offset.y = self.offset.y - p.y
  self.offset.z = self.offset.z - p.z
end

function CameraController:Update()
end

function CameraController:LateUpdate()
  local ppos = self.player.transform.position
  local p = Vector3.new(ppos.x, ppos.y, ppos.z)
  p.x = p.x + self.offset.x
  p.y = p.y + self.offset.y
  p.z = p.z + self.offset.z
  self.transform.position = p
end