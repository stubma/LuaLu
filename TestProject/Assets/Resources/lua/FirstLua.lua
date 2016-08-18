FirstLua = class("FirstLua")

function FirstLua:testMethod()
  print("from test method")
end

function FirstLua.staticMethod()
  print("from firstlua static method")
end