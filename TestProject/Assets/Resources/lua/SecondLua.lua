SecondLua = class("SecondLua", function() return LuaLu.LuaComponent.new() end)

function SecondLua:testMethod()
  print("from SecondLua test method")
end

function SecondLua.staticMethod()
  print("from SecondLua static method")
end

function SecondLua:Start()
  print("hahahaha, lua side Start for SecondLua!!!!")
end

function SecondLua:Update()
end