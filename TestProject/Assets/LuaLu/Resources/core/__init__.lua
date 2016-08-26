-- avoid memory leak
collectgarbage("setpause", 100)
collectgarbage("setstepmul", 5000)
    
-- load core lib
require("core/debug")
require("core/oop")
require("core/overload")
require("core/string")
require("core/table")
require("core/u3d")

-- for lua error traceback
function __G__TRACKBACK__(msg)
    u3d.log("----------------------------------------")
    u3d.log("LUA ERROR: " .. tostring(msg))
    u3d.log(debug.traceback())
    u3d.log("----------------------------------------")
end
