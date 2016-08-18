u3d = u3d or {}

-- cc.log
function u3d.log(...)
    print(string.format(...))
end

-- for CCLuaEngine traceback
function __G__TRACKBACK__(msg)
    cc.log("----------------------------------------")
    cc.log("LUA ERROR: " .. tostring(msg))
    cc.log(debug.traceback())
    cc.log("----------------------------------------")
end

-- load lua file under a folder, include subfolders
function loadLua(name)
	-- TODO
end

-- load all core lua
loadLua("core")

require("core/debug")
require("core/oop")
require("core/overload")
require("core/string")
require("core/table")