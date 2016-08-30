import("UnityEngine")

CameraController = class("CameraController", function() return LuaLu.LuaComponent.new() end)

function  CameraController:Awake()
    self.player = GameObject.Find("Player")
end

function CameraController:Start()
    self.offset = self.transform.position - self.player.transform.position

    self.addTestEvent(delegate(CameraController.mydel1))
    self.addTestEvent(delegate(self, CameraController.mydel2))
    self:test()
    self.removeTestEvent(delegate(self, CameraController.mydel2))
    self:test()
end

function CameraController:LateUpdate()
  self.transform.position = self.player.transform.position + self.offset
end

function CameraController.mydel1(i)
    print("hahah, static delegate 1 " .. i)
end

function CameraController:mydel2(i)
    print("hahah, delegate 2 " .. i)
end