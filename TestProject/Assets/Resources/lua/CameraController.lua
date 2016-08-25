import("UnityEngine")

CameraController = class("CameraController", function() return LuaLu.LuaComponent.new() end)

function CameraController:Start()
  --[[self.player = GameObject.Find("Player")
  local spos = self.transform.position
  self.offset = Vector3.new(spos.x, spos.y, spos.z)
  print("self.offset.x " .. self.offset.x .. ", y " .. self.offset.y .. ", z " .. self.offset.z)
  local p = self.player.transform.position
  print("p.x " .. p.x .. ", y " .. p.y .. ", z " .. p.z)
  self.offset.x = self.offset.x - p.x
  self.offset.y = self.offset.y - p.y
  self.offset.z = self.offset.z - p.z
  print("after self.offset.x " .. self.offset.x .. ", y " .. self.offset.y .. ", z " .. self.offset.z)--]]
  self.offset = Vector3.new(1, 1, 1)
  print("before set x to 2")
  self.offset.x = 2
  print("self.offset.x " .. self.offset.x)
end

function CameraController:Update()
end

function CameraController:LateUpdate()
  --[[local ppos = self.player.transform.position
  local p = Vector3.new(ppos.x, ppos.y, ppos.z)
  print("p.x " .. p.x .. ", y " .. p.y .. ", z " .. p.z)
  p.x = p.x + self.offset.x
  p.y = p.y + self.offset.y
  p.z = p.z + self.offset.z
  print("after p.x " .. (p.x + self.offset.x) .. ", y " .. (p.y + self.offset.y) .. ", z " .. (p.z + self.offset.z))
  self.transform.position = p--]]
end