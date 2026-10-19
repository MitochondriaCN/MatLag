using System.Diagnostics;
using System.Globalization;
using System.Text;
using MatLag.Cli;
using MatLag.Domain;

namespace MatLag;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"]
            || args is ["ref", "--help"] or ["ref", "-h"])
        {
            PrintHelp();
            return 0;
        }

        if (args is ["--version"] or ["-V"])
        {
            Console.WriteLine($"MatLag {typeof(Program).Assembly.GetName().Version?.ToString(3)}");
            return 0;
        }

        try
        {
            CommandLineOptions options = CommandLineOptions.Parse(args);
            string input = options.MatrixText ?? ReadInput(options.InputPath!);
            Matrix matrix = ParseMatrix(input);
            if (options.Transpose)
                matrix = Transpose(matrix);
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            Matrix result = MatrixTool.Ref(matrix, options.MaxPivots);
            double milliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            // 在输出前检查完整结果，避免溢出时输出一半矩阵。
            if (result.Elements.Any(value => !double.IsFinite(value)))
                throw new ArithmeticException("计算出现溢出或非有限值，请调整矩阵的数值范围。");

            string output = FormatMatrix(result, options);
            if (options.OutputPath is null or "-")
                Console.Write(output);
            else
                File.WriteAllText(options.OutputPath, output, new UTF8Encoding(false));
            if (options.Timing || options.Stats)
                Console.Error.WriteLine($"Ref 耗时：{milliseconds.ToString("F3", CultureInfo.InvariantCulture)} ms");
            if (options.Stats)
            {
                Console.Error.WriteLine($"矩阵：{matrix.RowCount}×{matrix.ColumnCount}；" +
                                        $"消元主元上限：{(options.MaxPivots == 0 ? "不限" : options.MaxPivots.ToString(CultureInfo.InvariantCulture))}");
                Console.Error.WriteLine($"Ref 当前线程托管分配：{(allocated / 1048576.0).ToString("F3", CultureInfo.InvariantCulture)} MiB");
            }
            return 0;
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine($"参数错误：{exception.Message}");
            return 2;
        }
        catch (ArithmeticException exception)
        {
            Console.Error.WriteLine($"计算失败：{exception.Message}");
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"文件读写失败：{exception.Message}");
            return 3;
        }
    }

    private static string ReadInput(string path)
    {
        if (path != "-")
            return File.ReadAllText(path);
        if (!Console.IsInputRedirected)
            throw new ArgumentException("--input - 需要管道或重定向输入。");
        return Console.In.ReadToEnd();
    }

    private static Matrix ParseMatrix(string text)
    {
        // 允许文本/CSV 文件末尾的换行，但不忽略空行和尾部分号。
        string[] rows = text.Trim().Replace("\r\n", "\n").Replace('\r', '\n').Split([';', '\n']);
        var values = new List<double>();
        int columns = 0;
        for (int row = 0; row < rows.Length; row++)
        {
            if (rows[row].Split(',').Any(field => string.IsNullOrWhiteSpace(field)))
                throw new ArgumentException($"第 {row + 1} 行包含空行或空单元格。");
            string[] tokens = rows[row].Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                throw new ArgumentException($"第 {row + 1} 行为空。");
            if (row == 0)
                columns = tokens.Length;
            else if (tokens.Length != columns)
                throw new ArgumentException($"第 {row + 1} 行有 {tokens.Length} 列，预期 {columns} 列。");

            for (int column = 0; column < tokens.Length; column++)
            {
                if (!double.TryParse(tokens[column], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                    || !double.IsFinite(value))
                    throw new ArgumentException($"第 {row + 1} 行第 {column + 1} 列不是有限数字：{tokens[column]}。");
                values.Add(value);
            }
        }

        return new Matrix((ulong)rows.Length, (ulong)columns, values.ToArray());
    }

    private static Matrix Transpose(Matrix matrix)
    {
        var values = new double[matrix.ColumnCount, matrix.RowCount];
        for (ulong row = 0; row < matrix.RowCount; row++)
        for (ulong column = 0; column < matrix.ColumnCount; column++)
            values[column, row] = matrix.GetElement(row, column);
        return new Matrix(values);
    }

    private static string FormatMatrix(Matrix matrix, CommandLineOptions options)
    {
        var output = new StringBuilder();
        string numberFormat = options.Scientific ? $"E{options.Precision - 1}" : $"G{options.Precision}";
        bool json = options.Format == "json";
        if (json)
            output.Append('[');
        for (ulong row = 0; row < matrix.RowCount; row++)
        {
            if (json)
            {
                if (row > 0)
                    output.Append(',');
                output.Append('[');
            }
            for (ulong column = 0; column < matrix.ColumnCount; column++)
            {
                if (column > 0)
                    output.Append(options.Format == "text" ? ' ' : ',');
                output.Append(matrix.GetElement(row, column).ToString(numberFormat, CultureInfo.InvariantCulture));
            }

            if (json)
                output.Append(']');
            else
                output.AppendLine();
        }

        if (json)
            output.Append(']').AppendLine();
        return output.ToString();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            MatLag — 矩阵命令行工具

            用法：
              MatLag ref "矩阵" [选项]
              MatLag ref --input <文件|-> [选项]
              MatLag --help | --version

            命令：
              ref       将矩阵化为行阶梯形（REF）

            输入/输出：
              -i, --input <路径>       从文本/CSV 文件读取；- 表示标准输入
              -o, --output <路径>      写入文件（覆盖）；默认或 - 表示标准输出
              -f, --format <格式>      输出格式：text（默认）、csv、json

            数值显示：
              -p, --precision <位数>   有效位数，1～17，默认 17
                  --scientific        强制使用科学计数法

            计算：
              -t, --transpose         先转置输入矩阵，再进行消元
                  --max-pivots <数量>  最多处理的主元数；默认 0 表示不限

            诊断/信息：
                  --timing            显示 Ref 耗时
                  --stats             显示尺寸、主元上限、Ref 耗时和托管分配量
              -h, --help              显示帮助
              -V, --version           显示版本

            矩阵格式：分号或换行分行，空格、制表符或逗号分列。
            支持负数、小数、科学计数法；小数点为 .，不支持 CSV 表头或 JSON 输入。
            精度只影响输出。限制主元数时，结果可能尚未达到完整行阶梯形。
            诊断信息写入标准错误；结果写入标准输出或指定文件。
            支持 --选项=值；使用 -- 可结束选项解析；不支持短选项合并。

            示例：
              dotnet run --project MatLag.csproj -- ref "1 2 3;4 5 6"
              MatLag ref "0,1,2;1,3,4" -p 6 --scientific --stats
              MatLag ref -i matrix.csv -o result.json -f json
              MatLag ref --input - --format=csv --max-pivots=2

            退出码：0 成功，1 计算失败，2 参数错误，3 文件读写失败。
            """);
    }
}
