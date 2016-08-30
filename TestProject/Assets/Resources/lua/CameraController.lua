import("UnityEngine")

CameraController = class("CameraController", LuaLu.LuaComponent)

function  CameraController:Awake()
    self.player = GameObject.Find("Player")
end

function CameraController:Start()
    self.offset = self.transform.position - self.player.transform.position
end

function CameraController:LateUpdate()
  self.transform.position = self.player.transform.position + self.offset
end