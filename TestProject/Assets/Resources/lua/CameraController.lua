import("UnityEngine")

CameraController = class("CameraController", function() return LuaLu.LuaComponent.new() end)

function CameraController:Start()
  self.player = GameObject.Find("Player")
  self.offset = self.transform.position - self.player.transform.position
end

function CameraController:Update()
end

function CameraController:LateUpdate()
  self.transform.position = self.player.transform.position + self.offset
end