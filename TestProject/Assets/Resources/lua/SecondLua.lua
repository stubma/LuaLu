SecondLua = class("SecondLua", function() return LuaLu.LuaComponent.new() end)

function SecondLua:Start()
  print("hahahaha, lua side Start for SecondLua!!!!")
end

function SecondLua:Update()
end