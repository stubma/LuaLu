import("UnityEngine")
import("System")

FirstLua = class("FirstLua", function() return LuaLu.LuaComponent.new() end)

function FirstLua:Start()
  local s1 = Vector3.new(1, 1, 1)
  local s2 = Vector3.new(1, 1, 1)
  if Object.Equals(s1, s2) then
    print("vector are same!!!")
  else
    print("vector are not same!!")
  end
end

function FirstLua:Update()
  local v = Vector3.new(15 * Time.deltaTime, 30 * Time.deltaTime, 45 * Time.deltaTime)
  self.transform:Rotate(v)
end