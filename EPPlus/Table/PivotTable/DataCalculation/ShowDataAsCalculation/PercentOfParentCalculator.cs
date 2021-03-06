﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OfficeOpenXml.Table.PivotTable.DataCalculation.ShowDataAsCalculation
{
	/// <summary>
	/// Calculates the <see cref="ShowDataAs.PercentOfParent"/> value in a pivot table.
	/// </summary>
	internal class PercentOfParentCalculator : PercentOfParentCalculatorBase
	{
		#region Constructors
		/// <summary>
		/// Constructs the calculator.
		/// </summary>
		/// <param name="pivotTable">The pivot table to calculate against.</param>
		/// <param name="dataFieldCollectionIndex">The index of the data field that the calculator is calculating.</param>
		/// <param name="totalsCalculator">A <see cref="TotalsFunctionHelper"/> to calculate values with.</param>
		public PercentOfParentCalculator(ExcelPivotTable pivotTable, int dataFieldCollectionIndex, TotalsFunctionHelper totalsCalculator) 
			: base(pivotTable, dataFieldCollectionIndex, totalsCalculator) { }
		#endregion

		#region ShowDataAsCalculatorBase Overrides
		/// <summary>
		/// Calculates a body value in a pivot table cell.
		/// </summary>
		/// <param name="dataRow">The row in the backing body data.</param>
		/// <param name="dataColumn">The column in the backing body data.</param>
		/// <param name="backingDatas">The backing data for the pivot table body.</param>
		/// <param name="grandGrandTotalValues">The backing data for the pivot table grand grand totals.</param>
		/// <param name="rowGrandTotalsValuesLists">The backing data for the pivot table row grand totals.</param>
		/// <param name="columnGrandTotalsValuesLists">The backing data for the pivot table column grand totals.</param>
		/// <returns>An object value for the cell.</returns>
		public override object CalculateBodyValue(
			int dataRow, int dataColumn,
			PivotCellBackingData[,] backingDatas,
			PivotCellBackingData[] grandGrandTotalValues,
			List<PivotCellBackingData> rowGrandTotalsValuesLists,
			List<PivotCellBackingData> columnGrandTotalsValuesLists)
		{
			var rowHeader = base.PivotTable.RowHeaders[dataRow];
			var columnHeader = base.PivotTable.ColumnHeaders[dataColumn];
			var cellBackingData = backingDatas[dataRow, dataColumn];
			var dataField = base.PivotTable.DataFields[base.DataFieldCollectionIndex];

			if (!base.PivotTable.RowFields.Any(f => f.Index == dataField.BaseField)
					&& !base.PivotTable.ColumnFields.Any(f => f.Index == dataField.BaseField))
			{
				// If the dataField.BaseField is not a row or column field, all values #N/A!
				return ExcelErrorValue.Create(eErrorType.NA);
			}

			// Find the index of the parent in the row header's cacheRecordIndices.
			var parentIndicesIndex = rowHeader.CacheRecordIndices.FindIndex(t => t.Item1 == dataField.BaseField);
			if (parentIndicesIndex == -1)
			{
				// Find the index of the parent in the column header's cacheRecordIndices.
				parentIndicesIndex = columnHeader.CacheRecordIndices.FindIndex(t => t.Item1 == dataField.BaseField);
				// The current cell is above the parent field so it does not get a value.
				if (parentIndicesIndex == -1)
					return null;
				var parentColumnHeaderIndices = columnHeader.CacheRecordIndices.Take(parentIndicesIndex + 1).ToList();
				return base.CalculateBodyValue(false, dataRow, dataColumn, parentColumnHeaderIndices, backingDatas);
			}
			else
			{
				// Find the index of the parent in the row header's cacheRecordIndices.
				var parentRowHeaderIndices = rowHeader.CacheRecordIndices.Take(parentIndicesIndex + 1).ToList();
				return base.CalculateBodyValue(true, dataRow, dataColumn, parentRowHeaderIndices, backingDatas);
			}
		}

		/// <summary>
		/// Calculates the grand total value in a pivot table cell.
		/// </summary>
		/// <param name="index">The index into the backing data.</param>
		/// <param name="grandTotalsBackingDatas">The backing data for grand totals.</param>
		/// <param name="columnGrandGrandTotalValues">The backing data for the column grand grand totals.</param>
		/// <param name="isRowTotal">A value indicating whether or not this calculation is for row totals.</param>
		/// <returns>An object value for the cell.</returns>
		public override object CalculateGrandTotalValue(
			int index,
			List<PivotCellBackingData> grandTotalsBackingDatas,
			PivotCellBackingData[] columnGrandGrandTotalValues,
			bool isRowTotal)
		{
			var dataField = base.PivotTable.DataFields[base.DataFieldCollectionIndex];
			if (!base.PivotTable.RowFields.Any(f => f.Index == dataField.BaseField)
					&& !base.PivotTable.ColumnFields.Any(f => f.Index == dataField.BaseField))
			{
				// If the dataField.BaseField is not a row or column field, all values #N/A!
				return ExcelErrorValue.Create(eErrorType.NA);
			}

			// If the base field is a row field and this is a row grand total
			// or the base field is a column field and this is a column grand total
			// then no value is written out.
			if ((base.PivotTable.RowFields.Any(r => r.Index == dataField.BaseField) && isRowTotal)
				|| (base.PivotTable.ColumnFields.Any(r => r.Index == dataField.BaseField) && !isRowTotal))
			{
				return null;
			}

			var cellBackingData = grandTotalsBackingDatas[index];
			var headers = isRowTotal ? base.PivotTable.ColumnHeaders : base.PivotTable.RowHeaders;
			var currentHeader = headers[cellBackingData.MajorAxisIndex];

			// If the current header is the base field, the value is 1.
			if (currentHeader.CacheRecordIndices?.Last()?.Item1 == dataField.BaseField)
				return 1;

			return base.CalculateGrandTotalValue(headers, cellBackingData, isRowTotal);
		}

		/// <summary>
		/// Calculates a grand grand total value for a pivot table cell.
		/// </summary>
		/// <param name="backingData">The backing data for the grand total cell to calculate.</param>
		/// <returns>An object value for the cell.</returns>
		public override object CalculateGrandGrandTotalValue(PivotCellBackingData backingData)
		{
			var dataField = base.PivotTable.DataFields[base.DataFieldCollectionIndex];
			if (!base.PivotTable.RowFields.Any(f => f.Index == dataField.BaseField)
					&& !base.PivotTable.ColumnFields.Any(f => f.Index == dataField.BaseField))
			{
				// If the dataField.BaseField is not a row or column field, all values #N/A!
				return ExcelErrorValue.Create(eErrorType.NA);
			}
			return null;
		}
		#endregion

		#region Private Methods
		private List<int> FindSiblings(List<PivotTableHeader> headers, List<Tuple<int, int>> childIndices)
		{
			var siblingHeaderIndices = new List<int>();
			for (int i = 0; i < headers.Count; i++)
			{
				if (this.IsSibling(headers[i], childIndices))
					siblingHeaderIndices.Add(i);
			}
			return siblingHeaderIndices;
		}

		private bool IsSibling(PivotTableHeader header, List<Tuple<int, int>> childIndices)
		{
			var possibleSiblingIndices = header.CacheRecordIndices;
			if (childIndices == null || possibleSiblingIndices == null || childIndices.Count <= 1 || childIndices.Count != possibleSiblingIndices.Count)
				return false;
			for (int i = 0; i < childIndices.Count - 1; i++)
			{
				if (childIndices[i] != possibleSiblingIndices[i])
					return false;
			}
			return true;
		}

		private bool IndexMatch(List<Tuple<int, int>> parentIndices, List<Tuple<int, int>> containsIndices)
		{
			if (parentIndices.Count < containsIndices.Count)
				return false;
			for (int i = 0; i < containsIndices.Count; i++)
			{
				if (parentIndices[i] != containsIndices[i])
					return false;
			}
			return true;
		}
		#endregion
	}
}
