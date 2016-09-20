import("LuaLu")

TestCaseRunner = class("TestCaseRunner")

function TestCaseRunner:ctor()
	self.m_success = false
end

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
	return t:TestListI({ 100, 200, 300 })
end

function TestCaseRunner:testcase4(t)
	return t:TestListValueTruncate({ 0x12345678, 0x12345678 }, { 0x12345678, 0x12345678 })
end

function TestCaseRunner:testcase5(t)
	return t:TestDictionaryII({ [100] = 101, [200] = 202, [300] = 303 })
end

function TestCaseRunner:testcase6(t)
	return t:TestDictionarySS({ hello = "world", test = "case" })
end

function TestCaseRunner:testcase7(t)
	self.m_success = false
	return t:TestDelegateIZ(delegate(self, TestCaseRunner.delegateIZ))
end

function TestCaseRunner:delegateIZ(num)
	self.m_success = num == 0x12345678
	return self.m_success
end

function TestCaseRunner:testcase8(t)
	self.m_success = false
	t:TestActionS(delegate(self, TestCaseRunner.delegateS))
	return self.m_success
end

function TestCaseRunner:delegateS(str)
	self.m_success = str == "hello"
end

function TestCaseRunner:testcase9(t)
	return TestClass.TestStaticMethodIZ(0x12345678)
end

function TestCaseRunner:testcase10(t)
	if t.FieldI == 0x12345678 then
		t.FieldI = 100
		if t.FieldI == 100 then
			return true
		end
	end
	return false
end

function TestCaseRunner:testcase11(t)
	if t.PropertyS == "hello" then
		t.PropertyS = "world"
		if t.PropertyS == "world" then
			return true
		end
	end
	return false
end

function TestCaseRunner:testcase12(t)
	return t:TestGenericT("System.String", "哈哈哈")
end