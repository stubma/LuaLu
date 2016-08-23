FirstLua = class("FirstLua", function() return LuaLu.LuaComponent.new() end)

function FirstLua:testMethod()
  print("from test method")
end

function FirstLua.staticMethod()
  print("from firstlua static method")
end

function FirstLua:Start()
  print("hahahaha, lua side Start for first lua!!!!")
  print("firstlua component tag is " .. self.tag)
  if self:CompareTag("PickUp") then
    print("this component tag is pickup!! 哈哈")
  else
    print("this compent tag is not!!!")
  end
end

function FirstLua:Update()
end