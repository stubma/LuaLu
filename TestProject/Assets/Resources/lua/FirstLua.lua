FirstLua = class("FirstLua")

function FirstLua:testMethod()
  print("from test method")
end

local f = FirstLua.new()
f:testMethod()
