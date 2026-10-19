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

## 命令行工具

在仓库根目录用一行命令计算行阶梯形（REF），无需交互输入：

```sh
dotnet run --project MatLag.csproj -- ref "1 2 3;4 5 6"
```

输出：

```text
1 2 3
0 -3 -6
```

- 矩阵参数须放在引号内；分号或换行分行，空格、制表符或逗号分列，各行列数必须一致。
- 支持矩形矩阵、负数、小数和科学计数法（小数点使用 `.`），元素必须为有限数字。
- 文件输入支持纯数字文本与无表头 CSV，不支持 JSON 输入；空行和空单元格会报错，允许文件末尾换行。
- 参数可放在矩阵前后，支持 `--选项=值`；短选项不合并，`--` 用于结束选项解析。
- 诊断信息与错误写入标准错误，标准输出只包含结果矩阵，方便重定向。

| 参数 | 作用 | 默认值 |
| --- | --- | --- |
| `-i, --input <路径>` | 从文件读取矩阵；`-` 从管道/重定向读取，与位置矩阵参数互斥 | 位置矩阵参数 |
| `-o, --output <路径>` | 将结果写入文件（覆盖）；`-` 写入标准输出 | 标准输出 |
| `-f, --format <格式>` | 输出为 `text`、`csv` 或 `json` 二维数字数组 | `text` |
| `-p, --precision <位数>` | 输出有效位数，范围 1～17 | `17` |
| `--scientific` | 强制使用科学计数法，精度仍为有效位数 | 关闭 |
| `-t, --transpose` | 先转置输入，再进行消元 | 关闭 |
| `--max-pivots <数量>` | 最多处理的主元数，`0` 表示不限 | `0` |
| `--timing` | 显示 Ref 耗时 | 关闭 |
| `--stats` | 显示矩阵尺寸、主元上限、Ref 耗时及当前线程托管分配量 | 关闭 |
| `-h, --help` | 查看帮助，无参数运行也显示帮助 | — |
| `-V, --version` | 显示版本 | — |

精度和科学计数法只影响输出，不改变内部计算；`--max-pivots` 限制消元进度，结果可能尚未达到完整行阶梯形。耗时和分配量仅统计 `Ref`，不包含输入解析、转置与输出；首次调用可能包含 JIT 开销。

更多示例：

```sh
dotnet run --project MatLag.csproj -c Release -- ref "0,1,2;1,3,4" --timing
dotnet run --project MatLag.csproj -- ref "1e-2 -2;3 4" > result.txt
dotnet run --project MatLag.csproj -- ref "0,1,2;1,3,4" -p 6 --scientific --stats
dotnet run --project MatLag.csproj -- ref -i matrix.csv -o result.json -f json
dotnet run --project MatLag.csproj -- ref -i - --format=csv < matrix.csv
dotnet run --project MatLag.csproj -- ref "1 2;3 4;5 6" -t --max-pivots=1
dotnet run --project MatLag.csproj -- --version
```

退出码：`0` 成功、`1` 计算失败、`2` 参数错误、`3` 文件读写失败。`--input -` 只接受管道或重定向，不进入交互式输入。

## 项目结构

- `Domain/MatLag.Domain.csproj`：独立计算类库，无 Web 依赖。
- `Web/MatLag.Web.csproj`：ASP.NET Core 接口与原生 HTML/CSS/JavaScript 页面，无 Node.js、数据库或 CDN 依赖。
- `MatLag.csproj`：非交互式矩阵命令行工具，引用同一 Domain 类库。

Web 通过 `MatrixDemoService` 将数字数组转换成领域对象，调用现有 `Ref`，不在前端实现消元算法。分步状态均从原矩阵计算，返回后在浏览器内切换。当前算法使用严格非零判断，页面报告的主元数量沿用此规则，不是容差意义的数值秩判定。
