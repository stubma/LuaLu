FirstLua = class("FirstLua", function() return LuaLu.LuaComponent.new() end)

function FirstLua:testMethod()
  print("from test method")
end

function FirstLua.staticMethod()
  print("from firstlua static method")
end

function FirstLua:Start()
  local s1 = UnityEngine.Vector3.new(1, 1, 1)
  local s2 = UnityEngine.Vector3.new(1, 1, 1)
  if System.Object.Equals(s1, s2) then
    print("vector are same!!!")
  else
    print("vector are not same!!")
  end
end

function FirstLua:Update()
  --collectgarbage()
  local v = UnityEngine.Vector3.new(15 * UnityEngine.Time.deltaTime, 30 * UnityEngine.Time.deltaTime, 45 * UnityEngine.Time.deltaTime)
  self.transform:Rotate(v)
end