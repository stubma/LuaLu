SecondLua = class("SecondLua", function() return LuaComponent.new() end)

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
  print("hahahaha, lua side Update for SecondLua!!!!")
end