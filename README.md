# SimpleHTTPServer

> 这三天在帮别人写 MCBE 插件，需要用到 Web 端，而 ASP.NET 又太大了，不够轻量，于是自己手搓了 HTTP 服务端。
>
> 它非常简单也正好满足需求，但由于技术有限，它有着**数不清的 Bug 和不优雅的代码**，所以弃用了。现在放上 Github 用作留恋，或者未来某一天我会回头……

基于 C# 的 [HttpListener](https://learn.microsoft.com/zh-cn/dotnet/api/system.net.httplistener?view=net-7.0) 库进行开发。

### 目前支持功能

- 可响应指定目录内的 HTML/CSS/JS 和图片资源；
- 可指定路由和请求方法以及对应的处理方法；
- 可响应文本数据、Json 数据和 202、204、404 状态。