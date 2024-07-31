### 简介
DialogPro是为**超长剧本**（如GalGame）设计的**易于书写和修改**的剧本书写格式。
你可以像写小说一样在**纯文本**的环境下仅通过几个符号完成分支剧情、选择肢、演出效果、富文本的配置。
对于程序层面可以灵活拓展各种功能，来实现各种特殊需求。
---
### 对话语句
> speaker&nbsp;&nbsp;&nbsp;&nbsp;
> <span style="color:#DDFFFF">#</span>&emsp;
> say&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
> <span style="color:#DDFFFF"><></span>&emsp;&emsp;
> some&nbsp;thing&nbsp;&nbsp;
> <span style="color:#DDFFFF">< color = blue ></span>
> <br>
> <span style="color:#999999">
> 讲话名称&nbsp;&nbsp;
> <b>#</b>&nbsp;&nbsp;&nbsp;
> 内容&nbsp;&nbsp;&nbsp;&nbsp;
> 注解(空)&emsp;&nbsp;
> 内容&nbsp;&emsp;&emsp;&emsp;&emsp;
> 注解 
> </span>
- 注解的目标为上一个注解到当前的注解之间的文段，注解是**非必要**的可以灵活使用
- 注解键值对用 | 隔开 eg:  `< color = red | effect = shake | stop =3.5f >`
- 讲话名称也可以被注解
---
### 赋值语句 
> <span style="color:#DDFFFF">@val</span>
> &nbsp;=&nbsp;
> <span style="color:#DDFFFF">@num</span>
> &nbsp;/&nbsp;(&nbsp;20&nbsp;+&nbsp;
> <span style="color:#DDFFFF">*m</span>
> &nbsp;)&nbsp;+&nbsp;1
- <span style="color:#DDFFFF">@val</span> :全局变量val
- <span style="color:#DDFFFF">*num</span> :局部变量num (当前运行的剧本)
- 运算式的空格**可重复不可省略**
---
### 标签语句（条件分支）
> <span style="color:#EE99AA">{</span>
> <span style="color:#DDFFFF"> *num == 2 </span>
> <span style="color:#EE99AA">}</span>
> &emsp;<span style="color:#999999">(判定条件)</span><br>
> <span style="color:#EE99AA">{</span>
> &emsp;<span style="color:#999999">(单独一行，表示条件分支的起点)</span><br>
> &emsp;<b>......</b>
> &emsp;<span style="color:#999999">(分支内容)</span><br>
> <span style="color:#EE99AA">}
> &emsp;<span style="color:#999999">(单独一行，表示条件分支的终点)</span></span>
- 标签语句的第一行表示其判定条件，如果其值**为true或者!=0**将会执行分支，否则跳过
---
### 标签语句（选项分支）
> <span style="color:#EE99AA">{</span>
> <span style="color:#DDFFFF"> *num == 2 </span>
> <span style="color:#EE99AA">}</span>
> <span style="color:#EE99AA">(</span>
>  Label A 
> <span style="color:#EE99AA">)</span>
> &emsp;<span style="color:#999999">(判定条件,选项标签内容)</span><br>
> <span style="color:#EE99AA">{</span>
> &emsp;<span style="color:#999999">(单独一行，表示条件分支的起点)</span></span><br>
> &emsp;<b>......</b>
> &emsp;<span style="color:#999999">(分支内容)</span></span><br>
> <span style="color:#EE99AA">}</span>
> &emsp;<span style="color:#999999">(单独一行，表示条件分支的终点)</span></span><br>
- 判定条件可以为空:`{}`表示永真
- 选项显示内容可以被注解,注意用**英文小括号**
- **连续出现**的选项将被视为**一组选项**
---
### 调用语句
> <span style="color:#DDFFFF">===</span>
> &nbsp;&nbsp;&nbsp;
> fun_name&nbsp;&nbsp;&nbsp;&nbsp;
> <span style="color:#EE99AA">|</span>
> &nbsp;&nbsp;
> name1
> <span style="color:#EE99AA"> = </span>
> value1&nbsp;&nbsp;
> <span style="color:#EE99AA">|</span>
> &nbsp;&nbsp;
> name2
> <span style="color:#EE99AA"> = </span>
> value2
> <br>
> <span style="color:#999999">
> <b>===</b>&nbsp;&nbsp;&nbsp;
> 调用方法名&nbsp;&nbsp;&nbsp;
> <b>|</b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
> 名称 = 参数&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
> <b>|</b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
> 名称 = 参数
> </span>
- 参数键值可以省略
---
### 包含语句
> <span style="color:#DDFFFF">#++</span>&nbsp;
> file_name
> &emsp;<span style="color:#999999">(文件相对路径)</span>
- 将读取包含路径下的`xxx.dh.txt`文件
- 包含的文件只允许存在宏定义语句
---
### 宏定义语句
> <span style="color:#DDFFFF">#==</span>
> &nbsp;&nbsp;&nbsp;
> <span style="color:#AADDFF">fun_name</span>&nbsp;
> <span style="color:#EE99AA">{</span>
> <b><span style="color:#AADDFF">......</span></b>
> <span style="color:#EE99AA">}</span>
> <br>
> <span style="color:#999999">
> <b>#==</b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
> 宏名称&nbsp;&nbsp;&nbsp;&nbsp;
> <b>{</b>定义内容<b>}</b>
> </span>
- `#<>` 用于定义注解
- `#==` 用于定义调用语句
- `#{}` 用于定义条件
eg: 
> <span style="color:#DDFFFF">#<></span>&nbsp;
> <span style="color:#AADDFF">red4.5</span>&nbsp;
> <span style="color:#EE99AA">{</span>
> <span style="color:#AADDFF">StartRichText = 1 | color = red | size = 4.5</span>
> <span style="color:#EE99AA">}</span><br>
> <span style="color:#DDFFFF">#<></span>&nbsp;
> <span style="color:#AADDFF">/</span>&nbsp;
> <span style="color:#EE99AA">{</span>
> <span style="color:#AADDFF">EndRichText = 1</span>
> <span style="color:#EE99AA">}</span>
> <br>
> speaker # The world is 
> <span style="color:#AADDFF"><red4.5></span>
> dead<span style="color:#AADDFF"></></span>!