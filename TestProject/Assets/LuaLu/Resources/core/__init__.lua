u3d = u3d or {}

-- avoid memory leak
collectgarbage("setpause", 100)
collectgarbage("setstepmul", 5000)
    
-- load core lib
require("core/debug")
require("core/oop")
require("core/overload")
require("core/string")
require("core/table")

-- cc.log
function u3d.log(...)
    print(string.format(...))
end

-- for CCLuaEngine traceback
function __G__TRACKBACK__(msg)
    u3d.log("----------------------------------------")
    u3d.log("LUA ERROR: " .. tostring(msg))
    u3d.log(debug.traceback())
    u3d.log("----------------------------------------")
end

-- import so that you can omit namespace when use a class
function import(pkg)
	-- find table to be imported
	local nsList = string.split(pkg, ".")
	local t = _G
	for _,ns in ipairs(nsList) do
		t = t[ns]
		if t == nil then
			u3d.log("can not find package to import: " .. pkg)
			return
		end
	end
  
  -- if has old env, copy it
  local e = {}
  local old = getfenv(2)
  if old ~= nil then
    for k,v in pairs(old) do
      e[k] = v
    end
  end
  
  -- copy the package imported
  for k,v in pairs(t) do
    if e[k] ~= nil then
      u3d.log("env key '" .. k .. "' exists, will be overrided by new import")
    end
    e[k] = v
  end

	-- set env
	setmetatable(e, { __index = _G, __newindex = _G })
	setfenv(2, e)
end