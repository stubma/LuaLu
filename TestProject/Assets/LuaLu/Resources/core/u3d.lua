u3d = u3d or {}

-- log
function u3d.log(...)
    print(string.format(...))
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

-- get type
function typeof(t)
	-- get name
	local n = t
	if type(t) ~= "string" then
		n = tolua.type(t)
	end

	-- add assembly
	if string.startswith(n, "UnityEngine.") then
		local dotIndex,_ = string.find(string.reverse(n), ".", 1, true)
		local ns = string.sub(n, 1, -dotIndex - 1)
		n = n .. ", " .. ns
	end

	-- get type
	return System.Type.GetType(n)
end