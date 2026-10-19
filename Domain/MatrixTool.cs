namespace MatLag.Domain;

public static class MatrixTool
{
    public static Matrix BuildIdentityMatrix(ulong rowCount)
    {
        double[] m = new double[rowCount * rowCount];

        for (ulong i = 0; i < rowCount; i++)
        {
            for (ulong j = 0; j < rowCount; j++)
            {
                if (i == j)
                    m[rowCount * i + j] = 1;
                else
                    m[rowCount * i + j] = 0;
            }
        }

        return new Matrix(rowCount, rowCount, m);
    }

    /// <summary>
    /// 构建行置换矩阵。
    /// </summary>
    /// <param name="rowCount">对应矩阵总行数。</param>
    /// <param name="rowIndex1">要交换的第一行下标（从0开始）</param>
    /// <param name="rowIndex2">要交换的第二行下标（从0开始）</param>
    public static Matrix BuildRowInterchangeMatrix(ulong rowCount, ulong rowIndex1, ulong rowIndex2)
    {
        double[] m = new double[rowCount * rowCount];

        for (ulong i = 0; i < rowCount; i++)
        {
            for (ulong j = 0; j < rowCount; j++)
            {
                if (i != rowIndex1 && i != rowIndex2)
                {
                    if (i == j)
                    {
                        m[rowCount * i + j] = 1;
                    }
                    else
                    {
                        m[rowCount * i + j] = 0;
                    }
                }
                else
                {
                    if ((i == rowIndex1 && j == rowIndex2) || (i == rowIndex2 && j == rowIndex1))
                    {
                        m[rowCount * i + j] = 1;
                    }
                    else
                    {
                        m[rowCount * i + j] = 0;
                    }
                }
            }
        }

        return new Matrix(rowCount, rowCount, m);
    }

    public static Matrix BuildRowAdditionMatrix(ulong rowCount, ulong sourceRowIndex, double scalar,
        ulong targetRowIndex)
    {
        Matrix bareI = BuildIdentityMatrix(rowCount);
        return bareI.WithElement(targetRowIndex, sourceRowIndex, scalar);
    }

    public static Matrix Ref(Matrix m)
    {
        Matrix currentM = m;
        ulong fineRows = 0;

        // 逐列处理
        for (ulong column = 0; column < m.ColumnCount; column++)
        {
            // 非零行下标集
            List<ulong> nonZeroRowIndices = new List<ulong>();

            // 查找非零行
            for (ulong row = fineRows; row < m.RowCount; row++)
            {
                if (currentM.GetElement(row, column) != 0)
                {
                    nonZeroRowIndices.Add(row);
                }
            }

            // 判断非零行并换行
            switch (nonZeroRowIndices.Count)
            {
                case 0: // 无非零行（本列全为0）
                    continue; // 下一列
                case 1: // 1个非零行
                    if (nonZeroRowIndices[0] == fineRows) // 已到合适位置
                        break;
                    // 换行
                    currentM = BuildRowInterchangeMatrix(m.RowCount, nonZeroRowIndices[0], fineRows) * currentM;
                    nonZeroRowIndices.RemoveAt(0);
                    nonZeroRowIndices.Add(fineRows);
                    break;
                default: // 1个以上非零行
                    if (nonZeroRowIndices.Any(r => r == fineRows))
                        break;
                    // 换行
                    currentM = BuildRowInterchangeMatrix(m.RowCount, nonZeroRowIndices[0], fineRows) * currentM;
                    nonZeroRowIndices.RemoveAt(0);
                    nonZeroRowIndices.Add(fineRows);
                    break;
            }

            // 消去下方行
            foreach (var nonZeroRowIndex in nonZeroRowIndices)
            {
                if (nonZeroRowIndex == fineRows)
                    continue;

                // 消元乘数
                double multiplier = -currentM.GetElement(nonZeroRowIndex, column) /
                                    currentM.GetElement(fineRows, column);
                // 消元矩阵
                Matrix p = BuildRowAdditionMatrix(currentM.RowCount, fineRows, multiplier, nonZeroRowIndex);
                // 消元
                currentM = p * currentM;

                // 消除舍入误差，主元列前导元素直接设为0
                currentM = currentM.WithElement(nonZeroRowIndex, column, 0);
            }

            fineRows++;
        }

        return currentM;
    }
}