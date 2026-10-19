using System.Diagnostics;
using System.Runtime.InteropServices;
using MatLag.Domain;

namespace MatLag;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($"运行环境：{RuntimeInformation.FrameworkDescription} / {RuntimeInformation.ProcessArchitecture}");
#if DEBUG
        Console.WriteLine("当前为 Debug，建议用 dotnet run -c Release 测试性能。\n");
#endif
        Console.WriteLine("每组预热 3 次，再测量 5 次，报告中位数；矩阵创建不计时，运算内部的分配和复制计时。");
        Console.WriteLine("分配量是单次运算的累计托管分配，不是峰值内存。\n");

        // 固定种子，方便重复运行时比较。
        var random = new Random(42);
        RunBenchmarks("矩阵加法", [16, 32, 64, 128], (left, right) => left + right, random);
        RunBenchmarks("矩阵乘法", [64, 128, 256, 512], (left, right) => left * right, random);
    }

    private static void RunBenchmarks(string title, int[] sizes,
        Func<Matrix, Matrix, Matrix> operation, Random random)
    {
        const int repetitions = 5;
        Console.WriteLine(title);
        Console.WriteLine($"{"尺寸",-12} {"中位耗时(ms)",14} {"相对上组",10} {"分配(MiB/次)",15} {"GC次数(0/1/2)",16} {"结果[0,0]",14}");
        double previousMilliseconds = 0;

        foreach (int size in sizes)
        {
            var left = CreateMatrix(size, random);
            var right = CreateMatrix(size, random);

            for (int i = 0; i < 3; i++)
            {
                GC.KeepAlive(operation(left, right));
            }

            var elapsed = new double[repetitions];
            long allocatedTotal = 0;
            var collections = new int[3];
            double sample = 0;

            for (int i = 0; i < repetitions; i++)
            {
                // 清理上轮垃圾；运算自身触发的 GC 仍计入耗时。
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                int gen0 = GC.CollectionCount(0);
                int gen1 = GC.CollectionCount(1);
                int gen2 = GC.CollectionCount(2);
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                long start = Stopwatch.GetTimestamp();

                var result = operation(left, right);

                elapsed[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                allocatedTotal += GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                collections[0] += GC.CollectionCount(0) - gen0;
                collections[1] += GC.CollectionCount(1) - gen1;
                collections[2] += GC.CollectionCount(2) - gen2;
                sample = result.GetElement(0, 0);
                GC.KeepAlive(result);
            }

            Array.Sort(elapsed);
            double median = elapsed[repetitions / 2];
            string ratio = previousMilliseconds > 0 ? $"{median / previousMilliseconds:F2}x" : "-";
            double allocatedMiB = allocatedTotal / (double)repetitions / 1024 / 1024;
            string gcCounts = $"{collections[0]}/{collections[1]}/{collections[2]}";
            Console.WriteLine($"{$"{size}×{size}",-12} {median,14:F3} {ratio,10} {allocatedMiB,15:F3} {gcCounts,16} {sample,14:G6}");
            previousMilliseconds = median;
        }

        Console.WriteLine("GC 次数为 5 次测量的合计，不包含测量前主动清理。\n");
    }

    private static Matrix CreateMatrix(int size, Random random)
    {
        var elements = new double[size, size];
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                elements[row, column] = random.NextDouble();
            }
        }

        return new Matrix(elements);
    }
}
