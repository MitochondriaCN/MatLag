namespace MatLag.Domain;

public class Matrix
{
    private readonly double[,] _elements;

    /// <summary>
    /// 行数
    /// </summary>
    public ulong RowCount { get; private set; }

    /// <summary>
    /// 列数
    /// </summary>
    public ulong ColumnCount { get; private set; }

    public double[] Elements
    {
        get
        {
            double[] elem = new double[RowCount * ColumnCount];
            for (ulong i = 0; i < RowCount; i++)
            {
                for (ulong j = 0; j < ColumnCount; j++)
                {
                    elem[ColumnCount * i + j] = _elements[i, j];
                }
            }

            return elem;
        }
    }

    public Matrix(ulong rowCount, ulong columnCount, params double[] elements)
    {
        if (rowCount == 0 || columnCount == 0)
        {
            throw new ArgumentOutOfRangeException(rowCount == 0 ? nameof(rowCount) : nameof(columnCount), "行数或列数不能为0");
        }

        if ((ulong)elements.LongLength != rowCount * columnCount)
        {
            throw new ArgumentException("给定元素的数量不符合给定矩阵大小。");
        }

        _elements = new double[rowCount, columnCount];

        for (ulong i = 0; i < rowCount; i++)
        {
            for (ulong j = 0; j < columnCount; j++)
            {
                _elements[i, j] = elements[columnCount * i + j];
            }
        }

        RowCount = rowCount;
        ColumnCount = columnCount;
    }

    public Matrix(double[,] elements)
    {
        if (elements.GetLength(0) == 0 || elements.GetLength(1) == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elements), "行数或列数不能为0");
        }

        _elements = (double[,])elements.Clone();

        RowCount = (ulong)elements.GetLength(0);
        ColumnCount = (ulong)elements.GetLength(1);
    }

    public double GetElement(ulong row, ulong column)
    {
        if (row >= RowCount || column >= ColumnCount)
        {
            throw new ArgumentException("给定行或列下标不符合矩阵大小。");
        }

        return _elements[row, column];
    }

    public Matrix WithElement(ulong row, ulong column, double value)
    {
        if (row >= RowCount || column >= ColumnCount)
        {
            throw new ArgumentOutOfRangeException(row >= RowCount ? nameof(row) : nameof(column));
        }

        double[,] newElements = (double[,])_elements.Clone();
        newElements[row, column] = value;

        return new Matrix(newElements);
    }

    public static Matrix operator +(Matrix m1, Matrix m2)
    {
        if (m1.RowCount != m2.RowCount || m1.ColumnCount != m2.ColumnCount)
        {
            throw new ArgumentException("相加矩阵大小不相等。");
        }

        var addedElements = m1.Elements.Select((e, index) => e + m2.Elements[index]).ToArray();

        return new Matrix(m1.RowCount, m1.ColumnCount, addedElements);
    }

    public static Matrix operator *(Matrix l, Matrix r)
    {
        if (l.ColumnCount != r.RowCount)
        {
            throw new ArgumentException("两矩阵维度不匹配，无法相乘。");
        }

        double[,] result = new double[l.RowCount, r.ColumnCount];

        for (ulong i = 0; i < l.RowCount; i++)
        {
            for (ulong j = 0; j < r.ColumnCount; j++)
            {
                // 计算(i,j)位置元素
                double sum = 0;

                for (ulong k = 0; k < l.ColumnCount; k++)
                {
                    sum += l.GetElement(i, k) * r.GetElement(k, j);
                }

                result[i, j] = sum;
            }
        }

        return new Matrix(result);
    }
}