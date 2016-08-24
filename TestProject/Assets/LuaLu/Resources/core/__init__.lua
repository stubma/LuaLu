u3d = u3d or {}

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

require("core/debug")
require("core/oop")
require("core/overload")
require("core/string")
require("core/table")