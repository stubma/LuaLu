FirstLua = class("FirstLua", function() return LuaComponent.new() end)

function FirstLua:testMethod()
  print("from test method")
end

function FirstLua.staticMethod()
  print("from firstlua static method")
end

function FirstLua:Start()
  print("hahahaha, lua side Start for first lua!!!!")
end

function FirstLua:Update()
  print("hahahaha, lua side Update for first lua!!!!")
end