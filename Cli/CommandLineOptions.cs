using System.Globalization;

namespace MatLag.Cli;

internal sealed class CommandLineOptions
{
    public string? MatrixText { get; private set; }
    public string? InputPath { get; private set; }
    public string? OutputPath { get; private set; }
    public string Format { get; private set; } = "text";
    public int Precision { get; private set; } = 17;
    public ulong MaxPivots { get; private set; }
    public bool Scientific { get; private set; }
    public bool Transpose { get; private set; }
    public bool Timing { get; private set; }
    public bool Stats { get; private set; }

    public static CommandLineOptions Parse(string[] args)
    {
        if (args[0] != "ref")
            throw new ArgumentException($"未知命令：{args[0]}。使用 --help 查看用法。");

        var options = new CommandLineOptions();
        var seen = new HashSet<string>();
        bool positionalOnly = false;
        for (int i = 1; i < args.Length; i++)
        {
            string argument = args[i];
            if (!positionalOnly && argument == "--")
            {
                positionalOnly = true;
                continue;
            }

            int equals = argument.StartsWith("--", StringComparison.Ordinal) ? argument.IndexOf('=') : -1;
            string name = equals >= 0 ? argument[..equals] : argument;
            name = name switch
            {
                "-i" => "--input",
                "-o" => "--output",
                "-f" => "--format",
                "-p" => "--precision",
                "-t" => "--transpose",
                _ => name
            };

            bool takesValue = name is "--input" or "--output" or "--format" or "--precision" or "--max-pivots";
            bool flag = name is "--scientific" or "--transpose" or "--timing" or "--stats";
            if (!positionalOnly && (takesValue || flag))
            {
                if (!seen.Add(name))
                    throw new ArgumentException($"重复参数：{name}。");
                if (flag && equals >= 0)
                    throw new ArgumentException($"{name} 不接受参数值。");

                string value = "";
                if (takesValue)
                {
                    if (equals >= 0)
                        value = argument[(equals + 1)..];
                    else if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                             && args[i + 1] is not ("-i" or "-o" or "-f" or "-p" or "-t" or "-h" or "-V"))
                        value = args[++i];
                    if (string.IsNullOrWhiteSpace(value))
                        throw new ArgumentException($"{name} 缺少参数值。");
                }

                switch (name)
                {
                    case "--input": options.InputPath = value; break;
                    case "--output": options.OutputPath = value; break;
                    case "--format":
                        if (value is not ("text" or "csv" or "json"))
                            throw new ArgumentException("--format 必须为 text、csv 或 json。");
                        options.Format = value;
                        break;
                    case "--precision":
                        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int precision)
                            || precision is < 1 or > 17)
                            throw new ArgumentException("--precision 必须为 1～17 的整数（有效位数）。");
                        options.Precision = precision;
                        break;
                    case "--max-pivots":
                        if (!ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ulong pivots))
                            throw new ArgumentException("--max-pivots 必须为非负整数，0 表示不限。");
                        options.MaxPivots = pivots;
                        break;
                    case "--scientific": options.Scientific = true; break;
                    case "--transpose": options.Transpose = true; break;
                    case "--timing": options.Timing = true; break;
                    case "--stats": options.Stats = true; break;
                }

                continue;
            }

            // 单个负数和以负数开头的矩阵也是合法的位置参数。
            if (!positionalOnly && argument.StartsWith('-') && argument != "-"
                && (argument.Length < 2 || !(char.IsAsciiDigit(argument[1]) || argument[1] == '.')))
                throw new ArgumentException($"未知参数：{argument}。");
            if (options.MatrixText is not null)
                throw new ArgumentException("只能提供一个矩阵参数，请将完整矩阵放在同一对引号内。");
            options.MatrixText = argument;
        }

        if (options.MatrixText is not null && options.InputPath is not null)
            throw new ArgumentException("矩阵参数与 --input 不能同时使用。");
        if (options.MatrixText is null && options.InputPath is null)
            throw new ArgumentException("请提供矩阵参数或 --input 文件；使用 --input - 从管道读取。");
        return options;
    }
}
