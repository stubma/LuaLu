FirstLua = class("FirstLua", function() return LuaLu.LuaComponent.new() end)

function FirstLua:testMethod()
  print("from test method")
end

function FirstLua.staticMethod()
  print("from firstlua static method")
end

function FirstLua:Start()
end

function FirstLua:Update()
  self.transform:Rotate(UnityEngine.Vector3.new(15 * UnityEngine.Time.deltaTime, 30 * UnityEngine.Time.deltaTime, 45 * UnityEngine.Time.deltaTime))
end