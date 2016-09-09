import("LuaLu")

TestCaseRunner = class("TestCaseRunner")

function TestCaseRunner:run()
	local t = TestClass.new()
	local success = 0
	local fail = 0
	local i = 1
	fn = "testcase" .. i
	f = self[fn]
	while f ~= nil do
		if not f(self, t) then
			fail = fail + 1
			print(fn .. " failed")
		else
			success = success + 1
		end
		i = i + 1
		fn = "testcase" .. i
		f = self[fn]
	end
	print("test case done, failed " .. fail .. ", success " .. success)
end

function TestCaseRunner:testcase1(t)
	-- test primitive types
	return t:TestPrimitiveTypes(100, 100, 100, true, 100, 100, 100, 100, 100, 100, 100, 100, 100)
end

function TestCaseRunner:testcase2(t)
	return t:TestValueTruncate(0x12345678, 0x12345678, 0x12345678, 0x12345678, 0x12345678)
end

function TestCaseRunner:testcase3(t)
	return t:TestListInt({ 100, 200, 300 })
end

function TestCaseRunner:testcase4(t)
	return t:TestListValueTruncate({ 0x12345678, 0x12345678 }, { 0x12345678, 0x12345678 })
end

function TestCaseRunner:testcase5(t)
	return t:TestDictionaryIntInt({ [100] = 101, [200] = 202, [300] = 303 })
end