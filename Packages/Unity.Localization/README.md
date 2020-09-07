# 本地化

资源本地化，多语言配置





## ﻿菜单

###### Window/General

- Localization

  打开本地化编辑器




## 如何打开

两种打开方式

- 双击 `*.lang.xml`  文件显示本地化编辑器
- 或者点击菜单 `Window/General/Localization`



## 添加本地化文件

1. 打开本地化编辑器
2. 点击 `新建` 按钮，弹出新建文件框
3. 选择文件夹 `Assets/Resources/Localization`
4. 输入[语言代码](#语言代码)，比如：zh(中文)，en(英文)
5. 点击 `保存` 按钮将创建 `*.lang.xml`文件





## 使用

1. 继承 `DefaultLocalizationValues` 实现本地化语言字符串字典

2. 初始化

   ```
   Localization.Initialize();
   ```

3. 获取本地化字符串

   ```C#
   "Hello World".Localization()
   ```

   



## 本地化编辑器

**[当前]  [选择的]  [默认]  [界面区域]  [区域]  [系统语言]**

优先级依次

- 当前

  当前运行时所使用的语言

- 选择的

  用户指定的语言，优先级最高

  ```c#
  Localization.SelectedLang
  ```

- 默认

  用户设置的默认语言

  ```
  Localization.DefaultLang
  ```

- 界面区域

  线程区域界面语言

  ```c#
  Thread.CurrentThread.CurrentUICulture
  ```

- 区域

  线程区域语言

  ```c#
  Thread.CurrentThread.CurrentCulture
  ```

- 系统语言

  ```c#
  UnityEngine.Application.systemLanguage
  ```

  



**<[基础语言代码](#语言代码)> 编辑按钮 <[当前语言代码](#语言代码)> 编辑[<[语言代码](#语言代码)>]按钮 新建按钮 *按钮**

- 基础语言代码

  基础配置文件，获取键值

- 编辑按钮

  切换到基础文件编辑

- 当前语言代码

  当前编辑的文件

- 编辑[[语言代码](#语言代码)]按钮

  切换到语言代码文件编辑

- 新建按钮

  新建配置文件

- *按钮

  跳转到当前文件

- 新名称输入框

  新增语言键值，输入后按回车添加

- 数据类型

  默认字符串类型

- 键值名称

  - 点击复制名称
  - 双击编辑名称

- 复选框

  未勾选表示继承基础值

- 菜单

  - 删除

    删除该项






## 编辑器GUI使用本地化



   ```c#
private static LocalizationValues editorLocalizationValues;
public static LocalizationValues EditorLocalizationValues
{
	get
    {
    	if (editorLocalizationValues == null)
			editorLocalizationValues = new DirectoryLocalizationValues("<(*.lang.xml)文件夹路径>"));
			return editorLocalizationValues;
	}
}
   
void OnGUI(){
    using (Localization.BeginScope(EditorBuildData.EditorLocalizationValues))
    {
        //"<Key>".Localization()        
    }
}
   
   ```

   







## 语言代码

### 格式

```
语言-区域
```

- 语言

  小写

- 区域

  可选，大写



### 语言代码表

| 语言名称 | 语言            | Unity              |
| -------- | --------------- | ------------------ |
| de       | 德语            | German             |
| en       | 英语            | English            |
| en-US    | 英语 (美国)     |                    |
| fr       | 法语            | French             |
| it       | 意大利语        | Italian            |
| ja       | 日语            | Japanese           |
| ko       | 韩语            | Korean             |
| ru       | 俄语            | Russian            |
| zh       | 中文简体        | Chinese            |
| zh-CN    | 中文简体 (中国) | ChineseSimplified  |
| zh-TW    | 中文繁体 (台湾) | ChineseTraditional |
| pt       | 葡萄牙语        | Portuguese         |
| hu       | 匈牙利语        | Hungarian          |
| es       | 西班牙语        | Spanish            |
| ...      | ...             |                    |



[语言代码缩写表]( https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes )

