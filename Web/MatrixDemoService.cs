using System.Diagnostics;
using MatLag.Domain;

namespace MatLag.Web;

public sealed record RefRequest(double[][]? Matrix, bool Steps = true);
public sealed record RefResponse(List<double[][]> States, int PivotCount, double ElapsedMilliseconds);

// Web 的边界：只在这里将传输数组转换为领域对象，Domain 无需了解 HTTP 或界面。
public sealed class MatrixDemoService
{
    public RefResponse Calculate(RefRequest request)
    {
        var rows = request.Matrix;
        int limit = request.Steps ? 10 : 32;
        if (rows is null || rows.Length == 0 || rows.Length > limit || rows[0] is null
            || rows[0].Length == 0 || rows[0].Length > limit)
            throw new ArgumentException($"{(request.Steps ? "分步演示" : "直接完成")}的行数、列数均须在 1～{limit} 之间。");

        int columns = rows[0].Length;
        if (rows.Any(row => row is null || row.Length != columns || row.Any(value => !double.IsFinite(value))))
            throw new ArgumentException("请提供列数一致、只包含有限数字的矩阵。");

        var original = new Matrix((ulong)rows.Length, (ulong)columns, rows.SelectMany(row => row).ToArray());
        long start = Stopwatch.GetTimestamp();
        var final = MatrixTool.Ref(original);
        var finalValues = ToArray(final);
        int pivots = finalValues.Count(row => row.Any(value => value != 0));
        var states = new List<double[][]> { ToArray(original) };
        if (request.Steps)
        {
            for (int step = 1; step < pivots; step++)
                states.Add(ToArray(MatrixTool.Ref(original, (ulong)step)));
            if (pivots > 0)
                states.Add(finalValues);
        }
        else
        {
            states.Add(finalValues);
        }

        return new RefResponse(states, pivots, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }

    private static double[][] ToArray(Matrix matrix)
    {
        var result = new double[(int)matrix.RowCount][];
        for (ulong row = 0; row < matrix.RowCount; row++)
        {
            result[row] = new double[(int)matrix.ColumnCount];
            for (ulong column = 0; column < matrix.ColumnCount; column++)
            {
                double value = matrix.GetElement(row, column);
                if (!double.IsFinite(value))
                    throw new ArithmeticException("计算出现溢出或非有限值，请调整输入矩阵的数值范围。");
                result[row][column] = value;
            }
        }
        return result;
    }
}
