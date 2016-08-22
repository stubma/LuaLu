FirstLua = class("FirstLua", function() return LuaLu.LuaComponent.new() end)

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
end