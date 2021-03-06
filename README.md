If you are an English speaker, please view <a href="#what-is-this">English Version</a>

这是什么
===

这是一个Unity3D的Lua插件, 是我用来学习Unity的小项目, 当然也还是有一定的实用性的. 目前unity的lua插件已经有了slua, ulua, unilua, nlua等等, 我看了看觉得我写的会更好用些, 不吹不黑, 各位随意. lua绑定的代码主要还是参考cocos2dx-classical的实现, 针对C#做了很多修改, 另外看了一点cstolua的代码, 大致如此. 工程的名字LuaLu, 我觉得这个名字起得不错, 避免了和其它插件名字雷同, Lu本身和我的英文名也有关联, LuaLu读起来还是"撸啊撸", 技术宅男的形象跃然纸上.

模板方法的绑定还没做完, 没啥动力了, 懒得继续, 就这样吧. 基于unity 5.4.0f3 Personal, OSX, iOS已测试通过.

如何安装
===

把Assets下的LuaLu目录拷贝过来放到你的Assets下面就行了, 不要改名. 不过如果你非要改, 记得把LuaConst.cs里的```CORE_LUA_PREFIX```改一下就行.

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

使用import简化代码
---
由于C#的类都有比较长的命名空间, 在lua端可以用import简化类名长度, 例如:

```
import("UnityEngine")
local obj = GameObject.new("name")
local obj2 = UnityEngine.GameObject.new("name") -- or you can still type full name
```
NOTE:

* 如果两个命名空间中有同名的类, 则后import的会覆盖前面的, 例如:

```
import "UnityEngine"
import "System"
local obj = Object.new() -- it is a System.Object, not UnityEngine.Object
```
使用typeof得到System.Type
---
可以使用typeof得到类型对象, 例如

```
imoprt "UnityEngine"
local t = typeof(GameObject)
print(t.FullName)
```
NOTE:

* typeof只是一个自定义的函数用来获得类型对象, 不要和lua自带的type函数搞混了. import, typeof, delegate都定义在```LuaLu/Resources/core/u3d.lua```中

使用操作符重载
---
有些值类型, 比如Vector2/3/4, 这些类都重载了一些操作符, 这些操作符重载在lua端也可以使用, 例如:

```
import "UnityEngine"
local v1 = Vector3.new(1, 1, 1)
local v2 = Vector3.new(2, 3, 4)
local v3 = v1 + v2
local v4 = v1 * 2.4
local v5 = v2 / 2
```
NOTE:

* 由于lua支持的操作符重载数量有限, 所以某些重载就不能使用了. C#能在lua端使用的重载只有```+, -, *, /, %, <, <=, ==```
* 对于```>, >=, !=```, lua是通过```<, <=, ==```模拟的. 所以, 如果C#端重载了```==```和```!=```, 但是实现方式不一样那就有问题了, 不过这种情况应该比较少见.
* 对于```*```和```/```, 注意类型必须在前面, 也就说不要写成```local v4 = 2.4 * v1```

关于LuaComponent和对应的lua脚本
---
当你为一个GameObject添加了lua脚本后, 其实际上是为GameObject附加了一个LuaComponent, 而对应的lua文件初始具有如下的内容:

```
import("UnityEngine")

YourClassName = class("YourClassName", LuaLu.LuaComponent)

function YourClassName:ctor()
end

function YourClassName:Start()
end

function YourClassName:Update()
end
```
NOTE:

* lua文件必须位于```Assets/Resources```目录下, 但是实际上这些lua文件不会被打包到最终的app中, 因为unity并不支持lua扩展名. 所以每当你对lua文件作出修改时, 一个对应的拷贝会生成在```Assets/Generated/Resources```下, 并追加了```.bytes```扩展名
* lua文件名必须和类名一致, 如果lua文件名叫Test.lua, 则里面的类必须叫Test. 如果你修改了lua文件名, LuaComponent会自动修改保持对lua文件的引用, 但是lua文件里面的类名目前还不能自动变化, 需要你手动修改一下
* ```LuaComponent```把```MonoBehavior中```的消息全部导入到了lua端, 如果你需要处理什么消息, 在lua文件中添加对应的方法即可
* ```LuaComponent```把自身实例与lua端进行了绑定, 所以你不需要对这个lua类调用```new```方法, 在```LuaComponent```的```Awake```方法中, 它调用了```BindInstanceToLuaClass```把自己和lua端连接了起来, 就好像你在lua端new过了一样, 在```BindInstanceToLuaClass```调用时, lua端的```ctor```被调用

What is this
===

This is a Unity3D Lua plugin, it is a JUST learning project but it can be used.

Template method binding is not done yet but I lost motivation, this is it. Tested on Unity 5.4.0f3 Personal with OSX and iOS

How to install
===

Just copy Assets/LuaLu to your project Assets folder, do NOT rename, keep the folder name "LuaLu". Buf if you really want to rename it, remember change ```CORE_LUA_PREFIX``` in LuaConst.cs
Tutorial
===

Initial setup
---
After coping LuaLu, you should see Lua menu in unity editor, select ```Generate Unity Lua Binding``` menu item to generate unity class lua binding, and it's all. You can write your lua for unity now! The generator only produces classes required by test project but you can uncomment some code to generate more. However, there are too many classes and it slows down compilation. So, I will do more work to make binding generator more customized.

Create lua and attach to game object
---
1. select a game object
2. click ```Add Component``` button in inspector, select to add "Lua/Lua Script" component. The component implemention is LuaComponent.cs
3. it will pops up a file dialog, choose a directory to save your new lua file, NOTE: the lua file must be saved in ```Assets/Resources``` folder
4. now you can edit lua file, there is a LuaLu section in unity preference, you can set favorite lua editor here, currently it only has three options. I will add code assistant for Sublime Text first.
5. ```LuaComponent``` will redirect ```MonoBehaviour``` messages to lua side so you can operate game object in lua now. Take a glance at TestProject for a quick understanding. 

Access properties, fields and methods in lua
---
If there is C# class A:

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
Syntax in lua:

```
local inst = A.new()
inst.a = ...
A.b = ...
inst:ma()
A.mb(p1)
A.mb(p1, p2)
```
NOTE:

* Constructor is named as ```new``` in lua side
* For overload methods, lua binding will check parameter count and types to decide which C# method to be called
* For static methods, ```inst.mb()``` also works. But for good code style, I suggest ```A.mb()```

Define class in lua
---
You can inherit C# class in lua side, if there is C# class A, then:

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

* Three classes defined above. B derives from C# class A but C is a pure lua class(in fact, a table), and D derives from C so it is also a pure lua class. To create instance, use ```new``` no matter where they derive from
* ```new``` will invoke ```ctor```, and you can do initialization in ```ctor```
* ```ctor``` will be invoked by inheritance order, super type is first, then derived types
* Because A is a C# class, it doesn' have ```ctor``` so only B's ```ctor``` is called

Use delegate in lua
---
If there is C# class A:

```
public class A {
	public delegate string TestDelegate(int i);
	public event TestDelegate TestEvent;
	public static event TestDelegate TestStaticEvent;
	public void test(TesDelegate d) { ... }
}
```
You can use ```delegate``` function in lua side:

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

* For any event ```E``` defined in C# side, there is an ```addE``` and a ```removeE``` corresponded in lua side. If event is static then these two functions are also static
* For any parameter whose type is C# delegate, use ```delegate``` to wrap lua function
* ```delegate``` can have one or two parameters which means ```target``` and ```handler```. If ```target``` is missing, the ```handler``` will be treated as a static function

Use import to simplify code
---
It may be tedious to write full name of C# class in lua side, so you can use ```import``` to simplify code, such as:

```
import("UnityEngine")
local obj = GameObject.new("name")
local obj2 = UnityEngine.GameObject.new("name") -- or you can still type full name
```
NOTE:

* If name conflicts in two imports, the later one will take precedence. For example:

```
import "UnityEngine"
import "System"
local obj = Object.new() -- it is a System.Object, not UnityEngine.Object
```
Use typeof to get System.Type
---
If you want to get a System.Type in lua side, you can use ```typeof```:

```
imoprt "UnityEngine"
local t = typeof(GameObject)
print(t.FullName)
```
NOTE:

* ```typeof``` is just a custom function defined ```LuaLu/Resources/core/u3d.lua```, don't confuse it with lua ```type``` function

Operator overload
---
If a C# class, such as Vector3, overloads operators, some can be supported in lua side, for example:

```
import "UnityEngine"
local v1 = Vector3.new(1, 1, 1)
local v2 = Vector3.new(2, 3, 4)
local v3 = v1 + v2
local v4 = v1 * 2.4
local v5 = v2 / 2
```
NOTE:

* lua only supports few of them, so not all C# operator overloads can be used in lua side. To be precisely, lua supports ```+, -, *, /, %, <, <=, ==```
* For ```>, >=, !=```, lua uses ```<, <=, ==``` to simulate them. So, if C# overloads ```==``` and ```!=``` but their logic is different, then problem may occurs. However it is not unusual, I think.
* For ```*``` and ```/```, place class ahead, i.e., ```local v4 = 2.4 * v1``` won't work

About LuaComponent and lua script counterpart
---
When you attach a lua script to a GameObject, you do attach a ```LuaComponent``` actually. Every ```LuaComponent``` has a lua script bound which looks like below:

```
import("UnityEngine")

YourClassName = class("YourClassName", LuaLu.LuaComponent)

function YourClassName:ctor()
end

function YourClassName:Start()
end

function YourClassName:Update()
end
```
NOTE:

* lua file must be saved under ```Assets/Resources```, but it will not be packaged in final app because unity doesn't support lua extension. So whenever you modified a lua file, a copy will be saved in ```Assets/Generated/Resources``` with ```.bytes``` extension.
* The lua class name must be same as lua file name, that means you should have a "Test" class in Test.lua. If you rename lua file, LuaComponent will automatically re-bound it but the class name should be modified manually. We don't support automatic refactoring yet.
* LuaComponent redirects all MonoBehavior's messages to lua side. Adding related functions in lua side if you want to process them.
* LuaComponent instance binds itself to lua side. The binding is done in LuaComponent's ```Awake``` method, it calls ```BindInstanceToLuaClass``` to simulate a ```new``` operation so that ```ctor``` is invoked like a normal ```new``` function call. After binding the lua side is totally bridged with C# side and you never need call ```new``` for LuaComponent's lua class. It looks like unity supports lua intrinsically, works like magic.
