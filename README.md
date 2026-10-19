# MatLag

古有 MatLab，今有 MatLag。每一次运算都值得等待。

## 浏览器演示

需要 .NET 10 SDK。在 Rider 中选择 **MatLag.Web** 运行，或在仓库根目录执行：

```sh
dotnet run --project Web/MatLag.Web.csproj
```

打开 <http://localhost:5180>。

- 直接在网格中填写矩阵，支持负数、小数、科学计数法，单元格留空按 0 处理。
- 可从 Excel 或空格分隔的多行文本粘贴到任意单元格，自动扩展网格。
- 「开始演示」支持最多 10×10 的矩阵，可逐步查看或自动播放；每步处理一个主元及其下方元素。
- 「直接完成」支持最多 32×32，只显示原矩阵与最终结果。
- 结果数字支持调整有效位数；悬停或聚焦可查看完整值，≈0 仅为显示处理。
- 修改输入后旧结果自动清除。示例包括行交换、零列和线性相关行。

## 项目结构

- `Domain/MatLag.Domain.csproj`：独立计算类库，无 Web 依赖。
- `Web/MatLag.Web.csproj`：ASP.NET Core 接口与原生 HTML/CSS/JavaScript 页面，无 Node.js、数据库或 CDN 依赖。
- `MatLag.csproj`：原有控制台压力测试，引用同一 Domain 类库。

Web 通过 `MatrixDemoService` 将数字数组转换成领域对象，调用现有 `Ref`，不在前端实现消元算法。分步状态均从原矩阵计算，返回后在浏览器内切换。当前算法使用严格非零判断，页面报告的主元数量沿用此规则，不是容差意义的数值秩判定。
