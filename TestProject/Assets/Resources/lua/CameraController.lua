import("UnityEngine")
require("test/TestCaseRunner")

CameraController = class("CameraController", LuaLu.LuaComponent)

function  CameraController:Awake()
    self.player = GameObject.Find("Player")
end

function CameraController:Start()
    self.offset = self.transform.position - self.player.transform.position

    -- uncomment below if don't want to run test case
    local tc = TestCaseRunner.new()
    tc:run()
end

function CameraController:LateUpdate()
  self.transform.position = self.player.transform.position + self.offset
end