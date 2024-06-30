using Highs2;
using LpSolveDotNet;

namespace LPApi
{
    public enum Direction
    {
        Min = 0,
        Max = 1
    }

    public class TableProblem
    {
        private const double _negInf = -1e30;
        private const double _posInf = 1e30;

        public Direction Direction { get; set; }
        public double?[][] Rows { get; set; }
        public double[] VarCosts { get; set; }

        public TableProblem()
        {
            Rows = [];
            VarCosts = [];
        }

        public HighsModel ToHighsModel()
        {
            var varCount = VarCosts.Length;

            double[] cc = VarCosts; // col cost: coefficients in objective function

            var singleVarRows = new List<double?[]>(); // useful for col bounds
            var multiVarRows = new List<double?[]>(); // constraints
            foreach (var row in Rows)
            {
                if (row.Skip(1).Take(varCount).Count(value => value.HasValue) == 1) // skip L and U
                    singleVarRows.Add(row);
                else
                    multiVarRows.Add(row);                        
            }

            var colLowerBounds = new List<double>();
            var colUpperBounds = new List<double>();
            for (var i = 0; i < varCount; i++)
            {
                var lowerBound = 
                    singleVarRows
                        .Where(singleVarRow => singleVarRow[i + 1].HasValue) // skip L
                        .Select(singleVarRow => singleVarRow.First() / singleVarRow[i + 1]!.Value) // it's possible to provide a value different from 1, so divide
                        .Where(value => value.HasValue)
                        .Min();
                colLowerBounds.Add(lowerBound ?? _negInf);
                var upperBound = 
                    singleVarRows
                        .Where(singleVarRow => singleVarRow[i + 1].HasValue) // skip L
                        .Select(singleVarRow => singleVarRow.Last() / singleVarRow[i + 1]!.Value) // it's possible to provide a value different from 1, so divide
                        .Where(value => value.HasValue)
                        .Max();
                colUpperBounds.Add(upperBound ?? _posInf);
            }
            double[] cl = [.. colLowerBounds]; // col lower
            double[] cu = [.. colUpperBounds]; // col upper

            double[] rl = multiVarRows.Select(row => row.First() ?? _negInf).ToArray(); // row lower
            double[] ru = multiVarRows.Select(row => row.Last() ?? _posInf).ToArray(); // row upper

            var startCounter = 0;
            var startList = new List<int>();
            var indexList = new List<int>();
            var valueList = new List<double>();
            for (var i = 0; i < varCount; i++) // optimization: use same loop as upper one
            {
                startList.Add(startCounter);
                for (var rowIndex = 0; rowIndex < multiVarRows.Count; rowIndex++)
                {
                    var multiVarRow = multiVarRows[rowIndex];
                    var value = multiVarRow[i+1]; // skip L
                    if (value.HasValue)
                    {
                        indexList.Add(rowIndex);
                        valueList.Add(value.Value);
                        startCounter++;
                    }
                }
            }

            int[] astart = [.. startList]; // positions in aindex/avalue of first non-zero in each column
            int[] aindex = [.. indexList]; // row indices of non-zeros
            double[] avalue = [.. valueList]; // values of non-zeros
            var sense = Direction == Direction.Min ? HighsObjectiveSense.kMinimize : HighsObjectiveSense.kMaximize;
            double offset = 0;
            HighsMatrixFormat a_format = HighsMatrixFormat.kColwise;

            return new HighsModel(cc, cl, cu, rl, ru, astart, aindex, avalue, null, offset, a_format, sense);
        }

        public LpSolve ToLpSolve()
        {
            var varCount = VarCosts.Length;

            double[] cc = VarCosts; // col cost: coefficients in objective function

            var singleVarRows = new List<double?[]>(); // useful for col bounds
            var multiVarRows = new List<double?[]>(); // constraints
            foreach (var row in Rows)
            {
                if (row.Skip(1).Take(varCount).Count(value => value.HasValue) == 1) // skip L and U
                    singleVarRows.Add(row);
                else
                    multiVarRows.Add(row);
            }

            var lpSolve = LpSolve.make_lp(multiVarRows.Count, varCount);

            for (var i = 0; i < varCount; i++)
            {
                var lowerBound =
                    singleVarRows
                        .Where(singleVarRow => singleVarRow[i + 1].HasValue) // skip L
                        .Select(singleVarRow => singleVarRow.First() / singleVarRow[i + 1]!.Value) // it's possible to provide a value different from 1, so divide
                        .Where(value => value.HasValue)
                        .Min();

                if (lowerBound.HasValue)
                    lpSolve.set_lowbo(i + 1, lowerBound.Value);

                var upperBound =
                    singleVarRows
                        .Where(singleVarRow => singleVarRow[i + 1].HasValue) // skip L
                        .Select(singleVarRow => singleVarRow.Last() / singleVarRow[i + 1]!.Value) // it's possible to provide a value different from 1, so divide
                        .Where(value => value.HasValue)
                        .Max();

                if (upperBound.HasValue)
                    lpSolve.set_upbo(i + 1, upperBound.Value);
            }

            foreach (var row in multiVarRows)
            {
                var indexList = new List<int>();
                var valueList = new List<double>();
                for (var i = 1; i <= varCount; i++)
                {
                    var value = row[i];
                    if (!value.HasValue)
                        continue;

                    indexList.Add(i);
                    valueList.Add(value.Value);
                }

                var lowerBound = row.First();
                var upperBound = row.Last();
                if (lowerBound.HasValue)
                    lpSolve.add_constraintex(indexList.Count, [.. valueList], [.. indexList], lpsolve_constr_types.GE, lowerBound.Value);
                if (upperBound.HasValue)
                    lpSolve.add_constraintex(indexList.Count, [.. valueList], [.. indexList], lpsolve_constr_types.LE, upperBound.Value);
            }

            lpSolve.set_sense(maximize: Direction == Direction.Max);
            lpSolve.set_obj_fnex(varCount, VarCosts, VarCosts.Select((_, index) => index + 1).ToArray());

            return lpSolve;
        }
    }

    public class MPSProblem(string data)
    {
        public string Data { get; set; } = data;
    }
}
