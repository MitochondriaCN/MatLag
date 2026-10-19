using System.Diagnostics;
using MatLag.Domain;

namespace MatLag;

class Program
{
    static void Main(string[] args)
    {
        // 包含全零列、行交换、线性相关行，预期秩为 3。
        var matrix = new Matrix(new double[,]
        {
            { 0, 0, 2, 1, -1, 3, 4 },
            { 0, 1, 0, 2, 1, -1, 2 },
            { 0, 0, 0, 0, 2, 1, -2 },
            { 0, 1, 2, 3, 0, 2, 6 },
            { 0, 0, 4, 2, 0, 7, 6 }
        });

        Console.WriteLine("=== 5×7 矩阵：零列、换行、相关行（预期秩 3） ===");
        PrintMatrix(matrix);
        Matrix result = MatrixTool.Ref(matrix);
        Console.WriteLine("\n消元后的行阶梯形矩阵：");
        PrintMatrix(result);
        CheckEchelon(result, 3);

        Console.WriteLine("\n=== 稠密增广矩阵压力测试（固定随机种子，已知解） ===");
        Console.WriteLine("耗时仅包含 Ref；分配量为当前线程的累计托管分配。首次调用可能包含 JIT 开销。");
        var random = new Random(42);
        foreach (int size in new[] { 8, 16, 32, 64 })
        {
            RunDenseTest(size, random);
        }
    }

    private static void RunDenseTest(int size, Random random)
    {
        // 严格对角占优，保证系数矩阵可逆；构造 b = Ax，便于独立检查消元结果。
        var elements = new double[size, size + 1];
        var expected = new double[size];
        for (int i = 0; i < size; i++)
            expected[i] = i % 7 - 3;

        for (int row = 0; row < size; row++)
        {
            double sum = 0;
            for (int column = 0; column < size; column++)
            {
                if (row == column)
                    continue;
                elements[row, column] = random.Next(-9, 10);
                sum += Math.Abs(elements[row, column]);
            }

            elements[row, row] = sum + 1;
            for (int column = 0; column < size; column++)
                elements[row, size] += elements[row, column] * expected[column];
        }

        var matrix = new Matrix(elements);
        if (size == 8)
        {
            Console.WriteLine("\n8×9 原增广矩阵：");
            PrintMatrix(matrix);
        }

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        Matrix result = MatrixTool.Ref(matrix);
        double milliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        if (size == 8)
        {
            Console.WriteLine("\n8×9 消元结果：");
            PrintMatrix(result);
        }

        CheckEchelon(result, size);

        // 回代求解，再与预设解及原方程比较，避免只凭矩阵外观判断正确性。
        var actual = new double[size];
        for (int row = size - 1; row >= 0; row--)
        {
            double value = result.GetElement((ulong)row, (ulong)size);
            for (int column = row + 1; column < size; column++)
                value -= result.GetElement((ulong)row, (ulong)column) * actual[column];
            actual[row] = value / result.GetElement((ulong)row, (ulong)row);
        }

        double maxError = 0;
        double maxResidual = 0;
        for (int row = 0; row < size; row++)
        {
            maxError = Math.Max(maxError, Math.Abs(actual[row] - expected[row]));
            double residual = -elements[row, size];
            for (int column = 0; column < size; column++)
                residual += elements[row, column] * actual[column];
            maxResidual = Math.Max(maxResidual, Math.Abs(residual));
        }

        bool passed = double.IsFinite(maxError) && double.IsFinite(maxResidual)
                      && maxError < 1e-8 && maxResidual < 1e-8;
        Console.WriteLine($"{size}×{size + 1}：{milliseconds:F3} ms，分配 {allocated / 1024.0 / 1024:F3} MiB，" +
                          $"最大解误差 {maxError:E3}，最大残差 {maxResidual:E3}，{(passed ? "通过" : "失败")}");
        if (!passed)
            Environment.ExitCode = 1;
    }

    private static void CheckEchelon(Matrix matrix, int expectedRank)
    {
        const double tolerance = 1e-9;
        int previousPivot = -1;
        int rank = 0;
        bool zeroRowSeen = false;
        bool passed = true;
        for (ulong row = 0; row < matrix.RowCount; row++)
        {
            int pivot = -1;
            for (ulong column = 0; column < matrix.ColumnCount; column++)
            {
                double value = matrix.GetElement(row, column);
                if (!double.IsFinite(value))
                    passed = false;
                if (pivot < 0 && Math.Abs(value) > tolerance)
                    pivot = (int)column;
            }

            if (pivot < 0)
            {
                zeroRowSeen = true;
                continue;
            }

            if (zeroRowSeen || pivot <= previousPivot)
                passed = false;
            previousPivot = pivot;
            rank++;
        }

        passed &= rank == expectedRank;
        Console.WriteLine($"阶梯形检查（容差 {tolerance:E0}）：{(passed ? "通过" : "失败")}，秩 {rank}，预期 {expectedRank}");
        if (!passed)
            Environment.ExitCode = 1;
    }

    private static void PrintMatrix(Matrix matrix)
    {
        for (ulong row = 0; row < matrix.RowCount; row++)
        {
            for (ulong column = 0; column < matrix.ColumnCount; column++)
            {
                Console.Write($"{matrix.GetElement(row, column),14:G6} ");
            }

            Console.WriteLine();
        }
    }
}
