这是什么
===

这是一个Unity3D的Lua插件, 还在开发中, 基本功能已经成型了. 这同时也是我的学习项目, 本人unity新手, 所以做个实际的东西学的快些. 目前unity的lua插件已经有了slua, ulua, unilua, nlua等等, 我看了看觉得我写的会更好用些, 不吹不黑, 各位随意. lua绑定的代码主要还是参考cocos2dx-classical的实现, 针对C#做了很多修改, 另外看了一点cstolua的代码, 大致如此. 工程的名字LuaLu, 我觉得这个名字起得不错, 避免了和其它插件名字雷同, Lu本身和我的英文名也有关联, LuaLu读起来还是"撸啊撸", 技术宅男的形象跃然纸上.

What is this
===

This is a Unity3D Lua plugin, it is still in active development but basically you can run it now. It is my learning project also and I want it be the best of unity lua plugins. Welcome to use it.

如何安装
===

我是一个喜欢简单的人, 希望把东西包装的很简单, 这个安装, 很简单, 你把Assets下的LuaLu目录拷贝过来放到你的Assets下面就行了, 不要改名, 必须叫LuaLu

How to install
===

Just copy Assets/LuaLu to your project Assets folder, do NOT rename, keep the folder name "LuaLu"

如何使用
===

拷贝完后在unity中出现Lua菜单, 选择```Generate Unity Lua Binding```生成unity类的绑定, 生成的绑定脚本在Assets/Scripts/LuaBinding下, 准备工作就这么多, 剩下的就是写你的lua脚本了. <font color=red>注意绑定生成器还没有全部完成, 我现在就生成了几个主要的类用来测试, 你可以跑跑现在的TestProject</font>

How to use
===

After coping LuaLu, you should see Lua menu in unity editor, select ```Generate Unity Lua Binding``` menu item to generate unity class lua binding, and it's all. You can write your lua for unity now! <font color=red>Note: the binding generator is not fully completed, I just generate several classes for testing, you can run TestProject to see what I have done</font>

用户指南
===

1. 创建lua脚本绑定到游戏对象
---
1. 选择一个游戏对象
2. 在inspector中点击```Add Component```, 选择添加一个Lua/Lua Script组件, 这个组件的实现类是LuaComponent.cs
3. 添加了这个组件后会立刻弹出一个文件对话框, 提示你创建一个新lua文件, 请注意, lua文件必须放在Assets/Resources下
4. 完成后, project视图里会显示新创建的lua文件, 你可以开始编辑了, 可以在unity的设置里找到"LuaLu"面板, 里面可以设置lua文件的默认编辑器. 目前有三个选项: 系统缺省, ZeroBraneStudio, 自定义. ZeroBraneStudio可以在github里搜. 之所以做这个设置是考虑以后给不同的编辑器添加代码提示, 首先会给ZeroBraneStudio做.
5. LuaComponent会把MonoBehaviour的事件都导入到绑定的lua里, 这样你就可以完全用lua脚本操作游戏对象了. 具体看看TestProject就明白了, LuaComponent一般情况下不需要去修改, 它只是起一个初始化和桥接作用.

Guide
===

2. Create lua and attach to game object
---

1. select a game object
2. click ```Add Component``` button in inspector, select to add "Lua/Lua Script" component. The component implemention is LuaComponent.cs
3. it will pops up a file dialog, choose a directory to save your new lua file, NOTE: the lua file must be saved in Assets/Resources folder
4. now you can edit lua file, there is a LuaLu section in unity preference, you can set favorite lua editor here, currently it only has three options. You can search ZeroBraneStudio in github. I will add code assistant for ZeroBraneStudio first.
5. LuaComponent will redirect MonoBehaviour messages to lua side so you can operate game object in lua now. Take a glance at TestProject for a quick understanding. LuaComponent only does some initialization and bridge work so you hardly need to modify it.