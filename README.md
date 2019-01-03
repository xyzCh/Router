### Router 
`a HttpModule`

---
`2019/1/3 update`  `session support`  
asp.NET在PostResolveRequestCache之后的AcquireRequestState事件中才获取当前请求的session，但router在PostResolveRequestCache事件中拦截请求进行处理，跳过了后边事件的执行，因此在用户代码中使用session时会出现问题。本着不大量改动现有用户代码又可以使用session,另一方面由于技术和精力有限,作出以下更新（应该有更好的办法）:    
+ router的实现中，由原来的PostResolveRequestCache事件中直接拦截然请求后进行反射处理改为:在PostResolveRequestCache事件中拦截请求，最后调用HttpContext.RemapHandler(ForwardHttpHandler)，利用自定义的ForWardHttpHandler做跳转，最终在ForWardHttpHandler中进行请求的处理。
+ web.config中重新加载Session模块(用户代码要做出的改变)
  ```html
  <modules>
    ...
    <add name="*" type="Router.FilterModule" />
    <remove name="Session"/>
    <add name="Session" type="System.Web.SessionState.SessionStateModule"/> <!--add-->
  </modules>
  ```

-----
`v0.11`  
new:  
加入了RoutingMethod特性,实现直接传递参数到方法  
**eg.**
```C#
using Routing.Attributes;
...
[RoutingMethod]
public string getPeople(string id){
  ...
}

[RoutringMethod(UseHttpGet=false)]
public void setPeople(People p){
  ...
}
```
*the Http request like following:  
getPeople: /....xx.router?action=getPeople&id=123  
setPeople: /....xx.router?action=setPeople,FormData:{p:"{"id":123,"name":"test"}"}*

---
`v0.1`
>经常写.ashx,每天都是switch case,于是就有了Router,用法如下:  
1. 引用Router.dll  
2. web.config中添加httpModules  
```html
<!--集成模式-->
<system.webServer>
  ...
  <modules>
    ...
    <add name="*" type="Router.FilterModule" />
  </modules>
</system.webServer>

<!--经典模式-->
<system.web>
  ...
  <httpModules>
    ...
    <add name="*" type="Router.FilterModule" />
  </httpModules>
</system.web>
```
3. web.config中添加appSettings (可选)
```html
<appSettings>
  ...
  <add key="actionParam" value="method" />
</appSettings>
```
若设置此项且value不为空或空白符，则请求url应为:  
`.../xx.router?appSettings[actionParam]=methodname&param1=xx&....`  
*`如上actionParam设置，请求url应为：.../xx.router?method=methodname&param1=xx&....`*  
不设置此项，或此项value为空或空白符，则appSettings[actionParam]默认为`'action'`,相应的请求url为:  
`.../xx.router?action=methodname&param1=xx&....`  

*`注意：如果请求的methodname为空，即不传递参数appSettings[actionParam]或传递的参数值为空，则默认执行ProcessRequest方法`*    
4. 接下来就可以愉快的使用了

**eg.**  
> 被请求的/test/a.ashx代码如下
```C#
...
public void ProcessRequest (HttpContext context) {
        ...
        context.Response.Write("default method");
}
    
public void hello(HttpContext context) {
        ...
        context.Response.Write("hello world!");
}
...
```
> `请求url: /test/a.router?action=hello  //output 'hello world'`  
`请求url: /test/a.router   //默认执行ProcessRequest方法,output 'default method'`
