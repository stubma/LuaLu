这是什么
===

这是一个Unity3D的Lua插件, 还在开发中, 基本功能已经成型了. 这同时也是我的学习项目, 本人unity新手, 所以做个实际的东西学的快些. 目前unity的lua插件已经有了slua, ulua, unilua, nlua等等, 我看了看觉得我写的会更好用些, 不吹不黑, 各位随意. lua绑定的代码主要还是参考cocos2dx-classical的实现, 针对C#做了很多修改, 另外看了一点cstolua的代码, 大致如此. 工程的名字LuaLu, 我觉得这个名字起得不错, 避免了和其它插件名字雷同, Lu本身和我的英文名也有关联, LuaLu读起来还是"撸啊撸", 技术宅男的形象跃然纸上.

What is this
===

This is a Unity3D Lua plugin, it is still in active development but basically you can run it now. It is my learning project also and I want it be the best of unity lua plugins. Welcome to use it.

如何安装
===

把Assets下的LuaLu目录拷贝过来放到你的Assets下面就行了, 不要改名. 不过如果你非要改, 记得把LuaConst.cs里的```CORE_LUA_PREFIX```改一下就行.

How to install
===

Just copy Assets/LuaLu to your project Assets folder, do NOT rename, keep the folder name "LuaLu". Buf if you really want to rename it, remember change ```CORE_LUA_PREFIX``` in LuaConst.cs

使用指南
===
初始设置
---
LuaLu目录拷贝完后应该在unity中看到Lua菜单, 选择```Generate Unity Lua Binding```生成unity类的绑定, 生成的绑定脚本在Assets/Scripts/LuaBinding下. 目前生成器注释掉了部分代码以避免生成太多的类, 光是生成System, UnityEngine, UnityEngine.UI的类就多达600个文件, 会拖慢编译速度, 所以这块会继续完善, 让绑定生成器更加定制化. 目前就是生成了测试工程需要用到的类, 十多个而已.
创建lua脚本绑定到游戏对象
---
1. 选择一个游戏对象
2. 在inspector中点击```Add Component```, 选择添加一个Lua/Lua Script组件, 这个组件的实现类是LuaComponent.cs
3. 添加了这个组件后会立刻弹出一个文件对话框, 提示你创建一个新lua文件, 请注意, lua文件必须放在Assets/Resources下
4. 完成后, project视图里会显示新创建的lua文件, 你可以开始编辑了, 可以在unity的设置里找到"LuaLu"面板, 里面可以设置lua文件的默认编辑器. 之所以做这个设置是考虑以后给不同的编辑器添加代码提示, 首先会给Sublime Text做.
5. LuaComponent会把MonoBehaviour的事件都导入到绑定的lua里, 这样你就可以完全用lua脚本操作游戏对象了. 具体看看TestProject就明白了, LuaComponent一般情况下不需要去修改, 它只是起一个初始化和桥接作用.

在lua中访问类的方法和属性
---
例如有C#类:

```
public class A {
	public int a;
	public static int b;
	public A() { ... }
	public int ma() { ... }
	public static int mb(int p1) { ... }
	public static int mb(int p2, int p2) { ... }
}
```
则在lua中访问, 基本和C#相同:

```
local inst = A.new()
inst.a = ...
A.b = ...
inst:ma()
A.mb(p1)
A.mb(p1, p2)
```
NOTE:

* 构造函数在lua端统一为new方法
* 重载的方法在lua端只有一个, 通过传入参数的不同调用不同的C#方法
* 对于静态方法而言, 在lua端你用inst.mb()当然也可以调用, 不过为了代码规范, 还是A.mb()好看些

在lua中定义类
---
在lua中可以继承C#中已经生成绑定的类, 假如你有上面的类A, 则

```
B = class("B", A)
local b = B.new()
function B:ctor()
	-- invoked when new
end

C = class("C")
local c = C.new()
function C:ctor()
	-- invoked when new
end

D = class("D", C)
local d = D.new()
function D:ctor()
	-- invoked when new, after C's ctor
end
```
NOTE:

* 上面定义了三个类, B是从C#的A类派生的, 而C是一个纯lua类, 实际也就是个lua的table, D则从C派生. 生成实例的时候都用new方法
* new会调用ctor方法, 可以在ctor中做初始化
* D类在new时, 会按照继承关系先调用C的ctor, 再调用D的ctor
* B初始化只有B的ctor被调用, 因为A是绑定生成的类, 没有ctor

在lua中使用Delegate
---
例如有C#类:

```
public class A {
	public delegate string TestDelegate(int i);
	public event TestDelegate TestEvent;
	public static event TestDelegate TestStaticEvent;
	public void test(TesDelegate d) { ... }
}
```
在lua端, 可以使用delegate方法包装某个函数传递给C#端

```
B = class("B")

function B:method(i)
	return "string"
end

function B.staticMethod(i)
	return "string"
end

function B:test()
	local a = A.new()
	a:addTestEvent(delegate(self, B.method))
	a:addTestEvent(delegate(B.staticMethod))
	A.addTestStaticEvent(delegate(self, B.method))
	A.addTestStaticEvent(delegate(B.staticMethod))
	a:removeTestEvent(delegate(self, B.method))
	A.removeTestStaticEvent(delegate(B.staticMethod))
	a:test(delegate(self, B.method))
end
```
NOTE:

* 对于每一个事件, 假设事件名为E, lua端的A类中会有相应的addE/removeE方法, 如果事件是静态的, 则addE/removeE也是静态的, 虽然你也可以用a.add/removeTestStaticEvent来访问, 但是为了代码规范嘛, 嗯
* 对于使用了Delegate作为参数的方法, 一样用delegate函数包装即可
* delegate方法可以有1或者2个参数, 分别为target和handler, 如果不提供target, 则认为你是要用一个静态方法作为Delegate

Tutorial
===

Initial setup
---
After coping LuaLu, you should see Lua menu in unity editor, select ```Generate Unity Lua Binding``` menu item to generate unity class lua binding, and it's all. You can write your lua for unity now! The generator only produces classes required by test project but you can uncomment some code to generate more. However, there are too many classes and it slows down compilation. So, I will do more work to make binding generator more customized.

Create lua and attach to game object
---
1. select a game object
2. click ```Add Component``` button in inspector, select to add "Lua/Lua Script" component. The component implemention is LuaComponent.cs
3. it will pops up a file dialog, choose a directory to save your new lua file, NOTE: the lua file must be saved in Assets/Resources folder
4. now you can edit lua file, there is a LuaLu section in unity preference, you can set favorite lua editor here, currently it only has three options. I will add code assistant for Sublime Text first.
5. LuaComponent will redirect MonoBehaviour messages to lua side so you can operate game object in lua now. Take a glance at TestProject for a quick understanding. 