﻿/*******************************************************************************
* You may amend and distribute as you like, but don't remove this header!
*
* EPPlus provides server-side generation of Excel 2007/2010 spreadsheets.
* See http://www.codeplex.com/EPPlus for details.
*
* Copyright (C) 2011-2018 Michelle Lau, Evan Schallerer, and others as noted in the source history.
*
* This library is free software; you can redistribute it and/or
* modify it under the terms of the GNU Lesser General Public
* License as published by the Free Software Foundation; either
* version 2.1 of the License, or (at your option) any later version.
* This library is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
* See the GNU Lesser General Public License for more details.
*
* The GNU Lesser General Public License can be viewed at http://www.opensource.org/licenses/lgpl-license.php
* If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
*
* All code and executables are provided "as is" with no warranty either express or implied. 
* The author accepts no liability for any damage or loss of business that this product may cause.
*
* For code change notes, see the source control history.
*******************************************************************************/
using System.IO;
using System.Linq;
using EPPlusTest.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfficeOpenXml.Table.PivotTable;

namespace EPPlusTest.Table.PivotTable
{
	[TestClass]
	public class ExcelPivotTableTest
	{
		#region Integration Tests
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTablesWorksheetSources.xlsx")]
		public void PivotTableXmlLoadsCorrectly()
		{
			var testFile = new FileInfo(@"PivotTablesWorksheetSources.xlsx");
			var tempFile = new FileInfo(Path.GetTempFileName());
			if (tempFile.Exists)
				tempFile.Delete();
			testFile.CopyTo(tempFile.FullName);
			try
			{
				using (var package = new ExcelPackage(tempFile))
				{
					Assert.AreEqual(2, package.Workbook.PivotCacheDefinitions.Count());

					var cacheRecords1 = package.Workbook.PivotCacheDefinitions[0].CacheRecords;
					var cacheRecords2 = package.Workbook.PivotCacheDefinitions[1].CacheRecords;

					Assert.AreNotEqual(cacheRecords1, cacheRecords2);
					Assert.AreEqual(22, cacheRecords1.Count);
					Assert.AreEqual(36, cacheRecords2.Count);
					Assert.AreEqual(cacheRecords1.Count, cacheRecords1.Count);
					Assert.AreEqual(cacheRecords2.Count, cacheRecords2.Count);

					var worksheet1 = package.Workbook.Worksheets["sheet1"];
					var worksheet2 = package.Workbook.Worksheets["sheet2"];
					var worksheet3 = package.Workbook.Worksheets["sheet3"];

					Assert.AreEqual(0, worksheet1.PivotTables.Count());
					Assert.AreEqual(2, worksheet2.PivotTables.Count());
					Assert.AreEqual(1, worksheet3.PivotTables.Count());

					Assert.AreEqual(worksheet2.PivotTables[0].CacheDefinition, worksheet2.PivotTables[1].CacheDefinition);
					Assert.AreNotEqual(worksheet2.PivotTables[0].CacheDefinition, worksheet3.PivotTables[0].CacheDefinition);
				}
			}
			finally
			{
				tempFile.Delete();
			}
		}
		#endregion

		#region Refresh Tests
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWorksheetSource.xlsx")]
		public void PivotTableRefreshFromCacheWithChangedData()
		{
			var file = new FileInfo("PivotTableWorksheetSource.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets.First();
					var pivotTable = worksheet.PivotTables.First();
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					worksheet.Cells[4, 5].Value = "Blue";
					worksheet.Cells[5, 5].Value = "Green";
					worksheet.Cells[6, 5].Value = "Purple";
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("I10:J17"), pivotTable.Address);
					Assert.AreEqual(4, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(7, pivotTable.RowItems.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 11, 9, "Blue"),
					new ExpectedCellValue(sheetName, 12, 9, "Bike"),
					new ExpectedCellValue(sheetName, 13, 9, "Green"),
					new ExpectedCellValue(sheetName, 14, 9, "Car"),
					new ExpectedCellValue(sheetName, 15, 9, "Purple"),
					new ExpectedCellValue(sheetName, 16, 9, "Skateboard"),
					new ExpectedCellValue(sheetName, 17, 9, "Grand Total"),

					new ExpectedCellValue(sheetName, 10, 10, "Sum of Cost"),
					new ExpectedCellValue(sheetName, 11, 10, 100d),
					new ExpectedCellValue(sheetName, 12, 10, 100d),
					new ExpectedCellValue(sheetName, 13, 10, 90000d),
					new ExpectedCellValue(sheetName, 14, 10, 90000d),
					new ExpectedCellValue(sheetName, 15, 10, 10d),
					new ExpectedCellValue(sheetName, 16, 10, 10d),
					new ExpectedCellValue(sheetName, 17, 10, 90110d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWorksheetSource.xlsx")]
		public void PivotTableRefreshFromCacheWithAddedData()
		{
			var file = new FileInfo("PivotTableWorksheetSource.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets.First();
					var pivotTable = worksheet.PivotTables.First();
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					worksheet.Cells[7, 3].Value = 4;
					worksheet.Cells[7, 4].Value = "Scooter";
					worksheet.Cells[7, 5].Value = "Purple";
					worksheet.Cells[7, 6].Value = 28;
					cacheDefinition.SetSourceRangeAddress(worksheet, worksheet.Cells["C3:F7"]);
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("I10:J18"), pivotTable.Address);
					Assert.AreEqual(4, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(8, pivotTable.RowItems.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 11, 9, "Black"),
					new ExpectedCellValue(sheetName, 12, 9, "Bike"),
					new ExpectedCellValue(sheetName, 13, 9, "Skateboard"),
					new ExpectedCellValue(sheetName, 14, 9, "Purple"),
					new ExpectedCellValue(sheetName, 15, 9, "Scooter"),
					new ExpectedCellValue(sheetName, 16, 9, "Red"),
					new ExpectedCellValue(sheetName, 17, 9, "Car"),
					new ExpectedCellValue(sheetName, 18, 9, "Grand Total"),

					new ExpectedCellValue(sheetName, 10, 10, "Sum of Cost"),
					new ExpectedCellValue(sheetName, 11, 10, 110d),
					new ExpectedCellValue(sheetName, 12, 10, 100d),
					new ExpectedCellValue(sheetName, 13, 10, 10d),
					new ExpectedCellValue(sheetName, 14, 10, 28d),
					new ExpectedCellValue(sheetName, 15, 10, 28d),
					new ExpectedCellValue(sheetName, 16, 10, 90000d),
					new ExpectedCellValue(sheetName, 17, 10, 90000d),
					new ExpectedCellValue(sheetName, 18, 10, 90138d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWorksheetSource.xlsx")]
		public void PivotTableRefreshFromCacheRemoveRow()
		{
			var file = new FileInfo("PivotTableWorksheetSource.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets.First();
					var pivotTable = worksheet.PivotTables.First();
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.SetSourceRangeAddress(worksheet, worksheet.Cells["C3:F5"]);
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("I10:J15"), pivotTable.Address);
					Assert.AreEqual(4, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(5, pivotTable.RowItems.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 11, 9, "Black"),
					new ExpectedCellValue(sheetName, 12, 9, "Bike"),
					new ExpectedCellValue(sheetName, 13, 9, "Red"),
					new ExpectedCellValue(sheetName, 14, 9, "Car"),
					new ExpectedCellValue(sheetName, 15, 9, "Grand Total"),

					new ExpectedCellValue(sheetName, 10, 10, "Sum of Cost"),
					new ExpectedCellValue(sheetName, 11, 10, 100d),
					new ExpectedCellValue(sheetName, 12, 10, 100d),
					new ExpectedCellValue(sheetName, 13, 10, 90000d),
					new ExpectedCellValue(sheetName, 14, 10, 90000d),
					new ExpectedCellValue(sheetName, 15, 10, 90100d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnFields.xlsx")]
		public void PivotTableRefreshColumnItemsWithChangedData()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets.First();
					var pivotTable = worksheet.PivotTables.First();
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					worksheet.Cells[4, 3].Value = "January";
					worksheet.Cells[7, 3].Value = "January";
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B12:O23"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 16, 2, "20100007"),
					new ExpectedCellValue(sheetName, 17, 2, "20100017"),
					new ExpectedCellValue(sheetName, 18, 2, "20100070"),
					new ExpectedCellValue(sheetName, 19, 2, "20100076"),
					new ExpectedCellValue(sheetName, 20, 2, "20100083"),
					new ExpectedCellValue(sheetName, 21, 2, "20100085"),
					new ExpectedCellValue(sheetName, 22, 2, "20100090"),
					new ExpectedCellValue(sheetName, 23, 2, "Grand Total"),
					new ExpectedCellValue(sheetName, 13, 3, "January"),
					new ExpectedCellValue(sheetName, 14, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 15, 3, "Chicago"),
					new ExpectedCellValue(sheetName, 16, 3, 415.75),
					new ExpectedCellValue(sheetName, 23, 3, 415.75),
					new ExpectedCellValue(sheetName, 15, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 17, 4, 415.75),
					new ExpectedCellValue(sheetName, 22, 4, 415.75),
					new ExpectedCellValue(sheetName, 23, 4, 831.5),
					new ExpectedCellValue(sheetName, 15, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 19, 5, 415.75),
					new ExpectedCellValue(sheetName, 23, 5, 415.75),
					new ExpectedCellValue(sheetName, 14, 6, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 16, 6, 415.75),
					new ExpectedCellValue(sheetName, 17, 6, 415.75),
					new ExpectedCellValue(sheetName, 19, 6, 415.75),
					new ExpectedCellValue(sheetName, 22, 6, 415.75),
					new ExpectedCellValue(sheetName, 23, 6, 1663d),
					new ExpectedCellValue(sheetName, 14, 7, "Headlamp"),
					new ExpectedCellValue(sheetName, 15, 7, "Chicago"),
					new ExpectedCellValue(sheetName, 20, 7, 24.99),
					new ExpectedCellValue(sheetName, 23, 7, 24.99),
					new ExpectedCellValue(sheetName, 14, 8, "Headlamp Total"),
					new ExpectedCellValue(sheetName, 20, 8, 24.99),
					new ExpectedCellValue(sheetName, 23, 8, 24.99),
					new ExpectedCellValue(sheetName, 13, 9, "January Total"),
					new ExpectedCellValue(sheetName, 16, 9, 415.75),
					new ExpectedCellValue(sheetName, 17, 9, 415.75),
					new ExpectedCellValue(sheetName, 19, 9, 415.75),
					new ExpectedCellValue(sheetName, 20, 9, 24.99),
					new ExpectedCellValue(sheetName, 22, 9, 415.75),
					new ExpectedCellValue(sheetName, 23, 9, 1687.99),
					new ExpectedCellValue(sheetName, 13, 10, "February"),
					new ExpectedCellValue(sheetName, 14, 10, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 15, 10, "San Francisco"),
					new ExpectedCellValue(sheetName, 21, 10, 99d),
					new ExpectedCellValue(sheetName, 23, 10, 99d),
					new ExpectedCellValue(sheetName, 14, 11, "Sleeping Bag Total"),
					new ExpectedCellValue(sheetName, 21, 11, 99d),
					new ExpectedCellValue(sheetName, 23, 11, 99d),
					new ExpectedCellValue(sheetName, 14, 12, "Tent"),
					new ExpectedCellValue(sheetName, 15, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 18, 12, 199d),
					new ExpectedCellValue(sheetName, 23, 12, 199d),
					new ExpectedCellValue(sheetName, 14, 13, "Tent Total"),
					new ExpectedCellValue(sheetName, 18, 13, 199d),
					new ExpectedCellValue(sheetName, 13, 14, "February Total"),
					new ExpectedCellValue(sheetName, 18, 14, 199d),
					new ExpectedCellValue(sheetName, 21, 14, 99d),
					new ExpectedCellValue(sheetName, 23, 14, 298d),
					new ExpectedCellValue(sheetName, 13, 15, "Grand Total"),
					new ExpectedCellValue(sheetName, 16, 15, 415.75),
					new ExpectedCellValue(sheetName, 17, 15, 415.75),
					new ExpectedCellValue(sheetName, 18, 15, 199d),
					new ExpectedCellValue(sheetName, 19, 15, 415.75),
					new ExpectedCellValue(sheetName, 20, 15, 24.99),
					new ExpectedCellValue(sheetName, 21, 15, 99d),
					new ExpectedCellValue(sheetName, 22, 15, 415.75),
					new ExpectedCellValue(sheetName, 23, 15, 1985.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnFields.xlsx")]
		public void PivotTableRefreshColumnItemsWithAddedData()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets.First();
					var pivotTable = worksheet.PivotTables.First();
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					worksheet.Cells[9, 1].Value = 20100091;
					worksheet.Cells[9, 2].Value = "Texas";
					worksheet.Cells[9, 3].Value = "December";
					worksheet.Cells[9, 4].Value = "Bike";
					worksheet.Cells[9, 5].Value = 20;
					worksheet.Cells[9, 6].Value = 1;
					worksheet.Cells[9, 7].Value = 20;
					cacheDefinition.SetSourceRangeAddress(worksheet, worksheet.Cells["A1:G9"]);
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B12:U24"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(9, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 16, 2, "20100007"),
					new ExpectedCellValue(sheetName, 17, 2, "20100017"),
					new ExpectedCellValue(sheetName, 18, 2, "20100070"),
					new ExpectedCellValue(sheetName, 19, 2, "20100076"),
					new ExpectedCellValue(sheetName, 20, 2, "20100083"),
					new ExpectedCellValue(sheetName, 21, 2, "20100085"),
					new ExpectedCellValue(sheetName, 22, 2, "20100090"),
					new ExpectedCellValue(sheetName, 23, 2, "20100091"),
					new ExpectedCellValue(sheetName, 24, 2, "Grand Total"),
					new ExpectedCellValue(sheetName, 13, 3, "January"),
					new ExpectedCellValue(sheetName, 14, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 15, 3, "Chicago"),
					new ExpectedCellValue(sheetName, 16, 3, 415.75),
					new ExpectedCellValue(sheetName, 24, 3, 415.75),
					new ExpectedCellValue(sheetName, 15, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 22, 4, 415.75),
					new ExpectedCellValue(sheetName, 24, 4, 415.75),
					new ExpectedCellValue(sheetName, 15, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 19, 5, 415.75),
					new ExpectedCellValue(sheetName, 24, 5, 415.75),
					new ExpectedCellValue(sheetName, 14, 6, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 16, 6, 415.75),
					new ExpectedCellValue(sheetName, 19, 6, 415.75),
					new ExpectedCellValue(sheetName, 22, 6, 415.75),
					new ExpectedCellValue(sheetName, 24, 6, 1247.25),
					new ExpectedCellValue(sheetName, 13, 7, "January Total"),
					new ExpectedCellValue(sheetName, 16, 7, 415.75),
					new ExpectedCellValue(sheetName, 19, 7, 415.75),
					new ExpectedCellValue(sheetName, 22, 7, 415.75),
					new ExpectedCellValue(sheetName, 24, 7, 1247.25),
					new ExpectedCellValue(sheetName, 13, 8, "February"),
					new ExpectedCellValue(sheetName, 14, 8, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 15, 8, "San Francisco"),
					new ExpectedCellValue(sheetName, 21, 8, 99d),
					new ExpectedCellValue(sheetName, 24, 8, 99d),
					new ExpectedCellValue(sheetName, 14, 9, "Sleeping Bag Total"),
					new ExpectedCellValue(sheetName, 21, 9, 99d),
					new ExpectedCellValue(sheetName, 24, 9, 99d),
					new ExpectedCellValue(sheetName, 14, 10, "Tent"),
					new ExpectedCellValue(sheetName, 15, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 18, 10, 199d),
					new ExpectedCellValue(sheetName, 24, 10, 199d),
					new ExpectedCellValue(sheetName, 14, 11, "Tent Total"),
					new ExpectedCellValue(sheetName, 18, 11, 199d),
					new ExpectedCellValue(sheetName, 24, 11, 199d),
					new ExpectedCellValue(sheetName, 13, 12, "February Total"),
					new ExpectedCellValue(sheetName, 18, 12, 199d),
					new ExpectedCellValue(sheetName, 21, 12, 99d),
					new ExpectedCellValue(sheetName, 24, 12, 298d),
					new ExpectedCellValue(sheetName, 13, 13, "March"),
					new ExpectedCellValue(sheetName, 14, 13, "Car Rack"),
					new ExpectedCellValue(sheetName, 15, 13, "Nashville"),
					new ExpectedCellValue(sheetName, 17, 13, 415.75),
					new ExpectedCellValue(sheetName, 24, 13, 415.75),
					new ExpectedCellValue(sheetName, 14, 14, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 17, 14, 415.75),
					new ExpectedCellValue(sheetName, 24, 14, 415.75),
					new ExpectedCellValue(sheetName, 14, 15, "Headlamp"),
					new ExpectedCellValue(sheetName, 15, 15, "Chicago"),
					new ExpectedCellValue(sheetName, 20, 15, 24.99),
					new ExpectedCellValue(sheetName, 24, 15, 24.99),
					new ExpectedCellValue(sheetName, 14, 16, "Headlamp Total"),
					new ExpectedCellValue(sheetName, 20, 16, 24.99),
					new ExpectedCellValue(sheetName, 24, 16, 24.99),
					new ExpectedCellValue(sheetName, 13, 17, "March Total"),
					new ExpectedCellValue(sheetName, 17, 17, 415.75),
					new ExpectedCellValue(sheetName, 20, 17, 24.99),
					new ExpectedCellValue(sheetName, 24, 17, 440.74),
					new ExpectedCellValue(sheetName, 13, 18, "December"),
					new ExpectedCellValue(sheetName, 14, 18, "Bike"),
					new ExpectedCellValue(sheetName, 15, 18, "Texas"),
					new ExpectedCellValue(sheetName, 23, 18, 20d),
					new ExpectedCellValue(sheetName, 24, 18, 20d),
					new ExpectedCellValue(sheetName, 14, 19, "Bike Total"),
					new ExpectedCellValue(sheetName, 23, 19, 20d),
					new ExpectedCellValue(sheetName, 24, 19, 20d),
					new ExpectedCellValue(sheetName, 13, 20, "December Total"),
					new ExpectedCellValue(sheetName, 23, 20, 20d),
					new ExpectedCellValue(sheetName, 24, 20, 20d),
					new ExpectedCellValue(sheetName, 13, 21, "Grand Total"),
					new ExpectedCellValue(sheetName, 16, 21, 415.75),
					new ExpectedCellValue(sheetName, 17, 21, 415.75),
					new ExpectedCellValue(sheetName, 18, 21, 199d),
					new ExpectedCellValue(sheetName, 19, 21, 415.75),
					new ExpectedCellValue(sheetName, 20, 21, 24.99),
					new ExpectedCellValue(sheetName, 21, 21, 99d),
					new ExpectedCellValue(sheetName, 22, 21, 415.75),
					new ExpectedCellValue(sheetName, 23, 21, 20d),
					new ExpectedCellValue(sheetName, 24, 21, 2005.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnFields.xlsx")]
		public void PivotTableRefreshColumnItemsWithRemoveData()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets.First();
					var pivotTable = worksheet.PivotTables.First();
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.SetSourceRangeAddress(worksheet, worksheet.Cells["A1:G5"]);
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B12:M20"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 16, 2, "20100007"),
					new ExpectedCellValue(sheetName, 17, 2, "20100076"),
					new ExpectedCellValue(sheetName, 18, 2, "20100083"),
					new ExpectedCellValue(sheetName, 19, 2, "20100085"),
					new ExpectedCellValue(sheetName, 20, 2, "Grand Total"),
					new ExpectedCellValue(sheetName, 13, 3, "January"),
					new ExpectedCellValue(sheetName, 14, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 15, 3, "Chicago"),
					new ExpectedCellValue(sheetName, 16, 3, 415.75),
					new ExpectedCellValue(sheetName, 20, 3, 415.75),
					new ExpectedCellValue(sheetName, 15, 4, "San Francisco"),
					new ExpectedCellValue(sheetName, 17, 4, 415.75),
					new ExpectedCellValue(sheetName, 20, 4, 415.75),
					new ExpectedCellValue(sheetName, 14, 5, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 16, 5, 415.75),
					new ExpectedCellValue(sheetName, 17, 5, 415.75),
					new ExpectedCellValue(sheetName, 20, 5, 831.5),
					new ExpectedCellValue(sheetName, 13, 6, "January Total"),
					new ExpectedCellValue(sheetName, 16, 6, 415.75),
					new ExpectedCellValue(sheetName, 17, 6, 415.75),
					new ExpectedCellValue(sheetName, 20, 6, 831.5),
					new ExpectedCellValue(sheetName, 13, 7, "February"),
					new ExpectedCellValue(sheetName, 14, 7, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 15, 7, "San Francisco"),
					new ExpectedCellValue(sheetName, 19, 7, 99d),
					new ExpectedCellValue(sheetName, 20, 7, 99d),
					new ExpectedCellValue(sheetName, 14, 8, "Sleeping Bag Total"),
					new ExpectedCellValue(sheetName, 19, 8, 99d),
					new ExpectedCellValue(sheetName, 20, 8, 99d),
					new ExpectedCellValue(sheetName, 13, 9, "February Total"),
					new ExpectedCellValue(sheetName, 19, 9, 99d),
					new ExpectedCellValue(sheetName, 20, 9, 99d),
					new ExpectedCellValue(sheetName, 13, 10, "March"),
					new ExpectedCellValue(sheetName, 14, 10, "Headlamp"),
					new ExpectedCellValue(sheetName, 15, 10, "Chicago"),
					new ExpectedCellValue(sheetName, 18, 10, 24.99),
					new ExpectedCellValue(sheetName, 20, 10, 24.99),
					new ExpectedCellValue(sheetName, 14, 11, "Headlamp Total"),
					new ExpectedCellValue(sheetName, 18, 11, 24.99),
					new ExpectedCellValue(sheetName, 20, 11, 24.99),
					new ExpectedCellValue(sheetName, 13, 12, "March Total"),
					new ExpectedCellValue(sheetName, 18, 12, 24.99),
					new ExpectedCellValue(sheetName, 20, 12, 24.99),
					new ExpectedCellValue(sheetName, 13, 13, "Grand Total"),
					new ExpectedCellValue(sheetName, 16, 13, 415.75),
					new ExpectedCellValue(sheetName, 17, 13, 415.75),
					new ExpectedCellValue(sheetName, 18, 13, 24.99),
					new ExpectedCellValue(sheetName, 19, 13, 99d),
					new ExpectedCellValue(sheetName, 20, 13, 955.49)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnFields.xlsx")]
		public void PivotTableRefreshDeletingSourceRow()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets.First();
					var pivotTable = worksheet.PivotTables.First();
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					worksheet.DeleteRow(6);
					cacheDefinition.SetSourceRangeAddress(worksheet, worksheet.Cells["A1:G7"]);
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B11:P21"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 15, 2, "20100007"),
					new ExpectedCellValue(sheetName, 16, 2, "20100017"),
					new ExpectedCellValue(sheetName, 17, 2, "20100076"),
					new ExpectedCellValue(sheetName, 18, 2, "20100083"),
					new ExpectedCellValue(sheetName, 19, 2, "20100085"),
					new ExpectedCellValue(sheetName, 20, 2, "20100090"),
					new ExpectedCellValue(sheetName, 21, 2, "Grand Total"),
					new ExpectedCellValue(sheetName, 12, 3, "January"),
					new ExpectedCellValue(sheetName, 13, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 14, 3, "Chicago"),
					new ExpectedCellValue(sheetName, 15, 3, 415.75),
					new ExpectedCellValue(sheetName, 21, 3, 415.75),
					new ExpectedCellValue(sheetName, 14, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 20, 4, 415.75),
					new ExpectedCellValue(sheetName, 21, 4, 415.75),
					new ExpectedCellValue(sheetName, 14, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 17, 5, 415.75),
					new ExpectedCellValue(sheetName, 21, 5, 415.75),
					new ExpectedCellValue(sheetName, 13, 6, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 15, 6, 415.75),
					new ExpectedCellValue(sheetName, 17, 6, 415.75),
					new ExpectedCellValue(sheetName, 20, 6, 415.75),
					new ExpectedCellValue(sheetName, 21, 6, 1247.25),
					new ExpectedCellValue(sheetName, 12, 7, "January Total"),
					new ExpectedCellValue(sheetName, 15, 7, 415.75),
					new ExpectedCellValue(sheetName, 17, 7, 415.75),
					new ExpectedCellValue(sheetName, 20, 7, 415.75),
					new ExpectedCellValue(sheetName, 21, 7, 1247.25),
					new ExpectedCellValue(sheetName, 12, 8, "February"),
					new ExpectedCellValue(sheetName, 13, 8, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 14, 8, "San Francisco"),
					new ExpectedCellValue(sheetName, 19, 8, 99d),
					new ExpectedCellValue(sheetName, 21, 8, 99d),
					new ExpectedCellValue(sheetName, 13, 9, "Sleeping Bag Total"),
					new ExpectedCellValue(sheetName, 19, 9, 99d),
					new ExpectedCellValue(sheetName, 21, 9, 99d),
					new ExpectedCellValue(sheetName, 12, 10, "February Total"),
					new ExpectedCellValue(sheetName, 19, 10, 99d),
					new ExpectedCellValue(sheetName, 21, 10, 99d),
					new ExpectedCellValue(sheetName, 12, 11, "March"),
					new ExpectedCellValue(sheetName, 13, 11, "Car Rack"),
					new ExpectedCellValue(sheetName, 14, 11, "Nashville"),
					new ExpectedCellValue(sheetName, 16, 11, 415.75),
					new ExpectedCellValue(sheetName, 21, 11, 415.75),
					new ExpectedCellValue(sheetName, 13, 12, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 16, 12, 415.75),
					new ExpectedCellValue(sheetName, 21, 12, 415.75),
					new ExpectedCellValue(sheetName, 13, 13, "Headlamp"),
					new ExpectedCellValue(sheetName, 14, 13, "Chicago"),
					new ExpectedCellValue(sheetName, 18, 13, 24.99),
					new ExpectedCellValue(sheetName, 21, 13, 24.99),
					new ExpectedCellValue(sheetName, 13, 14, "Headlamp Total"),
					new ExpectedCellValue(sheetName, 18, 14, 24.99),
					new ExpectedCellValue(sheetName, 21, 14, 24.99),
					new ExpectedCellValue(sheetName, 12, 15, "March Total"),
					new ExpectedCellValue(sheetName, 16, 15, 415.75),
					new ExpectedCellValue(sheetName, 18, 15, 24.99),
					new ExpectedCellValue(sheetName, 21, 15, 440.74),
					new ExpectedCellValue(sheetName, 12, 16, "Grand Total"),
					new ExpectedCellValue(sheetName, 15, 16, 415.75),
					new ExpectedCellValue(sheetName, 16, 16, 415.75),
					new ExpectedCellValue(sheetName, 17, 16, 415.75),
					new ExpectedCellValue(sheetName, 18, 16, 24.99),
					new ExpectedCellValue(sheetName, 19, 16, 99d),
					new ExpectedCellValue(sheetName, 20, 16, 415.75),
					new ExpectedCellValue(sheetName, 21, 16, 1786.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowFieldsColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleRowFieldsColumnDataFields()
		{
			var file = new FileInfo("PivotTableWithMultipleRowFieldsColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B2:D19"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 2, "Row Labels"),
					new ExpectedCellValue(sheetName, 2, 3, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 2, 4, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 3, 2, "January"),
					new ExpectedCellValue(sheetName, 3, 3, null),
					new ExpectedCellValue(sheetName, 3, 4, null),
					new ExpectedCellValue(sheetName, 4, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 4, 3, null),
					new ExpectedCellValue(sheetName, 4, 4, null),
					new ExpectedCellValue(sheetName, 5, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 3, 415.75),
					new ExpectedCellValue(sheetName, 5, 4, 2),
					new ExpectedCellValue(sheetName, 6, 2, "Nashville"),
					new ExpectedCellValue(sheetName, 6, 3, 831.5),
					new ExpectedCellValue(sheetName, 6, 4, 4),
					new ExpectedCellValue(sheetName, 7, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 7, 3, 415.75),
					new ExpectedCellValue(sheetName, 7, 4, 1),
					new ExpectedCellValue(sheetName, 8, 2, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 8, 3, 1663),
					new ExpectedCellValue(sheetName, 8, 4, 7),
					new ExpectedCellValue(sheetName, 9, 2, "January Total"),
					new ExpectedCellValue(sheetName, 9, 3, 1663),
					new ExpectedCellValue(sheetName, 9, 4, 7),
					new ExpectedCellValue(sheetName, 10, 2, "February"),
					new ExpectedCellValue(sheetName, 10, 3, null),
					new ExpectedCellValue(sheetName, 10, 4, null),
					new ExpectedCellValue(sheetName, 11, 2, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 11, 3, null),
					new ExpectedCellValue(sheetName, 11, 4, null),
					new ExpectedCellValue(sheetName, 12, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 12, 3, 99),
					new ExpectedCellValue(sheetName, 12, 4, 1),
					new ExpectedCellValue(sheetName, 13, 2, "Sleeping Bag Total"),
					new ExpectedCellValue(sheetName, 13, 3, 99),
					new ExpectedCellValue(sheetName, 13, 4, 1),
					new ExpectedCellValue(sheetName, 14, 2, "Tent"),
					new ExpectedCellValue(sheetName, 14, 3, null),
					new ExpectedCellValue(sheetName, 14, 4, null),
					new ExpectedCellValue(sheetName, 15, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 15, 3, 24.99),
					new ExpectedCellValue(sheetName, 15, 4, 1),
					new ExpectedCellValue(sheetName, 16, 2, "Nashville"),
					new ExpectedCellValue(sheetName, 16, 3, 199),
					new ExpectedCellValue(sheetName, 16, 4, 6),
					new ExpectedCellValue(sheetName, 17, 2, "Tent Total"),
					new ExpectedCellValue(sheetName, 17, 3, 223.99),
					new ExpectedCellValue(sheetName, 17, 4, 7),
					new ExpectedCellValue(sheetName, 18, 2, "February Total"),
					new ExpectedCellValue(sheetName, 18, 3, 322.99),
					new ExpectedCellValue(sheetName, 18, 4, 8),
					new ExpectedCellValue(sheetName, 19, 2, "Grand Total"),
					new ExpectedCellValue(sheetName, 19, 3, 1985.99),
					new ExpectedCellValue(sheetName, 19, 4, 15),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithRowFieldsOnly.xlsx")]
		public void PivotTableRefreshSingleColumnNoDataFields()
		{
			var file = new FileInfo("PivotTableWithRowFieldsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["PivotTables"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:A5"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "PivotTables";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "January"),
					new ExpectedCellValue(sheetName, 3, 1, "February"),
					new ExpectedCellValue(sheetName, 4, 1, "March"),
					new ExpectedCellValue(sheetName, 5, 1, "Grand Total")
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithRowFieldsOnly.xlsx")]
		public void PivotTableRefreshSingleColumnTwoRowFieldsAndNoDataFields()
		{
			var file = new FileInfo("PivotTableWithRowFieldsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["PivotTables"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("E1:E12"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "PivotTables";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 5, "January"),
					new ExpectedCellValue(sheetName, 3, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 6, 5, "February"),
					new ExpectedCellValue(sheetName, 7, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 8, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 9, 5, "March"),
					new ExpectedCellValue(sheetName, 10, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 11, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 12, 5, "Grand Total")
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMAttrbuteFieldItems.xlsx")]
		public void PivotTableRefreshFieldItemsWithMAttributes()
		{
			var file = new FileInfo("PivotTableWithMAttrbuteFieldItems.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Sheet1"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					worksheet.Cells[2, 3].Value = "December";
					worksheet.Cells[5, 3].Value = "December";
					worksheet.Cells[8, 3].Value = "December";
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("C15:D19"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
						{
							foreach (var item in field.Items)
							{
								Assert.IsNull(item.TopNode.Attributes["m"]);
								Assert.AreEqual(1, item.TopNode.Attributes.Count);
							}
						}
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 16, 3, "February"),
					new ExpectedCellValue(sheetName, 17, 3, "March"),
					new ExpectedCellValue(sheetName, 18, 3, "December"),
					new ExpectedCellValue(sheetName, 19, 3, "Grand Total"),
					new ExpectedCellValue(sheetName, 15, 4, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 4, 7d),
					new ExpectedCellValue(sheetName, 17, 4, 3d),
					new ExpectedCellValue(sheetName, 18, 4, 5d),
					new ExpectedCellValue(sheetName, 19, 4, 15d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMonthInSpanish.xlsx")]
		public void PivotTableRefreshSortMonthsInSpanishCorrectly()
		{
			var file = new FileInfo("PivotTableWithMonthInSpanish.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets.First();
					var pivotTable = worksheet.PivotTables.First();
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A13:F25"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 15, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 16, 1, "enero"),
					new ExpectedCellValue(sheetName, 17, 1, "febrero"),
					new ExpectedCellValue(sheetName, 18, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 19, 1, "enero"),
					new ExpectedCellValue(sheetName, 20, 1, "febrero"),
					new ExpectedCellValue(sheetName, 21, 1, "marzo"),
					new ExpectedCellValue(sheetName, 22, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 23, 1, "enero"),
					new ExpectedCellValue(sheetName, 24, 1, "marzo"),
					new ExpectedCellValue(sheetName, 25, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 14, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 15, 2, 831.5),
					new ExpectedCellValue(sheetName, 16, 2, 831.5),
					new ExpectedCellValue(sheetName, 18, 2, 1663),
					new ExpectedCellValue(sheetName, 19, 2, 831.5),
					new ExpectedCellValue(sheetName, 21, 2, 831.5),
					new ExpectedCellValue(sheetName, 22, 2, 415.75),
					new ExpectedCellValue(sheetName, 24, 2, 415.75),
					new ExpectedCellValue(sheetName, 25, 2, 2910.25),
					new ExpectedCellValue(sheetName, 14, 3, "Headlamp"),
					new ExpectedCellValue(sheetName, 15, 3, 24.99),
					new ExpectedCellValue(sheetName, 17, 3, 24.99),
					new ExpectedCellValue(sheetName, 25, 3, 24.99),
					new ExpectedCellValue(sheetName, 14, 4, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 22, 4, 99d),
					new ExpectedCellValue(sheetName, 23, 4, 99d),
					new ExpectedCellValue(sheetName, 25, 4, 99d),
					new ExpectedCellValue(sheetName, 14, 5, "Tent"),
					new ExpectedCellValue(sheetName, 18, 5, 1194d),
					new ExpectedCellValue(sheetName, 20, 5, 1194d),
					new ExpectedCellValue(sheetName, 25, 5, 1194d),
					new ExpectedCellValue(sheetName, 14, 6, "Grand Total"),
					new ExpectedCellValue(sheetName, 15, 6, 856.49),
					new ExpectedCellValue(sheetName, 16, 6, 831.5),
					new ExpectedCellValue(sheetName, 17, 6, 24.99),
					new ExpectedCellValue(sheetName, 18, 6, 2857d),
					new ExpectedCellValue(sheetName, 19, 6, 831.5),
					new ExpectedCellValue(sheetName, 20, 6, 1194d),
					new ExpectedCellValue(sheetName, 21, 6, 831.5),
					new ExpectedCellValue(sheetName, 22, 6, 514.75),
					new ExpectedCellValue(sheetName, 23, 6, 99d),
					new ExpectedCellValue(sheetName, 24, 6, 415.75),
					new ExpectedCellValue(sheetName, 25, 6, 4228.24)

				});
			}
		}

		#region Calculated Fields Tests
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithCalculatedFields.xlsx")]
		public void PivotTableRefreshCalculatedField()
		{
			var file = new FileInfo("PivotTableWithCalculatedFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "Sheet2";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					var formulaCacheField = cacheDefinition.CacheFields.First(c => !string.IsNullOrEmpty(c.Formula));
					formulaCacheField.Formula = "'Wholesale Price'";
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B2:D13"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 2, 4, "Sum of CalculatedField"),
					new ExpectedCellValue(sheetName, 3, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 3, 3, 7),
					new ExpectedCellValue(sheetName, 3, 4, 1663),
					new ExpectedCellValue(sheetName, 4, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 3, 2),
					new ExpectedCellValue(sheetName, 4, 4, 415.75),
					new ExpectedCellValue(sheetName, 5, 2, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 3, 4),
					new ExpectedCellValue(sheetName, 5, 4, 831.5),
					new ExpectedCellValue(sheetName, 6, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 6, 3, 1),
					new ExpectedCellValue(sheetName, 6, 4, 415.75),
					new ExpectedCellValue(sheetName, 7, 2, "Headlamp"),
					new ExpectedCellValue(sheetName, 7, 3, 1),
					new ExpectedCellValue(sheetName, 7, 4, 24.99),
					new ExpectedCellValue(sheetName, 8, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 8, 3, 1),
					new ExpectedCellValue(sheetName, 8, 4, 24.99),
					new ExpectedCellValue(sheetName, 9, 2, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 9, 3, 1),
					new ExpectedCellValue(sheetName, 9, 4, 99),
					new ExpectedCellValue(sheetName, 10, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 10, 3, 1),
					new ExpectedCellValue(sheetName, 10, 4, 99),
					new ExpectedCellValue(sheetName, 11, 2, "Tent"),
					new ExpectedCellValue(sheetName, 11, 3, 6),
					new ExpectedCellValue(sheetName, 11, 4, 199),
					new ExpectedCellValue(sheetName, 12, 2, "Nashville"),
					new ExpectedCellValue(sheetName, 12, 3, 6),
					new ExpectedCellValue(sheetName, 12, 4, 199),
					new ExpectedCellValue(sheetName, 13, 2, "Grand Total"),
					new ExpectedCellValue(sheetName, 13, 3, 15),
					new ExpectedCellValue(sheetName, 13, 4, 1985.99),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithCalculatedFields.xlsx")]
		public void PivotTableRefreshCalculatedFieldFormulaMultipliesFields()
		{
			var file = new FileInfo("PivotTableWithCalculatedFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "Sheet2";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B2:D13"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 2, 4, "Sum of CalculatedField"),
					new ExpectedCellValue(sheetName, 3, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 3, 3, 7),
					new ExpectedCellValue(sheetName, 3, 4, 11641),
					new ExpectedCellValue(sheetName, 4, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 3, 2),
					new ExpectedCellValue(sheetName, 4, 4, 831.5),
					new ExpectedCellValue(sheetName, 5, 2, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 3, 4),
					new ExpectedCellValue(sheetName, 5, 4, 3326),
					new ExpectedCellValue(sheetName, 6, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 6, 3, 1),
					new ExpectedCellValue(sheetName, 6, 4, 415.75),
					new ExpectedCellValue(sheetName, 7, 2, "Headlamp"),
					new ExpectedCellValue(sheetName, 7, 3, 1),
					new ExpectedCellValue(sheetName, 7, 4, 24.99),
					new ExpectedCellValue(sheetName, 8, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 8, 3, 1),
					new ExpectedCellValue(sheetName, 8, 4, 24.99),
					new ExpectedCellValue(sheetName, 9, 2, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 9, 3, 1),
					new ExpectedCellValue(sheetName, 9, 4, 99),
					new ExpectedCellValue(sheetName, 10, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 10, 3, 1),
					new ExpectedCellValue(sheetName, 10, 4, 99),
					new ExpectedCellValue(sheetName, 11, 2, "Tent"),
					new ExpectedCellValue(sheetName, 11, 3, 6),
					new ExpectedCellValue(sheetName, 11, 4, 1194),
					new ExpectedCellValue(sheetName, 12, 2, "Nashville"),
					new ExpectedCellValue(sheetName, 12, 3, 6),
					new ExpectedCellValue(sheetName, 12, 4, 1194),
					new ExpectedCellValue(sheetName, 13, 2, "Grand Total"),
					new ExpectedCellValue(sheetName, 13, 3, 15),
					new ExpectedCellValue(sheetName, 13, 4, 29789.85),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithCalculatedFields.xlsx")]
		public void PivotTableRefreshCalculatedFieldFormulaContainsStringField()
		{
			var file = new FileInfo("PivotTableWithCalculatedFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "Sheet2";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					var formulaCacheField = cacheDefinition.CacheFields.First(c => !string.IsNullOrEmpty(c.Formula));
					formulaCacheField.Formula = "'Wholesale Price' * Item";
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B2:D13"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 2, 4, "Sum of CalculatedField"),
					new ExpectedCellValue(sheetName, 3, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 3, 3, 7),
					new ExpectedCellValue(sheetName, 3, 4, 0),
					new ExpectedCellValue(sheetName, 4, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 3, 2),
					new ExpectedCellValue(sheetName, 4, 4, 0),
					new ExpectedCellValue(sheetName, 5, 2, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 3, 4),
					new ExpectedCellValue(sheetName, 5, 4, 0),
					new ExpectedCellValue(sheetName, 6, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 6, 3, 1),
					new ExpectedCellValue(sheetName, 6, 4, 0),
					new ExpectedCellValue(sheetName, 7, 2, "Headlamp"),
					new ExpectedCellValue(sheetName, 7, 3, 1),
					new ExpectedCellValue(sheetName, 7, 4, 0),
					new ExpectedCellValue(sheetName, 8, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 8, 3, 1),
					new ExpectedCellValue(sheetName, 8, 4, 0),
					new ExpectedCellValue(sheetName, 9, 2, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 9, 3, 1),
					new ExpectedCellValue(sheetName, 9, 4, 0),
					new ExpectedCellValue(sheetName, 10, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 10, 3, 1),
					new ExpectedCellValue(sheetName, 10, 4, 0),
					new ExpectedCellValue(sheetName, 11, 2, "Tent"),
					new ExpectedCellValue(sheetName, 11, 3, 6),
					new ExpectedCellValue(sheetName, 11, 4, 0),
					new ExpectedCellValue(sheetName, 12, 2, "Nashville"),
					new ExpectedCellValue(sheetName, 12, 3, 6),
					new ExpectedCellValue(sheetName, 12, 4, 0),
					new ExpectedCellValue(sheetName, 13, 2, "Grand Total"),
					new ExpectedCellValue(sheetName, 13, 3, 15),
					new ExpectedCellValue(sheetName, 13, 4, 0),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithCalculatedFields.xlsx")]
		public void PivotTableRefreshCalculatedFieldFormulaReferencesOtherCalculatedField()
		{
			var file = new FileInfo("PivotTableWithCalculatedFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "Sheet2";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B18:D27"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 18, 3, "Sum of CalculatedField"),
					new ExpectedCellValue(sheetName, 18, 4, "Sum of CalculatedField2"),
					new ExpectedCellValue(sheetName, 19, 2, "January"),
					new ExpectedCellValue(sheetName, 19, 3, 6236.25),
					new ExpectedCellValue(sheetName, 19, 4, 5),
					new ExpectedCellValue(sheetName, 20, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 20, 3, 6236.25),
					new ExpectedCellValue(sheetName, 20, 4, 5),
					new ExpectedCellValue(sheetName, 21, 2, "February"),
					new ExpectedCellValue(sheetName, 21, 3, 2086.00),
					new ExpectedCellValue(sheetName, 21, 4, 7),
					new ExpectedCellValue(sheetName, 22, 2, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 22, 3, 99d),
					new ExpectedCellValue(sheetName, 22, 4, 1),
					new ExpectedCellValue(sheetName, 23, 2, "Tent"),
					new ExpectedCellValue(sheetName, 23, 3, 1194d),
					new ExpectedCellValue(sheetName, 23, 4, 6),
					new ExpectedCellValue(sheetName, 24, 2, "March"),
					new ExpectedCellValue(sheetName, 24, 3, 1322.22),
					new ExpectedCellValue(sheetName, 24, 4, 3),
					new ExpectedCellValue(sheetName, 25, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 25, 3, 831.5),
					new ExpectedCellValue(sheetName, 25, 4, 2),
					new ExpectedCellValue(sheetName, 26, 2, "Headlamp"),
					new ExpectedCellValue(sheetName, 26, 3, 24.99),
					new ExpectedCellValue(sheetName, 26, 4, 1),
					new ExpectedCellValue(sheetName, 27, 2, "Grand Total"),
					new ExpectedCellValue(sheetName, 27, 3, 29789.85),
					new ExpectedCellValue(sheetName, 27, 4, 15),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithCalculatedFields.xlsx")]
		public void PivotTableRefreshColumnRowAndPageFieldsWithCalculatedField()
		{
			var file = new FileInfo("PivotTableWithCalculatedFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "Sheet2";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("G4:L14"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 7, null),
					new ExpectedCellValue(sheetName, 4, 8, "Column Labels"),
					new ExpectedCellValue(sheetName, 4, 9, null),
					new ExpectedCellValue(sheetName, 4, 10, null),
					new ExpectedCellValue(sheetName, 4, 11, null),
					new ExpectedCellValue(sheetName, 4, 12, null),
					new ExpectedCellValue(sheetName, 5, 7, null),
					new ExpectedCellValue(sheetName, 5, 8, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 9, "Chicago Total"),
					new ExpectedCellValue(sheetName, 5, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 11, "Nashville Total"),
					new ExpectedCellValue(sheetName, 5, 12, "Grand Total"),
					new ExpectedCellValue(sheetName, 6, 7, "Row Labels"),
					new ExpectedCellValue(sheetName, 6, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 6, 9, null),
					new ExpectedCellValue(sheetName, 6, 10, "Car Rack"),
					new ExpectedCellValue(sheetName, 6, 11, null),
					new ExpectedCellValue(sheetName, 6, 12, null),
					new ExpectedCellValue(sheetName, 7, 7, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 7, 8, null),
					new ExpectedCellValue(sheetName, 7, 9, null),
					new ExpectedCellValue(sheetName, 7, 10, null),
					new ExpectedCellValue(sheetName, 7, 11, null),
					new ExpectedCellValue(sheetName, 7, 12, null),
					new ExpectedCellValue(sheetName, 8, 7, "January"),
					new ExpectedCellValue(sheetName, 8, 8, 415.75),
					new ExpectedCellValue(sheetName, 8, 9, 415.75),
					new ExpectedCellValue(sheetName, 8, 10, 415.75),
					new ExpectedCellValue(sheetName, 8, 11, 415.75),
					new ExpectedCellValue(sheetName, 8, 12, 831.5),
					new ExpectedCellValue(sheetName, 9, 7, "March"),
					new ExpectedCellValue(sheetName, 9, 8, null),
					new ExpectedCellValue(sheetName, 9, 9, null),
					new ExpectedCellValue(sheetName, 9, 10, 415.75),
					new ExpectedCellValue(sheetName, 9, 11, 415.75),
					new ExpectedCellValue(sheetName, 9, 12, 415.75),
					new ExpectedCellValue(sheetName, 10, 7, "Sum of CalculatedField"),
					new ExpectedCellValue(sheetName, 10, 8, null),
					new ExpectedCellValue(sheetName, 10, 9, null),
					new ExpectedCellValue(sheetName, 10, 10, null),
					new ExpectedCellValue(sheetName, 10, 11, null),
					new ExpectedCellValue(sheetName, 10, 12, null),
					new ExpectedCellValue(sheetName, 11, 7, "January"),
					new ExpectedCellValue(sheetName, 11, 8, 831.5),
					new ExpectedCellValue(sheetName, 11, 9, 831.5),
					new ExpectedCellValue(sheetName, 11, 10, 831.5),
					new ExpectedCellValue(sheetName, 11, 11, 831.5),
					new ExpectedCellValue(sheetName, 11, 12, 3326d),
					new ExpectedCellValue(sheetName, 12, 7, "March"),
					new ExpectedCellValue(sheetName, 12, 8, 0d),
					new ExpectedCellValue(sheetName, 12, 9, 0d),
					new ExpectedCellValue(sheetName, 12, 10, 831.5),
					new ExpectedCellValue(sheetName, 12, 11, 831.5),
					new ExpectedCellValue(sheetName, 12, 12, 831.5),
					new ExpectedCellValue(sheetName, 13, 7, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 13, 8, 415.75),
					new ExpectedCellValue(sheetName, 13, 9, 415.75),
					new ExpectedCellValue(sheetName, 13, 10, 831.5),
					new ExpectedCellValue(sheetName, 13, 11, 831.5),
					new ExpectedCellValue(sheetName, 13, 12, 1247.25),
					new ExpectedCellValue(sheetName, 14, 7, "Total Sum of CalculatedField"),
					new ExpectedCellValue(sheetName, 14, 8, 831.5),
					new ExpectedCellValue(sheetName, 14, 9, 831.5),
					new ExpectedCellValue(sheetName, 14, 10, 3326d),
					new ExpectedCellValue(sheetName, 14, 11, 3326d),
					new ExpectedCellValue(sheetName, 14, 12, 7483.5),
				});
			}
		}
		#endregion
		#endregion

		#region UpdateData Field Values Tests
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithNoColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldOneRowFieldWithTrueSubtotalTop()
		{
			var file = new FileInfo("PivotTableWithNoColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["PivotTables"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:B5"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "PivotTables";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "January"),
					new ExpectedCellValue(sheetName, 3, 1, "February"),
					new ExpectedCellValue(sheetName, 4, 1, "March"),
					new ExpectedCellValue(sheetName, 5, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 2, 2078.75),
					new ExpectedCellValue(sheetName, 3, 2, 1293d),
					new ExpectedCellValue(sheetName, 4, 2, 856.49),
					new ExpectedCellValue(sheetName, 5, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithNoColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldTwoRowFieldsWithTrueSubtotalTop()
		{
			var file = new FileInfo("PivotTableWithNoColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["PivotTables"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("F1:G10"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "PivotTables";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 6, "January"),
					new ExpectedCellValue(sheetName, 3, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 4, 6, "February"),
					new ExpectedCellValue(sheetName, 5, 6, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 6, 6, "Tent"),
					new ExpectedCellValue(sheetName, 7, 6, "March"),
					new ExpectedCellValue(sheetName, 8, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 9, 6, "Headlamp"),
					new ExpectedCellValue(sheetName, 10, 6, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 7, 2078.75),
					new ExpectedCellValue(sheetName, 3, 7, 2078.75),
					new ExpectedCellValue(sheetName, 4, 7, 1293d),
					new ExpectedCellValue(sheetName, 5, 7, 99d),
					new ExpectedCellValue(sheetName, 6, 7, 1194d),
					new ExpectedCellValue(sheetName, 7, 7, 856.49),
					new ExpectedCellValue(sheetName, 8, 7, 831.5),
					new ExpectedCellValue(sheetName, 9, 7, 24.99),
					new ExpectedCellValue(sheetName, 10, 7, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithNoColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldTwoRowFieldsWithFalseSubtotalTop()
		{
			var file = new FileInfo("PivotTableWithNoColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["PivotTables"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("F1:G13"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "PivotTables";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 6, "January"),
					new ExpectedCellValue(sheetName, 3, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 4, 6, "January Total"),
					new ExpectedCellValue(sheetName, 5, 6, "February"),
					new ExpectedCellValue(sheetName, 6, 6, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 7, 6, "Tent"),
					new ExpectedCellValue(sheetName, 8, 6, "February Total"),
					new ExpectedCellValue(sheetName, 9, 6, "March"),
					new ExpectedCellValue(sheetName, 10, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 11, 6, "Headlamp"),
					new ExpectedCellValue(sheetName, 12, 6, "March Total"),
					new ExpectedCellValue(sheetName, 13, 6, "Grand Total"),
					new ExpectedCellValue(sheetName, 3, 7, 2078.75),
					new ExpectedCellValue(sheetName, 4, 7, 2078.75),
					new ExpectedCellValue(sheetName, 6, 7, 99d),
					new ExpectedCellValue(sheetName, 7, 7, 1194d),
					new ExpectedCellValue(sheetName, 8, 7, 1293d),
					new ExpectedCellValue(sheetName, 10, 7, 831.5),
					new ExpectedCellValue(sheetName, 11, 7, 24.99),
					new ExpectedCellValue(sheetName, 12, 7, 856.49),
					new ExpectedCellValue(sheetName, 13, 7, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithNoColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldTwoRowFieldsWithNoSubtotal()
		{
			var file = new FileInfo("PivotTableWithNoColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["PivotTables"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubTotalFunctions = eSubTotalFunctions.None;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("F1:G10"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "PivotTables";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 6, "January"),
					new ExpectedCellValue(sheetName, 3, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 4, 6, "February"),
					new ExpectedCellValue(sheetName, 5, 6, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 6, 6, "Tent"),
					new ExpectedCellValue(sheetName, 7, 6, "March"),
					new ExpectedCellValue(sheetName, 8, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 9, 6, "Headlamp"),
					new ExpectedCellValue(sheetName, 10, 6, "Grand Total"),
					new ExpectedCellValue(sheetName, 3, 7, 2078.75),
					new ExpectedCellValue(sheetName, 5, 7, 99d),
					new ExpectedCellValue(sheetName, 6, 7, 1194d),
					new ExpectedCellValue(sheetName, 8, 7, 831.5),
					new ExpectedCellValue(sheetName, 9, 7, 24.99),
					new ExpectedCellValue(sheetName, 10, 7, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithNoColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldThreeRowFieldsWithTrueSubtotalTop()
		{
			var file = new FileInfo("PivotTableWithNoColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["PivotTables"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("L1:M17"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "PivotTables";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 12, "January"),
					new ExpectedCellValue(sheetName, 3, 12, "Car Rack"),
					new ExpectedCellValue(sheetName, 4, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 6, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 7, 12, "February"),
					new ExpectedCellValue(sheetName, 8, 12, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 9, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 10, 12, "Tent"),
					new ExpectedCellValue(sheetName, 11, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 12, 12, "March"),
					new ExpectedCellValue(sheetName, 13, 12, "Car Rack"),
					new ExpectedCellValue(sheetName, 14, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 15, 12, "Headlamp"),
					new ExpectedCellValue(sheetName, 16, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 17, 12, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 13, 2078.75),
					new ExpectedCellValue(sheetName, 3, 13, 2078.75),
					new ExpectedCellValue(sheetName, 4, 13, 831.5),
					new ExpectedCellValue(sheetName, 5, 13, 831.5),
					new ExpectedCellValue(sheetName, 6, 13, 415.75),
					new ExpectedCellValue(sheetName, 7, 13, 1293d),
					new ExpectedCellValue(sheetName, 8, 13, 99d),
					new ExpectedCellValue(sheetName, 9, 13, 99d),
					new ExpectedCellValue(sheetName, 10, 13, 1194d),
					new ExpectedCellValue(sheetName, 11, 13, 1194d),
					new ExpectedCellValue(sheetName, 12, 13, 856.49),
					new ExpectedCellValue(sheetName, 13, 13, 831.5),
					new ExpectedCellValue(sheetName, 14, 13, 831.5),
					new ExpectedCellValue(sheetName, 15, 13, 24.99),
					new ExpectedCellValue(sheetName, 16, 13, 24.99),
					new ExpectedCellValue(sheetName, 17, 13, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithNoColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldThreeRowFieldsWithFalseSubtotalTop()
		{
			var file = new FileInfo("PivotTableWithNoColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["PivotTables"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("L1:M25"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "PivotTables";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 12, "January"),
					new ExpectedCellValue(sheetName, 3, 12, "Car Rack"),
					new ExpectedCellValue(sheetName, 4, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 6, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 7, 12, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 8, 12, "January Total"),
					new ExpectedCellValue(sheetName, 9, 12, "February"),
					new ExpectedCellValue(sheetName, 10, 12, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 11, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 12, 12, "Sleeping Bag Total"),
					new ExpectedCellValue(sheetName, 13, 12, "Tent"),
					new ExpectedCellValue(sheetName, 14, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 15, 12, "Tent Total"),
					new ExpectedCellValue(sheetName, 16, 12, "February Total"),
					new ExpectedCellValue(sheetName, 17, 12, "March"),
					new ExpectedCellValue(sheetName, 18, 12, "Car Rack"),
					new ExpectedCellValue(sheetName, 19, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 20, 12, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 21, 12, "Headlamp"),
					new ExpectedCellValue(sheetName, 22, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 23, 12, "Headlamp Total"),
					new ExpectedCellValue(sheetName, 24, 12, "March Total"),
					new ExpectedCellValue(sheetName, 25, 12, "Grand Total"),
					new ExpectedCellValue(sheetName, 4, 13, 831.5),
					new ExpectedCellValue(sheetName, 5, 13, 831.5),
					new ExpectedCellValue(sheetName, 6, 13, 415.75),
					new ExpectedCellValue(sheetName, 7, 13, 2078.75),
					new ExpectedCellValue(sheetName, 8, 13, 2078.75),
					new ExpectedCellValue(sheetName, 11, 13, 99d),
					new ExpectedCellValue(sheetName, 12, 13, 99d),
					new ExpectedCellValue(sheetName, 14, 13, 1194d),
					new ExpectedCellValue(sheetName, 15, 13, 1194d),
					new ExpectedCellValue(sheetName, 16, 13, 1293d),
					new ExpectedCellValue(sheetName, 19, 13, 831.5),
					new ExpectedCellValue(sheetName, 20, 13, 831.5),
					new ExpectedCellValue(sheetName, 22, 13, 24.99),
					new ExpectedCellValue(sheetName, 23, 13, 24.99),
					new ExpectedCellValue(sheetName, 24, 13, 856.49),
					new ExpectedCellValue(sheetName, 25, 13, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithNoSubtotals.xlsx")]
		public void PivotTableRefreshDataFieldsRowsAndColumnsWithNoSubtotal()
		{
			var file = new FileInfo("PivotTableWithNoSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["NoSubtotals"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:I13"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "NoSubtotals";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "1"),
					new ExpectedCellValue(sheetName, 6, 1, "2"),
					new ExpectedCellValue(sheetName, 7, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 8, 1, "1"),
					new ExpectedCellValue(sheetName, 9, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 10, 1, "1"),
					new ExpectedCellValue(sheetName, 11, 1, "Tent"),
					new ExpectedCellValue(sheetName, 12, 1, "6"),
					new ExpectedCellValue(sheetName, 13, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 3, 2, "January"),
					new ExpectedCellValue(sheetName, 6, 2, 415.75),
					new ExpectedCellValue(sheetName, 13, 2, 415.75),
					new ExpectedCellValue(sheetName, 3, 3, "March"),
					new ExpectedCellValue(sheetName, 8, 3, 24.99),
					new ExpectedCellValue(sheetName, 13, 3, 24.99),
					new ExpectedCellValue(sheetName, 2, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 3, 4, "January"),
					new ExpectedCellValue(sheetName, 6, 4, 415.75),
					new ExpectedCellValue(sheetName, 13, 2, 415.75),
					new ExpectedCellValue(sheetName, 3, 5, "February"),
					new ExpectedCellValue(sheetName, 12, 5, 199d),
					new ExpectedCellValue(sheetName, 13, 5, 199d),
					new ExpectedCellValue(sheetName, 3, 6, "March"),
					new ExpectedCellValue(sheetName, 6, 6, 415.75),
					new ExpectedCellValue(sheetName, 13, 6, 415.75),
					new ExpectedCellValue(sheetName, 2, 7, "San Francisco"),
					new ExpectedCellValue(sheetName, 3, 7, "January"),
					new ExpectedCellValue(sheetName, 5, 7, 415.75),
					new ExpectedCellValue(sheetName, 13, 7, 415.75),
					new ExpectedCellValue(sheetName, 3, 8, "February"),
					new ExpectedCellValue(sheetName, 10, 8, 99d),
					new ExpectedCellValue(sheetName, 13, 8, 99d),
					new ExpectedCellValue(sheetName, 2, 9, "Grand Total"),
					new ExpectedCellValue(sheetName, 5, 9, 415.75),
					new ExpectedCellValue(sheetName, 6, 9, 1247.25),
					new ExpectedCellValue(sheetName, 8, 9, 24.99),
					new ExpectedCellValue(sheetName, 10, 9, 99d),
					new ExpectedCellValue(sheetName, 12, 9, 199d),
					new ExpectedCellValue(sheetName, 13, 9, 1985.99),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithGrandTotals.xlsx")]
		public void PivotTableRefreshDataFieldsRowsAndColumnsGrandTotalOff()
		{
			var file = new FileInfo("PivotTableWithGrandTotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["GrandTotals"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					pivotTable.RowGrandTotals = false;
					pivotTable.ColumnGrandTotals = false;
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:K7"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "GrandTotals";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 6, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 7, 1, "Tent"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 3, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 2, 831.5),
					new ExpectedCellValue(sheetName, 3, 3, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 3, 831.5),
					new ExpectedCellValue(sheetName, 3, 4, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 4, 415.75),
					new ExpectedCellValue(sheetName, 2, 5, "January Total"),
					new ExpectedCellValue(sheetName, 4, 5, 2078.75),
					new ExpectedCellValue(sheetName, 2, 6, "February"),
					new ExpectedCellValue(sheetName, 3, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 6, 1194d),
					new ExpectedCellValue(sheetName, 3, 7, "San Francisco"),
					new ExpectedCellValue(sheetName, 6, 7, 99d),
					new ExpectedCellValue(sheetName, 2, 8, "February Total"),
					new ExpectedCellValue(sheetName, 6, 8, 99d),
					new ExpectedCellValue(sheetName, 7, 8, 1194d),
					new ExpectedCellValue(sheetName, 2, 9, "March"),
					new ExpectedCellValue(sheetName, 3, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 9, 24.99),
					new ExpectedCellValue(sheetName, 3, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 10, 831.5),
					new ExpectedCellValue(sheetName, 2, 11, "March Total"),
					new ExpectedCellValue(sheetName, 4, 11, 831.5),
					new ExpectedCellValue(sheetName, 5, 11, 24.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithGrandTotals.xlsx")]
		public void PivotTableRefreshDataFieldsColumnGrandTotalOff()
		{
			var file = new FileInfo("PivotTableWithGrandTotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["GrandTotals"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					pivotTable.ColumnGrandTotals = false;
					pivotTable.RowGrandTotals = true;
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:K8"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "GrandTotals";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 6, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 7, 1, "Tent"),
					new ExpectedCellValue(sheetName, 8, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 3, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 2, 831.5),
					new ExpectedCellValue(sheetName, 8, 2, 831.5),
					new ExpectedCellValue(sheetName, 3, 3, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 3, 831.5),
					new ExpectedCellValue(sheetName, 8, 3, 831.5),
					new ExpectedCellValue(sheetName, 3, 4, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 4, 415.75),
					new ExpectedCellValue(sheetName, 8, 4, 415.75),
					new ExpectedCellValue(sheetName, 2, 5, "January Total"),
					new ExpectedCellValue(sheetName, 4, 5, 2078.75),
					new ExpectedCellValue(sheetName, 8, 5, 2078.75),
					new ExpectedCellValue(sheetName, 2, 6, "February"),
					new ExpectedCellValue(sheetName, 3, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 6, 1194d),
					new ExpectedCellValue(sheetName, 8, 6, 1194d),
					new ExpectedCellValue(sheetName, 3, 7, "San Francisco"),
					new ExpectedCellValue(sheetName, 6, 7, 99d),
					new ExpectedCellValue(sheetName, 8, 7, 99d),
					new ExpectedCellValue(sheetName, 2, 8, "February Total"),
					new ExpectedCellValue(sheetName, 6, 8, 99d),
					new ExpectedCellValue(sheetName, 7, 8, 1194d),
					new ExpectedCellValue(sheetName, 8, 8, 1293d),
					new ExpectedCellValue(sheetName, 2, 9, "March"),
					new ExpectedCellValue(sheetName, 3, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 9, 24.99),
					new ExpectedCellValue(sheetName, 8, 9, 24.99),
					new ExpectedCellValue(sheetName, 3, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 10, 831.5),
					new ExpectedCellValue(sheetName, 8, 10, 831.5),
					new ExpectedCellValue(sheetName, 2, 11, "March Total"),
					new ExpectedCellValue(sheetName, 4, 11, 831.5),
					new ExpectedCellValue(sheetName, 5, 11, 24.99),
					new ExpectedCellValue(sheetName, 8, 11, 856.49)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithGrandTotals.xlsx")]
		public void PivotTableRefreshDataFieldsRowGrandTotalOff()
		{
			var file = new FileInfo("PivotTableWithGrandTotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["GrandTotals"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					pivotTable.RowGrandTotals = false;
					pivotTable.ColumnGrandTotals = true;
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:L7"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "GrandTotals";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 6, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 7, 1, "Tent"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 3, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 2, 831.5),
					new ExpectedCellValue(sheetName, 3, 3, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 3, 831.5),
					new ExpectedCellValue(sheetName, 3, 4, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 4, 415.75),
					new ExpectedCellValue(sheetName, 2, 5, "January Total"),
					new ExpectedCellValue(sheetName, 4, 5, 2078.75),
					new ExpectedCellValue(sheetName, 2, 6, "February"),
					new ExpectedCellValue(sheetName, 3, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 6, 1194d),
					new ExpectedCellValue(sheetName, 3, 7, "San Francisco"),
					new ExpectedCellValue(sheetName, 6, 7, 99d),
					new ExpectedCellValue(sheetName, 2, 8, "February Total"),
					new ExpectedCellValue(sheetName, 6, 8, 99d),
					new ExpectedCellValue(sheetName, 7, 8, 1194d),
					new ExpectedCellValue(sheetName, 2, 9, "March"),
					new ExpectedCellValue(sheetName, 3, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 9, 24.99),
					new ExpectedCellValue(sheetName, 3, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 10, 831.5),
					new ExpectedCellValue(sheetName, 2, 11, "March Total"),
					new ExpectedCellValue(sheetName, 4, 11, 831.5),
					new ExpectedCellValue(sheetName, 5, 11, 24.99),
					new ExpectedCellValue(sheetName, 2, 12, "Grand Total"),
					new ExpectedCellValue(sheetName, 4, 12, 2910.25),
					new ExpectedCellValue(sheetName, 5, 12, 24.99),
					new ExpectedCellValue(sheetName, 6, 12, 99d),
					new ExpectedCellValue(sheetName, 7, 12, 1194d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleDataFields.xlsx")]
		public void PivotTableRefreshMultipleDataFields()
		{
			var file = new FileInfo("PivotTableWithMultipleDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["MultipleDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:C5"), pivotTable.Address);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "MultipleDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 3, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 5, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 2, 2, 3),
					new ExpectedCellValue(sheetName, 3, 2, 10),
					new ExpectedCellValue(sheetName, 4, 2, 2),
					new ExpectedCellValue(sheetName, 5, 2, 15),
					new ExpectedCellValue(sheetName, 1, 3, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 3, 856.49),
					new ExpectedCellValue(sheetName, 3, 3, 2857d),
					new ExpectedCellValue(sheetName, 4, 3, 514.75),
					new ExpectedCellValue(sheetName, 5, 3, 4228.24),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleDataFields.xlsx")]
		public void PivotTableRefreshMultipleDataFieldsNoGrandTotal()
		{
			var file = new FileInfo("PivotTableWithMultipleDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["MultipleDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					pivotTable.RowGrandTotals = false;
					pivotTable.ColumnGrandTotals = true;
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:C4"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "MultipleDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 3, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 2, 2, 3),
					new ExpectedCellValue(sheetName, 3, 2, 10),
					new ExpectedCellValue(sheetName, 4, 2, 2),
					new ExpectedCellValue(sheetName, 1, 3, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 3, 856.49),
					new ExpectedCellValue(sheetName, 3, 3, 2857d),
					new ExpectedCellValue(sheetName, 4, 3, 514.75)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleDataFields.xlsx")]
		public void PivotTableRefreshMultipleDataFieldsWithColumnHeaders()
		{
			var file = new FileInfo("PivotTableWithMultipleDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["MultipleDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A10:W17"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "MultipleDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 14, 1, "January"),
					new ExpectedCellValue(sheetName, 15, 1, "February"),
					new ExpectedCellValue(sheetName, 16, 1, "March"),
					new ExpectedCellValue(sheetName, 17, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 11, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 12, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 13, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 2, 2d),
					new ExpectedCellValue(sheetName, 17, 2, 2d),
					new ExpectedCellValue(sheetName, 13, 3, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 3, 831.5),
					new ExpectedCellValue(sheetName, 17, 3, 831.5),
					new ExpectedCellValue(sheetName, 12, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 13, 4, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 4, 2d),
					new ExpectedCellValue(sheetName, 16, 4, 2d),
					new ExpectedCellValue(sheetName, 17, 4, 4d),
					new ExpectedCellValue(sheetName, 13, 5, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 5, 831.5),
					new ExpectedCellValue(sheetName, 16, 5, 831.5),
					new ExpectedCellValue(sheetName, 17, 5, 1663d),
					new ExpectedCellValue(sheetName, 12, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 13, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 6, 1d),
					new ExpectedCellValue(sheetName, 17, 6, 1d),
					new ExpectedCellValue(sheetName, 13, 7, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 7, 415.75),
					new ExpectedCellValue(sheetName, 17, 7, 415.75),
					new ExpectedCellValue(sheetName, 11, 8, "Car Rack Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 8, 5d),
					new ExpectedCellValue(sheetName, 16, 8, 2d),
					new ExpectedCellValue(sheetName, 17, 8, 7d),
					new ExpectedCellValue(sheetName, 11, 9, "Car Rack Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 9, 2078.75),
					new ExpectedCellValue(sheetName, 16, 9, 831.5),
					new ExpectedCellValue(sheetName, 17, 9, 2910.25),
					new ExpectedCellValue(sheetName, 11, 10, "Headlamp"),
					new ExpectedCellValue(sheetName, 12, 10, "Chicago"),
					new ExpectedCellValue(sheetName, 13, 10, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 10, 1d),
					new ExpectedCellValue(sheetName, 17, 10, 1d),
					new ExpectedCellValue(sheetName, 13, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 11, 24.99),
					new ExpectedCellValue(sheetName, 17, 11, 24.99),
					new ExpectedCellValue(sheetName, 11, 12, "Headlamp Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 12, 1d),
					new ExpectedCellValue(sheetName, 17, 12, 1d),
					new ExpectedCellValue(sheetName, 11, 13, "Headlamp Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 13, 24.99),
					new ExpectedCellValue(sheetName, 17, 13, 24.99),
					new ExpectedCellValue(sheetName, 11, 14, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 12, 14, "San Francisco"),
					new ExpectedCellValue(sheetName, 13, 14, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 14, 1d),
					new ExpectedCellValue(sheetName, 17, 14, 1d),
					new ExpectedCellValue(sheetName, 13, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 15, 15, 99d),
					new ExpectedCellValue(sheetName, 17, 15, 99d),
					new ExpectedCellValue(sheetName, 11, 16, "Sleeping Bag Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 16, 1d),
					new ExpectedCellValue(sheetName, 17, 16, 1d),
					new ExpectedCellValue(sheetName, 11, 17, "Sleeping Bag Sum of Total"),
					new ExpectedCellValue(sheetName, 15, 17, 99d),
					new ExpectedCellValue(sheetName, 17, 17, 99d),
					new ExpectedCellValue(sheetName, 11, 18, "Tent"),
					new ExpectedCellValue(sheetName, 12, 18, "Nashville"),
					new ExpectedCellValue(sheetName, 13, 18, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 18, 6d),
					new ExpectedCellValue(sheetName, 17, 18, 6d),
					new ExpectedCellValue(sheetName, 13, 19, "Sum of Total"),
					new ExpectedCellValue(sheetName, 15, 19, 1194d),
					new ExpectedCellValue(sheetName, 17, 19, 1194d),
					new ExpectedCellValue(sheetName, 11, 20, "Tent Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 20, 6d),
					new ExpectedCellValue(sheetName, 17, 20, 6d),
					new ExpectedCellValue(sheetName, 11, 21, "Tent Sum of Total"),
					new ExpectedCellValue(sheetName, 15, 21, 1194d),
					new ExpectedCellValue(sheetName, 17, 21, 1194d),
					new ExpectedCellValue(sheetName, 11, 22, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 22, 5d),
					new ExpectedCellValue(sheetName, 15, 22, 7d),
					new ExpectedCellValue(sheetName, 16, 22, 3d),
					new ExpectedCellValue(sheetName, 17, 22, 15d),
					new ExpectedCellValue(sheetName, 11, 23, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 23, 2078.75),
					new ExpectedCellValue(sheetName, 15, 23, 1293d),
					new ExpectedCellValue(sheetName, 16, 23, 856.49),
					new ExpectedCellValue(sheetName, 17, 23, 4228.24)
				});
			}
		}

		#region Multiple Row Data Fields
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsOneRowFieldOneColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsOneRowAndOneColumn()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsOneRowFieldOneColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:E13"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 6, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 8, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 9, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 10, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 11, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 12, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 4, 2, 2d),
					new ExpectedCellValue(sheetName, 5, 2, 831.5),
					new ExpectedCellValue(sheetName, 7, 2, 2d),
					new ExpectedCellValue(sheetName, 8, 2, 831.5),
					new ExpectedCellValue(sheetName, 10, 2, 1d),
					new ExpectedCellValue(sheetName, 11, 2, 415.75),
					new ExpectedCellValue(sheetName, 12, 2, 5d),
					new ExpectedCellValue(sheetName, 13, 2, 2078.75),
					new ExpectedCellValue(sheetName, 2, 3, "February"),
					new ExpectedCellValue(sheetName, 7, 3, 6d),
					new ExpectedCellValue(sheetName, 8, 3, 1194d),
					new ExpectedCellValue(sheetName, 10, 3, 1d),
					new ExpectedCellValue(sheetName, 11, 3, 99d),
					new ExpectedCellValue(sheetName, 12, 3, 7d),
					new ExpectedCellValue(sheetName, 13, 3, 1293d),
					new ExpectedCellValue(sheetName, 2, 4, "March"),
					new ExpectedCellValue(sheetName, 4, 4, 1d),
					new ExpectedCellValue(sheetName, 5, 4, 24.99),
					new ExpectedCellValue(sheetName, 7, 4, 2d),
					new ExpectedCellValue(sheetName, 8, 4, 831.5),
					new ExpectedCellValue(sheetName, 12, 4, 3d),
					new ExpectedCellValue(sheetName, 13, 4, 856.49),
					new ExpectedCellValue(sheetName, 2, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 4, 5, 3d),
					new ExpectedCellValue(sheetName, 5, 5, 856.49),
					new ExpectedCellValue(sheetName, 7, 5, 10d),
					new ExpectedCellValue(sheetName, 8, 5, 2857d),
					new ExpectedCellValue(sheetName, 10, 5, 2d),
					new ExpectedCellValue(sheetName, 11, 5, 514.75),
					new ExpectedCellValue(sheetName, 12, 5, 15d),
					new ExpectedCellValue(sheetName, 13, 5, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsTwoRowFieldOneColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsTwoRowsAndOneColumnSubtotalsOff()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsTwoRowFieldOneColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubTotalFunctions = eSubTotalFunctions.None;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:E25"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 6, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 7, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 8, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 9, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 10, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 11, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 12, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 1, "Tent"),
					new ExpectedCellValue(sheetName, 15, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 17, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 18, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 19, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 21, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 22, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 23, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 24, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 25, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 5, 2, 2d),
					new ExpectedCellValue(sheetName, 6, 2, 831.5),
					new ExpectedCellValue(sheetName, 12, 2, 2d),
					new ExpectedCellValue(sheetName, 13, 2, 831.5),
					new ExpectedCellValue(sheetName, 19, 2, 1d),
					new ExpectedCellValue(sheetName, 20, 2, 415.75),
					new ExpectedCellValue(sheetName, 24, 2, 5d),
					new ExpectedCellValue(sheetName, 25, 2, 2078.75),
					new ExpectedCellValue(sheetName, 2, 3, "February"),
					new ExpectedCellValue(sheetName, 15, 3, 6d),
					new ExpectedCellValue(sheetName, 16, 3, 1194d),
					new ExpectedCellValue(sheetName, 22, 3, 1d),
					new ExpectedCellValue(sheetName, 23, 3, 99d),
					new ExpectedCellValue(sheetName, 24, 3, 7d),
					new ExpectedCellValue(sheetName, 25, 3, 1293d),
					new ExpectedCellValue(sheetName, 2, 4, "March"),
					new ExpectedCellValue(sheetName, 8, 4, 1d),
					new ExpectedCellValue(sheetName, 9, 4, 24.99),
					new ExpectedCellValue(sheetName, 12, 4, 2d),
					new ExpectedCellValue(sheetName, 13, 4, 831.5),
					new ExpectedCellValue(sheetName, 24, 4, 3d),
					new ExpectedCellValue(sheetName, 25, 4, 856.49),
					new ExpectedCellValue(sheetName, 2, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 5, 5, 2d),
					new ExpectedCellValue(sheetName, 6, 5, 831.5),
					new ExpectedCellValue(sheetName, 8, 5, 1d),
					new ExpectedCellValue(sheetName, 9, 5, 24.99),
					new ExpectedCellValue(sheetName, 12, 5, 4d),
					new ExpectedCellValue(sheetName, 13, 5, 1663d),
					new ExpectedCellValue(sheetName, 15, 5, 6d),
					new ExpectedCellValue(sheetName, 16, 5, 1194d),
					new ExpectedCellValue(sheetName, 19, 5, 1d),
					new ExpectedCellValue(sheetName, 20, 5, 415.75),
					new ExpectedCellValue(sheetName, 22, 5, 1d),
					new ExpectedCellValue(sheetName, 23, 5, 99d),
					new ExpectedCellValue(sheetName, 24, 5, 15d),
					new ExpectedCellValue(sheetName, 25, 5, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsTwoRowFieldOneColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsTwoRowsAndOneColumnSubtotalsOn()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsTwoRowFieldOneColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:E31"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 6, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 7, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 8, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 9, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 10, 1, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 11, 1, "Chicago Sum of Total"),
					new ExpectedCellValue(sheetName, 12, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 13, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 14, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 1, "Tent"),
					new ExpectedCellValue(sheetName, 17, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 18, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 19, 1, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 1, "Nashville Sum of Total"),
					new ExpectedCellValue(sheetName, 21, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 22, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 23, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 24, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 25, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 26, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 27, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 28, 1, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 29, 1, "San Francisco Sum of Total"),
					new ExpectedCellValue(sheetName, 30, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 31, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 5, 2, 2d),
					new ExpectedCellValue(sheetName, 6, 2, 831.5),
					new ExpectedCellValue(sheetName, 10, 2, 2d),
					new ExpectedCellValue(sheetName, 11, 2, 831.5),
					new ExpectedCellValue(sheetName, 14, 2, 2d),
					new ExpectedCellValue(sheetName, 15, 2, 831.5),
					new ExpectedCellValue(sheetName, 19, 2, 2d),
					new ExpectedCellValue(sheetName, 20, 2, 831.5),
					new ExpectedCellValue(sheetName, 23, 2, 1d),
					new ExpectedCellValue(sheetName, 24, 2, 415.75),
					new ExpectedCellValue(sheetName, 28, 2, 1d),
					new ExpectedCellValue(sheetName, 29, 2, 415.75),
					new ExpectedCellValue(sheetName, 30, 2, 5d),
					new ExpectedCellValue(sheetName, 31, 2, 2078.75),
					new ExpectedCellValue(sheetName, 2, 3, "February"),
					new ExpectedCellValue(sheetName, 17, 3, 6d),
					new ExpectedCellValue(sheetName, 18, 3, 1194d),
					new ExpectedCellValue(sheetName, 19, 3, 6d),
					new ExpectedCellValue(sheetName, 20, 3, 1194d),
					new ExpectedCellValue(sheetName, 26, 3, 1d),
					new ExpectedCellValue(sheetName, 27, 3, 99d),
					new ExpectedCellValue(sheetName, 28, 3, 1d),
					new ExpectedCellValue(sheetName, 29, 3, 99d),
					new ExpectedCellValue(sheetName, 30, 3, 7d),
					new ExpectedCellValue(sheetName, 31, 3, 1293d),
					new ExpectedCellValue(sheetName, 2, 4, "March"),
					new ExpectedCellValue(sheetName, 8, 4, 1d),
					new ExpectedCellValue(sheetName, 9, 4, 24.99),
					new ExpectedCellValue(sheetName, 10, 4, 1d),
					new ExpectedCellValue(sheetName, 11, 4, 24.99),
					new ExpectedCellValue(sheetName, 14, 4, 2d),
					new ExpectedCellValue(sheetName, 15, 4, 831.5),
					new ExpectedCellValue(sheetName, 19, 4, 2d),
					new ExpectedCellValue(sheetName, 20, 4, 831.5),
					new ExpectedCellValue(sheetName, 30, 4, 3d),
					new ExpectedCellValue(sheetName, 31, 4, 856.49),
					new ExpectedCellValue(sheetName, 2, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 5, 5, 2d),
					new ExpectedCellValue(sheetName, 6, 5, 831.5),
					new ExpectedCellValue(sheetName, 8, 5, 1d),
					new ExpectedCellValue(sheetName, 9, 5, 24.99),
					new ExpectedCellValue(sheetName, 10, 5, 3d),
					new ExpectedCellValue(sheetName, 11, 5, 856.49),
					new ExpectedCellValue(sheetName, 14, 5, 4d),
					new ExpectedCellValue(sheetName, 15, 5, 1663d),
					new ExpectedCellValue(sheetName, 17, 5, 6d),
					new ExpectedCellValue(sheetName, 18, 5, 1194d),
					new ExpectedCellValue(sheetName, 19, 5, 10d),
					new ExpectedCellValue(sheetName, 20, 5, 2857d),
					new ExpectedCellValue(sheetName, 23, 5, 1d),
					new ExpectedCellValue(sheetName, 24, 5, 415.75),
					new ExpectedCellValue(sheetName, 26, 5, 1d),
					new ExpectedCellValue(sheetName, 27, 5, 99d),
					new ExpectedCellValue(sheetName, 28, 5, 2d),
					new ExpectedCellValue(sheetName, 29, 5, 514.75),
					new ExpectedCellValue(sheetName, 30, 5, 15d),
					new ExpectedCellValue(sheetName, 31, 5, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsThreeRowFieldOneColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsThreeRowsAndOneColumnSubtotalsOff()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsThreeRowFieldOneColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubTotalFunctions = eSubTotalFunctions.None;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:E34"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(7, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "20100007"),
					new ExpectedCellValue(sheetName, 6, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 7, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 8, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 9, 1, "20100083"),
					new ExpectedCellValue(sheetName, 10, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 11, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 12, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 13, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 14, 1, "20100017"),
					new ExpectedCellValue(sheetName, 15, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 17, 1, "20100090"),
					new ExpectedCellValue(sheetName, 18, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 19, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 20, 1, "Tent"),
					new ExpectedCellValue(sheetName, 21, 1, "20100070"),
					new ExpectedCellValue(sheetName, 22, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 23, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 24, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 25, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 26, 1, "20100076"),
					new ExpectedCellValue(sheetName, 27, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 28, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 29, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 30, 1, "20100085"),
					new ExpectedCellValue(sheetName, 31, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 32, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 34, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 6, 2, 2d),
					new ExpectedCellValue(sheetName, 7, 2, 831.5),
					new ExpectedCellValue(sheetName, 18, 2, 2d),
					new ExpectedCellValue(sheetName, 19, 2, 831.5),
					new ExpectedCellValue(sheetName, 27, 2, 1d),
					new ExpectedCellValue(sheetName, 28, 2, 415.75),
					new ExpectedCellValue(sheetName, 33, 2, 5d),
					new ExpectedCellValue(sheetName, 34, 2, 2078.75),
					new ExpectedCellValue(sheetName, 2, 3, "February"),
					new ExpectedCellValue(sheetName, 22, 3, 6d),
					new ExpectedCellValue(sheetName, 23, 3, 1194d),
					new ExpectedCellValue(sheetName, 31, 3, 1d),
					new ExpectedCellValue(sheetName, 32, 3, 99d),
					new ExpectedCellValue(sheetName, 33, 3, 7d),
					new ExpectedCellValue(sheetName, 34, 3, 1293d),
					new ExpectedCellValue(sheetName, 2, 4, "March"),
					new ExpectedCellValue(sheetName, 10, 4, 1d),
					new ExpectedCellValue(sheetName, 11, 4, 24.99),
					new ExpectedCellValue(sheetName, 15, 4, 2d),
					new ExpectedCellValue(sheetName, 16, 4, 831.5),
					new ExpectedCellValue(sheetName, 33, 4, 3d),
					new ExpectedCellValue(sheetName, 34, 4, 856.49),
					new ExpectedCellValue(sheetName, 2, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 6, 5, 2d),
					new ExpectedCellValue(sheetName, 7, 5, 831.5),
					new ExpectedCellValue(sheetName, 10, 5, 1d),
					new ExpectedCellValue(sheetName, 11, 5, 24.99),
					new ExpectedCellValue(sheetName, 15, 5, 2d),
					new ExpectedCellValue(sheetName, 16, 5, 831.5),
					new ExpectedCellValue(sheetName, 18, 5, 2d),
					new ExpectedCellValue(sheetName, 19, 5, 831.5),
					new ExpectedCellValue(sheetName, 22, 5, 6d),
					new ExpectedCellValue(sheetName, 23, 5, 1194d),
					new ExpectedCellValue(sheetName, 27, 5, 1d),
					new ExpectedCellValue(sheetName, 28, 5, 415.75),
					new ExpectedCellValue(sheetName, 31, 5, 1d),
					new ExpectedCellValue(sheetName, 32, 5, 99d),
					new ExpectedCellValue(sheetName, 33, 5, 15d),
					new ExpectedCellValue(sheetName, 34, 5, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsThreeRowFieldOneColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsThreeRowsAndOneColumnSubtotalsOn()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsThreeRowFieldOneColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:E52"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "20100007"),
					new ExpectedCellValue(sheetName, 6, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 7, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 8, 1, "Car Rack Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 9, 1, "Car Rack Sum of Total"),
					new ExpectedCellValue(sheetName, 10, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 11, 1, "20100083"),
					new ExpectedCellValue(sheetName, 12, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 1, "Headlamp Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 1, "Headlamp Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 1, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 17, 1, "Chicago Sum of Total"),
					new ExpectedCellValue(sheetName, 18, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 19, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 20, 1, "20100017"),
					new ExpectedCellValue(sheetName, 21, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 23, 1, "20100090"),
					new ExpectedCellValue(sheetName, 24, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 25, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 26, 1, "Car Rack Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 27, 1, "Car Rack Sum of Total"),
					new ExpectedCellValue(sheetName, 28, 1, "Tent"),
					new ExpectedCellValue(sheetName, 29, 1, "20100070"),
					new ExpectedCellValue(sheetName, 30, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 31, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 32, 1, "Tent Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 1, "Tent Sum of Total"),
					new ExpectedCellValue(sheetName, 34, 1, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 35, 1, "Nashville Sum of Total"),
					new ExpectedCellValue(sheetName, 36, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 37, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 38, 1, "20100076"),
					new ExpectedCellValue(sheetName, 39, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 40, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 41, 1, "Car Rack Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 42, 1, "Car Rack Sum of Total"),
					new ExpectedCellValue(sheetName, 43, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 44, 1, "20100085"),
					new ExpectedCellValue(sheetName, 45, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 46, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 47, 1, "Sleeping Bag Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 48, 1, "Sleeping Bag Sum of Total"),
					new ExpectedCellValue(sheetName, 49, 1, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 50, 1, "San Francisco Sum of Total"),
					new ExpectedCellValue(sheetName, 51, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 52, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 6, 2, 2d),
					new ExpectedCellValue(sheetName, 7, 2, 831.5),
					new ExpectedCellValue(sheetName, 8, 2, 2d),
					new ExpectedCellValue(sheetName, 9, 2, 831.5),
					new ExpectedCellValue(sheetName, 16, 2, 2d),
					new ExpectedCellValue(sheetName, 17, 2, 831.5),
					new ExpectedCellValue(sheetName, 24, 2, 2d),
					new ExpectedCellValue(sheetName, 25, 2, 831.5),
					new ExpectedCellValue(sheetName, 26, 2, 2d),
					new ExpectedCellValue(sheetName, 27, 2, 831.5),
					new ExpectedCellValue(sheetName, 34, 2, 2d),
					new ExpectedCellValue(sheetName, 35, 2, 831.5),
					new ExpectedCellValue(sheetName, 39, 2, 1d),
					new ExpectedCellValue(sheetName, 40, 2, 415.75),
					new ExpectedCellValue(sheetName, 41, 2, 1d),
					new ExpectedCellValue(sheetName, 42, 2, 415.75),
					new ExpectedCellValue(sheetName, 49, 2, 1d),
					new ExpectedCellValue(sheetName, 50, 2, 415.75),
					new ExpectedCellValue(sheetName, 51, 2, 5d),
					new ExpectedCellValue(sheetName, 52, 2, 2078.75),
					new ExpectedCellValue(sheetName, 2, 3, "February"),
					new ExpectedCellValue(sheetName, 30, 3, 6d),
					new ExpectedCellValue(sheetName, 31, 3, 1194d),
					new ExpectedCellValue(sheetName, 32, 3, 6d),
					new ExpectedCellValue(sheetName, 33, 3, 1194d),
					new ExpectedCellValue(sheetName, 34, 3, 6d),
					new ExpectedCellValue(sheetName, 35, 3, 1194d),
					new ExpectedCellValue(sheetName, 45, 3, 1d),
					new ExpectedCellValue(sheetName, 46, 3, 99d),
					new ExpectedCellValue(sheetName, 47, 3, 1d),
					new ExpectedCellValue(sheetName, 48, 3, 99d),
					new ExpectedCellValue(sheetName, 49, 3, 1d),
					new ExpectedCellValue(sheetName, 50, 3, 99d),
					new ExpectedCellValue(sheetName, 51, 3, 7d),
					new ExpectedCellValue(sheetName, 52, 3, 1293d),
					new ExpectedCellValue(sheetName, 2, 4, "March"),
					new ExpectedCellValue(sheetName, 12, 4, 1d),
					new ExpectedCellValue(sheetName, 13, 4, 24.99),
					new ExpectedCellValue(sheetName, 14, 4, 1d),
					new ExpectedCellValue(sheetName, 15, 4, 24.99),
					new ExpectedCellValue(sheetName, 16, 4, 1d),
					new ExpectedCellValue(sheetName, 17, 4, 24.99),
					new ExpectedCellValue(sheetName, 21, 4, 2d),
					new ExpectedCellValue(sheetName, 22, 4, 831.5),
					new ExpectedCellValue(sheetName, 26, 4, 2d),
					new ExpectedCellValue(sheetName, 27, 4, 831.5),
					new ExpectedCellValue(sheetName, 34, 4, 2d),
					new ExpectedCellValue(sheetName, 35, 4, 831.5),
					new ExpectedCellValue(sheetName, 51, 4, 3d),
					new ExpectedCellValue(sheetName, 52, 4, 856.49),
					new ExpectedCellValue(sheetName, 2, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 6, 5, 2d),
					new ExpectedCellValue(sheetName, 7, 5, 831.5),
					new ExpectedCellValue(sheetName, 8, 5, 2d),
					new ExpectedCellValue(sheetName, 9, 5, 831.5),
					new ExpectedCellValue(sheetName, 12, 5, 1d),
					new ExpectedCellValue(sheetName, 13, 5, 24.99),
					new ExpectedCellValue(sheetName, 14, 5, 1d),
					new ExpectedCellValue(sheetName, 15, 5, 24.99),
					new ExpectedCellValue(sheetName, 16, 5, 3d),
					new ExpectedCellValue(sheetName, 17, 5, 856.49),
					new ExpectedCellValue(sheetName, 21, 5, 2d),
					new ExpectedCellValue(sheetName, 22, 5, 831.5),
					new ExpectedCellValue(sheetName, 24, 5, 2d),
					new ExpectedCellValue(sheetName, 25, 5, 831.5),
					new ExpectedCellValue(sheetName, 26, 5, 4d),
					new ExpectedCellValue(sheetName, 27, 5, 1663),
					new ExpectedCellValue(sheetName, 30, 5, 6d),
					new ExpectedCellValue(sheetName, 31, 5, 1194d),
					new ExpectedCellValue(sheetName, 32, 5, 6d),
					new ExpectedCellValue(sheetName, 33, 5, 1194d),
					new ExpectedCellValue(sheetName, 34, 5, 10d),
					new ExpectedCellValue(sheetName, 35, 5, 2857d),
					new ExpectedCellValue(sheetName, 39, 5, 1d),
					new ExpectedCellValue(sheetName, 40, 5, 415.75),
					new ExpectedCellValue(sheetName, 41, 5, 1d),
					new ExpectedCellValue(sheetName, 42, 5, 415.75),
					new ExpectedCellValue(sheetName, 45, 5, 1d),
					new ExpectedCellValue(sheetName, 46, 5, 99d),
					new ExpectedCellValue(sheetName, 47, 5, 1d),
					new ExpectedCellValue(sheetName, 48, 5, 99d),
					new ExpectedCellValue(sheetName, 49, 5, 2d),
					new ExpectedCellValue(sheetName, 50, 5, 514.75),
					new ExpectedCellValue(sheetName, 51, 5, 15d),
					new ExpectedCellValue(sheetName, 52, 5, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsOneRowAndNoColumns()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:B12"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "January"),
					new ExpectedCellValue(sheetName, 3, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 4, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 5, 1, "February"),
					new ExpectedCellValue(sheetName, 6, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 7, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 8, 1, "March"),
					new ExpectedCellValue(sheetName, 9, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 10, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 11, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 12, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 3, 2, 5d),
					new ExpectedCellValue(sheetName, 4, 2, 2078.75),
					new ExpectedCellValue(sheetName, 6, 2, 7d),
					new ExpectedCellValue(sheetName, 7, 2, 1293d),
					new ExpectedCellValue(sheetName, 9, 2, 3d),
					new ExpectedCellValue(sheetName, 10, 2, 856.49),
					new ExpectedCellValue(sheetName, 11, 2, 15d),
					new ExpectedCellValue(sheetName, 12, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsTwoRowsAndNoColumnsSubtotalsOff()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubTotalFunctions = eSubTotalFunctions.None;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("F1:G26"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 6, "January"),
					new ExpectedCellValue(sheetName, 3, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 4, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 6, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 7, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 8, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 9, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 10, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 11, 6, "February"),
					new ExpectedCellValue(sheetName, 12, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 14, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 15, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 17, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 18, 6, "March"),
					new ExpectedCellValue(sheetName, 19, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 21, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 22, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 23, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 24, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 25, 6, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 26, 6, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 4, 7, 2d),
					new ExpectedCellValue(sheetName, 5, 7, 2d),
					new ExpectedCellValue(sheetName, 6, 7, 1d),
					new ExpectedCellValue(sheetName, 8, 7, 831.5),
					new ExpectedCellValue(sheetName, 9, 7, 831.5),
					new ExpectedCellValue(sheetName, 10, 7, 415.75),
					new ExpectedCellValue(sheetName, 13, 7, 6d),
					new ExpectedCellValue(sheetName, 14, 7, 1d),
					new ExpectedCellValue(sheetName, 16, 7, 1194d),
					new ExpectedCellValue(sheetName, 17, 7, 99d),
					new ExpectedCellValue(sheetName, 20, 7, 1d),
					new ExpectedCellValue(sheetName, 21, 7, 2d),
					new ExpectedCellValue(sheetName, 23, 7, 24.99),
					new ExpectedCellValue(sheetName, 24, 7, 831.5),
					new ExpectedCellValue(sheetName, 25, 7, 15d),
					new ExpectedCellValue(sheetName, 26, 7, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsTwoRowsAndNoColumnsSubtotalsOn()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("F1:G32"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 6, "January"),
					new ExpectedCellValue(sheetName, 3, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 4, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 6, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 7, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 8, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 9, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 10, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 11, 6, "January Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 12, 6, "January Sum of Total"),
					new ExpectedCellValue(sheetName, 13, 6, "February"),
					new ExpectedCellValue(sheetName, 14, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 16, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 17, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 18, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 19, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 20, 6, "February Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 21, 6, "February Sum of Total"),
					new ExpectedCellValue(sheetName, 22, 6, "March"),
					new ExpectedCellValue(sheetName, 23, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 24, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 25, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 26, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 27, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 28, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 29, 6, "March Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 30, 6, "March Sum of Total"),
					new ExpectedCellValue(sheetName, 31, 6, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 32, 6, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 4, 7, 2d),
					new ExpectedCellValue(sheetName, 5, 7, 2d),
					new ExpectedCellValue(sheetName, 6, 7, 1d),
					new ExpectedCellValue(sheetName, 8, 7, 831.5),
					new ExpectedCellValue(sheetName, 9, 7, 831.5),
					new ExpectedCellValue(sheetName, 10, 7, 415.75),
					new ExpectedCellValue(sheetName, 11, 7, 5d),
					new ExpectedCellValue(sheetName, 12, 7, 2078.75),
					new ExpectedCellValue(sheetName, 15, 7, 6d),
					new ExpectedCellValue(sheetName, 16, 7, 1d),
					new ExpectedCellValue(sheetName, 18, 7, 1194d),
					new ExpectedCellValue(sheetName, 19, 7, 99d),
					new ExpectedCellValue(sheetName, 20, 7, 7d),
					new ExpectedCellValue(sheetName, 21, 7, 1293d),
					new ExpectedCellValue(sheetName, 24, 7, 1d),
					new ExpectedCellValue(sheetName, 25, 7, 2d),
					new ExpectedCellValue(sheetName, 27, 7, 24.99),
					new ExpectedCellValue(sheetName, 28, 7, 831.5),
					new ExpectedCellValue(sheetName, 29, 7, 3d),
					new ExpectedCellValue(sheetName, 30, 7, 856.49),
					new ExpectedCellValue(sheetName, 31, 7, 15d),
					new ExpectedCellValue(sheetName, 32, 7, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsTwoRowsAndNoColumnsLastColumnDataField()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("K1:L33"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 11, "January"),
					new ExpectedCellValue(sheetName, 3, 11, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 11, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 6, 11, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 11, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 8, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 9, 11, "San Francisco"),
					new ExpectedCellValue(sheetName, 10, 11, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 11, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 12, 11, "January Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 11, "January Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 11, "February"),
					new ExpectedCellValue(sheetName, 15, 11, "Nashville"),
					new ExpectedCellValue(sheetName, 16, 11, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 17, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 18, 11, "San Francisco"),
					new ExpectedCellValue(sheetName, 19, 11, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 21, 11, "February Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 11, "February Sum of Total"),
					new ExpectedCellValue(sheetName, 23, 11, "March"),
					new ExpectedCellValue(sheetName, 24, 11, "Chicago"),
					new ExpectedCellValue(sheetName, 25, 11, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 26, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 27, 11, "Nashville"),
					new ExpectedCellValue(sheetName, 28, 11, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 29, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 30, 11, "March Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 31, 11, "March Sum of Total"),
					new ExpectedCellValue(sheetName, 32, 11, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 11, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 4, 12, 2d),
					new ExpectedCellValue(sheetName, 5, 12, 831.5d),
					new ExpectedCellValue(sheetName, 7, 12, 2d),
					new ExpectedCellValue(sheetName, 8, 12, 831.5d),
					new ExpectedCellValue(sheetName, 10, 12, 1d),
					new ExpectedCellValue(sheetName, 11, 12, 415.75d),
					new ExpectedCellValue(sheetName, 12, 12, 5d),
					new ExpectedCellValue(sheetName, 13, 12, 2078.75),
					new ExpectedCellValue(sheetName, 16, 12, 6d),
					new ExpectedCellValue(sheetName, 17, 12, 1194d),
					new ExpectedCellValue(sheetName, 19, 12, 1d),
					new ExpectedCellValue(sheetName, 20, 12, 99d),
					new ExpectedCellValue(sheetName, 21, 12, 7d),
					new ExpectedCellValue(sheetName, 22, 12, 1293d),
					new ExpectedCellValue(sheetName, 25, 12, 1d),
					new ExpectedCellValue(sheetName, 26, 12, 24.99),
					new ExpectedCellValue(sheetName, 28, 12, 2d),
					new ExpectedCellValue(sheetName, 29, 12, 831.5),
					new ExpectedCellValue(sheetName, 30, 12, 3d),
					new ExpectedCellValue(sheetName, 31, 12, 856.49),
					new ExpectedCellValue(sheetName, 32, 12, 15),
					new ExpectedCellValue(sheetName, 33, 12, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsThreeRowsAndNoColumnsLastColumnDataField()
		{
			var file = new FileInfo("PivotTableWithMultipleRowDataFieldsNoColumnField.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("P1:Q54"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 16, "January"),
					new ExpectedCellValue(sheetName, 3, 16, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 16, "20100007"),
					new ExpectedCellValue(sheetName, 5, 16, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 6, 16, "Sum of Total"),
					new ExpectedCellValue(sheetName, 7, 16, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 8, 16, "Chicago Sum of Total"),
					new ExpectedCellValue(sheetName, 9, 16, "Nashville"),
					new ExpectedCellValue(sheetName, 10, 16, "20100090"),
					new ExpectedCellValue(sheetName, 11, 16, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 12, 16, "Sum of Total"),
					new ExpectedCellValue(sheetName, 13, 16, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 16, "Nashville Sum of Total"),
					new ExpectedCellValue(sheetName, 15, 16, "San Francisco"),
					new ExpectedCellValue(sheetName, 16, 16, "20100076"),
					new ExpectedCellValue(sheetName, 17, 16, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 18, 16, "Sum of Total"),
					new ExpectedCellValue(sheetName, 19, 16, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 16, "San Francisco Sum of Total"),
					new ExpectedCellValue(sheetName, 21, 16, "January Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 16, "January Sum of Total"),
					new ExpectedCellValue(sheetName, 23, 16, "February"),
					new ExpectedCellValue(sheetName, 24, 16, "Nashville"),
					new ExpectedCellValue(sheetName, 25, 16, "20100070"),
					new ExpectedCellValue(sheetName, 26, 16, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 27, 16, "Sum of Total"),
					new ExpectedCellValue(sheetName, 28, 16, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 29, 16, "Nashville Sum of Total"),
					new ExpectedCellValue(sheetName, 30, 16, "San Francisco"),
					new ExpectedCellValue(sheetName, 31, 16, "20100085"),
					new ExpectedCellValue(sheetName, 32, 16, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 16, "Sum of Total"),
					new ExpectedCellValue(sheetName, 34, 16, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 35, 16, "San Francisco Sum of Total"),
					new ExpectedCellValue(sheetName, 36, 16, "February Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 37, 16, "February Sum of Total"),
					new ExpectedCellValue(sheetName, 38, 16, "March"),
					new ExpectedCellValue(sheetName, 39, 16, "Chicago"),
					new ExpectedCellValue(sheetName, 40, 16, "20100083"),
					new ExpectedCellValue(sheetName, 41, 16, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 42, 16, "Sum of Total"),
					new ExpectedCellValue(sheetName, 43, 16, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 44, 16, "Chicago Sum of Total"),
					new ExpectedCellValue(sheetName, 45, 16, "Nashville"),
					new ExpectedCellValue(sheetName, 46, 16, "20100017"),
					new ExpectedCellValue(sheetName, 47, 16, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 48, 16, "Sum of Total"),
					new ExpectedCellValue(sheetName, 49, 16, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 50, 16, "Nashville Sum of Total"),
					new ExpectedCellValue(sheetName, 51, 16, "March Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 52, 16, "March Sum of Total"),
					new ExpectedCellValue(sheetName, 53, 16, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 54, 16, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 5, 17, 2d),
					new ExpectedCellValue(sheetName, 6, 17, 831.5d),
					new ExpectedCellValue(sheetName, 7, 17, 2d),
					new ExpectedCellValue(sheetName, 8, 17, 831.5d),
					new ExpectedCellValue(sheetName, 11, 17, 2d),
					new ExpectedCellValue(sheetName, 12, 17, 831.5d),
					new ExpectedCellValue(sheetName, 13, 17, 2d),
					new ExpectedCellValue(sheetName, 14, 17, 831.5d),
					new ExpectedCellValue(sheetName, 17, 17, 1d),
					new ExpectedCellValue(sheetName, 18, 17, 415.75d),
					new ExpectedCellValue(sheetName, 19, 17, 1d),
					new ExpectedCellValue(sheetName, 20, 17, 415.75d),
					new ExpectedCellValue(sheetName, 21, 17, 5d),
					new ExpectedCellValue(sheetName, 22, 17, 2078.75),
					new ExpectedCellValue(sheetName, 26, 17, 6d),
					new ExpectedCellValue(sheetName, 27, 17, 1194d),
					new ExpectedCellValue(sheetName, 28, 17, 6d),
					new ExpectedCellValue(sheetName, 29, 17, 1194d),
					new ExpectedCellValue(sheetName, 32, 17, 1d),
					new ExpectedCellValue(sheetName, 33, 17, 99d),
					new ExpectedCellValue(sheetName, 34, 17, 1d),
					new ExpectedCellValue(sheetName, 35, 17, 99d),
					new ExpectedCellValue(sheetName, 36, 17, 7d),
					new ExpectedCellValue(sheetName, 37, 17, 1293d),
					new ExpectedCellValue(sheetName, 41, 17, 1d),
					new ExpectedCellValue(sheetName, 42, 17, 24.99),
					new ExpectedCellValue(sheetName, 43, 17, 1d),
					new ExpectedCellValue(sheetName, 44, 17, 24.99),
					new ExpectedCellValue(sheetName, 47, 17, 2d),
					new ExpectedCellValue(sheetName, 48, 17, 831.5),
					new ExpectedCellValue(sheetName, 49, 17, 2d),
					new ExpectedCellValue(sheetName, 50, 17, 831.5),
					new ExpectedCellValue(sheetName, 51, 17, 3d),
					new ExpectedCellValue(sheetName, 52, 17, 856.49),
					new ExpectedCellValue(sheetName, 53, 17, 15),
					new ExpectedCellValue(sheetName, 54, 17, 4228.24)
				});
			}
		}

		#region Pivot Tables with page fields
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithPageFields.xlsx")]
		public void PivotTableRefreshWithPageFields()
		{
			var file = new FileInfo("PivotTableWithPageFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Sheet1"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					pivotTable.PageFields[0].Item = 1;
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("J9:M18"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 9, 10, null),
					new ExpectedCellValue(sheetName, 9, 11, "Column Labels"),
					new ExpectedCellValue(sheetName, 10, 10, "Row Labels"),
					new ExpectedCellValue(sheetName, 10, 11, "Nashville"),
					new ExpectedCellValue(sheetName, 10, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 10, 13, "Grand Total"),
					new ExpectedCellValue(sheetName, 11, 10, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 12, 10, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 12, 12, 99d),
					new ExpectedCellValue(sheetName, 12, 13, 99d),
					new ExpectedCellValue(sheetName, 13, 10, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 12, 1d),
					new ExpectedCellValue(sheetName, 13, 13, 1d),
					new ExpectedCellValue(sheetName, 14, 10, "Tent"),
					new ExpectedCellValue(sheetName, 15, 10, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 15, 11, 199d),
					new ExpectedCellValue(sheetName, 15, 13, 199d),
					new ExpectedCellValue(sheetName, 16, 10, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 11, 6d),
					new ExpectedCellValue(sheetName, 16, 13, 6d),
					new ExpectedCellValue(sheetName, 17, 10, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 17, 11, 199d),
					new ExpectedCellValue(sheetName, 17, 12, 99d),
					new ExpectedCellValue(sheetName, 17, 13, 298d),
					new ExpectedCellValue(sheetName, 18, 10, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 18, 11, 6d),
					new ExpectedCellValue(sheetName, 18, 12, 1d),
					new ExpectedCellValue(sheetName, 18, 13, 7d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithPageFields.xlsx")]
		public void PivotTableRefreshWithPageFieldsMultipleFilters()
		{
			var file = new FileInfo("PivotTableWithPageFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Sheet1"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					pivotTable.PageFields[0].Item = 1;
					pivotTable.PageFields[1].Item = 2;
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("J9:L15"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 9, 10, null),
					new ExpectedCellValue(sheetName, 9, 11, "Column Labels"),
					new ExpectedCellValue(sheetName, 10, 10, "Row Labels"),
					new ExpectedCellValue(sheetName, 10, 11, "Nashville"),
					new ExpectedCellValue(sheetName, 10, 12, "Grand Total"),
					new ExpectedCellValue(sheetName, 11, 10, "Tent"),
					new ExpectedCellValue(sheetName, 12, 10, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 12, 11, 199d),
					new ExpectedCellValue(sheetName, 12, 12, 199d),
					new ExpectedCellValue(sheetName, 13, 10, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 11, 6d),
					new ExpectedCellValue(sheetName, 13, 12, 6d),
					new ExpectedCellValue(sheetName, 14, 10, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 14, 11, 199d),
					new ExpectedCellValue(sheetName, 14, 12, 199d),
					new ExpectedCellValue(sheetName, 15, 10, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 11, 6d),
					new ExpectedCellValue(sheetName, 15, 12, 6d),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithPageFields.xlsx")]
		public void PivotTableRefreshWithMultiSelectPageFields()
		{
			var file = new FileInfo("PivotTableWithPageFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Sheet1"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					pivotTable.Fields[2].Items[1].Hidden = true;
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("J9:N18"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 9, 10, null),
					new ExpectedCellValue(sheetName, 9, 11, "Column Labels"),
					new ExpectedCellValue(sheetName, 10, 10, "Row Labels"),
					new ExpectedCellValue(sheetName, 10, 11, "Chicago"),
					new ExpectedCellValue(sheetName, 10, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 10, 13, "San Francisco"),
					new ExpectedCellValue(sheetName, 10, 14, "Grand Total"),
					new ExpectedCellValue(sheetName, 11, 10, "Car Rack"),
					new ExpectedCellValue(sheetName, 12, 10, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 12, 11, 415.75),
					new ExpectedCellValue(sheetName, 12, 12, 831.5),
					new ExpectedCellValue(sheetName, 12, 13, 415.75),
					new ExpectedCellValue(sheetName, 12, 14, 1663d),
					new ExpectedCellValue(sheetName, 13, 10, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 11, 2d),
					new ExpectedCellValue(sheetName, 13, 12, 4d),
					new ExpectedCellValue(sheetName, 13, 13, 1d),
					new ExpectedCellValue(sheetName, 13, 14, 7d),
					new ExpectedCellValue(sheetName, 14, 10, "Headlamp"),
					new ExpectedCellValue(sheetName, 15, 10, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 15, 11, 24.99),
					new ExpectedCellValue(sheetName, 15, 14, 24.99),
					new ExpectedCellValue(sheetName, 16, 10, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 11, 1d),
					new ExpectedCellValue(sheetName, 16, 14, 1d),
					new ExpectedCellValue(sheetName, 17, 10, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 17, 11, 440.74),
					new ExpectedCellValue(sheetName, 17, 12, 831.5),
					new ExpectedCellValue(sheetName, 17, 13, 415.75),
					new ExpectedCellValue(sheetName, 17, 14, 1687.99),
					new ExpectedCellValue(sheetName, 18, 10, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 18, 11, 3d),
					new ExpectedCellValue(sheetName, 18, 12, 4d),
					new ExpectedCellValue(sheetName, 18, 13, 1d),
					new ExpectedCellValue(sheetName, 18, 14, 8d)
				});
			}
		}
		#endregion
		#endregion

		#region Multiple Column Data Fields
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAtLeafNode()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:I7"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 6, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 7, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 3, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 4, 2, 2d),
					new ExpectedCellValue(sheetName, 5, 2, 2d),
					new ExpectedCellValue(sheetName, 6, 2, 1d),
					new ExpectedCellValue(sheetName, 7, 2, 5d),
					new ExpectedCellValue(sheetName, 3, 3, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 4, 3, 415.75),
					new ExpectedCellValue(sheetName, 5, 3, 415.75),
					new ExpectedCellValue(sheetName, 6, 3, 415.75),
					new ExpectedCellValue(sheetName, 7, 3, 1247.25),
					new ExpectedCellValue(sheetName, 2, 4, "February"),
					new ExpectedCellValue(sheetName, 3, 4, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 4, 6d),
					new ExpectedCellValue(sheetName, 6, 4, 1d),
					new ExpectedCellValue(sheetName, 7, 4, 7d),
					new ExpectedCellValue(sheetName, 3, 5, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 5, 5, 199d),
					new ExpectedCellValue(sheetName, 6, 5, 99d),
					new ExpectedCellValue(sheetName, 7, 5, 298d),
					new ExpectedCellValue(sheetName, 2, 6, "March"),
					new ExpectedCellValue(sheetName, 3, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 4, 6, 1d),
					new ExpectedCellValue(sheetName, 5, 6, 2d),
					new ExpectedCellValue(sheetName, 7, 6, 3d),
					new ExpectedCellValue(sheetName, 3, 7, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 4, 7, 24.99),
					new ExpectedCellValue(sheetName, 5, 7, 415.75),
					new ExpectedCellValue(sheetName, 7, 7, 440.74),
					new ExpectedCellValue(sheetName, 2, 8, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 4, 8, 3d),
					new ExpectedCellValue(sheetName, 5, 8, 10d),
					new ExpectedCellValue(sheetName, 6, 8, 2d),
					new ExpectedCellValue(sheetName, 7, 8, 15d),
					new ExpectedCellValue(sheetName, 2, 9, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 4, 9, 440.74),
					new ExpectedCellValue(sheetName, 5, 9, 1030.5),
					new ExpectedCellValue(sheetName, 6, 9, 514.75),
					new ExpectedCellValue(sheetName, 7, 9, 1985.99),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAsParentRowDepthTwo()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A11:I17"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 14, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 15, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 16, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 17, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 12, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 2, "January"),
					new ExpectedCellValue(sheetName, 14, 2, 2d),
					new ExpectedCellValue(sheetName, 15, 2, 2d),
					new ExpectedCellValue(sheetName, 16, 2, 1d),
					new ExpectedCellValue(sheetName, 17, 2, 5d),
					new ExpectedCellValue(sheetName, 13, 3, "February"),
					new ExpectedCellValue(sheetName, 15, 3, 6d),
					new ExpectedCellValue(sheetName, 16, 3, 1d),
					new ExpectedCellValue(sheetName, 17, 3, 7d),
					new ExpectedCellValue(sheetName, 13, 4, "March"),
					new ExpectedCellValue(sheetName, 14, 4, 1d),
					new ExpectedCellValue(sheetName, 15, 4, 2d),
					new ExpectedCellValue(sheetName, 17, 4, 3d),
					new ExpectedCellValue(sheetName, 12, 5, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 13, 5, "January"),
					new ExpectedCellValue(sheetName, 14, 5, 415.75),
					new ExpectedCellValue(sheetName, 15, 5, 415.75),
					new ExpectedCellValue(sheetName, 16, 5, 415.75),
					new ExpectedCellValue(sheetName, 17, 5, 1247.25),
					new ExpectedCellValue(sheetName, 13, 6, "February"),
					new ExpectedCellValue(sheetName, 15, 6, 199d),
					new ExpectedCellValue(sheetName, 16, 6, 99d),
					new ExpectedCellValue(sheetName, 17, 6, 298d),
					new ExpectedCellValue(sheetName, 13, 7, "March"),
					new ExpectedCellValue(sheetName, 14, 7, 24.99),
					new ExpectedCellValue(sheetName, 15, 7, 415.75),
					new ExpectedCellValue(sheetName, 17, 7, 440.74),
					new ExpectedCellValue(sheetName, 12, 8, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 8, 3d),
					new ExpectedCellValue(sheetName, 15, 8, 10d),
					new ExpectedCellValue(sheetName, 16, 8, 2d),
					new ExpectedCellValue(sheetName, 17, 8, 15d),
					new ExpectedCellValue(sheetName, 12, 9, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 14, 9, 440.74),
					new ExpectedCellValue(sheetName, 15, 9, 1030.5),
					new ExpectedCellValue(sheetName, 16, 9, 514.75),
					new ExpectedCellValue(sheetName, 17, 9, 1985.99),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAsParentNodeColumnDepthThreeSubtotalsOn()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A21:W29"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 25, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 26, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 27, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 28, 1, "Tent"),
					new ExpectedCellValue(sheetName, 29, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 22, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 23, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 24, 2, "January"),
					new ExpectedCellValue(sheetName, 25, 2, 2d),
					new ExpectedCellValue(sheetName, 29, 2, 2d),
					new ExpectedCellValue(sheetName, 24, 3, "March"),
					new ExpectedCellValue(sheetName, 26, 3, 1d),
					new ExpectedCellValue(sheetName, 29, 3, 1d),
					new ExpectedCellValue(sheetName, 23, 4, "Chicago Total"),
					new ExpectedCellValue(sheetName, 25, 4, 2d),
					new ExpectedCellValue(sheetName, 26, 4, 1d),
					new ExpectedCellValue(sheetName, 29, 4, 3d),
					new ExpectedCellValue(sheetName, 23, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 24, 5, "January"),
					new ExpectedCellValue(sheetName, 25, 5, 2d),
					new ExpectedCellValue(sheetName, 29, 5, 2d),
					new ExpectedCellValue(sheetName, 24, 6, "February"),
					new ExpectedCellValue(sheetName, 28, 6, 6d),
					new ExpectedCellValue(sheetName, 29, 6, 6d),
					new ExpectedCellValue(sheetName, 24, 7, "March"),
					new ExpectedCellValue(sheetName, 25, 7, 2d),
					new ExpectedCellValue(sheetName, 29, 7, 2d),
					new ExpectedCellValue(sheetName, 23, 8, "Nashville Total"),
					new ExpectedCellValue(sheetName, 25, 8, 4d),
					new ExpectedCellValue(sheetName, 28, 8, 6d),
					new ExpectedCellValue(sheetName, 29, 8, 10d),
					new ExpectedCellValue(sheetName, 23, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 24, 9, "January"),
					new ExpectedCellValue(sheetName, 25, 9, 1d),
					new ExpectedCellValue(sheetName, 29, 9, 1d),
					new ExpectedCellValue(sheetName, 24, 10, "February"),
					new ExpectedCellValue(sheetName, 27, 10, 1d),
					new ExpectedCellValue(sheetName, 29, 10, 1d),
					new ExpectedCellValue(sheetName, 23, 11, "San Francisco Total"),
					new ExpectedCellValue(sheetName, 25, 11, 1d),
					new ExpectedCellValue(sheetName, 27, 11, 1d),
					new ExpectedCellValue(sheetName, 29, 11, 2d),
					new ExpectedCellValue(sheetName, 22, 12, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 23, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 24, 12, "January"),
					new ExpectedCellValue(sheetName, 25, 12, 415.75),
					new ExpectedCellValue(sheetName, 29, 12, 415.75),
					new ExpectedCellValue(sheetName, 24, 13, "March"),
					new ExpectedCellValue(sheetName, 26, 13, 24.99),
					new ExpectedCellValue(sheetName, 29, 13, 24.99),
					new ExpectedCellValue(sheetName, 23, 14, "Chicago Total"),
					new ExpectedCellValue(sheetName, 25, 14, 415.75),
					new ExpectedCellValue(sheetName, 26, 14, 24.99),
					new ExpectedCellValue(sheetName, 29, 14, 440.74),
					new ExpectedCellValue(sheetName, 23, 15, "Nashville"),
					new ExpectedCellValue(sheetName, 24, 15, "January"),
					new ExpectedCellValue(sheetName, 25, 15, 415.75),
					new ExpectedCellValue(sheetName, 29, 15, 415.75),
					new ExpectedCellValue(sheetName, 24, 16, "February"),
					new ExpectedCellValue(sheetName, 28, 16, 199d),
					new ExpectedCellValue(sheetName, 29, 16, 199d),
					new ExpectedCellValue(sheetName, 24, 17, "March"),
					new ExpectedCellValue(sheetName, 25, 17, 415.75),
					new ExpectedCellValue(sheetName, 29, 17, 415.75),
					new ExpectedCellValue(sheetName, 23, 18, "Nashville Total"),
					new ExpectedCellValue(sheetName, 25, 18, 831.5),
					new ExpectedCellValue(sheetName, 28, 18, 199d),
					new ExpectedCellValue(sheetName, 29, 18, 1030.5),
					new ExpectedCellValue(sheetName, 23, 19, "San Francisco"),
					new ExpectedCellValue(sheetName, 24, 19, "January"),
					new ExpectedCellValue(sheetName, 25, 19, 415.75),
					new ExpectedCellValue(sheetName, 29, 19, 415.75),
					new ExpectedCellValue(sheetName, 24, 20, "February"),
					new ExpectedCellValue(sheetName, 27, 20, 99d),
					new ExpectedCellValue(sheetName, 29, 20, 99d),
					new ExpectedCellValue(sheetName, 23, 21, "San Francisco Total"),
					new ExpectedCellValue(sheetName, 25, 21, 415.75),
					new ExpectedCellValue(sheetName, 27, 21, 99d),
					new ExpectedCellValue(sheetName, 29, 21, 514.75),
					new ExpectedCellValue(sheetName, 22, 22, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 25, 22, 7d),
					new ExpectedCellValue(sheetName, 26, 22, 1d),
					new ExpectedCellValue(sheetName, 27, 22, 1d),
					new ExpectedCellValue(sheetName, 28, 22, 6d),
					new ExpectedCellValue(sheetName, 29, 22, 15d),
					new ExpectedCellValue(sheetName, 22, 23, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 25, 23, 1663d),
					new ExpectedCellValue(sheetName, 26, 23, 24.99),
					new ExpectedCellValue(sheetName, 27, 23, 99d),
					new ExpectedCellValue(sheetName, 28, 23, 199d),
					new ExpectedCellValue(sheetName, 29, 23, 1985.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAsParentNodeColumnDepthThreeSubtotalsOff()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubTotalFunctions = eSubTotalFunctions.None;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A21:Q29"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 25, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 26, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 27, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 28, 1, "Tent"),
					new ExpectedCellValue(sheetName, 29, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 22, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 23, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 24, 2, "January"),
					new ExpectedCellValue(sheetName, 25, 2, 2d),
					new ExpectedCellValue(sheetName, 29, 2, 2d),
					new ExpectedCellValue(sheetName, 24, 3, "March"),
					new ExpectedCellValue(sheetName, 26, 3, 1d),
					new ExpectedCellValue(sheetName, 29, 3, 1d),
					new ExpectedCellValue(sheetName, 23, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 24, 4, "January"),
					new ExpectedCellValue(sheetName, 25, 4, 2d),
					new ExpectedCellValue(sheetName, 29, 4, 2d),
					new ExpectedCellValue(sheetName, 24, 5, "February"),
					new ExpectedCellValue(sheetName, 28, 5, 6d),
					new ExpectedCellValue(sheetName, 29, 5, 6d),
					new ExpectedCellValue(sheetName, 24, 6, "March"),
					new ExpectedCellValue(sheetName, 25, 6, 2d),
					new ExpectedCellValue(sheetName, 29, 6, 2d),
					new ExpectedCellValue(sheetName, 23, 7, "San Francisco"),
					new ExpectedCellValue(sheetName, 24, 7, "January"),
					new ExpectedCellValue(sheetName, 25, 7, 1d),
					new ExpectedCellValue(sheetName, 29, 7, 1d),
					new ExpectedCellValue(sheetName, 24, 8, "February"),
					new ExpectedCellValue(sheetName, 27, 8, 1d),
					new ExpectedCellValue(sheetName, 29, 8, 1d),
					new ExpectedCellValue(sheetName, 22, 9, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 23, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 24, 9, "January"),
					new ExpectedCellValue(sheetName, 25, 9, 415.75),
					new ExpectedCellValue(sheetName, 29, 9, 415.75),
					new ExpectedCellValue(sheetName, 24, 10, "March"),
					new ExpectedCellValue(sheetName, 26, 10, 24.99),
					new ExpectedCellValue(sheetName, 29, 10, 24.99),
					new ExpectedCellValue(sheetName, 23, 11, "Nashville"),
					new ExpectedCellValue(sheetName, 24, 11, "January"),
					new ExpectedCellValue(sheetName, 25, 11, 415.75),
					new ExpectedCellValue(sheetName, 29, 11, 415.75),
					new ExpectedCellValue(sheetName, 24, 12, "February"),
					new ExpectedCellValue(sheetName, 28, 12, 199d),
					new ExpectedCellValue(sheetName, 29, 12, 199d),
					new ExpectedCellValue(sheetName, 24, 13, "March"),
					new ExpectedCellValue(sheetName, 25, 13, 415.75),
					new ExpectedCellValue(sheetName, 29, 13, 415.75),
					new ExpectedCellValue(sheetName, 23, 14, "San Francisco"),
					new ExpectedCellValue(sheetName, 24, 14, "January"),
					new ExpectedCellValue(sheetName, 25, 14, 415.75),
					new ExpectedCellValue(sheetName, 29, 14, 415.75),
					new ExpectedCellValue(sheetName, 24, 15, "February"),
					new ExpectedCellValue(sheetName, 27, 15, 99d),
					new ExpectedCellValue(sheetName, 29, 15, 99d),
					new ExpectedCellValue(sheetName, 22, 16, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 25, 16, 7d),
					new ExpectedCellValue(sheetName, 26, 16, 1d),
					new ExpectedCellValue(sheetName, 27, 16, 1d),
					new ExpectedCellValue(sheetName, 28, 16, 6d),
					new ExpectedCellValue(sheetName, 29, 16, 15d),
					new ExpectedCellValue(sheetName, 22, 17, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 25, 17, 1663d),
					new ExpectedCellValue(sheetName, 26, 17, 24.99),
					new ExpectedCellValue(sheetName, 27, 17, 99d),
					new ExpectedCellValue(sheetName, 28, 17, 199d),
					new ExpectedCellValue(sheetName, 29, 17, 1985.99),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAsInnerChildSubtotalsOn()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A33:S40"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 37, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 38, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 39, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 40, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 34, 2, "January"),
					new ExpectedCellValue(sheetName, 35, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 36, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 37, 2, 2d),
					new ExpectedCellValue(sheetName, 38, 2, 2d),
					new ExpectedCellValue(sheetName, 39, 2, 1d),
					new ExpectedCellValue(sheetName, 40, 2, 5d),
					new ExpectedCellValue(sheetName, 35, 3, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 36, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 37, 3, 415.75),
					new ExpectedCellValue(sheetName, 38, 3, 415.75),
					new ExpectedCellValue(sheetName, 39, 3, 415.75),
					new ExpectedCellValue(sheetName, 40, 3, 1247.25),
					new ExpectedCellValue(sheetName, 34, 4, "January Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 37, 4, 2d),
					new ExpectedCellValue(sheetName, 38, 4, 2d),
					new ExpectedCellValue(sheetName, 39, 4, 1d),
					new ExpectedCellValue(sheetName, 40, 4, 5d),
					new ExpectedCellValue(sheetName, 34, 5, "January Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 37, 5, 415.75),
					new ExpectedCellValue(sheetName, 38, 5, 415.75),
					new ExpectedCellValue(sheetName, 39, 5, 415.75),
					new ExpectedCellValue(sheetName, 40, 5, 1247.25),
					new ExpectedCellValue(sheetName, 34, 6, "February"),
					new ExpectedCellValue(sheetName, 35, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 36, 6, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 39, 6, 1d),
					new ExpectedCellValue(sheetName, 40, 6, 1d),
					new ExpectedCellValue(sheetName, 36, 7, "Tent"),
					new ExpectedCellValue(sheetName, 38, 7, 6d),
					new ExpectedCellValue(sheetName, 40, 7, 6d),
					new ExpectedCellValue(sheetName, 35, 8, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 36, 8, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 39, 8, 99d),
					new ExpectedCellValue(sheetName, 40, 8, 99d),
					new ExpectedCellValue(sheetName, 36, 9, "Tent"),
					new ExpectedCellValue(sheetName, 38, 9, 199d),
					new ExpectedCellValue(sheetName, 40, 9, 199d),
					new ExpectedCellValue(sheetName, 34, 10, "February Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 38, 10, 6d),
					new ExpectedCellValue(sheetName, 39, 10, 1d),
					new ExpectedCellValue(sheetName, 40, 10, 7d),
					new ExpectedCellValue(sheetName, 34, 11, "February Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 38, 11, 199d),
					new ExpectedCellValue(sheetName, 39, 11, 99d),
					new ExpectedCellValue(sheetName, 40, 11, 298d),
					new ExpectedCellValue(sheetName, 34, 12, "March"),
					new ExpectedCellValue(sheetName, 35, 12, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 36, 12, "Car Rack"),
					new ExpectedCellValue(sheetName, 38, 12, 2d),
					new ExpectedCellValue(sheetName, 40, 12, 2d),
					new ExpectedCellValue(sheetName, 36, 13, "Headlamp"),
					new ExpectedCellValue(sheetName, 37, 13, 1d),
					new ExpectedCellValue(sheetName, 40, 13, 1d),
					new ExpectedCellValue(sheetName, 35, 14, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 36, 14, "Car Rack"),
					new ExpectedCellValue(sheetName, 38, 14, 415.75),
					new ExpectedCellValue(sheetName, 40, 14, 415.75),
					new ExpectedCellValue(sheetName, 36, 15, "Headlamp"),
					new ExpectedCellValue(sheetName, 37, 15, 24.99),
					new ExpectedCellValue(sheetName, 40, 15, 24.99),
					new ExpectedCellValue(sheetName, 34, 16, "March Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 37, 16, 1d),
					new ExpectedCellValue(sheetName, 38, 16, 2d),
					new ExpectedCellValue(sheetName, 40, 16, 3d),
					new ExpectedCellValue(sheetName, 34, 17, "March Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 37, 17, 24.99),
					new ExpectedCellValue(sheetName, 38, 17, 415.75),
					new ExpectedCellValue(sheetName, 40, 17, 440.74),
					new ExpectedCellValue(sheetName, 34, 18, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 37, 18, 3d),
					new ExpectedCellValue(sheetName, 38, 18, 10d),
					new ExpectedCellValue(sheetName, 39, 18, 2d),
					new ExpectedCellValue(sheetName, 40, 18, 15d),
					new ExpectedCellValue(sheetName, 34, 19, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 37, 19, 440.74),
					new ExpectedCellValue(sheetName, 38, 19, 1030.5),
					new ExpectedCellValue(sheetName, 39, 19, 514.75),
					new ExpectedCellValue(sheetName, 40, 19, 1985.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAsInnerChildSubtotalsOff()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubTotalFunctions = eSubTotalFunctions.None;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A33:M40"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 37, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 38, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 39, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 40, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 34, 2, "January"),
					new ExpectedCellValue(sheetName, 35, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 36, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 37, 2, 2d),
					new ExpectedCellValue(sheetName, 38, 2, 2d),
					new ExpectedCellValue(sheetName, 39, 2, 1d),
					new ExpectedCellValue(sheetName, 40, 2, 5d),
					new ExpectedCellValue(sheetName, 35, 3, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 36, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 37, 3, 415.75),
					new ExpectedCellValue(sheetName, 38, 3, 415.75),
					new ExpectedCellValue(sheetName, 39, 3, 415.75),
					new ExpectedCellValue(sheetName, 40, 3, 1247.25),
					new ExpectedCellValue(sheetName, 34, 4, "February"),
					new ExpectedCellValue(sheetName, 35, 4, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 36, 4, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 39, 4, 1d),
					new ExpectedCellValue(sheetName, 40, 4, 1d),
					new ExpectedCellValue(sheetName, 36, 5, "Tent"),
					new ExpectedCellValue(sheetName, 38, 5, 6d),
					new ExpectedCellValue(sheetName, 40, 5, 6d),
					new ExpectedCellValue(sheetName, 35, 6, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 36, 6, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 39, 6, 99d),
					new ExpectedCellValue(sheetName, 40, 6, 99d),
					new ExpectedCellValue(sheetName, 36, 7, "Tent"),
					new ExpectedCellValue(sheetName, 38, 7, 199d),
					new ExpectedCellValue(sheetName, 40, 7, 199d),
					new ExpectedCellValue(sheetName, 34, 8, "March"),
					new ExpectedCellValue(sheetName, 35, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 36, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 38, 8, 2d),
					new ExpectedCellValue(sheetName, 40, 8, 2d),
					new ExpectedCellValue(sheetName, 36, 9, "Headlamp"),
					new ExpectedCellValue(sheetName, 37, 9, 1d),
					new ExpectedCellValue(sheetName, 40, 9, 1d),
					new ExpectedCellValue(sheetName, 35, 10, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 36, 10, "Car Rack"),
					new ExpectedCellValue(sheetName, 38, 10, 415.75),
					new ExpectedCellValue(sheetName, 40, 10, 415.75),
					new ExpectedCellValue(sheetName, 36, 11, "Headlamp"),
					new ExpectedCellValue(sheetName, 37, 11, 24.99),
					new ExpectedCellValue(sheetName, 40, 11, 24.99),
					new ExpectedCellValue(sheetName, 34, 12, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 37, 12, 3d),
					new ExpectedCellValue(sheetName, 38, 12, 10d),
					new ExpectedCellValue(sheetName, 39, 12, 2d),
					new ExpectedCellValue(sheetName, 40, 12, 15d),
					new ExpectedCellValue(sheetName, 34, 13, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 37, 13, 440.74),
					new ExpectedCellValue(sheetName, 38, 13, 1030.5),
					new ExpectedCellValue(sheetName, 39, 13, 514.75),
					new ExpectedCellValue(sheetName, 40, 13, 1985.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAsFirstInnerChildSubtotalsOn()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable5"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A44:AK56"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 49, 1, 20100007),
					new ExpectedCellValue(sheetName, 50, 1, 20100017),
					new ExpectedCellValue(sheetName, 51, 1, 20100070),
					new ExpectedCellValue(sheetName, 52, 1, 20100076),
					new ExpectedCellValue(sheetName, 53, 1, 20100083),
					new ExpectedCellValue(sheetName, 54, 1, 20100085),
					new ExpectedCellValue(sheetName, 55, 1, 20100090),
					new ExpectedCellValue(sheetName, 56, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 45, 2, "January"),
					new ExpectedCellValue(sheetName, 46, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 47, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 48, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 49, 2, 2d),
					new ExpectedCellValue(sheetName, 56, 2, 2d),
					new ExpectedCellValue(sheetName, 47, 3, "Chicago Total"),
					new ExpectedCellValue(sheetName, 49, 3, 2d),
					new ExpectedCellValue(sheetName, 56, 3, 2d),
					new ExpectedCellValue(sheetName, 47, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 4, "Car Rack"),
					new ExpectedCellValue(sheetName, 55, 4, 2d),
					new ExpectedCellValue(sheetName, 56, 4, 2d),
					new ExpectedCellValue(sheetName, 47, 5, "Nashville Total"),
					new ExpectedCellValue(sheetName, 55, 5, 2d),
					new ExpectedCellValue(sheetName, 56, 5, 2d),
					new ExpectedCellValue(sheetName, 47, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 48, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 52, 6, 1d),
					new ExpectedCellValue(sheetName, 56, 6, 1d),
					new ExpectedCellValue(sheetName, 47, 7, "San Francisco Total"),
					new ExpectedCellValue(sheetName, 52, 7, 1d),
					new ExpectedCellValue(sheetName, 56, 7, 1d),
					new ExpectedCellValue(sheetName, 46, 8, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 47, 8, "Chicago"),
					new ExpectedCellValue(sheetName, 48, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 49, 8, 415.75),
					new ExpectedCellValue(sheetName, 56, 8, 415.75),
					new ExpectedCellValue(sheetName, 47, 9, "Chicago Total"),
					new ExpectedCellValue(sheetName, 49, 9, 415.75),
					new ExpectedCellValue(sheetName, 56, 9, 415.75),
					new ExpectedCellValue(sheetName, 47, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 10, "Car Rack"),
					new ExpectedCellValue(sheetName, 55, 10, 415.75),
					new ExpectedCellValue(sheetName, 56, 10, 415.75),
					new ExpectedCellValue(sheetName, 47, 11, "Nashville Total"),
					new ExpectedCellValue(sheetName, 55, 11, 415.75),
					new ExpectedCellValue(sheetName, 56, 11, 415.75),
					new ExpectedCellValue(sheetName, 47, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 48, 12, "Car Rack"),
					new ExpectedCellValue(sheetName, 52, 12, 415.75),
					new ExpectedCellValue(sheetName, 56, 12, 415.75),
					new ExpectedCellValue(sheetName, 47, 13, "San Francisco Total"),
					new ExpectedCellValue(sheetName, 52, 13, 415.75),
					new ExpectedCellValue(sheetName, 56, 13, 415.75),
					new ExpectedCellValue(sheetName, 45, 14, "January Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 49, 14, 2d),
					new ExpectedCellValue(sheetName, 52, 14, 1d),
					new ExpectedCellValue(sheetName, 55, 14, 2d),
					new ExpectedCellValue(sheetName, 56, 14, 5d),
					new ExpectedCellValue(sheetName, 45, 15, "January Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 49, 15, 415.75),
					new ExpectedCellValue(sheetName, 52, 15, 415.75),
					new ExpectedCellValue(sheetName, 55, 15, 415.75),
					new ExpectedCellValue(sheetName, 56, 15, 1247.25),
					new ExpectedCellValue(sheetName, 45, 16, "February"),
					new ExpectedCellValue(sheetName, 46, 16, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 47, 16, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 16, "Tent"),
					new ExpectedCellValue(sheetName, 51, 16, 6d),
					new ExpectedCellValue(sheetName, 56, 16, 6d),
					new ExpectedCellValue(sheetName, 47, 17, "Nashville Total"),
					new ExpectedCellValue(sheetName, 51, 17, 6d),
					new ExpectedCellValue(sheetName, 56, 17, 6d),
					new ExpectedCellValue(sheetName, 47, 18, "San Francisco"),
					new ExpectedCellValue(sheetName, 48, 18, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 54, 18, 1d),
					new ExpectedCellValue(sheetName, 56, 18, 1d),
					new ExpectedCellValue(sheetName, 47, 19, "San Francisco Total"),
					new ExpectedCellValue(sheetName, 54, 19, 1d),
					new ExpectedCellValue(sheetName, 56, 19, 1d),
					new ExpectedCellValue(sheetName, 46, 20, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 47, 20, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 20, "Tent"),
					new ExpectedCellValue(sheetName, 51, 20, 199d),
					new ExpectedCellValue(sheetName, 56, 20, 199d),
					new ExpectedCellValue(sheetName, 47, 21, "Nashville Total"),
					new ExpectedCellValue(sheetName, 51, 21, 199d),
					new ExpectedCellValue(sheetName, 56, 21, 199d),
					new ExpectedCellValue(sheetName, 47, 22, "San Francisco"),
					new ExpectedCellValue(sheetName, 48, 22, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 54, 22, 99d),
					new ExpectedCellValue(sheetName, 56, 22, 99d),
					new ExpectedCellValue(sheetName, 47, 23, "San Francisco Total"),
					new ExpectedCellValue(sheetName, 54, 23, 99d),
					new ExpectedCellValue(sheetName, 56, 23, 99d),
					new ExpectedCellValue(sheetName, 45, 24, "February Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 51, 24, 6d),
					new ExpectedCellValue(sheetName, 54, 24, 1d),
					new ExpectedCellValue(sheetName, 56, 24, 7d),
					new ExpectedCellValue(sheetName, 45, 25, "February Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 51, 25, 199d),
					new ExpectedCellValue(sheetName, 54, 25, 99d),
					new ExpectedCellValue(sheetName, 56, 25, 298d),
					new ExpectedCellValue(sheetName, 45, 26, "March"),
					new ExpectedCellValue(sheetName, 46, 26, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 47, 26, "Chicago"),
					new ExpectedCellValue(sheetName, 48, 26, "Headlamp"),
					new ExpectedCellValue(sheetName, 53, 26, 1d),
					new ExpectedCellValue(sheetName, 56, 26, 1d),
					new ExpectedCellValue(sheetName, 47, 27, "Chicago Total"),
					new ExpectedCellValue(sheetName, 53, 27, 1d),
					new ExpectedCellValue(sheetName, 56, 27, 1d),
					new ExpectedCellValue(sheetName, 47, 28, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 28, "Car Rack"),
					new ExpectedCellValue(sheetName, 50, 28, 2d),
					new ExpectedCellValue(sheetName, 56, 28, 2d),
					new ExpectedCellValue(sheetName, 47, 29, "Nashville Total"),
					new ExpectedCellValue(sheetName, 50, 29, 2d),
					new ExpectedCellValue(sheetName, 56, 29, 2d),
					new ExpectedCellValue(sheetName, 46, 30, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 47, 30, "Chicago"),
					new ExpectedCellValue(sheetName, 48, 30, "Headlamp"),
					new ExpectedCellValue(sheetName, 53, 30, 24.99),
					new ExpectedCellValue(sheetName, 56, 30, 24.99),
					new ExpectedCellValue(sheetName, 47, 31, "Chicago Total"),
					new ExpectedCellValue(sheetName, 53, 31, 24.99),
					new ExpectedCellValue(sheetName, 56, 31, 24.99),
					new ExpectedCellValue(sheetName, 47, 32, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 32, "Car Rack"),
					new ExpectedCellValue(sheetName, 50, 32, 415.75),
					new ExpectedCellValue(sheetName, 56, 32, 415.75),
					new ExpectedCellValue(sheetName, 47, 33, "Nashville Total"),
					new ExpectedCellValue(sheetName, 50, 33, 415.75),
					new ExpectedCellValue(sheetName, 56, 33, 415.75),
					new ExpectedCellValue(sheetName, 45, 34, "March Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 50, 34, 2d),
					new ExpectedCellValue(sheetName, 53, 34, 1d),
					new ExpectedCellValue(sheetName, 56, 34, 3d),
					new ExpectedCellValue(sheetName, 45, 35, "March Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 50, 35, 415.75),
					new ExpectedCellValue(sheetName, 53, 35, 24.99),
					new ExpectedCellValue(sheetName, 56, 35, 440.74),
					new ExpectedCellValue(sheetName, 45, 36, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 49, 36, 2d),
					new ExpectedCellValue(sheetName, 50, 36, 2d),
					new ExpectedCellValue(sheetName, 51, 36, 6d),
					new ExpectedCellValue(sheetName, 52, 36, 1d),
					new ExpectedCellValue(sheetName, 53, 36, 1d),
					new ExpectedCellValue(sheetName, 54, 36, 1d),
					new ExpectedCellValue(sheetName, 55, 36, 2d),
					new ExpectedCellValue(sheetName, 56, 36, 15d),
					new ExpectedCellValue(sheetName, 45, 37, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 49, 37, 415.75),
					new ExpectedCellValue(sheetName, 50, 37, 415.75),
					new ExpectedCellValue(sheetName, 51, 37, 199d),
					new ExpectedCellValue(sheetName, 52, 37, 415.75),
					new ExpectedCellValue(sheetName, 53, 37, 24.99),
					new ExpectedCellValue(sheetName, 54, 37, 99d),
					new ExpectedCellValue(sheetName, 55, 37, 415.75),
					new ExpectedCellValue(sheetName, 56, 37, 1985.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAsFirstInnerChildSubtotalsOff()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable5"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubTotalFunctions = eSubTotalFunctions.None;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A44:Q56"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(7, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 49, 1, 20100007),
					new ExpectedCellValue(sheetName, 50, 1, 20100017),
					new ExpectedCellValue(sheetName, 51, 1, 20100070),
					new ExpectedCellValue(sheetName, 52, 1, 20100076),
					new ExpectedCellValue(sheetName, 53, 1, 20100083),
					new ExpectedCellValue(sheetName, 54, 1, 20100085),
					new ExpectedCellValue(sheetName, 55, 1, 20100090),
					new ExpectedCellValue(sheetName, 56, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 45, 2, "January"),
					new ExpectedCellValue(sheetName, 46, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 47, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 48, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 49, 2, 2d),
					new ExpectedCellValue(sheetName, 56, 2, 2d),
					new ExpectedCellValue(sheetName, 47, 3, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 55, 3, 2d),
					new ExpectedCellValue(sheetName, 56, 3, 2d),
					new ExpectedCellValue(sheetName, 47, 4, "San Francisco"),
					new ExpectedCellValue(sheetName, 48, 4, "Car Rack"),
					new ExpectedCellValue(sheetName, 52, 4, 1d),
					new ExpectedCellValue(sheetName, 56, 4, 1d),
					new ExpectedCellValue(sheetName, 46, 5, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 47, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 48, 5, "Car Rack"),
					new ExpectedCellValue(sheetName, 49, 5, 415.75),
					new ExpectedCellValue(sheetName, 56, 5, 415.75),
					new ExpectedCellValue(sheetName, 47, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 55, 6, 415.75),
					new ExpectedCellValue(sheetName, 56, 6, 415.75),
					new ExpectedCellValue(sheetName, 47, 7, "San Francisco"),
					new ExpectedCellValue(sheetName, 48, 7, "Car Rack"),
					new ExpectedCellValue(sheetName, 52, 7, 415.75),
					new ExpectedCellValue(sheetName, 56, 7, 415.75),
					new ExpectedCellValue(sheetName, 45, 8, "February"),
					new ExpectedCellValue(sheetName, 46, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 47, 8, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 8, "Tent"),
					new ExpectedCellValue(sheetName, 51, 8, 6d),
					new ExpectedCellValue(sheetName, 56, 8, 6d),
					new ExpectedCellValue(sheetName, 47, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 48, 9, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 54, 9, 1d),
					new ExpectedCellValue(sheetName, 56, 9, 1d),
					new ExpectedCellValue(sheetName, 46, 10, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 47, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 10, "Tent"),
					new ExpectedCellValue(sheetName, 51, 10, 199d),
					new ExpectedCellValue(sheetName, 56, 10, 199d),
					new ExpectedCellValue(sheetName, 47, 11, "San Francisco"),
					new ExpectedCellValue(sheetName, 48, 11, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 54, 11, 99d),
					new ExpectedCellValue(sheetName, 56, 11, 99d),
					new ExpectedCellValue(sheetName, 45, 12, "March"),
					new ExpectedCellValue(sheetName, 46, 12, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 47, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 48, 12, "Headlamp"),
					new ExpectedCellValue(sheetName, 53, 12, 1d),
					new ExpectedCellValue(sheetName, 56, 12, 1d),
					new ExpectedCellValue(sheetName, 47, 13, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 13, "Car Rack"),
					new ExpectedCellValue(sheetName, 50, 13, 2d),
					new ExpectedCellValue(sheetName, 56, 13, 2d),
					new ExpectedCellValue(sheetName, 46, 14, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 47, 14, "Chicago"),
					new ExpectedCellValue(sheetName, 48, 14, "Headlamp"),
					new ExpectedCellValue(sheetName, 53, 14, 24.99),
					new ExpectedCellValue(sheetName, 56, 14, 24.99),
					new ExpectedCellValue(sheetName, 47, 15, "Nashville"),
					new ExpectedCellValue(sheetName, 48, 15, "Car Rack"),
					new ExpectedCellValue(sheetName, 50, 15, 415.75),
					new ExpectedCellValue(sheetName, 56, 15, 415.75),
					new ExpectedCellValue(sheetName, 45, 16, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 49, 16, 2d),
					new ExpectedCellValue(sheetName, 50, 16, 2d),
					new ExpectedCellValue(sheetName, 51, 16, 6d),
					new ExpectedCellValue(sheetName, 52, 16, 1d),
					new ExpectedCellValue(sheetName, 53, 16, 1d),
					new ExpectedCellValue(sheetName, 54, 16, 1d),
					new ExpectedCellValue(sheetName, 55, 16, 2d),
					new ExpectedCellValue(sheetName, 56, 16, 15d),
					new ExpectedCellValue(sheetName, 45, 17, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 49, 17, 415.75),
					new ExpectedCellValue(sheetName, 50, 17, 415.75),
					new ExpectedCellValue(sheetName, 51, 17, 199d),
					new ExpectedCellValue(sheetName, 52, 17, 415.75),
					new ExpectedCellValue(sheetName, 53, 17, 24.99),
					new ExpectedCellValue(sheetName, 54, 17, 99d),
					new ExpectedCellValue(sheetName, 55, 17, 415.75),
					new ExpectedCellValue(sheetName, 56, 17, 1985.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAsLastInnerChildSubtotalsOn()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable6"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A60:AK72"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 65, 1, 20100007),
					new ExpectedCellValue(sheetName, 66, 1, 20100017),
					new ExpectedCellValue(sheetName, 67, 1, 20100070),
					new ExpectedCellValue(sheetName, 68, 1, 20100076),
					new ExpectedCellValue(sheetName, 69, 1, 20100083),
					new ExpectedCellValue(sheetName, 70, 1, 20100085),
					new ExpectedCellValue(sheetName, 71, 1, 20100090),
					new ExpectedCellValue(sheetName, 72, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 61, 2, "January"),
					new ExpectedCellValue(sheetName, 62, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 63, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 65, 2, 2d),
					new ExpectedCellValue(sheetName, 72, 2, 2d),
					new ExpectedCellValue(sheetName, 63, 3, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 65, 3, 415.75),
					new ExpectedCellValue(sheetName, 72, 3, 415.75),
					new ExpectedCellValue(sheetName, 62, 4, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 65, 4, 2d),
					new ExpectedCellValue(sheetName, 72, 4, 2d),
					new ExpectedCellValue(sheetName, 62, 5, "Chicago Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 65, 5, 415.75),
					new ExpectedCellValue(sheetName, 72, 5, 415.75),
					new ExpectedCellValue(sheetName, 62, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 63, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 71, 6, 2d),
					new ExpectedCellValue(sheetName, 72, 6, 2d),
					new ExpectedCellValue(sheetName, 63, 7, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 7, "Car Rack"),
					new ExpectedCellValue(sheetName, 71, 7, 415.75),
					new ExpectedCellValue(sheetName, 72, 7, 415.75),
					new ExpectedCellValue(sheetName, 62, 8, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 71, 8, 2d),
					new ExpectedCellValue(sheetName, 72, 8, 2d),
					new ExpectedCellValue(sheetName, 62, 9, "Nashville Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 71, 9, 415.75),
					new ExpectedCellValue(sheetName, 72, 9, 415.75),
					new ExpectedCellValue(sheetName, 62, 10, "San Francisco"),
					new ExpectedCellValue(sheetName, 63, 10, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 10, "Car Rack"),
					new ExpectedCellValue(sheetName, 68, 10, 1d),
					new ExpectedCellValue(sheetName, 72, 10, 1d),
					new ExpectedCellValue(sheetName, 63, 11, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 11, "Car Rack"),
					new ExpectedCellValue(sheetName, 68, 11, 415.75),
					new ExpectedCellValue(sheetName, 72, 11, 415.75),
					new ExpectedCellValue(sheetName, 62, 12, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 68, 12, 1d),
					new ExpectedCellValue(sheetName, 72, 12, 1d),
					new ExpectedCellValue(sheetName, 62, 13, "San Francisco Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 68, 13, 415.75),
					new ExpectedCellValue(sheetName, 72, 13, 415.75),
					new ExpectedCellValue(sheetName, 61, 14, "January Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 65, 14, 2d),
					new ExpectedCellValue(sheetName, 68, 14, 1d),
					new ExpectedCellValue(sheetName, 71, 14, 2d),
					new ExpectedCellValue(sheetName, 72, 14, 5d),
					new ExpectedCellValue(sheetName, 61, 15, "January Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 65, 15, 415.75),
					new ExpectedCellValue(sheetName, 68, 15, 415.75),
					new ExpectedCellValue(sheetName, 71, 15, 415.75),
					new ExpectedCellValue(sheetName, 72, 15, 1247.25),
					new ExpectedCellValue(sheetName, 61, 16, "February"),
					new ExpectedCellValue(sheetName, 62, 16, "Nashville"),
					new ExpectedCellValue(sheetName, 63, 16, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 16, "Tent"),
					new ExpectedCellValue(sheetName, 67, 16, 6d),
					new ExpectedCellValue(sheetName, 72, 16, 6d),
					new ExpectedCellValue(sheetName, 63, 17, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 17, "Tent"),
					new ExpectedCellValue(sheetName, 67, 17, 199d),
					new ExpectedCellValue(sheetName, 72, 17, 199d),
					new ExpectedCellValue(sheetName, 62, 18, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 67, 18, 6d),
					new ExpectedCellValue(sheetName, 72, 18, 6d),
					new ExpectedCellValue(sheetName, 62, 19, "Nashville Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 67, 19, 199d),
					new ExpectedCellValue(sheetName, 72, 19, 199d),
					new ExpectedCellValue(sheetName, 62, 20, "San Francisco"),
					new ExpectedCellValue(sheetName, 63, 20, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 20, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 70, 20, 1d),
					new ExpectedCellValue(sheetName, 72, 20, 1d),
					new ExpectedCellValue(sheetName, 63, 21, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 21, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 70, 21, 99d),
					new ExpectedCellValue(sheetName, 72, 21, 99d),
					new ExpectedCellValue(sheetName, 62, 22, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 70, 22, 1d),
					new ExpectedCellValue(sheetName, 72, 22, 1d),
					new ExpectedCellValue(sheetName, 62, 23, "San Francisco Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 70, 23, 99d),
					new ExpectedCellValue(sheetName, 72, 23, 99d),
					new ExpectedCellValue(sheetName, 61, 24, "February Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 67, 24, 6d),
					new ExpectedCellValue(sheetName, 70, 24, 1d),
					new ExpectedCellValue(sheetName, 72, 24, 7d),
					new ExpectedCellValue(sheetName, 61, 25, "February Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 67, 25, 199d),
					new ExpectedCellValue(sheetName, 70, 25, 99d),
					new ExpectedCellValue(sheetName, 72, 25, 298d),
					new ExpectedCellValue(sheetName, 61, 26, "March"),
					new ExpectedCellValue(sheetName, 62, 26, "Chicago"),
					new ExpectedCellValue(sheetName, 63, 26, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 26, "Headlamp"),
					new ExpectedCellValue(sheetName, 69, 26, 1d),
					new ExpectedCellValue(sheetName, 72, 26, 1d),
					new ExpectedCellValue(sheetName, 63, 27, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 27, "Headlamp"),
					new ExpectedCellValue(sheetName, 69, 27, 24.99),
					new ExpectedCellValue(sheetName, 72, 27, 24.99),
					new ExpectedCellValue(sheetName, 62, 28, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 69, 28, 1d),
					new ExpectedCellValue(sheetName, 72, 28, 1d),
					new ExpectedCellValue(sheetName, 62, 29, "Chicago Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 69, 29, 24.99),
					new ExpectedCellValue(sheetName, 72, 29, 24.99),
					new ExpectedCellValue(sheetName, 62, 30, "Nashville"),
					new ExpectedCellValue(sheetName, 63, 30, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 30, "Car Rack"),
					new ExpectedCellValue(sheetName, 66, 30, 2d),
					new ExpectedCellValue(sheetName, 72, 30, 2d),
					new ExpectedCellValue(sheetName, 63, 31, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 31, "Car Rack"),
					new ExpectedCellValue(sheetName, 66, 31, 415.75),
					new ExpectedCellValue(sheetName, 72, 31, 415.75),
					new ExpectedCellValue(sheetName, 62, 32, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 66, 32, 2d),
					new ExpectedCellValue(sheetName, 72, 32, 2d),
					new ExpectedCellValue(sheetName, 62, 33, "Nashville Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 66, 33, 415.75),
					new ExpectedCellValue(sheetName, 72, 33, 415.75),
					new ExpectedCellValue(sheetName, 61, 34, "March Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 66, 34, 2d),
					new ExpectedCellValue(sheetName, 69, 34, 1d),
					new ExpectedCellValue(sheetName, 72, 34, 3d),
					new ExpectedCellValue(sheetName, 61, 35, "March Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 66, 35, 415.75),
					new ExpectedCellValue(sheetName, 69, 35, 24.99),
					new ExpectedCellValue(sheetName, 72, 35, 440.74),
					new ExpectedCellValue(sheetName, 61, 36, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 65, 36, 2d),
					new ExpectedCellValue(sheetName, 66, 36, 2d),
					new ExpectedCellValue(sheetName, 67, 36, 6d),
					new ExpectedCellValue(sheetName, 68, 36, 1d),
					new ExpectedCellValue(sheetName, 69, 36, 1d),
					new ExpectedCellValue(sheetName, 70, 36, 1d),
					new ExpectedCellValue(sheetName, 71, 36, 2d),
					new ExpectedCellValue(sheetName, 72, 36, 15d),
					new ExpectedCellValue(sheetName, 61, 37, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 65, 37, 415.75),
					new ExpectedCellValue(sheetName, 66, 37, 415.75),
					new ExpectedCellValue(sheetName, 67, 37, 199d),
					new ExpectedCellValue(sheetName, 68, 37, 415.75),
					new ExpectedCellValue(sheetName, 69, 37, 24.99),
					new ExpectedCellValue(sheetName, 70, 37, 99d),
					new ExpectedCellValue(sheetName, 71, 37, 415.75),
					new ExpectedCellValue(sheetName, 72, 37, 1985.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableWithMultipleColumnDataFields.xlsx")]
		public void PivotTableRefreshMultipleColumnDataFieldsAsLastInnerChildSubtotalsOff()
		{
			var file = new FileInfo("PivotTableWithMultipleColumnDataFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable6"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubTotalFunctions = eSubTotalFunctions.None;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A60:Q72"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(7, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 65, 1, 20100007),
					new ExpectedCellValue(sheetName, 66, 1, 20100017),
					new ExpectedCellValue(sheetName, 67, 1, 20100070),
					new ExpectedCellValue(sheetName, 68, 1, 20100076),
					new ExpectedCellValue(sheetName, 69, 1, 20100083),
					new ExpectedCellValue(sheetName, 70, 1, 20100085),
					new ExpectedCellValue(sheetName, 71, 1, 20100090),
					new ExpectedCellValue(sheetName, 72, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 61, 2, "January"),
					new ExpectedCellValue(sheetName, 62, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 63, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 65, 2, 2d),
					new ExpectedCellValue(sheetName, 72, 2, 2d),
					new ExpectedCellValue(sheetName, 63, 3, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 65, 3, 415.75),
					new ExpectedCellValue(sheetName, 72, 3, 415.75),
					new ExpectedCellValue(sheetName, 62, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 63, 4, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 4, "Car Rack"),
					new ExpectedCellValue(sheetName, 71, 4, 2d),
					new ExpectedCellValue(sheetName, 72, 4, 2d),
					new ExpectedCellValue(sheetName, 63, 5, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 5, "Car Rack"),
					new ExpectedCellValue(sheetName, 71, 5, 415.75),
					new ExpectedCellValue(sheetName, 72, 5, 415.75),
					new ExpectedCellValue(sheetName, 62, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 63, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 68, 6, 1d),
					new ExpectedCellValue(sheetName, 72, 6, 1d),
					new ExpectedCellValue(sheetName, 63, 7, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 7, "Car Rack"),
					new ExpectedCellValue(sheetName, 68, 7, 415.75),
					new ExpectedCellValue(sheetName, 72, 7, 415.75),
					new ExpectedCellValue(sheetName, 61, 8, "February"),
					new ExpectedCellValue(sheetName, 62, 8, "Nashville"),
					new ExpectedCellValue(sheetName, 63, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 8, "Tent"),
					new ExpectedCellValue(sheetName, 67, 8, 6d),
					new ExpectedCellValue(sheetName, 72, 8, 6d),
					new ExpectedCellValue(sheetName, 63, 9, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 9, "Tent"),
					new ExpectedCellValue(sheetName, 67, 9, 199d),
					new ExpectedCellValue(sheetName, 72, 9, 199d),
					new ExpectedCellValue(sheetName, 62, 10, "San Francisco"),
					new ExpectedCellValue(sheetName, 63, 10, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 10, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 70, 10, 1d),
					new ExpectedCellValue(sheetName, 72, 10, 1d),
					new ExpectedCellValue(sheetName, 63, 11, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 11, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 70, 11, 99d),
					new ExpectedCellValue(sheetName, 72, 11, 99d),
					new ExpectedCellValue(sheetName, 61, 12, "March"),
					new ExpectedCellValue(sheetName, 62, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 63, 12, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 12, "Headlamp"),
					new ExpectedCellValue(sheetName, 69, 12, 1d),
					new ExpectedCellValue(sheetName, 72, 12, 1d),
					new ExpectedCellValue(sheetName, 63, 13, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 13, "Headlamp"),
					new ExpectedCellValue(sheetName, 69, 13, 24.99),
					new ExpectedCellValue(sheetName, 72, 13, 24.99),
					new ExpectedCellValue(sheetName, 62, 14, "Nashville"),
					new ExpectedCellValue(sheetName, 63, 14, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 64, 14, "Car Rack"),
					new ExpectedCellValue(sheetName, 66, 14, 2d),
					new ExpectedCellValue(sheetName, 72, 14, 2d),
					new ExpectedCellValue(sheetName, 63, 15, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 15, "Car Rack"),
					new ExpectedCellValue(sheetName, 66, 15, 415.75),
					new ExpectedCellValue(sheetName, 72, 15, 415.75),
					new ExpectedCellValue(sheetName, 61, 16, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 65, 16, 2d),
					new ExpectedCellValue(sheetName, 66, 16, 2d),
					new ExpectedCellValue(sheetName, 67, 16, 6d),
					new ExpectedCellValue(sheetName, 68, 16, 1d),
					new ExpectedCellValue(sheetName, 69, 16, 1d),
					new ExpectedCellValue(sheetName, 70, 16, 1d),
					new ExpectedCellValue(sheetName, 71, 16, 2d),
					new ExpectedCellValue(sheetName, 72, 16, 15d),
					new ExpectedCellValue(sheetName, 61, 17, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 65, 17, 415.75),
					new ExpectedCellValue(sheetName, 66, 17, 415.75),
					new ExpectedCellValue(sheetName, 67, 17, 199d),
					new ExpectedCellValue(sheetName, 68, 17, 415.75),
					new ExpectedCellValue(sheetName, 69, 17, 24.99),
					new ExpectedCellValue(sheetName, 70, 17, 99d),
					new ExpectedCellValue(sheetName, 71, 17, 415.75),
					new ExpectedCellValue(sheetName, 72, 17, 1985.99)
				});
			}
		}
		#endregion

		#region Custom Field Subtotals
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomRowFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleRowFieldsColumnDataFields()
		{
			var file = new FileInfo("PivotTableCustomRowFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				void validateSheet() => TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 2, "Row Labels"),
					new ExpectedCellValue(sheetName, 3, 2, "January"),
					new ExpectedCellValue(sheetName, 4, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 2, "January Sum"),
					new ExpectedCellValue(sheetName, 6, 2, "January Count"),
					new ExpectedCellValue(sheetName, 7, 2, "January Average"),
					new ExpectedCellValue(sheetName, 8, 2, "January Max"),
					new ExpectedCellValue(sheetName, 9, 2, "January Min"),
					new ExpectedCellValue(sheetName, 10, 2, "January Product"),
					new ExpectedCellValue(sheetName, 11, 2, "January Count"),
					new ExpectedCellValue(sheetName, 12, 2, "January StdDev"),
					new ExpectedCellValue(sheetName, 13, 2, "January StdDevp"),
					new ExpectedCellValue(sheetName, 14, 2, "January Var"),
					new ExpectedCellValue(sheetName, 15, 2, "January Varp"),
					new ExpectedCellValue(sheetName, 16, 2, "February"),
					new ExpectedCellValue(sheetName, 17, 2, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 18, 2, "Tent"),
					new ExpectedCellValue(sheetName, 19, 2, "February Sum"),
					new ExpectedCellValue(sheetName, 20, 2, "February Count"),
					new ExpectedCellValue(sheetName, 21, 2, "February Average"),
					new ExpectedCellValue(sheetName, 22, 2, "February Max"),
					new ExpectedCellValue(sheetName, 23, 2, "February Min"),
					new ExpectedCellValue(sheetName, 24, 2, "February Product"),
					new ExpectedCellValue(sheetName, 25, 2, "February Count"),
					new ExpectedCellValue(sheetName, 26, 2, "February StdDev"),
					new ExpectedCellValue(sheetName, 27, 2, "February StdDevp"),
					new ExpectedCellValue(sheetName, 28, 2, "February Var"),
					new ExpectedCellValue(sheetName, 29, 2, "February Varp"),
					new ExpectedCellValue(sheetName, 30, 2, "Grand Total"),

					new ExpectedCellValue(sheetName, 2, 3, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 3, 3, null),
					new ExpectedCellValue(sheetName, 4, 3, 1663),
					new ExpectedCellValue(sheetName, 5, 3, 1663),
					new ExpectedCellValue(sheetName, 6, 3, 4),
					new ExpectedCellValue(sheetName, 7, 3, 415.75),
					new ExpectedCellValue(sheetName, 8, 3, 415.75),
					new ExpectedCellValue(sheetName, 9, 3, 415.75),
					new ExpectedCellValue(sheetName, 10, 3, 29876452710),
					new ExpectedCellValue(sheetName, 11, 3, 4),
					new ExpectedCellValue(sheetName, 12, 3, 0),
					new ExpectedCellValue(sheetName, 13, 3, 0),
					new ExpectedCellValue(sheetName, 14, 3, 0),
					new ExpectedCellValue(sheetName, 15, 3, 0),
					new ExpectedCellValue(sheetName, 16, 3, null),
					new ExpectedCellValue(sheetName, 17, 3, 99),
					new ExpectedCellValue(sheetName, 18, 3, 223.99),
					new ExpectedCellValue(sheetName, 19, 3, 322.99),
					new ExpectedCellValue(sheetName, 20, 3, 3),
					new ExpectedCellValue(sheetName, 21, 3, 107.663333),
					new ExpectedCellValue(sheetName, 22, 3, 199),
					new ExpectedCellValue(sheetName, 23, 3, 24.99),
					new ExpectedCellValue(sheetName, 24, 3, 492327.99),
					new ExpectedCellValue(sheetName, 25, 3, 3),
					new ExpectedCellValue(sheetName, 26, 3, 87.32788806),
					new ExpectedCellValue(sheetName, 27, 3, 71.30292202),
					new ExpectedCellValue(sheetName, 28, 3, 7626.160033),
					new ExpectedCellValue(sheetName, 29, 3, 5084.106689),
					new ExpectedCellValue(sheetName, 30, 3, 1985.99),

					new ExpectedCellValue(sheetName, 2, 4, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 3, 4, null),
					new ExpectedCellValue(sheetName, 4, 4, 7),
					new ExpectedCellValue(sheetName, 5, 4, 7),
					new ExpectedCellValue(sheetName, 6, 4, 4),
					new ExpectedCellValue(sheetName, 7, 4, 1.75),
					new ExpectedCellValue(sheetName, 8, 4, 2),
					new ExpectedCellValue(sheetName, 9, 4, 1),
					new ExpectedCellValue(sheetName, 10, 4, 8),
					new ExpectedCellValue(sheetName, 11, 4, 4),
					new ExpectedCellValue(sheetName, 12, 4, .5),
					new ExpectedCellValue(sheetName, 13, 4, .433012702),
					new ExpectedCellValue(sheetName, 14, 4, .25),
					new ExpectedCellValue(sheetName, 15, 4, .1875),
					new ExpectedCellValue(sheetName, 16, 4, null),
					new ExpectedCellValue(sheetName, 17, 4, 1),
					new ExpectedCellValue(sheetName, 18, 4, 7),
					new ExpectedCellValue(sheetName, 19, 4, 8),
					new ExpectedCellValue(sheetName, 20, 4, 3),
					new ExpectedCellValue(sheetName, 21, 4, 2.666666667),
					new ExpectedCellValue(sheetName, 22, 4, 6),
					new ExpectedCellValue(sheetName, 23, 4, 1),
					new ExpectedCellValue(sheetName, 24, 4, 6),
					new ExpectedCellValue(sheetName, 25, 4, 3),
					new ExpectedCellValue(sheetName, 26, 4, 2.886751346),
					new ExpectedCellValue(sheetName, 27, 4, 2.357022604),
					new ExpectedCellValue(sheetName, 28, 4, 8.333333333),
					new ExpectedCellValue(sheetName, 29, 4, 5.555555556),
					new ExpectedCellValue(sheetName, 30, 4, 15)
				});

				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B2:D30"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(13, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				validateSheet();

				// Test again but with subtotalTop turned on. Results should be identical.
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = true;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B2:D30"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(13, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				validateSheet();
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomRowFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleRowFieldsSingleDataFieldSubtotalsTopOff()
		{
			var file = new FileInfo("PivotTableCustomRowFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("B2:D30"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(13, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 6, "Row Labels"),
					new ExpectedCellValue(sheetName, 3, 6, "January"),
					new ExpectedCellValue(sheetName, 4, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 6, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 8, 6, "Car Rack Max"),
					new ExpectedCellValue(sheetName, 9, 6, "Car Rack Min"),
					new ExpectedCellValue(sheetName, 10, 6, "Car Rack Product"),
					new ExpectedCellValue(sheetName, 11, 6, "January Count"),
					new ExpectedCellValue(sheetName, 12, 6, "February"),
					new ExpectedCellValue(sheetName, 13, 6, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 14, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 15, 6, "Sleeping Bag Max"),
					new ExpectedCellValue(sheetName, 16, 6, "Sleeping Bag Min"),
					new ExpectedCellValue(sheetName, 17, 6, "Sleeping Bag Product"),
					new ExpectedCellValue(sheetName, 18, 6, "Tent"),
					new ExpectedCellValue(sheetName, 19, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 20, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 21, 6, "Tent Max"),
					new ExpectedCellValue(sheetName, 22, 6, "Tent Min"),
					new ExpectedCellValue(sheetName, 23, 6, "Tent Product"),
					new ExpectedCellValue(sheetName, 24, 6, "February Count"),
					new ExpectedCellValue(sheetName, 25, 6, "Grand Total"),

					new ExpectedCellValue(sheetName, 2, 7, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 3, 7, null),
					new ExpectedCellValue(sheetName, 4, 7, null),
					new ExpectedCellValue(sheetName, 5, 7, 415.75),
					new ExpectedCellValue(sheetName, 6, 7, 831.5),
					new ExpectedCellValue(sheetName, 7, 7, 415.75),
					new ExpectedCellValue(sheetName, 8, 7, 415.75),
					new ExpectedCellValue(sheetName, 9, 7, 415.75),
					new ExpectedCellValue(sheetName, 10, 7, 29876452710),
					new ExpectedCellValue(sheetName, 11, 7, 4),
					new ExpectedCellValue(sheetName, 12, 7, null),
					new ExpectedCellValue(sheetName, 13, 7, null),
					new ExpectedCellValue(sheetName, 14, 7, 99),
					new ExpectedCellValue(sheetName, 15, 7, 99),
					new ExpectedCellValue(sheetName, 16, 7, 99),
					new ExpectedCellValue(sheetName, 17, 7, 99),
					new ExpectedCellValue(sheetName, 18, 7, null),
					new ExpectedCellValue(sheetName, 19, 7, 24.99),
					new ExpectedCellValue(sheetName, 20, 7, 199),
					new ExpectedCellValue(sheetName, 21, 7, 199),
					new ExpectedCellValue(sheetName, 22, 7, 24.99),
					new ExpectedCellValue(sheetName, 23, 7, 4973.01),
					new ExpectedCellValue(sheetName, 24, 7, 3),
					new ExpectedCellValue(sheetName, 25, 7, 1985.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomRowFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleRowFieldsSingleDataFieldSubtotalsTopOn()
		{
			var file = new FileInfo("PivotTableCustomRowFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = true;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("F2:G23"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 6, "Row Labels"),
					new ExpectedCellValue(sheetName, 3, 6, "January"),
					new ExpectedCellValue(sheetName, 4, 6, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 6, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 8, 6, "Car Rack Max"),
					new ExpectedCellValue(sheetName, 9, 6, "Car Rack Min"),
					new ExpectedCellValue(sheetName, 10, 6, "Car Rack Product"),
					new ExpectedCellValue(sheetName, 11, 6, "February"),
					new ExpectedCellValue(sheetName, 12, 6, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 13, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 14, 6, "Sleeping Bag Max"),
					new ExpectedCellValue(sheetName, 15, 6, "Sleeping Bag Min"),
					new ExpectedCellValue(sheetName, 16, 6, "Sleeping Bag Product"),
					new ExpectedCellValue(sheetName, 17, 6, "Tent"),
					new ExpectedCellValue(sheetName, 18, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 19, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 20, 6, "Tent Max"),
					new ExpectedCellValue(sheetName, 21, 6, "Tent Min"),
					new ExpectedCellValue(sheetName, 22, 6, "Tent Product"),
					new ExpectedCellValue(sheetName, 23, 6, "Grand Total"),

					new ExpectedCellValue(sheetName, 2, 7, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 3, 7, 4),
					new ExpectedCellValue(sheetName, 4, 7, null),
					new ExpectedCellValue(sheetName, 5, 7, 415.75),
					new ExpectedCellValue(sheetName, 6, 7, 831.5),
					new ExpectedCellValue(sheetName, 7, 7, 415.75),
					new ExpectedCellValue(sheetName, 8, 7, 415.75),
					new ExpectedCellValue(sheetName, 9, 7, 415.75),
					new ExpectedCellValue(sheetName, 10, 7, 29876452710),
					new ExpectedCellValue(sheetName, 11, 7, 3),
					new ExpectedCellValue(sheetName, 12, 7, null),
					new ExpectedCellValue(sheetName, 13, 7, 99),
					new ExpectedCellValue(sheetName, 14, 7, 99),
					new ExpectedCellValue(sheetName, 15, 7, 99),
					new ExpectedCellValue(sheetName, 16, 7, 99),
					new ExpectedCellValue(sheetName, 17, 7, null),
					new ExpectedCellValue(sheetName, 18, 7, 24.99),
					new ExpectedCellValue(sheetName, 19, 7, 199),
					new ExpectedCellValue(sheetName, 20, 7, 199),
					new ExpectedCellValue(sheetName, 21, 7, 24.99),
					new ExpectedCellValue(sheetName, 22, 7, 4973.01),
					new ExpectedCellValue(sheetName, 23, 7, 1985.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomRowFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleRowFieldsRowDataFieldFirst()
		{
			var file = new FileInfo("PivotTableCustomRowFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				void validateWorksheet() => TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 9, "Row Labels"),
					new ExpectedCellValue(sheetName, 3, 9, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 4, 9, "January"),
					new ExpectedCellValue(sheetName, 5, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 6, 9, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 8, 9, "January Count"),
					new ExpectedCellValue(sheetName, 9, 9, "January Average"),
					new ExpectedCellValue(sheetName, 10, 9, "February"),
					new ExpectedCellValue(sheetName, 11, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 12, 9, "Nashville"),
					new ExpectedCellValue(sheetName, 13, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 14, 9, "February Count"),
					new ExpectedCellValue(sheetName, 15, 9, "February Average"),
					new ExpectedCellValue(sheetName, 16, 9, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 17, 9, "January"),
					new ExpectedCellValue(sheetName, 18, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 19, 9, "Nashville"),
					new ExpectedCellValue(sheetName, 20, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 21, 9, "January Count"),
					new ExpectedCellValue(sheetName, 22, 9, "January Average"),
					new ExpectedCellValue(sheetName, 23, 9, "February"),
					new ExpectedCellValue(sheetName, 24, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 25, 9, "Nashville"),
					new ExpectedCellValue(sheetName, 26, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 27, 9, "February Count"),
					new ExpectedCellValue(sheetName, 28, 9, "February Average"),
					new ExpectedCellValue(sheetName, 29, 9, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 30, 9, "Total Sum of Units Sold"),

					new ExpectedCellValue(sheetName, 2, 10, null),
					new ExpectedCellValue(sheetName, 3, 10, null),
					new ExpectedCellValue(sheetName, 4, 10, null),
					new ExpectedCellValue(sheetName, 5, 10, 415.75),
					new ExpectedCellValue(sheetName, 6, 10, 831.5),
					new ExpectedCellValue(sheetName, 7, 10, 415.75),
					new ExpectedCellValue(sheetName, 8, 10, 4),
					new ExpectedCellValue(sheetName, 9, 10, 415.75),
					new ExpectedCellValue(sheetName, 10, 10, null),
					new ExpectedCellValue(sheetName, 11, 10, 24.99),
					new ExpectedCellValue(sheetName, 12, 10, 199),
					new ExpectedCellValue(sheetName, 13, 10, 99),
					new ExpectedCellValue(sheetName, 14, 10, 3),
					new ExpectedCellValue(sheetName, 15, 10, 107.6633333),
					new ExpectedCellValue(sheetName, 16, 10, null),
					new ExpectedCellValue(sheetName, 17, 10, null),
					new ExpectedCellValue(sheetName, 18, 10, 2),
					new ExpectedCellValue(sheetName, 19, 10, 4),
					new ExpectedCellValue(sheetName, 20, 10, 1),
					new ExpectedCellValue(sheetName, 21, 10, 4),
					new ExpectedCellValue(sheetName, 22, 10, 1.75),
					new ExpectedCellValue(sheetName, 23, 10, null),
					new ExpectedCellValue(sheetName, 24, 10, 1),
					new ExpectedCellValue(sheetName, 25, 10, 6),
					new ExpectedCellValue(sheetName, 26, 10, 1),
					new ExpectedCellValue(sheetName, 27, 10, 3),
					new ExpectedCellValue(sheetName, 28, 10, 2.666666667),
					new ExpectedCellValue(sheetName, 29, 10, 1985.99),
					new ExpectedCellValue(sheetName, 30, 10, 15),
				});

				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("I2:J30"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}
				validateWorksheet();

				// Run again with subtotalTop = true, results should be the same.
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = true;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("I2:J30"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}
				validateWorksheet();
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomRowFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleRowFieldsRowDataFieldSecond()
		{
			var file = new FileInfo("PivotTableCustomRowFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				void validateWorksheet() => TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 12, "Row Labels"),
					new ExpectedCellValue(sheetName, 3, 12, "January"),
					new ExpectedCellValue(sheetName, 4, 12, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 5, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 6, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 8, 12, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 9, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 10, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 11, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 12, 12, "January Max of Wholesale Price"),
					new ExpectedCellValue(sheetName, 13, 12, "January Max of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 12, "January Min of Wholesale Price"),
					new ExpectedCellValue(sheetName, 15, 12, "January Min of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 12, "February"),
					new ExpectedCellValue(sheetName, 17, 12, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 18, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 19, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 20, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 21, 12, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 23, 12, "Nashville"),
					new ExpectedCellValue(sheetName, 24, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 25, 12, "February Max of Wholesale Price"),
					new ExpectedCellValue(sheetName, 26, 12, "February Max of Units Sold"),
					new ExpectedCellValue(sheetName, 27, 12, "February Min of Wholesale Price"),
					new ExpectedCellValue(sheetName, 28, 12, "February Min of Units Sold"),
					new ExpectedCellValue(sheetName, 29, 12, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 30, 12, "Total Sum of Units Sold"),

					new ExpectedCellValue(sheetName, 2, 13, null),
					new ExpectedCellValue(sheetName, 3, 13, null),
					new ExpectedCellValue(sheetName, 4, 13, null),
					new ExpectedCellValue(sheetName, 5, 13, 415.75),
					new ExpectedCellValue(sheetName, 6, 13, 831.5),
					new ExpectedCellValue(sheetName, 7, 13, 415.75),
					new ExpectedCellValue(sheetName, 8, 13, null),
					new ExpectedCellValue(sheetName, 9, 13, 2),
					new ExpectedCellValue(sheetName, 10, 13, 4),
					new ExpectedCellValue(sheetName, 11, 13, 1),
					new ExpectedCellValue(sheetName, 12, 13, 415.75),
					new ExpectedCellValue(sheetName, 13, 13, 2),
					new ExpectedCellValue(sheetName, 14, 13, 415.75),
					new ExpectedCellValue(sheetName, 15, 13, 1),
					new ExpectedCellValue(sheetName, 16, 13, null),
					new ExpectedCellValue(sheetName, 17, 13, null),
					new ExpectedCellValue(sheetName, 18, 13, 24.99),
					new ExpectedCellValue(sheetName, 19, 13, 199),
					new ExpectedCellValue(sheetName, 20, 13, 99),
					new ExpectedCellValue(sheetName, 21, 13, null),
					new ExpectedCellValue(sheetName, 22, 13, 1),
					new ExpectedCellValue(sheetName, 23, 13, 6),
					new ExpectedCellValue(sheetName, 24, 13, 1),
					new ExpectedCellValue(sheetName, 25, 13, 199),
					new ExpectedCellValue(sheetName, 26, 13, 6),
					new ExpectedCellValue(sheetName, 27, 13, 24.99),
					new ExpectedCellValue(sheetName, 28, 13, 1),
					new ExpectedCellValue(sheetName, 29, 13, 1985.99),
					new ExpectedCellValue(sheetName, 30, 13, 15),
				});

				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("L2:M30"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}
				validateWorksheet();

				// Run again with subtotalTop = true, results should be the same.
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = true;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("L2:M30"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}
				validateWorksheet();
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomRowFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleRowFieldsRowDataFieldLast()
		{
			var file = new FileInfo("PivotTableCustomRowFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				void validateWorksheet() => TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 15, "Row Labels"),
					new ExpectedCellValue(sheetName, 3, 15, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 15, "January"),
					new ExpectedCellValue(sheetName, 5, 15, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 6, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 7, 15, "February"),
					new ExpectedCellValue(sheetName, 8, 15, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 9, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 10, 15, "Chicago Average of Wholesale Price"),
					new ExpectedCellValue(sheetName, 11, 15, "Chicago Average of Units Sold"),
					new ExpectedCellValue(sheetName, 12, 15, "Chicago Max of Wholesale Price"),
					new ExpectedCellValue(sheetName, 13, 15, "Chicago Max of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 15, "Nashville"),
					new ExpectedCellValue(sheetName, 15, 15, "January"),
					new ExpectedCellValue(sheetName, 16, 15, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 17, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 18, 15, "February"),
					new ExpectedCellValue(sheetName, 19, 15, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 20, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 21, 15, "Nashville Average of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 15, "Nashville Average of Units Sold"),
					new ExpectedCellValue(sheetName, 23, 15, "Nashville Max of Wholesale Price"),
					new ExpectedCellValue(sheetName, 24, 15, "Nashville Max of Units Sold"),
					new ExpectedCellValue(sheetName, 25, 15, "San Francisco"),
					new ExpectedCellValue(sheetName, 26, 15, "January"),
					new ExpectedCellValue(sheetName, 27, 15, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 28, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 29, 15, "February"),
					new ExpectedCellValue(sheetName, 30, 15, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 31, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 32, 15, "San Francisco Average of Wholesale Price"),
					new ExpectedCellValue(sheetName, 33, 15, "San Francisco Average of Units Sold"),
					new ExpectedCellValue(sheetName, 34, 15, "San Francisco Max of Wholesale Price"),
					new ExpectedCellValue(sheetName, 35, 15, "San Francisco Max of Units Sold"),
					new ExpectedCellValue(sheetName, 36, 15, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 37, 15, "Total Sum of Units Sold"),

					new ExpectedCellValue(sheetName, 2, 16, null),
					new ExpectedCellValue(sheetName, 3, 16, null),
					new ExpectedCellValue(sheetName, 4, 16, null),
					new ExpectedCellValue(sheetName, 5, 16, 415.75),
					new ExpectedCellValue(sheetName, 6, 16, 2),
					new ExpectedCellValue(sheetName, 7, 16, null),
					new ExpectedCellValue(sheetName, 8, 16, 24.99),
					new ExpectedCellValue(sheetName, 9, 16, 1),
					new ExpectedCellValue(sheetName, 10, 16, 220.37),
					new ExpectedCellValue(sheetName, 11, 16, 1.5),
					new ExpectedCellValue(sheetName, 12, 16, 415.75),
					new ExpectedCellValue(sheetName, 13, 16, 2),
					new ExpectedCellValue(sheetName, 14, 16, null),
					new ExpectedCellValue(sheetName, 15, 16, null),
					new ExpectedCellValue(sheetName, 16, 16, 831.5),
					new ExpectedCellValue(sheetName, 17, 16, 4),
					new ExpectedCellValue(sheetName, 18, 16, null),
					new ExpectedCellValue(sheetName, 19, 16, 199),
					new ExpectedCellValue(sheetName, 20, 16, 6),
					new ExpectedCellValue(sheetName, 21, 16, 343.5),
					new ExpectedCellValue(sheetName, 22, 16, 3.33333333),
					new ExpectedCellValue(sheetName, 23, 16, 415.75),
					new ExpectedCellValue(sheetName, 24, 16, 6),
					new ExpectedCellValue(sheetName, 25, 16, null),
					new ExpectedCellValue(sheetName, 26, 16, null),
					new ExpectedCellValue(sheetName, 27, 16, 415.75),
					new ExpectedCellValue(sheetName, 28, 16, 1),
					new ExpectedCellValue(sheetName, 29, 16, null),
					new ExpectedCellValue(sheetName, 30, 16, 99),
					new ExpectedCellValue(sheetName, 31, 16, 1),
					new ExpectedCellValue(sheetName, 32, 16, 257.375),
					new ExpectedCellValue(sheetName, 33, 16, 1),
					new ExpectedCellValue(sheetName, 34, 16, 415.75),
					new ExpectedCellValue(sheetName, 35, 16, 1),
					new ExpectedCellValue(sheetName, 36, 16, 1985.99),
					new ExpectedCellValue(sheetName, 37, 16, 15),
				});

				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable5"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("O2:P37"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}
				validateWorksheet();

				// Run again with subtotalTop = true, results should be the same.
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable5"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = true;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("O2:P37"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}
				validateWorksheet();
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomColumnFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleColumnFieldsRowDataFields()
		{
			var file = new FileInfo("PivotTableCustomColumnFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("F3:U7"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}

				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 6, null),
					new ExpectedCellValue(sheetName, 4, 7, "January"),
					new ExpectedCellValue(sheetName, 4, 8, null),
					new ExpectedCellValue(sheetName, 4, 9, null),
					new ExpectedCellValue(sheetName, 4, 10, "January Sum"),
					new ExpectedCellValue(sheetName, 4, 11, "January Count"),
					new ExpectedCellValue(sheetName, 4, 12, "January Average"),
					new ExpectedCellValue(sheetName, 4, 13, "January Max"),
					new ExpectedCellValue(sheetName, 4, 14, "February"),
					new ExpectedCellValue(sheetName, 4, 15, null),
					new ExpectedCellValue(sheetName, 4, 16, null),
					new ExpectedCellValue(sheetName, 4, 17, "February Sum"),
					new ExpectedCellValue(sheetName, 4, 18, "February Count"),
					new ExpectedCellValue(sheetName, 4, 19, "February Average"),
					new ExpectedCellValue(sheetName, 4, 20, "February Max"),
					new ExpectedCellValue(sheetName, 4, 21, "Grand Total"),

					new ExpectedCellValue(sheetName, 5, 6, "Values"),
					new ExpectedCellValue(sheetName, 5, 7, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 8, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 5, 10, null),
					new ExpectedCellValue(sheetName, 5, 11, null),
					new ExpectedCellValue(sheetName, 5, 12, null),
					new ExpectedCellValue(sheetName, 5, 13, null),
					new ExpectedCellValue(sheetName, 5, 14, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 15, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 16, "San Francisco"),
					new ExpectedCellValue(sheetName, 5, 17, null),
					new ExpectedCellValue(sheetName, 5, 18, null),
					new ExpectedCellValue(sheetName, 5, 19, null),
					new ExpectedCellValue(sheetName, 5, 20, null),
					new ExpectedCellValue(sheetName, 5, 21, null),

					new ExpectedCellValue(sheetName, 6, 6, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 6, 7, 415.75),
					new ExpectedCellValue(sheetName, 6, 8, 831.5),
					new ExpectedCellValue(sheetName, 6, 9, 415.75),
					new ExpectedCellValue(sheetName, 6, 10, 1663),
					new ExpectedCellValue(sheetName, 6, 11, 4),
					new ExpectedCellValue(sheetName, 6, 12, 415.75),
					new ExpectedCellValue(sheetName, 6, 13, 415.75),
					new ExpectedCellValue(sheetName, 6, 14, 24.99),
					new ExpectedCellValue(sheetName, 6, 15, 199),
					new ExpectedCellValue(sheetName, 6, 16, 99),
					new ExpectedCellValue(sheetName, 6, 17, 322.99),
					new ExpectedCellValue(sheetName, 6, 18, 3),
					new ExpectedCellValue(sheetName, 6, 19, 107.6633333),
					new ExpectedCellValue(sheetName, 6, 20, 199),
					new ExpectedCellValue(sheetName, 6, 21, 1985.99),

					new ExpectedCellValue(sheetName, 7, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 7, 7, 2),
					new ExpectedCellValue(sheetName, 7, 8, 4),
					new ExpectedCellValue(sheetName, 7, 9, 1),
					new ExpectedCellValue(sheetName, 7, 10, 7),
					new ExpectedCellValue(sheetName, 7, 11, 4),
					new ExpectedCellValue(sheetName, 7, 12, 1.75),
					new ExpectedCellValue(sheetName, 7, 13, 2),
					new ExpectedCellValue(sheetName, 7, 14, 1),
					new ExpectedCellValue(sheetName, 7, 15, 6),
					new ExpectedCellValue(sheetName, 7, 16, 1),
					new ExpectedCellValue(sheetName, 7, 17, 8),
					new ExpectedCellValue(sheetName, 7, 18, 3),
					new ExpectedCellValue(sheetName, 7, 19, 2.666666667),
					new ExpectedCellValue(sheetName, 7, 20, 6),
					new ExpectedCellValue(sheetName, 7, 21, 15),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomColumnFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleColumnFieldsSingleDataField()
		{
			var file = new FileInfo("PivotTableCustomColumnFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("F10:Q13"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}

				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 10, 6, null),
					new ExpectedCellValue(sheetName, 10, 7, "Column Labels"),
					new ExpectedCellValue(sheetName, 10, 8, null),
					new ExpectedCellValue(sheetName, 10, 11, null),
					new ExpectedCellValue(sheetName, 10, 13, null),
					new ExpectedCellValue(sheetName, 10, 17, null),

					new ExpectedCellValue(sheetName, 11, 6, null),
					new ExpectedCellValue(sheetName, 11, 7, "January"),
					new ExpectedCellValue(sheetName, 11, 8, null),
					new ExpectedCellValue(sheetName, 11, 9, null),
					new ExpectedCellValue(sheetName, 11, 10, "January Max"),
					new ExpectedCellValue(sheetName, 11, 11, "January Min"),
					new ExpectedCellValue(sheetName, 11, 12, "February"),
					new ExpectedCellValue(sheetName, 11, 13, null),
					new ExpectedCellValue(sheetName, 11, 14, null),
					new ExpectedCellValue(sheetName, 11, 15, "February Max"),
					new ExpectedCellValue(sheetName, 11, 16, "February Min"),
					new ExpectedCellValue(sheetName, 11, 17, "Grand Total"),

					new ExpectedCellValue(sheetName, 12, 6, null),
					new ExpectedCellValue(sheetName, 12, 7, "Chicago"),
					new ExpectedCellValue(sheetName, 12, 8, "Nashville"),
					new ExpectedCellValue(sheetName, 12, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 12, 10, null),
					new ExpectedCellValue(sheetName, 12, 11, null),
					new ExpectedCellValue(sheetName, 12, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 12, 13, "Nashville"),
					new ExpectedCellValue(sheetName, 12, 14, "San Francisco"),
					new ExpectedCellValue(sheetName, 12, 15, null),
					new ExpectedCellValue(sheetName, 12, 16, null),
					new ExpectedCellValue(sheetName, 12, 17, null),

					new ExpectedCellValue(sheetName, 13, 6, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 13, 7, 415.75),
					new ExpectedCellValue(sheetName, 13, 8, 831.5),
					new ExpectedCellValue(sheetName, 13, 9, 415.75),
					new ExpectedCellValue(sheetName, 13, 10, 415.75),
					new ExpectedCellValue(sheetName, 13, 11, 415.75),
					new ExpectedCellValue(sheetName, 13, 12, 24.99),
					new ExpectedCellValue(sheetName, 13, 13, 199),
					new ExpectedCellValue(sheetName, 13, 14, 99),
					new ExpectedCellValue(sheetName, 13, 15, 199),
					new ExpectedCellValue(sheetName, 13, 16, 24.99),
					new ExpectedCellValue(sheetName, 13, 17, 1985.99),
				});
			}
		}


		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomColumnFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleColumnAndRowFieldsWithRowDataFields()
		{
			var file = new FileInfo("PivotTableCustomColumnFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("F16:Q42"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}

				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 16, 6, null),
					new ExpectedCellValue(sheetName, 16, 7, "Column Labels"),
					new ExpectedCellValue(sheetName, 16, 8, null),

					new ExpectedCellValue(sheetName, 17, 6, null),
					new ExpectedCellValue(sheetName, 17, 7, "Car Rack"),
					new ExpectedCellValue(sheetName, 17, 8, null),
					new ExpectedCellValue(sheetName, 17, 9, null),
					new ExpectedCellValue(sheetName, 17, 10, null),
					new ExpectedCellValue(sheetName, 17, 11, "Car Rack Product"),
					new ExpectedCellValue(sheetName, 17, 12, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 17, 13, "Sleeping Bag Product"),
					new ExpectedCellValue(sheetName, 17, 14, "Tent"),
					new ExpectedCellValue(sheetName, 17, 15, null),
					new ExpectedCellValue(sheetName, 17, 16, "Tent Product"),
					new ExpectedCellValue(sheetName, 17, 17, "Grand Total"),

					new ExpectedCellValue(sheetName, 18, 6, "Row Labels"),
					new ExpectedCellValue(sheetName, 18, 7, "20100007"),
					new ExpectedCellValue(sheetName, 18, 8, "20100017"),
					new ExpectedCellValue(sheetName, 18, 9, "20100076"),
					new ExpectedCellValue(sheetName, 18, 10, "20100090"),
					new ExpectedCellValue(sheetName, 18, 11, null),
					new ExpectedCellValue(sheetName, 18, 12, "20100085"),
					new ExpectedCellValue(sheetName, 18, 13, null),
					new ExpectedCellValue(sheetName, 18, 14, "20100070"),
					new ExpectedCellValue(sheetName, 18, 15, "20100083"),
					new ExpectedCellValue(sheetName, 18, 16, null),
					new ExpectedCellValue(sheetName, 18, 17, null),

					new ExpectedCellValue(sheetName, 19, 6, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 19, 8, null),
					new ExpectedCellValue(sheetName, 19, 10, null),
					new ExpectedCellValue(sheetName, 19, 12, null),
					new ExpectedCellValue(sheetName, 19, 14, null),
					new ExpectedCellValue(sheetName, 19, 16, null),
					new ExpectedCellValue(sheetName, 19, 17, null),

					new ExpectedCellValue(sheetName, 20, 6, "January"),
					new ExpectedCellValue(sheetName, 20, 10, null),
					new ExpectedCellValue(sheetName, 20, 12, null),
					new ExpectedCellValue(sheetName, 20, 14, null),
					new ExpectedCellValue(sheetName, 20, 16, null),
					new ExpectedCellValue(sheetName, 20, 17, null),

					new ExpectedCellValue(sheetName, 21, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 21, 7, 415.75),
					new ExpectedCellValue(sheetName, 21, 10, null),
					new ExpectedCellValue(sheetName, 21, 11, 415.75),
					new ExpectedCellValue(sheetName, 21, 12, null),
					new ExpectedCellValue(sheetName, 21, 14, null),
					new ExpectedCellValue(sheetName, 21, 16, null),
					new ExpectedCellValue(sheetName, 21, 17, 415.75),

					new ExpectedCellValue(sheetName, 22, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 22, 7, null),
					new ExpectedCellValue(sheetName, 22, 8, 415.75),
					new ExpectedCellValue(sheetName, 22, 9, null),
					new ExpectedCellValue(sheetName, 22, 10, 415.75),
					new ExpectedCellValue(sheetName, 22, 11, 172848.0625),
					new ExpectedCellValue(sheetName, 22, 12, null),
					new ExpectedCellValue(sheetName, 22, 13, null),
					new ExpectedCellValue(sheetName, 22, 14, null),
					new ExpectedCellValue(sheetName, 22, 16, null),
					new ExpectedCellValue(sheetName, 22, 17, 831.5),

					new ExpectedCellValue(sheetName, 23, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 23, 7, null),
					new ExpectedCellValue(sheetName, 23, 8, null),
					new ExpectedCellValue(sheetName, 23, 9, 415.75),
					new ExpectedCellValue(sheetName, 23, 10, null),
					new ExpectedCellValue(sheetName, 23, 11, 415.75),
					new ExpectedCellValue(sheetName, 23, 12, null),
					new ExpectedCellValue(sheetName, 23, 13, null),
					new ExpectedCellValue(sheetName, 23, 14, null),
					new ExpectedCellValue(sheetName, 23, 16, null),
					new ExpectedCellValue(sheetName, 23, 17, 415.75),

					new ExpectedCellValue(sheetName, 24, 6, "January Count"),
					new ExpectedCellValue(sheetName, 24, 7, 1),
					new ExpectedCellValue(sheetName, 24, 8, 1),
					new ExpectedCellValue(sheetName, 24, 9, 1),
					new ExpectedCellValue(sheetName, 24, 10, 1),
					new ExpectedCellValue(sheetName, 24, 11, null),
					new ExpectedCellValue(sheetName, 24, 12, null),
					new ExpectedCellValue(sheetName, 24, 13, null),
					new ExpectedCellValue(sheetName, 24, 14, null),
					new ExpectedCellValue(sheetName, 24, 16, null),
					new ExpectedCellValue(sheetName, 24, 17, 4),

					new ExpectedCellValue(sheetName, 25, 6, "February"),
					new ExpectedCellValue(sheetName, 25, 7, null),
					new ExpectedCellValue(sheetName, 25, 8, null),
					new ExpectedCellValue(sheetName, 25, 9, null),
					new ExpectedCellValue(sheetName, 25, 10, null),
					new ExpectedCellValue(sheetName, 25, 11, null),
					new ExpectedCellValue(sheetName, 25, 12, null),
					new ExpectedCellValue(sheetName, 25, 13, null),
					new ExpectedCellValue(sheetName, 25, 14, null),
					new ExpectedCellValue(sheetName, 25, 16, null),
					new ExpectedCellValue(sheetName, 25, 17, null),

					new ExpectedCellValue(sheetName, 26, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 26, 7, null),
					new ExpectedCellValue(sheetName, 26, 8, null),
					new ExpectedCellValue(sheetName, 26, 9, null),
					new ExpectedCellValue(sheetName, 26, 10, null),
					new ExpectedCellValue(sheetName, 26, 11, null),
					new ExpectedCellValue(sheetName, 26, 12, null),
					new ExpectedCellValue(sheetName, 26, 13, null),
					new ExpectedCellValue(sheetName, 26, 15, 24.99),
					new ExpectedCellValue(sheetName, 26, 16, 24.99),
					new ExpectedCellValue(sheetName, 26, 17, 24.99),

					new ExpectedCellValue(sheetName, 27, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 27, 7, null),
					new ExpectedCellValue(sheetName, 27, 9, null),
					new ExpectedCellValue(sheetName, 27, 11, null),
					new ExpectedCellValue(sheetName, 27, 13, null),
					new ExpectedCellValue(sheetName, 27, 14, 199),
					new ExpectedCellValue(sheetName, 27, 15, null),
					new ExpectedCellValue(sheetName, 27, 16, 199),
					new ExpectedCellValue(sheetName, 27, 17, 199),

					new ExpectedCellValue(sheetName, 28, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 28, 7, null),
					new ExpectedCellValue(sheetName, 28, 8, null),
					new ExpectedCellValue(sheetName, 28, 9, null),
					new ExpectedCellValue(sheetName, 28, 10, null),
					new ExpectedCellValue(sheetName, 28, 11, null),
					new ExpectedCellValue(sheetName, 28, 12, 99),
					new ExpectedCellValue(sheetName, 28, 13, 99),
					new ExpectedCellValue(sheetName, 28, 14, null),
					new ExpectedCellValue(sheetName, 28, 16, null),
					new ExpectedCellValue(sheetName, 28, 17, 99),

					new ExpectedCellValue(sheetName, 29, 6, "February Count"),
					new ExpectedCellValue(sheetName, 29, 7, null),
					new ExpectedCellValue(sheetName, 29, 9, null),
					new ExpectedCellValue(sheetName, 29, 11, null),
					new ExpectedCellValue(sheetName, 29, 12, 1),
					new ExpectedCellValue(sheetName, 29, 13, null),
					new ExpectedCellValue(sheetName, 29, 14, 1),
					new ExpectedCellValue(sheetName, 29, 15, 1),
					new ExpectedCellValue(sheetName, 29, 16, null),
					new ExpectedCellValue(sheetName, 29, 17, 3),

					new ExpectedCellValue(sheetName, 30, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 30, 7, null),
					new ExpectedCellValue(sheetName, 30, 9, null),
					new ExpectedCellValue(sheetName, 30, 11, null),
					new ExpectedCellValue(sheetName, 30, 13, null),
					new ExpectedCellValue(sheetName, 30, 15, null),
					new ExpectedCellValue(sheetName, 30, 17, null),

					new ExpectedCellValue(sheetName, 31, 6, "January"),
					new ExpectedCellValue(sheetName, 31, 7, null),
					new ExpectedCellValue(sheetName, 31, 9, null),
					new ExpectedCellValue(sheetName, 31, 11, null),
					new ExpectedCellValue(sheetName, 31, 13, null),
					new ExpectedCellValue(sheetName, 31, 15, null),
					new ExpectedCellValue(sheetName, 31, 17, null),

					new ExpectedCellValue(sheetName, 32, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 32, 7, 2),
					new ExpectedCellValue(sheetName, 32, 9, null),
					new ExpectedCellValue(sheetName, 32, 11, 2),
					new ExpectedCellValue(sheetName, 32, 13, null),
					new ExpectedCellValue(sheetName, 32, 15, null),
					new ExpectedCellValue(sheetName, 32, 17, 2),

					new ExpectedCellValue(sheetName, 33, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 33, 7, null),
					new ExpectedCellValue(sheetName, 33, 8, 2),
					new ExpectedCellValue(sheetName, 33, 9, null),
					new ExpectedCellValue(sheetName, 33, 10, 2),
					new ExpectedCellValue(sheetName, 33, 11, 4),
					new ExpectedCellValue(sheetName, 33, 13, null),
					new ExpectedCellValue(sheetName, 33, 15, null),
					new ExpectedCellValue(sheetName, 33, 16, null),
					new ExpectedCellValue(sheetName, 33, 17, 4),

					new ExpectedCellValue(sheetName, 34, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 34, 7, null),
					new ExpectedCellValue(sheetName, 34, 9, 1),
					new ExpectedCellValue(sheetName, 34, 10, null),
					new ExpectedCellValue(sheetName, 34, 11, 1),
					new ExpectedCellValue(sheetName, 34, 12, null),
					new ExpectedCellValue(sheetName, 34, 14, null),
					new ExpectedCellValue(sheetName, 34, 16, null),
					new ExpectedCellValue(sheetName, 34, 17, 1),

					new ExpectedCellValue(sheetName, 35, 6, "January Count"),
					new ExpectedCellValue(sheetName, 35, 7, 1),
					new ExpectedCellValue(sheetName, 35, 8, 1),
					new ExpectedCellValue(sheetName, 35, 9, 1),
					new ExpectedCellValue(sheetName, 35, 10, 1),
					new ExpectedCellValue(sheetName, 35, 11, null),
					new ExpectedCellValue(sheetName, 35, 12, null),
					new ExpectedCellValue(sheetName, 35, 14, null),
					new ExpectedCellValue(sheetName, 35, 16, null),
					new ExpectedCellValue(sheetName, 35, 17, 4),

					new ExpectedCellValue(sheetName, 36, 6, "February"),
					new ExpectedCellValue(sheetName, 36, 8, null),
					new ExpectedCellValue(sheetName, 36, 10, null),
					new ExpectedCellValue(sheetName, 36, 12, null),
					new ExpectedCellValue(sheetName, 36, 14, null),
					new ExpectedCellValue(sheetName, 36, 16, null),
					new ExpectedCellValue(sheetName, 36, 17, null),

					new ExpectedCellValue(sheetName, 37, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 37, 7, null),
					new ExpectedCellValue(sheetName, 37, 9, null),
					new ExpectedCellValue(sheetName, 37, 11, null),
					new ExpectedCellValue(sheetName, 37, 13, null),
					new ExpectedCellValue(sheetName, 37, 15, 1),
					new ExpectedCellValue(sheetName, 37, 16, 1),
					new ExpectedCellValue(sheetName, 37, 17, 1),

					new ExpectedCellValue(sheetName, 38, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 38, 7, null),
					new ExpectedCellValue(sheetName, 38, 9, null),
					new ExpectedCellValue(sheetName, 38, 11, null),
					new ExpectedCellValue(sheetName, 38, 13, null),
					new ExpectedCellValue(sheetName, 38, 14, 6),
					new ExpectedCellValue(sheetName, 38, 16, 6),
					new ExpectedCellValue(sheetName, 38, 17, 6),

					new ExpectedCellValue(sheetName, 39, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 39, 7, null),
					new ExpectedCellValue(sheetName, 39, 9, null),
					new ExpectedCellValue(sheetName, 39, 11, null),
					new ExpectedCellValue(sheetName, 39, 12, 1),
					new ExpectedCellValue(sheetName, 39, 13, 1),
					new ExpectedCellValue(sheetName, 39, 15, null),
					new ExpectedCellValue(sheetName, 39, 17, 1),

					new ExpectedCellValue(sheetName, 40, 6, "February Count"),
					new ExpectedCellValue(sheetName, 40, 7, null),
					new ExpectedCellValue(sheetName, 40, 9, null),
					new ExpectedCellValue(sheetName, 40, 11, null),
					new ExpectedCellValue(sheetName, 40, 12, 1),
					new ExpectedCellValue(sheetName, 40, 13, null),
					new ExpectedCellValue(sheetName, 40, 14, 1),
					new ExpectedCellValue(sheetName, 40, 15, 1),
					new ExpectedCellValue(sheetName, 40, 16, null),
					new ExpectedCellValue(sheetName, 40, 17, 3),

					new ExpectedCellValue(sheetName, 41, 6, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 41, 7, 415.75),
					new ExpectedCellValue(sheetName, 41, 8, 415.75),
					new ExpectedCellValue(sheetName, 41, 9, 415.75),
					new ExpectedCellValue(sheetName, 41, 10, 415.75),
					new ExpectedCellValue(sheetName, 41, 11, 29876452710),
					new ExpectedCellValue(sheetName, 41, 12, 99),
					new ExpectedCellValue(sheetName, 41, 13, 99),
					new ExpectedCellValue(sheetName, 41, 14, 199),
					new ExpectedCellValue(sheetName, 41, 15, 24.99),
					new ExpectedCellValue(sheetName, 41, 16, 4973.01),
					new ExpectedCellValue(sheetName, 41, 17, 1985.99),

					new ExpectedCellValue(sheetName, 42, 6, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 42, 7, 2),
					new ExpectedCellValue(sheetName, 42, 8, 2),
					new ExpectedCellValue(sheetName, 42, 9, 1),
					new ExpectedCellValue(sheetName, 42, 10, 2),
					new ExpectedCellValue(sheetName, 42, 11, 8),
					new ExpectedCellValue(sheetName, 42, 12, 1),
					new ExpectedCellValue(sheetName, 42, 13, 1),
					new ExpectedCellValue(sheetName, 42, 14, 6),
					new ExpectedCellValue(sheetName, 42, 15, 1),
					new ExpectedCellValue(sheetName, 42, 16, 6),
					new ExpectedCellValue(sheetName, 42, 17, 15),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableCustomColumnFieldSubtotals.xlsx")]
		public void PivotTableRefreshCustomSubtotalsMultipleColumnAndRowFieldsWithRowDataFieldsSubtotalTopOn()
		{
			var file = new FileInfo("PivotTableCustomColumnFieldSubtotals.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				string sheetName = "PivotTables";
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets[sheetName];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = true;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					package.SaveAs(@"C:\Users\ems\Downloads\OUT.xlsx");
					this.CheckPivotTableAddress(new ExcelAddress("F16:Q38"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					package.SaveAs(newFile.File);
				}

				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 16, 6, null),
					new ExpectedCellValue(sheetName, 16, 7, "Column Labels"),
					new ExpectedCellValue(sheetName, 16, 8, null),

					new ExpectedCellValue(sheetName, 17, 6, null),
					new ExpectedCellValue(sheetName, 17, 7, "Car Rack"),
					new ExpectedCellValue(sheetName, 17, 8, null),
					new ExpectedCellValue(sheetName, 17, 9, null),
					new ExpectedCellValue(sheetName, 17, 10, null),
					new ExpectedCellValue(sheetName, 17, 11, "Car Rack Product"),
					new ExpectedCellValue(sheetName, 17, 12, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 17, 13, "Sleeping Bag Product"),
					new ExpectedCellValue(sheetName, 17, 14, "Tent"),
					new ExpectedCellValue(sheetName, 17, 15, null),
					new ExpectedCellValue(sheetName, 17, 16, "Tent Product"),
					new ExpectedCellValue(sheetName, 17, 17, "Grand Total"),

					new ExpectedCellValue(sheetName, 18, 6, "Row Labels"),
					new ExpectedCellValue(sheetName, 18, 7, "20100007"),
					new ExpectedCellValue(sheetName, 18, 8, "20100017"),
					new ExpectedCellValue(sheetName, 18, 9, "20100076"),
					new ExpectedCellValue(sheetName, 18, 10, "20100090"),
					new ExpectedCellValue(sheetName, 18, 11, null),
					new ExpectedCellValue(sheetName, 18, 12, "20100085"),
					new ExpectedCellValue(sheetName, 18, 13, null),
					new ExpectedCellValue(sheetName, 18, 14, "20100070"),
					new ExpectedCellValue(sheetName, 18, 15, "20100083"),
					new ExpectedCellValue(sheetName, 18, 16, null),
					new ExpectedCellValue(sheetName, 18, 17, null),

					new ExpectedCellValue(sheetName, 19, 6, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 19, 8, null),
					new ExpectedCellValue(sheetName, 19, 10, null),
					new ExpectedCellValue(sheetName, 19, 12, null),
					new ExpectedCellValue(sheetName, 19, 14, null),
					new ExpectedCellValue(sheetName, 19, 16, null),
					new ExpectedCellValue(sheetName, 19, 17, null),

					new ExpectedCellValue(sheetName, 20, 6, "January"),
					new ExpectedCellValue(sheetName, 20, 7, 1),
					new ExpectedCellValue(sheetName, 20, 8, 1),
					new ExpectedCellValue(sheetName, 20, 9, 1),
					new ExpectedCellValue(sheetName, 20, 10, 1),
					new ExpectedCellValue(sheetName, 20, 11, null),
					new ExpectedCellValue(sheetName, 20, 12, null),
					new ExpectedCellValue(sheetName, 20, 14, null),
					new ExpectedCellValue(sheetName, 20, 16, null),
					new ExpectedCellValue(sheetName, 20, 17, 4),

					new ExpectedCellValue(sheetName, 21, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 21, 7, 415.75),
					new ExpectedCellValue(sheetName, 21, 10, null),
					new ExpectedCellValue(sheetName, 21, 11, 415.75),
					new ExpectedCellValue(sheetName, 21, 12, null),
					new ExpectedCellValue(sheetName, 21, 14, null),
					new ExpectedCellValue(sheetName, 21, 16, null),
					new ExpectedCellValue(sheetName, 21, 17, 415.75),

					new ExpectedCellValue(sheetName, 22, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 22, 7, null),
					new ExpectedCellValue(sheetName, 22, 8, 415.75),
					new ExpectedCellValue(sheetName, 22, 9, null),
					new ExpectedCellValue(sheetName, 22, 10, 415.75),
					new ExpectedCellValue(sheetName, 22, 11, 172848.0625),
					new ExpectedCellValue(sheetName, 22, 12, null),
					new ExpectedCellValue(sheetName, 22, 13, null),
					new ExpectedCellValue(sheetName, 22, 14, null),
					new ExpectedCellValue(sheetName, 22, 16, null),
					new ExpectedCellValue(sheetName, 22, 17, 831.5),

					new ExpectedCellValue(sheetName, 23, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 23, 7, null),
					new ExpectedCellValue(sheetName, 23, 8, null),
					new ExpectedCellValue(sheetName, 23, 9, 415.75),
					new ExpectedCellValue(sheetName, 23, 10, null),
					new ExpectedCellValue(sheetName, 23, 11, 415.75),
					new ExpectedCellValue(sheetName, 23, 12, null),
					new ExpectedCellValue(sheetName, 23, 13, null),
					new ExpectedCellValue(sheetName, 23, 14, null),
					new ExpectedCellValue(sheetName, 23, 16, null),
					new ExpectedCellValue(sheetName, 23, 17, 415.75),

					new ExpectedCellValue(sheetName, 24, 6, "February"),
					new ExpectedCellValue(sheetName, 24, 7, null),
					new ExpectedCellValue(sheetName, 24, 8, null),
					new ExpectedCellValue(sheetName, 24, 9, null),
					new ExpectedCellValue(sheetName, 24, 10, null),
					new ExpectedCellValue(sheetName, 24, 11, null),
					new ExpectedCellValue(sheetName, 24, 12, 1),
					new ExpectedCellValue(sheetName, 24, 13, null),
					new ExpectedCellValue(sheetName, 24, 14, 1),
					new ExpectedCellValue(sheetName, 24, 15, 1),
					new ExpectedCellValue(sheetName, 24, 16, null),
					new ExpectedCellValue(sheetName, 24, 17, 3),

					new ExpectedCellValue(sheetName, 25, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 25, 7, null),
					new ExpectedCellValue(sheetName, 25, 8, null),
					new ExpectedCellValue(sheetName, 25, 9, null),
					new ExpectedCellValue(sheetName, 25, 10, null),
					new ExpectedCellValue(sheetName, 25, 11, null),
					new ExpectedCellValue(sheetName, 25, 12, null),
					new ExpectedCellValue(sheetName, 25, 13, null),
					new ExpectedCellValue(sheetName, 25, 15, 24.99),
					new ExpectedCellValue(sheetName, 25, 16, 24.99),
					new ExpectedCellValue(sheetName, 25, 17, 24.99),

					new ExpectedCellValue(sheetName, 26, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 26, 7, null),
					new ExpectedCellValue(sheetName, 26, 9, null),
					new ExpectedCellValue(sheetName, 26, 11, null),
					new ExpectedCellValue(sheetName, 26, 13, null),
					new ExpectedCellValue(sheetName, 26, 14, 199),
					new ExpectedCellValue(sheetName, 26, 15, null),
					new ExpectedCellValue(sheetName, 26, 16, 199),
					new ExpectedCellValue(sheetName, 26, 17, 199),

					new ExpectedCellValue(sheetName, 27, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 27, 7, null),
					new ExpectedCellValue(sheetName, 27, 8, null),
					new ExpectedCellValue(sheetName, 27, 9, null),
					new ExpectedCellValue(sheetName, 27, 10, null),
					new ExpectedCellValue(sheetName, 27, 11, null),
					new ExpectedCellValue(sheetName, 27, 12, 99),
					new ExpectedCellValue(sheetName, 27, 13, 99),
					new ExpectedCellValue(sheetName, 27, 14, null),
					new ExpectedCellValue(sheetName, 27, 16, null),
					new ExpectedCellValue(sheetName, 27, 17, 99),

					new ExpectedCellValue(sheetName, 28, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 28, 7, null),
					new ExpectedCellValue(sheetName, 28, 9, null),
					new ExpectedCellValue(sheetName, 28, 11, null),
					new ExpectedCellValue(sheetName, 28, 13, null),
					new ExpectedCellValue(sheetName, 28, 15, null),
					new ExpectedCellValue(sheetName, 28, 17, null),

					new ExpectedCellValue(sheetName, 29, 6, "January"),
					new ExpectedCellValue(sheetName, 29, 7, 1),
					new ExpectedCellValue(sheetName, 29, 8, 1),
					new ExpectedCellValue(sheetName, 29, 9, 1),
					new ExpectedCellValue(sheetName, 29, 10, 1),
					new ExpectedCellValue(sheetName, 29, 11, null),
					new ExpectedCellValue(sheetName, 29, 13, null),
					new ExpectedCellValue(sheetName, 29, 15, null),
					new ExpectedCellValue(sheetName, 29, 17, 4),

					new ExpectedCellValue(sheetName, 30, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 30, 7, 2),
					new ExpectedCellValue(sheetName, 30, 9, null),
					new ExpectedCellValue(sheetName, 30, 11, 2),
					new ExpectedCellValue(sheetName, 30, 13, null),
					new ExpectedCellValue(sheetName, 30, 15, null),
					new ExpectedCellValue(sheetName, 30, 17, 2),

					new ExpectedCellValue(sheetName, 31, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 31, 7, null),
					new ExpectedCellValue(sheetName, 31, 8, 2),
					new ExpectedCellValue(sheetName, 31, 9, null),
					new ExpectedCellValue(sheetName, 31, 10, 2),
					new ExpectedCellValue(sheetName, 31, 11, 4),
					new ExpectedCellValue(sheetName, 31, 13, null),
					new ExpectedCellValue(sheetName, 31, 15, null),
					new ExpectedCellValue(sheetName, 31, 16, null),
					new ExpectedCellValue(sheetName, 31, 17, 4),

					new ExpectedCellValue(sheetName, 32, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 32, 7, null),
					new ExpectedCellValue(sheetName, 32, 9, 1),
					new ExpectedCellValue(sheetName, 32, 10, null),
					new ExpectedCellValue(sheetName, 32, 11, 1),
					new ExpectedCellValue(sheetName, 32, 12, null),
					new ExpectedCellValue(sheetName, 32, 14, null),
					new ExpectedCellValue(sheetName, 32, 16, null),
					new ExpectedCellValue(sheetName, 32, 17, 1),

					new ExpectedCellValue(sheetName, 33, 6, "February"),
					new ExpectedCellValue(sheetName, 33, 8, null),
					new ExpectedCellValue(sheetName, 33, 10, null),
					new ExpectedCellValue(sheetName, 33, 12, 1),
					new ExpectedCellValue(sheetName, 33, 13, null),
					new ExpectedCellValue(sheetName, 33, 14, 1),
					new ExpectedCellValue(sheetName, 33, 15, 1),
					new ExpectedCellValue(sheetName, 33, 16, null),
					new ExpectedCellValue(sheetName, 33, 17, 3),

					new ExpectedCellValue(sheetName, 34, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 34, 7, null),
					new ExpectedCellValue(sheetName, 34, 9, null),
					new ExpectedCellValue(sheetName, 34, 11, null),
					new ExpectedCellValue(sheetName, 34, 13, null),
					new ExpectedCellValue(sheetName, 34, 15, 1),
					new ExpectedCellValue(sheetName, 34, 16, 1),
					new ExpectedCellValue(sheetName, 34, 17, 1),

					new ExpectedCellValue(sheetName, 35, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 35, 7, null),
					new ExpectedCellValue(sheetName, 35, 9, null),
					new ExpectedCellValue(sheetName, 35, 11, null),
					new ExpectedCellValue(sheetName, 35, 13, null),
					new ExpectedCellValue(sheetName, 35, 14, 6),
					new ExpectedCellValue(sheetName, 35, 16, 6),
					new ExpectedCellValue(sheetName, 35, 17, 6),

					new ExpectedCellValue(sheetName, 36, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 36, 7, null),
					new ExpectedCellValue(sheetName, 36, 9, null),
					new ExpectedCellValue(sheetName, 36, 11, null),
					new ExpectedCellValue(sheetName, 36, 12, 1),
					new ExpectedCellValue(sheetName, 36, 13, 1),
					new ExpectedCellValue(sheetName, 36, 15, null),
					new ExpectedCellValue(sheetName, 36, 17, 1),

					new ExpectedCellValue(sheetName, 37, 6, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 37, 7, 415.75),
					new ExpectedCellValue(sheetName, 37, 8, 415.75),
					new ExpectedCellValue(sheetName, 37, 9, 415.75),
					new ExpectedCellValue(sheetName, 37, 10, 415.75),
					new ExpectedCellValue(sheetName, 37, 11, 29876452710),
					new ExpectedCellValue(sheetName, 37, 12, 99),
					new ExpectedCellValue(sheetName, 37, 13, 99),
					new ExpectedCellValue(sheetName, 37, 14, 199),
					new ExpectedCellValue(sheetName, 37, 15, 24.99),
					new ExpectedCellValue(sheetName, 37, 16, 4973.01),
					new ExpectedCellValue(sheetName, 37, 17, 1985.99),

					new ExpectedCellValue(sheetName, 38, 6, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 38, 7, 2),
					new ExpectedCellValue(sheetName, 38, 8, 2),
					new ExpectedCellValue(sheetName, 38, 9, 1),
					new ExpectedCellValue(sheetName, 38, 10, 2),
					new ExpectedCellValue(sheetName, 38, 11, 8),
					new ExpectedCellValue(sheetName, 38, 12, 1),
					new ExpectedCellValue(sheetName, 38, 13, 1),
					new ExpectedCellValue(sheetName, 38, 14, 6),
					new ExpectedCellValue(sheetName, 38, 15, 1),
					new ExpectedCellValue(sheetName, 38, 16, 6),
					new ExpectedCellValue(sheetName, 38, 17, 15),
				});
			}
		}
		#endregion
		#endregion

		#region Sorting Tests
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingRowFieldsWithNoReferencesToDataFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["SortingWithoutReferences"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:C12"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "SortingWithoutReferences";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 3, 1, 20100085),
					new ExpectedCellValue(sheetName, 4, 1, 20100076),
					new ExpectedCellValue(sheetName, 5, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 6, 1, 20100090),
					new ExpectedCellValue(sheetName, 7, 1, 20100070),
					new ExpectedCellValue(sheetName, 8, 1, 20100017),
					new ExpectedCellValue(sheetName, 9, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 10, 1, 20100083),
					new ExpectedCellValue(sheetName, 11, 1, 20100007),
					new ExpectedCellValue(sheetName, 12, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 2, 2, 514.75),
					new ExpectedCellValue(sheetName, 3, 2, 99d),
					new ExpectedCellValue(sheetName, 4, 2, 415.75),
					new ExpectedCellValue(sheetName, 5, 2, 1030.5),
					new ExpectedCellValue(sheetName, 6, 2, 415.75),
					new ExpectedCellValue(sheetName, 7, 2, 199d),
					new ExpectedCellValue(sheetName, 8, 2, 415.75),
					new ExpectedCellValue(sheetName, 9, 2, 440.74),
					new ExpectedCellValue(sheetName, 10, 2, 24.99),
					new ExpectedCellValue(sheetName, 11, 2, 415.75),
					new ExpectedCellValue(sheetName, 12, 2, 1985.99),
					new ExpectedCellValue(sheetName, 1, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 2, 3, 2d),
					new ExpectedCellValue(sheetName, 3, 3, 1d),
					new ExpectedCellValue(sheetName, 4, 3, 1d),
					new ExpectedCellValue(sheetName, 5, 3, 10d),
					new ExpectedCellValue(sheetName, 6, 3, 2d),
					new ExpectedCellValue(sheetName, 7, 3, 6d),
					new ExpectedCellValue(sheetName, 8, 3, 2d),
					new ExpectedCellValue(sheetName, 9, 3, 3d),
					new ExpectedCellValue(sheetName, 10, 3, 1d),
					new ExpectedCellValue(sheetName, 11, 3, 2d),
					new ExpectedCellValue(sheetName, 12, 3, 15d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingColumnFieldsWithNoReferencesToDataFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["SortingWithoutReferences"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A16:W23"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "SortingWithoutReferences";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 20, 1, "January"),
					new ExpectedCellValue(sheetName, 21, 1, "February"),
					new ExpectedCellValue(sheetName, 22, 1, "March"),
					new ExpectedCellValue(sheetName, 23, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 17, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 18, 2, 20100085),
					new ExpectedCellValue(sheetName, 19, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 21, 2, 99d),
					new ExpectedCellValue(sheetName, 23, 2, 99d),
					new ExpectedCellValue(sheetName, 19, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 21, 3, 1d),
					new ExpectedCellValue(sheetName, 23, 3, 1d),
					new ExpectedCellValue(sheetName, 18, 4, 20100076),
					new ExpectedCellValue(sheetName, 19, 4, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 20, 4, 415.75),
					new ExpectedCellValue(sheetName, 23, 4, 415.75),
					new ExpectedCellValue(sheetName, 19, 5, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 5, 1d),
					new ExpectedCellValue(sheetName, 23, 5, 1d),
					new ExpectedCellValue(sheetName, 17, 6, "San Francisco Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 20, 6, 415.75),
					new ExpectedCellValue(sheetName, 21, 6, 99d),
					new ExpectedCellValue(sheetName, 23, 6, 514.75),
					new ExpectedCellValue(sheetName, 17, 7, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 7, 1d),
					new ExpectedCellValue(sheetName, 21, 7, 1d),
					new ExpectedCellValue(sheetName, 23, 7, 2d),
					new ExpectedCellValue(sheetName, 17, 8, "Nashville"),
					new ExpectedCellValue(sheetName, 18, 8, 20100090),
					new ExpectedCellValue(sheetName, 19, 8, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 20, 8, 415.75),
					new ExpectedCellValue(sheetName, 23, 8, 415.75),
					new ExpectedCellValue(sheetName, 19, 9, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 9, 2d),
					new ExpectedCellValue(sheetName, 23, 9, 2d),
					new ExpectedCellValue(sheetName, 18, 10, 20100070),
					new ExpectedCellValue(sheetName, 19, 10, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 21, 10, 199d),
					new ExpectedCellValue(sheetName, 23, 10, 199d),
					new ExpectedCellValue(sheetName, 19, 11, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 21, 11, 6d),
					new ExpectedCellValue(sheetName, 23, 11, 6d),
					new ExpectedCellValue(sheetName, 18, 12, 20100017),
					new ExpectedCellValue(sheetName, 19, 12, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 12, 415.75),
					new ExpectedCellValue(sheetName, 23, 12, 415.75),
					new ExpectedCellValue(sheetName, 19, 13, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 13, 2d),
					new ExpectedCellValue(sheetName, 23, 13, 2d),
					new ExpectedCellValue(sheetName, 17, 14, "Nashville Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 20, 14, 415.75),
					new ExpectedCellValue(sheetName, 21, 14, 199d),
					new ExpectedCellValue(sheetName, 22, 14, 415.75),
					new ExpectedCellValue(sheetName, 23, 14, 1030.5),
					new ExpectedCellValue(sheetName, 17, 15, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 15, 2d),
					new ExpectedCellValue(sheetName, 21, 15, 6d),
					new ExpectedCellValue(sheetName, 22, 15, 2d),
					new ExpectedCellValue(sheetName, 23, 15, 10d),
					new ExpectedCellValue(sheetName, 17, 16, "Chicago"),
					new ExpectedCellValue(sheetName, 18, 16, 20100083),
					new ExpectedCellValue(sheetName, 19, 16, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 16, 24.99),
					new ExpectedCellValue(sheetName, 23, 16, 24.99),
					new ExpectedCellValue(sheetName, 19, 17, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 17, 1d),
					new ExpectedCellValue(sheetName, 23, 17, 1d),
					new ExpectedCellValue(sheetName, 18, 18, 20100007),
					new ExpectedCellValue(sheetName, 19, 18, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 20, 18, 415.75),
					new ExpectedCellValue(sheetName, 23, 18, 415.75),
					new ExpectedCellValue(sheetName, 19, 19, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 19, 2d),
					new ExpectedCellValue(sheetName, 23, 19, 2d),
					new ExpectedCellValue(sheetName, 17, 20, "Chicago Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 20, 20, 415.75),
					new ExpectedCellValue(sheetName, 22, 20, 24.99),
					new ExpectedCellValue(sheetName, 23, 20, 440.74),
					new ExpectedCellValue(sheetName, 17, 21, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 21, 2d),
					new ExpectedCellValue(sheetName, 22, 21, 1d),
					new ExpectedCellValue(sheetName, 23, 21, 3d),
					new ExpectedCellValue(sheetName, 17, 22, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 20, 22, 1247.25),
					new ExpectedCellValue(sheetName, 21, 22, 298d),
					new ExpectedCellValue(sheetName, 22, 22, 440.74),
					new ExpectedCellValue(sheetName, 23, 22, 1985.99),
					new ExpectedCellValue(sheetName, 17, 23, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 23, 5d),
					new ExpectedCellValue(sheetName, 21, 23, 7d),
					new ExpectedCellValue(sheetName, 22, 23, 3d),
					new ExpectedCellValue(sheetName, 23, 23, 15d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingRowFieldsAndColumnFieldsWithNoReferencesToDataFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["SortingWithoutReferences"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A27:I40"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "SortingWithoutReferences";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 30, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 31, 1, 20100085),
					new ExpectedCellValue(sheetName, 32, 1, 20100076),
					new ExpectedCellValue(sheetName, 33, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 34, 1, 20100090),
					new ExpectedCellValue(sheetName, 35, 1, 20100070),
					new ExpectedCellValue(sheetName, 36, 1, 20100017),
					new ExpectedCellValue(sheetName, 37, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 38, 1, 20100083),
					new ExpectedCellValue(sheetName, 39, 1, 20100007),
					new ExpectedCellValue(sheetName, 40, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 28, 2, "March"),
					new ExpectedCellValue(sheetName, 29, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 2, 2d),
					new ExpectedCellValue(sheetName, 36, 2, 2d),
					new ExpectedCellValue(sheetName, 37, 2, 1d),
					new ExpectedCellValue(sheetName, 38, 2, 1d),
					new ExpectedCellValue(sheetName, 40, 2, 3d),
					new ExpectedCellValue(sheetName, 29, 3, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 33, 3, 415.75),
					new ExpectedCellValue(sheetName, 36, 3, 415.75),
					new ExpectedCellValue(sheetName, 37, 3, 24.99),
					new ExpectedCellValue(sheetName, 38, 3, 24.99),
					new ExpectedCellValue(sheetName, 40, 3, 440.74),
					new ExpectedCellValue(sheetName, 28, 4, "February"),
					new ExpectedCellValue(sheetName, 29, 4, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 30, 4, 1d),
					new ExpectedCellValue(sheetName, 31, 4, 1d),
					new ExpectedCellValue(sheetName, 33, 4, 6d),
					new ExpectedCellValue(sheetName, 35, 4, 6d),
					new ExpectedCellValue(sheetName, 40, 4, 7d),
					new ExpectedCellValue(sheetName, 29, 5, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 30, 5, 99d),
					new ExpectedCellValue(sheetName, 31, 5, 99d),
					new ExpectedCellValue(sheetName, 33, 5, 199d),
					new ExpectedCellValue(sheetName, 35, 5, 199d),
					new ExpectedCellValue(sheetName, 40, 5, 298d),
					new ExpectedCellValue(sheetName, 28, 6, "January"),
					new ExpectedCellValue(sheetName, 29, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 30, 6, 1d),
					new ExpectedCellValue(sheetName, 32, 6, 1d),
					new ExpectedCellValue(sheetName, 33, 6, 2d),
					new ExpectedCellValue(sheetName, 34, 6, 2d),
					new ExpectedCellValue(sheetName, 37, 6, 2d),
					new ExpectedCellValue(sheetName, 39, 6, 2d),
					new ExpectedCellValue(sheetName, 40, 6, 5d),
					new ExpectedCellValue(sheetName, 29, 7, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 30, 7, 415.75),
					new ExpectedCellValue(sheetName, 32, 7, 415.75),
					new ExpectedCellValue(sheetName, 33, 7, 415.75),
					new ExpectedCellValue(sheetName, 34, 7, 415.75),
					new ExpectedCellValue(sheetName, 37, 7, 415.75),
					new ExpectedCellValue(sheetName, 39, 7, 415.75),
					new ExpectedCellValue(sheetName, 40, 7, 1247.25),
					new ExpectedCellValue(sheetName, 28, 8, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 30, 8, 2d),
					new ExpectedCellValue(sheetName, 31, 8, 1d),
					new ExpectedCellValue(sheetName, 32, 8, 1d),
					new ExpectedCellValue(sheetName, 33, 8, 10d),
					new ExpectedCellValue(sheetName, 34, 8, 2d),
					new ExpectedCellValue(sheetName, 35, 8, 6d),
					new ExpectedCellValue(sheetName, 36, 8, 2d),
					new ExpectedCellValue(sheetName, 37, 8, 3d),
					new ExpectedCellValue(sheetName, 38, 8, 1d),
					new ExpectedCellValue(sheetName, 39, 8, 2d),
					new ExpectedCellValue(sheetName, 40, 8, 15d),
					new ExpectedCellValue(sheetName, 28, 9, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 30, 9, 514.75),
					new ExpectedCellValue(sheetName, 31, 9, 99d),
					new ExpectedCellValue(sheetName, 32, 9, 415.75),
					new ExpectedCellValue(sheetName, 33, 9, 1030.5),
					new ExpectedCellValue(sheetName, 34, 9, 415.75),
					new ExpectedCellValue(sheetName, 35, 9, 199d),
					new ExpectedCellValue(sheetName, 36, 9, 415.75),
					new ExpectedCellValue(sheetName, 37, 9, 440.74),
					new ExpectedCellValue(sheetName, 38, 9, 24.99),
					new ExpectedCellValue(sheetName, 39, 9, 415.75),
					new ExpectedCellValue(sheetName, 40, 9, 1985.99)

				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsOneRowFieldAndNoColumnFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["SortingWithReferencesRowFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:C5"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "SortingWithReferencesRowFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 3, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 5, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 2, 2, 1030.5),
					new ExpectedCellValue(sheetName, 3, 2, 514.75),
					new ExpectedCellValue(sheetName, 4, 2, 440.74),
					new ExpectedCellValue(sheetName, 5, 2, 1985.99),
					new ExpectedCellValue(sheetName, 1, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 2, 3, 10d),
					new ExpectedCellValue(sheetName, 3, 3, 2d),
					new ExpectedCellValue(sheetName, 4, 3, 3d),
					new ExpectedCellValue(sheetName, 5, 3, 15d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsTwoRowFieldsAndNoColumnFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["SortingWithReferencesRowFields"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A9:C19"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "SortingWithReferencesRowFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 10, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 11, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 12, 1, "Tent"),
					new ExpectedCellValue(sheetName, 13, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 14, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 15, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 16, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 17, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 18, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 19, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 9, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 10, 2, 1030.5),
					new ExpectedCellValue(sheetName, 11, 2, 831.5),
					new ExpectedCellValue(sheetName, 12, 2, 199d),
					new ExpectedCellValue(sheetName, 13, 2, 514.75),
					new ExpectedCellValue(sheetName, 14, 2, 415.75),
					new ExpectedCellValue(sheetName, 15, 2, 99d),
					new ExpectedCellValue(sheetName, 16, 2, 440.74),
					new ExpectedCellValue(sheetName, 17, 2, 24.99),
					new ExpectedCellValue(sheetName, 18, 2, 415.75),
					new ExpectedCellValue(sheetName, 19, 2, 1985.99),
					new ExpectedCellValue(sheetName, 9, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 10, 3, 10d),
					new ExpectedCellValue(sheetName, 11, 3, 4d),
					new ExpectedCellValue(sheetName, 12, 3, 6d),
					new ExpectedCellValue(sheetName, 13, 3, 2d),
					new ExpectedCellValue(sheetName, 14, 3, 1d),
					new ExpectedCellValue(sheetName, 15, 3, 1d),
					new ExpectedCellValue(sheetName, 16, 3, 3d),
					new ExpectedCellValue(sheetName, 17, 3, 1d),
					new ExpectedCellValue(sheetName, 18, 3, 2d),
					new ExpectedCellValue(sheetName, 19, 3, 15d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsTwoRowFieldsAndNoColumnFieldsCorrectSortWithSameValuesDescending()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["SortingWithReferencesRowFields"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A23:C34"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "SortingWithReferencesRowFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 24, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 25, 1, "February"),
					new ExpectedCellValue(sheetName, 26, 1, "March"),
					new ExpectedCellValue(sheetName, 27, 1, "January"),
					new ExpectedCellValue(sheetName, 28, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 29, 1, "February"),
					new ExpectedCellValue(sheetName, 30, 1, "January"),
					new ExpectedCellValue(sheetName, 31, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 32, 1, "January"),
					new ExpectedCellValue(sheetName, 33, 1, "March"),
					new ExpectedCellValue(sheetName, 34, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 23, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 24, 2, 1030.5),
					new ExpectedCellValue(sheetName, 25, 2, 199d),
					new ExpectedCellValue(sheetName, 26, 2, 415.75),
					new ExpectedCellValue(sheetName, 27, 2, 415.75),
					new ExpectedCellValue(sheetName, 28, 2, 514.75),
					new ExpectedCellValue(sheetName, 29, 2, 99d),
					new ExpectedCellValue(sheetName, 30, 2, 415.75),
					new ExpectedCellValue(sheetName, 31, 2, 440.74),
					new ExpectedCellValue(sheetName, 32, 2, 415.75),
					new ExpectedCellValue(sheetName, 33, 2, 24.99),
					new ExpectedCellValue(sheetName, 34, 2, 1985.99),
					new ExpectedCellValue(sheetName, 23, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 24, 3, 10d),
					new ExpectedCellValue(sheetName, 25, 3, 6d),
					new ExpectedCellValue(sheetName, 26, 3, 2d),
					new ExpectedCellValue(sheetName, 27, 3, 2d),
					new ExpectedCellValue(sheetName, 28, 3, 2d),
					new ExpectedCellValue(sheetName, 29, 3, 1d),
					new ExpectedCellValue(sheetName, 30, 3, 1d),
					new ExpectedCellValue(sheetName, 31, 3, 3d),
					new ExpectedCellValue(sheetName, 32, 3, 2d),
					new ExpectedCellValue(sheetName, 33, 3, 1d),
					new ExpectedCellValue(sheetName, 34, 3, 15d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsTwoRowFieldsAndNoColumnFieldsCorrectSortWithSameValuesAscending()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["SortingWithReferencesRowFields"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A38:C49"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "SortingWithReferencesRowFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 39, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 40, 1, "February"),
					new ExpectedCellValue(sheetName, 41, 1, "January"),
					new ExpectedCellValue(sheetName, 42, 1, "March"),
					new ExpectedCellValue(sheetName, 43, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 44, 1, "February"),
					new ExpectedCellValue(sheetName, 45, 1, "January"),
					new ExpectedCellValue(sheetName, 46, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 47, 1, "March"),
					new ExpectedCellValue(sheetName, 48, 1, "January"),
					new ExpectedCellValue(sheetName, 49, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 38, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 39, 2, 1030.5),
					new ExpectedCellValue(sheetName, 40, 2, 199d),
					new ExpectedCellValue(sheetName, 41, 2, 415.75),
					new ExpectedCellValue(sheetName, 42, 2, 415.75),
					new ExpectedCellValue(sheetName, 43, 2, 514.75),
					new ExpectedCellValue(sheetName, 44, 2, 99d),
					new ExpectedCellValue(sheetName, 45, 2, 415.75),
					new ExpectedCellValue(sheetName, 46, 2, 440.74),
					new ExpectedCellValue(sheetName, 47, 2, 24.99),
					new ExpectedCellValue(sheetName, 48, 2, 415.75),
					new ExpectedCellValue(sheetName, 49, 2, 1985.99),
					new ExpectedCellValue(sheetName, 38, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 39, 3, 10d),
					new ExpectedCellValue(sheetName, 40, 3, 6d),
					new ExpectedCellValue(sheetName, 41, 3, 2d),
					new ExpectedCellValue(sheetName, 42, 3, 2d),
					new ExpectedCellValue(sheetName, 43, 3, 2d),
					new ExpectedCellValue(sheetName, 44, 3, 1d),
					new ExpectedCellValue(sheetName, 45, 3, 1d),
					new ExpectedCellValue(sheetName, 46, 3, 3d),
					new ExpectedCellValue(sheetName, 47, 3, 1d),
					new ExpectedCellValue(sheetName, 48, 3, 2d),
					new ExpectedCellValue(sheetName, 49, 3, 15d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsTwoRowFieldsAndOneColumnFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowColumnFieldsSorting"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:K14"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowColumnFieldsSorting";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 1, "February"),
					new ExpectedCellValue(sheetName, 6, 1, "January"),
					new ExpectedCellValue(sheetName, 7, 1, "March"),
					new ExpectedCellValue(sheetName, 8, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 9, 1, "February"),
					new ExpectedCellValue(sheetName, 10, 1, "January"),
					new ExpectedCellValue(sheetName, 11, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 12, 1, "March"),
					new ExpectedCellValue(sheetName, 13, 1, "January"),
					new ExpectedCellValue(sheetName, 14, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 3, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 4, 2, 831.5),
					new ExpectedCellValue(sheetName, 6, 2, 415.75),
					new ExpectedCellValue(sheetName, 7, 2, 415.75),
					new ExpectedCellValue(sheetName, 8, 2, 415.75),
					new ExpectedCellValue(sheetName, 10, 2, 415.75),
					new ExpectedCellValue(sheetName, 11, 2, 415.75),
					new ExpectedCellValue(sheetName, 13, 2, 415.75),
					new ExpectedCellValue(sheetName, 14, 2, 1663d),
					new ExpectedCellValue(sheetName, 3, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 4, 3, 4d),
					new ExpectedCellValue(sheetName, 6, 3, 2d),
					new ExpectedCellValue(sheetName, 7, 3, 2d),
					new ExpectedCellValue(sheetName, 8, 3, 1d),
					new ExpectedCellValue(sheetName, 10, 3, 1d),
					new ExpectedCellValue(sheetName, 11, 3, 2d),
					new ExpectedCellValue(sheetName, 13, 3, 2d),
					new ExpectedCellValue(sheetName, 14, 3, 7d),
					new ExpectedCellValue(sheetName, 2, 4, "Headlamp"),
					new ExpectedCellValue(sheetName, 3, 4, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 11, 4, 24.99),
					new ExpectedCellValue(sheetName, 12, 4, 24.99),
					new ExpectedCellValue(sheetName, 14, 4, 24.99),
					new ExpectedCellValue(sheetName, 3, 5, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 11, 5, 1d),
					new ExpectedCellValue(sheetName, 12, 5, 1d),
					new ExpectedCellValue(sheetName, 14, 5, 1d),
					new ExpectedCellValue(sheetName, 2, 6, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 3, 6, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 8, 6, 99d),
					new ExpectedCellValue(sheetName, 9, 6, 99d),
					new ExpectedCellValue(sheetName, 14, 6, 99d),
					new ExpectedCellValue(sheetName, 3, 7, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 8, 7, 1d),
					new ExpectedCellValue(sheetName, 9, 7, 1d),
					new ExpectedCellValue(sheetName, 14, 7, 1d),
					new ExpectedCellValue(sheetName, 2, 8, "Tent"),
					new ExpectedCellValue(sheetName, 3, 8, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 4, 8, 199d),
					new ExpectedCellValue(sheetName, 5, 8, 199d),
					new ExpectedCellValue(sheetName, 14, 8, 199d),
					new ExpectedCellValue(sheetName, 3, 9, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 4, 9, 6d),
					new ExpectedCellValue(sheetName, 5, 9, 6d),
					new ExpectedCellValue(sheetName, 14, 9, 6d),
					new ExpectedCellValue(sheetName, 2, 10, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 4, 10, 1030.5),
					new ExpectedCellValue(sheetName, 5, 10, 199d),
					new ExpectedCellValue(sheetName, 6, 10, 415.75),
					new ExpectedCellValue(sheetName, 7, 10, 415.75),
					new ExpectedCellValue(sheetName, 8, 10, 514.75),
					new ExpectedCellValue(sheetName, 9, 10, 99d),
					new ExpectedCellValue(sheetName, 10, 10, 415.75),
					new ExpectedCellValue(sheetName, 11, 10, 440.74),
					new ExpectedCellValue(sheetName, 12, 10, 24.99),
					new ExpectedCellValue(sheetName, 13, 10, 415.75),
					new ExpectedCellValue(sheetName, 14, 10, 1985.99),
					new ExpectedCellValue(sheetName, 2, 11, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 4, 11, 10d),
					new ExpectedCellValue(sheetName, 5, 11, 6d),
					new ExpectedCellValue(sheetName, 6, 11, 2d),
					new ExpectedCellValue(sheetName, 7, 11, 2d),
					new ExpectedCellValue(sheetName, 8, 11, 2d),
					new ExpectedCellValue(sheetName, 9, 11, 1d),
					new ExpectedCellValue(sheetName, 10, 11, 1d),
					new ExpectedCellValue(sheetName, 11, 11, 3d),
					new ExpectedCellValue(sheetName, 12, 11, 1d),
					new ExpectedCellValue(sheetName, 13, 11, 2d),
					new ExpectedCellValue(sheetName, 14, 11, 15d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsOneRowFieldAndTwoColumnFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowColumnFieldsSorting"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A18:S25"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowColumnFieldsSorting";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 22, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 23, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 24, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 25, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 19, 2, "March"),
					new ExpectedCellValue(sheetName, 20, 2, "Headlamp"),
					new ExpectedCellValue(sheetName, 21, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 24, 2, 24.99),
					new ExpectedCellValue(sheetName, 25, 2, 24.99),
					new ExpectedCellValue(sheetName, 21, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 24, 3, 1d),
					new ExpectedCellValue(sheetName, 25, 3, 1d),
					new ExpectedCellValue(sheetName, 20, 4, "Car Rack"),
					new ExpectedCellValue(sheetName, 21, 4, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 4, 415.75),
					new ExpectedCellValue(sheetName, 25, 4, 415.75),
					new ExpectedCellValue(sheetName, 21, 5, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 5, 2d),
					new ExpectedCellValue(sheetName, 25, 5, 2d),
					new ExpectedCellValue(sheetName, 19, 6, "March Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 6, 415.75),
					new ExpectedCellValue(sheetName, 24, 6, 24.99),
					new ExpectedCellValue(sheetName, 25, 6, 440.74),
					new ExpectedCellValue(sheetName, 19, 7, "March Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 7, 2d),
					new ExpectedCellValue(sheetName, 24, 7, 1d),
					new ExpectedCellValue(sheetName, 25, 7, 3d),
					new ExpectedCellValue(sheetName, 19, 8, "January"),
					new ExpectedCellValue(sheetName, 20, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 21, 8, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 8, 415.75),
					new ExpectedCellValue(sheetName, 23, 8, 415.75),
					new ExpectedCellValue(sheetName, 24, 8, 415.75),
					new ExpectedCellValue(sheetName, 25, 8, 1247.25),
					new ExpectedCellValue(sheetName, 21, 9, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 9, 2d),
					new ExpectedCellValue(sheetName, 23, 9, 1d),
					new ExpectedCellValue(sheetName, 24, 9, 2d),
					new ExpectedCellValue(sheetName, 25, 9, 5d),
					new ExpectedCellValue(sheetName, 19, 10, "January Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 10, 415.75),
					new ExpectedCellValue(sheetName, 23, 10, 415.75),
					new ExpectedCellValue(sheetName, 24, 10, 415.75),
					new ExpectedCellValue(sheetName, 25, 10, 1247.25),
					new ExpectedCellValue(sheetName, 19, 11, "January Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 11, 2d),
					new ExpectedCellValue(sheetName, 23, 11, 1d),
					new ExpectedCellValue(sheetName, 24, 11, 2d),
					new ExpectedCellValue(sheetName, 25, 11, 5d),
					new ExpectedCellValue(sheetName, 19, 12, "February"),
					new ExpectedCellValue(sheetName, 20, 12, "Tent"),
					new ExpectedCellValue(sheetName, 21, 12, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 12, 199d),
					new ExpectedCellValue(sheetName, 25, 12, 199d),
					new ExpectedCellValue(sheetName, 21, 13, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 13, 6d),
					new ExpectedCellValue(sheetName, 25, 13, 6d),
					new ExpectedCellValue(sheetName, 20, 14, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 21, 14, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 23, 14, 99d),
					new ExpectedCellValue(sheetName, 25, 14, 99d),
					new ExpectedCellValue(sheetName, 21, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 23, 15, 1d),
					new ExpectedCellValue(sheetName, 25, 15, 1d),
					new ExpectedCellValue(sheetName, 19, 16, "February Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 16, 199d),
					new ExpectedCellValue(sheetName, 23, 16, 99d),
					new ExpectedCellValue(sheetName, 25, 16, 298d),
					new ExpectedCellValue(sheetName, 19, 17, "February Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 17, 6d),
					new ExpectedCellValue(sheetName, 23, 17, 1d),
					new ExpectedCellValue(sheetName, 25, 17, 7d),
					new ExpectedCellValue(sheetName, 19, 18, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 22, 18, 1030.5),
					new ExpectedCellValue(sheetName, 23, 18, 514.75),
					new ExpectedCellValue(sheetName, 24, 18, 440.74),
					new ExpectedCellValue(sheetName, 25, 18, 1985.99),
					new ExpectedCellValue(sheetName, 19, 19, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 19, 10d),
					new ExpectedCellValue(sheetName, 23, 19, 2d),
					new ExpectedCellValue(sheetName, 24, 19, 3d),
					new ExpectedCellValue(sheetName, 25, 19, 15d),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsTwoRowFieldAndTwoColumnFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowColumnFieldsSorting"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A29:AH44"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowColumnFieldsSorting";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					// These values will need to be changed when Bug #10033 is completed.
					new ExpectedCellValue(sheetName, 33, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 34, 1, 20100076),
					new ExpectedCellValue(sheetName, 35, 1, 20100007),
					new ExpectedCellValue(sheetName, 36, 1, 20100017),
					new ExpectedCellValue(sheetName, 37, 1, 20100090),
					new ExpectedCellValue(sheetName, 38, 1, "Tent"),
					new ExpectedCellValue(sheetName, 39, 1, 20100070),
					new ExpectedCellValue(sheetName, 40, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 41, 1, 20100085),
					new ExpectedCellValue(sheetName, 42, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 43, 1, 20100083),
					new ExpectedCellValue(sheetName, 44, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 30, 2, "March"),
					new ExpectedCellValue(sheetName, 31, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 32, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 42, 2, 24.99),
					new ExpectedCellValue(sheetName, 43, 2, 24.99),
					new ExpectedCellValue(sheetName, 44, 2, 24.99),
					new ExpectedCellValue(sheetName, 32, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 42, 3, 1d),
					new ExpectedCellValue(sheetName, 43, 3, 1d),
					new ExpectedCellValue(sheetName, 44, 3, 1d),
					new ExpectedCellValue(sheetName, 32, 4, "Sum of Total"),
					new ExpectedCellValue(sheetName, 42, 4, 24.99),
					new ExpectedCellValue(sheetName, 43, 4, 24.99),
					new ExpectedCellValue(sheetName, 44, 4, 24.99),
					new ExpectedCellValue(sheetName, 31, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 32, 5, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 33, 5, 415.75),
					new ExpectedCellValue(sheetName, 36, 5, 415.75),
					new ExpectedCellValue(sheetName, 44, 5, 415.75),
					new ExpectedCellValue(sheetName, 32, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 6, 2d),
					new ExpectedCellValue(sheetName, 36, 6, 2d),
					new ExpectedCellValue(sheetName, 44, 6, 2d),
					new ExpectedCellValue(sheetName, 32, 7, "Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 7, 831.5),
					new ExpectedCellValue(sheetName, 36, 7, 831.5),
					new ExpectedCellValue(sheetName, 44, 7, 831.5),
					new ExpectedCellValue(sheetName, 30, 8, "March Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 33, 8, 415.75),
					new ExpectedCellValue(sheetName, 36, 8, 415.75),
					new ExpectedCellValue(sheetName, 42, 8, 24.99),
					new ExpectedCellValue(sheetName, 43, 8, 24.99),
					new ExpectedCellValue(sheetName, 44, 8, 440.74),
					new ExpectedCellValue(sheetName, 30, 9, "March Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 9, 2d),
					new ExpectedCellValue(sheetName, 36, 9, 2d),
					new ExpectedCellValue(sheetName, 42, 9, 1d),
					new ExpectedCellValue(sheetName, 43, 9, 1d),
					new ExpectedCellValue(sheetName, 44, 9, 3d),
					new ExpectedCellValue(sheetName, 30, 10, "March Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 10, 831.5),
					new ExpectedCellValue(sheetName, 36, 10, 831.5),
					new ExpectedCellValue(sheetName, 42, 10, 24.99),
					new ExpectedCellValue(sheetName, 43, 10, 24.99),
					new ExpectedCellValue(sheetName, 44, 10, 856.49),
					new ExpectedCellValue(sheetName, 30, 11, "February"),
					new ExpectedCellValue(sheetName, 31, 11, "San Francisco"),
					new ExpectedCellValue(sheetName, 32, 11, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 40, 11, 99d),
					new ExpectedCellValue(sheetName, 41, 11, 99d),
					new ExpectedCellValue(sheetName, 44, 11, 99d),
					new ExpectedCellValue(sheetName, 32, 12, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 40, 12, 1d),
					new ExpectedCellValue(sheetName, 41, 12, 1d),
					new ExpectedCellValue(sheetName, 44, 12, 1d),
					new ExpectedCellValue(sheetName, 32, 13, "Sum of Total"),
					new ExpectedCellValue(sheetName, 40, 13, 99d),
					new ExpectedCellValue(sheetName, 41, 13, 99d),
					new ExpectedCellValue(sheetName, 44, 13, 99d),
					new ExpectedCellValue(sheetName, 31, 14, "Nashville"),
					new ExpectedCellValue(sheetName, 32, 14, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 38, 14, 199d),
					new ExpectedCellValue(sheetName, 39, 14, 199d),
					new ExpectedCellValue(sheetName, 44, 14, 199d),
					new ExpectedCellValue(sheetName, 32, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 38, 15, 6d),
					new ExpectedCellValue(sheetName, 39, 15, 6d),
					new ExpectedCellValue(sheetName, 44, 15, 6d),
					new ExpectedCellValue(sheetName, 32, 16, "Sum of Total"),
					new ExpectedCellValue(sheetName, 38, 16, 1194d),
					new ExpectedCellValue(sheetName, 39, 16, 1194d),
					new ExpectedCellValue(sheetName, 44, 16, 1194d),
					new ExpectedCellValue(sheetName, 30, 17, "February Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 38, 17, 199d),
					new ExpectedCellValue(sheetName, 39, 17, 199d),
					new ExpectedCellValue(sheetName, 40, 17, 99d),
					new ExpectedCellValue(sheetName, 41, 17, 99d),
					new ExpectedCellValue(sheetName, 44, 17, 298d),
					new ExpectedCellValue(sheetName, 30, 18, "February Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 38, 18, 6d),
					new ExpectedCellValue(sheetName, 39, 18, 6d),
					new ExpectedCellValue(sheetName, 40, 18, 1d),
					new ExpectedCellValue(sheetName, 41, 18, 1d),
					new ExpectedCellValue(sheetName, 44, 18, 7d),
					new ExpectedCellValue(sheetName, 30, 19, "February Sum of Total"),
					new ExpectedCellValue(sheetName, 38, 19, 1194d),
					new ExpectedCellValue(sheetName, 39, 19, 1194d),
					new ExpectedCellValue(sheetName, 40, 19, 99d),
					new ExpectedCellValue(sheetName, 41, 19, 99d),
					new ExpectedCellValue(sheetName, 44, 19, 1293d),
					new ExpectedCellValue(sheetName, 30, 20, "January"),
					new ExpectedCellValue(sheetName, 31, 20, "San Francisco"),
					new ExpectedCellValue(sheetName, 32, 20, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 33, 20, 415.75),
					new ExpectedCellValue(sheetName, 34, 20, 415.75),
					new ExpectedCellValue(sheetName, 44, 20, 415.75),
					new ExpectedCellValue(sheetName, 32, 21, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 21, 1d),
					new ExpectedCellValue(sheetName, 34, 21, 1d),
					new ExpectedCellValue(sheetName, 44, 21, 1d),
					new ExpectedCellValue(sheetName, 32, 22, "Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 22, 415.75),
					new ExpectedCellValue(sheetName, 34, 22, 415.75),
					new ExpectedCellValue(sheetName, 44, 22, 415.75),
					new ExpectedCellValue(sheetName, 31, 23, "Chicago"),
					new ExpectedCellValue(sheetName, 32, 23, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 33, 23, 415.75),
					new ExpectedCellValue(sheetName, 35, 23, 415.75),
					new ExpectedCellValue(sheetName, 44, 23, 415.75),
					new ExpectedCellValue(sheetName, 32, 24, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 24, 2d),
					new ExpectedCellValue(sheetName, 35, 24, 2d),
					new ExpectedCellValue(sheetName, 44, 24, 2d),
					new ExpectedCellValue(sheetName, 32, 25, "Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 25, 831.5),
					new ExpectedCellValue(sheetName, 35, 25, 831.5),
					new ExpectedCellValue(sheetName, 44, 25, 831.5),
					new ExpectedCellValue(sheetName, 31, 26, "Nashville"),
					new ExpectedCellValue(sheetName, 32, 26, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 33, 26, 415.75),
					new ExpectedCellValue(sheetName, 37, 26, 415.75),
					new ExpectedCellValue(sheetName, 44, 26, 415.75),
					new ExpectedCellValue(sheetName, 32, 27, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 27, 2d),
					new ExpectedCellValue(sheetName, 37, 27, 2d),
					new ExpectedCellValue(sheetName, 44, 27, 2d),
					new ExpectedCellValue(sheetName, 32, 28, "Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 28, 831.5),
					new ExpectedCellValue(sheetName, 37, 28, 831.5),
					new ExpectedCellValue(sheetName, 44, 28, 831.5),
					new ExpectedCellValue(sheetName, 30, 29, "January Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 33, 29, 1247.25),
					new ExpectedCellValue(sheetName, 34, 29, 415.75),
					new ExpectedCellValue(sheetName, 35, 29, 415.75),
					new ExpectedCellValue(sheetName, 37, 29, 415.75),
					new ExpectedCellValue(sheetName, 44, 29, 1247.25),
					new ExpectedCellValue(sheetName, 30, 30, "January Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 30, 5d),
					new ExpectedCellValue(sheetName, 34, 30, 1d),
					new ExpectedCellValue(sheetName, 35, 30, 2d),
					new ExpectedCellValue(sheetName, 37, 30, 2d),
					new ExpectedCellValue(sheetName, 44, 30, 5d),
					new ExpectedCellValue(sheetName, 30, 31, "January Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 31, 2078.75),
					new ExpectedCellValue(sheetName, 34, 31, 415.75),
					new ExpectedCellValue(sheetName, 35, 31, 831.5),
					new ExpectedCellValue(sheetName, 37, 31, 831.5),
					new ExpectedCellValue(sheetName, 44, 31, 2078.75),
					new ExpectedCellValue(sheetName, 30, 32, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 33, 32, 1663d),
					new ExpectedCellValue(sheetName, 34, 32, 415.75),
					new ExpectedCellValue(sheetName, 35, 32, 415.75),
					new ExpectedCellValue(sheetName, 36, 32, 415.75),
					new ExpectedCellValue(sheetName, 37, 32, 415.75),
					new ExpectedCellValue(sheetName, 38, 32, 199d),
					new ExpectedCellValue(sheetName, 39, 32, 199d),
					new ExpectedCellValue(sheetName, 40, 32, 99d),
					new ExpectedCellValue(sheetName, 41, 32, 99d),
					new ExpectedCellValue(sheetName, 42, 32, 24.99),
					new ExpectedCellValue(sheetName, 43, 32, 24.99),
					new ExpectedCellValue(sheetName, 44, 32, 1985.99),
					new ExpectedCellValue(sheetName, 30, 33, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 33, 33, 7d),
					new ExpectedCellValue(sheetName, 34, 33, 1d),
					new ExpectedCellValue(sheetName, 35, 33, 2d),
					new ExpectedCellValue(sheetName, 36, 33, 2d),
					new ExpectedCellValue(sheetName, 37, 33, 2d),
					new ExpectedCellValue(sheetName, 38, 33, 6d),
					new ExpectedCellValue(sheetName, 39, 33, 6d),
					new ExpectedCellValue(sheetName, 40, 33, 1d),
					new ExpectedCellValue(sheetName, 41, 33, 1d),
					new ExpectedCellValue(sheetName, 42, 33, 1d),
					new ExpectedCellValue(sheetName, 43, 33, 1d),
					new ExpectedCellValue(sheetName, 44, 33, 15d),
					new ExpectedCellValue(sheetName, 30, 34, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 34, 2910.25),
					new ExpectedCellValue(sheetName, 34, 34, 415.75),
					new ExpectedCellValue(sheetName, 35, 34, 831.5),
					new ExpectedCellValue(sheetName, 36, 34, 831.5),
					new ExpectedCellValue(sheetName, 37, 34, 831.5),
					new ExpectedCellValue(sheetName, 38, 34, 1194d),
					new ExpectedCellValue(sheetName, 39, 34, 1194d),
					new ExpectedCellValue(sheetName, 40, 34, 99d),
					new ExpectedCellValue(sheetName, 41, 34, 99d),
					new ExpectedCellValue(sheetName, 42, 34, 24.99),
					new ExpectedCellValue(sheetName, 43, 34, 24.99),
					new ExpectedCellValue(sheetName, 44, 34, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsRowFieldsWithDataFieldsSortingRowsAndColumns()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DataFieldsInRows"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A1:E13"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DataFieldsInRows";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 6, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 7, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 8, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 9, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 10, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 11, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 12, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, "February"),
					new ExpectedCellValue(sheetName, 4, 2, 1d),
					new ExpectedCellValue(sheetName, 5, 2, 99d),
					new ExpectedCellValue(sheetName, 10, 2, 6d),
					new ExpectedCellValue(sheetName, 11, 2, 1194d),
					new ExpectedCellValue(sheetName, 12, 2, 7d),
					new ExpectedCellValue(sheetName, 13, 2, 1293d),
					new ExpectedCellValue(sheetName, 2, 3, "January"),
					new ExpectedCellValue(sheetName, 4, 3, 1d),
					new ExpectedCellValue(sheetName, 5, 3, 415.75),
					new ExpectedCellValue(sheetName, 7, 3, 2d),
					new ExpectedCellValue(sheetName, 8, 3, 831.5),
					new ExpectedCellValue(sheetName, 10, 3, 2d),
					new ExpectedCellValue(sheetName, 11, 3, 831.5),
					new ExpectedCellValue(sheetName, 12, 3, 5d),
					new ExpectedCellValue(sheetName, 13, 3, 2078.75),
					new ExpectedCellValue(sheetName, 2, 4, "March"),
					new ExpectedCellValue(sheetName, 7, 4, 1d),
					new ExpectedCellValue(sheetName, 8, 4, 24.99),
					new ExpectedCellValue(sheetName, 10, 4, 2d),
					new ExpectedCellValue(sheetName, 11, 4, 831.5),
					new ExpectedCellValue(sheetName, 12, 4, 3d),
					new ExpectedCellValue(sheetName, 13, 4, 856.49),
					new ExpectedCellValue(sheetName, 2, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 4, 5, 2d),
					new ExpectedCellValue(sheetName, 5, 5, 514.75),
					new ExpectedCellValue(sheetName, 7, 5, 3d),
					new ExpectedCellValue(sheetName, 8, 5, 856.49),
					new ExpectedCellValue(sheetName, 10, 5, 10d),
					new ExpectedCellValue(sheetName, 11, 5, 2857d),
					new ExpectedCellValue(sheetName, 12, 5, 15d),
					new ExpectedCellValue(sheetName, 13, 5, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsRowFieldsWithDataFieldsSortingRowsColumnsDataFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DataFieldsInRows"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A17:E29"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DataFieldsInRows";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 19, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 20, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 21, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 23, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 24, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 25, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 26, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 27, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 28, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 29, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 18, 2, "March"),
					new ExpectedCellValue(sheetName, 20, 2, 831.5),
					new ExpectedCellValue(sheetName, 21, 2, 2d),
					new ExpectedCellValue(sheetName, 23, 2, 24.99),
					new ExpectedCellValue(sheetName, 24, 2, 1d),
					new ExpectedCellValue(sheetName, 28, 2, 856.49),
					new ExpectedCellValue(sheetName, 29, 2, 3d),
					new ExpectedCellValue(sheetName, 18, 3, "February"),
					new ExpectedCellValue(sheetName, 20, 3, 1194d),
					new ExpectedCellValue(sheetName, 21, 3, 6d),
					new ExpectedCellValue(sheetName, 26, 3, 99d),
					new ExpectedCellValue(sheetName, 27, 3, 1d),
					new ExpectedCellValue(sheetName, 28, 3, 1293d),
					new ExpectedCellValue(sheetName, 29, 3, 7d),
					new ExpectedCellValue(sheetName, 18, 4, "January"),
					new ExpectedCellValue(sheetName, 20, 4, 831.5),
					new ExpectedCellValue(sheetName, 21, 4, 2d),
					new ExpectedCellValue(sheetName, 23, 4, 831.5),
					new ExpectedCellValue(sheetName, 24, 4, 2d),
					new ExpectedCellValue(sheetName, 26, 4, 415.75),
					new ExpectedCellValue(sheetName, 27, 4, 1d),
					new ExpectedCellValue(sheetName, 28, 4, 2078.75),
					new ExpectedCellValue(sheetName, 29, 4, 5d),
					new ExpectedCellValue(sheetName, 18, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 20, 5, 2857d),
					new ExpectedCellValue(sheetName, 21, 5, 10d),
					new ExpectedCellValue(sheetName, 23, 5, 856.49),
					new ExpectedCellValue(sheetName, 24, 5, 3d),
					new ExpectedCellValue(sheetName, 26, 5, 514.75),
					new ExpectedCellValue(sheetName, 27, 5, 2d),
					new ExpectedCellValue(sheetName, 28, 5, 4228.24),
					new ExpectedCellValue(sheetName, 29, 5, 15d),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsRowFieldsWithDataFieldsAndTwoColumnFieldsSortingRowsColumnsDataFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DataFieldsInRows"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A33:J46"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DataFieldsInRows";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 36, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 37, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 38, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 39, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 40, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 41, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 42, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 43, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 44, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 45, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 46, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 34, 2, "February"),
					new ExpectedCellValue(sheetName, 35, 2, "Tent"),
					new ExpectedCellValue(sheetName, 36, 2, null),
					new ExpectedCellValue(sheetName, 37, 2, null),
					new ExpectedCellValue(sheetName, 38, 2, null),
					new ExpectedCellValue(sheetName, 39, 2, null),
					new ExpectedCellValue(sheetName, 40, 2, 1194d),
					new ExpectedCellValue(sheetName, 41, 2, 6d),
					new ExpectedCellValue(sheetName, 42, 2, null),
					new ExpectedCellValue(sheetName, 43, 2, null),
					new ExpectedCellValue(sheetName, 44, 2, null),
					new ExpectedCellValue(sheetName, 45, 2, 1194d),
					new ExpectedCellValue(sheetName, 46, 2, 6d),
					new ExpectedCellValue(sheetName, 35, 3, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 36, 3, null),
					new ExpectedCellValue(sheetName, 37, 3, 99d),
					new ExpectedCellValue(sheetName, 38, 3, 1d),
					new ExpectedCellValue(sheetName, 39, 3, null),
					new ExpectedCellValue(sheetName, 40, 3, null),
					new ExpectedCellValue(sheetName, 41, 3, null),
					new ExpectedCellValue(sheetName, 42, 3, null),
					new ExpectedCellValue(sheetName, 43, 3, null),
					new ExpectedCellValue(sheetName, 44, 3, null),
					new ExpectedCellValue(sheetName, 45, 3, 99d),
					new ExpectedCellValue(sheetName, 46, 3, 1d),
					new ExpectedCellValue(sheetName, 34, 4, "February Total"),
					new ExpectedCellValue(sheetName, 35, 4, null),
					new ExpectedCellValue(sheetName, 36, 4, null),
					new ExpectedCellValue(sheetName, 37, 4, 99d),
					new ExpectedCellValue(sheetName, 38, 4, 1d),
					new ExpectedCellValue(sheetName, 39, 4, null),
					new ExpectedCellValue(sheetName, 40, 4, 1194d),
					new ExpectedCellValue(sheetName, 41, 4, 6d),
					new ExpectedCellValue(sheetName, 42, 4, null),
					new ExpectedCellValue(sheetName, 43, 4, null),
					new ExpectedCellValue(sheetName, 44, 4, null),
					new ExpectedCellValue(sheetName, 45, 4, 1293d),
					new ExpectedCellValue(sheetName, 46, 4, 7d),
					new ExpectedCellValue(sheetName, 34, 5, "January"),
					new ExpectedCellValue(sheetName, 35, 5, "Car Rack"),
					new ExpectedCellValue(sheetName, 36, 5, null),
					new ExpectedCellValue(sheetName, 37, 5, 415.75),
					new ExpectedCellValue(sheetName, 38, 5, 1d),
					new ExpectedCellValue(sheetName, 39, 5, null),
					new ExpectedCellValue(sheetName, 40, 5, 831.5),
					new ExpectedCellValue(sheetName, 41, 5, 2d),
					new ExpectedCellValue(sheetName, 42, 5, null),
					new ExpectedCellValue(sheetName, 43, 5, 831.5),
					new ExpectedCellValue(sheetName, 44, 5, 2d),
					new ExpectedCellValue(sheetName, 45, 5, 2078.75),
					new ExpectedCellValue(sheetName, 46, 5, 5d),
					new ExpectedCellValue(sheetName, 34, 6, "January Total"),
					new ExpectedCellValue(sheetName, 35, 6, null),
					new ExpectedCellValue(sheetName, 36, 6, null),
					new ExpectedCellValue(sheetName, 37, 6, 415.75),
					new ExpectedCellValue(sheetName, 38, 6, 1d),
					new ExpectedCellValue(sheetName, 39, 6, null),
					new ExpectedCellValue(sheetName, 40, 6, 831.5),
					new ExpectedCellValue(sheetName, 41, 6, 2d),
					new ExpectedCellValue(sheetName, 42, 6, null),
					new ExpectedCellValue(sheetName, 43, 6, 831.5),
					new ExpectedCellValue(sheetName, 44, 6, 2d),
					new ExpectedCellValue(sheetName, 45, 6, 2078.75),
					new ExpectedCellValue(sheetName, 46, 6, 5d),
					new ExpectedCellValue(sheetName, 34, 7, "March"),
					new ExpectedCellValue(sheetName, 35, 7, "Headlamp"),
					new ExpectedCellValue(sheetName, 36, 7, null),
					new ExpectedCellValue(sheetName, 37, 7, null),
					new ExpectedCellValue(sheetName, 38, 7, null),
					new ExpectedCellValue(sheetName, 39, 7, null),
					new ExpectedCellValue(sheetName, 40, 7, null),
					new ExpectedCellValue(sheetName, 41, 7, null),
					new ExpectedCellValue(sheetName, 42, 7, null),
					new ExpectedCellValue(sheetName, 43, 7, 24.99),
					new ExpectedCellValue(sheetName, 44, 7, 1d),
					new ExpectedCellValue(sheetName, 45, 7, 24.99),
					new ExpectedCellValue(sheetName, 46, 7, 1d),
					new ExpectedCellValue(sheetName, 35, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 36, 8, null),
					new ExpectedCellValue(sheetName, 37, 8, null),
					new ExpectedCellValue(sheetName, 38, 8, null),
					new ExpectedCellValue(sheetName, 39, 8, null),
					new ExpectedCellValue(sheetName, 40, 8, 831.5),
					new ExpectedCellValue(sheetName, 41, 8, 2d),
					new ExpectedCellValue(sheetName, 42, 8, null),
					new ExpectedCellValue(sheetName, 43, 8, null),
					new ExpectedCellValue(sheetName, 44, 8, null),
					new ExpectedCellValue(sheetName, 45, 8, 831.5),
					new ExpectedCellValue(sheetName, 46, 8, 2d),
					new ExpectedCellValue(sheetName, 34, 9, "March Total"),
					new ExpectedCellValue(sheetName, 35, 9, null),
					new ExpectedCellValue(sheetName, 36, 9, null),
					new ExpectedCellValue(sheetName, 37, 9, null),
					new ExpectedCellValue(sheetName, 38, 9, null),
					new ExpectedCellValue(sheetName, 39, 9, null),
					new ExpectedCellValue(sheetName, 40, 9, 831.5),
					new ExpectedCellValue(sheetName, 41, 9, 2d),
					new ExpectedCellValue(sheetName, 42, 9, null),
					new ExpectedCellValue(sheetName, 43, 9, 24.99),
					new ExpectedCellValue(sheetName, 44, 9, 1d),
					new ExpectedCellValue(sheetName, 45, 9, 856.49),
					new ExpectedCellValue(sheetName, 46, 9, 3d),
					new ExpectedCellValue(sheetName, 34, 10, "Grand Total"),
					new ExpectedCellValue(sheetName, 35, 10, null),
					new ExpectedCellValue(sheetName, 36, 10, null),
					new ExpectedCellValue(sheetName, 37, 10, 514.75),
					new ExpectedCellValue(sheetName, 38, 10, 2d),
					new ExpectedCellValue(sheetName, 39, 10, null),
					new ExpectedCellValue(sheetName, 40, 10, 2857d),
					new ExpectedCellValue(sheetName, 41, 10, 10d),
					new ExpectedCellValue(sheetName, 42, 10, null),
					new ExpectedCellValue(sheetName, 43, 10, 856.49),
					new ExpectedCellValue(sheetName, 44, 10, 3d),
					new ExpectedCellValue(sheetName, 45, 10, 4228.24),
					new ExpectedCellValue(sheetName, 46, 10, 15d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableSorting.xlsx")]
		public void PivotTableRefreshCustomSortingWithReferencesToDataFieldsTwoRowFieldsWithDataFieldsAndTwoColumnFieldsSortingRowsColumnsDataFields()
		{
			var file = new FileInfo("PivotTableSorting.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DataFieldsInRows"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					this.CheckPivotTableAddress(new ExcelAddress("A50:J84"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DataFieldsInRows";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 53, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 54, 1, 20100090),
					new ExpectedCellValue(sheetName, 55, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 56, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 57, 1, 20100070),
					new ExpectedCellValue(sheetName, 58, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 59, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 60, 1, 20100017),
					new ExpectedCellValue(sheetName, 61, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 62, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 63, 1, "Nashville Sum of Total"),
					new ExpectedCellValue(sheetName, 64, 1, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 65, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 66, 1, 20100083),
					new ExpectedCellValue(sheetName, 67, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 68, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 69, 1, 20100007),
					new ExpectedCellValue(sheetName, 70, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 71, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 72, 1, "Chicago Sum of Total"),
					new ExpectedCellValue(sheetName, 73, 1, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 74, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 75, 1, 20100085),
					new ExpectedCellValue(sheetName, 76, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 77, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 78, 1, 20100076),
					new ExpectedCellValue(sheetName, 79, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 80, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 81, 1, "San Francisco Sum of Total"),
					new ExpectedCellValue(sheetName, 82, 1, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 83, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 84, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 51, 2, "February"),
					new ExpectedCellValue(sheetName, 52, 2, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 53, 2, null),
					new ExpectedCellValue(sheetName, 54, 2, null),
					new ExpectedCellValue(sheetName, 55, 2, null),
					new ExpectedCellValue(sheetName, 56, 2, null),
					new ExpectedCellValue(sheetName, 57, 2, null),
					new ExpectedCellValue(sheetName, 58, 2, null),
					new ExpectedCellValue(sheetName, 59, 2, null),
					new ExpectedCellValue(sheetName, 60, 2, null),
					new ExpectedCellValue(sheetName, 61, 2, null),
					new ExpectedCellValue(sheetName, 62, 2, null),
					new ExpectedCellValue(sheetName, 63, 2, null),
					new ExpectedCellValue(sheetName, 64, 2, null),
					new ExpectedCellValue(sheetName, 65, 2, null),
					new ExpectedCellValue(sheetName, 66, 2, null),
					new ExpectedCellValue(sheetName, 67, 2, null),
					new ExpectedCellValue(sheetName, 68, 2, null),
					new ExpectedCellValue(sheetName, 69, 2, null),
					new ExpectedCellValue(sheetName, 70, 2, null),
					new ExpectedCellValue(sheetName, 71, 2, null),
					new ExpectedCellValue(sheetName, 72, 2, null),
					new ExpectedCellValue(sheetName, 73, 2, null),
					new ExpectedCellValue(sheetName, 74, 2, null),
					new ExpectedCellValue(sheetName, 75, 2, null),
					new ExpectedCellValue(sheetName, 76, 2, 99d),
					new ExpectedCellValue(sheetName, 77, 2, 1d),
					new ExpectedCellValue(sheetName, 78, 2, null),
					new ExpectedCellValue(sheetName, 79, 2, null),
					new ExpectedCellValue(sheetName, 80, 2, null),
					new ExpectedCellValue(sheetName, 81, 2, 99d),
					new ExpectedCellValue(sheetName, 82, 2, 1d),
					new ExpectedCellValue(sheetName, 83, 2, 99d),
					new ExpectedCellValue(sheetName, 84, 2, 1d),
					new ExpectedCellValue(sheetName, 52, 3, "Tent"),
					new ExpectedCellValue(sheetName, 53, 3, null),
					new ExpectedCellValue(sheetName, 54, 3, null),
					new ExpectedCellValue(sheetName, 55, 3, null),
					new ExpectedCellValue(sheetName, 56, 3, null),
					new ExpectedCellValue(sheetName, 57, 3, null),
					new ExpectedCellValue(sheetName, 58, 3, 1194d),
					new ExpectedCellValue(sheetName, 59, 3, 6d),
					new ExpectedCellValue(sheetName, 60, 3, null),
					new ExpectedCellValue(sheetName, 61, 3, null),
					new ExpectedCellValue(sheetName, 62, 3, null),
					new ExpectedCellValue(sheetName, 63, 3, 1194d),
					new ExpectedCellValue(sheetName, 64, 3, 6d),
					new ExpectedCellValue(sheetName, 65, 3, null),
					new ExpectedCellValue(sheetName, 66, 3, null),
					new ExpectedCellValue(sheetName, 67, 3, null),
					new ExpectedCellValue(sheetName, 68, 3, null),
					new ExpectedCellValue(sheetName, 69, 3, null),
					new ExpectedCellValue(sheetName, 70, 3, null),
					new ExpectedCellValue(sheetName, 71, 3, null),
					new ExpectedCellValue(sheetName, 72, 3, null),
					new ExpectedCellValue(sheetName, 73, 3, null),
					new ExpectedCellValue(sheetName, 74, 3, null),
					new ExpectedCellValue(sheetName, 75, 3, null),
					new ExpectedCellValue(sheetName, 76, 3, null),
					new ExpectedCellValue(sheetName, 77, 3, null),
					new ExpectedCellValue(sheetName, 78, 3, null),
					new ExpectedCellValue(sheetName, 79, 3, null),
					new ExpectedCellValue(sheetName, 80, 3, null),
					new ExpectedCellValue(sheetName, 81, 3, null),
					new ExpectedCellValue(sheetName, 82, 3, null),
					new ExpectedCellValue(sheetName, 83, 3, 1194d),
					new ExpectedCellValue(sheetName, 84, 3, 6d),
					new ExpectedCellValue(sheetName, 51, 4, "February Total"),
					new ExpectedCellValue(sheetName, 52, 4, null),
					new ExpectedCellValue(sheetName, 53, 4, null),
					new ExpectedCellValue(sheetName, 54, 4, null),
					new ExpectedCellValue(sheetName, 55, 4, null),
					new ExpectedCellValue(sheetName, 56, 4, null),
					new ExpectedCellValue(sheetName, 57, 4, null),
					new ExpectedCellValue(sheetName, 58, 4, 1194d),
					new ExpectedCellValue(sheetName, 59, 4, 6d),
					new ExpectedCellValue(sheetName, 60, 4, null),
					new ExpectedCellValue(sheetName, 61, 4, null),
					new ExpectedCellValue(sheetName, 62, 4, null),
					new ExpectedCellValue(sheetName, 63, 4, 1194d),
					new ExpectedCellValue(sheetName, 64, 4, 6d),
					new ExpectedCellValue(sheetName, 65, 4, null),
					new ExpectedCellValue(sheetName, 66, 4, null),
					new ExpectedCellValue(sheetName, 67, 4, null),
					new ExpectedCellValue(sheetName, 68, 4, null),
					new ExpectedCellValue(sheetName, 69, 4, null),
					new ExpectedCellValue(sheetName, 70, 4, null),
					new ExpectedCellValue(sheetName, 71, 4, null),
					new ExpectedCellValue(sheetName, 72, 4, null),
					new ExpectedCellValue(sheetName, 73, 4, null),
					new ExpectedCellValue(sheetName, 74, 4, null),
					new ExpectedCellValue(sheetName, 75, 4, null),
					new ExpectedCellValue(sheetName, 76, 4, 99d),
					new ExpectedCellValue(sheetName, 77, 4, 1d),
					new ExpectedCellValue(sheetName, 78, 4, null),
					new ExpectedCellValue(sheetName, 79, 4, null),
					new ExpectedCellValue(sheetName, 80, 4, null),
					new ExpectedCellValue(sheetName, 81, 4, 99d),
					new ExpectedCellValue(sheetName, 82, 4, 1d),
					new ExpectedCellValue(sheetName, 83, 4, 1293d),
					new ExpectedCellValue(sheetName, 84, 4, 7d),
					new ExpectedCellValue(sheetName, 51, 5, "January"),
					new ExpectedCellValue(sheetName, 52, 5, "Car Rack"),
					new ExpectedCellValue(sheetName, 53, 5, null),
					new ExpectedCellValue(sheetName, 54, 5, null),
					new ExpectedCellValue(sheetName, 55, 5, 831.5),
					new ExpectedCellValue(sheetName, 56, 5, 2d),
					new ExpectedCellValue(sheetName, 57, 5, null),
					new ExpectedCellValue(sheetName, 58, 5, null),
					new ExpectedCellValue(sheetName, 59, 5, null),
					new ExpectedCellValue(sheetName, 60, 5, null),
					new ExpectedCellValue(sheetName, 61, 5, null),
					new ExpectedCellValue(sheetName, 62, 5, null),
					new ExpectedCellValue(sheetName, 63, 5, 831.5),
					new ExpectedCellValue(sheetName, 64, 5, 2d),
					new ExpectedCellValue(sheetName, 65, 5, null),
					new ExpectedCellValue(sheetName, 66, 5, null),
					new ExpectedCellValue(sheetName, 67, 5, null),
					new ExpectedCellValue(sheetName, 68, 5, null),
					new ExpectedCellValue(sheetName, 69, 5, null),
					new ExpectedCellValue(sheetName, 70, 5, 831.5),
					new ExpectedCellValue(sheetName, 71, 5, 2d),
					new ExpectedCellValue(sheetName, 72, 5, 831.5),
					new ExpectedCellValue(sheetName, 73, 5, 2d),
					new ExpectedCellValue(sheetName, 74, 5, null),
					new ExpectedCellValue(sheetName, 75, 5, null),
					new ExpectedCellValue(sheetName, 76, 5, null),
					new ExpectedCellValue(sheetName, 77, 5, null),
					new ExpectedCellValue(sheetName, 78, 5, null),
					new ExpectedCellValue(sheetName, 79, 5, 415.75),
					new ExpectedCellValue(sheetName, 80, 5, 1d),
					new ExpectedCellValue(sheetName, 81, 5, 415.75),
					new ExpectedCellValue(sheetName, 82, 5, 1d),
					new ExpectedCellValue(sheetName, 83, 5, 2078.75),
					new ExpectedCellValue(sheetName, 84, 5, 5d),
					new ExpectedCellValue(sheetName, 51, 6, "January Total"),
					new ExpectedCellValue(sheetName, 52, 6, null),
					new ExpectedCellValue(sheetName, 53, 6, null),
					new ExpectedCellValue(sheetName, 54, 6, null),
					new ExpectedCellValue(sheetName, 55, 6, 831.5),
					new ExpectedCellValue(sheetName, 56, 6, 2d),
					new ExpectedCellValue(sheetName, 57, 6, null),
					new ExpectedCellValue(sheetName, 58, 6, null),
					new ExpectedCellValue(sheetName, 59, 6, null),
					new ExpectedCellValue(sheetName, 60, 6, null),
					new ExpectedCellValue(sheetName, 61, 6, null),
					new ExpectedCellValue(sheetName, 62, 6, null),
					new ExpectedCellValue(sheetName, 63, 6, 831.5),
					new ExpectedCellValue(sheetName, 64, 6, 2d),
					new ExpectedCellValue(sheetName, 65, 6, null),
					new ExpectedCellValue(sheetName, 66, 6, null),
					new ExpectedCellValue(sheetName, 67, 6, null),
					new ExpectedCellValue(sheetName, 68, 6, null),
					new ExpectedCellValue(sheetName, 69, 6, null),
					new ExpectedCellValue(sheetName, 70, 6, 831.5),
					new ExpectedCellValue(sheetName, 71, 6, 2d),
					new ExpectedCellValue(sheetName, 72, 6, 831.5),
					new ExpectedCellValue(sheetName, 73, 6, 2d),
					new ExpectedCellValue(sheetName, 74, 6, null),
					new ExpectedCellValue(sheetName, 75, 6, null),
					new ExpectedCellValue(sheetName, 76, 6, null),
					new ExpectedCellValue(sheetName, 77, 6, null),
					new ExpectedCellValue(sheetName, 78, 6, null),
					new ExpectedCellValue(sheetName, 79, 6, 415.75),
					new ExpectedCellValue(sheetName, 80, 6, 1d),
					new ExpectedCellValue(sheetName, 81, 6, 415.75),
					new ExpectedCellValue(sheetName, 82, 6, 1d),
					new ExpectedCellValue(sheetName, 83, 6, 2078.75),
					new ExpectedCellValue(sheetName, 84, 6, 5d),
					new ExpectedCellValue(sheetName, 51, 7, "March"),
					new ExpectedCellValue(sheetName, 52, 7, "Headlamp"),
					new ExpectedCellValue(sheetName, 53, 7, null),
					new ExpectedCellValue(sheetName, 54, 7, null),
					new ExpectedCellValue(sheetName, 55, 7, null),
					new ExpectedCellValue(sheetName, 56, 7, null),
					new ExpectedCellValue(sheetName, 57, 7, null),
					new ExpectedCellValue(sheetName, 58, 7, null),
					new ExpectedCellValue(sheetName, 59, 7, null),
					new ExpectedCellValue(sheetName, 60, 7, null),
					new ExpectedCellValue(sheetName, 61, 7, null),
					new ExpectedCellValue(sheetName, 62, 7, null),
					new ExpectedCellValue(sheetName, 63, 7, null),
					new ExpectedCellValue(sheetName, 64, 7, null),
					new ExpectedCellValue(sheetName, 65, 7, null),
					new ExpectedCellValue(sheetName, 66, 7, null),
					new ExpectedCellValue(sheetName, 67, 7, 24.99),
					new ExpectedCellValue(sheetName, 68, 7, 1d),
					new ExpectedCellValue(sheetName, 69, 7, null),
					new ExpectedCellValue(sheetName, 70, 7, null),
					new ExpectedCellValue(sheetName, 71, 7, null),
					new ExpectedCellValue(sheetName, 72, 7, 24.99),
					new ExpectedCellValue(sheetName, 73, 7, 1d),
					new ExpectedCellValue(sheetName, 74, 7, null),
					new ExpectedCellValue(sheetName, 75, 7, null),
					new ExpectedCellValue(sheetName, 76, 7, null),
					new ExpectedCellValue(sheetName, 77, 7, null),
					new ExpectedCellValue(sheetName, 78, 7, null),
					new ExpectedCellValue(sheetName, 79, 7, null),
					new ExpectedCellValue(sheetName, 80, 7, null),
					new ExpectedCellValue(sheetName, 81, 7, null),
					new ExpectedCellValue(sheetName, 82, 7, null),
					new ExpectedCellValue(sheetName, 83, 7, 24.99),
					new ExpectedCellValue(sheetName, 84, 7, 1d),
					new ExpectedCellValue(sheetName, 52, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 53, 8, null),
					new ExpectedCellValue(sheetName, 54, 8, null),
					new ExpectedCellValue(sheetName, 55, 8, null),
					new ExpectedCellValue(sheetName, 56, 8, null),
					new ExpectedCellValue(sheetName, 57, 8, null),
					new ExpectedCellValue(sheetName, 58, 8, null),
					new ExpectedCellValue(sheetName, 59, 8, null),
					new ExpectedCellValue(sheetName, 60, 8, null),
					new ExpectedCellValue(sheetName, 61, 8, 831.5),
					new ExpectedCellValue(sheetName, 62, 8, 2d),
					new ExpectedCellValue(sheetName, 63, 8, 831.5),
					new ExpectedCellValue(sheetName, 64, 8, 2d),
					new ExpectedCellValue(sheetName, 65, 8, null),
					new ExpectedCellValue(sheetName, 66, 8, null),
					new ExpectedCellValue(sheetName, 67, 8, null),
					new ExpectedCellValue(sheetName, 68, 8, null),
					new ExpectedCellValue(sheetName, 69, 8, null),
					new ExpectedCellValue(sheetName, 70, 8, null),
					new ExpectedCellValue(sheetName, 71, 8, null),
					new ExpectedCellValue(sheetName, 72, 8, null),
					new ExpectedCellValue(sheetName, 73, 8, null),
					new ExpectedCellValue(sheetName, 74, 8, null),
					new ExpectedCellValue(sheetName, 75, 8, null),
					new ExpectedCellValue(sheetName, 76, 8, null),
					new ExpectedCellValue(sheetName, 77, 8, null),
					new ExpectedCellValue(sheetName, 78, 8, null),
					new ExpectedCellValue(sheetName, 79, 8, null),
					new ExpectedCellValue(sheetName, 80, 8, null),
					new ExpectedCellValue(sheetName, 81, 8, null),
					new ExpectedCellValue(sheetName, 82, 8, null),
					new ExpectedCellValue(sheetName, 83, 8, 831.5),
					new ExpectedCellValue(sheetName, 84, 8, 2d),
					new ExpectedCellValue(sheetName, 51, 9, "March Total"),
					new ExpectedCellValue(sheetName, 52, 9, null),
					new ExpectedCellValue(sheetName, 53, 9, null),
					new ExpectedCellValue(sheetName, 54, 9, null),
					new ExpectedCellValue(sheetName, 55, 9, null),
					new ExpectedCellValue(sheetName, 56, 9, null),
					new ExpectedCellValue(sheetName, 57, 9, null),
					new ExpectedCellValue(sheetName, 58, 9, null),
					new ExpectedCellValue(sheetName, 59, 9, null),
					new ExpectedCellValue(sheetName, 60, 9, null),
					new ExpectedCellValue(sheetName, 61, 9, 831.5),
					new ExpectedCellValue(sheetName, 62, 9, 2d),
					new ExpectedCellValue(sheetName, 63, 9, 831.5),
					new ExpectedCellValue(sheetName, 64, 9, 2d),
					new ExpectedCellValue(sheetName, 65, 9, null),
					new ExpectedCellValue(sheetName, 66, 9, null),
					new ExpectedCellValue(sheetName, 67, 9, 24.99),
					new ExpectedCellValue(sheetName, 68, 9, 1d),
					new ExpectedCellValue(sheetName, 69, 9, null),
					new ExpectedCellValue(sheetName, 70, 9, null),
					new ExpectedCellValue(sheetName, 71, 9, null),
					new ExpectedCellValue(sheetName, 72, 9, 24.99),
					new ExpectedCellValue(sheetName, 73, 9, 1d),
					new ExpectedCellValue(sheetName, 74, 9, null),
					new ExpectedCellValue(sheetName, 75, 9, null),
					new ExpectedCellValue(sheetName, 76, 9, null),
					new ExpectedCellValue(sheetName, 77, 9, null),
					new ExpectedCellValue(sheetName, 78, 9, null),
					new ExpectedCellValue(sheetName, 79, 9, null),
					new ExpectedCellValue(sheetName, 80, 9, null),
					new ExpectedCellValue(sheetName, 81, 9, null),
					new ExpectedCellValue(sheetName, 82, 9, null),
					new ExpectedCellValue(sheetName, 83, 9, 856.49),
					new ExpectedCellValue(sheetName, 84, 9, 3d),
					new ExpectedCellValue(sheetName, 51, 10, "Grand Total"),
					new ExpectedCellValue(sheetName, 52, 10, null),
					new ExpectedCellValue(sheetName, 53, 10, null),
					new ExpectedCellValue(sheetName, 54, 10, null),
					new ExpectedCellValue(sheetName, 55, 10, 831.5),
					new ExpectedCellValue(sheetName, 56, 10, 2d),
					new ExpectedCellValue(sheetName, 57, 10, null),
					new ExpectedCellValue(sheetName, 58, 10, 1194d),
					new ExpectedCellValue(sheetName, 59, 10, 6d),
					new ExpectedCellValue(sheetName, 60, 10, null),
					new ExpectedCellValue(sheetName, 61, 10, 831.5),
					new ExpectedCellValue(sheetName, 62, 10, 2d),
					new ExpectedCellValue(sheetName, 63, 10, 2857d),
					new ExpectedCellValue(sheetName, 64, 10, 10d),
					new ExpectedCellValue(sheetName, 65, 10, null),
					new ExpectedCellValue(sheetName, 66, 10, null),
					new ExpectedCellValue(sheetName, 67, 10, 24.99),
					new ExpectedCellValue(sheetName, 68, 10, 1d),
					new ExpectedCellValue(sheetName, 69, 10, null),
					new ExpectedCellValue(sheetName, 70, 10, 831.5),
					new ExpectedCellValue(sheetName, 71, 10, 2d),
					new ExpectedCellValue(sheetName, 72, 10, 856.49),
					new ExpectedCellValue(sheetName, 73, 10, 3d),
					new ExpectedCellValue(sheetName, 74, 10, null),
					new ExpectedCellValue(sheetName, 75, 10, null),
					new ExpectedCellValue(sheetName, 76, 10, 99d),
					new ExpectedCellValue(sheetName, 77, 10, 1d),
					new ExpectedCellValue(sheetName, 78, 10, null),
					new ExpectedCellValue(sheetName, 79, 10, 415.75),
					new ExpectedCellValue(sheetName, 80, 10, 1d),
					new ExpectedCellValue(sheetName, 81, 10, 514.75),
					new ExpectedCellValue(sheetName, 82, 10, 2d),
					new ExpectedCellValue(sheetName, 83, 10, 4228.24),
					new ExpectedCellValue(sheetName, 84, 10, 15d)
				});
			}
		}
		#endregion

		#region Date Grouping Tests
		// 2-Combo Months Quarters Years
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingYearsAndQuartersOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsTwoFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:B10"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsTwoFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, 2016),
					new ExpectedCellValue(sheetName, 3, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 4, 1, 2017),
					new ExpectedCellValue(sheetName, 5, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 6, 1, 2018),
					new ExpectedCellValue(sheetName, 7, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 8, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 9, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 10, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, 831.5),
					new ExpectedCellValue(sheetName, 3, 2, 831.5),
					new ExpectedCellValue(sheetName, 4, 2, 930.5),
					new ExpectedCellValue(sheetName, 5, 2, 930.5),
					new ExpectedCellValue(sheetName, 6, 2, 2466.24),
					new ExpectedCellValue(sheetName, 7, 2, 856.49),
					new ExpectedCellValue(sheetName, 8, 2, 1194d),
					new ExpectedCellValue(sheetName, 9, 2, 415.75),
					new ExpectedCellValue(sheetName, 10, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingYearsAndQuartersOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesColumnsTwoFieldSubtotalBot"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:B13"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesColumnsTwoFieldSubtotalBot";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, 2016),
					new ExpectedCellValue(sheetName, 3, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 4, 1, "2016 Total"),
					new ExpectedCellValue(sheetName, 5, 1, 2017),
					new ExpectedCellValue(sheetName, 6, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 7, 1, "2017 Total"),
					new ExpectedCellValue(sheetName, 8, 1, 2018),
					new ExpectedCellValue(sheetName, 9, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 10, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 11, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 12, 1, "2018 Total"),
					new ExpectedCellValue(sheetName, 13, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, null),
					new ExpectedCellValue(sheetName, 3, 2, 831.5),
					new ExpectedCellValue(sheetName, 4, 2, 831.5),
					new ExpectedCellValue(sheetName, 5, 2, null),
					new ExpectedCellValue(sheetName, 6, 2, 930.5),
					new ExpectedCellValue(sheetName, 7, 2, 930.5),
					new ExpectedCellValue(sheetName, 8, 2, null),
					new ExpectedCellValue(sheetName, 9, 2, 856.49),
					new ExpectedCellValue(sheetName, 10, 2, 1194d),
					new ExpectedCellValue(sheetName, 11, 2, 415.75),
					new ExpectedCellValue(sheetName, 12, 2, 2466.24),
					new ExpectedCellValue(sheetName, 13, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingQuartersAndYearsOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsTwoFields"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("E1:F10"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsTwoFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 5, "Qtr1"),
					new ExpectedCellValue(sheetName, 3, 5, 2018),
					new ExpectedCellValue(sheetName, 4, 5, "Qtr2"),
					new ExpectedCellValue(sheetName, 5, 5, 2016),
					new ExpectedCellValue(sheetName, 6, 5, 2018),
					new ExpectedCellValue(sheetName, 7, 5, "Qtr4"),
					new ExpectedCellValue(sheetName, 8, 5, 2017),
					new ExpectedCellValue(sheetName, 9, 5, 2018),
					new ExpectedCellValue(sheetName, 10, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 6, 856.49),
					new ExpectedCellValue(sheetName, 3, 6, 856.49),
					new ExpectedCellValue(sheetName, 4, 6, 2025.5),
					new ExpectedCellValue(sheetName, 5, 6, 831.5),
					new ExpectedCellValue(sheetName, 6, 6, 1194d),
					new ExpectedCellValue(sheetName, 7, 6, 1346.25),
					new ExpectedCellValue(sheetName, 8, 6, 930.5),
					new ExpectedCellValue(sheetName, 9, 6, 415.75),
					new ExpectedCellValue(sheetName, 10, 6, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingQuartersAndYearsOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesColumnsTwoFieldSubtotalBot"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("E1:F13"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesColumnsTwoFieldSubtotalBot";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 5, "Qtr1"),
					new ExpectedCellValue(sheetName, 3, 5, 2018),
					new ExpectedCellValue(sheetName, 4, 5, "Qtr1 Total"),
					new ExpectedCellValue(sheetName, 5, 5, "Qtr2"),
					new ExpectedCellValue(sheetName, 6, 5, 2016),
					new ExpectedCellValue(sheetName, 7, 5, 2018),
					new ExpectedCellValue(sheetName, 8, 5, "Qtr2 Total"),
					new ExpectedCellValue(sheetName, 9, 5, "Qtr4"),
					new ExpectedCellValue(sheetName, 10, 5, 2017),
					new ExpectedCellValue(sheetName, 11, 5, 2018),
					new ExpectedCellValue(sheetName, 12, 5, "Qtr4 Total"),
					new ExpectedCellValue(sheetName, 13, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 6, null),
					new ExpectedCellValue(sheetName, 3, 6, 856.49),
					new ExpectedCellValue(sheetName, 4, 6, 856.49),
					new ExpectedCellValue(sheetName, 5, 6, null),
					new ExpectedCellValue(sheetName, 6, 6, 831.5),
					new ExpectedCellValue(sheetName, 7, 6, 1194d),
					new ExpectedCellValue(sheetName, 8, 6, 2025.5),
					new ExpectedCellValue(sheetName, 9, 6, null),
					new ExpectedCellValue(sheetName, 10, 6, 930.5),
					new ExpectedCellValue(sheetName, 11, 6, 415.75),
					new ExpectedCellValue(sheetName, 12, 6, 1346.25),
					new ExpectedCellValue(sheetName, 13, 6, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingMonthsAndQuartersOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsTwoFields"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("I1:J14"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsTwoFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 9, "Feb"),
					new ExpectedCellValue(sheetName, 3, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 4, 9, "Mar"),
					new ExpectedCellValue(sheetName, 5, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 6, 9, "May"),
					new ExpectedCellValue(sheetName, 7, 9, "Qtr2"),
					new ExpectedCellValue(sheetName, 8, 9, "Oct"),
					new ExpectedCellValue(sheetName, 9, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 10, 9, "Nov"),
					new ExpectedCellValue(sheetName, 11, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 12, 9, "Dec"),
					new ExpectedCellValue(sheetName, 13, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 14, 9, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 10, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 10, 831.5),
					new ExpectedCellValue(sheetName, 3, 10, 831.5),
					new ExpectedCellValue(sheetName, 4, 10, 24.99),
					new ExpectedCellValue(sheetName, 5, 10, 24.99),
					new ExpectedCellValue(sheetName, 6, 10, 2025.5),
					new ExpectedCellValue(sheetName, 7, 10, 2025.5),
					new ExpectedCellValue(sheetName, 8, 10, 831.5),
					new ExpectedCellValue(sheetName, 9, 10, 831.5),
					new ExpectedCellValue(sheetName, 10, 10, 415.75),
					new ExpectedCellValue(sheetName, 11, 10, 415.75),
					new ExpectedCellValue(sheetName, 12, 10, 99d),
					new ExpectedCellValue(sheetName, 13, 10, 99d),
					new ExpectedCellValue(sheetName, 14, 10, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingMonthsAndQuartersOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesColumnsTwoFieldSubtotalBot"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("I1:J20"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesColumnsTwoFieldSubtotalBot";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 9, "Feb"),
					new ExpectedCellValue(sheetName, 3, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 4, 9, "Feb Total"),
					new ExpectedCellValue(sheetName, 5, 9, "Mar"),
					new ExpectedCellValue(sheetName, 6, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 7, 9, "Mar Total"),
					new ExpectedCellValue(sheetName, 8, 9, "May"),
					new ExpectedCellValue(sheetName, 9, 9, "Qtr2"),
					new ExpectedCellValue(sheetName, 10, 9, "May Total"),
					new ExpectedCellValue(sheetName, 11, 9, "Oct"),
					new ExpectedCellValue(sheetName, 12, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 13, 9, "Oct Total"),
					new ExpectedCellValue(sheetName, 14, 9, "Nov"),
					new ExpectedCellValue(sheetName, 15, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 16, 9, "Nov Total"),
					new ExpectedCellValue(sheetName, 17, 9, "Dec"),
					new ExpectedCellValue(sheetName, 18, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 19, 9, "Dec Total"),
					new ExpectedCellValue(sheetName, 20, 9, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 10, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 10, null),
					new ExpectedCellValue(sheetName, 3, 10, 831.5),
					new ExpectedCellValue(sheetName, 4, 10, 831.5),
					new ExpectedCellValue(sheetName, 5, 10, null),
					new ExpectedCellValue(sheetName, 6, 10, 24.99),
					new ExpectedCellValue(sheetName, 7, 10, 24.99),
					new ExpectedCellValue(sheetName, 8, 10, null),
					new ExpectedCellValue(sheetName, 9, 10, 2025.5),
					new ExpectedCellValue(sheetName, 10, 10, 2025.5),
					new ExpectedCellValue(sheetName, 11, 10, null),
					new ExpectedCellValue(sheetName, 12, 10, 831.5),
					new ExpectedCellValue(sheetName, 13, 10, 831.5),
					new ExpectedCellValue(sheetName, 14, 10, null),
					new ExpectedCellValue(sheetName, 15, 10, 415.75),
					new ExpectedCellValue(sheetName, 16, 10, 415.75),
					new ExpectedCellValue(sheetName, 17, 10, null),
					new ExpectedCellValue(sheetName, 18, 10, 99d),
					new ExpectedCellValue(sheetName, 19, 10, 99d),
					new ExpectedCellValue(sheetName, 20, 10, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingQuartersAndMonthsOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsTwoFields"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("M1:N11"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsTwoFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 13, "Qtr1"),
					new ExpectedCellValue(sheetName, 3, 13, "Feb"),
					new ExpectedCellValue(sheetName, 4, 13, "Mar"),
					new ExpectedCellValue(sheetName, 5, 13, "Qtr2"),
					new ExpectedCellValue(sheetName, 6, 13, "May"),
					new ExpectedCellValue(sheetName, 7, 13, "Qtr4"),
					new ExpectedCellValue(sheetName, 8, 13, "Oct"),
					new ExpectedCellValue(sheetName, 9, 13, "Nov"),
					new ExpectedCellValue(sheetName, 10, 13, "Dec"),
					new ExpectedCellValue(sheetName, 11, 13, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 14, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 14, 856.49),
					new ExpectedCellValue(sheetName, 3, 14, 831.5),
					new ExpectedCellValue(sheetName, 4, 14, 24.99),
					new ExpectedCellValue(sheetName, 5, 14, 2025.5),
					new ExpectedCellValue(sheetName, 6, 14, 2025.5),
					new ExpectedCellValue(sheetName, 7, 14, 1346.25),
					new ExpectedCellValue(sheetName, 8, 14, 831.5),
					new ExpectedCellValue(sheetName, 9, 14, 415.75),
					new ExpectedCellValue(sheetName, 10, 14, 99d),
					new ExpectedCellValue(sheetName, 11, 14, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingQuartersAndMonthsOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesColumnsTwoFieldSubtotalBot"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("M1:N14"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesColumnsTwoFieldSubtotalBot";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 13, "Qtr1"),
					new ExpectedCellValue(sheetName, 3, 13, "Feb"),
					new ExpectedCellValue(sheetName, 4, 13, "Mar"),
					new ExpectedCellValue(sheetName, 5, 13, "Qtr1 Total"),
					new ExpectedCellValue(sheetName, 6, 13, "Qtr2"),
					new ExpectedCellValue(sheetName, 7, 13, "May"),
					new ExpectedCellValue(sheetName, 8, 13, "Qtr2 Total"),
					new ExpectedCellValue(sheetName, 9, 13, "Qtr4"),
					new ExpectedCellValue(sheetName, 10, 13, "Oct"),
					new ExpectedCellValue(sheetName, 11, 13, "Nov"),
					new ExpectedCellValue(sheetName, 12, 13, "Dec"),
					new ExpectedCellValue(sheetName, 13, 13, "Qtr4 Total"),
					new ExpectedCellValue(sheetName, 14, 13, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 14, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 14, null),
					new ExpectedCellValue(sheetName, 3, 14, 831.5),
					new ExpectedCellValue(sheetName, 4, 14, 24.99),
					new ExpectedCellValue(sheetName, 5, 14, 856.49),
					new ExpectedCellValue(sheetName, 6, 14, null),
					new ExpectedCellValue(sheetName, 7, 14, 2025.5),
					new ExpectedCellValue(sheetName, 8, 14, 2025.5),
					new ExpectedCellValue(sheetName, 9, 14, null),
					new ExpectedCellValue(sheetName, 10, 14, 831.5),
					new ExpectedCellValue(sheetName, 11, 14, 415.75),
					new ExpectedCellValue(sheetName, 12, 14, 99d),
					new ExpectedCellValue(sheetName, 13, 14, 1346.25),
					new ExpectedCellValue(sheetName, 14, 14, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingMonthsAndYearsOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsTwoFields"];
					var pivotTable = worksheet.PivotTables["PivotTable5"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("Q1:R15"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsTwoFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 17, "Feb"),
					new ExpectedCellValue(sheetName, 3, 17, 2018),
					new ExpectedCellValue(sheetName, 4, 17, "Mar"),
					new ExpectedCellValue(sheetName, 5, 17, 2018),
					new ExpectedCellValue(sheetName, 6, 17, "May"),
					new ExpectedCellValue(sheetName, 7, 17, 2016),
					new ExpectedCellValue(sheetName, 8, 17, 2018),
					new ExpectedCellValue(sheetName, 9, 17, "Oct"),
					new ExpectedCellValue(sheetName, 10, 17, 2017),
					new ExpectedCellValue(sheetName, 11, 17, "Nov"),
					new ExpectedCellValue(sheetName, 12, 17, 2018),
					new ExpectedCellValue(sheetName, 13, 17, "Dec"),
					new ExpectedCellValue(sheetName, 14, 17, 2017),
					new ExpectedCellValue(sheetName, 15, 17, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 18, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 18, 831.5),
					new ExpectedCellValue(sheetName, 3, 18, 831.5),
					new ExpectedCellValue(sheetName, 4, 18, 24.99),
					new ExpectedCellValue(sheetName, 5, 18, 24.99),
					new ExpectedCellValue(sheetName, 6, 18, 2025.5),
					new ExpectedCellValue(sheetName, 7, 18, 831.5),
					new ExpectedCellValue(sheetName, 8, 18, 1194d),
					new ExpectedCellValue(sheetName, 9, 18, 831.5),
					new ExpectedCellValue(sheetName, 10, 18, 831.5),
					new ExpectedCellValue(sheetName, 11, 18, 415.75),
					new ExpectedCellValue(sheetName, 12, 18, 415.75),
					new ExpectedCellValue(sheetName, 13, 18, 99d),
					new ExpectedCellValue(sheetName, 14, 18, 99d),
					new ExpectedCellValue(sheetName, 15, 18, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingMonthsAndYearsOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesColumnsTwoFieldSubtotalBot"];
					var pivotTable = worksheet.PivotTables["PivotTable5"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("Q1:R21"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesColumnsTwoFieldSubtotalBot";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 17, "Feb"),
					new ExpectedCellValue(sheetName, 3, 17, 2018),
					new ExpectedCellValue(sheetName, 4, 17, "Feb Total"),
					new ExpectedCellValue(sheetName, 5, 17, "Mar"),
					new ExpectedCellValue(sheetName, 6, 17, 2018),
					new ExpectedCellValue(sheetName, 7, 17, "Mar Total"),
					new ExpectedCellValue(sheetName, 8, 17, "May"),
					new ExpectedCellValue(sheetName, 9, 17, 2016),
					new ExpectedCellValue(sheetName, 10, 17, 2018),
					new ExpectedCellValue(sheetName, 11, 17, "May Total"),
					new ExpectedCellValue(sheetName, 12, 17, "Oct"),
					new ExpectedCellValue(sheetName, 13, 17, 2017),
					new ExpectedCellValue(sheetName, 14, 17, "Oct Total"),
					new ExpectedCellValue(sheetName, 15, 17, "Nov"),
					new ExpectedCellValue(sheetName, 16, 17, 2018),
					new ExpectedCellValue(sheetName, 17, 17, "Nov Total"),
					new ExpectedCellValue(sheetName, 18, 17, "Dec"),
					new ExpectedCellValue(sheetName, 19, 17, 2017),
					new ExpectedCellValue(sheetName, 20, 17, "Dec Total"),
					new ExpectedCellValue(sheetName, 21, 17, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 18, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 18, null),
					new ExpectedCellValue(sheetName, 3, 18, 831.5),
					new ExpectedCellValue(sheetName, 4, 18, 831.5),
					new ExpectedCellValue(sheetName, 5, 18, null),
					new ExpectedCellValue(sheetName, 6, 18, 24.99),
					new ExpectedCellValue(sheetName, 7, 18, 24.99),
					new ExpectedCellValue(sheetName, 8, 18, null),
					new ExpectedCellValue(sheetName, 9, 18, 831.5),
					new ExpectedCellValue(sheetName, 10, 18, 1194d),
					new ExpectedCellValue(sheetName, 11, 18, 2025.5),
					new ExpectedCellValue(sheetName, 12, 18, null),
					new ExpectedCellValue(sheetName, 13, 18, 831.5),
					new ExpectedCellValue(sheetName, 14, 18, 831.5),
					new ExpectedCellValue(sheetName, 15, 18, null),
					new ExpectedCellValue(sheetName, 16, 18, 415.75),
					new ExpectedCellValue(sheetName, 17, 18, 415.75),
					new ExpectedCellValue(sheetName, 18, 18, null),
					new ExpectedCellValue(sheetName, 19, 18, 99d),
					new ExpectedCellValue(sheetName, 20, 18, 99d),
					new ExpectedCellValue(sheetName, 21, 18, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingYearsAndMonthsOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsTwoFields"];
					var pivotTable = worksheet.PivotTables["PivotTable6"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("U1:V12"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsTwoFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 21, 2016),
					new ExpectedCellValue(sheetName, 3, 21, "May"),
					new ExpectedCellValue(sheetName, 4, 21, 2017),
					new ExpectedCellValue(sheetName, 5, 21, "Oct"),
					new ExpectedCellValue(sheetName, 6, 21, "Dec"),
					new ExpectedCellValue(sheetName, 7, 21, 2018),
					new ExpectedCellValue(sheetName, 8, 21, "Feb"),
					new ExpectedCellValue(sheetName, 9, 21, "Mar"),
					new ExpectedCellValue(sheetName, 10, 21, "May"),
					new ExpectedCellValue(sheetName, 11, 21, "Nov"),
					new ExpectedCellValue(sheetName, 12, 21, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 22, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 22, 831.5),
					new ExpectedCellValue(sheetName, 3, 22, 831.5),
					new ExpectedCellValue(sheetName, 4, 22, 930.5),
					new ExpectedCellValue(sheetName, 5, 22, 831.5),
					new ExpectedCellValue(sheetName, 6, 22, 99d),
					new ExpectedCellValue(sheetName, 7, 22, 2466.24),
					new ExpectedCellValue(sheetName, 8, 22, 831.5),
					new ExpectedCellValue(sheetName, 9, 22, 24.99),
					new ExpectedCellValue(sheetName, 10, 22, 1194d),
					new ExpectedCellValue(sheetName, 11, 22, 415.75),
					new ExpectedCellValue(sheetName, 12, 22, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingYearsAndMonthsOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesColumnsTwoFieldSubtotalBot"];
					var pivotTable = worksheet.PivotTables["PivotTable6"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("U1:V15"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesColumnsTwoFieldSubtotalBot";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 21, 2016),
					new ExpectedCellValue(sheetName, 3, 21, "May"),
					new ExpectedCellValue(sheetName, 4, 21, "2016 Total"),
					new ExpectedCellValue(sheetName, 5, 21, 2017),
					new ExpectedCellValue(sheetName, 6, 21, "Oct"),
					new ExpectedCellValue(sheetName, 7, 21, "Dec"),
					new ExpectedCellValue(sheetName, 8, 21, "2017 Total"),
					new ExpectedCellValue(sheetName, 9, 21, 2018),
					new ExpectedCellValue(sheetName, 10, 21, "Feb"),
					new ExpectedCellValue(sheetName, 11, 21, "Mar"),
					new ExpectedCellValue(sheetName, 12, 21, "May"),
					new ExpectedCellValue(sheetName, 13, 21, "Nov"),
					new ExpectedCellValue(sheetName, 14, 21, "2018 Total"),
					new ExpectedCellValue(sheetName, 15, 21, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 22, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 22, null),
					new ExpectedCellValue(sheetName, 3, 22, 831.5),
					new ExpectedCellValue(sheetName, 4, 22, 831.5),
					new ExpectedCellValue(sheetName, 5, 22, null),
					new ExpectedCellValue(sheetName, 6, 22, 831.5),
					new ExpectedCellValue(sheetName, 7, 22, 99d),
					new ExpectedCellValue(sheetName, 8, 22, 930.5),
					new ExpectedCellValue(sheetName, 9, 22, null),
					new ExpectedCellValue(sheetName, 10, 22, 831.5),
					new ExpectedCellValue(sheetName, 11, 22, 24.99),
					new ExpectedCellValue(sheetName, 12, 22, 1194d),
					new ExpectedCellValue(sheetName, 13, 22, 415.75),
					new ExpectedCellValue(sheetName, 14, 22, 2466.24),
					new ExpectedCellValue(sheetName, 15, 22, 4228.24)
				});
			}
		}

		// 3-Combo Months Quarters Years
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingYearsQuartersAndMonthsOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsThreeFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:B17"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsThreeFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, 2016),
					new ExpectedCellValue(sheetName, 3, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 4, 1, "May"),
					new ExpectedCellValue(sheetName, 5, 1, 2017),
					new ExpectedCellValue(sheetName, 6, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 7, 1, "Oct"),
					new ExpectedCellValue(sheetName, 8, 1, "Dec"),
					new ExpectedCellValue(sheetName, 9, 1, 2018),
					new ExpectedCellValue(sheetName, 10, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 11, 1, "Feb"),
					new ExpectedCellValue(sheetName, 12, 1, "Mar"),
					new ExpectedCellValue(sheetName, 13, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 14, 1, "May"),
					new ExpectedCellValue(sheetName, 15, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 16, 1, "Nov"),
					new ExpectedCellValue(sheetName, 17, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, 831.5),
					new ExpectedCellValue(sheetName, 3, 2, 831.5),
					new ExpectedCellValue(sheetName, 4, 2, 831.5),
					new ExpectedCellValue(sheetName, 5, 2, 930.5),
					new ExpectedCellValue(sheetName, 6, 2, 930.5),
					new ExpectedCellValue(sheetName, 7, 2, 831.5),
					new ExpectedCellValue(sheetName, 8, 2, 99d),
					new ExpectedCellValue(sheetName, 9, 2, 2466.24),
					new ExpectedCellValue(sheetName, 10, 2, 856.49),
					new ExpectedCellValue(sheetName, 11, 2, 831.5),
					new ExpectedCellValue(sheetName, 12, 2, 24.99),
					new ExpectedCellValue(sheetName, 13, 2, 1194d),
					new ExpectedCellValue(sheetName, 14, 2, 1194d),
					new ExpectedCellValue(sheetName, 15, 2, 415.75),
					new ExpectedCellValue(sheetName, 16, 2, 415.75),
					new ExpectedCellValue(sheetName, 17, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingYearsQuartersAndMonthsOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesThreeFieldsSubtotalBottom"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:B25"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesThreeFieldsSubtotalBottom";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, 2016),
					new ExpectedCellValue(sheetName, 3, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 4, 1, "May"),
					new ExpectedCellValue(sheetName, 5, 1, "Qtr2 Total"),
					new ExpectedCellValue(sheetName, 6, 1, "2016 Total"),
					new ExpectedCellValue(sheetName, 7, 1, 2017),
					new ExpectedCellValue(sheetName, 8, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 9, 1, "Oct"),
					new ExpectedCellValue(sheetName, 10, 1, "Dec"),
					new ExpectedCellValue(sheetName, 11, 1, "Qtr4 Total"),
					new ExpectedCellValue(sheetName, 12, 1, "2017 Total"),
					new ExpectedCellValue(sheetName, 13, 1, 2018),
					new ExpectedCellValue(sheetName, 14, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 15, 1, "Feb"),
					new ExpectedCellValue(sheetName, 16, 1, "Mar"),
					new ExpectedCellValue(sheetName, 17, 1, "Qtr1 Total"),
					new ExpectedCellValue(sheetName, 18, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 19, 1, "May"),
					new ExpectedCellValue(sheetName, 20, 1, "Qtr2 Total"),
					new ExpectedCellValue(sheetName, 21, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 22, 1, "Nov"),
					new ExpectedCellValue(sheetName, 23, 1, "Qtr4 Total"),
					new ExpectedCellValue(sheetName, 24, 1, "2018 Total"),
					new ExpectedCellValue(sheetName, 25, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, null),
					new ExpectedCellValue(sheetName, 3, 2, null),
					new ExpectedCellValue(sheetName, 4, 2, 831.5),
					new ExpectedCellValue(sheetName, 5, 2, 831.5),
					new ExpectedCellValue(sheetName, 6, 2, 831.5),
					new ExpectedCellValue(sheetName, 7, 2, null),
					new ExpectedCellValue(sheetName, 8, 2, null),
					new ExpectedCellValue(sheetName, 9, 2, 831.5),
					new ExpectedCellValue(sheetName, 10, 2, 99d),
					new ExpectedCellValue(sheetName, 11, 2, 930.5),
					new ExpectedCellValue(sheetName, 12, 2, 930.5),
					new ExpectedCellValue(sheetName, 13, 2, null),
					new ExpectedCellValue(sheetName, 14, 2, null),
					new ExpectedCellValue(sheetName, 15, 2, 831.5),
					new ExpectedCellValue(sheetName, 16, 2, 24.99),
					new ExpectedCellValue(sheetName, 17, 2, 856.49),
					new ExpectedCellValue(sheetName, 18, 2, null),
					new ExpectedCellValue(sheetName, 19, 2, 1194d),
					new ExpectedCellValue(sheetName, 20, 2, 1194d),
					new ExpectedCellValue(sheetName, 21, 2, null),
					new ExpectedCellValue(sheetName, 22, 2, 415.75),
					new ExpectedCellValue(sheetName, 23, 2, 415.75),
					new ExpectedCellValue(sheetName, 24, 2, 2466.24),
					new ExpectedCellValue(sheetName, 25, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingQuartersYearsAndMonthsOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsThreeFields"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("E1:F17"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsThreeFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 5, "Qtr1"),
					new ExpectedCellValue(sheetName, 3, 5, 2018),
					new ExpectedCellValue(sheetName, 4, 5, "Feb"),
					new ExpectedCellValue(sheetName, 5, 5, "Mar"),
					new ExpectedCellValue(sheetName, 6, 5, "Qtr2"),
					new ExpectedCellValue(sheetName, 7, 5, 2016),
					new ExpectedCellValue(sheetName, 8, 5, "May"),
					new ExpectedCellValue(sheetName, 9, 5, 2018),
					new ExpectedCellValue(sheetName, 10, 5, "May"),
					new ExpectedCellValue(sheetName, 11, 5, "Qtr4"),
					new ExpectedCellValue(sheetName, 12, 5, 2017),
					new ExpectedCellValue(sheetName, 13, 5, "Oct"),
					new ExpectedCellValue(sheetName, 14, 5, "Dec"),
					new ExpectedCellValue(sheetName, 15, 5, 2018),
					new ExpectedCellValue(sheetName, 16, 5, "Nov"),
					new ExpectedCellValue(sheetName, 17, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 6, 856.49),
					new ExpectedCellValue(sheetName, 3, 6, 856.49),
					new ExpectedCellValue(sheetName, 4, 6, 831.5),
					new ExpectedCellValue(sheetName, 5, 6, 24.99),
					new ExpectedCellValue(sheetName, 6, 6, 2025.5),
					new ExpectedCellValue(sheetName, 7, 6, 831.5),
					new ExpectedCellValue(sheetName, 8, 6, 831.5),
					new ExpectedCellValue(sheetName, 9, 6, 1194d),
					new ExpectedCellValue(sheetName, 10, 6, 1194d),
					new ExpectedCellValue(sheetName, 11, 6, 1346.25),
					new ExpectedCellValue(sheetName, 12, 6, 930.5),
					new ExpectedCellValue(sheetName, 13, 6, 831.5),
					new ExpectedCellValue(sheetName, 14, 6, 99d),
					new ExpectedCellValue(sheetName, 15, 6, 415.75),
					new ExpectedCellValue(sheetName, 16, 6, 415.75),
					new ExpectedCellValue(sheetName, 17, 6, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingQuartersYearsAndMonthsOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesThreeFieldsSubtotalBottom"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("E1:F25"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesThreeFieldsSubtotalBottom";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 5, "Qtr1"),
					new ExpectedCellValue(sheetName, 3, 5, 2018),
					new ExpectedCellValue(sheetName, 4, 5, "Feb"),
					new ExpectedCellValue(sheetName, 5, 5, "Mar"),
					new ExpectedCellValue(sheetName, 6, 5, "2018 Total"),
					new ExpectedCellValue(sheetName, 7, 5, "Qtr1 Total"),
					new ExpectedCellValue(sheetName, 8, 5, "Qtr2"),
					new ExpectedCellValue(sheetName, 9, 5, 2016),
					new ExpectedCellValue(sheetName, 10, 5, "May"),
					new ExpectedCellValue(sheetName, 11, 5, "2016 Total"),
					new ExpectedCellValue(sheetName, 12, 5, 2018),
					new ExpectedCellValue(sheetName, 13, 5, "May"),
					new ExpectedCellValue(sheetName, 14, 5, "2018 Total"),
					new ExpectedCellValue(sheetName, 15, 5, "Qtr2 Total"),
					new ExpectedCellValue(sheetName, 16, 5, "Qtr4"),
					new ExpectedCellValue(sheetName, 17, 5, 2017),
					new ExpectedCellValue(sheetName, 18, 5, "Oct"),
					new ExpectedCellValue(sheetName, 19, 5, "Dec"),
					new ExpectedCellValue(sheetName, 20, 5, "2017 Total"),
					new ExpectedCellValue(sheetName, 21, 5, 2018),
					new ExpectedCellValue(sheetName, 22, 5, "Nov"),
					new ExpectedCellValue(sheetName, 23, 5, "2018 Total"),
					new ExpectedCellValue(sheetName, 24, 5, "Qtr4 Total"),
					new ExpectedCellValue(sheetName, 25, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 6, null),
					new ExpectedCellValue(sheetName, 3, 6, null),
					new ExpectedCellValue(sheetName, 4, 6, 831.5),
					new ExpectedCellValue(sheetName, 5, 6, 24.99),
					new ExpectedCellValue(sheetName, 6, 6, 856.49),
					new ExpectedCellValue(sheetName, 7, 6, 856.49),
					new ExpectedCellValue(sheetName, 8, 6, null),
					new ExpectedCellValue(sheetName, 9, 6, null),
					new ExpectedCellValue(sheetName, 10, 6, 831.5),
					new ExpectedCellValue(sheetName, 11, 6, 831.5),
					new ExpectedCellValue(sheetName, 12, 6, null),
					new ExpectedCellValue(sheetName, 13, 6, 1194d),
					new ExpectedCellValue(sheetName, 14, 6, 1194d),
					new ExpectedCellValue(sheetName, 15, 6, 2025.5),
					new ExpectedCellValue(sheetName, 16, 6, null),
					new ExpectedCellValue(sheetName, 17, 6, null),
					new ExpectedCellValue(sheetName, 18, 6, 831.5),
					new ExpectedCellValue(sheetName, 19, 6, 99d),
					new ExpectedCellValue(sheetName, 20, 6, 930.5),
					new ExpectedCellValue(sheetName, 21, 6, null),
					new ExpectedCellValue(sheetName, 22, 6, 415.75),
					new ExpectedCellValue(sheetName, 23, 6, 415.75),
					new ExpectedCellValue(sheetName, 24, 6, 1346.25),
					new ExpectedCellValue(sheetName, 25, 6, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingYearsMonthsAndQuartersOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsThreeFields"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("I1:J19"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsThreeFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 9, 2016),
					new ExpectedCellValue(sheetName, 3, 9, "May"),
					new ExpectedCellValue(sheetName, 4, 9, "Qtr2"),
					new ExpectedCellValue(sheetName, 5, 9, 2017),
					new ExpectedCellValue(sheetName, 6, 9, "Oct"),
					new ExpectedCellValue(sheetName, 7, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 8, 9, "Dec"),
					new ExpectedCellValue(sheetName, 9, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 10, 9, 2018),
					new ExpectedCellValue(sheetName, 11, 9, "Feb"),
					new ExpectedCellValue(sheetName, 12, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 13, 9, "Mar"),
					new ExpectedCellValue(sheetName, 14, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 15, 9, "May"),
					new ExpectedCellValue(sheetName, 16, 9, "Qtr2"),
					new ExpectedCellValue(sheetName, 17, 9, "Nov"),
					new ExpectedCellValue(sheetName, 18, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 19, 9, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 10, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 10, 831.5),
					new ExpectedCellValue(sheetName, 3, 10, 831.5),
					new ExpectedCellValue(sheetName, 4, 10, 831.5),
					new ExpectedCellValue(sheetName, 5, 10, 930.5),
					new ExpectedCellValue(sheetName, 6, 10, 831.5),
					new ExpectedCellValue(sheetName, 7, 10, 831.5),
					new ExpectedCellValue(sheetName, 8, 10, 99d),
					new ExpectedCellValue(sheetName, 9, 10, 99d),
					new ExpectedCellValue(sheetName, 10, 10, 2466.24),
					new ExpectedCellValue(sheetName, 11, 10, 831.5),
					new ExpectedCellValue(sheetName, 12, 10, 831.5),
					new ExpectedCellValue(sheetName, 13, 10, 24.99),
					new ExpectedCellValue(sheetName, 14, 10, 24.99),
					new ExpectedCellValue(sheetName, 15, 10, 1194d),
					new ExpectedCellValue(sheetName, 16, 10, 1194d),
					new ExpectedCellValue(sheetName, 17, 10, 415.75),
					new ExpectedCellValue(sheetName, 18, 10, 415.75),
					new ExpectedCellValue(sheetName, 19, 10, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingYearsMonthsAndQuartersOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesThreeFieldsSubtotalBottom"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("I1:J29"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesThreeFieldsSubtotalBottom";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 9, 2016),
					new ExpectedCellValue(sheetName, 3, 9, "May"),
					new ExpectedCellValue(sheetName, 4, 9, "Qtr2"),
					new ExpectedCellValue(sheetName, 5, 9, "May Total"),
					new ExpectedCellValue(sheetName, 6, 9, "2016 Total"),
					new ExpectedCellValue(sheetName, 7, 9, 2017),
					new ExpectedCellValue(sheetName, 8, 9, "Oct"),
					new ExpectedCellValue(sheetName, 9, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 10, 9, "Oct Total"),
					new ExpectedCellValue(sheetName, 11, 9, "Dec"),
					new ExpectedCellValue(sheetName, 12, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 13, 9, "Dec Total"),
					new ExpectedCellValue(sheetName, 14, 9, "2017 Total"),
					new ExpectedCellValue(sheetName, 15, 9, 2018),
					new ExpectedCellValue(sheetName, 16, 9, "Feb"),
					new ExpectedCellValue(sheetName, 17, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 18, 9, "Feb Total"),
					new ExpectedCellValue(sheetName, 19, 9, "Mar"),
					new ExpectedCellValue(sheetName, 20, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 21, 9, "Mar Total"),
					new ExpectedCellValue(sheetName, 22, 9, "May"),
					new ExpectedCellValue(sheetName, 23, 9, "Qtr2"),
					new ExpectedCellValue(sheetName, 24, 9, "May Total"),
					new ExpectedCellValue(sheetName, 25, 9, "Nov"),
					new ExpectedCellValue(sheetName, 26, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 27, 9, "Nov Total"),
					new ExpectedCellValue(sheetName, 28, 9, "2018 Total"),
					new ExpectedCellValue(sheetName, 29, 9, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 10, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 10, null),
					new ExpectedCellValue(sheetName, 3, 10, null),
					new ExpectedCellValue(sheetName, 4, 10, 831.5),
					new ExpectedCellValue(sheetName, 5, 10, 831.5),
					new ExpectedCellValue(sheetName, 6, 10, 831.5),
					new ExpectedCellValue(sheetName, 7, 10, null),
					new ExpectedCellValue(sheetName, 8, 10, null),
					new ExpectedCellValue(sheetName, 9, 10, 831.5),
					new ExpectedCellValue(sheetName, 10, 10, 831.5),
					new ExpectedCellValue(sheetName, 11, 10, null),
					new ExpectedCellValue(sheetName, 12, 10, 99d),
					new ExpectedCellValue(sheetName, 13, 10, 99d),
					new ExpectedCellValue(sheetName, 14, 10, 930.5),
					new ExpectedCellValue(sheetName, 15, 10, null),
					new ExpectedCellValue(sheetName, 16, 10, null),
					new ExpectedCellValue(sheetName, 17, 10, 831.5),
					new ExpectedCellValue(sheetName, 18, 10, 831.5),
					new ExpectedCellValue(sheetName, 19, 10, null),
					new ExpectedCellValue(sheetName, 20, 10, 24.99),
					new ExpectedCellValue(sheetName, 21, 10, 24.99),
					new ExpectedCellValue(sheetName, 22, 10, null),
					new ExpectedCellValue(sheetName, 23, 10, 1194d),
					new ExpectedCellValue(sheetName, 24, 10, 1194d),
					new ExpectedCellValue(sheetName, 25, 10, null),
					new ExpectedCellValue(sheetName, 26, 10, 415.75),
					new ExpectedCellValue(sheetName, 27, 10, 415.75),
					new ExpectedCellValue(sheetName, 28, 10, 2466.24),
					new ExpectedCellValue(sheetName, 29, 10, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingQuartersMonthsAndYearsOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsThreeFields"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("M1:N18"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsThreeFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 13, "Qtr1"),
					new ExpectedCellValue(sheetName, 3, 13, "Feb"),
					new ExpectedCellValue(sheetName, 4, 13, 2018),
					new ExpectedCellValue(sheetName, 5, 13, "Mar"),
					new ExpectedCellValue(sheetName, 6, 13, 2018),
					new ExpectedCellValue(sheetName, 7, 13, "Qtr2"),
					new ExpectedCellValue(sheetName, 8, 13, "May"),
					new ExpectedCellValue(sheetName, 9, 13, 2016),
					new ExpectedCellValue(sheetName, 10, 13, 2018),
					new ExpectedCellValue(sheetName, 11, 13, "Qtr4"),
					new ExpectedCellValue(sheetName, 12, 13, "Oct"),
					new ExpectedCellValue(sheetName, 13, 13, 2017),
					new ExpectedCellValue(sheetName, 14, 13, "Nov"),
					new ExpectedCellValue(sheetName, 15, 13, 2018),
					new ExpectedCellValue(sheetName, 16, 13, "Dec"),
					new ExpectedCellValue(sheetName, 17, 13, 2017),
					new ExpectedCellValue(sheetName, 18, 13, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 14, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 14, 856.49),
					new ExpectedCellValue(sheetName, 3, 14, 831.5),
					new ExpectedCellValue(sheetName, 4, 14, 831.5),
					new ExpectedCellValue(sheetName, 5, 14, 24.99),
					new ExpectedCellValue(sheetName, 6, 14, 24.99),
					new ExpectedCellValue(sheetName, 7, 14, 2025.5),
					new ExpectedCellValue(sheetName, 8, 14, 2025.5),
					new ExpectedCellValue(sheetName, 9, 14, 831.5),
					new ExpectedCellValue(sheetName, 10, 14, 1194d),
					new ExpectedCellValue(sheetName, 11, 14, 1346.25),
					new ExpectedCellValue(sheetName, 12, 14, 831.5),
					new ExpectedCellValue(sheetName, 13, 14, 831.5),
					new ExpectedCellValue(sheetName, 14, 14, 415.75),
					new ExpectedCellValue(sheetName, 15, 14, 415.75),
					new ExpectedCellValue(sheetName, 16, 14, 99d),
					new ExpectedCellValue(sheetName, 17, 14, 99d),
					new ExpectedCellValue(sheetName, 18, 14, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingQuartersMonthsAndYearsOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesThreeFieldsSubtotalBottom"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("M1:N27"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesThreeFieldsSubtotalBottom";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 13, "Qtr1"),
					new ExpectedCellValue(sheetName, 3, 13, "Feb"),
					new ExpectedCellValue(sheetName, 4, 13, 2018),
					new ExpectedCellValue(sheetName, 5, 13, "Feb Total"),
					new ExpectedCellValue(sheetName, 6, 13, "Mar"),
					new ExpectedCellValue(sheetName, 7, 13, 2018),
					new ExpectedCellValue(sheetName, 8, 13, "Mar Total"),
					new ExpectedCellValue(sheetName, 9, 13, "Qtr1 Total"),
					new ExpectedCellValue(sheetName, 10, 13, "Qtr2"),
					new ExpectedCellValue(sheetName, 11, 13, "May"),
					new ExpectedCellValue(sheetName, 12, 13, 2016),
					new ExpectedCellValue(sheetName, 13, 13, 2018),
					new ExpectedCellValue(sheetName, 14, 13, "May Total"),
					new ExpectedCellValue(sheetName, 15, 13, "Qtr2 Total"),
					new ExpectedCellValue(sheetName, 16, 13, "Qtr4"),
					new ExpectedCellValue(sheetName, 17, 13, "Oct"),
					new ExpectedCellValue(sheetName, 18, 13, 2017),
					new ExpectedCellValue(sheetName, 19, 13, "Oct Total"),
					new ExpectedCellValue(sheetName, 20, 13, "Nov"),
					new ExpectedCellValue(sheetName, 21, 13, 2018),
					new ExpectedCellValue(sheetName, 22, 13, "Nov Total"),
					new ExpectedCellValue(sheetName, 23, 13, "Dec"),
					new ExpectedCellValue(sheetName, 24, 13, 2017),
					new ExpectedCellValue(sheetName, 25, 13, "Dec Total"),
					new ExpectedCellValue(sheetName, 26, 13, "Qtr4 Total"),
					new ExpectedCellValue(sheetName, 27, 13, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 14, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 14, null),
					new ExpectedCellValue(sheetName, 3, 14, null),
					new ExpectedCellValue(sheetName, 4, 14, 831.5),
					new ExpectedCellValue(sheetName, 5, 14, 831.5),
					new ExpectedCellValue(sheetName, 6, 14, null),
					new ExpectedCellValue(sheetName, 7, 14, 24.99),
					new ExpectedCellValue(sheetName, 8, 14, 24.99),
					new ExpectedCellValue(sheetName, 9, 14, 856.49),
					new ExpectedCellValue(sheetName, 10, 14, null),
					new ExpectedCellValue(sheetName, 11, 14, null),
					new ExpectedCellValue(sheetName, 12, 14, 831.5),
					new ExpectedCellValue(sheetName, 13, 14, 1194d),
					new ExpectedCellValue(sheetName, 14, 14, 2025.5),
					new ExpectedCellValue(sheetName, 15, 14, 2025.5),
					new ExpectedCellValue(sheetName, 16, 14, null),
					new ExpectedCellValue(sheetName, 17, 14, null),
					new ExpectedCellValue(sheetName, 18, 14, 831.5),
					new ExpectedCellValue(sheetName, 19, 14, 831.5),
					new ExpectedCellValue(sheetName, 20, 14, null),
					new ExpectedCellValue(sheetName, 21, 14, 415.75),
					new ExpectedCellValue(sheetName, 22, 14, 415.75),
					new ExpectedCellValue(sheetName, 23, 14, null),
					new ExpectedCellValue(sheetName, 24, 14, 99d),
					new ExpectedCellValue(sheetName, 25, 14, 99d),
					new ExpectedCellValue(sheetName, 26, 14, 1346.25),
					new ExpectedCellValue(sheetName, 27, 14, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingMonthsYearsAndQuartersOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsThreeFields"];
					var pivotTable = worksheet.PivotTables["PivotTable5"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("Q1:R22"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsThreeFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 17, "Feb"),
					new ExpectedCellValue(sheetName, 3, 17, 2018),
					new ExpectedCellValue(sheetName, 4, 17, "Qtr1"),
					new ExpectedCellValue(sheetName, 5, 17, "Mar"),
					new ExpectedCellValue(sheetName, 6, 17, 2018),
					new ExpectedCellValue(sheetName, 7, 17, "Qtr1"),
					new ExpectedCellValue(sheetName, 8, 17, "May"),
					new ExpectedCellValue(sheetName, 9, 17, 2016),
					new ExpectedCellValue(sheetName, 10, 17, "Qtr2"),
					new ExpectedCellValue(sheetName, 11, 17, 2018),
					new ExpectedCellValue(sheetName, 12, 17, "Qtr2"),
					new ExpectedCellValue(sheetName, 13, 17, "Oct"),
					new ExpectedCellValue(sheetName, 14, 17, 2017),
					new ExpectedCellValue(sheetName, 15, 17, "Qtr4"),
					new ExpectedCellValue(sheetName, 16, 17, "Nov"),
					new ExpectedCellValue(sheetName, 17, 17, 2018),
					new ExpectedCellValue(sheetName, 18, 17, "Qtr4"),
					new ExpectedCellValue(sheetName, 19, 17, "Dec"),
					new ExpectedCellValue(sheetName, 20, 17, 2017),
					new ExpectedCellValue(sheetName, 21, 17, "Qtr4"),
					new ExpectedCellValue(sheetName, 22, 17, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 18, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 18, 831.5),
					new ExpectedCellValue(sheetName, 3, 18, 831.5),
					new ExpectedCellValue(sheetName, 4, 18, 831.5),
					new ExpectedCellValue(sheetName, 5, 18, 24.99),
					new ExpectedCellValue(sheetName, 6, 18, 24.99),
					new ExpectedCellValue(sheetName, 7, 18, 24.99),
					new ExpectedCellValue(sheetName, 8, 18, 2025.5),
					new ExpectedCellValue(sheetName, 9, 18, 831.5),
					new ExpectedCellValue(sheetName, 10, 18, 831.5),
					new ExpectedCellValue(sheetName, 11, 18, 1194d),
					new ExpectedCellValue(sheetName, 12, 18, 1194d),
					new ExpectedCellValue(sheetName, 13, 18, 831.5),
					new ExpectedCellValue(sheetName, 14, 18, 831.5),
					new ExpectedCellValue(sheetName, 15, 18, 831.5),
					new ExpectedCellValue(sheetName, 16, 18, 415.75),
					new ExpectedCellValue(sheetName, 17, 18, 415.75),
					new ExpectedCellValue(sheetName, 18, 18, 415.75),
					new ExpectedCellValue(sheetName, 19, 18, 99d),
					new ExpectedCellValue(sheetName, 20, 18, 99d),
					new ExpectedCellValue(sheetName, 21, 18, 99d),
					new ExpectedCellValue(sheetName, 22, 18, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingMonthsYearsAndQuartersOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesThreeFieldsSubtotalBottom"];
					var pivotTable = worksheet.PivotTables["PivotTable5"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("Q1:R35"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesThreeFieldsSubtotalBottom";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 17, "Feb"),
					new ExpectedCellValue(sheetName, 3, 17, 2018),
					new ExpectedCellValue(sheetName, 4, 17, "Qtr1"),
					new ExpectedCellValue(sheetName, 5, 17, "2018 Total"),
					new ExpectedCellValue(sheetName, 6, 17, "Feb Total"),
					new ExpectedCellValue(sheetName, 7, 17, "Mar"),
					new ExpectedCellValue(sheetName, 8, 17, 2018),
					new ExpectedCellValue(sheetName, 9, 17, "Qtr1"),
					new ExpectedCellValue(sheetName, 10, 17, "2018 Total"),
					new ExpectedCellValue(sheetName, 11, 17, "Mar Total"),
					new ExpectedCellValue(sheetName, 12, 17, "May"),
					new ExpectedCellValue(sheetName, 13, 17, 2016),
					new ExpectedCellValue(sheetName, 14, 17, "Qtr2"),
					new ExpectedCellValue(sheetName, 15, 17, "2016 Total"),
					new ExpectedCellValue(sheetName, 16, 17, 2018),
					new ExpectedCellValue(sheetName, 17, 17, "Qtr2"),
					new ExpectedCellValue(sheetName, 18, 17, "2018 Total"),
					new ExpectedCellValue(sheetName, 19, 17, "May Total"),
					new ExpectedCellValue(sheetName, 20, 17, "Oct"),
					new ExpectedCellValue(sheetName, 21, 17, 2017),
					new ExpectedCellValue(sheetName, 22, 17, "Qtr4"),
					new ExpectedCellValue(sheetName, 23, 17, "2017 Total"),
					new ExpectedCellValue(sheetName, 24, 17, "Oct Total"),
					new ExpectedCellValue(sheetName, 25, 17, "Nov"),
					new ExpectedCellValue(sheetName, 26, 17, 2018),
					new ExpectedCellValue(sheetName, 27, 17, "Qtr4"),
					new ExpectedCellValue(sheetName, 28, 17, "2018 Total"),
					new ExpectedCellValue(sheetName, 29, 17, "Nov Total"),
					new ExpectedCellValue(sheetName, 30, 17, "Dec"),
					new ExpectedCellValue(sheetName, 31, 17, 2017),
					new ExpectedCellValue(sheetName, 32, 17, "Qtr4"),
					new ExpectedCellValue(sheetName, 33, 17, "2017 Total"),
					new ExpectedCellValue(sheetName, 34, 17, "Dec Total"),
					new ExpectedCellValue(sheetName, 35, 17, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 18, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 18, null),
					new ExpectedCellValue(sheetName, 3, 18, null),
					new ExpectedCellValue(sheetName, 4, 18, 831.5),
					new ExpectedCellValue(sheetName, 5, 18, 831.5),
					new ExpectedCellValue(sheetName, 6, 18, 831.5),
					new ExpectedCellValue(sheetName, 7, 18, null),
					new ExpectedCellValue(sheetName, 8, 18, null),
					new ExpectedCellValue(sheetName, 9, 18, 24.99),
					new ExpectedCellValue(sheetName, 10, 18, 24.99),
					new ExpectedCellValue(sheetName, 11, 18, 24.99),
					new ExpectedCellValue(sheetName, 12, 18, null),
					new ExpectedCellValue(sheetName, 13, 18, null),
					new ExpectedCellValue(sheetName, 14, 18, 831.5),
					new ExpectedCellValue(sheetName, 15, 18, 831.5),
					new ExpectedCellValue(sheetName, 16, 18, null),
					new ExpectedCellValue(sheetName, 17, 18, 1194d),
					new ExpectedCellValue(sheetName, 18, 18, 1194d),
					new ExpectedCellValue(sheetName, 19, 18, 2025.5),
					new ExpectedCellValue(sheetName, 20, 18, null),
					new ExpectedCellValue(sheetName, 21, 18, null),
					new ExpectedCellValue(sheetName, 22, 18, 831.5),
					new ExpectedCellValue(sheetName, 23, 18, 831.5),
					new ExpectedCellValue(sheetName, 24, 18, 831.5),
					new ExpectedCellValue(sheetName, 25, 18, null),
					new ExpectedCellValue(sheetName, 26, 18, null),
					new ExpectedCellValue(sheetName, 27, 18, 415.75),
					new ExpectedCellValue(sheetName, 28, 18, 415.75),
					new ExpectedCellValue(sheetName, 29, 18, 415.75),
					new ExpectedCellValue(sheetName, 30, 18, null),
					new ExpectedCellValue(sheetName, 31, 18, null),
					new ExpectedCellValue(sheetName, 32, 18, 99d),
					new ExpectedCellValue(sheetName, 33, 18, 99d),
					new ExpectedCellValue(sheetName, 34, 18, 99d),
					new ExpectedCellValue(sheetName, 35, 18, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingMonthsQuartersAndYearsOnlyNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesWithoutColumnsThreeFields"];
					var pivotTable = worksheet.PivotTables["PivotTable6"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("U1:V21"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesWithoutColumnsThreeFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 21, "Feb"),
					new ExpectedCellValue(sheetName, 3, 21, "Qtr1"),
					new ExpectedCellValue(sheetName, 4, 21, 2018),
					new ExpectedCellValue(sheetName, 5, 21, "Mar"),
					new ExpectedCellValue(sheetName, 6, 21, "Qtr1"),
					new ExpectedCellValue(sheetName, 7, 21, 2018),
					new ExpectedCellValue(sheetName, 8, 21, "May"),
					new ExpectedCellValue(sheetName, 9, 21, "Qtr2"),
					new ExpectedCellValue(sheetName, 10, 21, 2016),
					new ExpectedCellValue(sheetName, 11, 21, 2018),
					new ExpectedCellValue(sheetName, 12, 21, "Oct"),
					new ExpectedCellValue(sheetName, 13, 21, "Qtr4"),
					new ExpectedCellValue(sheetName, 14, 21, 2017),
					new ExpectedCellValue(sheetName, 15, 21, "Nov"),
					new ExpectedCellValue(sheetName, 16, 21, "Qtr4"),
					new ExpectedCellValue(sheetName, 17, 21, 2018),
					new ExpectedCellValue(sheetName, 18, 21, "Dec"),
					new ExpectedCellValue(sheetName, 19, 21, "Qtr4"),
					new ExpectedCellValue(sheetName, 20, 21, 2017),
					new ExpectedCellValue(sheetName, 21, 21, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 22, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 22, 831.5),
					new ExpectedCellValue(sheetName, 3, 22, 831.5),
					new ExpectedCellValue(sheetName, 4, 22, 831.5),
					new ExpectedCellValue(sheetName, 5, 22, 24.99),
					new ExpectedCellValue(sheetName, 6, 22, 24.99),
					new ExpectedCellValue(sheetName, 7, 22, 24.99),
					new ExpectedCellValue(sheetName, 8, 22, 2025.5),
					new ExpectedCellValue(sheetName, 9, 22, 2025.5),
					new ExpectedCellValue(sheetName, 10, 22, 831.5),
					new ExpectedCellValue(sheetName, 11, 22, 1194d),
					new ExpectedCellValue(sheetName, 12, 22, 831.5),
					new ExpectedCellValue(sheetName, 13, 22, 831.5),
					new ExpectedCellValue(sheetName, 14, 22, 831.5),
					new ExpectedCellValue(sheetName, 15, 22, 415.75),
					new ExpectedCellValue(sheetName, 16, 22, 415.75),
					new ExpectedCellValue(sheetName, 17, 22, 415.75),
					new ExpectedCellValue(sheetName, 18, 22, 99d),
					new ExpectedCellValue(sheetName, 19, 22, 99d),
					new ExpectedCellValue(sheetName, 20, 22, 99d),
					new ExpectedCellValue(sheetName, 21, 22, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsOnly.xlsx")]
		public void PivotTableRefreshDateGroupingMonthsQuartersAndYearsOnlyNoColumnFieldsSubtotalBottom()
		{
			var file = new FileInfo("PivotTableDateGroupingsOnly.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["DatesThreeFieldsSubtotalBottom"];
					var pivotTable = worksheet.PivotTables["PivotTable6"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("U1:V33"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "DatesThreeFieldsSubtotalBottom";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 21, "Feb"),
					new ExpectedCellValue(sheetName, 3, 21, "Qtr1"),
					new ExpectedCellValue(sheetName, 4, 21, 2018),
					new ExpectedCellValue(sheetName, 5, 21, "Qtr1 Total"),
					new ExpectedCellValue(sheetName, 6, 21, "Feb Total"),
					new ExpectedCellValue(sheetName, 7, 21, "Mar"),
					new ExpectedCellValue(sheetName, 8, 21, "Qtr1"),
					new ExpectedCellValue(sheetName, 9, 21, 2018),
					new ExpectedCellValue(sheetName, 10, 21, "Qtr1 Total"),
					new ExpectedCellValue(sheetName, 11, 21, "Mar Total"),
					new ExpectedCellValue(sheetName, 12, 21, "May"),
					new ExpectedCellValue(sheetName, 13, 21, "Qtr2"),
					new ExpectedCellValue(sheetName, 14, 21, 2016),
					new ExpectedCellValue(sheetName, 15, 21, 2018),
					new ExpectedCellValue(sheetName, 16, 21, "Qtr2 Total"),
					new ExpectedCellValue(sheetName, 17, 21, "May Total"),
					new ExpectedCellValue(sheetName, 18, 21, "Oct"),
					new ExpectedCellValue(sheetName, 19, 21, "Qtr4"),
					new ExpectedCellValue(sheetName, 20, 21, 2017),
					new ExpectedCellValue(sheetName, 21, 21, "Qtr4 Total"),
					new ExpectedCellValue(sheetName, 22, 21, "Oct Total"),
					new ExpectedCellValue(sheetName, 23, 21, "Nov"),
					new ExpectedCellValue(sheetName, 24, 21, "Qtr4"),
					new ExpectedCellValue(sheetName, 25, 21, 2018),
					new ExpectedCellValue(sheetName, 26, 21, "Qtr4 Total"),
					new ExpectedCellValue(sheetName, 27, 21, "Nov Total"),
					new ExpectedCellValue(sheetName, 28, 21, "Dec"),
					new ExpectedCellValue(sheetName, 29, 21, "Qtr4"),
					new ExpectedCellValue(sheetName, 30, 21, 2017),
					new ExpectedCellValue(sheetName, 31, 21, "Qtr4 Total"),
					new ExpectedCellValue(sheetName, 32, 21, "Dec Total"),
					new ExpectedCellValue(sheetName, 33, 21, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 22, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 22, null),
					new ExpectedCellValue(sheetName, 3, 22, null),
					new ExpectedCellValue(sheetName, 4, 22, 831.5),
					new ExpectedCellValue(sheetName, 5, 22, 831.5),
					new ExpectedCellValue(sheetName, 6, 22, 831.5),
					new ExpectedCellValue(sheetName, 7, 22, null),
					new ExpectedCellValue(sheetName, 8, 22, null),
					new ExpectedCellValue(sheetName, 9, 22, 24.99),
					new ExpectedCellValue(sheetName, 10, 22, 24.99),
					new ExpectedCellValue(sheetName, 11, 22, 24.99),
					new ExpectedCellValue(sheetName, 12, 22, null),
					new ExpectedCellValue(sheetName, 13, 22, null),
					new ExpectedCellValue(sheetName, 14, 22, 831.5),
					new ExpectedCellValue(sheetName, 15, 22, 1194d),
					new ExpectedCellValue(sheetName, 16, 22, 2025.5),
					new ExpectedCellValue(sheetName, 17, 22, 2025.5),
					new ExpectedCellValue(sheetName, 18, 22, null),
					new ExpectedCellValue(sheetName, 19, 22, null),
					new ExpectedCellValue(sheetName, 20, 22, 831.5),
					new ExpectedCellValue(sheetName, 21, 22, 831.5),
					new ExpectedCellValue(sheetName, 22, 22, 831.5),
					new ExpectedCellValue(sheetName, 23, 22, null),
					new ExpectedCellValue(sheetName, 24, 22, null),
					new ExpectedCellValue(sheetName, 25, 22, 415.75),
					new ExpectedCellValue(sheetName, 26, 22, 415.75),
					new ExpectedCellValue(sheetName, 27, 22, 415.75),
					new ExpectedCellValue(sheetName, 28, 22, null),
					new ExpectedCellValue(sheetName, 29, 22, null),
					new ExpectedCellValue(sheetName, 30, 22, 99d),
					new ExpectedCellValue(sheetName, 31, 22, 99d),
					new ExpectedCellValue(sheetName, 32, 22, 99d),
					new ExpectedCellValue(sheetName, 33, 22, 4228.24)
				});
			}
		}

		// One Non-Date Grouping With Date Groupings And No Column Fields
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsInRowFields.xlsx")]
		public void PivotTableRefreshDateGroupingOneNonDateGroupingParentNodeNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsInRowFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowFieldsDateGrouping"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:B19"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowFieldsDateGrouping";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 3, 1, "Mar"),
					new ExpectedCellValue(sheetName, 4, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 5, 1, "May"),
					new ExpectedCellValue(sheetName, 6, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 7, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 8, 1, "Feb"),
					new ExpectedCellValue(sheetName, 9, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 10, 1, "May"),
					new ExpectedCellValue(sheetName, 11, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 12, 1, "Oct"),
					new ExpectedCellValue(sheetName, 13, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 14, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 15, 1, "Nov"),
					new ExpectedCellValue(sheetName, 16, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 17, 1, "Dec"),
					new ExpectedCellValue(sheetName, 18, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 19, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, 856.49),
					new ExpectedCellValue(sheetName, 3, 2, 24.99),
					new ExpectedCellValue(sheetName, 4, 2, 24.99),
					new ExpectedCellValue(sheetName, 5, 2, 831.5),
					new ExpectedCellValue(sheetName, 6, 2, 831.5),
					new ExpectedCellValue(sheetName, 7, 2, 2857),
					new ExpectedCellValue(sheetName, 8, 2, 831.5),
					new ExpectedCellValue(sheetName, 9, 2, 831.5),
					new ExpectedCellValue(sheetName, 10, 2, 1194d),
					new ExpectedCellValue(sheetName, 11, 2, 1194d),
					new ExpectedCellValue(sheetName, 12, 2, 831.5),
					new ExpectedCellValue(sheetName, 13, 2, 831.5),
					new ExpectedCellValue(sheetName, 14, 2, 514.75),
					new ExpectedCellValue(sheetName, 15, 2, 415.75),
					new ExpectedCellValue(sheetName, 16, 2, 415.75),
					new ExpectedCellValue(sheetName, 17, 2, 99d),
					new ExpectedCellValue(sheetName, 18, 2, 99d),
					new ExpectedCellValue(sheetName, 19, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsInRowFields.xlsx")]
		public void PivotTableRefreshDateGroupingOneNonDateGroupingLeafNodeNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsInRowFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowFieldsDateGrouping"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("E1:F21"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowFieldsDateGrouping";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 5, "Feb"),
					new ExpectedCellValue(sheetName, 3, 5, "Qtr1"),
					new ExpectedCellValue(sheetName, 4, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 5, "Mar"),
					new ExpectedCellValue(sheetName, 6, 5, "Qtr1"),
					new ExpectedCellValue(sheetName, 7, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 8, 5, "May"),
					new ExpectedCellValue(sheetName, 9, 5, "Qtr2"),
					new ExpectedCellValue(sheetName, 10, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 11, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 12, 5, "Oct"),
					new ExpectedCellValue(sheetName, 13, 5, "Qtr4"),
					new ExpectedCellValue(sheetName, 14, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 15, 5, "Nov"),
					new ExpectedCellValue(sheetName, 16, 5, "Qtr4"),
					new ExpectedCellValue(sheetName, 17, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 18, 5, "Dec"),
					new ExpectedCellValue(sheetName, 19, 5, "Qtr4"),
					new ExpectedCellValue(sheetName, 20, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 21, 5, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 6, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 6, 831.5),
					new ExpectedCellValue(sheetName, 3, 6, 831.5),
					new ExpectedCellValue(sheetName, 4, 6, 831.5),
					new ExpectedCellValue(sheetName, 5, 6, 24.99),
					new ExpectedCellValue(sheetName, 6, 6, 24.99),
					new ExpectedCellValue(sheetName, 7, 6, 24.99),
					new ExpectedCellValue(sheetName, 8, 6, 2025.5),
					new ExpectedCellValue(sheetName, 9, 6, 2025.5),
					new ExpectedCellValue(sheetName, 10, 6, 831.5),
					new ExpectedCellValue(sheetName, 11, 6, 1194d),
					new ExpectedCellValue(sheetName, 12, 6, 831.5),
					new ExpectedCellValue(sheetName, 13, 6, 831.5),
					new ExpectedCellValue(sheetName, 14, 6, 831.5),
					new ExpectedCellValue(sheetName, 15, 6, 415.75),
					new ExpectedCellValue(sheetName, 16, 6, 415.75),
					new ExpectedCellValue(sheetName, 17, 6, 415.75),
					new ExpectedCellValue(sheetName, 18, 6, 99d),
					new ExpectedCellValue(sheetName, 19, 6, 99d),
					new ExpectedCellValue(sheetName, 20, 6, 99d),
					new ExpectedCellValue(sheetName, 21, 6, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsInRowFields.xlsx")]
		public void PivotTableRefreshDateGroupingTwoNonDateGroupingsNoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsInRowFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowFieldsDateGrouping"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("I1:J29"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowFieldsDateGrouping";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 9, "Feb"),
					new ExpectedCellValue(sheetName, 3, 9, "Car Rack"),
					new ExpectedCellValue(sheetName, 4, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 5, 9, "Nashville"),
					new ExpectedCellValue(sheetName, 6, 9, "Mar"),
					new ExpectedCellValue(sheetName, 7, 9, "Headlamp"),
					new ExpectedCellValue(sheetName, 8, 9, "Qtr1"),
					new ExpectedCellValue(sheetName, 9, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 10, 9, "May"),
					new ExpectedCellValue(sheetName, 11, 9, "Car Rack"),
					new ExpectedCellValue(sheetName, 12, 9, "Qtr2"),
					new ExpectedCellValue(sheetName, 13, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 14, 9, "Tent"),
					new ExpectedCellValue(sheetName, 15, 9, "Qtr2"),
					new ExpectedCellValue(sheetName, 16, 9, "Nashville"),
					new ExpectedCellValue(sheetName, 17, 9, "Oct"),
					new ExpectedCellValue(sheetName, 18, 9, "Car Rack"),
					new ExpectedCellValue(sheetName, 19, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 20, 9, "Nashville"),
					new ExpectedCellValue(sheetName, 21, 9, "Nov"),
					new ExpectedCellValue(sheetName, 22, 9, "Car Rack"),
					new ExpectedCellValue(sheetName, 23, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 24, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 25, 9, "Dec"),
					new ExpectedCellValue(sheetName, 26, 9, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 27, 9, "Qtr4"),
					new ExpectedCellValue(sheetName, 28, 9, "San Francisco"),
					new ExpectedCellValue(sheetName, 29, 9, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 10, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 10, 831.5),
					new ExpectedCellValue(sheetName, 3, 10, 831.5),
					new ExpectedCellValue(sheetName, 4, 10, null),
					new ExpectedCellValue(sheetName, 5, 10, 831.5),
					new ExpectedCellValue(sheetName, 6, 10, 24.99),
					new ExpectedCellValue(sheetName, 7, 10, 24.99),
					new ExpectedCellValue(sheetName, 8, 10, null),
					new ExpectedCellValue(sheetName, 9, 10, 24.99),
					new ExpectedCellValue(sheetName, 10, 10, 2025.5),
					new ExpectedCellValue(sheetName, 11, 10, 831.5),
					new ExpectedCellValue(sheetName, 12, 10, null),
					new ExpectedCellValue(sheetName, 13, 10, 831.5),
					new ExpectedCellValue(sheetName, 14, 10, 1194d),
					new ExpectedCellValue(sheetName, 15, 10, null),
					new ExpectedCellValue(sheetName, 16, 10, 1194d),
					new ExpectedCellValue(sheetName, 17, 10, 831.5),
					new ExpectedCellValue(sheetName, 18, 10, 831.5),
					new ExpectedCellValue(sheetName, 19, 10, null),
					new ExpectedCellValue(sheetName, 20, 10, 831.5),
					new ExpectedCellValue(sheetName, 21, 10, 415.75),
					new ExpectedCellValue(sheetName, 22, 10, 415.75),
					new ExpectedCellValue(sheetName, 23, 10, null),
					new ExpectedCellValue(sheetName, 24, 10, 415.75),
					new ExpectedCellValue(sheetName, 25, 10, 99d),
					new ExpectedCellValue(sheetName, 26, 10, 99d),
					new ExpectedCellValue(sheetName, 27, 10, null),
					new ExpectedCellValue(sheetName, 28, 10, 99d),
					new ExpectedCellValue(sheetName, 29, 10, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsInRowFields.xlsx")]
		public void PivotTableRefreshDateGroupingRowFieldDateGroupingsOneColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsInRowFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowColumnDateGroupings"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:F22"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowColumnDateGroupings";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 1, "Feb"),
					new ExpectedCellValue(sheetName, 4, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 5, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 6, 1, "Mar"),
					new ExpectedCellValue(sheetName, 7, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 8, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 9, 1, "May"),
					new ExpectedCellValue(sheetName, 10, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 11, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 12, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 13, 1, "Oct"),
					new ExpectedCellValue(sheetName, 14, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 15, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 16, 1, "Nov"),
					new ExpectedCellValue(sheetName, 17, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 18, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 19, 1, "Dec"),
					new ExpectedCellValue(sheetName, 20, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 21, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 22, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 3, 2, 831.5),
					new ExpectedCellValue(sheetName, 4, 2, 831.5),
					new ExpectedCellValue(sheetName, 5, 2, 831.5),
					new ExpectedCellValue(sheetName, 6, 2, null),
					new ExpectedCellValue(sheetName, 7, 2, null),
					new ExpectedCellValue(sheetName, 8, 2, null),
					new ExpectedCellValue(sheetName, 9, 2, 831.5),
					new ExpectedCellValue(sheetName, 10, 2, 831.5),
					new ExpectedCellValue(sheetName, 11, 2, 831.5),
					new ExpectedCellValue(sheetName, 12, 2, null),
					new ExpectedCellValue(sheetName, 13, 2, 831.5),
					new ExpectedCellValue(sheetName, 14, 2, 831.5),
					new ExpectedCellValue(sheetName, 15, 2, 831.5),
					new ExpectedCellValue(sheetName, 16, 2, 415.75),
					new ExpectedCellValue(sheetName, 17, 2, 415.75),
					new ExpectedCellValue(sheetName, 18, 2, 415.75),
					new ExpectedCellValue(sheetName, 19, 2, null),
					new ExpectedCellValue(sheetName, 20, 2, null),
					new ExpectedCellValue(sheetName, 21, 2, null),
					new ExpectedCellValue(sheetName, 22, 2, 2910.25),
					new ExpectedCellValue(sheetName, 2, 3, "Headlamp"),
					new ExpectedCellValue(sheetName, 3, 3, null),
					new ExpectedCellValue(sheetName, 4, 3, null),
					new ExpectedCellValue(sheetName, 5, 3, null),
					new ExpectedCellValue(sheetName, 6, 3, 24.99),
					new ExpectedCellValue(sheetName, 7, 3, 24.99),
					new ExpectedCellValue(sheetName, 8, 3, 24.99),
					new ExpectedCellValue(sheetName, 9, 3, null),
					new ExpectedCellValue(sheetName, 10, 3, null),
					new ExpectedCellValue(sheetName, 11, 3, null),
					new ExpectedCellValue(sheetName, 12, 3, null),
					new ExpectedCellValue(sheetName, 13, 3, null),
					new ExpectedCellValue(sheetName, 14, 3, null),
					new ExpectedCellValue(sheetName, 15, 3, null),
					new ExpectedCellValue(sheetName, 16, 3, null),
					new ExpectedCellValue(sheetName, 17, 3, null),
					new ExpectedCellValue(sheetName, 18, 3, null),
					new ExpectedCellValue(sheetName, 19, 3, null),
					new ExpectedCellValue(sheetName, 20, 3, null),
					new ExpectedCellValue(sheetName, 21, 3, null),
					new ExpectedCellValue(sheetName, 22, 3, 24.99),
					new ExpectedCellValue(sheetName, 2, 4, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 3, 4, null),
					new ExpectedCellValue(sheetName, 4, 4, null),
					new ExpectedCellValue(sheetName, 5, 4, null),
					new ExpectedCellValue(sheetName, 6, 4, null),
					new ExpectedCellValue(sheetName, 7, 4, null),
					new ExpectedCellValue(sheetName, 8, 4, null),
					new ExpectedCellValue(sheetName, 9, 4, null),
					new ExpectedCellValue(sheetName, 10, 4, null),
					new ExpectedCellValue(sheetName, 11, 4, null),
					new ExpectedCellValue(sheetName, 12, 4, null),
					new ExpectedCellValue(sheetName, 13, 4, null),
					new ExpectedCellValue(sheetName, 14, 4, null),
					new ExpectedCellValue(sheetName, 15, 4, null),
					new ExpectedCellValue(sheetName, 16, 4, null),
					new ExpectedCellValue(sheetName, 17, 4, null),
					new ExpectedCellValue(sheetName, 18, 4, null),
					new ExpectedCellValue(sheetName, 19, 4, 99d),
					new ExpectedCellValue(sheetName, 20, 4, 99d),
					new ExpectedCellValue(sheetName, 21, 4, 99d),
					new ExpectedCellValue(sheetName, 22, 4, 99d),
					new ExpectedCellValue(sheetName, 2, 5, "Tent"),
					new ExpectedCellValue(sheetName, 3, 5, null),
					new ExpectedCellValue(sheetName, 4, 5, null),
					new ExpectedCellValue(sheetName, 5, 5, null),
					new ExpectedCellValue(sheetName, 6, 5, null),
					new ExpectedCellValue(sheetName, 7, 5, null),
					new ExpectedCellValue(sheetName, 8, 5, null),
					new ExpectedCellValue(sheetName, 9, 5, 1194d),
					new ExpectedCellValue(sheetName, 10, 5, 1194d),
					new ExpectedCellValue(sheetName, 11, 5, null),
					new ExpectedCellValue(sheetName, 12, 5, 1194d),
					new ExpectedCellValue(sheetName, 13, 5, null),
					new ExpectedCellValue(sheetName, 14, 5, null),
					new ExpectedCellValue(sheetName, 15, 5, null),
					new ExpectedCellValue(sheetName, 16, 5, null),
					new ExpectedCellValue(sheetName, 17, 5, null),
					new ExpectedCellValue(sheetName, 18, 5, null),
					new ExpectedCellValue(sheetName, 19, 5, null),
					new ExpectedCellValue(sheetName, 20, 5, null),
					new ExpectedCellValue(sheetName, 21, 5, null),
					new ExpectedCellValue(sheetName, 22, 5, 1194d),
					new ExpectedCellValue(sheetName, 2, 6, "Grand Total"),
					new ExpectedCellValue(sheetName, 3, 6, 831.5),
					new ExpectedCellValue(sheetName, 4, 6, 831.5),
					new ExpectedCellValue(sheetName, 5, 6, 831.5),
					new ExpectedCellValue(sheetName, 6, 6, 24.99),
					new ExpectedCellValue(sheetName, 7, 6, 24.99),
					new ExpectedCellValue(sheetName, 8, 6, 24.99),
					new ExpectedCellValue(sheetName, 9, 6, 2025.5),
					new ExpectedCellValue(sheetName, 10, 6, 2025.5),
					new ExpectedCellValue(sheetName, 11, 6, 831.5),
					new ExpectedCellValue(sheetName, 12, 6, 1194d),
					new ExpectedCellValue(sheetName, 13, 6, 831.5),
					new ExpectedCellValue(sheetName, 14, 6, 831.5),
					new ExpectedCellValue(sheetName, 15, 6, 831.5),
					new ExpectedCellValue(sheetName, 16, 6, 415.75),
					new ExpectedCellValue(sheetName, 17, 6, 415.75),
					new ExpectedCellValue(sheetName, 18, 6, 415.75),
					new ExpectedCellValue(sheetName, 19, 6, 99d),
					new ExpectedCellValue(sheetName, 20, 6, 99d),
					new ExpectedCellValue(sheetName, 21, 6, 99d),
					new ExpectedCellValue(sheetName, 22, 6, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsInRowFields.xlsx")]
		public void PivotTableRefreshDateGroupingRowFieldDateGroupingsTwoColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsInRowFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowColumnDateGroupings"];
					var pivotTable = worksheet.PivotTables["PivotTable2"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A26:K44"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowColumnDateGroupings";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 29, 1, 2016),
					new ExpectedCellValue(sheetName, 30, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 31, 1, "May"),
					new ExpectedCellValue(sheetName, 32, 1, 2017),
					new ExpectedCellValue(sheetName, 33, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 34, 1, "Oct"),
					new ExpectedCellValue(sheetName, 35, 1, "Dec"),
					new ExpectedCellValue(sheetName, 36, 1, 2018),
					new ExpectedCellValue(sheetName, 37, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 38, 1, "Feb"),
					new ExpectedCellValue(sheetName, 39, 1, "Mar"),
					new ExpectedCellValue(sheetName, 40, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 41, 1, "May"),
					new ExpectedCellValue(sheetName, 42, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 43, 1, "Nov"),
					new ExpectedCellValue(sheetName, 44, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 27, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 28, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 29, 2, 831.5),
					new ExpectedCellValue(sheetName, 30, 2, 831.5),
					new ExpectedCellValue(sheetName, 31, 2, 831.5),
					new ExpectedCellValue(sheetName, 32, 2, null),
					new ExpectedCellValue(sheetName, 33, 2, null),
					new ExpectedCellValue(sheetName, 34, 2, null),
					new ExpectedCellValue(sheetName, 35, 2, null),
					new ExpectedCellValue(sheetName, 36, 2, null),
					new ExpectedCellValue(sheetName, 37, 2, null),
					new ExpectedCellValue(sheetName, 38, 2, null),
					new ExpectedCellValue(sheetName, 39, 2, null),
					new ExpectedCellValue(sheetName, 40, 2, null),
					new ExpectedCellValue(sheetName, 41, 2, null),
					new ExpectedCellValue(sheetName, 42, 2, null),
					new ExpectedCellValue(sheetName, 43, 2, null),
					new ExpectedCellValue(sheetName, 44, 2, 831.5),
					new ExpectedCellValue(sheetName, 28, 3, "Headlamp"),
					new ExpectedCellValue(sheetName, 29, 3, null),
					new ExpectedCellValue(sheetName, 30, 3, null),
					new ExpectedCellValue(sheetName, 31, 3, null),
					new ExpectedCellValue(sheetName, 32, 3, null),
					new ExpectedCellValue(sheetName, 33, 3, null),
					new ExpectedCellValue(sheetName, 34, 3, null),
					new ExpectedCellValue(sheetName, 35, 3, null),
					new ExpectedCellValue(sheetName, 36, 3, 24.99),
					new ExpectedCellValue(sheetName, 37, 3, 24.99),
					new ExpectedCellValue(sheetName, 38, 3, null),
					new ExpectedCellValue(sheetName, 39, 3, 24.99),
					new ExpectedCellValue(sheetName, 40, 3, null),
					new ExpectedCellValue(sheetName, 41, 3, null),
					new ExpectedCellValue(sheetName, 42, 3, null),
					new ExpectedCellValue(sheetName, 43, 3, null),
					new ExpectedCellValue(sheetName, 44, 3, 24.99),
					new ExpectedCellValue(sheetName, 27, 4, "Chicago Total"),
					new ExpectedCellValue(sheetName, 28, 4, null),
					new ExpectedCellValue(sheetName, 29, 4, 831.5),
					new ExpectedCellValue(sheetName, 30, 4, 831.5),
					new ExpectedCellValue(sheetName, 31, 4, 831.5),
					new ExpectedCellValue(sheetName, 32, 4, null),
					new ExpectedCellValue(sheetName, 33, 4, null),
					new ExpectedCellValue(sheetName, 34, 4, null),
					new ExpectedCellValue(sheetName, 35, 4, null),
					new ExpectedCellValue(sheetName, 36, 4, 24.99),
					new ExpectedCellValue(sheetName, 37, 4, 24.99),
					new ExpectedCellValue(sheetName, 38, 4, null),
					new ExpectedCellValue(sheetName, 39, 4, 24.99),
					new ExpectedCellValue(sheetName, 40, 4, null),
					new ExpectedCellValue(sheetName, 41, 4, null),
					new ExpectedCellValue(sheetName, 42, 4, null),
					new ExpectedCellValue(sheetName, 43, 4, null),
					new ExpectedCellValue(sheetName, 44, 4, 856.49),
					new ExpectedCellValue(sheetName, 27, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 28, 5, "Car Rack"),
					new ExpectedCellValue(sheetName, 29, 5, null),
					new ExpectedCellValue(sheetName, 30, 5, null),
					new ExpectedCellValue(sheetName, 31, 5, null),
					new ExpectedCellValue(sheetName, 32, 5, 831.5),
					new ExpectedCellValue(sheetName, 33, 5, 831.5),
					new ExpectedCellValue(sheetName, 34, 5, 831.5),
					new ExpectedCellValue(sheetName, 35, 5, null),
					new ExpectedCellValue(sheetName, 36, 5, 831.5),
					new ExpectedCellValue(sheetName, 37, 5, 831.5),
					new ExpectedCellValue(sheetName, 38, 5, 831.5),
					new ExpectedCellValue(sheetName, 39, 5, null),
					new ExpectedCellValue(sheetName, 40, 5, null),
					new ExpectedCellValue(sheetName, 41, 5, null),
					new ExpectedCellValue(sheetName, 42, 5, null),
					new ExpectedCellValue(sheetName, 43, 5, null),
					new ExpectedCellValue(sheetName, 44, 5, 1663d),
					new ExpectedCellValue(sheetName, 27, 6, null),
					new ExpectedCellValue(sheetName, 28, 6, "Tent"),
					new ExpectedCellValue(sheetName, 29, 6, null),
					new ExpectedCellValue(sheetName, 30, 6, null),
					new ExpectedCellValue(sheetName, 31, 6, null),
					new ExpectedCellValue(sheetName, 32, 6, null),
					new ExpectedCellValue(sheetName, 33, 6, null),
					new ExpectedCellValue(sheetName, 34, 6, null),
					new ExpectedCellValue(sheetName, 35, 6, null),
					new ExpectedCellValue(sheetName, 36, 6, 1194d),
					new ExpectedCellValue(sheetName, 37, 6, null),
					new ExpectedCellValue(sheetName, 38, 6, null),
					new ExpectedCellValue(sheetName, 39, 6, null),
					new ExpectedCellValue(sheetName, 40, 6, 1194d),
					new ExpectedCellValue(sheetName, 41, 6, 1194d),
					new ExpectedCellValue(sheetName, 42, 6, null),
					new ExpectedCellValue(sheetName, 43, 6, null),
					new ExpectedCellValue(sheetName, 44, 6, 1194d),
					new ExpectedCellValue(sheetName, 27, 7, "Nashville Total"),
					new ExpectedCellValue(sheetName, 28, 7, null),
					new ExpectedCellValue(sheetName, 29, 7, null),
					new ExpectedCellValue(sheetName, 30, 7, null),
					new ExpectedCellValue(sheetName, 31, 7, null),
					new ExpectedCellValue(sheetName, 32, 7, 831.5),
					new ExpectedCellValue(sheetName, 33, 7, 831.5),
					new ExpectedCellValue(sheetName, 34, 7, 831.5),
					new ExpectedCellValue(sheetName, 35, 7, null),
					new ExpectedCellValue(sheetName, 36, 7, 2025.5),
					new ExpectedCellValue(sheetName, 37, 7, 831.5),
					new ExpectedCellValue(sheetName, 38, 7, 831.5),
					new ExpectedCellValue(sheetName, 39, 7, null),
					new ExpectedCellValue(sheetName, 40, 7, 1194d),
					new ExpectedCellValue(sheetName, 41, 7, 1194d),
					new ExpectedCellValue(sheetName, 42, 7, null),
					new ExpectedCellValue(sheetName, 43, 7, null),
					new ExpectedCellValue(sheetName, 44, 7, 2857d),
					new ExpectedCellValue(sheetName, 27, 8, "San Francisco"),
					new ExpectedCellValue(sheetName, 28, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 29, 8, null),
					new ExpectedCellValue(sheetName, 30, 8, null),
					new ExpectedCellValue(sheetName, 31, 8, null),
					new ExpectedCellValue(sheetName, 32, 8, null),
					new ExpectedCellValue(sheetName, 33, 8, null),
					new ExpectedCellValue(sheetName, 34, 8, null),
					new ExpectedCellValue(sheetName, 35, 8, null),
					new ExpectedCellValue(sheetName, 36, 8, 415.75),
					new ExpectedCellValue(sheetName, 37, 8, null),
					new ExpectedCellValue(sheetName, 38, 8, null),
					new ExpectedCellValue(sheetName, 39, 8, null),
					new ExpectedCellValue(sheetName, 40, 8, null),
					new ExpectedCellValue(sheetName, 41, 8, null),
					new ExpectedCellValue(sheetName, 42, 8, 415.75),
					new ExpectedCellValue(sheetName, 43, 8, 415.75),
					new ExpectedCellValue(sheetName, 44, 8, 415.75),
					new ExpectedCellValue(sheetName, 27, 9, null),
					new ExpectedCellValue(sheetName, 28, 9, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 29, 9, null),
					new ExpectedCellValue(sheetName, 30, 9, null),
					new ExpectedCellValue(sheetName, 31, 9, null),
					new ExpectedCellValue(sheetName, 32, 9, 99d),
					new ExpectedCellValue(sheetName, 33, 9, 99d),
					new ExpectedCellValue(sheetName, 34, 9, null),
					new ExpectedCellValue(sheetName, 35, 9, 99d),
					new ExpectedCellValue(sheetName, 36, 9, null),
					new ExpectedCellValue(sheetName, 37, 9, null),
					new ExpectedCellValue(sheetName, 38, 9, null),
					new ExpectedCellValue(sheetName, 39, 9, null),
					new ExpectedCellValue(sheetName, 40, 9, null),
					new ExpectedCellValue(sheetName, 41, 9, null),
					new ExpectedCellValue(sheetName, 42, 9, null),
					new ExpectedCellValue(sheetName, 43, 9, null),
					new ExpectedCellValue(sheetName, 44, 9, 99d),
					new ExpectedCellValue(sheetName, 27, 10, "San Francisco Total"),
					new ExpectedCellValue(sheetName, 28, 10, null),
					new ExpectedCellValue(sheetName, 29, 10, null),
					new ExpectedCellValue(sheetName, 30, 10, null),
					new ExpectedCellValue(sheetName, 31, 10, null),
					new ExpectedCellValue(sheetName, 32, 10, 99d),
					new ExpectedCellValue(sheetName, 33, 10, 99d),
					new ExpectedCellValue(sheetName, 34, 10, null),
					new ExpectedCellValue(sheetName, 35, 10, 99d),
					new ExpectedCellValue(sheetName, 36, 10, 415.75),
					new ExpectedCellValue(sheetName, 37, 10, null),
					new ExpectedCellValue(sheetName, 38, 10, null),
					new ExpectedCellValue(sheetName, 39, 10, null),
					new ExpectedCellValue(sheetName, 40, 10, null),
					new ExpectedCellValue(sheetName, 41, 10, null),
					new ExpectedCellValue(sheetName, 42, 10, 415.75),
					new ExpectedCellValue(sheetName, 43, 10, 415.75),
					new ExpectedCellValue(sheetName, 44, 10, 514.75),
					new ExpectedCellValue(sheetName, 27, 11, "Grand Total"),
					new ExpectedCellValue(sheetName, 28, 11, null),
					new ExpectedCellValue(sheetName, 29, 11, 831.5),
					new ExpectedCellValue(sheetName, 30, 11, 831.5),
					new ExpectedCellValue(sheetName, 31, 11, 831.5),
					new ExpectedCellValue(sheetName, 32, 11, 930.5),
					new ExpectedCellValue(sheetName, 33, 11, 930.5),
					new ExpectedCellValue(sheetName, 34, 11, 831.5),
					new ExpectedCellValue(sheetName, 35, 11, 99d),
					new ExpectedCellValue(sheetName, 36, 11, 2466.24),
					new ExpectedCellValue(sheetName, 37, 11, 856.49),
					new ExpectedCellValue(sheetName, 38, 11, 831.5),
					new ExpectedCellValue(sheetName, 39, 11, 24.99),
					new ExpectedCellValue(sheetName, 40, 11, 1194d),
					new ExpectedCellValue(sheetName, 41, 11, 1194d),
					new ExpectedCellValue(sheetName, 42, 11, 415.75),
					new ExpectedCellValue(sheetName, 43, 11, 415.75),
					new ExpectedCellValue(sheetName, 44, 11, 4228.24),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsInRowFields.xlsx")]
		public void PivotTableRefreshDateGroupingRowFieldDateGroupingsTwoColumnFieldsAndTwoColumnDataFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsInRowFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowColumnDateGroupings"];
					var pivotTable = worksheet.PivotTables["PivotTable3"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A48:W67"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowColumnDateGroupings";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 52, 1, 2016),
					new ExpectedCellValue(sheetName, 53, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 54, 1, "May"),
					new ExpectedCellValue(sheetName, 55, 1, 2017),
					new ExpectedCellValue(sheetName, 56, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 57, 1, "Oct"),
					new ExpectedCellValue(sheetName, 58, 1, "Dec"),
					new ExpectedCellValue(sheetName, 59, 1, 2018),
					new ExpectedCellValue(sheetName, 60, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 61, 1, "Feb"),
					new ExpectedCellValue(sheetName, 62, 1, "Mar"),
					new ExpectedCellValue(sheetName, 63, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 64, 1, "May"),
					new ExpectedCellValue(sheetName, 65, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 66, 1, "Nov"),
					new ExpectedCellValue(sheetName, 67, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 49, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 50, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 51, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 52, 2, 415.75),
					new ExpectedCellValue(sheetName, 53, 2, 415.75),
					new ExpectedCellValue(sheetName, 54, 2, 415.75),
					new ExpectedCellValue(sheetName, 55, 2, null),
					new ExpectedCellValue(sheetName, 56, 2, null),
					new ExpectedCellValue(sheetName, 57, 2, null),
					new ExpectedCellValue(sheetName, 58, 2, null),
					new ExpectedCellValue(sheetName, 59, 2, null),
					new ExpectedCellValue(sheetName, 60, 2, null),
					new ExpectedCellValue(sheetName, 61, 2, null),
					new ExpectedCellValue(sheetName, 62, 2, null),
					new ExpectedCellValue(sheetName, 63, 2, null),
					new ExpectedCellValue(sheetName, 64, 2, null),
					new ExpectedCellValue(sheetName, 65, 2, null),
					new ExpectedCellValue(sheetName, 66, 2, null),
					new ExpectedCellValue(sheetName, 67, 2, 415.75),
					new ExpectedCellValue(sheetName, 49, 3, null),
					new ExpectedCellValue(sheetName, 50, 3, null),
					new ExpectedCellValue(sheetName, 51, 3, "Sum of Total"),
					new ExpectedCellValue(sheetName, 52, 3, 831.5),
					new ExpectedCellValue(sheetName, 53, 3, 831.5),
					new ExpectedCellValue(sheetName, 54, 3, 831.5),
					new ExpectedCellValue(sheetName, 55, 3, null),
					new ExpectedCellValue(sheetName, 56, 3, null),
					new ExpectedCellValue(sheetName, 57, 3, null),
					new ExpectedCellValue(sheetName, 58, 3, null),
					new ExpectedCellValue(sheetName, 59, 3, null),
					new ExpectedCellValue(sheetName, 60, 3, null),
					new ExpectedCellValue(sheetName, 61, 3, null),
					new ExpectedCellValue(sheetName, 62, 3, null),
					new ExpectedCellValue(sheetName, 63, 3, null),
					new ExpectedCellValue(sheetName, 64, 3, null),
					new ExpectedCellValue(sheetName, 65, 3, null),
					new ExpectedCellValue(sheetName, 66, 3, null),
					new ExpectedCellValue(sheetName, 67, 3, 831.5),
					new ExpectedCellValue(sheetName, 49, 4, null),
					new ExpectedCellValue(sheetName, 50, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 51, 4, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 52, 4, null),
					new ExpectedCellValue(sheetName, 53, 4, null),
					new ExpectedCellValue(sheetName, 54, 4, null),
					new ExpectedCellValue(sheetName, 55, 4, 415.75),
					new ExpectedCellValue(sheetName, 56, 4, 415.75),
					new ExpectedCellValue(sheetName, 57, 4, 415.75),
					new ExpectedCellValue(sheetName, 58, 4, null),
					new ExpectedCellValue(sheetName, 59, 4, 415.75),
					new ExpectedCellValue(sheetName, 60, 4, 415.75),
					new ExpectedCellValue(sheetName, 61, 4, 415.75),
					new ExpectedCellValue(sheetName, 62, 4, null),
					new ExpectedCellValue(sheetName, 63, 4, null),
					new ExpectedCellValue(sheetName, 64, 4, null),
					new ExpectedCellValue(sheetName, 65, 4, null),
					new ExpectedCellValue(sheetName, 66, 4, null),
					new ExpectedCellValue(sheetName, 67, 4, 831.5),
					new ExpectedCellValue(sheetName, 49, 5, null),
					new ExpectedCellValue(sheetName, 50, 5, null),
					new ExpectedCellValue(sheetName, 51, 5, "Sum of Total"),
					new ExpectedCellValue(sheetName, 52, 5, null),
					new ExpectedCellValue(sheetName, 53, 5, null),
					new ExpectedCellValue(sheetName, 54, 5, null),
					new ExpectedCellValue(sheetName, 55, 5, 831.5),
					new ExpectedCellValue(sheetName, 56, 5, 831.5),
					new ExpectedCellValue(sheetName, 57, 5, 831.5),
					new ExpectedCellValue(sheetName, 58, 5, null),
					new ExpectedCellValue(sheetName, 59, 5, 831.5),
					new ExpectedCellValue(sheetName, 60, 5, 831.5),
					new ExpectedCellValue(sheetName, 61, 5, 831.5),
					new ExpectedCellValue(sheetName, 62, 5, null),
					new ExpectedCellValue(sheetName, 63, 5, null),
					new ExpectedCellValue(sheetName, 64, 5, null),
					new ExpectedCellValue(sheetName, 65, 5, null),
					new ExpectedCellValue(sheetName, 66, 5, null),
					new ExpectedCellValue(sheetName, 67, 5, 1663d),
					new ExpectedCellValue(sheetName, 49, 6, null),
					new ExpectedCellValue(sheetName, 50, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 51, 6, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 52, 6, null),
					new ExpectedCellValue(sheetName, 53, 6, null),
					new ExpectedCellValue(sheetName, 54, 6, null),
					new ExpectedCellValue(sheetName, 55, 6, null),
					new ExpectedCellValue(sheetName, 56, 6, null),
					new ExpectedCellValue(sheetName, 57, 6, null),
					new ExpectedCellValue(sheetName, 58, 6, null),
					new ExpectedCellValue(sheetName, 59, 6, 415.75),
					new ExpectedCellValue(sheetName, 60, 6, null),
					new ExpectedCellValue(sheetName, 61, 6, null),
					new ExpectedCellValue(sheetName, 62, 6, null),
					new ExpectedCellValue(sheetName, 63, 6, null),
					new ExpectedCellValue(sheetName, 64, 6, null),
					new ExpectedCellValue(sheetName, 65, 6, 415.75),
					new ExpectedCellValue(sheetName, 66, 6, 415.75),
					new ExpectedCellValue(sheetName, 67, 6, 415.75),
					new ExpectedCellValue(sheetName, 49, 7, null),
					new ExpectedCellValue(sheetName, 50, 7, null),
					new ExpectedCellValue(sheetName, 51, 7, "Sum of Total"),
					new ExpectedCellValue(sheetName, 52, 7, null),
					new ExpectedCellValue(sheetName, 53, 7, null),
					new ExpectedCellValue(sheetName, 54, 7, null),
					new ExpectedCellValue(sheetName, 55, 7, null),
					new ExpectedCellValue(sheetName, 56, 7, null),
					new ExpectedCellValue(sheetName, 57, 7, null),
					new ExpectedCellValue(sheetName, 58, 7, null),
					new ExpectedCellValue(sheetName, 59, 7, 415.75),
					new ExpectedCellValue(sheetName, 60, 7, null),
					new ExpectedCellValue(sheetName, 61, 7, null),
					new ExpectedCellValue(sheetName, 62, 7, null),
					new ExpectedCellValue(sheetName, 63, 7, null),
					new ExpectedCellValue(sheetName, 64, 7, null),
					new ExpectedCellValue(sheetName, 65, 7, 415.75),
					new ExpectedCellValue(sheetName, 66, 7, 415.75),
					new ExpectedCellValue(sheetName, 67, 7, 415.75),
					new ExpectedCellValue(sheetName, 49, 8, "Car Rack Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 50, 8, null),
					new ExpectedCellValue(sheetName, 51, 8, null),
					new ExpectedCellValue(sheetName, 52, 8, 415.75),
					new ExpectedCellValue(sheetName, 53, 8, 415.75),
					new ExpectedCellValue(sheetName, 54, 8, 415.75),
					new ExpectedCellValue(sheetName, 55, 8, 415.75),
					new ExpectedCellValue(sheetName, 56, 8, 415.75),
					new ExpectedCellValue(sheetName, 57, 8, 415.75),
					new ExpectedCellValue(sheetName, 58, 8, null),
					new ExpectedCellValue(sheetName, 59, 8, 831.5),
					new ExpectedCellValue(sheetName, 60, 8, 415.75),
					new ExpectedCellValue(sheetName, 61, 8, 415.75),
					new ExpectedCellValue(sheetName, 62, 8, null),
					new ExpectedCellValue(sheetName, 63, 8, null),
					new ExpectedCellValue(sheetName, 64, 8, null),
					new ExpectedCellValue(sheetName, 65, 8, 415.75),
					new ExpectedCellValue(sheetName, 66, 8, 415.75),
					new ExpectedCellValue(sheetName, 67, 8, 1663d),
					new ExpectedCellValue(sheetName, 49, 9, "Car Rack Sum of Total"),
					new ExpectedCellValue(sheetName, 50, 9, null),
					new ExpectedCellValue(sheetName, 51, 9, null),
					new ExpectedCellValue(sheetName, 52, 9, 831.5),
					new ExpectedCellValue(sheetName, 53, 9, 831.5),
					new ExpectedCellValue(sheetName, 54, 9, 831.5),
					new ExpectedCellValue(sheetName, 55, 9, 831.5),
					new ExpectedCellValue(sheetName, 56, 9, 831.5),
					new ExpectedCellValue(sheetName, 57, 9, 831.5),
					new ExpectedCellValue(sheetName, 58, 9, null),
					new ExpectedCellValue(sheetName, 59, 9, 1247.25),
					new ExpectedCellValue(sheetName, 60, 9, 831.5),
					new ExpectedCellValue(sheetName, 61, 9, 831.5),
					new ExpectedCellValue(sheetName, 62, 9, null),
					new ExpectedCellValue(sheetName, 63, 9, null),
					new ExpectedCellValue(sheetName, 64, 9, null),
					new ExpectedCellValue(sheetName, 65, 9, 415.75),
					new ExpectedCellValue(sheetName, 66, 9, 415.75),
					new ExpectedCellValue(sheetName, 67, 9, 2910.25),
					new ExpectedCellValue(sheetName, 49, 10, "Headlamp"),
					new ExpectedCellValue(sheetName, 50, 10, "Chicago"),
					new ExpectedCellValue(sheetName, 51, 10, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 52, 10, null),
					new ExpectedCellValue(sheetName, 53, 10, null),
					new ExpectedCellValue(sheetName, 54, 10, null),
					new ExpectedCellValue(sheetName, 55, 10, null),
					new ExpectedCellValue(sheetName, 56, 10, null),
					new ExpectedCellValue(sheetName, 57, 10, null),
					new ExpectedCellValue(sheetName, 58, 10, null),
					new ExpectedCellValue(sheetName, 59, 10, 24.99),
					new ExpectedCellValue(sheetName, 60, 10, 24.99),
					new ExpectedCellValue(sheetName, 61, 10, null),
					new ExpectedCellValue(sheetName, 62, 10, 24.99),
					new ExpectedCellValue(sheetName, 63, 10, null),
					new ExpectedCellValue(sheetName, 64, 10, null),
					new ExpectedCellValue(sheetName, 65, 10, null),
					new ExpectedCellValue(sheetName, 66, 10, null),
					new ExpectedCellValue(sheetName, 67, 10, 24.99),
					new ExpectedCellValue(sheetName, 49, 11, null),
					new ExpectedCellValue(sheetName, 50, 11, null),
					new ExpectedCellValue(sheetName, 51, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 52, 11, null),
					new ExpectedCellValue(sheetName, 53, 11, null),
					new ExpectedCellValue(sheetName, 54, 11, null),
					new ExpectedCellValue(sheetName, 55, 11, null),
					new ExpectedCellValue(sheetName, 56, 11, null),
					new ExpectedCellValue(sheetName, 57, 11, null),
					new ExpectedCellValue(sheetName, 58, 11, null),
					new ExpectedCellValue(sheetName, 59, 11, 24.99),
					new ExpectedCellValue(sheetName, 60, 11, 24.99),
					new ExpectedCellValue(sheetName, 61, 11, null),
					new ExpectedCellValue(sheetName, 62, 11, 24.99),
					new ExpectedCellValue(sheetName, 63, 11, null),
					new ExpectedCellValue(sheetName, 64, 11, null),
					new ExpectedCellValue(sheetName, 65, 11, null),
					new ExpectedCellValue(sheetName, 66, 11, null),
					new ExpectedCellValue(sheetName, 67, 11, 24.99),
					new ExpectedCellValue(sheetName, 49, 12, "Headlamp Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 50, 12, null),
					new ExpectedCellValue(sheetName, 51, 12, null),
					new ExpectedCellValue(sheetName, 52, 12, null),
					new ExpectedCellValue(sheetName, 53, 12, null),
					new ExpectedCellValue(sheetName, 54, 12, null),
					new ExpectedCellValue(sheetName, 55, 12, null),
					new ExpectedCellValue(sheetName, 56, 12, null),
					new ExpectedCellValue(sheetName, 57, 12, null),
					new ExpectedCellValue(sheetName, 58, 12, null),
					new ExpectedCellValue(sheetName, 59, 12, 24.99),
					new ExpectedCellValue(sheetName, 60, 12, 24.99),
					new ExpectedCellValue(sheetName, 61, 12, null),
					new ExpectedCellValue(sheetName, 62, 12, 24.99),
					new ExpectedCellValue(sheetName, 63, 12, null),
					new ExpectedCellValue(sheetName, 64, 12, null),
					new ExpectedCellValue(sheetName, 65, 12, null),
					new ExpectedCellValue(sheetName, 66, 12, null),
					new ExpectedCellValue(sheetName, 67, 12, 24.99),
					new ExpectedCellValue(sheetName, 49, 13, "Headlamp Sum of Total"),
					new ExpectedCellValue(sheetName, 50, 13, null),
					new ExpectedCellValue(sheetName, 51, 13, null),
					new ExpectedCellValue(sheetName, 52, 13, null),
					new ExpectedCellValue(sheetName, 53, 13, null),
					new ExpectedCellValue(sheetName, 54, 13, null),
					new ExpectedCellValue(sheetName, 55, 13, null),
					new ExpectedCellValue(sheetName, 56, 13, null),
					new ExpectedCellValue(sheetName, 57, 13, null),
					new ExpectedCellValue(sheetName, 58, 13, null),
					new ExpectedCellValue(sheetName, 59, 13, 24.99),
					new ExpectedCellValue(sheetName, 60, 13, 24.99),
					new ExpectedCellValue(sheetName, 61, 13, null),
					new ExpectedCellValue(sheetName, 62, 13, 24.99),
					new ExpectedCellValue(sheetName, 63, 13, null),
					new ExpectedCellValue(sheetName, 64, 13, null),
					new ExpectedCellValue(sheetName, 65, 13, null),
					new ExpectedCellValue(sheetName, 66, 13, null),
					new ExpectedCellValue(sheetName, 67, 13, 24.99),
					new ExpectedCellValue(sheetName, 49, 14, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 50, 14, "San Francisco"),
					new ExpectedCellValue(sheetName, 51, 14, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 52, 14, null),
					new ExpectedCellValue(sheetName, 53, 14, null),
					new ExpectedCellValue(sheetName, 54, 14, null),
					new ExpectedCellValue(sheetName, 55, 14, 99d),
					new ExpectedCellValue(sheetName, 56, 14, 99d),
					new ExpectedCellValue(sheetName, 57, 14, null),
					new ExpectedCellValue(sheetName, 58, 14, 99d),
					new ExpectedCellValue(sheetName, 59, 14, null),
					new ExpectedCellValue(sheetName, 60, 14, null),
					new ExpectedCellValue(sheetName, 61, 14, null),
					new ExpectedCellValue(sheetName, 62, 14, null),
					new ExpectedCellValue(sheetName, 63, 14, null),
					new ExpectedCellValue(sheetName, 64, 14, null),
					new ExpectedCellValue(sheetName, 65, 14, null),
					new ExpectedCellValue(sheetName, 66, 14, null),
					new ExpectedCellValue(sheetName, 67, 14, 99d),
					new ExpectedCellValue(sheetName, 49, 15, null),
					new ExpectedCellValue(sheetName, 50, 15, null),
					new ExpectedCellValue(sheetName, 51, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 52, 15, null),
					new ExpectedCellValue(sheetName, 53, 15, null),
					new ExpectedCellValue(sheetName, 54, 15, null),
					new ExpectedCellValue(sheetName, 55, 15, 99d),
					new ExpectedCellValue(sheetName, 56, 15, 99d),
					new ExpectedCellValue(sheetName, 57, 15, null),
					new ExpectedCellValue(sheetName, 58, 15, 99d),
					new ExpectedCellValue(sheetName, 59, 15, null),
					new ExpectedCellValue(sheetName, 60, 15, null),
					new ExpectedCellValue(sheetName, 61, 15, null),
					new ExpectedCellValue(sheetName, 62, 15, null),
					new ExpectedCellValue(sheetName, 63, 15, null),
					new ExpectedCellValue(sheetName, 64, 15, null),
					new ExpectedCellValue(sheetName, 65, 15, null),
					new ExpectedCellValue(sheetName, 66, 15, null),
					new ExpectedCellValue(sheetName, 67, 15, 99d),
					new ExpectedCellValue(sheetName, 49, 16, "Sleeping Bag Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 50, 16, null),
					new ExpectedCellValue(sheetName, 51, 16, null),
					new ExpectedCellValue(sheetName, 52, 16, null),
					new ExpectedCellValue(sheetName, 53, 16, null),
					new ExpectedCellValue(sheetName, 54, 16, null),
					new ExpectedCellValue(sheetName, 55, 16, 99d),
					new ExpectedCellValue(sheetName, 56, 16, 99d),
					new ExpectedCellValue(sheetName, 57, 16, null),
					new ExpectedCellValue(sheetName, 58, 16, 99d),
					new ExpectedCellValue(sheetName, 59, 16, null),
					new ExpectedCellValue(sheetName, 60, 16, null),
					new ExpectedCellValue(sheetName, 61, 16, null),
					new ExpectedCellValue(sheetName, 62, 16, null),
					new ExpectedCellValue(sheetName, 63, 16, null),
					new ExpectedCellValue(sheetName, 64, 16, null),
					new ExpectedCellValue(sheetName, 65, 16, null),
					new ExpectedCellValue(sheetName, 66, 16, null),
					new ExpectedCellValue(sheetName, 67, 16, 99d),
					new ExpectedCellValue(sheetName, 49, 17, "Sleeping Bag Sum of Total"),
					new ExpectedCellValue(sheetName, 50, 17, null),
					new ExpectedCellValue(sheetName, 51, 17, null),
					new ExpectedCellValue(sheetName, 52, 17, null),
					new ExpectedCellValue(sheetName, 53, 17, null),
					new ExpectedCellValue(sheetName, 54, 17, null),
					new ExpectedCellValue(sheetName, 55, 17, 99d),
					new ExpectedCellValue(sheetName, 56, 17, 99d),
					new ExpectedCellValue(sheetName, 57, 17, null),
					new ExpectedCellValue(sheetName, 58, 17, 99d),
					new ExpectedCellValue(sheetName, 59, 17, null),
					new ExpectedCellValue(sheetName, 60, 17, null),
					new ExpectedCellValue(sheetName, 61, 17, null),
					new ExpectedCellValue(sheetName, 62, 17, null),
					new ExpectedCellValue(sheetName, 63, 17, null),
					new ExpectedCellValue(sheetName, 64, 17, null),
					new ExpectedCellValue(sheetName, 65, 17, null),
					new ExpectedCellValue(sheetName, 66, 17, null),
					new ExpectedCellValue(sheetName, 67, 17, 99d),
					new ExpectedCellValue(sheetName, 49, 18, "Tent"),
					new ExpectedCellValue(sheetName, 50, 18, "Nashville"),
					new ExpectedCellValue(sheetName, 51, 18, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 52, 18, null),
					new ExpectedCellValue(sheetName, 53, 18, null),
					new ExpectedCellValue(sheetName, 54, 18, null),
					new ExpectedCellValue(sheetName, 55, 18, null),
					new ExpectedCellValue(sheetName, 56, 18, null),
					new ExpectedCellValue(sheetName, 57, 18, null),
					new ExpectedCellValue(sheetName, 58, 18, null),
					new ExpectedCellValue(sheetName, 59, 18, 199d),
					new ExpectedCellValue(sheetName, 60, 18, null),
					new ExpectedCellValue(sheetName, 61, 18, null),
					new ExpectedCellValue(sheetName, 62, 18, null),
					new ExpectedCellValue(sheetName, 63, 18, 199d),
					new ExpectedCellValue(sheetName, 64, 18, 199d),
					new ExpectedCellValue(sheetName, 65, 18, null),
					new ExpectedCellValue(sheetName, 66, 18, null),
					new ExpectedCellValue(sheetName, 67, 18, 199d),
					new ExpectedCellValue(sheetName, 49, 19, null),
					new ExpectedCellValue(sheetName, 50, 19, null),
					new ExpectedCellValue(sheetName, 51, 19, "Sum of Total"),
					new ExpectedCellValue(sheetName, 52, 19, null),
					new ExpectedCellValue(sheetName, 53, 19, null),
					new ExpectedCellValue(sheetName, 54, 19, null),
					new ExpectedCellValue(sheetName, 55, 19, null),
					new ExpectedCellValue(sheetName, 56, 19, null),
					new ExpectedCellValue(sheetName, 57, 19, null),
					new ExpectedCellValue(sheetName, 58, 19, null),
					new ExpectedCellValue(sheetName, 59, 19, 1194d),
					new ExpectedCellValue(sheetName, 60, 19, null),
					new ExpectedCellValue(sheetName, 61, 19, null),
					new ExpectedCellValue(sheetName, 62, 19, null),
					new ExpectedCellValue(sheetName, 63, 19, 1194d),
					new ExpectedCellValue(sheetName, 64, 19, 1194d),
					new ExpectedCellValue(sheetName, 65, 19, null),
					new ExpectedCellValue(sheetName, 66, 19, null),
					new ExpectedCellValue(sheetName, 67, 19, 1194d),
					new ExpectedCellValue(sheetName, 49, 20, "Tent Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 50, 20, null),
					new ExpectedCellValue(sheetName, 51, 20, null),
					new ExpectedCellValue(sheetName, 52, 20, null),
					new ExpectedCellValue(sheetName, 53, 20, null),
					new ExpectedCellValue(sheetName, 54, 20, null),
					new ExpectedCellValue(sheetName, 55, 20, null),
					new ExpectedCellValue(sheetName, 56, 20, null),
					new ExpectedCellValue(sheetName, 57, 20, null),
					new ExpectedCellValue(sheetName, 58, 20, null),
					new ExpectedCellValue(sheetName, 59, 20, 199d),
					new ExpectedCellValue(sheetName, 60, 20, null),
					new ExpectedCellValue(sheetName, 61, 20, null),
					new ExpectedCellValue(sheetName, 62, 20, null),
					new ExpectedCellValue(sheetName, 63, 20, 199d),
					new ExpectedCellValue(sheetName, 64, 20, 199d),
					new ExpectedCellValue(sheetName, 65, 20, null),
					new ExpectedCellValue(sheetName, 66, 20, null),
					new ExpectedCellValue(sheetName, 67, 20, 199d),
					new ExpectedCellValue(sheetName, 49, 21, "Tent Sum of Total"),
					new ExpectedCellValue(sheetName, 50, 21, null),
					new ExpectedCellValue(sheetName, 51, 21, null),
					new ExpectedCellValue(sheetName, 52, 21, null),
					new ExpectedCellValue(sheetName, 53, 21, null),
					new ExpectedCellValue(sheetName, 54, 21, null),
					new ExpectedCellValue(sheetName, 55, 21, null),
					new ExpectedCellValue(sheetName, 56, 21, null),
					new ExpectedCellValue(sheetName, 57, 21, null),
					new ExpectedCellValue(sheetName, 58, 21, null),
					new ExpectedCellValue(sheetName, 59, 21, 1194d),
					new ExpectedCellValue(sheetName, 60, 21, null),
					new ExpectedCellValue(sheetName, 61, 21, null),
					new ExpectedCellValue(sheetName, 62, 21, null),
					new ExpectedCellValue(sheetName, 63, 21, 1194d),
					new ExpectedCellValue(sheetName, 64, 21, 1194d),
					new ExpectedCellValue(sheetName, 65, 21, null),
					new ExpectedCellValue(sheetName, 66, 21, null),
					new ExpectedCellValue(sheetName, 67, 21, 1194d),
					new ExpectedCellValue(sheetName, 49, 22, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 50, 22, null),
					new ExpectedCellValue(sheetName, 51, 22, null),
					new ExpectedCellValue(sheetName, 52, 22, 415.75),
					new ExpectedCellValue(sheetName, 53, 22, 415.75),
					new ExpectedCellValue(sheetName, 54, 22, 415.75),
					new ExpectedCellValue(sheetName, 55, 22, 514.75),
					new ExpectedCellValue(sheetName, 56, 22, 514.75),
					new ExpectedCellValue(sheetName, 57, 22, 415.75),
					new ExpectedCellValue(sheetName, 58, 22, 99d),
					new ExpectedCellValue(sheetName, 59, 22, 1055.49),
					new ExpectedCellValue(sheetName, 60, 22, 440.74),
					new ExpectedCellValue(sheetName, 61, 22, 415.75),
					new ExpectedCellValue(sheetName, 62, 22, 24.99),
					new ExpectedCellValue(sheetName, 63, 22, 199d),
					new ExpectedCellValue(sheetName, 64, 22, 199d),
					new ExpectedCellValue(sheetName, 65, 22, 415.75),
					new ExpectedCellValue(sheetName, 66, 22, 415.75),
					new ExpectedCellValue(sheetName, 67, 22, 1985.99),
					new ExpectedCellValue(sheetName, 49, 23, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 50, 23, null),
					new ExpectedCellValue(sheetName, 51, 23, null),
					new ExpectedCellValue(sheetName, 52, 23, 831.5),
					new ExpectedCellValue(sheetName, 53, 23, 831.5),
					new ExpectedCellValue(sheetName, 54, 23, 831.5),
					new ExpectedCellValue(sheetName, 55, 23, 930.5),
					new ExpectedCellValue(sheetName, 56, 23, 930.5),
					new ExpectedCellValue(sheetName, 57, 23, 831.5),
					new ExpectedCellValue(sheetName, 58, 23, 99d),
					new ExpectedCellValue(sheetName, 59, 23, 2466.24),
					new ExpectedCellValue(sheetName, 60, 23, 856.49),
					new ExpectedCellValue(sheetName, 61, 23, 831.5),
					new ExpectedCellValue(sheetName, 62, 23, 24.99),
					new ExpectedCellValue(sheetName, 63, 23, 1194d),
					new ExpectedCellValue(sheetName, 64, 23, 1194d),
					new ExpectedCellValue(sheetName, 65, 23, 415.75),
					new ExpectedCellValue(sheetName, 66, 23, 415.75),
					new ExpectedCellValue(sheetName, 67, 23, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingsInRowFields.xlsx")]
		public void PivotTableRefreshDateGroupingRowFieldDateGroupingsTwoColumnFieldsAndTwoRowDataFields()
		{
			var file = new FileInfo("PivotTableDateGroupingsInRowFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowColumnDateGroupings"];
					var pivotTable = worksheet.PivotTables["PivotTable4"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A71:L120"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowColumnDateGroupings";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 74, 1, 2016),
					new ExpectedCellValue(sheetName, 75, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 76, 1, "May"),
					new ExpectedCellValue(sheetName, 77, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 78, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 79, 1, "Qtr2 Sum of Total"),
					new ExpectedCellValue(sheetName, 80, 1, "Qtr2 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 81, 1, "2016 Sum of Total"),
					new ExpectedCellValue(sheetName, 82, 1, "2016 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 83, 1, 2017),
					new ExpectedCellValue(sheetName, 84, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 85, 1, "Oct"),
					new ExpectedCellValue(sheetName, 86, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 87, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 88, 1, "Dec"),
					new ExpectedCellValue(sheetName, 89, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 90, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 91, 1, "Qtr4 Sum of Total"),
					new ExpectedCellValue(sheetName, 92, 1, "Qtr4 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 93, 1, "2017 Sum of Total"),
					new ExpectedCellValue(sheetName, 94, 1, "2017 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 95, 1, 2018),
					new ExpectedCellValue(sheetName, 96, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 97, 1, "Feb"),
					new ExpectedCellValue(sheetName, 98, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 99, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 100, 1, "Mar"),
					new ExpectedCellValue(sheetName, 101, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 102, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 103, 1, "Qtr1 Sum of Total"),
					new ExpectedCellValue(sheetName, 104, 1, "Qtr1 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 105, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 106, 1, "May"),
					new ExpectedCellValue(sheetName, 107, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 108, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 109, 1, "Qtr2 Sum of Total"),
					new ExpectedCellValue(sheetName, 110, 1, "Qtr2 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 111, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 112, 1, "Nov"),
					new ExpectedCellValue(sheetName, 113, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 114, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 115, 1, "Qtr4 Sum of Total"),
					new ExpectedCellValue(sheetName, 116, 1, "Qtr4 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 117, 1, "2018 Sum of Total"),
					new ExpectedCellValue(sheetName, 118, 1, "2018 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 119, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 120, 1, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 72, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 73, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 74, 2, null),
					new ExpectedCellValue(sheetName, 75, 2, null),
					new ExpectedCellValue(sheetName, 76, 2, null),
					new ExpectedCellValue(sheetName, 77, 2, 831.5),
					new ExpectedCellValue(sheetName, 78, 2, 415.75),
					new ExpectedCellValue(sheetName, 79, 2, 831.5),
					new ExpectedCellValue(sheetName, 80, 2, 415.75),
					new ExpectedCellValue(sheetName, 81, 2, 831.5),
					new ExpectedCellValue(sheetName, 82, 2, 415.75),
					new ExpectedCellValue(sheetName, 83, 2, null),
					new ExpectedCellValue(sheetName, 84, 2, null),
					new ExpectedCellValue(sheetName, 85, 2, null),
					new ExpectedCellValue(sheetName, 86, 2, null),
					new ExpectedCellValue(sheetName, 87, 2, null),
					new ExpectedCellValue(sheetName, 88, 2, null),
					new ExpectedCellValue(sheetName, 89, 2, null),
					new ExpectedCellValue(sheetName, 90, 2, null),
					new ExpectedCellValue(sheetName, 91, 2, null),
					new ExpectedCellValue(sheetName, 92, 2, null),
					new ExpectedCellValue(sheetName, 93, 2, null),
					new ExpectedCellValue(sheetName, 94, 2, null),
					new ExpectedCellValue(sheetName, 95, 2, null),
					new ExpectedCellValue(sheetName, 96, 2, null),
					new ExpectedCellValue(sheetName, 97, 2, null),
					new ExpectedCellValue(sheetName, 98, 2, null),
					new ExpectedCellValue(sheetName, 99, 2, null),
					new ExpectedCellValue(sheetName, 100, 2, null),
					new ExpectedCellValue(sheetName, 101, 2, null),
					new ExpectedCellValue(sheetName, 102, 2, null),
					new ExpectedCellValue(sheetName, 103, 2, null),
					new ExpectedCellValue(sheetName, 104, 2, null),
					new ExpectedCellValue(sheetName, 105, 2, null),
					new ExpectedCellValue(sheetName, 106, 2, null),
					new ExpectedCellValue(sheetName, 107, 2, null),
					new ExpectedCellValue(sheetName, 108, 2, null),
					new ExpectedCellValue(sheetName, 109, 2, null),
					new ExpectedCellValue(sheetName, 110, 2, null),
					new ExpectedCellValue(sheetName, 111, 2, null),
					new ExpectedCellValue(sheetName, 112, 2, null),
					new ExpectedCellValue(sheetName, 113, 2, null),
					new ExpectedCellValue(sheetName, 114, 2, null),
					new ExpectedCellValue(sheetName, 115, 2, null),
					new ExpectedCellValue(sheetName, 116, 2, null),
					new ExpectedCellValue(sheetName, 117, 2, null),
					new ExpectedCellValue(sheetName, 118, 2, null),
					new ExpectedCellValue(sheetName, 119, 2, 831.5),
					new ExpectedCellValue(sheetName, 120, 2, 415.75),
					new ExpectedCellValue(sheetName, 72, 3, null),
					new ExpectedCellValue(sheetName, 73, 3, "Nashville"),
					new ExpectedCellValue(sheetName, 74, 3, null),
					new ExpectedCellValue(sheetName, 75, 3, null),
					new ExpectedCellValue(sheetName, 76, 3, null),
					new ExpectedCellValue(sheetName, 77, 3, null),
					new ExpectedCellValue(sheetName, 78, 3, null),
					new ExpectedCellValue(sheetName, 79, 3, null),
					new ExpectedCellValue(sheetName, 80, 3, null),
					new ExpectedCellValue(sheetName, 81, 3, null),
					new ExpectedCellValue(sheetName, 82, 3, null),
					new ExpectedCellValue(sheetName, 83, 3, null),
					new ExpectedCellValue(sheetName, 84, 3, null),
					new ExpectedCellValue(sheetName, 85, 3, null),
					new ExpectedCellValue(sheetName, 86, 3, 831.5),
					new ExpectedCellValue(sheetName, 87, 3, 415.75),
					new ExpectedCellValue(sheetName, 88, 3, null),
					new ExpectedCellValue(sheetName, 89, 3, null),
					new ExpectedCellValue(sheetName, 90, 3, null),
					new ExpectedCellValue(sheetName, 91, 3, 831.5),
					new ExpectedCellValue(sheetName, 92, 3, 415.75),
					new ExpectedCellValue(sheetName, 93, 3, 831.5),
					new ExpectedCellValue(sheetName, 94, 3, 415.75),
					new ExpectedCellValue(sheetName, 95, 3, null),
					new ExpectedCellValue(sheetName, 96, 3, null),
					new ExpectedCellValue(sheetName, 97, 3, null),
					new ExpectedCellValue(sheetName, 98, 3, 831.5),
					new ExpectedCellValue(sheetName, 99, 3, 415.75),
					new ExpectedCellValue(sheetName, 100, 3, null),
					new ExpectedCellValue(sheetName, 101, 3, null),
					new ExpectedCellValue(sheetName, 102, 3, null),
					new ExpectedCellValue(sheetName, 103, 3, 831.5),
					new ExpectedCellValue(sheetName, 104, 3, 415.75),
					new ExpectedCellValue(sheetName, 105, 3, null),
					new ExpectedCellValue(sheetName, 106, 3, null),
					new ExpectedCellValue(sheetName, 107, 3, null),
					new ExpectedCellValue(sheetName, 108, 3, null),
					new ExpectedCellValue(sheetName, 109, 3, null),
					new ExpectedCellValue(sheetName, 110, 3, null),
					new ExpectedCellValue(sheetName, 111, 3, null),
					new ExpectedCellValue(sheetName, 112, 3, null),
					new ExpectedCellValue(sheetName, 113, 3, null),
					new ExpectedCellValue(sheetName, 114, 3, null),
					new ExpectedCellValue(sheetName, 115, 3, null),
					new ExpectedCellValue(sheetName, 116, 3, null),
					new ExpectedCellValue(sheetName, 117, 3, 831.5),
					new ExpectedCellValue(sheetName, 118, 3, 415.75),
					new ExpectedCellValue(sheetName, 119, 3, 1663d),
					new ExpectedCellValue(sheetName, 120, 3, 831.5),
					new ExpectedCellValue(sheetName, 72, 4, null),
					new ExpectedCellValue(sheetName, 73, 4, "San Francisco"),
					new ExpectedCellValue(sheetName, 74, 4, null),
					new ExpectedCellValue(sheetName, 75, 4, null),
					new ExpectedCellValue(sheetName, 76, 4, null),
					new ExpectedCellValue(sheetName, 77, 4, null),
					new ExpectedCellValue(sheetName, 78, 4, null),
					new ExpectedCellValue(sheetName, 79, 4, null),
					new ExpectedCellValue(sheetName, 80, 4, null),
					new ExpectedCellValue(sheetName, 81, 4, null),
					new ExpectedCellValue(sheetName, 82, 4, null),
					new ExpectedCellValue(sheetName, 83, 4, null),
					new ExpectedCellValue(sheetName, 84, 4, null),
					new ExpectedCellValue(sheetName, 85, 4, null),
					new ExpectedCellValue(sheetName, 86, 4, null),
					new ExpectedCellValue(sheetName, 87, 4, null),
					new ExpectedCellValue(sheetName, 88, 4, null),
					new ExpectedCellValue(sheetName, 89, 4, null),
					new ExpectedCellValue(sheetName, 90, 4, null),
					new ExpectedCellValue(sheetName, 91, 4, null),
					new ExpectedCellValue(sheetName, 92, 4, null),
					new ExpectedCellValue(sheetName, 93, 4, null),
					new ExpectedCellValue(sheetName, 94, 4, null),
					new ExpectedCellValue(sheetName, 95, 4, null),
					new ExpectedCellValue(sheetName, 96, 4, null),
					new ExpectedCellValue(sheetName, 97, 4, null),
					new ExpectedCellValue(sheetName, 98, 4, null),
					new ExpectedCellValue(sheetName, 99, 4, null),
					new ExpectedCellValue(sheetName, 100, 4, null),
					new ExpectedCellValue(sheetName, 101, 4, null),
					new ExpectedCellValue(sheetName, 102, 4, null),
					new ExpectedCellValue(sheetName, 103, 4, null),
					new ExpectedCellValue(sheetName, 104, 4, null),
					new ExpectedCellValue(sheetName, 105, 4, null),
					new ExpectedCellValue(sheetName, 106, 4, null),
					new ExpectedCellValue(sheetName, 107, 4, null),
					new ExpectedCellValue(sheetName, 108, 4, null),
					new ExpectedCellValue(sheetName, 109, 4, null),
					new ExpectedCellValue(sheetName, 110, 4, null),
					new ExpectedCellValue(sheetName, 111, 4, null),
					new ExpectedCellValue(sheetName, 112, 4, null),
					new ExpectedCellValue(sheetName, 113, 4, 415.75),
					new ExpectedCellValue(sheetName, 114, 4, 415.75),
					new ExpectedCellValue(sheetName, 115, 4, 415.75),
					new ExpectedCellValue(sheetName, 116, 4, 415.75),
					new ExpectedCellValue(sheetName, 117, 4, 415.75),
					new ExpectedCellValue(sheetName, 118, 4, 415.75),
					new ExpectedCellValue(sheetName, 119, 4, 415.75),
					new ExpectedCellValue(sheetName, 120, 4, 415.75),
					new ExpectedCellValue(sheetName, 72, 5, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 73, 5, null),
					new ExpectedCellValue(sheetName, 74, 5, null),
					new ExpectedCellValue(sheetName, 75, 5, null),
					new ExpectedCellValue(sheetName, 76, 5, null),
					new ExpectedCellValue(sheetName, 77, 5, 831.5),
					new ExpectedCellValue(sheetName, 78, 5, 415.75),
					new ExpectedCellValue(sheetName, 79, 5, 831.5),
					new ExpectedCellValue(sheetName, 80, 5, 415.75),
					new ExpectedCellValue(sheetName, 81, 5, 831.5),
					new ExpectedCellValue(sheetName, 82, 5, 415.75),
					new ExpectedCellValue(sheetName, 83, 5, null),
					new ExpectedCellValue(sheetName, 84, 5, null),
					new ExpectedCellValue(sheetName, 85, 5, null),
					new ExpectedCellValue(sheetName, 86, 5, 831.5),
					new ExpectedCellValue(sheetName, 87, 5, 415.75),
					new ExpectedCellValue(sheetName, 88, 5, null),
					new ExpectedCellValue(sheetName, 89, 5, null),
					new ExpectedCellValue(sheetName, 90, 5, null),
					new ExpectedCellValue(sheetName, 91, 5, 831.5),
					new ExpectedCellValue(sheetName, 92, 5, 415.75),
					new ExpectedCellValue(sheetName, 93, 5, 831.5),
					new ExpectedCellValue(sheetName, 94, 5, 415.75),
					new ExpectedCellValue(sheetName, 95, 5, null),
					new ExpectedCellValue(sheetName, 96, 5, null),
					new ExpectedCellValue(sheetName, 97, 5, null),
					new ExpectedCellValue(sheetName, 98, 5, 831.5),
					new ExpectedCellValue(sheetName, 99, 5, 415.75),
					new ExpectedCellValue(sheetName, 100, 5, null),
					new ExpectedCellValue(sheetName, 101, 5, null),
					new ExpectedCellValue(sheetName, 102, 5, null),
					new ExpectedCellValue(sheetName, 103, 5, 831.5),
					new ExpectedCellValue(sheetName, 104, 5, 415.75),
					new ExpectedCellValue(sheetName, 105, 5, null),
					new ExpectedCellValue(sheetName, 106, 5, null),
					new ExpectedCellValue(sheetName, 107, 5, null),
					new ExpectedCellValue(sheetName, 108, 5, null),
					new ExpectedCellValue(sheetName, 109, 5, null),
					new ExpectedCellValue(sheetName, 110, 5, null),
					new ExpectedCellValue(sheetName, 111, 5, null),
					new ExpectedCellValue(sheetName, 112, 5, null),
					new ExpectedCellValue(sheetName, 113, 5, 415.75),
					new ExpectedCellValue(sheetName, 114, 5, 415.75),
					new ExpectedCellValue(sheetName, 115, 5, 415.75),
					new ExpectedCellValue(sheetName, 116, 5, 415.75),
					new ExpectedCellValue(sheetName, 117, 5, 1247.25),
					new ExpectedCellValue(sheetName, 118, 5, 831.5),
					new ExpectedCellValue(sheetName, 119, 5, 2910.25),
					new ExpectedCellValue(sheetName, 120, 5, 1663d),
					new ExpectedCellValue(sheetName, 72, 6, "Headlamp"),
					new ExpectedCellValue(sheetName, 73, 6, "Chicago"),
					new ExpectedCellValue(sheetName, 74, 6, null),
					new ExpectedCellValue(sheetName, 75, 6, null),
					new ExpectedCellValue(sheetName, 76, 6, null),
					new ExpectedCellValue(sheetName, 77, 6, null),
					new ExpectedCellValue(sheetName, 78, 6, null),
					new ExpectedCellValue(sheetName, 79, 6, null),
					new ExpectedCellValue(sheetName, 80, 6, null),
					new ExpectedCellValue(sheetName, 81, 6, null),
					new ExpectedCellValue(sheetName, 82, 6, null),
					new ExpectedCellValue(sheetName, 83, 6, null),
					new ExpectedCellValue(sheetName, 84, 6, null),
					new ExpectedCellValue(sheetName, 85, 6, null),
					new ExpectedCellValue(sheetName, 86, 6, null),
					new ExpectedCellValue(sheetName, 87, 6, null),
					new ExpectedCellValue(sheetName, 88, 6, null),
					new ExpectedCellValue(sheetName, 89, 6, null),
					new ExpectedCellValue(sheetName, 90, 6, null),
					new ExpectedCellValue(sheetName, 91, 6, null),
					new ExpectedCellValue(sheetName, 92, 6, null),
					new ExpectedCellValue(sheetName, 93, 6, null),
					new ExpectedCellValue(sheetName, 94, 6, null),
					new ExpectedCellValue(sheetName, 95, 6, null),
					new ExpectedCellValue(sheetName, 96, 6, null),
					new ExpectedCellValue(sheetName, 97, 6, null),
					new ExpectedCellValue(sheetName, 98, 6, null),
					new ExpectedCellValue(sheetName, 99, 6, null),
					new ExpectedCellValue(sheetName, 100, 6, null),
					new ExpectedCellValue(sheetName, 101, 6, 24.99),
					new ExpectedCellValue(sheetName, 102, 6, 24.99),
					new ExpectedCellValue(sheetName, 103, 6, 24.99),
					new ExpectedCellValue(sheetName, 104, 6, 24.99),
					new ExpectedCellValue(sheetName, 105, 6, null),
					new ExpectedCellValue(sheetName, 106, 6, null),
					new ExpectedCellValue(sheetName, 107, 6, null),
					new ExpectedCellValue(sheetName, 108, 6, null),
					new ExpectedCellValue(sheetName, 109, 6, null),
					new ExpectedCellValue(sheetName, 110, 6, null),
					new ExpectedCellValue(sheetName, 111, 6, null),
					new ExpectedCellValue(sheetName, 112, 6, null),
					new ExpectedCellValue(sheetName, 113, 6, null),
					new ExpectedCellValue(sheetName, 114, 6, null),
					new ExpectedCellValue(sheetName, 115, 6, null),
					new ExpectedCellValue(sheetName, 116, 6, null),
					new ExpectedCellValue(sheetName, 117, 6, 24.99),
					new ExpectedCellValue(sheetName, 118, 6, 24.99),
					new ExpectedCellValue(sheetName, 119, 6, 24.99),
					new ExpectedCellValue(sheetName, 120, 6, 24.99),
					new ExpectedCellValue(sheetName, 72, 7, "Headlamp Total"),
					new ExpectedCellValue(sheetName, 73, 7, null),
					new ExpectedCellValue(sheetName, 74, 7, null),
					new ExpectedCellValue(sheetName, 75, 7, null),
					new ExpectedCellValue(sheetName, 76, 7, null),
					new ExpectedCellValue(sheetName, 77, 7, null),
					new ExpectedCellValue(sheetName, 78, 7, null),
					new ExpectedCellValue(sheetName, 79, 7, null),
					new ExpectedCellValue(sheetName, 80, 7, null),
					new ExpectedCellValue(sheetName, 81, 7, null),
					new ExpectedCellValue(sheetName, 82, 7, null),
					new ExpectedCellValue(sheetName, 83, 7, null),
					new ExpectedCellValue(sheetName, 84, 7, null),
					new ExpectedCellValue(sheetName, 85, 7, null),
					new ExpectedCellValue(sheetName, 86, 7, null),
					new ExpectedCellValue(sheetName, 87, 7, null),
					new ExpectedCellValue(sheetName, 88, 7, null),
					new ExpectedCellValue(sheetName, 89, 7, null),
					new ExpectedCellValue(sheetName, 90, 7, null),
					new ExpectedCellValue(sheetName, 91, 7, null),
					new ExpectedCellValue(sheetName, 92, 7, null),
					new ExpectedCellValue(sheetName, 93, 7, null),
					new ExpectedCellValue(sheetName, 94, 7, null),
					new ExpectedCellValue(sheetName, 95, 7, null),
					new ExpectedCellValue(sheetName, 96, 7, null),
					new ExpectedCellValue(sheetName, 97, 7, null),
					new ExpectedCellValue(sheetName, 98, 7, null),
					new ExpectedCellValue(sheetName, 99, 7, null),
					new ExpectedCellValue(sheetName, 100, 7, null),
					new ExpectedCellValue(sheetName, 101, 7, 24.99),
					new ExpectedCellValue(sheetName, 102, 7, 24.99),
					new ExpectedCellValue(sheetName, 103, 7, 24.99),
					new ExpectedCellValue(sheetName, 104, 7, 24.99),
					new ExpectedCellValue(sheetName, 105, 7, null),
					new ExpectedCellValue(sheetName, 106, 7, null),
					new ExpectedCellValue(sheetName, 107, 7, null),
					new ExpectedCellValue(sheetName, 108, 7, null),
					new ExpectedCellValue(sheetName, 109, 7, null),
					new ExpectedCellValue(sheetName, 110, 7, null),
					new ExpectedCellValue(sheetName, 111, 7, null),
					new ExpectedCellValue(sheetName, 112, 7, null),
					new ExpectedCellValue(sheetName, 113, 7, null),
					new ExpectedCellValue(sheetName, 114, 7, null),
					new ExpectedCellValue(sheetName, 115, 7, null),
					new ExpectedCellValue(sheetName, 116, 7, null),
					new ExpectedCellValue(sheetName, 117, 7, 24.99),
					new ExpectedCellValue(sheetName, 118, 7, 24.99),
					new ExpectedCellValue(sheetName, 119, 7, 24.99),
					new ExpectedCellValue(sheetName, 120, 7, 24.99),
					new ExpectedCellValue(sheetName, 72, 8, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 73, 8, "San Francisco"),
					new ExpectedCellValue(sheetName, 74, 8, null),
					new ExpectedCellValue(sheetName, 75, 8, null),
					new ExpectedCellValue(sheetName, 76, 8, null),
					new ExpectedCellValue(sheetName, 77, 8, null),
					new ExpectedCellValue(sheetName, 78, 8, null),
					new ExpectedCellValue(sheetName, 79, 8, null),
					new ExpectedCellValue(sheetName, 80, 8, null),
					new ExpectedCellValue(sheetName, 81, 8, null),
					new ExpectedCellValue(sheetName, 82, 8, null),
					new ExpectedCellValue(sheetName, 83, 8, null),
					new ExpectedCellValue(sheetName, 84, 8, null),
					new ExpectedCellValue(sheetName, 85, 8, null),
					new ExpectedCellValue(sheetName, 86, 8, null),
					new ExpectedCellValue(sheetName, 87, 8, null),
					new ExpectedCellValue(sheetName, 88, 8, null),
					new ExpectedCellValue(sheetName, 89, 8, 99d),
					new ExpectedCellValue(sheetName, 90, 8, 99d),
					new ExpectedCellValue(sheetName, 91, 8, 99d),
					new ExpectedCellValue(sheetName, 92, 8, 99d),
					new ExpectedCellValue(sheetName, 93, 8, 99d),
					new ExpectedCellValue(sheetName, 94, 8, 99d),
					new ExpectedCellValue(sheetName, 95, 8, null),
					new ExpectedCellValue(sheetName, 96, 8, null),
					new ExpectedCellValue(sheetName, 97, 8, null),
					new ExpectedCellValue(sheetName, 98, 8, null),
					new ExpectedCellValue(sheetName, 99, 8, null),
					new ExpectedCellValue(sheetName, 100, 8, null),
					new ExpectedCellValue(sheetName, 101, 8, null),
					new ExpectedCellValue(sheetName, 102, 8, null),
					new ExpectedCellValue(sheetName, 103, 8, null),
					new ExpectedCellValue(sheetName, 104, 8, null),
					new ExpectedCellValue(sheetName, 105, 8, null),
					new ExpectedCellValue(sheetName, 106, 8, null),
					new ExpectedCellValue(sheetName, 107, 8, null),
					new ExpectedCellValue(sheetName, 108, 8, null),
					new ExpectedCellValue(sheetName, 109, 8, null),
					new ExpectedCellValue(sheetName, 110, 8, null),
					new ExpectedCellValue(sheetName, 111, 8, null),
					new ExpectedCellValue(sheetName, 112, 8, null),
					new ExpectedCellValue(sheetName, 113, 8, null),
					new ExpectedCellValue(sheetName, 114, 8, null),
					new ExpectedCellValue(sheetName, 115, 8, null),
					new ExpectedCellValue(sheetName, 116, 8, null),
					new ExpectedCellValue(sheetName, 117, 8, null),
					new ExpectedCellValue(sheetName, 118, 8, null),
					new ExpectedCellValue(sheetName, 119, 8, 99d),
					new ExpectedCellValue(sheetName, 120, 8, 99d),
					new ExpectedCellValue(sheetName, 72, 9, "Sleeping Bag Total"),
					new ExpectedCellValue(sheetName, 73, 9, null),
					new ExpectedCellValue(sheetName, 74, 9, null),
					new ExpectedCellValue(sheetName, 75, 9, null),
					new ExpectedCellValue(sheetName, 76, 9, null),
					new ExpectedCellValue(sheetName, 77, 9, null),
					new ExpectedCellValue(sheetName, 78, 9, null),
					new ExpectedCellValue(sheetName, 79, 9, null),
					new ExpectedCellValue(sheetName, 80, 9, null),
					new ExpectedCellValue(sheetName, 81, 9, null),
					new ExpectedCellValue(sheetName, 82, 9, null),
					new ExpectedCellValue(sheetName, 83, 9, null),
					new ExpectedCellValue(sheetName, 84, 9, null),
					new ExpectedCellValue(sheetName, 85, 9, null),
					new ExpectedCellValue(sheetName, 86, 9, null),
					new ExpectedCellValue(sheetName, 87, 9, null),
					new ExpectedCellValue(sheetName, 88, 9, null),
					new ExpectedCellValue(sheetName, 89, 9, 99d),
					new ExpectedCellValue(sheetName, 90, 9, 99d),
					new ExpectedCellValue(sheetName, 91, 9, 99d),
					new ExpectedCellValue(sheetName, 92, 9, 99d),
					new ExpectedCellValue(sheetName, 93, 9, 99d),
					new ExpectedCellValue(sheetName, 94, 9, 99d),
					new ExpectedCellValue(sheetName, 95, 9, null),
					new ExpectedCellValue(sheetName, 96, 9, null),
					new ExpectedCellValue(sheetName, 97, 9, null),
					new ExpectedCellValue(sheetName, 98, 9, null),
					new ExpectedCellValue(sheetName, 99, 9, null),
					new ExpectedCellValue(sheetName, 100, 9, null),
					new ExpectedCellValue(sheetName, 101, 9, null),
					new ExpectedCellValue(sheetName, 102, 9, null),
					new ExpectedCellValue(sheetName, 103, 9, null),
					new ExpectedCellValue(sheetName, 104, 9, null),
					new ExpectedCellValue(sheetName, 105, 9, null),
					new ExpectedCellValue(sheetName, 106, 9, null),
					new ExpectedCellValue(sheetName, 107, 9, null),
					new ExpectedCellValue(sheetName, 108, 9, null),
					new ExpectedCellValue(sheetName, 109, 9, null),
					new ExpectedCellValue(sheetName, 110, 9, null),
					new ExpectedCellValue(sheetName, 111, 9, null),
					new ExpectedCellValue(sheetName, 112, 9, null),
					new ExpectedCellValue(sheetName, 113, 9, null),
					new ExpectedCellValue(sheetName, 114, 9, null),
					new ExpectedCellValue(sheetName, 115, 9, null),
					new ExpectedCellValue(sheetName, 116, 9, null),
					new ExpectedCellValue(sheetName, 117, 9, null),
					new ExpectedCellValue(sheetName, 118, 9, null),
					new ExpectedCellValue(sheetName, 119, 9, 99d),
					new ExpectedCellValue(sheetName, 120, 9, 99d),
					new ExpectedCellValue(sheetName, 72, 10, "Tent"),
					new ExpectedCellValue(sheetName, 73, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 74, 10, null),
					new ExpectedCellValue(sheetName, 75, 10, null),
					new ExpectedCellValue(sheetName, 76, 10, null),
					new ExpectedCellValue(sheetName, 77, 10, null),
					new ExpectedCellValue(sheetName, 78, 10, null),
					new ExpectedCellValue(sheetName, 79, 10, null),
					new ExpectedCellValue(sheetName, 80, 10, null),
					new ExpectedCellValue(sheetName, 81, 10, null),
					new ExpectedCellValue(sheetName, 82, 10, null),
					new ExpectedCellValue(sheetName, 83, 10, null),
					new ExpectedCellValue(sheetName, 84, 10, null),
					new ExpectedCellValue(sheetName, 85, 10, null),
					new ExpectedCellValue(sheetName, 86, 10, null),
					new ExpectedCellValue(sheetName, 87, 10, null),
					new ExpectedCellValue(sheetName, 88, 10, null),
					new ExpectedCellValue(sheetName, 89, 10, null),
					new ExpectedCellValue(sheetName, 90, 10, null),
					new ExpectedCellValue(sheetName, 91, 10, null),
					new ExpectedCellValue(sheetName, 92, 10, null),
					new ExpectedCellValue(sheetName, 93, 10, null),
					new ExpectedCellValue(sheetName, 94, 10, null),
					new ExpectedCellValue(sheetName, 95, 10, null),
					new ExpectedCellValue(sheetName, 96, 10, null),
					new ExpectedCellValue(sheetName, 97, 10, null),
					new ExpectedCellValue(sheetName, 98, 10, null),
					new ExpectedCellValue(sheetName, 99, 10, null),
					new ExpectedCellValue(sheetName, 100, 10, null),
					new ExpectedCellValue(sheetName, 101, 10, null),
					new ExpectedCellValue(sheetName, 102, 10, null),
					new ExpectedCellValue(sheetName, 103, 10, null),
					new ExpectedCellValue(sheetName, 104, 10, null),
					new ExpectedCellValue(sheetName, 105, 10, null),
					new ExpectedCellValue(sheetName, 106, 10, null),
					new ExpectedCellValue(sheetName, 107, 10, 1194d),
					new ExpectedCellValue(sheetName, 108, 10, 199d),
					new ExpectedCellValue(sheetName, 109, 10, 1194d),
					new ExpectedCellValue(sheetName, 110, 10, 199d),
					new ExpectedCellValue(sheetName, 111, 10, null),
					new ExpectedCellValue(sheetName, 112, 10, null),
					new ExpectedCellValue(sheetName, 113, 10, null),
					new ExpectedCellValue(sheetName, 114, 10, null),
					new ExpectedCellValue(sheetName, 115, 10, null),
					new ExpectedCellValue(sheetName, 116, 10, null),
					new ExpectedCellValue(sheetName, 117, 10, 1194d),
					new ExpectedCellValue(sheetName, 118, 10, 199d),
					new ExpectedCellValue(sheetName, 119, 10, 1194d),
					new ExpectedCellValue(sheetName, 120, 10, 199d),
					new ExpectedCellValue(sheetName, 72, 11, "Tent Total"),
					new ExpectedCellValue(sheetName, 73, 11, null),
					new ExpectedCellValue(sheetName, 74, 11, null),
					new ExpectedCellValue(sheetName, 75, 11, null),
					new ExpectedCellValue(sheetName, 76, 11, null),
					new ExpectedCellValue(sheetName, 77, 11, null),
					new ExpectedCellValue(sheetName, 78, 11, null),
					new ExpectedCellValue(sheetName, 79, 11, null),
					new ExpectedCellValue(sheetName, 80, 11, null),
					new ExpectedCellValue(sheetName, 81, 11, null),
					new ExpectedCellValue(sheetName, 82, 11, null),
					new ExpectedCellValue(sheetName, 83, 11, null),
					new ExpectedCellValue(sheetName, 84, 11, null),
					new ExpectedCellValue(sheetName, 85, 11, null),
					new ExpectedCellValue(sheetName, 86, 11, null),
					new ExpectedCellValue(sheetName, 87, 11, null),
					new ExpectedCellValue(sheetName, 88, 11, null),
					new ExpectedCellValue(sheetName, 89, 11, null),
					new ExpectedCellValue(sheetName, 90, 11, null),
					new ExpectedCellValue(sheetName, 91, 11, null),
					new ExpectedCellValue(sheetName, 92, 11, null),
					new ExpectedCellValue(sheetName, 93, 11, null),
					new ExpectedCellValue(sheetName, 94, 11, null),
					new ExpectedCellValue(sheetName, 95, 11, null),
					new ExpectedCellValue(sheetName, 96, 11, null),
					new ExpectedCellValue(sheetName, 97, 11, null),
					new ExpectedCellValue(sheetName, 98, 11, null),
					new ExpectedCellValue(sheetName, 99, 11, null),
					new ExpectedCellValue(sheetName, 100, 11, null),
					new ExpectedCellValue(sheetName, 101, 11, null),
					new ExpectedCellValue(sheetName, 102, 11, null),
					new ExpectedCellValue(sheetName, 103, 11, null),
					new ExpectedCellValue(sheetName, 104, 11, null),
					new ExpectedCellValue(sheetName, 105, 11, null),
					new ExpectedCellValue(sheetName, 106, 11, null),
					new ExpectedCellValue(sheetName, 107, 11, 1194d),
					new ExpectedCellValue(sheetName, 108, 11, 199d),
					new ExpectedCellValue(sheetName, 109, 11, 1194d),
					new ExpectedCellValue(sheetName, 110, 11, 199d),
					new ExpectedCellValue(sheetName, 111, 11, null),
					new ExpectedCellValue(sheetName, 112, 11, null),
					new ExpectedCellValue(sheetName, 113, 11, null),
					new ExpectedCellValue(sheetName, 114, 11, null),
					new ExpectedCellValue(sheetName, 115, 11, null),
					new ExpectedCellValue(sheetName, 116, 11, null),
					new ExpectedCellValue(sheetName, 117, 11, 1194d),
					new ExpectedCellValue(sheetName, 118, 11, 199d),
					new ExpectedCellValue(sheetName, 119, 11, 1194d),
					new ExpectedCellValue(sheetName, 120, 11, 199d),
					new ExpectedCellValue(sheetName, 72, 12, "Grand Total"),
					new ExpectedCellValue(sheetName, 73, 12, null),
					new ExpectedCellValue(sheetName, 74, 12, null),
					new ExpectedCellValue(sheetName, 75, 12, null),
					new ExpectedCellValue(sheetName, 76, 12, null),
					new ExpectedCellValue(sheetName, 77, 12, 831.5),
					new ExpectedCellValue(sheetName, 78, 12, 415.75),
					new ExpectedCellValue(sheetName, 79, 12, 831.5),
					new ExpectedCellValue(sheetName, 80, 12, 415.75),
					new ExpectedCellValue(sheetName, 81, 12, 831.5),
					new ExpectedCellValue(sheetName, 82, 12, 415.75),
					new ExpectedCellValue(sheetName, 83, 12, null),
					new ExpectedCellValue(sheetName, 84, 12, null),
					new ExpectedCellValue(sheetName, 85, 12, null),
					new ExpectedCellValue(sheetName, 86, 12, 831.5),
					new ExpectedCellValue(sheetName, 87, 12, 415.75),
					new ExpectedCellValue(sheetName, 88, 12, null),
					new ExpectedCellValue(sheetName, 89, 12, 99d),
					new ExpectedCellValue(sheetName, 90, 12, 99d),
					new ExpectedCellValue(sheetName, 91, 12, 930.5),
					new ExpectedCellValue(sheetName, 92, 12, 514.75),
					new ExpectedCellValue(sheetName, 93, 12, 930.5),
					new ExpectedCellValue(sheetName, 94, 12, 514.75),
					new ExpectedCellValue(sheetName, 95, 12, null),
					new ExpectedCellValue(sheetName, 96, 12, null),
					new ExpectedCellValue(sheetName, 97, 12, null),
					new ExpectedCellValue(sheetName, 98, 12, 831.5),
					new ExpectedCellValue(sheetName, 99, 12, 415.75),
					new ExpectedCellValue(sheetName, 100, 12, null),
					new ExpectedCellValue(sheetName, 101, 12, 24.99),
					new ExpectedCellValue(sheetName, 102, 12, 24.99),
					new ExpectedCellValue(sheetName, 103, 12, 856.49),
					new ExpectedCellValue(sheetName, 104, 12, 440.74),
					new ExpectedCellValue(sheetName, 105, 12, null),
					new ExpectedCellValue(sheetName, 106, 12, null),
					new ExpectedCellValue(sheetName, 107, 12, 1194d),
					new ExpectedCellValue(sheetName, 108, 12, 199d),
					new ExpectedCellValue(sheetName, 109, 12, 1194d),
					new ExpectedCellValue(sheetName, 110, 12, 199d),
					new ExpectedCellValue(sheetName, 111, 12, null),
					new ExpectedCellValue(sheetName, 112, 12, null),
					new ExpectedCellValue(sheetName, 113, 12, 415.75),
					new ExpectedCellValue(sheetName, 114, 12, 415.75),
					new ExpectedCellValue(sheetName, 115, 12, 415.75),
					new ExpectedCellValue(sheetName, 116, 12, 415.75),
					new ExpectedCellValue(sheetName, 117, 12, 2466.24),
					new ExpectedCellValue(sheetName, 118, 12, 1055.49),
					new ExpectedCellValue(sheetName, 119, 12, 4228.24),
					new ExpectedCellValue(sheetName, 120, 12, 1985.99),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGrouping.xlsx")]
		public void PivotTableRefreshDateGroupingColumnDataFields()
		{
			var file = new FileInfo("PivotTableDateGrouping.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["ColumnDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:AE22"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "ColumnDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 5, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 6, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 7, 1, "Feb"),
					new ExpectedCellValue(sheetName, 8, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 9, 1, "May"),
					new ExpectedCellValue(sheetName, 10, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 11, 1, "Oct"),
					new ExpectedCellValue(sheetName, 12, 1, "Nov"),
					new ExpectedCellValue(sheetName, 13, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 14, 1, "Qtr1"),
					new ExpectedCellValue(sheetName, 15, 1, "Mar"),
					new ExpectedCellValue(sheetName, 16, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 17, 1, "Qtr4"),
					new ExpectedCellValue(sheetName, 18, 1, "Dec"),
					new ExpectedCellValue(sheetName, 19, 1, "Tent"),
					new ExpectedCellValue(sheetName, 20, 1, "Qtr2"),
					new ExpectedCellValue(sheetName, 21, 1, "May"),
					new ExpectedCellValue(sheetName, 22, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 3, 2, 2016),
					new ExpectedCellValue(sheetName, 4, 2, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 5, 2, 415.75),
					new ExpectedCellValue(sheetName, 6, 2, null),
					new ExpectedCellValue(sheetName, 7, 2, null),
					new ExpectedCellValue(sheetName, 8, 2, 415.75),
					new ExpectedCellValue(sheetName, 9, 2, 415.75),
					new ExpectedCellValue(sheetName, 10, 2, null),
					new ExpectedCellValue(sheetName, 11, 2, null),
					new ExpectedCellValue(sheetName, 12, 2, null),
					new ExpectedCellValue(sheetName, 13, 2, null),
					new ExpectedCellValue(sheetName, 14, 2, null),
					new ExpectedCellValue(sheetName, 15, 2, null),
					new ExpectedCellValue(sheetName, 16, 2, null),
					new ExpectedCellValue(sheetName, 17, 2, null),
					new ExpectedCellValue(sheetName, 18, 2, null),
					new ExpectedCellValue(sheetName, 19, 2, null),
					new ExpectedCellValue(sheetName, 20, 2, null),
					new ExpectedCellValue(sheetName, 21, 2, null),
					new ExpectedCellValue(sheetName, 22, 2, 415.75),
					new ExpectedCellValue(sheetName, 2, 3, null),
					new ExpectedCellValue(sheetName, 3, 3, null),
					new ExpectedCellValue(sheetName, 4, 3, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 3, 2d),
					new ExpectedCellValue(sheetName, 6, 3, null),
					new ExpectedCellValue(sheetName, 7, 3, null),
					new ExpectedCellValue(sheetName, 8, 3, 2d),
					new ExpectedCellValue(sheetName, 9, 3, 2d),
					new ExpectedCellValue(sheetName, 10, 3, null),
					new ExpectedCellValue(sheetName, 11, 3, null),
					new ExpectedCellValue(sheetName, 12, 3, null),
					new ExpectedCellValue(sheetName, 13, 3, null),
					new ExpectedCellValue(sheetName, 14, 3, null),
					new ExpectedCellValue(sheetName, 15, 3, null),
					new ExpectedCellValue(sheetName, 16, 3, null),
					new ExpectedCellValue(sheetName, 17, 3, null),
					new ExpectedCellValue(sheetName, 18, 3, null),
					new ExpectedCellValue(sheetName, 19, 3, null),
					new ExpectedCellValue(sheetName, 20, 3, null),
					new ExpectedCellValue(sheetName, 21, 3, null),
					new ExpectedCellValue(sheetName, 22, 3, 2d),
					new ExpectedCellValue(sheetName, 2, 4, null),
					new ExpectedCellValue(sheetName, 3, 4, null),
					new ExpectedCellValue(sheetName, 4, 4, "Sum of Total"),
					new ExpectedCellValue(sheetName, 5, 4, 831.5),
					new ExpectedCellValue(sheetName, 6, 4, null),
					new ExpectedCellValue(sheetName, 7, 4, null),
					new ExpectedCellValue(sheetName, 8, 4, 831.5),
					new ExpectedCellValue(sheetName, 9, 4, 831.5),
					new ExpectedCellValue(sheetName, 10, 4, null),
					new ExpectedCellValue(sheetName, 11, 4, null),
					new ExpectedCellValue(sheetName, 12, 4, null),
					new ExpectedCellValue(sheetName, 13, 4, null),
					new ExpectedCellValue(sheetName, 14, 4, null),
					new ExpectedCellValue(sheetName, 15, 4, null),
					new ExpectedCellValue(sheetName, 16, 4, null),
					new ExpectedCellValue(sheetName, 17, 4, null),
					new ExpectedCellValue(sheetName, 18, 4, null),
					new ExpectedCellValue(sheetName, 19, 4, null),
					new ExpectedCellValue(sheetName, 20, 4, null),
					new ExpectedCellValue(sheetName, 21, 4, null),
					new ExpectedCellValue(sheetName, 22, 4, 831.5),
					new ExpectedCellValue(sheetName, 2, 5, null),
					new ExpectedCellValue(sheetName, 3, 5, 2018),
					new ExpectedCellValue(sheetName, 4, 5, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 5, 5, null),
					new ExpectedCellValue(sheetName, 6, 5, null),
					new ExpectedCellValue(sheetName, 7, 5, null),
					new ExpectedCellValue(sheetName, 8, 5, null),
					new ExpectedCellValue(sheetName, 9, 5, null),
					new ExpectedCellValue(sheetName, 10, 5, null),
					new ExpectedCellValue(sheetName, 11, 5, null),
					new ExpectedCellValue(sheetName, 12, 5, null),
					new ExpectedCellValue(sheetName, 13, 5, 24.99),
					new ExpectedCellValue(sheetName, 14, 5, 24.99),
					new ExpectedCellValue(sheetName, 15, 5, 24.99),
					new ExpectedCellValue(sheetName, 16, 5, null),
					new ExpectedCellValue(sheetName, 17, 5, null),
					new ExpectedCellValue(sheetName, 18, 5, null),
					new ExpectedCellValue(sheetName, 19, 5, null),
					new ExpectedCellValue(sheetName, 20, 5, null),
					new ExpectedCellValue(sheetName, 21, 5, null),
					new ExpectedCellValue(sheetName, 22, 5, 24.99),
					new ExpectedCellValue(sheetName, 2, 6, null),
					new ExpectedCellValue(sheetName, 3, 6, null),
					new ExpectedCellValue(sheetName, 4, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 6, null),
					new ExpectedCellValue(sheetName, 6, 6, null),
					new ExpectedCellValue(sheetName, 7, 6, null),
					new ExpectedCellValue(sheetName, 8, 6, null),
					new ExpectedCellValue(sheetName, 9, 6, null),
					new ExpectedCellValue(sheetName, 10, 6, null),
					new ExpectedCellValue(sheetName, 11, 6, null),
					new ExpectedCellValue(sheetName, 12, 6, null),
					new ExpectedCellValue(sheetName, 13, 6, 1d),
					new ExpectedCellValue(sheetName, 14, 6, 1d),
					new ExpectedCellValue(sheetName, 15, 6, 1d),
					new ExpectedCellValue(sheetName, 16, 6, null),
					new ExpectedCellValue(sheetName, 17, 6, null),
					new ExpectedCellValue(sheetName, 18, 6, null),
					new ExpectedCellValue(sheetName, 19, 6, null),
					new ExpectedCellValue(sheetName, 20, 6, null),
					new ExpectedCellValue(sheetName, 21, 6, null),
					new ExpectedCellValue(sheetName, 22, 6, 1d),
					new ExpectedCellValue(sheetName, 2, 7, null),
					new ExpectedCellValue(sheetName, 3, 7, null),
					new ExpectedCellValue(sheetName, 4, 7, "Sum of Total"),
					new ExpectedCellValue(sheetName, 5, 7, null),
					new ExpectedCellValue(sheetName, 6, 7, null),
					new ExpectedCellValue(sheetName, 7, 7, null),
					new ExpectedCellValue(sheetName, 8, 7, null),
					new ExpectedCellValue(sheetName, 9, 7, null),
					new ExpectedCellValue(sheetName, 10, 7, null),
					new ExpectedCellValue(sheetName, 11, 7, null),
					new ExpectedCellValue(sheetName, 12, 7, null),
					new ExpectedCellValue(sheetName, 13, 7, 24.99),
					new ExpectedCellValue(sheetName, 14, 7, 24.99),
					new ExpectedCellValue(sheetName, 15, 7, 24.99),
					new ExpectedCellValue(sheetName, 16, 7, null),
					new ExpectedCellValue(sheetName, 17, 7, null),
					new ExpectedCellValue(sheetName, 18, 7, null),
					new ExpectedCellValue(sheetName, 19, 7, null),
					new ExpectedCellValue(sheetName, 20, 7, null),
					new ExpectedCellValue(sheetName, 21, 7, null),
					new ExpectedCellValue(sheetName, 22, 7, 24.99),
					new ExpectedCellValue(sheetName, 2, 8, "Chicago Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 3, 8, null),
					new ExpectedCellValue(sheetName, 4, 8, null),
					new ExpectedCellValue(sheetName, 5, 8, 415.75),
					new ExpectedCellValue(sheetName, 6, 8, null),
					new ExpectedCellValue(sheetName, 7, 8, null),
					new ExpectedCellValue(sheetName, 8, 8, 415.75),
					new ExpectedCellValue(sheetName, 9, 8, 415.75),
					new ExpectedCellValue(sheetName, 10, 8, null),
					new ExpectedCellValue(sheetName, 11, 8, null),
					new ExpectedCellValue(sheetName, 12, 8, null),
					new ExpectedCellValue(sheetName, 13, 8, 24.99),
					new ExpectedCellValue(sheetName, 14, 8, 24.99),
					new ExpectedCellValue(sheetName, 15, 8, 24.99),
					new ExpectedCellValue(sheetName, 16, 8, null),
					new ExpectedCellValue(sheetName, 17, 8, null),
					new ExpectedCellValue(sheetName, 18, 8, null),
					new ExpectedCellValue(sheetName, 19, 8, null),
					new ExpectedCellValue(sheetName, 20, 8, null),
					new ExpectedCellValue(sheetName, 21, 8, null),
					new ExpectedCellValue(sheetName, 22, 8, 440.74),
					new ExpectedCellValue(sheetName, 2, 9, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 3, 9, null),
					new ExpectedCellValue(sheetName, 4, 9, null),
					new ExpectedCellValue(sheetName, 5, 9, 2d),
					new ExpectedCellValue(sheetName, 6, 9, null),
					new ExpectedCellValue(sheetName, 7, 9, null),
					new ExpectedCellValue(sheetName, 8, 9, 2d),
					new ExpectedCellValue(sheetName, 9, 9, 2d),
					new ExpectedCellValue(sheetName, 10, 9, null),
					new ExpectedCellValue(sheetName, 11, 9, null),
					new ExpectedCellValue(sheetName, 12, 9, null),
					new ExpectedCellValue(sheetName, 13, 9, 1d),
					new ExpectedCellValue(sheetName, 14, 9, 1d),
					new ExpectedCellValue(sheetName, 15, 9, 1d),
					new ExpectedCellValue(sheetName, 16, 9, null),
					new ExpectedCellValue(sheetName, 17, 9, null),
					new ExpectedCellValue(sheetName, 18, 9, null),
					new ExpectedCellValue(sheetName, 19, 9, null),
					new ExpectedCellValue(sheetName, 20, 9, null),
					new ExpectedCellValue(sheetName, 21, 9, null),
					new ExpectedCellValue(sheetName, 22, 9, 3d),
					new ExpectedCellValue(sheetName, 2, 10, "Chicago Sum of Total"),
					new ExpectedCellValue(sheetName, 3, 10, null),
					new ExpectedCellValue(sheetName, 4, 10, null),
					new ExpectedCellValue(sheetName, 5, 10, 831.5),
					new ExpectedCellValue(sheetName, 6, 10, null),
					new ExpectedCellValue(sheetName, 7, 10, null),
					new ExpectedCellValue(sheetName, 8, 10, 831.5),
					new ExpectedCellValue(sheetName, 9, 10, 831.5),
					new ExpectedCellValue(sheetName, 10, 10, null),
					new ExpectedCellValue(sheetName, 11, 10, null),
					new ExpectedCellValue(sheetName, 12, 10, null),
					new ExpectedCellValue(sheetName, 13, 10, 24.99),
					new ExpectedCellValue(sheetName, 14, 10, 24.99),
					new ExpectedCellValue(sheetName, 15, 10, 24.99),
					new ExpectedCellValue(sheetName, 16, 10, null),
					new ExpectedCellValue(sheetName, 17, 10, null),
					new ExpectedCellValue(sheetName, 18, 10, null),
					new ExpectedCellValue(sheetName, 19, 10, null),
					new ExpectedCellValue(sheetName, 20, 10, null),
					new ExpectedCellValue(sheetName, 21, 10, null),
					new ExpectedCellValue(sheetName, 22, 10, 856.49),
					new ExpectedCellValue(sheetName, 2, 11, "Nashville"),
					new ExpectedCellValue(sheetName, 3, 11, 2017),
					new ExpectedCellValue(sheetName, 4, 11, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 5, 11, 415.75),
					new ExpectedCellValue(sheetName, 6, 11, null),
					new ExpectedCellValue(sheetName, 7, 11, null),
					new ExpectedCellValue(sheetName, 8, 11, null),
					new ExpectedCellValue(sheetName, 9, 11, null),
					new ExpectedCellValue(sheetName, 10, 11, 415.75),
					new ExpectedCellValue(sheetName, 11, 11, 415.75),
					new ExpectedCellValue(sheetName, 12, 11, null),
					new ExpectedCellValue(sheetName, 13, 11, null),
					new ExpectedCellValue(sheetName, 14, 11, null),
					new ExpectedCellValue(sheetName, 15, 11, null),
					new ExpectedCellValue(sheetName, 16, 11, null),
					new ExpectedCellValue(sheetName, 17, 11, null),
					new ExpectedCellValue(sheetName, 18, 11, null),
					new ExpectedCellValue(sheetName, 19, 11, null),
					new ExpectedCellValue(sheetName, 20, 11, null),
					new ExpectedCellValue(sheetName, 21, 11, null),
					new ExpectedCellValue(sheetName, 22, 11, 415.75),
					new ExpectedCellValue(sheetName, 2, 12, null),
					new ExpectedCellValue(sheetName, 3, 12, null),
					new ExpectedCellValue(sheetName, 4, 12, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 12, 2d),
					new ExpectedCellValue(sheetName, 6, 12, null),
					new ExpectedCellValue(sheetName, 7, 12, null),
					new ExpectedCellValue(sheetName, 8, 12, null),
					new ExpectedCellValue(sheetName, 9, 12, null),
					new ExpectedCellValue(sheetName, 10, 12, 2d),
					new ExpectedCellValue(sheetName, 11, 12, 2d),
					new ExpectedCellValue(sheetName, 12, 12, null),
					new ExpectedCellValue(sheetName, 13, 12, null),
					new ExpectedCellValue(sheetName, 14, 12, null),
					new ExpectedCellValue(sheetName, 15, 12, null),
					new ExpectedCellValue(sheetName, 16, 12, null),
					new ExpectedCellValue(sheetName, 17, 12, null),
					new ExpectedCellValue(sheetName, 18, 12, null),
					new ExpectedCellValue(sheetName, 19, 12, null),
					new ExpectedCellValue(sheetName, 20, 12, null),
					new ExpectedCellValue(sheetName, 21, 12, null),
					new ExpectedCellValue(sheetName, 22, 12, 2d),
					new ExpectedCellValue(sheetName, 2, 13, null),
					new ExpectedCellValue(sheetName, 3, 13, null),
					new ExpectedCellValue(sheetName, 4, 13, "Sum of Total"),
					new ExpectedCellValue(sheetName, 5, 13, 831.5),
					new ExpectedCellValue(sheetName, 6, 13, null),
					new ExpectedCellValue(sheetName, 7, 13, null),
					new ExpectedCellValue(sheetName, 8, 13, null),
					new ExpectedCellValue(sheetName, 9, 13, null),
					new ExpectedCellValue(sheetName, 10, 13, 831.5),
					new ExpectedCellValue(sheetName, 11, 13, 831.5),
					new ExpectedCellValue(sheetName, 12, 13, null),
					new ExpectedCellValue(sheetName, 13, 13, null),
					new ExpectedCellValue(sheetName, 14, 13, null),
					new ExpectedCellValue(sheetName, 15, 13, null),
					new ExpectedCellValue(sheetName, 16, 13, null),
					new ExpectedCellValue(sheetName, 17, 13, null),
					new ExpectedCellValue(sheetName, 18, 13, null),
					new ExpectedCellValue(sheetName, 19, 13, null),
					new ExpectedCellValue(sheetName, 20, 13, null),
					new ExpectedCellValue(sheetName, 21, 13, null),
					new ExpectedCellValue(sheetName, 22, 13, 831.5),
					new ExpectedCellValue(sheetName, 2, 14, null),
					new ExpectedCellValue(sheetName, 3, 14, 2018),
					new ExpectedCellValue(sheetName, 4, 14, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 5, 14, 415.75),
					new ExpectedCellValue(sheetName, 6, 14, 415.75),
					new ExpectedCellValue(sheetName, 7, 14, 415.75),
					new ExpectedCellValue(sheetName, 8, 14, null),
					new ExpectedCellValue(sheetName, 9, 14, null),
					new ExpectedCellValue(sheetName, 10, 14, null),
					new ExpectedCellValue(sheetName, 11, 14, null),
					new ExpectedCellValue(sheetName, 12, 14, null),
					new ExpectedCellValue(sheetName, 13, 14, null),
					new ExpectedCellValue(sheetName, 14, 14, null),
					new ExpectedCellValue(sheetName, 15, 14, null),
					new ExpectedCellValue(sheetName, 16, 14, null),
					new ExpectedCellValue(sheetName, 17, 14, null),
					new ExpectedCellValue(sheetName, 18, 14, null),
					new ExpectedCellValue(sheetName, 19, 14, 199d),
					new ExpectedCellValue(sheetName, 20, 14, 199d),
					new ExpectedCellValue(sheetName, 21, 14, 199d),
					new ExpectedCellValue(sheetName, 22, 14, 614.75),
					new ExpectedCellValue(sheetName, 2, 15, null),
					new ExpectedCellValue(sheetName, 3, 15, null),
					new ExpectedCellValue(sheetName, 4, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 15, 2d),
					new ExpectedCellValue(sheetName, 6, 15, 2d),
					new ExpectedCellValue(sheetName, 7, 15, 2d),
					new ExpectedCellValue(sheetName, 8, 15, null),
					new ExpectedCellValue(sheetName, 9, 15, null),
					new ExpectedCellValue(sheetName, 10, 15, null),
					new ExpectedCellValue(sheetName, 11, 15, null),
					new ExpectedCellValue(sheetName, 12, 15, null),
					new ExpectedCellValue(sheetName, 13, 15, null),
					new ExpectedCellValue(sheetName, 14, 15, null),
					new ExpectedCellValue(sheetName, 15, 15, null),
					new ExpectedCellValue(sheetName, 16, 15, null),
					new ExpectedCellValue(sheetName, 17, 15, null),
					new ExpectedCellValue(sheetName, 18, 15, null),
					new ExpectedCellValue(sheetName, 19, 15, 6d),
					new ExpectedCellValue(sheetName, 20, 15, 6d),
					new ExpectedCellValue(sheetName, 21, 15, 6d),
					new ExpectedCellValue(sheetName, 22, 15, 8d),
					new ExpectedCellValue(sheetName, 2, 16, null),
					new ExpectedCellValue(sheetName, 3, 16, null),
					new ExpectedCellValue(sheetName, 4, 16, "Sum of Total"),
					new ExpectedCellValue(sheetName, 5, 16, 831.5),
					new ExpectedCellValue(sheetName, 6, 16, 831.5),
					new ExpectedCellValue(sheetName, 7, 16, 831.5),
					new ExpectedCellValue(sheetName, 8, 16, null),
					new ExpectedCellValue(sheetName, 9, 16, null),
					new ExpectedCellValue(sheetName, 10, 16, null),
					new ExpectedCellValue(sheetName, 11, 16, null),
					new ExpectedCellValue(sheetName, 12, 16, null),
					new ExpectedCellValue(sheetName, 13, 16, null),
					new ExpectedCellValue(sheetName, 14, 16, null),
					new ExpectedCellValue(sheetName, 15, 16, null),
					new ExpectedCellValue(sheetName, 16, 16, null),
					new ExpectedCellValue(sheetName, 17, 16, null),
					new ExpectedCellValue(sheetName, 18, 16, null),
					new ExpectedCellValue(sheetName, 19, 16, 1194d),
					new ExpectedCellValue(sheetName, 20, 16, 1194d),
					new ExpectedCellValue(sheetName, 21, 16, 1194d),
					new ExpectedCellValue(sheetName, 22, 16, 2025.5),
					new ExpectedCellValue(sheetName, 2, 17, "Nashville Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 3, 17, null),
					new ExpectedCellValue(sheetName, 4, 17, null),
					new ExpectedCellValue(sheetName, 5, 17, 831.5),
					new ExpectedCellValue(sheetName, 6, 17, 415.75),
					new ExpectedCellValue(sheetName, 7, 17, 415.75),
					new ExpectedCellValue(sheetName, 8, 17, null),
					new ExpectedCellValue(sheetName, 9, 17, null),
					new ExpectedCellValue(sheetName, 10, 17, 415.75),
					new ExpectedCellValue(sheetName, 11, 17, 415.75),
					new ExpectedCellValue(sheetName, 12, 17, null),
					new ExpectedCellValue(sheetName, 13, 17, null),
					new ExpectedCellValue(sheetName, 14, 17, null),
					new ExpectedCellValue(sheetName, 15, 17, null),
					new ExpectedCellValue(sheetName, 16, 17, null),
					new ExpectedCellValue(sheetName, 17, 17, null),
					new ExpectedCellValue(sheetName, 18, 17, null),
					new ExpectedCellValue(sheetName, 19, 17, 199d),
					new ExpectedCellValue(sheetName, 20, 17, 199d),
					new ExpectedCellValue(sheetName, 21, 17, 199d),
					new ExpectedCellValue(sheetName, 22, 17, 1030.5),
					new ExpectedCellValue(sheetName, 2, 18, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 3, 18, null),
					new ExpectedCellValue(sheetName, 4, 18, null),
					new ExpectedCellValue(sheetName, 5, 18, 4d),
					new ExpectedCellValue(sheetName, 6, 18, 2d),
					new ExpectedCellValue(sheetName, 7, 18, 2d),
					new ExpectedCellValue(sheetName, 8, 18, null),
					new ExpectedCellValue(sheetName, 9, 18, null),
					new ExpectedCellValue(sheetName, 10, 18, 2d),
					new ExpectedCellValue(sheetName, 11, 18, 2d),
					new ExpectedCellValue(sheetName, 12, 18, null),
					new ExpectedCellValue(sheetName, 13, 18, null),
					new ExpectedCellValue(sheetName, 14, 18, null),
					new ExpectedCellValue(sheetName, 15, 18, null),
					new ExpectedCellValue(sheetName, 16, 18, null),
					new ExpectedCellValue(sheetName, 17, 18, null),
					new ExpectedCellValue(sheetName, 18, 18, null),
					new ExpectedCellValue(sheetName, 19, 18, 6d),
					new ExpectedCellValue(sheetName, 20, 18, 6d),
					new ExpectedCellValue(sheetName, 21, 18, 6d),
					new ExpectedCellValue(sheetName, 22, 18, 10d),
					new ExpectedCellValue(sheetName, 2, 19, "Nashville Sum of Total"),
					new ExpectedCellValue(sheetName, 3, 19, null),
					new ExpectedCellValue(sheetName, 4, 19, null),
					new ExpectedCellValue(sheetName, 5, 19, 1663d),
					new ExpectedCellValue(sheetName, 6, 19, 831.5),
					new ExpectedCellValue(sheetName, 7, 19, 831.5),
					new ExpectedCellValue(sheetName, 8, 19, null),
					new ExpectedCellValue(sheetName, 9, 19, null),
					new ExpectedCellValue(sheetName, 10, 19, 831.5),
					new ExpectedCellValue(sheetName, 11, 19, 831.5),
					new ExpectedCellValue(sheetName, 12, 19, null),
					new ExpectedCellValue(sheetName, 13, 19, null),
					new ExpectedCellValue(sheetName, 14, 19, null),
					new ExpectedCellValue(sheetName, 15, 19, null),
					new ExpectedCellValue(sheetName, 16, 19, null),
					new ExpectedCellValue(sheetName, 17, 19, null),
					new ExpectedCellValue(sheetName, 18, 19, null),
					new ExpectedCellValue(sheetName, 19, 19, 1194d),
					new ExpectedCellValue(sheetName, 20, 19, 1194d),
					new ExpectedCellValue(sheetName, 21, 19, 1194d),
					new ExpectedCellValue(sheetName, 22, 19, 2857d),
					new ExpectedCellValue(sheetName, 2, 20, "San Francisco"),
					new ExpectedCellValue(sheetName, 3, 20, 2017),
					new ExpectedCellValue(sheetName, 4, 20, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 5, 20, null),
					new ExpectedCellValue(sheetName, 6, 20, null),
					new ExpectedCellValue(sheetName, 7, 20, null),
					new ExpectedCellValue(sheetName, 8, 20, null),
					new ExpectedCellValue(sheetName, 9, 20, null),
					new ExpectedCellValue(sheetName, 10, 20, null),
					new ExpectedCellValue(sheetName, 11, 20, null),
					new ExpectedCellValue(sheetName, 12, 20, null),
					new ExpectedCellValue(sheetName, 13, 20, null),
					new ExpectedCellValue(sheetName, 14, 20, null),
					new ExpectedCellValue(sheetName, 15, 20, null),
					new ExpectedCellValue(sheetName, 16, 20, 99d),
					new ExpectedCellValue(sheetName, 17, 20, 99d),
					new ExpectedCellValue(sheetName, 18, 20, 99d),
					new ExpectedCellValue(sheetName, 19, 20, null),
					new ExpectedCellValue(sheetName, 20, 20, null),
					new ExpectedCellValue(sheetName, 21, 20, null),
					new ExpectedCellValue(sheetName, 22, 20, 99d),
					new ExpectedCellValue(sheetName, 2, 21, null),
					new ExpectedCellValue(sheetName, 3, 21, null),
					new ExpectedCellValue(sheetName, 4, 21, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 21, null),
					new ExpectedCellValue(sheetName, 6, 21, null),
					new ExpectedCellValue(sheetName, 7, 21, null),
					new ExpectedCellValue(sheetName, 8, 21, null),
					new ExpectedCellValue(sheetName, 9, 21, null),
					new ExpectedCellValue(sheetName, 10, 21, null),
					new ExpectedCellValue(sheetName, 11, 21, null),
					new ExpectedCellValue(sheetName, 12, 21, null),
					new ExpectedCellValue(sheetName, 13, 21, null),
					new ExpectedCellValue(sheetName, 14, 21, null),
					new ExpectedCellValue(sheetName, 15, 21, null),
					new ExpectedCellValue(sheetName, 16, 21, 1d),
					new ExpectedCellValue(sheetName, 17, 21, 1d),
					new ExpectedCellValue(sheetName, 18, 21, 1d),
					new ExpectedCellValue(sheetName, 19, 21, null),
					new ExpectedCellValue(sheetName, 20, 21, null),
					new ExpectedCellValue(sheetName, 21, 21, null),
					new ExpectedCellValue(sheetName, 22, 21, 1d),
					new ExpectedCellValue(sheetName, 2, 22, null),
					new ExpectedCellValue(sheetName, 3, 22, null),
					new ExpectedCellValue(sheetName, 4, 22, "Sum of Total"),
					new ExpectedCellValue(sheetName, 5, 22, null),
					new ExpectedCellValue(sheetName, 6, 22, null),
					new ExpectedCellValue(sheetName, 7, 22, null),
					new ExpectedCellValue(sheetName, 8, 22, null),
					new ExpectedCellValue(sheetName, 9, 22, null),
					new ExpectedCellValue(sheetName, 10, 22, null),
					new ExpectedCellValue(sheetName, 11, 22, null),
					new ExpectedCellValue(sheetName, 12, 22, null),
					new ExpectedCellValue(sheetName, 13, 22, null),
					new ExpectedCellValue(sheetName, 14, 22, null),
					new ExpectedCellValue(sheetName, 15, 22, null),
					new ExpectedCellValue(sheetName, 16, 22, 99d),
					new ExpectedCellValue(sheetName, 17, 22, 99d),
					new ExpectedCellValue(sheetName, 18, 22, 99d),
					new ExpectedCellValue(sheetName, 19, 22, null),
					new ExpectedCellValue(sheetName, 20, 22, null),
					new ExpectedCellValue(sheetName, 21, 22, null),
					new ExpectedCellValue(sheetName, 22, 22, 99d),
					new ExpectedCellValue(sheetName, 2, 23, null),
					new ExpectedCellValue(sheetName, 3, 23, 2018),
					new ExpectedCellValue(sheetName, 4, 23, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 5, 23, 415.75),
					new ExpectedCellValue(sheetName, 6, 23, null),
					new ExpectedCellValue(sheetName, 7, 23, null),
					new ExpectedCellValue(sheetName, 8, 23, null),
					new ExpectedCellValue(sheetName, 9, 23, null),
					new ExpectedCellValue(sheetName, 10, 23, 415.75),
					new ExpectedCellValue(sheetName, 11, 23, null),
					new ExpectedCellValue(sheetName, 12, 23, 415.75),
					new ExpectedCellValue(sheetName, 13, 23, null),
					new ExpectedCellValue(sheetName, 14, 23, null),
					new ExpectedCellValue(sheetName, 15, 23, null),
					new ExpectedCellValue(sheetName, 16, 23, null),
					new ExpectedCellValue(sheetName, 17, 23, null),
					new ExpectedCellValue(sheetName, 18, 23, null),
					new ExpectedCellValue(sheetName, 19, 23, null),
					new ExpectedCellValue(sheetName, 20, 23, null),
					new ExpectedCellValue(sheetName, 21, 23, null),
					new ExpectedCellValue(sheetName, 22, 23, 415.75),
					new ExpectedCellValue(sheetName, 2, 24, null),
					new ExpectedCellValue(sheetName, 3, 24, null),
					new ExpectedCellValue(sheetName, 4, 24, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 5, 24, 1d),
					new ExpectedCellValue(sheetName, 6, 24, null),
					new ExpectedCellValue(sheetName, 7, 24, null),
					new ExpectedCellValue(sheetName, 8, 24, null),
					new ExpectedCellValue(sheetName, 9, 24, null),
					new ExpectedCellValue(sheetName, 10, 24, 1d),
					new ExpectedCellValue(sheetName, 11, 24, null),
					new ExpectedCellValue(sheetName, 12, 24, 1d),
					new ExpectedCellValue(sheetName, 13, 24, null),
					new ExpectedCellValue(sheetName, 14, 24, null),
					new ExpectedCellValue(sheetName, 15, 24, null),
					new ExpectedCellValue(sheetName, 16, 24, null),
					new ExpectedCellValue(sheetName, 17, 24, null),
					new ExpectedCellValue(sheetName, 18, 24, null),
					new ExpectedCellValue(sheetName, 19, 24, null),
					new ExpectedCellValue(sheetName, 20, 24, null),
					new ExpectedCellValue(sheetName, 21, 24, null),
					new ExpectedCellValue(sheetName, 22, 24, 1d),
					new ExpectedCellValue(sheetName, 2, 25, null),
					new ExpectedCellValue(sheetName, 3, 25, null),
					new ExpectedCellValue(sheetName, 4, 25, "Sum of Total"),
					new ExpectedCellValue(sheetName, 5, 25, 415.75),
					new ExpectedCellValue(sheetName, 6, 25, null),
					new ExpectedCellValue(sheetName, 7, 25, null),
					new ExpectedCellValue(sheetName, 8, 25, null),
					new ExpectedCellValue(sheetName, 9, 25, null),
					new ExpectedCellValue(sheetName, 10, 25, 415.75),
					new ExpectedCellValue(sheetName, 11, 25, null),
					new ExpectedCellValue(sheetName, 12, 25, 415.75),
					new ExpectedCellValue(sheetName, 13, 25, null),
					new ExpectedCellValue(sheetName, 14, 25, null),
					new ExpectedCellValue(sheetName, 15, 25, null),
					new ExpectedCellValue(sheetName, 16, 25, null),
					new ExpectedCellValue(sheetName, 17, 25, null),
					new ExpectedCellValue(sheetName, 18, 25, null),
					new ExpectedCellValue(sheetName, 19, 25, null),
					new ExpectedCellValue(sheetName, 20, 25, null),
					new ExpectedCellValue(sheetName, 21, 25, null),
					new ExpectedCellValue(sheetName, 22, 25, 415.75),
					new ExpectedCellValue(sheetName, 2, 26, "San Francisco Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 3, 26, null),
					new ExpectedCellValue(sheetName, 4, 26, null),
					new ExpectedCellValue(sheetName, 5, 26, 415.75),
					new ExpectedCellValue(sheetName, 6, 26, null),
					new ExpectedCellValue(sheetName, 7, 26, null),
					new ExpectedCellValue(sheetName, 8, 26, null),
					new ExpectedCellValue(sheetName, 9, 26, null),
					new ExpectedCellValue(sheetName, 10, 26, 415.75),
					new ExpectedCellValue(sheetName, 11, 26, null),
					new ExpectedCellValue(sheetName, 12, 26, 415.75),
					new ExpectedCellValue(sheetName, 13, 26, null),
					new ExpectedCellValue(sheetName, 14, 26, null),
					new ExpectedCellValue(sheetName, 15, 26, null),
					new ExpectedCellValue(sheetName, 16, 26, 99d),
					new ExpectedCellValue(sheetName, 17, 26, 99d),
					new ExpectedCellValue(sheetName, 18, 26, 99d),
					new ExpectedCellValue(sheetName, 19, 26, null),
					new ExpectedCellValue(sheetName, 20, 26, null),
					new ExpectedCellValue(sheetName, 21, 26, null),
					new ExpectedCellValue(sheetName, 22, 26, 514.75),
					new ExpectedCellValue(sheetName, 2, 27, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 3, 27, null),
					new ExpectedCellValue(sheetName, 4, 27, null),
					new ExpectedCellValue(sheetName, 5, 27, 1d),
					new ExpectedCellValue(sheetName, 6, 27, null),
					new ExpectedCellValue(sheetName, 7, 27, null),
					new ExpectedCellValue(sheetName, 8, 27, null),
					new ExpectedCellValue(sheetName, 9, 27, null),
					new ExpectedCellValue(sheetName, 10, 27, 1d),
					new ExpectedCellValue(sheetName, 11, 27, null),
					new ExpectedCellValue(sheetName, 12, 27, 1d),
					new ExpectedCellValue(sheetName, 13, 27, null),
					new ExpectedCellValue(sheetName, 14, 27, null),
					new ExpectedCellValue(sheetName, 15, 27, null),
					new ExpectedCellValue(sheetName, 16, 27, 1d),
					new ExpectedCellValue(sheetName, 17, 27, 1d),
					new ExpectedCellValue(sheetName, 18, 27, 1d),
					new ExpectedCellValue(sheetName, 19, 27, null),
					new ExpectedCellValue(sheetName, 20, 27, null),
					new ExpectedCellValue(sheetName, 21, 27, null),
					new ExpectedCellValue(sheetName, 22, 27, 2d),
					new ExpectedCellValue(sheetName, 2, 28, "San Francisco Sum of Total"),
					new ExpectedCellValue(sheetName, 3, 28, null),
					new ExpectedCellValue(sheetName, 4, 28, null),
					new ExpectedCellValue(sheetName, 5, 28, 415.75),
					new ExpectedCellValue(sheetName, 6, 28, null),
					new ExpectedCellValue(sheetName, 7, 28, null),
					new ExpectedCellValue(sheetName, 8, 28, null),
					new ExpectedCellValue(sheetName, 9, 28, null),
					new ExpectedCellValue(sheetName, 10, 28, 415.75),
					new ExpectedCellValue(sheetName, 11, 28, null),
					new ExpectedCellValue(sheetName, 12, 28, 415.75),
					new ExpectedCellValue(sheetName, 13, 28, null),
					new ExpectedCellValue(sheetName, 14, 28, null),
					new ExpectedCellValue(sheetName, 15, 28, null),
					new ExpectedCellValue(sheetName, 16, 28, 99d),
					new ExpectedCellValue(sheetName, 17, 28, 99d),
					new ExpectedCellValue(sheetName, 18, 28, 99d),
					new ExpectedCellValue(sheetName, 19, 28, null),
					new ExpectedCellValue(sheetName, 20, 28, null),
					new ExpectedCellValue(sheetName, 21, 28, null),
					new ExpectedCellValue(sheetName, 22, 28, 514.75),
					new ExpectedCellValue(sheetName, 2, 29, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 3, 29, null),
					new ExpectedCellValue(sheetName, 4, 29, null),
					new ExpectedCellValue(sheetName, 5, 29, 1663d),
					new ExpectedCellValue(sheetName, 6, 29, 415.75),
					new ExpectedCellValue(sheetName, 7, 29, 415.75),
					new ExpectedCellValue(sheetName, 8, 29, 415.75),
					new ExpectedCellValue(sheetName, 9, 29, 415.75),
					new ExpectedCellValue(sheetName, 10, 29, 831.5),
					new ExpectedCellValue(sheetName, 11, 29, 415.75),
					new ExpectedCellValue(sheetName, 12, 29, 415.75),
					new ExpectedCellValue(sheetName, 13, 29, 24.99),
					new ExpectedCellValue(sheetName, 14, 29, 24.99),
					new ExpectedCellValue(sheetName, 15, 29, 24.99),
					new ExpectedCellValue(sheetName, 16, 29, 99d),
					new ExpectedCellValue(sheetName, 17, 29, 99d),
					new ExpectedCellValue(sheetName, 18, 29, 99d),
					new ExpectedCellValue(sheetName, 19, 29, 199d),
					new ExpectedCellValue(sheetName, 20, 29, 199d),
					new ExpectedCellValue(sheetName, 21, 29, 199d),
					new ExpectedCellValue(sheetName, 22, 29, 1985.99),
					new ExpectedCellValue(sheetName, 2, 30, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 3, 30, null),
					new ExpectedCellValue(sheetName, 4, 30, null),
					new ExpectedCellValue(sheetName, 5, 30, 7d),
					new ExpectedCellValue(sheetName, 6, 30, 2d),
					new ExpectedCellValue(sheetName, 7, 30, 2d),
					new ExpectedCellValue(sheetName, 8, 30, 2d),
					new ExpectedCellValue(sheetName, 9, 30, 2d),
					new ExpectedCellValue(sheetName, 10, 30, 3d),
					new ExpectedCellValue(sheetName, 11, 30, 2d),
					new ExpectedCellValue(sheetName, 12, 30, 1d),
					new ExpectedCellValue(sheetName, 13, 30, 1d),
					new ExpectedCellValue(sheetName, 14, 30, 1d),
					new ExpectedCellValue(sheetName, 15, 30, 1d),
					new ExpectedCellValue(sheetName, 16, 30, 1d),
					new ExpectedCellValue(sheetName, 17, 30, 1d),
					new ExpectedCellValue(sheetName, 18, 30, 1d),
					new ExpectedCellValue(sheetName, 19, 30, 6d),
					new ExpectedCellValue(sheetName, 20, 30, 6d),
					new ExpectedCellValue(sheetName, 21, 30, 6d),
					new ExpectedCellValue(sheetName, 22, 30, 15d),
					new ExpectedCellValue(sheetName, 2, 31, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 3, 31, null),
					new ExpectedCellValue(sheetName, 4, 31, null),
					new ExpectedCellValue(sheetName, 5, 31, 2910.25),
					new ExpectedCellValue(sheetName, 6, 31, 831.5),
					new ExpectedCellValue(sheetName, 7, 31, 831.5),
					new ExpectedCellValue(sheetName, 8, 31, 831.5),
					new ExpectedCellValue(sheetName, 9, 31, 831.5),
					new ExpectedCellValue(sheetName, 10, 31, 1247.25),
					new ExpectedCellValue(sheetName, 11, 31, 831.5),
					new ExpectedCellValue(sheetName, 12, 31, 415.75),
					new ExpectedCellValue(sheetName, 13, 31, 24.99),
					new ExpectedCellValue(sheetName, 14, 31, 24.99),
					new ExpectedCellValue(sheetName, 15, 31, 24.99),
					new ExpectedCellValue(sheetName, 16, 31, 99d),
					new ExpectedCellValue(sheetName, 17, 31, 99d),
					new ExpectedCellValue(sheetName, 18, 31, 99d),
					new ExpectedCellValue(sheetName, 19, 31, 1194d),
					new ExpectedCellValue(sheetName, 20, 31, 1194d),
					new ExpectedCellValue(sheetName, 21, 31, 1194d),
					new ExpectedCellValue(sheetName, 22, 31, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGrouping.xlsx")]
		public void PivotTableRefreshDateGroupingRowDataFields()
		{
			var file = new FileInfo("PivotTableDateGrouping.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:S78"), pivotTable.Address);
					Assert.AreEqual(9, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[8].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 6, 1, 20100007),
					new ExpectedCellValue(sheetName, 7, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 8, 1, "May"),
					new ExpectedCellValue(sheetName, 9, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 10, 1, "May"),
					new ExpectedCellValue(sheetName, 11, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 12, 1, "May"),
					new ExpectedCellValue(sheetName, 13, 1, "20100007 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 14, 1, "20100007 Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 1, "20100007 Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 1, "20100017"),
					new ExpectedCellValue(sheetName, 17, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 18, 1, "Oct"),
					new ExpectedCellValue(sheetName, 19, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 1, "Oct"),
					new ExpectedCellValue(sheetName, 21, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 22, 1, "Oct"),
					new ExpectedCellValue(sheetName, 23, 1, "20100017 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 24, 1, "20100017 Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 25, 1, "20100017 Sum of Total"),
					new ExpectedCellValue(sheetName, 26, 1, "20100070"),
					new ExpectedCellValue(sheetName, 27, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 28, 1, "May"),
					new ExpectedCellValue(sheetName, 29, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 30, 1, "May"),
					new ExpectedCellValue(sheetName, 31, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 32, 1, "May"),
					new ExpectedCellValue(sheetName, 33, 1, "20100070 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 34, 1, "20100070 Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 35, 1, "20100070 Sum of Total"),
					new ExpectedCellValue(sheetName, 36, 1, "20100076"),
					new ExpectedCellValue(sheetName, 37, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 38, 1, "Nov"),
					new ExpectedCellValue(sheetName, 39, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 40, 1, "Nov"),
					new ExpectedCellValue(sheetName, 41, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 42, 1, "Nov"),
					new ExpectedCellValue(sheetName, 43, 1, "20100076 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 44, 1, "20100076 Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 45, 1, "20100076 Sum of Total"),
					new ExpectedCellValue(sheetName, 46, 1, "20100083"),
					new ExpectedCellValue(sheetName, 47, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 48, 1, "Mar"),
					new ExpectedCellValue(sheetName, 49, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 50, 1, "Mar"),
					new ExpectedCellValue(sheetName, 51, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 52, 1, "Mar"),
					new ExpectedCellValue(sheetName, 53, 1, "20100083 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 54, 1, "20100083 Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 55, 1, "20100083 Sum of Total"),
					new ExpectedCellValue(sheetName, 56, 1, "20100085"),
					new ExpectedCellValue(sheetName, 57, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 58, 1, "Dec"),
					new ExpectedCellValue(sheetName, 59, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 60, 1, "Dec"),
					new ExpectedCellValue(sheetName, 61, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 62, 1, "Dec"),
					new ExpectedCellValue(sheetName, 63, 1, "20100085 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 64, 1, "20100085 Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 65, 1, "20100085 Sum of Total"),
					new ExpectedCellValue(sheetName, 66, 1, "20100090"),
					new ExpectedCellValue(sheetName, 67, 1, "Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 68, 1, "Feb"),
					new ExpectedCellValue(sheetName, 69, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 70, 1, "Feb"),
					new ExpectedCellValue(sheetName, 71, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 72, 1, "Feb"),
					new ExpectedCellValue(sheetName, 73, 1, "20100090 Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 74, 1, "20100090 Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 75, 1, "20100090 Sum of Total"),
					new ExpectedCellValue(sheetName, 76, 1, "Total Sum of Wholesale Price"),
					new ExpectedCellValue(sheetName, 77, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 78, 1, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, "Car Rack"),
					new ExpectedCellValue(sheetName, 3, 2, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 2, 2016),
					new ExpectedCellValue(sheetName, 5, 2, "Qtr2"),
					new ExpectedCellValue(sheetName, 6, 2, null),
					new ExpectedCellValue(sheetName, 7, 2, null),
					new ExpectedCellValue(sheetName, 8, 2, 415.75),
					new ExpectedCellValue(sheetName, 9, 2, null),
					new ExpectedCellValue(sheetName, 10, 2, 2d),
					new ExpectedCellValue(sheetName, 11, 2, null),
					new ExpectedCellValue(sheetName, 12, 2, 831.5),
					new ExpectedCellValue(sheetName, 13, 2, 415.75),
					new ExpectedCellValue(sheetName, 14, 2, 2d),
					new ExpectedCellValue(sheetName, 15, 2, 831.5),
					new ExpectedCellValue(sheetName, 16, 2, null),
					new ExpectedCellValue(sheetName, 17, 2, null),
					new ExpectedCellValue(sheetName, 18, 2, null),
					new ExpectedCellValue(sheetName, 19, 2, null),
					new ExpectedCellValue(sheetName, 20, 2, null),
					new ExpectedCellValue(sheetName, 21, 2, null),
					new ExpectedCellValue(sheetName, 22, 2, null),
					new ExpectedCellValue(sheetName, 23, 2, null),
					new ExpectedCellValue(sheetName, 24, 2, null),
					new ExpectedCellValue(sheetName, 25, 2, null),
					new ExpectedCellValue(sheetName, 26, 2, null),
					new ExpectedCellValue(sheetName, 27, 2, null),
					new ExpectedCellValue(sheetName, 28, 2, null),
					new ExpectedCellValue(sheetName, 29, 2, null),
					new ExpectedCellValue(sheetName, 30, 2, null),
					new ExpectedCellValue(sheetName, 31, 2, null),
					new ExpectedCellValue(sheetName, 32, 2, null),
					new ExpectedCellValue(sheetName, 33, 2, null),
					new ExpectedCellValue(sheetName, 34, 2, null),
					new ExpectedCellValue(sheetName, 35, 2, null),
					new ExpectedCellValue(sheetName, 36, 2, null),
					new ExpectedCellValue(sheetName, 37, 2, null),
					new ExpectedCellValue(sheetName, 38, 2, null),
					new ExpectedCellValue(sheetName, 39, 2, null),
					new ExpectedCellValue(sheetName, 40, 2, null),
					new ExpectedCellValue(sheetName, 41, 2, null),
					new ExpectedCellValue(sheetName, 42, 2, null),
					new ExpectedCellValue(sheetName, 43, 2, null),
					new ExpectedCellValue(sheetName, 44, 2, null),
					new ExpectedCellValue(sheetName, 45, 2, null),
					new ExpectedCellValue(sheetName, 46, 2, null),
					new ExpectedCellValue(sheetName, 47, 2, null),
					new ExpectedCellValue(sheetName, 48, 2, null),
					new ExpectedCellValue(sheetName, 49, 2, null),
					new ExpectedCellValue(sheetName, 50, 2, null),
					new ExpectedCellValue(sheetName, 51, 2, null),
					new ExpectedCellValue(sheetName, 52, 2, null),
					new ExpectedCellValue(sheetName, 53, 2, null),
					new ExpectedCellValue(sheetName, 54, 2, null),
					new ExpectedCellValue(sheetName, 55, 2, null),
					new ExpectedCellValue(sheetName, 56, 2, null),
					new ExpectedCellValue(sheetName, 57, 2, null),
					new ExpectedCellValue(sheetName, 58, 2, null),
					new ExpectedCellValue(sheetName, 59, 2, null),
					new ExpectedCellValue(sheetName, 60, 2, null),
					new ExpectedCellValue(sheetName, 61, 2, null),
					new ExpectedCellValue(sheetName, 62, 2, null),
					new ExpectedCellValue(sheetName, 63, 2, null),
					new ExpectedCellValue(sheetName, 64, 2, null),
					new ExpectedCellValue(sheetName, 65, 2, null),
					new ExpectedCellValue(sheetName, 66, 2, null),
					new ExpectedCellValue(sheetName, 67, 2, null),
					new ExpectedCellValue(sheetName, 68, 2, null),
					new ExpectedCellValue(sheetName, 69, 2, null),
					new ExpectedCellValue(sheetName, 70, 2, null),
					new ExpectedCellValue(sheetName, 71, 2, null),
					new ExpectedCellValue(sheetName, 72, 2, null),
					new ExpectedCellValue(sheetName, 73, 2, null),
					new ExpectedCellValue(sheetName, 74, 2, null),
					new ExpectedCellValue(sheetName, 75, 2, null),
					new ExpectedCellValue(sheetName, 76, 2, 415.75),
					new ExpectedCellValue(sheetName, 77, 2, 2d),
					new ExpectedCellValue(sheetName, 78, 2, 831.5),
					new ExpectedCellValue(sheetName, 2, 3, null),
					new ExpectedCellValue(sheetName, 3, 3, "Chicago Total"),
					new ExpectedCellValue(sheetName, 4, 3, null),
					new ExpectedCellValue(sheetName, 5, 3, null),
					new ExpectedCellValue(sheetName, 6, 3, null),
					new ExpectedCellValue(sheetName, 7, 3, null),
					new ExpectedCellValue(sheetName, 8, 3, 415.75),
					new ExpectedCellValue(sheetName, 9, 3, null),
					new ExpectedCellValue(sheetName, 10, 3, 2d),
					new ExpectedCellValue(sheetName, 11, 3, null),
					new ExpectedCellValue(sheetName, 12, 3, 831.5),
					new ExpectedCellValue(sheetName, 13, 3, 415.75),
					new ExpectedCellValue(sheetName, 14, 3, 2d),
					new ExpectedCellValue(sheetName, 15, 3, 831.5),
					new ExpectedCellValue(sheetName, 16, 3, null),
					new ExpectedCellValue(sheetName, 17, 3, null),
					new ExpectedCellValue(sheetName, 18, 3, null),
					new ExpectedCellValue(sheetName, 19, 3, null),
					new ExpectedCellValue(sheetName, 20, 3, null),
					new ExpectedCellValue(sheetName, 21, 3, null),
					new ExpectedCellValue(sheetName, 22, 3, null),
					new ExpectedCellValue(sheetName, 23, 3, null),
					new ExpectedCellValue(sheetName, 24, 3, null),
					new ExpectedCellValue(sheetName, 25, 3, null),
					new ExpectedCellValue(sheetName, 26, 3, null),
					new ExpectedCellValue(sheetName, 27, 3, null),
					new ExpectedCellValue(sheetName, 28, 3, null),
					new ExpectedCellValue(sheetName, 29, 3, null),
					new ExpectedCellValue(sheetName, 30, 3, null),
					new ExpectedCellValue(sheetName, 31, 3, null),
					new ExpectedCellValue(sheetName, 32, 3, null),
					new ExpectedCellValue(sheetName, 33, 3, null),
					new ExpectedCellValue(sheetName, 34, 3, null),
					new ExpectedCellValue(sheetName, 35, 3, null),
					new ExpectedCellValue(sheetName, 36, 3, null),
					new ExpectedCellValue(sheetName, 37, 3, null),
					new ExpectedCellValue(sheetName, 38, 3, null),
					new ExpectedCellValue(sheetName, 39, 3, null),
					new ExpectedCellValue(sheetName, 40, 3, null),
					new ExpectedCellValue(sheetName, 41, 3, null),
					new ExpectedCellValue(sheetName, 42, 3, null),
					new ExpectedCellValue(sheetName, 43, 3, null),
					new ExpectedCellValue(sheetName, 44, 3, null),
					new ExpectedCellValue(sheetName, 45, 3, null),
					new ExpectedCellValue(sheetName, 46, 3, null),
					new ExpectedCellValue(sheetName, 47, 3, null),
					new ExpectedCellValue(sheetName, 48, 3, null),
					new ExpectedCellValue(sheetName, 49, 3, null),
					new ExpectedCellValue(sheetName, 50, 3, null),
					new ExpectedCellValue(sheetName, 51, 3, null),
					new ExpectedCellValue(sheetName, 52, 3, null),
					new ExpectedCellValue(sheetName, 53, 3, null),
					new ExpectedCellValue(sheetName, 54, 3, null),
					new ExpectedCellValue(sheetName, 55, 3, null),
					new ExpectedCellValue(sheetName, 56, 3, null),
					new ExpectedCellValue(sheetName, 57, 3, null),
					new ExpectedCellValue(sheetName, 58, 3, null),
					new ExpectedCellValue(sheetName, 59, 3, null),
					new ExpectedCellValue(sheetName, 60, 3, null),
					new ExpectedCellValue(sheetName, 61, 3, null),
					new ExpectedCellValue(sheetName, 62, 3, null),
					new ExpectedCellValue(sheetName, 63, 3, null),
					new ExpectedCellValue(sheetName, 64, 3, null),
					new ExpectedCellValue(sheetName, 65, 3, null),
					new ExpectedCellValue(sheetName, 66, 3, null),
					new ExpectedCellValue(sheetName, 67, 3, null),
					new ExpectedCellValue(sheetName, 68, 3, null),
					new ExpectedCellValue(sheetName, 69, 3, null),
					new ExpectedCellValue(sheetName, 70, 3, null),
					new ExpectedCellValue(sheetName, 71, 3, null),
					new ExpectedCellValue(sheetName, 72, 3, null),
					new ExpectedCellValue(sheetName, 73, 3, null),
					new ExpectedCellValue(sheetName, 74, 3, null),
					new ExpectedCellValue(sheetName, 75, 3, null),
					new ExpectedCellValue(sheetName, 76, 3, 415.75),
					new ExpectedCellValue(sheetName, 77, 3, 2d),
					new ExpectedCellValue(sheetName, 78, 3, 831.5),
					new ExpectedCellValue(sheetName, 2, 4, null),
					new ExpectedCellValue(sheetName, 3, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 4, 2017),
					new ExpectedCellValue(sheetName, 5, 4, "Qtr4"),
					new ExpectedCellValue(sheetName, 6, 4, null),
					new ExpectedCellValue(sheetName, 7, 4, null),
					new ExpectedCellValue(sheetName, 8, 4, null),
					new ExpectedCellValue(sheetName, 9, 4, null),
					new ExpectedCellValue(sheetName, 10, 4, null),
					new ExpectedCellValue(sheetName, 11, 4, null),
					new ExpectedCellValue(sheetName, 12, 4, null),
					new ExpectedCellValue(sheetName, 13, 4, null),
					new ExpectedCellValue(sheetName, 14, 4, null),
					new ExpectedCellValue(sheetName, 15, 4, null),
					new ExpectedCellValue(sheetName, 16, 4, null),
					new ExpectedCellValue(sheetName, 17, 4, null),
					new ExpectedCellValue(sheetName, 18, 4, 415.75),
					new ExpectedCellValue(sheetName, 19, 4, null),
					new ExpectedCellValue(sheetName, 20, 4, 2d),
					new ExpectedCellValue(sheetName, 21, 4, null),
					new ExpectedCellValue(sheetName, 22, 4, 831.5),
					new ExpectedCellValue(sheetName, 23, 4, 415.75),
					new ExpectedCellValue(sheetName, 24, 4, 2d),
					new ExpectedCellValue(sheetName, 25, 4, 831.5),
					new ExpectedCellValue(sheetName, 26, 4, null),
					new ExpectedCellValue(sheetName, 27, 4, null),
					new ExpectedCellValue(sheetName, 28, 4, null),
					new ExpectedCellValue(sheetName, 29, 4, null),
					new ExpectedCellValue(sheetName, 30, 4, null),
					new ExpectedCellValue(sheetName, 31, 4, null),
					new ExpectedCellValue(sheetName, 32, 4, null),
					new ExpectedCellValue(sheetName, 33, 4, null),
					new ExpectedCellValue(sheetName, 34, 4, null),
					new ExpectedCellValue(sheetName, 35, 4, null),
					new ExpectedCellValue(sheetName, 36, 4, null),
					new ExpectedCellValue(sheetName, 37, 4, null),
					new ExpectedCellValue(sheetName, 38, 4, null),
					new ExpectedCellValue(sheetName, 39, 4, null),
					new ExpectedCellValue(sheetName, 40, 4, null),
					new ExpectedCellValue(sheetName, 41, 4, null),
					new ExpectedCellValue(sheetName, 42, 4, null),
					new ExpectedCellValue(sheetName, 43, 4, null),
					new ExpectedCellValue(sheetName, 44, 4, null),
					new ExpectedCellValue(sheetName, 45, 4, null),
					new ExpectedCellValue(sheetName, 46, 4, null),
					new ExpectedCellValue(sheetName, 47, 4, null),
					new ExpectedCellValue(sheetName, 48, 4, null),
					new ExpectedCellValue(sheetName, 49, 4, null),
					new ExpectedCellValue(sheetName, 50, 4, null),
					new ExpectedCellValue(sheetName, 51, 4, null),
					new ExpectedCellValue(sheetName, 52, 4, null),
					new ExpectedCellValue(sheetName, 53, 4, null),
					new ExpectedCellValue(sheetName, 54, 4, null),
					new ExpectedCellValue(sheetName, 55, 4, null),
					new ExpectedCellValue(sheetName, 56, 4, null),
					new ExpectedCellValue(sheetName, 57, 4, null),
					new ExpectedCellValue(sheetName, 58, 4, null),
					new ExpectedCellValue(sheetName, 59, 4, null),
					new ExpectedCellValue(sheetName, 60, 4, null),
					new ExpectedCellValue(sheetName, 61, 4, null),
					new ExpectedCellValue(sheetName, 62, 4, null),
					new ExpectedCellValue(sheetName, 63, 4, null),
					new ExpectedCellValue(sheetName, 64, 4, null),
					new ExpectedCellValue(sheetName, 65, 4, null),
					new ExpectedCellValue(sheetName, 66, 4, null),
					new ExpectedCellValue(sheetName, 67, 4, null),
					new ExpectedCellValue(sheetName, 68, 4, null),
					new ExpectedCellValue(sheetName, 69, 4, null),
					new ExpectedCellValue(sheetName, 70, 4, null),
					new ExpectedCellValue(sheetName, 71, 4, null),
					new ExpectedCellValue(sheetName, 72, 4, null),
					new ExpectedCellValue(sheetName, 73, 4, null),
					new ExpectedCellValue(sheetName, 74, 4, null),
					new ExpectedCellValue(sheetName, 75, 4, null),
					new ExpectedCellValue(sheetName, 76, 4, 415.75),
					new ExpectedCellValue(sheetName, 77, 4, 2d),
					new ExpectedCellValue(sheetName, 78, 4, 831.5),
					new ExpectedCellValue(sheetName, 2, 5, null),
					new ExpectedCellValue(sheetName, 3, 5, null),
					new ExpectedCellValue(sheetName, 4, 5, 2018),
					new ExpectedCellValue(sheetName, 5, 5, "Qtr1"),
					new ExpectedCellValue(sheetName, 6, 5, null),
					new ExpectedCellValue(sheetName, 7, 5, null),
					new ExpectedCellValue(sheetName, 8, 5, null),
					new ExpectedCellValue(sheetName, 9, 5, null),
					new ExpectedCellValue(sheetName, 10, 5, null),
					new ExpectedCellValue(sheetName, 11, 5, null),
					new ExpectedCellValue(sheetName, 12, 5, null),
					new ExpectedCellValue(sheetName, 13, 5, null),
					new ExpectedCellValue(sheetName, 14, 5, null),
					new ExpectedCellValue(sheetName, 15, 5, null),
					new ExpectedCellValue(sheetName, 16, 5, null),
					new ExpectedCellValue(sheetName, 17, 5, null),
					new ExpectedCellValue(sheetName, 18, 5, null),
					new ExpectedCellValue(sheetName, 19, 5, null),
					new ExpectedCellValue(sheetName, 20, 5, null),
					new ExpectedCellValue(sheetName, 21, 5, null),
					new ExpectedCellValue(sheetName, 22, 5, null),
					new ExpectedCellValue(sheetName, 23, 5, null),
					new ExpectedCellValue(sheetName, 24, 5, null),
					new ExpectedCellValue(sheetName, 25, 5, null),
					new ExpectedCellValue(sheetName, 26, 5, null),
					new ExpectedCellValue(sheetName, 27, 5, null),
					new ExpectedCellValue(sheetName, 28, 5, null),
					new ExpectedCellValue(sheetName, 29, 5, null),
					new ExpectedCellValue(sheetName, 30, 5, null),
					new ExpectedCellValue(sheetName, 31, 5, null),
					new ExpectedCellValue(sheetName, 32, 5, null),
					new ExpectedCellValue(sheetName, 33, 5, null),
					new ExpectedCellValue(sheetName, 34, 5, null),
					new ExpectedCellValue(sheetName, 35, 5, null),
					new ExpectedCellValue(sheetName, 36, 5, null),
					new ExpectedCellValue(sheetName, 37, 5, null),
					new ExpectedCellValue(sheetName, 38, 5, null),
					new ExpectedCellValue(sheetName, 39, 5, null),
					new ExpectedCellValue(sheetName, 40, 5, null),
					new ExpectedCellValue(sheetName, 41, 5, null),
					new ExpectedCellValue(sheetName, 42, 5, null),
					new ExpectedCellValue(sheetName, 43, 5, null),
					new ExpectedCellValue(sheetName, 44, 5, null),
					new ExpectedCellValue(sheetName, 45, 5, null),
					new ExpectedCellValue(sheetName, 46, 5, null),
					new ExpectedCellValue(sheetName, 47, 5, null),
					new ExpectedCellValue(sheetName, 48, 5, null),
					new ExpectedCellValue(sheetName, 49, 5, null),
					new ExpectedCellValue(sheetName, 50, 5, null),
					new ExpectedCellValue(sheetName, 51, 5, null),
					new ExpectedCellValue(sheetName, 52, 5, null),
					new ExpectedCellValue(sheetName, 53, 5, null),
					new ExpectedCellValue(sheetName, 54, 5, null),
					new ExpectedCellValue(sheetName, 55, 5, null),
					new ExpectedCellValue(sheetName, 56, 5, null),
					new ExpectedCellValue(sheetName, 57, 5, null),
					new ExpectedCellValue(sheetName, 58, 5, null),
					new ExpectedCellValue(sheetName, 59, 5, null),
					new ExpectedCellValue(sheetName, 60, 5, null),
					new ExpectedCellValue(sheetName, 61, 5, null),
					new ExpectedCellValue(sheetName, 62, 5, null),
					new ExpectedCellValue(sheetName, 63, 5, null),
					new ExpectedCellValue(sheetName, 64, 5, null),
					new ExpectedCellValue(sheetName, 65, 5, null),
					new ExpectedCellValue(sheetName, 66, 5, null),
					new ExpectedCellValue(sheetName, 67, 5, null),
					new ExpectedCellValue(sheetName, 68, 5, 415.75),
					new ExpectedCellValue(sheetName, 69, 5, null),
					new ExpectedCellValue(sheetName, 70, 5, 2d),
					new ExpectedCellValue(sheetName, 71, 5, null),
					new ExpectedCellValue(sheetName, 72, 5, 831.5),
					new ExpectedCellValue(sheetName, 73, 5, 415.75),
					new ExpectedCellValue(sheetName, 74, 5, 2d),
					new ExpectedCellValue(sheetName, 75, 5, 831.5),
					new ExpectedCellValue(sheetName, 76, 5, 415.75),
					new ExpectedCellValue(sheetName, 77, 5, 2d),
					new ExpectedCellValue(sheetName, 78, 5, 831.5),
					new ExpectedCellValue(sheetName, 2, 6, null),
					new ExpectedCellValue(sheetName, 3, 6, "Nashville Total"),
					new ExpectedCellValue(sheetName, 4, 6, null),
					new ExpectedCellValue(sheetName, 5, 6, null),
					new ExpectedCellValue(sheetName, 6, 6, null),
					new ExpectedCellValue(sheetName, 7, 6, null),
					new ExpectedCellValue(sheetName, 8, 6, null),
					new ExpectedCellValue(sheetName, 9, 6, null),
					new ExpectedCellValue(sheetName, 10, 6, null),
					new ExpectedCellValue(sheetName, 11, 6, null),
					new ExpectedCellValue(sheetName, 12, 6, null),
					new ExpectedCellValue(sheetName, 13, 6, null),
					new ExpectedCellValue(sheetName, 14, 6, null),
					new ExpectedCellValue(sheetName, 15, 6, null),
					new ExpectedCellValue(sheetName, 16, 6, null),
					new ExpectedCellValue(sheetName, 17, 6, null),
					new ExpectedCellValue(sheetName, 18, 6, 415.75),
					new ExpectedCellValue(sheetName, 19, 6, null),
					new ExpectedCellValue(sheetName, 20, 6, 2d),
					new ExpectedCellValue(sheetName, 21, 6, null),
					new ExpectedCellValue(sheetName, 22, 6, 831.5),
					new ExpectedCellValue(sheetName, 23, 6, 415.75),
					new ExpectedCellValue(sheetName, 24, 6, 2d),
					new ExpectedCellValue(sheetName, 25, 6, 831.5),
					new ExpectedCellValue(sheetName, 26, 6, null),
					new ExpectedCellValue(sheetName, 27, 6, null),
					new ExpectedCellValue(sheetName, 28, 6, null),
					new ExpectedCellValue(sheetName, 29, 6, null),
					new ExpectedCellValue(sheetName, 30, 6, null),
					new ExpectedCellValue(sheetName, 31, 6, null),
					new ExpectedCellValue(sheetName, 32, 6, null),
					new ExpectedCellValue(sheetName, 33, 6, null),
					new ExpectedCellValue(sheetName, 34, 6, null),
					new ExpectedCellValue(sheetName, 35, 6, null),
					new ExpectedCellValue(sheetName, 36, 6, null),
					new ExpectedCellValue(sheetName, 37, 6, null),
					new ExpectedCellValue(sheetName, 38, 6, null),
					new ExpectedCellValue(sheetName, 39, 6, null),
					new ExpectedCellValue(sheetName, 40, 6, null),
					new ExpectedCellValue(sheetName, 41, 6, null),
					new ExpectedCellValue(sheetName, 42, 6, null),
					new ExpectedCellValue(sheetName, 43, 6, null),
					new ExpectedCellValue(sheetName, 44, 6, null),
					new ExpectedCellValue(sheetName, 45, 6, null),
					new ExpectedCellValue(sheetName, 46, 6, null),
					new ExpectedCellValue(sheetName, 47, 6, null),
					new ExpectedCellValue(sheetName, 48, 6, null),
					new ExpectedCellValue(sheetName, 49, 6, null),
					new ExpectedCellValue(sheetName, 50, 6, null),
					new ExpectedCellValue(sheetName, 51, 6, null),
					new ExpectedCellValue(sheetName, 52, 6, null),
					new ExpectedCellValue(sheetName, 53, 6, null),
					new ExpectedCellValue(sheetName, 54, 6, null),
					new ExpectedCellValue(sheetName, 55, 6, null),
					new ExpectedCellValue(sheetName, 56, 6, null),
					new ExpectedCellValue(sheetName, 57, 6, null),
					new ExpectedCellValue(sheetName, 58, 6, null),
					new ExpectedCellValue(sheetName, 59, 6, null),
					new ExpectedCellValue(sheetName, 60, 6, null),
					new ExpectedCellValue(sheetName, 61, 6, null),
					new ExpectedCellValue(sheetName, 62, 6, null),
					new ExpectedCellValue(sheetName, 63, 6, null),
					new ExpectedCellValue(sheetName, 64, 6, null),
					new ExpectedCellValue(sheetName, 65, 6, null),
					new ExpectedCellValue(sheetName, 66, 6, null),
					new ExpectedCellValue(sheetName, 67, 6, null),
					new ExpectedCellValue(sheetName, 68, 6, 415.75),
					new ExpectedCellValue(sheetName, 69, 6, null),
					new ExpectedCellValue(sheetName, 70, 6, 2d),
					new ExpectedCellValue(sheetName, 71, 6, null),
					new ExpectedCellValue(sheetName, 72, 6, 831.5),
					new ExpectedCellValue(sheetName, 73, 6, 415.75),
					new ExpectedCellValue(sheetName, 74, 6, 2d),
					new ExpectedCellValue(sheetName, 75, 6, 831.5),
					new ExpectedCellValue(sheetName, 76, 6, 831.5),
					new ExpectedCellValue(sheetName, 77, 6, 4d),
					new ExpectedCellValue(sheetName, 78, 6, 1663d),
					new ExpectedCellValue(sheetName, 2, 7, null),
					new ExpectedCellValue(sheetName, 3, 7, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 7, 2018),
					new ExpectedCellValue(sheetName, 5, 7, "Qtr4"),
					new ExpectedCellValue(sheetName, 6, 7, null),
					new ExpectedCellValue(sheetName, 7, 7, null),
					new ExpectedCellValue(sheetName, 8, 7, null),
					new ExpectedCellValue(sheetName, 9, 7, null),
					new ExpectedCellValue(sheetName, 10, 7, null),
					new ExpectedCellValue(sheetName, 11, 7, null),
					new ExpectedCellValue(sheetName, 12, 7, null),
					new ExpectedCellValue(sheetName, 13, 7, null),
					new ExpectedCellValue(sheetName, 14, 7, null),
					new ExpectedCellValue(sheetName, 15, 7, null),
					new ExpectedCellValue(sheetName, 16, 7, null),
					new ExpectedCellValue(sheetName, 17, 7, null),
					new ExpectedCellValue(sheetName, 18, 7, null),
					new ExpectedCellValue(sheetName, 19, 7, null),
					new ExpectedCellValue(sheetName, 20, 7, null),
					new ExpectedCellValue(sheetName, 21, 7, null),
					new ExpectedCellValue(sheetName, 22, 7, null),
					new ExpectedCellValue(sheetName, 23, 7, null),
					new ExpectedCellValue(sheetName, 24, 7, null),
					new ExpectedCellValue(sheetName, 25, 7, null),
					new ExpectedCellValue(sheetName, 26, 7, null),
					new ExpectedCellValue(sheetName, 27, 7, null),
					new ExpectedCellValue(sheetName, 28, 7, null),
					new ExpectedCellValue(sheetName, 29, 7, null),
					new ExpectedCellValue(sheetName, 30, 7, null),
					new ExpectedCellValue(sheetName, 31, 7, null),
					new ExpectedCellValue(sheetName, 32, 7, null),
					new ExpectedCellValue(sheetName, 33, 7, null),
					new ExpectedCellValue(sheetName, 34, 7, null),
					new ExpectedCellValue(sheetName, 35, 7, null),
					new ExpectedCellValue(sheetName, 36, 7, null),
					new ExpectedCellValue(sheetName, 37, 7, null),
					new ExpectedCellValue(sheetName, 38, 7, 415.75),
					new ExpectedCellValue(sheetName, 39, 7, null),
					new ExpectedCellValue(sheetName, 40, 7, 1d),
					new ExpectedCellValue(sheetName, 41, 7, null),
					new ExpectedCellValue(sheetName, 42, 7, 415.75),
					new ExpectedCellValue(sheetName, 43, 7, 415.75),
					new ExpectedCellValue(sheetName, 44, 7, 1),
					new ExpectedCellValue(sheetName, 45, 7, 415.75),
					new ExpectedCellValue(sheetName, 46, 7, null),
					new ExpectedCellValue(sheetName, 47, 7, null),
					new ExpectedCellValue(sheetName, 48, 7, null),
					new ExpectedCellValue(sheetName, 49, 7, null),
					new ExpectedCellValue(sheetName, 50, 7, null),
					new ExpectedCellValue(sheetName, 51, 7, null),
					new ExpectedCellValue(sheetName, 52, 7, null),
					new ExpectedCellValue(sheetName, 53, 7, null),
					new ExpectedCellValue(sheetName, 54, 7, null),
					new ExpectedCellValue(sheetName, 55, 7, null),
					new ExpectedCellValue(sheetName, 56, 7, null),
					new ExpectedCellValue(sheetName, 57, 7, null),
					new ExpectedCellValue(sheetName, 58, 7, null),
					new ExpectedCellValue(sheetName, 59, 7, null),
					new ExpectedCellValue(sheetName, 60, 7, null),
					new ExpectedCellValue(sheetName, 61, 7, null),
					new ExpectedCellValue(sheetName, 62, 7, null),
					new ExpectedCellValue(sheetName, 63, 7, null),
					new ExpectedCellValue(sheetName, 64, 7, null),
					new ExpectedCellValue(sheetName, 65, 7, null),
					new ExpectedCellValue(sheetName, 66, 7, null),
					new ExpectedCellValue(sheetName, 67, 7, null),
					new ExpectedCellValue(sheetName, 68, 7, null),
					new ExpectedCellValue(sheetName, 69, 7, null),
					new ExpectedCellValue(sheetName, 70, 7, null),
					new ExpectedCellValue(sheetName, 71, 7, null),
					new ExpectedCellValue(sheetName, 72, 7, null),
					new ExpectedCellValue(sheetName, 73, 7, null),
					new ExpectedCellValue(sheetName, 74, 7, null),
					new ExpectedCellValue(sheetName, 75, 7, null),
					new ExpectedCellValue(sheetName, 76, 7, 415.75),
					new ExpectedCellValue(sheetName, 77, 7, 1d),
					new ExpectedCellValue(sheetName, 78, 7, 415.75),
					new ExpectedCellValue(sheetName, 2, 8, null),
					new ExpectedCellValue(sheetName, 3, 8, "San Francisco Total"),
					new ExpectedCellValue(sheetName, 4, 8, null),
					new ExpectedCellValue(sheetName, 5, 8, null),
					new ExpectedCellValue(sheetName, 6, 8, null),
					new ExpectedCellValue(sheetName, 7, 8, null),
					new ExpectedCellValue(sheetName, 8, 8, null),
					new ExpectedCellValue(sheetName, 9, 8, null),
					new ExpectedCellValue(sheetName, 10, 8, null),
					new ExpectedCellValue(sheetName, 11, 8, null),
					new ExpectedCellValue(sheetName, 12, 8, null),
					new ExpectedCellValue(sheetName, 13, 8, null),
					new ExpectedCellValue(sheetName, 14, 8, null),
					new ExpectedCellValue(sheetName, 15, 8, null),
					new ExpectedCellValue(sheetName, 16, 8, null),
					new ExpectedCellValue(sheetName, 17, 8, null),
					new ExpectedCellValue(sheetName, 18, 8, null),
					new ExpectedCellValue(sheetName, 19, 8, null),
					new ExpectedCellValue(sheetName, 20, 8, null),
					new ExpectedCellValue(sheetName, 21, 8, null),
					new ExpectedCellValue(sheetName, 22, 8, null),
					new ExpectedCellValue(sheetName, 23, 8, null),
					new ExpectedCellValue(sheetName, 24, 8, null),
					new ExpectedCellValue(sheetName, 25, 8, null),
					new ExpectedCellValue(sheetName, 26, 8, null),
					new ExpectedCellValue(sheetName, 27, 8, null),
					new ExpectedCellValue(sheetName, 28, 8, null),
					new ExpectedCellValue(sheetName, 29, 8, null),
					new ExpectedCellValue(sheetName, 30, 8, null),
					new ExpectedCellValue(sheetName, 31, 8, null),
					new ExpectedCellValue(sheetName, 32, 8, null),
					new ExpectedCellValue(sheetName, 33, 8, null),
					new ExpectedCellValue(sheetName, 34, 8, null),
					new ExpectedCellValue(sheetName, 35, 8, null),
					new ExpectedCellValue(sheetName, 36, 8, null),
					new ExpectedCellValue(sheetName, 37, 8, null),
					new ExpectedCellValue(sheetName, 38, 8, 415.75),
					new ExpectedCellValue(sheetName, 39, 8, null),
					new ExpectedCellValue(sheetName, 40, 8, 1d),
					new ExpectedCellValue(sheetName, 41, 8, null),
					new ExpectedCellValue(sheetName, 42, 8, 415.75),
					new ExpectedCellValue(sheetName, 43, 8, 415.75),
					new ExpectedCellValue(sheetName, 44, 8, 1),
					new ExpectedCellValue(sheetName, 45, 8, 415.75),
					new ExpectedCellValue(sheetName, 46, 8, null),
					new ExpectedCellValue(sheetName, 47, 8, null),
					new ExpectedCellValue(sheetName, 48, 8, null),
					new ExpectedCellValue(sheetName, 49, 8, null),
					new ExpectedCellValue(sheetName, 50, 8, null),
					new ExpectedCellValue(sheetName, 51, 8, null),
					new ExpectedCellValue(sheetName, 52, 8, null),
					new ExpectedCellValue(sheetName, 53, 8, null),
					new ExpectedCellValue(sheetName, 54, 8, null),
					new ExpectedCellValue(sheetName, 55, 8, null),
					new ExpectedCellValue(sheetName, 56, 8, null),
					new ExpectedCellValue(sheetName, 57, 8, null),
					new ExpectedCellValue(sheetName, 58, 8, null),
					new ExpectedCellValue(sheetName, 59, 8, null),
					new ExpectedCellValue(sheetName, 60, 8, null),
					new ExpectedCellValue(sheetName, 61, 8, null),
					new ExpectedCellValue(sheetName, 62, 8, null),
					new ExpectedCellValue(sheetName, 63, 8, null),
					new ExpectedCellValue(sheetName, 64, 8, null),
					new ExpectedCellValue(sheetName, 65, 8, null),
					new ExpectedCellValue(sheetName, 66, 8, null),
					new ExpectedCellValue(sheetName, 67, 8, null),
					new ExpectedCellValue(sheetName, 68, 8, null),
					new ExpectedCellValue(sheetName, 69, 8, null),
					new ExpectedCellValue(sheetName, 70, 8, null),
					new ExpectedCellValue(sheetName, 71, 8, null),
					new ExpectedCellValue(sheetName, 72, 8, null),
					new ExpectedCellValue(sheetName, 73, 8, null),
					new ExpectedCellValue(sheetName, 74, 8, null),
					new ExpectedCellValue(sheetName, 75, 8, null),
					new ExpectedCellValue(sheetName, 76, 8, 415.75),
					new ExpectedCellValue(sheetName, 77, 8, 1d),
					new ExpectedCellValue(sheetName, 78, 8, 415.75),
					new ExpectedCellValue(sheetName, 2, 9, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 3, 9, null),
					new ExpectedCellValue(sheetName, 4, 9, null),
					new ExpectedCellValue(sheetName, 5, 9, null),
					new ExpectedCellValue(sheetName, 6, 9, null),
					new ExpectedCellValue(sheetName, 7, 9, null),
					new ExpectedCellValue(sheetName, 8, 9, 415.75),
					new ExpectedCellValue(sheetName, 9, 9, null),
					new ExpectedCellValue(sheetName, 10, 9, 2d),
					new ExpectedCellValue(sheetName, 11, 9, null),
					new ExpectedCellValue(sheetName, 12, 9, 831.5),
					new ExpectedCellValue(sheetName, 13, 9, 415.75),
					new ExpectedCellValue(sheetName, 14, 9, 2d),
					new ExpectedCellValue(sheetName, 15, 9, 831.5),
					new ExpectedCellValue(sheetName, 16, 9, null),
					new ExpectedCellValue(sheetName, 17, 9, null),
					new ExpectedCellValue(sheetName, 18, 9, 415.75),
					new ExpectedCellValue(sheetName, 19, 9, null),
					new ExpectedCellValue(sheetName, 20, 9, 2d),
					new ExpectedCellValue(sheetName, 21, 9, null),
					new ExpectedCellValue(sheetName, 22, 9, 831.5),
					new ExpectedCellValue(sheetName, 23, 9, 415.75),
					new ExpectedCellValue(sheetName, 24, 9, 2d),
					new ExpectedCellValue(sheetName, 25, 9, 831.5),
					new ExpectedCellValue(sheetName, 26, 9, null),
					new ExpectedCellValue(sheetName, 27, 9, null),
					new ExpectedCellValue(sheetName, 28, 9, null),
					new ExpectedCellValue(sheetName, 29, 9, null),
					new ExpectedCellValue(sheetName, 30, 9, null),
					new ExpectedCellValue(sheetName, 31, 9, null),
					new ExpectedCellValue(sheetName, 32, 9, null),
					new ExpectedCellValue(sheetName, 33, 9, null),
					new ExpectedCellValue(sheetName, 34, 9, null),
					new ExpectedCellValue(sheetName, 35, 9, null),
					new ExpectedCellValue(sheetName, 36, 9, null),
					new ExpectedCellValue(sheetName, 37, 9, null),
					new ExpectedCellValue(sheetName, 38, 9, 415.75),
					new ExpectedCellValue(sheetName, 39, 9, null),
					new ExpectedCellValue(sheetName, 40, 9, 1d),
					new ExpectedCellValue(sheetName, 41, 9, null),
					new ExpectedCellValue(sheetName, 42, 9, 415.75),
					new ExpectedCellValue(sheetName, 43, 9, 415.75),
					new ExpectedCellValue(sheetName, 44, 9, 1d),
					new ExpectedCellValue(sheetName, 45, 9, 415.75),
					new ExpectedCellValue(sheetName, 46, 9, null),
					new ExpectedCellValue(sheetName, 47, 9, null),
					new ExpectedCellValue(sheetName, 48, 9, null),
					new ExpectedCellValue(sheetName, 49, 9, null),
					new ExpectedCellValue(sheetName, 50, 9, null),
					new ExpectedCellValue(sheetName, 51, 9, null),
					new ExpectedCellValue(sheetName, 52, 9, null),
					new ExpectedCellValue(sheetName, 53, 9, null),
					new ExpectedCellValue(sheetName, 54, 9, null),
					new ExpectedCellValue(sheetName, 55, 9, null),
					new ExpectedCellValue(sheetName, 56, 9, null),
					new ExpectedCellValue(sheetName, 57, 9, null),
					new ExpectedCellValue(sheetName, 58, 9, null),
					new ExpectedCellValue(sheetName, 59, 9, null),
					new ExpectedCellValue(sheetName, 60, 9, null),
					new ExpectedCellValue(sheetName, 61, 9, null),
					new ExpectedCellValue(sheetName, 62, 9, null),
					new ExpectedCellValue(sheetName, 63, 9, null),
					new ExpectedCellValue(sheetName, 64, 9, null),
					new ExpectedCellValue(sheetName, 65, 9, null),
					new ExpectedCellValue(sheetName, 66, 9, null),
					new ExpectedCellValue(sheetName, 67, 9, null),
					new ExpectedCellValue(sheetName, 68, 9, 415.75),
					new ExpectedCellValue(sheetName, 69, 9, null),
					new ExpectedCellValue(sheetName, 70, 9, 2d),
					new ExpectedCellValue(sheetName, 71, 9, null),
					new ExpectedCellValue(sheetName, 72, 9, 831.5),
					new ExpectedCellValue(sheetName, 73, 9, 415.75),
					new ExpectedCellValue(sheetName, 74, 9, 2d),
					new ExpectedCellValue(sheetName, 75, 9, 831.5),
					new ExpectedCellValue(sheetName, 76, 9, 1663d),
					new ExpectedCellValue(sheetName, 77, 9, 7d),
					new ExpectedCellValue(sheetName, 78, 9, 2910.25),
					new ExpectedCellValue(sheetName, 2, 10, "Headlamp"),
					new ExpectedCellValue(sheetName, 3, 10, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 10, 2018),
					new ExpectedCellValue(sheetName, 5, 10, "Qtr1"),
					new ExpectedCellValue(sheetName, 6, 10, null),
					new ExpectedCellValue(sheetName, 7, 10, null),
					new ExpectedCellValue(sheetName, 8, 10, null),
					new ExpectedCellValue(sheetName, 9, 10, null),
					new ExpectedCellValue(sheetName, 10, 10, null),
					new ExpectedCellValue(sheetName, 11, 10, null),
					new ExpectedCellValue(sheetName, 12, 10, null),
					new ExpectedCellValue(sheetName, 13, 10, null),
					new ExpectedCellValue(sheetName, 14, 10, null),
					new ExpectedCellValue(sheetName, 15, 10, null),
					new ExpectedCellValue(sheetName, 16, 10, null),
					new ExpectedCellValue(sheetName, 17, 10, null),
					new ExpectedCellValue(sheetName, 18, 10, null),
					new ExpectedCellValue(sheetName, 19, 10, null),
					new ExpectedCellValue(sheetName, 20, 10, null),
					new ExpectedCellValue(sheetName, 21, 10, null),
					new ExpectedCellValue(sheetName, 22, 10, null),
					new ExpectedCellValue(sheetName, 23, 10, null),
					new ExpectedCellValue(sheetName, 24, 10, null),
					new ExpectedCellValue(sheetName, 25, 10, null),
					new ExpectedCellValue(sheetName, 26, 10, null),
					new ExpectedCellValue(sheetName, 27, 10, null),
					new ExpectedCellValue(sheetName, 28, 10, null),
					new ExpectedCellValue(sheetName, 29, 10, null),
					new ExpectedCellValue(sheetName, 30, 10, null),
					new ExpectedCellValue(sheetName, 31, 10, null),
					new ExpectedCellValue(sheetName, 32, 10, null),
					new ExpectedCellValue(sheetName, 33, 10, null),
					new ExpectedCellValue(sheetName, 34, 10, null),
					new ExpectedCellValue(sheetName, 35, 10, null),
					new ExpectedCellValue(sheetName, 36, 10, null),
					new ExpectedCellValue(sheetName, 37, 10, null),
					new ExpectedCellValue(sheetName, 38, 10, null),
					new ExpectedCellValue(sheetName, 39, 10, null),
					new ExpectedCellValue(sheetName, 40, 10, null),
					new ExpectedCellValue(sheetName, 41, 10, null),
					new ExpectedCellValue(sheetName, 42, 10, null),
					new ExpectedCellValue(sheetName, 43, 10, null),
					new ExpectedCellValue(sheetName, 44, 10, null),
					new ExpectedCellValue(sheetName, 45, 10, null),
					new ExpectedCellValue(sheetName, 46, 10, null),
					new ExpectedCellValue(sheetName, 47, 10, null),
					new ExpectedCellValue(sheetName, 48, 10, 24.99),
					new ExpectedCellValue(sheetName, 49, 10, null),
					new ExpectedCellValue(sheetName, 50, 10, 1d),
					new ExpectedCellValue(sheetName, 51, 10, null),
					new ExpectedCellValue(sheetName, 52, 10, 24.99),
					new ExpectedCellValue(sheetName, 53, 10, 24.99),
					new ExpectedCellValue(sheetName, 54, 10, 1d),
					new ExpectedCellValue(sheetName, 55, 10, 24.99),
					new ExpectedCellValue(sheetName, 56, 10, null),
					new ExpectedCellValue(sheetName, 57, 10, null),
					new ExpectedCellValue(sheetName, 58, 10, null),
					new ExpectedCellValue(sheetName, 59, 10, null),
					new ExpectedCellValue(sheetName, 60, 10, null),
					new ExpectedCellValue(sheetName, 61, 10, null),
					new ExpectedCellValue(sheetName, 62, 10, null),
					new ExpectedCellValue(sheetName, 63, 10, null),
					new ExpectedCellValue(sheetName, 64, 10, null),
					new ExpectedCellValue(sheetName, 65, 10, null),
					new ExpectedCellValue(sheetName, 66, 10, null),
					new ExpectedCellValue(sheetName, 67, 10, null),
					new ExpectedCellValue(sheetName, 68, 10, null),
					new ExpectedCellValue(sheetName, 69, 10, null),
					new ExpectedCellValue(sheetName, 70, 10, null),
					new ExpectedCellValue(sheetName, 71, 10, null),
					new ExpectedCellValue(sheetName, 72, 10, null),
					new ExpectedCellValue(sheetName, 73, 10, null),
					new ExpectedCellValue(sheetName, 74, 10, null),
					new ExpectedCellValue(sheetName, 75, 10, null),
					new ExpectedCellValue(sheetName, 76, 10, 24.99),
					new ExpectedCellValue(sheetName, 77, 10, 1d),
					new ExpectedCellValue(sheetName, 78, 10, 24.99),
					new ExpectedCellValue(sheetName, 2, 11, null),
					new ExpectedCellValue(sheetName, 3, 11, "Chicago Total"),
					new ExpectedCellValue(sheetName, 4, 11, null),
					new ExpectedCellValue(sheetName, 5, 11, null),
					new ExpectedCellValue(sheetName, 6, 11, null),
					new ExpectedCellValue(sheetName, 7, 11, null),
					new ExpectedCellValue(sheetName, 8, 11, null),
					new ExpectedCellValue(sheetName, 9, 11, null),
					new ExpectedCellValue(sheetName, 10, 11, null),
					new ExpectedCellValue(sheetName, 11, 11, null),
					new ExpectedCellValue(sheetName, 12, 11, null),
					new ExpectedCellValue(sheetName, 13, 11, null),
					new ExpectedCellValue(sheetName, 14, 11, null),
					new ExpectedCellValue(sheetName, 15, 11, null),
					new ExpectedCellValue(sheetName, 16, 11, null),
					new ExpectedCellValue(sheetName, 17, 11, null),
					new ExpectedCellValue(sheetName, 18, 11, null),
					new ExpectedCellValue(sheetName, 19, 11, null),
					new ExpectedCellValue(sheetName, 20, 11, null),
					new ExpectedCellValue(sheetName, 21, 11, null),
					new ExpectedCellValue(sheetName, 22, 11, null),
					new ExpectedCellValue(sheetName, 23, 11, null),
					new ExpectedCellValue(sheetName, 24, 11, null),
					new ExpectedCellValue(sheetName, 25, 11, null),
					new ExpectedCellValue(sheetName, 26, 11, null),
					new ExpectedCellValue(sheetName, 27, 11, null),
					new ExpectedCellValue(sheetName, 28, 11, null),
					new ExpectedCellValue(sheetName, 29, 11, null),
					new ExpectedCellValue(sheetName, 30, 11, null),
					new ExpectedCellValue(sheetName, 31, 11, null),
					new ExpectedCellValue(sheetName, 32, 11, null),
					new ExpectedCellValue(sheetName, 33, 11, null),
					new ExpectedCellValue(sheetName, 34, 11, null),
					new ExpectedCellValue(sheetName, 35, 11, null),
					new ExpectedCellValue(sheetName, 36, 11, null),
					new ExpectedCellValue(sheetName, 37, 11, null),
					new ExpectedCellValue(sheetName, 38, 11, null),
					new ExpectedCellValue(sheetName, 39, 11, null),
					new ExpectedCellValue(sheetName, 40, 11, null),
					new ExpectedCellValue(sheetName, 41, 11, null),
					new ExpectedCellValue(sheetName, 42, 11, null),
					new ExpectedCellValue(sheetName, 43, 11, null),
					new ExpectedCellValue(sheetName, 44, 11, null),
					new ExpectedCellValue(sheetName, 45, 11, null),
					new ExpectedCellValue(sheetName, 46, 11, null),
					new ExpectedCellValue(sheetName, 47, 11, null),
					new ExpectedCellValue(sheetName, 48, 11, 24.99),
					new ExpectedCellValue(sheetName, 49, 11, null),
					new ExpectedCellValue(sheetName, 50, 11, 1d),
					new ExpectedCellValue(sheetName, 51, 11, null),
					new ExpectedCellValue(sheetName, 52, 11, 24.99),
					new ExpectedCellValue(sheetName, 53, 11, 24.99),
					new ExpectedCellValue(sheetName, 54, 11, 1d),
					new ExpectedCellValue(sheetName, 55, 11, 24.99),
					new ExpectedCellValue(sheetName, 56, 11, null),
					new ExpectedCellValue(sheetName, 57, 11, null),
					new ExpectedCellValue(sheetName, 58, 11, null),
					new ExpectedCellValue(sheetName, 59, 11, null),
					new ExpectedCellValue(sheetName, 60, 11, null),
					new ExpectedCellValue(sheetName, 61, 11, null),
					new ExpectedCellValue(sheetName, 62, 11, null),
					new ExpectedCellValue(sheetName, 63, 11, null),
					new ExpectedCellValue(sheetName, 64, 11, null),
					new ExpectedCellValue(sheetName, 65, 11, null),
					new ExpectedCellValue(sheetName, 66, 11, null),
					new ExpectedCellValue(sheetName, 67, 11, null),
					new ExpectedCellValue(sheetName, 68, 11, null),
					new ExpectedCellValue(sheetName, 69, 11, null),
					new ExpectedCellValue(sheetName, 70, 11, null),
					new ExpectedCellValue(sheetName, 71, 11, null),
					new ExpectedCellValue(sheetName, 72, 11, null),
					new ExpectedCellValue(sheetName, 73, 11, null),
					new ExpectedCellValue(sheetName, 74, 11, null),
					new ExpectedCellValue(sheetName, 75, 11, null),
					new ExpectedCellValue(sheetName, 76, 11, 24.99),
					new ExpectedCellValue(sheetName, 77, 11, 1d),
					new ExpectedCellValue(sheetName, 78, 11, 24.99),
					new ExpectedCellValue(sheetName, 2, 12, "Headlamp Total"),
					new ExpectedCellValue(sheetName, 3, 12, null),
					new ExpectedCellValue(sheetName, 4, 12, null),
					new ExpectedCellValue(sheetName, 5, 12, null),
					new ExpectedCellValue(sheetName, 6, 12, null),
					new ExpectedCellValue(sheetName, 7, 12, null),
					new ExpectedCellValue(sheetName, 8, 12, null),
					new ExpectedCellValue(sheetName, 9, 12, null),
					new ExpectedCellValue(sheetName, 10, 12, null),
					new ExpectedCellValue(sheetName, 11, 12, null),
					new ExpectedCellValue(sheetName, 12, 12, null),
					new ExpectedCellValue(sheetName, 13, 12, null),
					new ExpectedCellValue(sheetName, 14, 12, null),
					new ExpectedCellValue(sheetName, 15, 12, null),
					new ExpectedCellValue(sheetName, 16, 12, null),
					new ExpectedCellValue(sheetName, 17, 12, null),
					new ExpectedCellValue(sheetName, 18, 12, null),
					new ExpectedCellValue(sheetName, 19, 12, null),
					new ExpectedCellValue(sheetName, 20, 12, null),
					new ExpectedCellValue(sheetName, 21, 12, null),
					new ExpectedCellValue(sheetName, 22, 12, null),
					new ExpectedCellValue(sheetName, 23, 12, null),
					new ExpectedCellValue(sheetName, 24, 12, null),
					new ExpectedCellValue(sheetName, 25, 12, null),
					new ExpectedCellValue(sheetName, 26, 12, null),
					new ExpectedCellValue(sheetName, 27, 12, null),
					new ExpectedCellValue(sheetName, 28, 12, null),
					new ExpectedCellValue(sheetName, 29, 12, null),
					new ExpectedCellValue(sheetName, 30, 12, null),
					new ExpectedCellValue(sheetName, 31, 12, null),
					new ExpectedCellValue(sheetName, 32, 12, null),
					new ExpectedCellValue(sheetName, 33, 12, null),
					new ExpectedCellValue(sheetName, 34, 12, null),
					new ExpectedCellValue(sheetName, 35, 12, null),
					new ExpectedCellValue(sheetName, 36, 12, null),
					new ExpectedCellValue(sheetName, 37, 12, null),
					new ExpectedCellValue(sheetName, 38, 12, null),
					new ExpectedCellValue(sheetName, 39, 12, null),
					new ExpectedCellValue(sheetName, 40, 12, null),
					new ExpectedCellValue(sheetName, 41, 12, null),
					new ExpectedCellValue(sheetName, 42, 12, null),
					new ExpectedCellValue(sheetName, 43, 12, null),
					new ExpectedCellValue(sheetName, 44, 12, null),
					new ExpectedCellValue(sheetName, 45, 12, null),
					new ExpectedCellValue(sheetName, 46, 12, null),
					new ExpectedCellValue(sheetName, 47, 12, null),
					new ExpectedCellValue(sheetName, 48, 12, 24.99),
					new ExpectedCellValue(sheetName, 49, 12, null),
					new ExpectedCellValue(sheetName, 50, 12, 1d),
					new ExpectedCellValue(sheetName, 51, 12, null),
					new ExpectedCellValue(sheetName, 52, 12, 24.99),
					new ExpectedCellValue(sheetName, 53, 12, 24.99),
					new ExpectedCellValue(sheetName, 54, 12, 1d),
					new ExpectedCellValue(sheetName, 55, 12, 24.99),
					new ExpectedCellValue(sheetName, 56, 12, null),
					new ExpectedCellValue(sheetName, 57, 12, null),
					new ExpectedCellValue(sheetName, 58, 12, null),
					new ExpectedCellValue(sheetName, 59, 12, null),
					new ExpectedCellValue(sheetName, 60, 12, null),
					new ExpectedCellValue(sheetName, 61, 12, null),
					new ExpectedCellValue(sheetName, 62, 12, null),
					new ExpectedCellValue(sheetName, 63, 12, null),
					new ExpectedCellValue(sheetName, 64, 12, null),
					new ExpectedCellValue(sheetName, 65, 12, null),
					new ExpectedCellValue(sheetName, 66, 12, null),
					new ExpectedCellValue(sheetName, 67, 12, null),
					new ExpectedCellValue(sheetName, 68, 12, null),
					new ExpectedCellValue(sheetName, 69, 12, null),
					new ExpectedCellValue(sheetName, 70, 12, null),
					new ExpectedCellValue(sheetName, 71, 12, null),
					new ExpectedCellValue(sheetName, 72, 12, null),
					new ExpectedCellValue(sheetName, 73, 12, null),
					new ExpectedCellValue(sheetName, 74, 12, null),
					new ExpectedCellValue(sheetName, 75, 12, null),
					new ExpectedCellValue(sheetName, 76, 12, 24.99),
					new ExpectedCellValue(sheetName, 77, 12, 1d),
					new ExpectedCellValue(sheetName, 78, 12, 24.99),
					new ExpectedCellValue(sheetName, 2, 13, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 3, 13, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 13, 2017),
					new ExpectedCellValue(sheetName, 5, 13, "Qtr4"),
					new ExpectedCellValue(sheetName, 6, 13, null),
					new ExpectedCellValue(sheetName, 7, 13, null),
					new ExpectedCellValue(sheetName, 8, 13, null),
					new ExpectedCellValue(sheetName, 9, 13, null),
					new ExpectedCellValue(sheetName, 10, 13, null),
					new ExpectedCellValue(sheetName, 11, 13, null),
					new ExpectedCellValue(sheetName, 12, 13, null),
					new ExpectedCellValue(sheetName, 13, 13, null),
					new ExpectedCellValue(sheetName, 14, 13, null),
					new ExpectedCellValue(sheetName, 15, 13, null),
					new ExpectedCellValue(sheetName, 16, 13, null),
					new ExpectedCellValue(sheetName, 17, 13, null),
					new ExpectedCellValue(sheetName, 18, 13, null),
					new ExpectedCellValue(sheetName, 19, 13, null),
					new ExpectedCellValue(sheetName, 20, 13, null),
					new ExpectedCellValue(sheetName, 21, 13, null),
					new ExpectedCellValue(sheetName, 22, 13, null),
					new ExpectedCellValue(sheetName, 23, 13, null),
					new ExpectedCellValue(sheetName, 24, 13, null),
					new ExpectedCellValue(sheetName, 25, 13, null),
					new ExpectedCellValue(sheetName, 26, 13, null),
					new ExpectedCellValue(sheetName, 27, 13, null),
					new ExpectedCellValue(sheetName, 28, 13, null),
					new ExpectedCellValue(sheetName, 29, 13, null),
					new ExpectedCellValue(sheetName, 30, 13, null),
					new ExpectedCellValue(sheetName, 31, 13, null),
					new ExpectedCellValue(sheetName, 32, 13, null),
					new ExpectedCellValue(sheetName, 33, 13, null),
					new ExpectedCellValue(sheetName, 34, 13, null),
					new ExpectedCellValue(sheetName, 35, 13, null),
					new ExpectedCellValue(sheetName, 36, 13, null),
					new ExpectedCellValue(sheetName, 37, 13, null),
					new ExpectedCellValue(sheetName, 38, 13, null),
					new ExpectedCellValue(sheetName, 39, 13, null),
					new ExpectedCellValue(sheetName, 40, 13, null),
					new ExpectedCellValue(sheetName, 41, 13, null),
					new ExpectedCellValue(sheetName, 42, 13, null),
					new ExpectedCellValue(sheetName, 43, 13, null),
					new ExpectedCellValue(sheetName, 44, 13, null),
					new ExpectedCellValue(sheetName, 45, 13, null),
					new ExpectedCellValue(sheetName, 46, 13, null),
					new ExpectedCellValue(sheetName, 47, 13, null),
					new ExpectedCellValue(sheetName, 48, 13, null),
					new ExpectedCellValue(sheetName, 49, 13, null),
					new ExpectedCellValue(sheetName, 50, 13, null),
					new ExpectedCellValue(sheetName, 51, 13, null),
					new ExpectedCellValue(sheetName, 52, 13, null),
					new ExpectedCellValue(sheetName, 53, 13, null),
					new ExpectedCellValue(sheetName, 54, 13, null),
					new ExpectedCellValue(sheetName, 55, 13, null),
					new ExpectedCellValue(sheetName, 56, 13, null),
					new ExpectedCellValue(sheetName, 57, 13, null),
					new ExpectedCellValue(sheetName, 58, 13, 99d),
					new ExpectedCellValue(sheetName, 59, 13, null),
					new ExpectedCellValue(sheetName, 60, 13, 1d),
					new ExpectedCellValue(sheetName, 61, 13, null),
					new ExpectedCellValue(sheetName, 62, 13, 99d),
					new ExpectedCellValue(sheetName, 63, 13, 99d),
					new ExpectedCellValue(sheetName, 64, 13, 1d),
					new ExpectedCellValue(sheetName, 65, 13, 99d),
					new ExpectedCellValue(sheetName, 66, 13, null),
					new ExpectedCellValue(sheetName, 67, 13, null),
					new ExpectedCellValue(sheetName, 68, 13, null),
					new ExpectedCellValue(sheetName, 69, 13, null),
					new ExpectedCellValue(sheetName, 70, 13, null),
					new ExpectedCellValue(sheetName, 71, 13, null),
					new ExpectedCellValue(sheetName, 72, 13, null),
					new ExpectedCellValue(sheetName, 73, 13, null),
					new ExpectedCellValue(sheetName, 74, 13, null),
					new ExpectedCellValue(sheetName, 75, 13, null),
					new ExpectedCellValue(sheetName, 76, 13, 99d),
					new ExpectedCellValue(sheetName, 77, 13, 1d),
					new ExpectedCellValue(sheetName, 78, 13, 99d),
					new ExpectedCellValue(sheetName, 2, 14, null),
					new ExpectedCellValue(sheetName, 3, 14, "San Francisco Total"),
					new ExpectedCellValue(sheetName, 4, 14, null),
					new ExpectedCellValue(sheetName, 5, 14, null),
					new ExpectedCellValue(sheetName, 6, 14, null),
					new ExpectedCellValue(sheetName, 7, 14, null),
					new ExpectedCellValue(sheetName, 8, 14, null),
					new ExpectedCellValue(sheetName, 9, 14, null),
					new ExpectedCellValue(sheetName, 10, 14, null),
					new ExpectedCellValue(sheetName, 11, 14, null),
					new ExpectedCellValue(sheetName, 12, 14, null),
					new ExpectedCellValue(sheetName, 13, 14, null),
					new ExpectedCellValue(sheetName, 14, 14, null),
					new ExpectedCellValue(sheetName, 15, 14, null),
					new ExpectedCellValue(sheetName, 16, 14, null),
					new ExpectedCellValue(sheetName, 17, 14, null),
					new ExpectedCellValue(sheetName, 18, 14, null),
					new ExpectedCellValue(sheetName, 19, 14, null),
					new ExpectedCellValue(sheetName, 20, 14, null),
					new ExpectedCellValue(sheetName, 21, 14, null),
					new ExpectedCellValue(sheetName, 22, 14, null),
					new ExpectedCellValue(sheetName, 23, 14, null),
					new ExpectedCellValue(sheetName, 24, 14, null),
					new ExpectedCellValue(sheetName, 25, 14, null),
					new ExpectedCellValue(sheetName, 26, 14, null),
					new ExpectedCellValue(sheetName, 27, 14, null),
					new ExpectedCellValue(sheetName, 28, 14, null),
					new ExpectedCellValue(sheetName, 29, 14, null),
					new ExpectedCellValue(sheetName, 30, 14, null),
					new ExpectedCellValue(sheetName, 31, 14, null),
					new ExpectedCellValue(sheetName, 32, 14, null),
					new ExpectedCellValue(sheetName, 33, 14, null),
					new ExpectedCellValue(sheetName, 34, 14, null),
					new ExpectedCellValue(sheetName, 35, 14, null),
					new ExpectedCellValue(sheetName, 36, 14, null),
					new ExpectedCellValue(sheetName, 37, 14, null),
					new ExpectedCellValue(sheetName, 38, 14, null),
					new ExpectedCellValue(sheetName, 39, 14, null),
					new ExpectedCellValue(sheetName, 40, 14, null),
					new ExpectedCellValue(sheetName, 41, 14, null),
					new ExpectedCellValue(sheetName, 42, 14, null),
					new ExpectedCellValue(sheetName, 43, 14, null),
					new ExpectedCellValue(sheetName, 44, 14, null),
					new ExpectedCellValue(sheetName, 45, 14, null),
					new ExpectedCellValue(sheetName, 46, 14, null),
					new ExpectedCellValue(sheetName, 47, 14, null),
					new ExpectedCellValue(sheetName, 48, 14, null),
					new ExpectedCellValue(sheetName, 49, 14, null),
					new ExpectedCellValue(sheetName, 50, 14, null),
					new ExpectedCellValue(sheetName, 51, 14, null),
					new ExpectedCellValue(sheetName, 52, 14, null),
					new ExpectedCellValue(sheetName, 53, 14, null),
					new ExpectedCellValue(sheetName, 54, 14, null),
					new ExpectedCellValue(sheetName, 55, 14, null),
					new ExpectedCellValue(sheetName, 56, 14, null),
					new ExpectedCellValue(sheetName, 57, 14, null),
					new ExpectedCellValue(sheetName, 58, 14, 99d),
					new ExpectedCellValue(sheetName, 59, 14, null),
					new ExpectedCellValue(sheetName, 60, 14, 1d),
					new ExpectedCellValue(sheetName, 61, 14, null),
					new ExpectedCellValue(sheetName, 62, 14, 99d),
					new ExpectedCellValue(sheetName, 63, 14, 99d),
					new ExpectedCellValue(sheetName, 64, 14, 1d),
					new ExpectedCellValue(sheetName, 65, 14, 99d),
					new ExpectedCellValue(sheetName, 66, 14, null),
					new ExpectedCellValue(sheetName, 67, 14, null),
					new ExpectedCellValue(sheetName, 68, 14, null),
					new ExpectedCellValue(sheetName, 69, 14, null),
					new ExpectedCellValue(sheetName, 70, 14, null),
					new ExpectedCellValue(sheetName, 71, 14, null),
					new ExpectedCellValue(sheetName, 72, 14, null),
					new ExpectedCellValue(sheetName, 73, 14, null),
					new ExpectedCellValue(sheetName, 74, 14, null),
					new ExpectedCellValue(sheetName, 75, 14, null),
					new ExpectedCellValue(sheetName, 76, 14, 99d),
					new ExpectedCellValue(sheetName, 77, 14, 1d),
					new ExpectedCellValue(sheetName, 78, 14, 99d),
					new ExpectedCellValue(sheetName, 2, 15, "Sleeping Bag Total"),
					new ExpectedCellValue(sheetName, 3, 15, null),
					new ExpectedCellValue(sheetName, 4, 15, null),
					new ExpectedCellValue(sheetName, 5, 15, null),
					new ExpectedCellValue(sheetName, 6, 15, null),
					new ExpectedCellValue(sheetName, 7, 15, null),
					new ExpectedCellValue(sheetName, 8, 15, null),
					new ExpectedCellValue(sheetName, 9, 15, null),
					new ExpectedCellValue(sheetName, 10, 15, null),
					new ExpectedCellValue(sheetName, 11, 15, null),
					new ExpectedCellValue(sheetName, 12, 15, null),
					new ExpectedCellValue(sheetName, 13, 15, null),
					new ExpectedCellValue(sheetName, 14, 15, null),
					new ExpectedCellValue(sheetName, 15, 15, null),
					new ExpectedCellValue(sheetName, 16, 15, null),
					new ExpectedCellValue(sheetName, 17, 15, null),
					new ExpectedCellValue(sheetName, 18, 15, null),
					new ExpectedCellValue(sheetName, 19, 15, null),
					new ExpectedCellValue(sheetName, 20, 15, null),
					new ExpectedCellValue(sheetName, 21, 15, null),
					new ExpectedCellValue(sheetName, 22, 15, null),
					new ExpectedCellValue(sheetName, 23, 15, null),
					new ExpectedCellValue(sheetName, 24, 15, null),
					new ExpectedCellValue(sheetName, 25, 15, null),
					new ExpectedCellValue(sheetName, 26, 15, null),
					new ExpectedCellValue(sheetName, 27, 15, null),
					new ExpectedCellValue(sheetName, 28, 15, null),
					new ExpectedCellValue(sheetName, 29, 15, null),
					new ExpectedCellValue(sheetName, 30, 15, null),
					new ExpectedCellValue(sheetName, 31, 15, null),
					new ExpectedCellValue(sheetName, 32, 15, null),
					new ExpectedCellValue(sheetName, 33, 15, null),
					new ExpectedCellValue(sheetName, 34, 15, null),
					new ExpectedCellValue(sheetName, 35, 15, null),
					new ExpectedCellValue(sheetName, 36, 15, null),
					new ExpectedCellValue(sheetName, 37, 15, null),
					new ExpectedCellValue(sheetName, 38, 15, null),
					new ExpectedCellValue(sheetName, 39, 15, null),
					new ExpectedCellValue(sheetName, 40, 15, null),
					new ExpectedCellValue(sheetName, 41, 15, null),
					new ExpectedCellValue(sheetName, 42, 15, null),
					new ExpectedCellValue(sheetName, 43, 15, null),
					new ExpectedCellValue(sheetName, 44, 15, null),
					new ExpectedCellValue(sheetName, 45, 15, null),
					new ExpectedCellValue(sheetName, 46, 15, null),
					new ExpectedCellValue(sheetName, 47, 15, null),
					new ExpectedCellValue(sheetName, 48, 15, null),
					new ExpectedCellValue(sheetName, 49, 15, null),
					new ExpectedCellValue(sheetName, 50, 15, null),
					new ExpectedCellValue(sheetName, 51, 15, null),
					new ExpectedCellValue(sheetName, 52, 15, null),
					new ExpectedCellValue(sheetName, 53, 15, null),
					new ExpectedCellValue(sheetName, 54, 15, null),
					new ExpectedCellValue(sheetName, 55, 15, null),
					new ExpectedCellValue(sheetName, 56, 15, null),
					new ExpectedCellValue(sheetName, 57, 15, null),
					new ExpectedCellValue(sheetName, 58, 15, 99d),
					new ExpectedCellValue(sheetName, 59, 15, null),
					new ExpectedCellValue(sheetName, 60, 15, 1d),
					new ExpectedCellValue(sheetName, 61, 15, null),
					new ExpectedCellValue(sheetName, 62, 15, 99d),
					new ExpectedCellValue(sheetName, 63, 15, 99d),
					new ExpectedCellValue(sheetName, 64, 15, 1d),
					new ExpectedCellValue(sheetName, 65, 15, 99d),
					new ExpectedCellValue(sheetName, 66, 15, null),
					new ExpectedCellValue(sheetName, 67, 15, null),
					new ExpectedCellValue(sheetName, 68, 15, null),
					new ExpectedCellValue(sheetName, 69, 15, null),
					new ExpectedCellValue(sheetName, 70, 15, null),
					new ExpectedCellValue(sheetName, 71, 15, null),
					new ExpectedCellValue(sheetName, 72, 15, null),
					new ExpectedCellValue(sheetName, 73, 15, null),
					new ExpectedCellValue(sheetName, 74, 15, null),
					new ExpectedCellValue(sheetName, 75, 15, null),
					new ExpectedCellValue(sheetName, 76, 15, 99d),
					new ExpectedCellValue(sheetName, 77, 15, 1d),
					new ExpectedCellValue(sheetName, 78, 15, 99d),
					new ExpectedCellValue(sheetName, 2, 16, "Tent"),
					new ExpectedCellValue(sheetName, 3, 16, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 16, 2018),
					new ExpectedCellValue(sheetName, 5, 16, "Qtr2"),
					new ExpectedCellValue(sheetName, 6, 16, null),
					new ExpectedCellValue(sheetName, 7, 16, null),
					new ExpectedCellValue(sheetName, 8, 16, null),
					new ExpectedCellValue(sheetName, 9, 16, null),
					new ExpectedCellValue(sheetName, 10, 16, null),
					new ExpectedCellValue(sheetName, 11, 16, null),
					new ExpectedCellValue(sheetName, 12, 16, null),
					new ExpectedCellValue(sheetName, 13, 16, null),
					new ExpectedCellValue(sheetName, 14, 16, null),
					new ExpectedCellValue(sheetName, 15, 16, null),
					new ExpectedCellValue(sheetName, 16, 16, null),
					new ExpectedCellValue(sheetName, 17, 16, null),
					new ExpectedCellValue(sheetName, 18, 16, null),
					new ExpectedCellValue(sheetName, 19, 16, null),
					new ExpectedCellValue(sheetName, 20, 16, null),
					new ExpectedCellValue(sheetName, 21, 16, null),
					new ExpectedCellValue(sheetName, 22, 16, null),
					new ExpectedCellValue(sheetName, 23, 16, null),
					new ExpectedCellValue(sheetName, 24, 16, null),
					new ExpectedCellValue(sheetName, 25, 16, null),
					new ExpectedCellValue(sheetName, 26, 16, null),
					new ExpectedCellValue(sheetName, 27, 16, null),
					new ExpectedCellValue(sheetName, 28, 16, 199d),
					new ExpectedCellValue(sheetName, 29, 16, null),
					new ExpectedCellValue(sheetName, 30, 16, 6d),
					new ExpectedCellValue(sheetName, 31, 16, null),
					new ExpectedCellValue(sheetName, 32, 16, 1194d),
					new ExpectedCellValue(sheetName, 33, 16, 199d),
					new ExpectedCellValue(sheetName, 34, 16, 6d),
					new ExpectedCellValue(sheetName, 35, 16, 1194d),
					new ExpectedCellValue(sheetName, 36, 16, null),
					new ExpectedCellValue(sheetName, 37, 16, null),
					new ExpectedCellValue(sheetName, 38, 16, null),
					new ExpectedCellValue(sheetName, 39, 16, null),
					new ExpectedCellValue(sheetName, 40, 16, null),
					new ExpectedCellValue(sheetName, 41, 16, null),
					new ExpectedCellValue(sheetName, 42, 16, null),
					new ExpectedCellValue(sheetName, 43, 16, null),
					new ExpectedCellValue(sheetName, 44, 16, null),
					new ExpectedCellValue(sheetName, 45, 16, null),
					new ExpectedCellValue(sheetName, 46, 16, null),
					new ExpectedCellValue(sheetName, 47, 16, null),
					new ExpectedCellValue(sheetName, 48, 16, null),
					new ExpectedCellValue(sheetName, 49, 16, null),
					new ExpectedCellValue(sheetName, 50, 16, null),
					new ExpectedCellValue(sheetName, 51, 16, null),
					new ExpectedCellValue(sheetName, 52, 16, null),
					new ExpectedCellValue(sheetName, 53, 16, null),
					new ExpectedCellValue(sheetName, 54, 16, null),
					new ExpectedCellValue(sheetName, 55, 16, null),
					new ExpectedCellValue(sheetName, 56, 16, null),
					new ExpectedCellValue(sheetName, 57, 16, null),
					new ExpectedCellValue(sheetName, 58, 16, null),
					new ExpectedCellValue(sheetName, 59, 16, null),
					new ExpectedCellValue(sheetName, 60, 16, null),
					new ExpectedCellValue(sheetName, 61, 16, null),
					new ExpectedCellValue(sheetName, 62, 16, null),
					new ExpectedCellValue(sheetName, 63, 16, null),
					new ExpectedCellValue(sheetName, 64, 16, null),
					new ExpectedCellValue(sheetName, 65, 16, null),
					new ExpectedCellValue(sheetName, 66, 16, null),
					new ExpectedCellValue(sheetName, 67, 16, null),
					new ExpectedCellValue(sheetName, 68, 16, null),
					new ExpectedCellValue(sheetName, 69, 16, null),
					new ExpectedCellValue(sheetName, 70, 16, null),
					new ExpectedCellValue(sheetName, 71, 16, null),
					new ExpectedCellValue(sheetName, 72, 16, null),
					new ExpectedCellValue(sheetName, 73, 16, null),
					new ExpectedCellValue(sheetName, 74, 16, null),
					new ExpectedCellValue(sheetName, 75, 16, null),
					new ExpectedCellValue(sheetName, 76, 16, 199d),
					new ExpectedCellValue(sheetName, 77, 16, 6d),
					new ExpectedCellValue(sheetName, 78, 16, 1194d),
					new ExpectedCellValue(sheetName, 2, 17, null),
					new ExpectedCellValue(sheetName, 3, 17, "Nashville Total"),
					new ExpectedCellValue(sheetName, 4, 17, null),
					new ExpectedCellValue(sheetName, 5, 17, null),
					new ExpectedCellValue(sheetName, 6, 17, null),
					new ExpectedCellValue(sheetName, 7, 17, null),
					new ExpectedCellValue(sheetName, 8, 17, null),
					new ExpectedCellValue(sheetName, 9, 17, null),
					new ExpectedCellValue(sheetName, 10, 17, null),
					new ExpectedCellValue(sheetName, 11, 17, null),
					new ExpectedCellValue(sheetName, 12, 17, null),
					new ExpectedCellValue(sheetName, 13, 17, null),
					new ExpectedCellValue(sheetName, 14, 17, null),
					new ExpectedCellValue(sheetName, 15, 17, null),
					new ExpectedCellValue(sheetName, 16, 17, null),
					new ExpectedCellValue(sheetName, 17, 17, null),
					new ExpectedCellValue(sheetName, 18, 17, null),
					new ExpectedCellValue(sheetName, 19, 17, null),
					new ExpectedCellValue(sheetName, 20, 17, null),
					new ExpectedCellValue(sheetName, 21, 17, null),
					new ExpectedCellValue(sheetName, 22, 17, null),
					new ExpectedCellValue(sheetName, 23, 17, null),
					new ExpectedCellValue(sheetName, 24, 17, null),
					new ExpectedCellValue(sheetName, 25, 17, null),
					new ExpectedCellValue(sheetName, 26, 17, null),
					new ExpectedCellValue(sheetName, 27, 17, null),
					new ExpectedCellValue(sheetName, 28, 17, 199d),
					new ExpectedCellValue(sheetName, 29, 17, null),
					new ExpectedCellValue(sheetName, 30, 17, 6d),
					new ExpectedCellValue(sheetName, 31, 17, null),
					new ExpectedCellValue(sheetName, 32, 17, 1194d),
					new ExpectedCellValue(sheetName, 33, 17, 199d),
					new ExpectedCellValue(sheetName, 34, 17, 6d),
					new ExpectedCellValue(sheetName, 35, 17, 1194d),
					new ExpectedCellValue(sheetName, 36, 17, null),
					new ExpectedCellValue(sheetName, 37, 17, null),
					new ExpectedCellValue(sheetName, 38, 17, null),
					new ExpectedCellValue(sheetName, 39, 17, null),
					new ExpectedCellValue(sheetName, 40, 17, null),
					new ExpectedCellValue(sheetName, 41, 17, null),
					new ExpectedCellValue(sheetName, 42, 17, null),
					new ExpectedCellValue(sheetName, 43, 17, null),
					new ExpectedCellValue(sheetName, 44, 17, null),
					new ExpectedCellValue(sheetName, 45, 17, null),
					new ExpectedCellValue(sheetName, 46, 17, null),
					new ExpectedCellValue(sheetName, 47, 17, null),
					new ExpectedCellValue(sheetName, 48, 17, null),
					new ExpectedCellValue(sheetName, 49, 17, null),
					new ExpectedCellValue(sheetName, 50, 17, null),
					new ExpectedCellValue(sheetName, 51, 17, null),
					new ExpectedCellValue(sheetName, 52, 17, null),
					new ExpectedCellValue(sheetName, 53, 17, null),
					new ExpectedCellValue(sheetName, 54, 17, null),
					new ExpectedCellValue(sheetName, 55, 17, null),
					new ExpectedCellValue(sheetName, 56, 17, null),
					new ExpectedCellValue(sheetName, 57, 17, null),
					new ExpectedCellValue(sheetName, 58, 17, null),
					new ExpectedCellValue(sheetName, 59, 17, null),
					new ExpectedCellValue(sheetName, 60, 17, null),
					new ExpectedCellValue(sheetName, 61, 17, null),
					new ExpectedCellValue(sheetName, 62, 17, null),
					new ExpectedCellValue(sheetName, 63, 17, null),
					new ExpectedCellValue(sheetName, 64, 17, null),
					new ExpectedCellValue(sheetName, 65, 17, null),
					new ExpectedCellValue(sheetName, 66, 17, null),
					new ExpectedCellValue(sheetName, 67, 17, null),
					new ExpectedCellValue(sheetName, 68, 17, null),
					new ExpectedCellValue(sheetName, 69, 17, null),
					new ExpectedCellValue(sheetName, 70, 17, null),
					new ExpectedCellValue(sheetName, 71, 17, null),
					new ExpectedCellValue(sheetName, 72, 17, null),
					new ExpectedCellValue(sheetName, 73, 17, null),
					new ExpectedCellValue(sheetName, 74, 17, null),
					new ExpectedCellValue(sheetName, 75, 17, null),
					new ExpectedCellValue(sheetName, 76, 17, 199d),
					new ExpectedCellValue(sheetName, 77, 17, 6d),
					new ExpectedCellValue(sheetName, 78, 17, 1194d),
					new ExpectedCellValue(sheetName, 2, 18, "Tent Total"),
					new ExpectedCellValue(sheetName, 3, 18, null),
					new ExpectedCellValue(sheetName, 4, 18, null),
					new ExpectedCellValue(sheetName, 5, 18, null),
					new ExpectedCellValue(sheetName, 6, 18, null),
					new ExpectedCellValue(sheetName, 7, 18, null),
					new ExpectedCellValue(sheetName, 8, 18, null),
					new ExpectedCellValue(sheetName, 9, 18, null),
					new ExpectedCellValue(sheetName, 10, 18, null),
					new ExpectedCellValue(sheetName, 11, 18, null),
					new ExpectedCellValue(sheetName, 12, 18, null),
					new ExpectedCellValue(sheetName, 13, 18, null),
					new ExpectedCellValue(sheetName, 14, 18, null),
					new ExpectedCellValue(sheetName, 15, 18, null),
					new ExpectedCellValue(sheetName, 16, 18, null),
					new ExpectedCellValue(sheetName, 17, 18, null),
					new ExpectedCellValue(sheetName, 18, 18, null),
					new ExpectedCellValue(sheetName, 19, 18, null),
					new ExpectedCellValue(sheetName, 20, 18, null),
					new ExpectedCellValue(sheetName, 21, 18, null),
					new ExpectedCellValue(sheetName, 22, 18, null),
					new ExpectedCellValue(sheetName, 23, 18, null),
					new ExpectedCellValue(sheetName, 24, 18, null),
					new ExpectedCellValue(sheetName, 25, 18, null),
					new ExpectedCellValue(sheetName, 26, 18, null),
					new ExpectedCellValue(sheetName, 27, 18, null),
					new ExpectedCellValue(sheetName, 28, 18, 199d),
					new ExpectedCellValue(sheetName, 29, 18, null),
					new ExpectedCellValue(sheetName, 30, 18, 6d),
					new ExpectedCellValue(sheetName, 31, 18, null),
					new ExpectedCellValue(sheetName, 32, 18, 1194d),
					new ExpectedCellValue(sheetName, 33, 18, 199d),
					new ExpectedCellValue(sheetName, 34, 18, 6d),
					new ExpectedCellValue(sheetName, 35, 18, 1194d),
					new ExpectedCellValue(sheetName, 36, 18, null),
					new ExpectedCellValue(sheetName, 37, 18, null),
					new ExpectedCellValue(sheetName, 38, 18, null),
					new ExpectedCellValue(sheetName, 39, 18, null),
					new ExpectedCellValue(sheetName, 40, 18, null),
					new ExpectedCellValue(sheetName, 41, 18, null),
					new ExpectedCellValue(sheetName, 42, 18, null),
					new ExpectedCellValue(sheetName, 43, 18, null),
					new ExpectedCellValue(sheetName, 44, 18, null),
					new ExpectedCellValue(sheetName, 45, 18, null),
					new ExpectedCellValue(sheetName, 46, 18, null),
					new ExpectedCellValue(sheetName, 47, 18, null),
					new ExpectedCellValue(sheetName, 48, 18, null),
					new ExpectedCellValue(sheetName, 49, 18, null),
					new ExpectedCellValue(sheetName, 50, 18, null),
					new ExpectedCellValue(sheetName, 51, 18, null),
					new ExpectedCellValue(sheetName, 52, 18, null),
					new ExpectedCellValue(sheetName, 53, 18, null),
					new ExpectedCellValue(sheetName, 54, 18, null),
					new ExpectedCellValue(sheetName, 55, 18, null),
					new ExpectedCellValue(sheetName, 56, 18, null),
					new ExpectedCellValue(sheetName, 57, 18, null),
					new ExpectedCellValue(sheetName, 58, 18, null),
					new ExpectedCellValue(sheetName, 59, 18, null),
					new ExpectedCellValue(sheetName, 60, 18, null),
					new ExpectedCellValue(sheetName, 61, 18, null),
					new ExpectedCellValue(sheetName, 62, 18, null),
					new ExpectedCellValue(sheetName, 63, 18, null),
					new ExpectedCellValue(sheetName, 64, 18, null),
					new ExpectedCellValue(sheetName, 65, 18, null),
					new ExpectedCellValue(sheetName, 66, 18, null),
					new ExpectedCellValue(sheetName, 67, 18, null),
					new ExpectedCellValue(sheetName, 68, 18, null),
					new ExpectedCellValue(sheetName, 69, 18, null),
					new ExpectedCellValue(sheetName, 70, 18, null),
					new ExpectedCellValue(sheetName, 71, 18, null),
					new ExpectedCellValue(sheetName, 72, 18, null),
					new ExpectedCellValue(sheetName, 73, 18, null),
					new ExpectedCellValue(sheetName, 74, 18, null),
					new ExpectedCellValue(sheetName, 75, 18, null),
					new ExpectedCellValue(sheetName, 76, 18, 199d),
					new ExpectedCellValue(sheetName, 77, 18, 6d),
					new ExpectedCellValue(sheetName, 78, 18, 1194d),

					new ExpectedCellValue(sheetName, 2, 19, "Grand Total"),
					new ExpectedCellValue(sheetName, 3, 19, null),
					new ExpectedCellValue(sheetName, 4, 19, null),
					new ExpectedCellValue(sheetName, 5, 19, null),
					new ExpectedCellValue(sheetName, 6, 19, null),
					new ExpectedCellValue(sheetName, 7, 19, null),
					new ExpectedCellValue(sheetName, 8, 19, 415.75),
					new ExpectedCellValue(sheetName, 9, 19, null),
					new ExpectedCellValue(sheetName, 10, 19, 2d),
					new ExpectedCellValue(sheetName, 11, 19, null),
					new ExpectedCellValue(sheetName, 12, 19, 831.5),
					new ExpectedCellValue(sheetName, 13, 19, 415.75),
					new ExpectedCellValue(sheetName, 14, 19, 2d),
					new ExpectedCellValue(sheetName, 15, 19, 831.5),
					new ExpectedCellValue(sheetName, 16, 19, null),
					new ExpectedCellValue(sheetName, 17, 19, null),
					new ExpectedCellValue(sheetName, 18, 19, 415.75),
					new ExpectedCellValue(sheetName, 19, 19, null),
					new ExpectedCellValue(sheetName, 20, 19, 2d),
					new ExpectedCellValue(sheetName, 21, 19, null),
					new ExpectedCellValue(sheetName, 22, 19, 831.5),
					new ExpectedCellValue(sheetName, 23, 19, 415.75),
					new ExpectedCellValue(sheetName, 24, 19, 2d),
					new ExpectedCellValue(sheetName, 25, 19, 831.5),
					new ExpectedCellValue(sheetName, 26, 19, null),
					new ExpectedCellValue(sheetName, 27, 19, null),
					new ExpectedCellValue(sheetName, 28, 19, 199d),
					new ExpectedCellValue(sheetName, 29, 19, null),
					new ExpectedCellValue(sheetName, 30, 19, 6d),
					new ExpectedCellValue(sheetName, 31, 19, null),
					new ExpectedCellValue(sheetName, 32, 19, 1194d),
					new ExpectedCellValue(sheetName, 33, 19, 199d),
					new ExpectedCellValue(sheetName, 34, 19, 6d),
					new ExpectedCellValue(sheetName, 35, 19, 1194d),
					new ExpectedCellValue(sheetName, 36, 19, null),
					new ExpectedCellValue(sheetName, 37, 19, null),
					new ExpectedCellValue(sheetName, 38, 19, 415.75),
					new ExpectedCellValue(sheetName, 39, 19, null),
					new ExpectedCellValue(sheetName, 40, 19, 1d),
					new ExpectedCellValue(sheetName, 41, 19, null),
					new ExpectedCellValue(sheetName, 42, 19, 415.75),
					new ExpectedCellValue(sheetName, 43, 19, 415.75),
					new ExpectedCellValue(sheetName, 44, 19, 1d),
					new ExpectedCellValue(sheetName, 45, 19, 415.75),
					new ExpectedCellValue(sheetName, 46, 19, null),
					new ExpectedCellValue(sheetName, 47, 19, null),
					new ExpectedCellValue(sheetName, 48, 19, 24.99),
					new ExpectedCellValue(sheetName, 49, 19, null),
					new ExpectedCellValue(sheetName, 50, 19, 1d),
					new ExpectedCellValue(sheetName, 51, 19, null),
					new ExpectedCellValue(sheetName, 52, 19, 24.99),
					new ExpectedCellValue(sheetName, 53, 19, 24.99),
					new ExpectedCellValue(sheetName, 54, 19, 1d),
					new ExpectedCellValue(sheetName, 55, 19, 24.99),
					new ExpectedCellValue(sheetName, 56, 19, null),
					new ExpectedCellValue(sheetName, 57, 19, null),
					new ExpectedCellValue(sheetName, 58, 19, 99d),
					new ExpectedCellValue(sheetName, 59, 19, null),
					new ExpectedCellValue(sheetName, 60, 19, 1d),
					new ExpectedCellValue(sheetName, 61, 19, null),
					new ExpectedCellValue(sheetName, 62, 19, 99d),
					new ExpectedCellValue(sheetName, 63, 19, 99d),
					new ExpectedCellValue(sheetName, 64, 19, 1d),
					new ExpectedCellValue(sheetName, 65, 19, 99d),
					new ExpectedCellValue(sheetName, 66, 19, null),
					new ExpectedCellValue(sheetName, 67, 19, null),
					new ExpectedCellValue(sheetName, 68, 19, 415.75),
					new ExpectedCellValue(sheetName, 69, 19, null),
					new ExpectedCellValue(sheetName, 70, 19, 2d),
					new ExpectedCellValue(sheetName, 71, 19, null),
					new ExpectedCellValue(sheetName, 72, 19, 831.5),
					new ExpectedCellValue(sheetName, 73, 19, 415.75),
					new ExpectedCellValue(sheetName, 74, 19, 2d),
					new ExpectedCellValue(sheetName, 75, 19, 831.5),
					new ExpectedCellValue(sheetName, 76, 19, 1985.99),
					new ExpectedCellValue(sheetName, 77, 19, 15d),
					new ExpectedCellValue(sheetName, 78, 19, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingByDays.xlsx")]
		public void PivotTableRefreshDateGroupingWithDaysOnly()
		{
			var file = new FileInfo("PivotTableDateGroupingByDays.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Sheet2"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(369, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet2";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "9-Feb"),
					new ExpectedCellValue(sheetName, 3, 1, "29-Mar"),
					new ExpectedCellValue(sheetName, 4, 1, "4-May"),
					new ExpectedCellValue(sheetName, 5, 1, "28-May"),
					new ExpectedCellValue(sheetName, 6, 1, "19-Oct"),
					new ExpectedCellValue(sheetName, 7, 1, "3-Nov"),
					new ExpectedCellValue(sheetName, 8, 1, "9-Dec"),
					new ExpectedCellValue(sheetName, 9, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 2, 831.5),
					new ExpectedCellValue(sheetName, 3, 2, 24.99),
					new ExpectedCellValue(sheetName, 4, 2, 1194d),
					new ExpectedCellValue(sheetName, 5, 2, 831.5),
					new ExpectedCellValue(sheetName, 6, 2, 831.5),
					new ExpectedCellValue(sheetName, 7, 2, 415.75),
					new ExpectedCellValue(sheetName, 8, 2, 99d),
					new ExpectedCellValue(sheetName, 9, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingByTime.xlsx")]
		public void PivotTableRefreshDateGroupingWithHoursOnly()
		{
			var file = new FileInfo("PivotTableDateGroupingByTime.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Hours"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(27, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Hours";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 13, 1, "12 AM"),
					new ExpectedCellValue(sheetName, 14, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 12, 2, "Sum of Total"),
					new ExpectedCellValue(sheetName, 13, 2, 4228.24),
					new ExpectedCellValue(sheetName, 14, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingByTime.xlsx")]
		public void PivotTableRefreshDateGroupingWithMinutesOnly()
		{
			var file = new FileInfo("PivotTableDateGroupingByTime.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Minutes"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A12:B14"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(63, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Minutes";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 13, 1, ":00"),
					new ExpectedCellValue(sheetName, 14, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 12, 2, "Sum of Total"),
					new ExpectedCellValue(sheetName, 13, 2, 4228.24),
					new ExpectedCellValue(sheetName, 14, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingByTime.xlsx")]
		public void PivotTableRefreshDateGroupingWithSecondsOnly()
		{
			var file = new FileInfo("PivotTableDateGroupingByTime.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Seconds"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A12:B14"), pivotTable.Address);
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(63, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Seconds";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 13, 1, ":00"),
					new ExpectedCellValue(sheetName, 14, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 12, 2, "Sum of Total"),
					new ExpectedCellValue(sheetName, 13, 2, 4228.24),
					new ExpectedCellValue(sheetName, 14, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingAllTypes.xlsx")]
		public void PivotTableRefreshDateGroupingAllTypes()
		{
			var file = new FileInfo("PivotTableDateGroupingAllTypes.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Sheet2"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:B45"), pivotTable.Address);
					Assert.AreEqual(13, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(63, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(63, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(27, pivotTable.Fields[8].Items.Count);
					Assert.AreEqual(369, pivotTable.Fields[9].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[10].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[11].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[12].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet2";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
						new ExpectedCellValue(sheetName, 2, 1, 2016),
						new ExpectedCellValue(sheetName, 3, 1, "Qtr2"),
						new ExpectedCellValue(sheetName, 4, 1, "May"),
						new ExpectedCellValue(sheetName, 5, 1, "28-May"),
						new ExpectedCellValue(sheetName, 6, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 7, 1, ":00"),
						new ExpectedCellValue(sheetName, 8, 1, ":00"),
						new ExpectedCellValue(sheetName, 9, 1, 2017),
						new ExpectedCellValue(sheetName, 10, 1, "Qtr4"),
						new ExpectedCellValue(sheetName, 11, 1, "Oct"),
						new ExpectedCellValue(sheetName, 12, 1, "19-Oct"),
						new ExpectedCellValue(sheetName, 13, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 14, 1, ":00"),
						new ExpectedCellValue(sheetName, 15, 1, ":00"),
						new ExpectedCellValue(sheetName, 16, 1, "Dec"),
						new ExpectedCellValue(sheetName, 17, 1, "9-Dec"),
						new ExpectedCellValue(sheetName, 18, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 19, 1, ":00"),
						new ExpectedCellValue(sheetName, 20, 1, ":00"),
						new ExpectedCellValue(sheetName, 21, 1, 2018),
						new ExpectedCellValue(sheetName, 22, 1, "Qtr1"),
						new ExpectedCellValue(sheetName, 23, 1, "Feb"),
						new ExpectedCellValue(sheetName, 24, 1, "9-Feb"),
						new ExpectedCellValue(sheetName, 25, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 26, 1, ":00"),
						new ExpectedCellValue(sheetName, 27, 1, ":00"),
						new ExpectedCellValue(sheetName, 28, 1, "Mar"),
						new ExpectedCellValue(sheetName, 29, 1, "29-Mar"),
						new ExpectedCellValue(sheetName, 30, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 31, 1, ":00"),
						new ExpectedCellValue(sheetName, 32, 1, ":00"),
						new ExpectedCellValue(sheetName, 33, 1, "Qtr2"),
						new ExpectedCellValue(sheetName, 34, 1, "May"),
						new ExpectedCellValue(sheetName, 35, 1, "4-May"),
						new ExpectedCellValue(sheetName, 36, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 37, 1, ":00"),
						new ExpectedCellValue(sheetName, 38, 1, ":00"),
						new ExpectedCellValue(sheetName, 39, 1, "Qtr4"),
						new ExpectedCellValue(sheetName, 40, 1, "Nov"),
						new ExpectedCellValue(sheetName, 41, 1, "3-Nov"),
						new ExpectedCellValue(sheetName, 42, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 43, 1, ":00"),
						new ExpectedCellValue(sheetName, 44, 1, ":00"),
						new ExpectedCellValue(sheetName, 45, 1, "Grand Total"),
						new ExpectedCellValue(sheetName, 1, 2, "Sum of Total"),
						new ExpectedCellValue(sheetName, 2, 2, 831.5),
						new ExpectedCellValue(sheetName, 3, 2, 831.5),
						new ExpectedCellValue(sheetName, 4, 2, 831.5),
						new ExpectedCellValue(sheetName, 5, 2, 831.5),
						new ExpectedCellValue(sheetName, 6, 2, 831.5),
						new ExpectedCellValue(sheetName, 7, 2, 831.5),
						new ExpectedCellValue(sheetName, 8, 2, 831.5),
						new ExpectedCellValue(sheetName, 9, 2, 930.5),
						new ExpectedCellValue(sheetName, 10, 2, 930.5),
						new ExpectedCellValue(sheetName, 11, 2, 831.5),
						new ExpectedCellValue(sheetName, 12, 2, 831.5),
						new ExpectedCellValue(sheetName, 13, 2, 831.5),
						new ExpectedCellValue(sheetName, 14, 2, 831.5),
						new ExpectedCellValue(sheetName, 15, 2, 831.5),
						new ExpectedCellValue(sheetName, 16, 2, 99d),
						new ExpectedCellValue(sheetName, 17, 2, 99d),
						new ExpectedCellValue(sheetName, 18, 2, 99d),
						new ExpectedCellValue(sheetName, 19, 2, 99d),
						new ExpectedCellValue(sheetName, 20, 2, 99d),
						new ExpectedCellValue(sheetName, 21, 2, 2466.24),
						new ExpectedCellValue(sheetName, 22, 2, 856.49),
						new ExpectedCellValue(sheetName, 23, 2, 831.5),
						new ExpectedCellValue(sheetName, 24, 2, 831.5),
						new ExpectedCellValue(sheetName, 25, 2, 831.5),
						new ExpectedCellValue(sheetName, 26, 2, 831.5),
						new ExpectedCellValue(sheetName, 27, 2, 831.5),
						new ExpectedCellValue(sheetName, 28, 2, 24.99),
						new ExpectedCellValue(sheetName, 29, 2, 24.99),
						new ExpectedCellValue(sheetName, 30, 2, 24.99),
						new ExpectedCellValue(sheetName, 31, 2, 24.99),
						new ExpectedCellValue(sheetName, 32, 2, 24.99),
						new ExpectedCellValue(sheetName, 33, 2, 1194d),
						new ExpectedCellValue(sheetName, 34, 2, 1194d),
						new ExpectedCellValue(sheetName, 35, 2, 1194d),
						new ExpectedCellValue(sheetName, 36, 2, 1194d),
						new ExpectedCellValue(sheetName, 37, 2, 1194d),
						new ExpectedCellValue(sheetName, 38, 2, 1194d),
						new ExpectedCellValue(sheetName, 39, 2, 415.75),
						new ExpectedCellValue(sheetName, 40, 2, 415.75),
						new ExpectedCellValue(sheetName, 41, 2, 415.75),
						new ExpectedCellValue(sheetName, 42, 2, 415.75),
						new ExpectedCellValue(sheetName, 43, 2, 415.75),
						new ExpectedCellValue(sheetName, 44, 2, 415.75),
						new ExpectedCellValue(sheetName, 45, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTables\PivotTableDateGroupingAllTypes.xlsx")]
		public void PivotTableRefreshDateGroupingAllTypesWithColumnFields()
		{
			var file = new FileInfo("PivotTableDateGroupingAllTypes.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["Sheet3"];
					var pivotTable = worksheet.PivotTables["PivotTable1"];
					foreach (var cacheDefinition in package.Workbook.PivotCacheDefinitions)
					{
						cacheDefinition.UpdateData();
					}
					this.CheckPivotTableAddress(new ExcelAddress("A1:R151"), pivotTable.Address);
					Assert.AreEqual(13, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(63, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[6].Items.Count);
					Assert.AreEqual(63, pivotTable.Fields[7].Items.Count);
					Assert.AreEqual(27, pivotTable.Fields[8].Items.Count);
					Assert.AreEqual(369, pivotTable.Fields[9].Items.Count);
					Assert.AreEqual(15, pivotTable.Fields[10].Items.Count);
					Assert.AreEqual(7, pivotTable.Fields[11].Items.Count);
					Assert.AreEqual(6, pivotTable.Fields[12].Items.Count);
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet3";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
						new ExpectedCellValue(sheetName, 5, 1, 2016),
						new ExpectedCellValue(sheetName, 6, 1, "Qtr2"),
						new ExpectedCellValue(sheetName, 7, 1, "28-May"),
						new ExpectedCellValue(sheetName, 8, 1, "Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 9, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 10, 1, ":00"),
						new ExpectedCellValue(sheetName, 11, 1, ":00"),
						new ExpectedCellValue(sheetName, 12, 1, "Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 13, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 14, 1, ":00"),
						new ExpectedCellValue(sheetName, 15, 1, ":00"),
						new ExpectedCellValue(sheetName, 16, 1, "Sum of Total"),
						new ExpectedCellValue(sheetName, 17, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 18, 1, ":00"),
						new ExpectedCellValue(sheetName, 19, 1, ":00"),
						new ExpectedCellValue(sheetName, 20, 1, "28-May Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 21, 1, "28-May Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 22, 1, "28-May Sum of Total"),
						new ExpectedCellValue(sheetName, 23, 1, "Qtr2 Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 24, 1, "Qtr2 Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 25, 1, "Qtr2 Sum of Total"),
						new ExpectedCellValue(sheetName, 26, 1, "2016 Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 27, 1, "2016 Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 28, 1, "2016 Sum of Total"),
						new ExpectedCellValue(sheetName, 29, 1, "2017"),
						new ExpectedCellValue(sheetName, 30, 1, "Qtr4"),
						new ExpectedCellValue(sheetName, 31, 1, "19-Oct"),
						new ExpectedCellValue(sheetName, 32, 1, "Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 33, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 34, 1, ":00"),
						new ExpectedCellValue(sheetName, 35, 1, ":00"),
						new ExpectedCellValue(sheetName, 36, 1, "Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 37, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 38, 1, ":00"),
						new ExpectedCellValue(sheetName, 39, 1, ":00"),
						new ExpectedCellValue(sheetName, 40, 1, "Sum of Total"),
						new ExpectedCellValue(sheetName, 41, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 42, 1, ":00"),
						new ExpectedCellValue(sheetName, 43, 1, ":00"),
						new ExpectedCellValue(sheetName, 44, 1, "19-Oct Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 45, 1, "19-Oct Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 46, 1, "19-Oct Sum of Total"),
						new ExpectedCellValue(sheetName, 47, 1, "9-Dec"),
						new ExpectedCellValue(sheetName, 48, 1, "Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 49, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 50, 1, ":00"),
						new ExpectedCellValue(sheetName, 51, 1, ":00"),
						new ExpectedCellValue(sheetName, 52, 1, "Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 53, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 54, 1, ":00"),
						new ExpectedCellValue(sheetName, 55, 1, ":00"),
						new ExpectedCellValue(sheetName, 56, 1, "Sum of Total"),
						new ExpectedCellValue(sheetName, 57, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 58, 1, ":00"),
						new ExpectedCellValue(sheetName, 59, 1, ":00"),
						new ExpectedCellValue(sheetName, 60, 1, "9-Dec Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 61, 1, "9-Dec Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 62, 1, "9-Dec Sum of Total"),
						new ExpectedCellValue(sheetName, 63, 1, "Qtr4 Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 64, 1, "Qtr4 Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 65, 1, "Qtr4 Sum of Total"),
						new ExpectedCellValue(sheetName, 66, 1, "2017 Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 67, 1, "2017 Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 68, 1, "2017 Sum of Total"),
						new ExpectedCellValue(sheetName, 69, 1, "2018"),
						new ExpectedCellValue(sheetName, 70, 1, "Qtr1"),
						new ExpectedCellValue(sheetName, 71, 1, "9-Feb"),
						new ExpectedCellValue(sheetName, 72, 1, "Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 73, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 74, 1, ":00"),
						new ExpectedCellValue(sheetName, 75, 1, ":00"),
						new ExpectedCellValue(sheetName, 76, 1, "Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 77, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 78, 1, ":00"),
						new ExpectedCellValue(sheetName, 79, 1, ":00"),
						new ExpectedCellValue(sheetName, 80, 1, "Sum of Total"),
						new ExpectedCellValue(sheetName, 81, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 82, 1, ":00"),
						new ExpectedCellValue(sheetName, 83, 1, ":00"),
						new ExpectedCellValue(sheetName, 84, 1, "9-Feb Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 85, 1, "9-Feb Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 86, 1, "9-Feb Sum of Total"),
						new ExpectedCellValue(sheetName, 87, 1, "29-Mar"),
						new ExpectedCellValue(sheetName, 88, 1, "Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 89, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 90, 1, ":00"),
						new ExpectedCellValue(sheetName, 91, 1, ":00"),
						new ExpectedCellValue(sheetName, 92, 1, "Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 93, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 94, 1, ":00"),
						new ExpectedCellValue(sheetName, 95, 1, ":00"),
						new ExpectedCellValue(sheetName, 96, 1, "Sum of Total"),
						new ExpectedCellValue(sheetName, 97, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 98, 1, ":00"),
						new ExpectedCellValue(sheetName, 99, 1, ":00"),
						new ExpectedCellValue(sheetName, 100, 1, "29-Mar Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 101, 1, "29-Mar Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 102, 1, "29-Mar Sum of Total"),
						new ExpectedCellValue(sheetName, 103, 1, "Qtr1 Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 104, 1, "Qtr1 Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 105, 1, "Qtr1 Sum of Total"),
						new ExpectedCellValue(sheetName, 106, 1, "Qtr2"),
						new ExpectedCellValue(sheetName, 107, 1, "4-May"),
						new ExpectedCellValue(sheetName, 108, 1, "Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 109, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 110, 1, ":00"),
						new ExpectedCellValue(sheetName, 111, 1, ":00"),
						new ExpectedCellValue(sheetName, 112, 1, "Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 113, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 114, 1, ":00"),
						new ExpectedCellValue(sheetName, 115, 1, ":00"),
						new ExpectedCellValue(sheetName, 116, 1, "Sum of Total"),
						new ExpectedCellValue(sheetName, 117, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 118, 1, ":00"),
						new ExpectedCellValue(sheetName, 119, 1, ":00"),
						new ExpectedCellValue(sheetName, 120, 1, "4-May Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 121, 1, "4-May Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 122, 1, "4-May Sum of Total"),
						new ExpectedCellValue(sheetName, 123, 1, "Qtr2 Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 124, 1, "Qtr2 Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 125, 1, "Qtr2 Sum of Total"),
						new ExpectedCellValue(sheetName, 126, 1, "Qtr4"),
						new ExpectedCellValue(sheetName, 127, 1, "3-Nov"),
						new ExpectedCellValue(sheetName, 128, 1, "Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 129, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 130, 1, ":00"),
						new ExpectedCellValue(sheetName, 131, 1, ":00"),
						new ExpectedCellValue(sheetName, 132, 1, "Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 133, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 134, 1, ":00"),
						new ExpectedCellValue(sheetName, 135, 1, ":00"),
						new ExpectedCellValue(sheetName, 136, 1, "Sum of Total"),
						new ExpectedCellValue(sheetName, 137, 1, "12 AM"),
						new ExpectedCellValue(sheetName, 138, 1, ":00"),
						new ExpectedCellValue(sheetName, 139, 1, ":00"),
						new ExpectedCellValue(sheetName, 140, 1, "3-Nov Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 141, 1, "3-Nov Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 142, 1, "3-Nov Sum of Total"),
						new ExpectedCellValue(sheetName, 143, 1, "Qtr4 Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 144, 1, "Qtr4 Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 145, 1, "Qtr4 Sum of Total"),
						new ExpectedCellValue(sheetName, 146, 1, "2018 Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 147, 1, "2018 Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 148, 1, "2018 Sum of Total"),
						new ExpectedCellValue(sheetName, 149, 1, "Total Sum of Wholesale Price"),
						new ExpectedCellValue(sheetName, 150, 1, "Total Sum of Units Sold"),
						new ExpectedCellValue(sheetName, 151, 1, "Total Sum of Total"),
						new ExpectedCellValue(sheetName, 2, 2, "Chicago"),
						new ExpectedCellValue(sheetName, 3, 2, "Car Rack"),
						new ExpectedCellValue(sheetName, 4, 2, "May"),
						new ExpectedCellValue(sheetName, 5, 2, null),
						new ExpectedCellValue(sheetName, 6, 2, null),
						new ExpectedCellValue(sheetName, 7, 2, null),
						new ExpectedCellValue(sheetName, 8, 2, null),
						new ExpectedCellValue(sheetName, 9, 2, 415.75),
						new ExpectedCellValue(sheetName, 10, 2, 415.75),
						new ExpectedCellValue(sheetName, 11, 2, 415.75),
						new ExpectedCellValue(sheetName, 12, 2, null),
						new ExpectedCellValue(sheetName, 13, 2, 2d),
						new ExpectedCellValue(sheetName, 14, 2, 2d),
						new ExpectedCellValue(sheetName, 15, 2, 2d),
						new ExpectedCellValue(sheetName, 16, 2, null),
						new ExpectedCellValue(sheetName, 17, 2, 831.5),
						new ExpectedCellValue(sheetName, 18, 2, 831.5),
						new ExpectedCellValue(sheetName, 19, 2, 831.5),
						new ExpectedCellValue(sheetName, 20, 2, 415.75),
						new ExpectedCellValue(sheetName, 21, 2, 2d),
						new ExpectedCellValue(sheetName, 22, 2, 831.5),
						new ExpectedCellValue(sheetName, 23, 2, 415.75),
						new ExpectedCellValue(sheetName, 24, 2, 2d),
						new ExpectedCellValue(sheetName, 25, 2, 831.5),
						new ExpectedCellValue(sheetName, 26, 2, 415.75),
						new ExpectedCellValue(sheetName, 27, 2, 2d),
						new ExpectedCellValue(sheetName, 28, 2, 831.5),
						new ExpectedCellValue(sheetName, 29, 2, null),
						new ExpectedCellValue(sheetName, 30, 2, null),
						new ExpectedCellValue(sheetName, 31, 2, null),
						new ExpectedCellValue(sheetName, 32, 2, null),
						new ExpectedCellValue(sheetName, 33, 2, null),
						new ExpectedCellValue(sheetName, 34, 2, null),
						new ExpectedCellValue(sheetName, 35, 2, null),
						new ExpectedCellValue(sheetName, 36, 2, null),
						new ExpectedCellValue(sheetName, 37, 2, null),
						new ExpectedCellValue(sheetName, 38, 2, null),
						new ExpectedCellValue(sheetName, 39, 2, null),
						new ExpectedCellValue(sheetName, 40, 2, null),
						new ExpectedCellValue(sheetName, 41, 2, null),
						new ExpectedCellValue(sheetName, 42, 2, null),
						new ExpectedCellValue(sheetName, 43, 2, null),
						new ExpectedCellValue(sheetName, 44, 2, null),
						new ExpectedCellValue(sheetName, 45, 2, null),
						new ExpectedCellValue(sheetName, 46, 2, null),
						new ExpectedCellValue(sheetName, 47, 2, null),
						new ExpectedCellValue(sheetName, 48, 2, null),
						new ExpectedCellValue(sheetName, 49, 2, null),
						new ExpectedCellValue(sheetName, 50, 2, null),
						new ExpectedCellValue(sheetName, 51, 2, null),
						new ExpectedCellValue(sheetName, 52, 2, null),
						new ExpectedCellValue(sheetName, 53, 2, null),
						new ExpectedCellValue(sheetName, 54, 2, null),
						new ExpectedCellValue(sheetName, 55, 2, null),
						new ExpectedCellValue(sheetName, 56, 2, null),
						new ExpectedCellValue(sheetName, 57, 2, null),
						new ExpectedCellValue(sheetName, 58, 2, null),
						new ExpectedCellValue(sheetName, 59, 2, null),
						new ExpectedCellValue(sheetName, 60, 2, null),
						new ExpectedCellValue(sheetName, 61, 2, null),
						new ExpectedCellValue(sheetName, 62, 2, null),
						new ExpectedCellValue(sheetName, 63, 2, null),
						new ExpectedCellValue(sheetName, 64, 2, null),
						new ExpectedCellValue(sheetName, 65, 2, null),
						new ExpectedCellValue(sheetName, 66, 2, null),
						new ExpectedCellValue(sheetName, 67, 2, null),
						new ExpectedCellValue(sheetName, 68, 2, null),
						new ExpectedCellValue(sheetName, 69, 2, null),
						new ExpectedCellValue(sheetName, 70, 2, null),
						new ExpectedCellValue(sheetName, 71, 2, null),
						new ExpectedCellValue(sheetName, 72, 2, null),
						new ExpectedCellValue(sheetName, 73, 2, null),
						new ExpectedCellValue(sheetName, 74, 2, null),
						new ExpectedCellValue(sheetName, 75, 2, null),
						new ExpectedCellValue(sheetName, 76, 2, null),
						new ExpectedCellValue(sheetName, 77, 2, null),
						new ExpectedCellValue(sheetName, 78, 2, null),
						new ExpectedCellValue(sheetName, 79, 2, null),
						new ExpectedCellValue(sheetName, 80, 2, null),
						new ExpectedCellValue(sheetName, 81, 2, null),
						new ExpectedCellValue(sheetName, 82, 2, null),
						new ExpectedCellValue(sheetName, 83, 2, null),
						new ExpectedCellValue(sheetName, 84, 2, null),
						new ExpectedCellValue(sheetName, 85, 2, null),
						new ExpectedCellValue(sheetName, 86, 2, null),
						new ExpectedCellValue(sheetName, 87, 2, null),
						new ExpectedCellValue(sheetName, 88, 2, null),
						new ExpectedCellValue(sheetName, 89, 2, null),
						new ExpectedCellValue(sheetName, 90, 2, null),
						new ExpectedCellValue(sheetName, 91, 2, null),
						new ExpectedCellValue(sheetName, 92, 2, null),
						new ExpectedCellValue(sheetName, 93, 2, null),
						new ExpectedCellValue(sheetName, 94, 2, null),
						new ExpectedCellValue(sheetName, 95, 2, null),
						new ExpectedCellValue(sheetName, 96, 2, null),
						new ExpectedCellValue(sheetName, 97, 2, null),
						new ExpectedCellValue(sheetName, 98, 2, null),
						new ExpectedCellValue(sheetName, 99, 2, null),
						new ExpectedCellValue(sheetName, 100, 2, null),
						new ExpectedCellValue(sheetName, 101, 2, null),
						new ExpectedCellValue(sheetName, 102, 2, null),
						new ExpectedCellValue(sheetName, 103, 2, null),
						new ExpectedCellValue(sheetName, 104, 2, null),
						new ExpectedCellValue(sheetName, 105, 2, null),
						new ExpectedCellValue(sheetName, 106, 2, null),
						new ExpectedCellValue(sheetName, 107, 2, null),
						new ExpectedCellValue(sheetName, 108, 2, null),
						new ExpectedCellValue(sheetName, 109, 2, null),
						new ExpectedCellValue(sheetName, 110, 2, null),
						new ExpectedCellValue(sheetName, 111, 2, null),
						new ExpectedCellValue(sheetName, 112, 2, null),
						new ExpectedCellValue(sheetName, 113, 2, null),
						new ExpectedCellValue(sheetName, 114, 2, null),
						new ExpectedCellValue(sheetName, 115, 2, null),
						new ExpectedCellValue(sheetName, 116, 2, null),
						new ExpectedCellValue(sheetName, 117, 2, null),
						new ExpectedCellValue(sheetName, 118, 2, null),
						new ExpectedCellValue(sheetName, 119, 2, null),
						new ExpectedCellValue(sheetName, 120, 2, null),
						new ExpectedCellValue(sheetName, 121, 2, null),
						new ExpectedCellValue(sheetName, 122, 2, null),
						new ExpectedCellValue(sheetName, 123, 2, null),
						new ExpectedCellValue(sheetName, 124, 2, null),
						new ExpectedCellValue(sheetName, 125, 2, null),
						new ExpectedCellValue(sheetName, 126, 2, null),
						new ExpectedCellValue(sheetName, 127, 2, null),
						new ExpectedCellValue(sheetName, 128, 2, null),
						new ExpectedCellValue(sheetName, 129, 2, null),
						new ExpectedCellValue(sheetName, 130, 2, null),
						new ExpectedCellValue(sheetName, 131, 2, null),
						new ExpectedCellValue(sheetName, 132, 2, null),
						new ExpectedCellValue(sheetName, 133, 2, null),
						new ExpectedCellValue(sheetName, 134, 2, null),
						new ExpectedCellValue(sheetName, 135, 2, null),
						new ExpectedCellValue(sheetName, 136, 2, null),
						new ExpectedCellValue(sheetName, 137, 2, null),
						new ExpectedCellValue(sheetName, 138, 2, null),
						new ExpectedCellValue(sheetName, 139, 2, null),
						new ExpectedCellValue(sheetName, 140, 2, null),
						new ExpectedCellValue(sheetName, 141, 2, null),
						new ExpectedCellValue(sheetName, 142, 2, null),
						new ExpectedCellValue(sheetName, 143, 2, null),
						new ExpectedCellValue(sheetName, 144, 2, null),
						new ExpectedCellValue(sheetName, 145, 2, null),
						new ExpectedCellValue(sheetName, 146, 2, null),
						new ExpectedCellValue(sheetName, 147, 2, null),
						new ExpectedCellValue(sheetName, 148, 2, null),
						new ExpectedCellValue(sheetName, 149, 2, 415.75),
						new ExpectedCellValue(sheetName, 150, 2, 2d),
						new ExpectedCellValue(sheetName, 151, 2, 831.5),
						new ExpectedCellValue(sheetName, 2, 3, null),
						new ExpectedCellValue(sheetName, 3, 3, "Car Rack Total"),
						new ExpectedCellValue(sheetName, 4, 3, null),
						new ExpectedCellValue(sheetName, 5, 3, null),
						new ExpectedCellValue(sheetName, 6, 3, null),
						new ExpectedCellValue(sheetName, 7, 3, null),
						new ExpectedCellValue(sheetName, 8, 3, null),
						new ExpectedCellValue(sheetName, 9, 3, 415.75),
						new ExpectedCellValue(sheetName, 10, 3, 415.75),
						new ExpectedCellValue(sheetName, 11, 3, 415.75),
						new ExpectedCellValue(sheetName, 12, 3, null),
						new ExpectedCellValue(sheetName, 13, 3, 2d),
						new ExpectedCellValue(sheetName, 14, 3, 2d),
						new ExpectedCellValue(sheetName, 15, 3, 2d),
						new ExpectedCellValue(sheetName, 16, 3, null),
						new ExpectedCellValue(sheetName, 17, 3, 831.5),
						new ExpectedCellValue(sheetName, 18, 3, 831.5),
						new ExpectedCellValue(sheetName, 19, 3, 831.5),
						new ExpectedCellValue(sheetName, 20, 3, 415.75),
						new ExpectedCellValue(sheetName, 21, 3, 2d),
						new ExpectedCellValue(sheetName, 22, 3, 831.5),
						new ExpectedCellValue(sheetName, 23, 3, 415.75),
						new ExpectedCellValue(sheetName, 24, 3, 2d),
						new ExpectedCellValue(sheetName, 25, 3, 831.5),
						new ExpectedCellValue(sheetName, 26, 3, 415.75),
						new ExpectedCellValue(sheetName, 27, 3, 2d),
						new ExpectedCellValue(sheetName, 28, 3, 831.5),
						new ExpectedCellValue(sheetName, 29, 3, null),
						new ExpectedCellValue(sheetName, 30, 3, null),
						new ExpectedCellValue(sheetName, 31, 3, null),
						new ExpectedCellValue(sheetName, 32, 3, null),
						new ExpectedCellValue(sheetName, 33, 3, null),
						new ExpectedCellValue(sheetName, 34, 3, null),
						new ExpectedCellValue(sheetName, 35, 3, null),
						new ExpectedCellValue(sheetName, 36, 3, null),
						new ExpectedCellValue(sheetName, 37, 3, null),
						new ExpectedCellValue(sheetName, 38, 3, null),
						new ExpectedCellValue(sheetName, 39, 3, null),
						new ExpectedCellValue(sheetName, 40, 3, null),
						new ExpectedCellValue(sheetName, 41, 3, null),
						new ExpectedCellValue(sheetName, 42, 3, null),
						new ExpectedCellValue(sheetName, 43, 3, null),
						new ExpectedCellValue(sheetName, 44, 3, null),
						new ExpectedCellValue(sheetName, 45, 3, null),
						new ExpectedCellValue(sheetName, 46, 3, null),
						new ExpectedCellValue(sheetName, 47, 3, null),
						new ExpectedCellValue(sheetName, 48, 3, null),
						new ExpectedCellValue(sheetName, 49, 3, null),
						new ExpectedCellValue(sheetName, 50, 3, null),
						new ExpectedCellValue(sheetName, 51, 3, null),
						new ExpectedCellValue(sheetName, 52, 3, null),
						new ExpectedCellValue(sheetName, 53, 3, null),
						new ExpectedCellValue(sheetName, 54, 3, null),
						new ExpectedCellValue(sheetName, 55, 3, null),
						new ExpectedCellValue(sheetName, 56, 3, null),
						new ExpectedCellValue(sheetName, 57, 3, null),
						new ExpectedCellValue(sheetName, 58, 3, null),
						new ExpectedCellValue(sheetName, 59, 3, null),
						new ExpectedCellValue(sheetName, 60, 3, null),
						new ExpectedCellValue(sheetName, 61, 3, null),
						new ExpectedCellValue(sheetName, 62, 3, null),
						new ExpectedCellValue(sheetName, 63, 3, null),
						new ExpectedCellValue(sheetName, 64, 3, null),
						new ExpectedCellValue(sheetName, 65, 3, null),
						new ExpectedCellValue(sheetName, 66, 3, null),
						new ExpectedCellValue(sheetName, 67, 3, null),
						new ExpectedCellValue(sheetName, 68, 3, null),
						new ExpectedCellValue(sheetName, 69, 3, null),
						new ExpectedCellValue(sheetName, 70, 3, null),
						new ExpectedCellValue(sheetName, 71, 3, null),
						new ExpectedCellValue(sheetName, 72, 3, null),
						new ExpectedCellValue(sheetName, 73, 3, null),
						new ExpectedCellValue(sheetName, 74, 3, null),
						new ExpectedCellValue(sheetName, 75, 3, null),
						new ExpectedCellValue(sheetName, 76, 3, null),
						new ExpectedCellValue(sheetName, 77, 3, null),
						new ExpectedCellValue(sheetName, 78, 3, null),
						new ExpectedCellValue(sheetName, 79, 3, null),
						new ExpectedCellValue(sheetName, 80, 3, null),
						new ExpectedCellValue(sheetName, 81, 3, null),
						new ExpectedCellValue(sheetName, 82, 3, null),
						new ExpectedCellValue(sheetName, 83, 3, null),
						new ExpectedCellValue(sheetName, 84, 3, null),
						new ExpectedCellValue(sheetName, 85, 3, null),
						new ExpectedCellValue(sheetName, 86, 3, null),
						new ExpectedCellValue(sheetName, 87, 3, null),
						new ExpectedCellValue(sheetName, 88, 3, null),
						new ExpectedCellValue(sheetName, 89, 3, null),
						new ExpectedCellValue(sheetName, 90, 3, null),
						new ExpectedCellValue(sheetName, 91, 3, null),
						new ExpectedCellValue(sheetName, 92, 3, null),
						new ExpectedCellValue(sheetName, 93, 3, null),
						new ExpectedCellValue(sheetName, 94, 3, null),
						new ExpectedCellValue(sheetName, 95, 3, null),
						new ExpectedCellValue(sheetName, 96, 3, null),
						new ExpectedCellValue(sheetName, 97, 3, null),
						new ExpectedCellValue(sheetName, 98, 3, null),
						new ExpectedCellValue(sheetName, 99, 3, null),
						new ExpectedCellValue(sheetName, 100, 3, null),
						new ExpectedCellValue(sheetName, 101, 3, null),
						new ExpectedCellValue(sheetName, 102, 3, null),
						new ExpectedCellValue(sheetName, 103, 3, null),
						new ExpectedCellValue(sheetName, 104, 3, null),
						new ExpectedCellValue(sheetName, 105, 3, null),
						new ExpectedCellValue(sheetName, 106, 3, null),
						new ExpectedCellValue(sheetName, 107, 3, null),
						new ExpectedCellValue(sheetName, 108, 3, null),
						new ExpectedCellValue(sheetName, 109, 3, null),
						new ExpectedCellValue(sheetName, 110, 3, null),
						new ExpectedCellValue(sheetName, 111, 3, null),
						new ExpectedCellValue(sheetName, 112, 3, null),
						new ExpectedCellValue(sheetName, 113, 3, null),
						new ExpectedCellValue(sheetName, 114, 3, null),
						new ExpectedCellValue(sheetName, 115, 3, null),
						new ExpectedCellValue(sheetName, 116, 3, null),
						new ExpectedCellValue(sheetName, 117, 3, null),
						new ExpectedCellValue(sheetName, 118, 3, null),
						new ExpectedCellValue(sheetName, 119, 3, null),
						new ExpectedCellValue(sheetName, 120, 3, null),
						new ExpectedCellValue(sheetName, 121, 3, null),
						new ExpectedCellValue(sheetName, 122, 3, null),
						new ExpectedCellValue(sheetName, 123, 3, null),
						new ExpectedCellValue(sheetName, 124, 3, null),
						new ExpectedCellValue(sheetName, 125, 3, null),
						new ExpectedCellValue(sheetName, 126, 3, null),
						new ExpectedCellValue(sheetName, 127, 3, null),
						new ExpectedCellValue(sheetName, 128, 3, null),
						new ExpectedCellValue(sheetName, 129, 3, null),
						new ExpectedCellValue(sheetName, 130, 3, null),
						new ExpectedCellValue(sheetName, 131, 3, null),
						new ExpectedCellValue(sheetName, 132, 3, null),
						new ExpectedCellValue(sheetName, 133, 3, null),
						new ExpectedCellValue(sheetName, 134, 3, null),
						new ExpectedCellValue(sheetName, 135, 3, null),
						new ExpectedCellValue(sheetName, 136, 3, null),
						new ExpectedCellValue(sheetName, 137, 3, null),
						new ExpectedCellValue(sheetName, 138, 3, null),
						new ExpectedCellValue(sheetName, 139, 3, null),
						new ExpectedCellValue(sheetName, 140, 3, null),
						new ExpectedCellValue(sheetName, 141, 3, null),
						new ExpectedCellValue(sheetName, 142, 3, null),
						new ExpectedCellValue(sheetName, 143, 3, null),
						new ExpectedCellValue(sheetName, 144, 3, null),
						new ExpectedCellValue(sheetName, 145, 3, null),
						new ExpectedCellValue(sheetName, 146, 3, null),
						new ExpectedCellValue(sheetName, 147, 3, null),
						new ExpectedCellValue(sheetName, 148, 3, null),
						new ExpectedCellValue(sheetName, 149, 3, 415.75),
						new ExpectedCellValue(sheetName, 150, 3, 2d),
						new ExpectedCellValue(sheetName, 151, 3, 831.5),
						new ExpectedCellValue(sheetName, 2, 4, null),
						new ExpectedCellValue(sheetName, 3, 4, "Headlamp"),
						new ExpectedCellValue(sheetName, 4, 4, "Mar"),
						new ExpectedCellValue(sheetName, 5, 4, null),
						new ExpectedCellValue(sheetName, 6, 4, null),
						new ExpectedCellValue(sheetName, 7, 4, null),
						new ExpectedCellValue(sheetName, 8, 4, null),
						new ExpectedCellValue(sheetName, 9, 4, null),
						new ExpectedCellValue(sheetName, 10, 4, null),
						new ExpectedCellValue(sheetName, 11, 4, null),
						new ExpectedCellValue(sheetName, 12, 4, null),
						new ExpectedCellValue(sheetName, 13, 4, null),
						new ExpectedCellValue(sheetName, 14, 4, null),
						new ExpectedCellValue(sheetName, 15, 4, null),
						new ExpectedCellValue(sheetName, 16, 4, null),
						new ExpectedCellValue(sheetName, 17, 4, null),
						new ExpectedCellValue(sheetName, 18, 4, null),
						new ExpectedCellValue(sheetName, 19, 4, null),
						new ExpectedCellValue(sheetName, 20, 4, null),
						new ExpectedCellValue(sheetName, 21, 4, null),
						new ExpectedCellValue(sheetName, 22, 4, null),
						new ExpectedCellValue(sheetName, 23, 4, null),
						new ExpectedCellValue(sheetName, 24, 4, null),
						new ExpectedCellValue(sheetName, 25, 4, null),
						new ExpectedCellValue(sheetName, 26, 4, null),
						new ExpectedCellValue(sheetName, 27, 4, null),
						new ExpectedCellValue(sheetName, 28, 4, null),
						new ExpectedCellValue(sheetName, 29, 4, null),
						new ExpectedCellValue(sheetName, 30, 4, null),
						new ExpectedCellValue(sheetName, 31, 4, null),
						new ExpectedCellValue(sheetName, 32, 4, null),
						new ExpectedCellValue(sheetName, 33, 4, null),
						new ExpectedCellValue(sheetName, 34, 4, null),
						new ExpectedCellValue(sheetName, 35, 4, null),
						new ExpectedCellValue(sheetName, 36, 4, null),
						new ExpectedCellValue(sheetName, 37, 4, null),
						new ExpectedCellValue(sheetName, 38, 4, null),
						new ExpectedCellValue(sheetName, 39, 4, null),
						new ExpectedCellValue(sheetName, 40, 4, null),
						new ExpectedCellValue(sheetName, 41, 4, null),
						new ExpectedCellValue(sheetName, 42, 4, null),
						new ExpectedCellValue(sheetName, 43, 4, null),
						new ExpectedCellValue(sheetName, 44, 4, null),
						new ExpectedCellValue(sheetName, 45, 4, null),
						new ExpectedCellValue(sheetName, 46, 4, null),
						new ExpectedCellValue(sheetName, 47, 4, null),
						new ExpectedCellValue(sheetName, 48, 4, null),
						new ExpectedCellValue(sheetName, 49, 4, null),
						new ExpectedCellValue(sheetName, 50, 4, null),
						new ExpectedCellValue(sheetName, 51, 4, null),
						new ExpectedCellValue(sheetName, 52, 4, null),
						new ExpectedCellValue(sheetName, 53, 4, null),
						new ExpectedCellValue(sheetName, 54, 4, null),
						new ExpectedCellValue(sheetName, 55, 4, null),
						new ExpectedCellValue(sheetName, 56, 4, null),
						new ExpectedCellValue(sheetName, 57, 4, null),
						new ExpectedCellValue(sheetName, 58, 4, null),
						new ExpectedCellValue(sheetName, 59, 4, null),
						new ExpectedCellValue(sheetName, 60, 4, null),
						new ExpectedCellValue(sheetName, 61, 4, null),
						new ExpectedCellValue(sheetName, 62, 4, null),
						new ExpectedCellValue(sheetName, 63, 4, null),
						new ExpectedCellValue(sheetName, 64, 4, null),
						new ExpectedCellValue(sheetName, 65, 4, null),
						new ExpectedCellValue(sheetName, 66, 4, null),
						new ExpectedCellValue(sheetName, 67, 4, null),
						new ExpectedCellValue(sheetName, 68, 4, null),
						new ExpectedCellValue(sheetName, 69, 4, null),
						new ExpectedCellValue(sheetName, 70, 4, null),
						new ExpectedCellValue(sheetName, 71, 4, null),
						new ExpectedCellValue(sheetName, 72, 4, null),
						new ExpectedCellValue(sheetName, 73, 4, null),
						new ExpectedCellValue(sheetName, 74, 4, null),
						new ExpectedCellValue(sheetName, 75, 4, null),
						new ExpectedCellValue(sheetName, 76, 4, null),
						new ExpectedCellValue(sheetName, 77, 4, null),
						new ExpectedCellValue(sheetName, 78, 4, null),
						new ExpectedCellValue(sheetName, 79, 4, null),
						new ExpectedCellValue(sheetName, 80, 4, null),
						new ExpectedCellValue(sheetName, 81, 4, null),
						new ExpectedCellValue(sheetName, 82, 4, null),
						new ExpectedCellValue(sheetName, 83, 4, null),
						new ExpectedCellValue(sheetName, 84, 4, null),
						new ExpectedCellValue(sheetName, 85, 4, null),
						new ExpectedCellValue(sheetName, 86, 4, null),
						new ExpectedCellValue(sheetName, 87, 4, null),
						new ExpectedCellValue(sheetName, 88, 4, null),
						new ExpectedCellValue(sheetName, 89, 4, 24.99),
						new ExpectedCellValue(sheetName, 90, 4, 24.99),
						new ExpectedCellValue(sheetName, 91, 4, 24.99),
						new ExpectedCellValue(sheetName, 92, 4, null),
						new ExpectedCellValue(sheetName, 93, 4, 1d),
						new ExpectedCellValue(sheetName, 94, 4, 1d),
						new ExpectedCellValue(sheetName, 95, 4, 1d),
						new ExpectedCellValue(sheetName, 96, 4, null),
						new ExpectedCellValue(sheetName, 97, 4, 24.99),
						new ExpectedCellValue(sheetName, 98, 4, 24.99),
						new ExpectedCellValue(sheetName, 99, 4, 24.99),
						new ExpectedCellValue(sheetName, 100, 4, 24.99),
						new ExpectedCellValue(sheetName, 101, 4, 1d),
						new ExpectedCellValue(sheetName, 102, 4, 24.99),
						new ExpectedCellValue(sheetName, 103, 4, 24.99),
						new ExpectedCellValue(sheetName, 104, 4, 1d),
						new ExpectedCellValue(sheetName, 105, 4, 24.99),
						new ExpectedCellValue(sheetName, 106, 4, null),
						new ExpectedCellValue(sheetName, 107, 4, null),
						new ExpectedCellValue(sheetName, 108, 4, null),
						new ExpectedCellValue(sheetName, 109, 4, null),
						new ExpectedCellValue(sheetName, 110, 4, null),
						new ExpectedCellValue(sheetName, 111, 4, null),
						new ExpectedCellValue(sheetName, 112, 4, null),
						new ExpectedCellValue(sheetName, 113, 4, null),
						new ExpectedCellValue(sheetName, 114, 4, null),
						new ExpectedCellValue(sheetName, 115, 4, null),
						new ExpectedCellValue(sheetName, 116, 4, null),
						new ExpectedCellValue(sheetName, 117, 4, null),
						new ExpectedCellValue(sheetName, 118, 4, null),
						new ExpectedCellValue(sheetName, 119, 4, null),
						new ExpectedCellValue(sheetName, 120, 4, null),
						new ExpectedCellValue(sheetName, 121, 4, null),
						new ExpectedCellValue(sheetName, 122, 4, null),
						new ExpectedCellValue(sheetName, 123, 4, null),
						new ExpectedCellValue(sheetName, 124, 4, null),
						new ExpectedCellValue(sheetName, 125, 4, null),
						new ExpectedCellValue(sheetName, 126, 4, null),
						new ExpectedCellValue(sheetName, 127, 4, null),
						new ExpectedCellValue(sheetName, 128, 4, null),
						new ExpectedCellValue(sheetName, 129, 4, null),
						new ExpectedCellValue(sheetName, 130, 4, null),
						new ExpectedCellValue(sheetName, 131, 4, null),
						new ExpectedCellValue(sheetName, 132, 4, null),
						new ExpectedCellValue(sheetName, 133, 4, null),
						new ExpectedCellValue(sheetName, 134, 4, null),
						new ExpectedCellValue(sheetName, 135, 4, null),
						new ExpectedCellValue(sheetName, 136, 4, null),
						new ExpectedCellValue(sheetName, 137, 4, null),
						new ExpectedCellValue(sheetName, 138, 4, null),
						new ExpectedCellValue(sheetName, 139, 4, null),
						new ExpectedCellValue(sheetName, 140, 4, null),
						new ExpectedCellValue(sheetName, 141, 4, null),
						new ExpectedCellValue(sheetName, 142, 4, null),
						new ExpectedCellValue(sheetName, 143, 4, null),
						new ExpectedCellValue(sheetName, 144, 4, null),
						new ExpectedCellValue(sheetName, 145, 4, null),
						new ExpectedCellValue(sheetName, 146, 4, 24.99),
						new ExpectedCellValue(sheetName, 147, 4, 1d),
						new ExpectedCellValue(sheetName, 148, 4, 24.99),
						new ExpectedCellValue(sheetName, 149, 4, 24.99),
						new ExpectedCellValue(sheetName, 150, 4, 1d),
						new ExpectedCellValue(sheetName, 151, 4, 24.99),
						new ExpectedCellValue(sheetName, 2, 5, null),
						new ExpectedCellValue(sheetName, 3, 5, "Headlamp Total"),
						new ExpectedCellValue(sheetName, 4, 5, null),
						new ExpectedCellValue(sheetName, 5, 5, null),
						new ExpectedCellValue(sheetName, 6, 5, null),
						new ExpectedCellValue(sheetName, 7, 5, null),
						new ExpectedCellValue(sheetName, 8, 5, null),
						new ExpectedCellValue(sheetName, 9, 5, null),
						new ExpectedCellValue(sheetName, 10, 5, null),
						new ExpectedCellValue(sheetName, 11, 5, null),
						new ExpectedCellValue(sheetName, 12, 5, null),
						new ExpectedCellValue(sheetName, 13, 5, null),
						new ExpectedCellValue(sheetName, 14, 5, null),
						new ExpectedCellValue(sheetName, 15, 5, null),
						new ExpectedCellValue(sheetName, 16, 5, null),
						new ExpectedCellValue(sheetName, 17, 5, null),
						new ExpectedCellValue(sheetName, 18, 5, null),
						new ExpectedCellValue(sheetName, 19, 5, null),
						new ExpectedCellValue(sheetName, 20, 5, null),
						new ExpectedCellValue(sheetName, 21, 5, null),
						new ExpectedCellValue(sheetName, 22, 5, null),
						new ExpectedCellValue(sheetName, 23, 5, null),
						new ExpectedCellValue(sheetName, 24, 5, null),
						new ExpectedCellValue(sheetName, 25, 5, null),
						new ExpectedCellValue(sheetName, 26, 5, null),
						new ExpectedCellValue(sheetName, 27, 5, null),
						new ExpectedCellValue(sheetName, 28, 5, null),
						new ExpectedCellValue(sheetName, 29, 5, null),
						new ExpectedCellValue(sheetName, 30, 5, null),
						new ExpectedCellValue(sheetName, 31, 5, null),
						new ExpectedCellValue(sheetName, 32, 5, null),
						new ExpectedCellValue(sheetName, 33, 5, null),
						new ExpectedCellValue(sheetName, 34, 5, null),
						new ExpectedCellValue(sheetName, 35, 5, null),
						new ExpectedCellValue(sheetName, 36, 5, null),
						new ExpectedCellValue(sheetName, 37, 5, null),
						new ExpectedCellValue(sheetName, 38, 5, null),
						new ExpectedCellValue(sheetName, 39, 5, null),
						new ExpectedCellValue(sheetName, 40, 5, null),
						new ExpectedCellValue(sheetName, 41, 5, null),
						new ExpectedCellValue(sheetName, 42, 5, null),
						new ExpectedCellValue(sheetName, 43, 5, null),
						new ExpectedCellValue(sheetName, 44, 5, null),
						new ExpectedCellValue(sheetName, 45, 5, null),
						new ExpectedCellValue(sheetName, 46, 5, null),
						new ExpectedCellValue(sheetName, 47, 5, null),
						new ExpectedCellValue(sheetName, 48, 5, null),
						new ExpectedCellValue(sheetName, 49, 5, null),
						new ExpectedCellValue(sheetName, 50, 5, null),
						new ExpectedCellValue(sheetName, 51, 5, null),
						new ExpectedCellValue(sheetName, 52, 5, null),
						new ExpectedCellValue(sheetName, 53, 5, null),
						new ExpectedCellValue(sheetName, 54, 5, null),
						new ExpectedCellValue(sheetName, 55, 5, null),
						new ExpectedCellValue(sheetName, 56, 5, null),
						new ExpectedCellValue(sheetName, 57, 5, null),
						new ExpectedCellValue(sheetName, 58, 5, null),
						new ExpectedCellValue(sheetName, 59, 5, null),
						new ExpectedCellValue(sheetName, 60, 5, null),
						new ExpectedCellValue(sheetName, 61, 5, null),
						new ExpectedCellValue(sheetName, 62, 5, null),
						new ExpectedCellValue(sheetName, 63, 5, null),
						new ExpectedCellValue(sheetName, 64, 5, null),
						new ExpectedCellValue(sheetName, 65, 5, null),
						new ExpectedCellValue(sheetName, 66, 5, null),
						new ExpectedCellValue(sheetName, 67, 5, null),
						new ExpectedCellValue(sheetName, 68, 5, null),
						new ExpectedCellValue(sheetName, 69, 5, null),
						new ExpectedCellValue(sheetName, 70, 5, null),
						new ExpectedCellValue(sheetName, 71, 5, null),
						new ExpectedCellValue(sheetName, 72, 5, null),
						new ExpectedCellValue(sheetName, 73, 5, null),
						new ExpectedCellValue(sheetName, 74, 5, null),
						new ExpectedCellValue(sheetName, 75, 5, null),
						new ExpectedCellValue(sheetName, 76, 5, null),
						new ExpectedCellValue(sheetName, 77, 5, null),
						new ExpectedCellValue(sheetName, 78, 5, null),
						new ExpectedCellValue(sheetName, 79, 5, null),
						new ExpectedCellValue(sheetName, 80, 5, null),
						new ExpectedCellValue(sheetName, 81, 5, null),
						new ExpectedCellValue(sheetName, 82, 5, null),
						new ExpectedCellValue(sheetName, 83, 5, null),
						new ExpectedCellValue(sheetName, 84, 5, null),
						new ExpectedCellValue(sheetName, 85, 5, null),
						new ExpectedCellValue(sheetName, 86, 5, null),
						new ExpectedCellValue(sheetName, 87, 5, null),
						new ExpectedCellValue(sheetName, 88, 5, null),
						new ExpectedCellValue(sheetName, 89, 5, 24.99),
						new ExpectedCellValue(sheetName, 90, 5, 24.99),
						new ExpectedCellValue(sheetName, 91, 5, 24.99),
						new ExpectedCellValue(sheetName, 92, 5, null),
						new ExpectedCellValue(sheetName, 93, 5, 1d),
						new ExpectedCellValue(sheetName, 94, 5, 1d),
						new ExpectedCellValue(sheetName, 95, 5, 1d),
						new ExpectedCellValue(sheetName, 96, 5, null),
						new ExpectedCellValue(sheetName, 97, 5, 24.99),
						new ExpectedCellValue(sheetName, 98, 5, 24.99),
						new ExpectedCellValue(sheetName, 99, 5, 24.99),
						new ExpectedCellValue(sheetName, 100, 5, 24.99),
						new ExpectedCellValue(sheetName, 101, 5, 1d),
						new ExpectedCellValue(sheetName, 102, 5, 24.99),
						new ExpectedCellValue(sheetName, 103, 5, 24.99),
						new ExpectedCellValue(sheetName, 104, 5, 1d),
						new ExpectedCellValue(sheetName, 105, 5, 24.99),
						new ExpectedCellValue(sheetName, 106, 5, null),
						new ExpectedCellValue(sheetName, 107, 5, null),
						new ExpectedCellValue(sheetName, 108, 5, null),
						new ExpectedCellValue(sheetName, 109, 5, null),
						new ExpectedCellValue(sheetName, 110, 5, null),
						new ExpectedCellValue(sheetName, 111, 5, null),
						new ExpectedCellValue(sheetName, 112, 5, null),
						new ExpectedCellValue(sheetName, 113, 5, null),
						new ExpectedCellValue(sheetName, 114, 5, null),
						new ExpectedCellValue(sheetName, 115, 5, null),
						new ExpectedCellValue(sheetName, 116, 5, null),
						new ExpectedCellValue(sheetName, 117, 5, null),
						new ExpectedCellValue(sheetName, 118, 5, null),
						new ExpectedCellValue(sheetName, 119, 5, null),
						new ExpectedCellValue(sheetName, 120, 5, null),
						new ExpectedCellValue(sheetName, 121, 5, null),
						new ExpectedCellValue(sheetName, 122, 5, null),
						new ExpectedCellValue(sheetName, 123, 5, null),
						new ExpectedCellValue(sheetName, 124, 5, null),
						new ExpectedCellValue(sheetName, 125, 5, null),
						new ExpectedCellValue(sheetName, 126, 5, null),
						new ExpectedCellValue(sheetName, 127, 5, null),
						new ExpectedCellValue(sheetName, 128, 5, null),
						new ExpectedCellValue(sheetName, 129, 5, null),
						new ExpectedCellValue(sheetName, 130, 5, null),
						new ExpectedCellValue(sheetName, 131, 5, null),
						new ExpectedCellValue(sheetName, 132, 5, null),
						new ExpectedCellValue(sheetName, 133, 5, null),
						new ExpectedCellValue(sheetName, 134, 5, null),
						new ExpectedCellValue(sheetName, 135, 5, null),
						new ExpectedCellValue(sheetName, 136, 5, null),
						new ExpectedCellValue(sheetName, 137, 5, null),
						new ExpectedCellValue(sheetName, 138, 5, null),
						new ExpectedCellValue(sheetName, 139, 5, null),
						new ExpectedCellValue(sheetName, 140, 5, null),
						new ExpectedCellValue(sheetName, 141, 5, null),
						new ExpectedCellValue(sheetName, 142, 5, null),
						new ExpectedCellValue(sheetName, 143, 5, null),
						new ExpectedCellValue(sheetName, 144, 5, null),
						new ExpectedCellValue(sheetName, 145, 5, null),
						new ExpectedCellValue(sheetName, 146, 5, 24.99),
						new ExpectedCellValue(sheetName, 147, 5, 1d),
						new ExpectedCellValue(sheetName, 148, 5, 24.99),
						new ExpectedCellValue(sheetName, 149, 5, 24.99),
						new ExpectedCellValue(sheetName, 150, 5, 1d),
						new ExpectedCellValue(sheetName, 151, 5, 24.99),
						new ExpectedCellValue(sheetName, 2, 6, "Chicago Total"),
						new ExpectedCellValue(sheetName, 3, 6, null),
						new ExpectedCellValue(sheetName, 4, 6, null),
						new ExpectedCellValue(sheetName, 5, 6, null),
						new ExpectedCellValue(sheetName, 6, 6, null),
						new ExpectedCellValue(sheetName, 7, 6, null),
						new ExpectedCellValue(sheetName, 8, 6, null),
						new ExpectedCellValue(sheetName, 9, 6, 415.75),
						new ExpectedCellValue(sheetName, 10, 6, 415.75),
						new ExpectedCellValue(sheetName, 11, 6, 415.75),
						new ExpectedCellValue(sheetName, 12, 6, null),
						new ExpectedCellValue(sheetName, 13, 6, 2d),
						new ExpectedCellValue(sheetName, 14, 6, 2d),
						new ExpectedCellValue(sheetName, 15, 6, 2d),
						new ExpectedCellValue(sheetName, 16, 6, null),
						new ExpectedCellValue(sheetName, 17, 6, 831.5),
						new ExpectedCellValue(sheetName, 18, 6, 831.5),
						new ExpectedCellValue(sheetName, 19, 6, 831.5),
						new ExpectedCellValue(sheetName, 20, 6, 415.75),
						new ExpectedCellValue(sheetName, 21, 6, 2d),
						new ExpectedCellValue(sheetName, 22, 6, 831.5),
						new ExpectedCellValue(sheetName, 23, 6, 415.75),
						new ExpectedCellValue(sheetName, 24, 6, 2d),
						new ExpectedCellValue(sheetName, 25, 6, 831.5),
						new ExpectedCellValue(sheetName, 26, 6, 415.75),
						new ExpectedCellValue(sheetName, 27, 6, 2d),
						new ExpectedCellValue(sheetName, 28, 6, 831.5),
						new ExpectedCellValue(sheetName, 29, 6, null),
						new ExpectedCellValue(sheetName, 30, 6, null),
						new ExpectedCellValue(sheetName, 31, 6, null),
						new ExpectedCellValue(sheetName, 32, 6, null),
						new ExpectedCellValue(sheetName, 33, 6, null),
						new ExpectedCellValue(sheetName, 34, 6, null),
						new ExpectedCellValue(sheetName, 35, 6, null),
						new ExpectedCellValue(sheetName, 36, 6, null),
						new ExpectedCellValue(sheetName, 37, 6, null),
						new ExpectedCellValue(sheetName, 38, 6, null),
						new ExpectedCellValue(sheetName, 39, 6, null),
						new ExpectedCellValue(sheetName, 40, 6, null),
						new ExpectedCellValue(sheetName, 41, 6, null),
						new ExpectedCellValue(sheetName, 42, 6, null),
						new ExpectedCellValue(sheetName, 43, 6, null),
						new ExpectedCellValue(sheetName, 44, 6, null),
						new ExpectedCellValue(sheetName, 45, 6, null),
						new ExpectedCellValue(sheetName, 46, 6, null),
						new ExpectedCellValue(sheetName, 47, 6, null),
						new ExpectedCellValue(sheetName, 48, 6, null),
						new ExpectedCellValue(sheetName, 49, 6, null),
						new ExpectedCellValue(sheetName, 50, 6, null),
						new ExpectedCellValue(sheetName, 51, 6, null),
						new ExpectedCellValue(sheetName, 52, 6, null),
						new ExpectedCellValue(sheetName, 53, 6, null),
						new ExpectedCellValue(sheetName, 54, 6, null),
						new ExpectedCellValue(sheetName, 55, 6, null),
						new ExpectedCellValue(sheetName, 56, 6, null),
						new ExpectedCellValue(sheetName, 57, 6, null),
						new ExpectedCellValue(sheetName, 58, 6, null),
						new ExpectedCellValue(sheetName, 59, 6, null),
						new ExpectedCellValue(sheetName, 60, 6, null),
						new ExpectedCellValue(sheetName, 61, 6, null),
						new ExpectedCellValue(sheetName, 62, 6, null),
						new ExpectedCellValue(sheetName, 63, 6, null),
						new ExpectedCellValue(sheetName, 64, 6, null),
						new ExpectedCellValue(sheetName, 65, 6, null),
						new ExpectedCellValue(sheetName, 66, 6, null),
						new ExpectedCellValue(sheetName, 67, 6, null),
						new ExpectedCellValue(sheetName, 68, 6, null),
						new ExpectedCellValue(sheetName, 69, 6, null),
						new ExpectedCellValue(sheetName, 70, 6, null),
						new ExpectedCellValue(sheetName, 71, 6, null),
						new ExpectedCellValue(sheetName, 72, 6, null),
						new ExpectedCellValue(sheetName, 73, 6, null),
						new ExpectedCellValue(sheetName, 74, 6, null),
						new ExpectedCellValue(sheetName, 75, 6, null),
						new ExpectedCellValue(sheetName, 76, 6, null),
						new ExpectedCellValue(sheetName, 77, 6, null),
						new ExpectedCellValue(sheetName, 78, 6, null),
						new ExpectedCellValue(sheetName, 79, 6, null),
						new ExpectedCellValue(sheetName, 80, 6, null),
						new ExpectedCellValue(sheetName, 81, 6, null),
						new ExpectedCellValue(sheetName, 82, 6, null),
						new ExpectedCellValue(sheetName, 83, 6, null),
						new ExpectedCellValue(sheetName, 84, 6, null),
						new ExpectedCellValue(sheetName, 85, 6, null),
						new ExpectedCellValue(sheetName, 86, 6, null),
						new ExpectedCellValue(sheetName, 87, 6, null),
						new ExpectedCellValue(sheetName, 88, 6, null),
						new ExpectedCellValue(sheetName, 89, 6, 24.99),
						new ExpectedCellValue(sheetName, 90, 6, 24.99),
						new ExpectedCellValue(sheetName, 91, 6, 24.99),
						new ExpectedCellValue(sheetName, 92, 6, null),
						new ExpectedCellValue(sheetName, 93, 6, 1d),
						new ExpectedCellValue(sheetName, 94, 6, 1d),
						new ExpectedCellValue(sheetName, 95, 6, 1d),
						new ExpectedCellValue(sheetName, 96, 6, null),
						new ExpectedCellValue(sheetName, 97, 6, 24.99),
						new ExpectedCellValue(sheetName, 98, 6, 24.99),
						new ExpectedCellValue(sheetName, 99, 6, 24.99),
						new ExpectedCellValue(sheetName, 100, 6, 24.99),
						new ExpectedCellValue(sheetName, 101, 6, 1d),
						new ExpectedCellValue(sheetName, 102, 6, 24.99),
						new ExpectedCellValue(sheetName, 103, 6, 24.99),
						new ExpectedCellValue(sheetName, 104, 6, 1d),
						new ExpectedCellValue(sheetName, 105, 6, 24.99),
						new ExpectedCellValue(sheetName, 106, 6, null),
						new ExpectedCellValue(sheetName, 107, 6, null),
						new ExpectedCellValue(sheetName, 108, 6, null),
						new ExpectedCellValue(sheetName, 109, 6, null),
						new ExpectedCellValue(sheetName, 110, 6, null),
						new ExpectedCellValue(sheetName, 111, 6, null),
						new ExpectedCellValue(sheetName, 112, 6, null),
						new ExpectedCellValue(sheetName, 113, 6, null),
						new ExpectedCellValue(sheetName, 114, 6, null),
						new ExpectedCellValue(sheetName, 115, 6, null),
						new ExpectedCellValue(sheetName, 116, 6, null),
						new ExpectedCellValue(sheetName, 117, 6, null),
						new ExpectedCellValue(sheetName, 118, 6, null),
						new ExpectedCellValue(sheetName, 119, 6, null),
						new ExpectedCellValue(sheetName, 120, 6, null),
						new ExpectedCellValue(sheetName, 121, 6, null),
						new ExpectedCellValue(sheetName, 122, 6, null),
						new ExpectedCellValue(sheetName, 123, 6, null),
						new ExpectedCellValue(sheetName, 124, 6, null),
						new ExpectedCellValue(sheetName, 125, 6, null),
						new ExpectedCellValue(sheetName, 126, 6, null),
						new ExpectedCellValue(sheetName, 127, 6, null),
						new ExpectedCellValue(sheetName, 128, 6, null),
						new ExpectedCellValue(sheetName, 129, 6, null),
						new ExpectedCellValue(sheetName, 130, 6, null),
						new ExpectedCellValue(sheetName, 131, 6, null),
						new ExpectedCellValue(sheetName, 132, 6, null),
						new ExpectedCellValue(sheetName, 133, 6, null),
						new ExpectedCellValue(sheetName, 134, 6, null),
						new ExpectedCellValue(sheetName, 135, 6, null),
						new ExpectedCellValue(sheetName, 136, 6, null),
						new ExpectedCellValue(sheetName, 137, 6, null),
						new ExpectedCellValue(sheetName, 138, 6, null),
						new ExpectedCellValue(sheetName, 139, 6, null),
						new ExpectedCellValue(sheetName, 140, 6, null),
						new ExpectedCellValue(sheetName, 141, 6, null),
						new ExpectedCellValue(sheetName, 142, 6, null),
						new ExpectedCellValue(sheetName, 143, 6, null),
						new ExpectedCellValue(sheetName, 144, 6, null),
						new ExpectedCellValue(sheetName, 145, 6, null),
						new ExpectedCellValue(sheetName, 146, 6, 24.99),
						new ExpectedCellValue(sheetName, 147, 6, 1d),
						new ExpectedCellValue(sheetName, 148, 6, 24.99),
						new ExpectedCellValue(sheetName, 149, 6, 440.74),
						new ExpectedCellValue(sheetName, 150, 6, 3d),
						new ExpectedCellValue(sheetName, 151, 6, 856.49),
						new ExpectedCellValue(sheetName, 2, 7, "Nashville"),
						new ExpectedCellValue(sheetName, 3, 7, "Car Rack"),
						new ExpectedCellValue(sheetName, 4, 7, "Feb"),
						new ExpectedCellValue(sheetName, 5, 7, null),
						new ExpectedCellValue(sheetName, 6, 7, null),
						new ExpectedCellValue(sheetName, 7, 7, null),
						new ExpectedCellValue(sheetName, 8, 7, null),
						new ExpectedCellValue(sheetName, 9, 7, null),
						new ExpectedCellValue(sheetName, 10, 7, null),
						new ExpectedCellValue(sheetName, 11, 7, null),
						new ExpectedCellValue(sheetName, 12, 7, null),
						new ExpectedCellValue(sheetName, 13, 7, null),
						new ExpectedCellValue(sheetName, 14, 7, null),
						new ExpectedCellValue(sheetName, 15, 7, null),
						new ExpectedCellValue(sheetName, 16, 7, null),
						new ExpectedCellValue(sheetName, 17, 7, null),
						new ExpectedCellValue(sheetName, 18, 7, null),
						new ExpectedCellValue(sheetName, 19, 7, null),
						new ExpectedCellValue(sheetName, 20, 7, null),
						new ExpectedCellValue(sheetName, 21, 7, null),
						new ExpectedCellValue(sheetName, 22, 7, null),
						new ExpectedCellValue(sheetName, 23, 7, null),
						new ExpectedCellValue(sheetName, 24, 7, null),
						new ExpectedCellValue(sheetName, 25, 7, null),
						new ExpectedCellValue(sheetName, 26, 7, null),
						new ExpectedCellValue(sheetName, 27, 7, null),
						new ExpectedCellValue(sheetName, 28, 7, null),
						new ExpectedCellValue(sheetName, 29, 7, null),
						new ExpectedCellValue(sheetName, 30, 7, null),
						new ExpectedCellValue(sheetName, 31, 7, null),
						new ExpectedCellValue(sheetName, 32, 7, null),
						new ExpectedCellValue(sheetName, 33, 7, null),
						new ExpectedCellValue(sheetName, 34, 7, null),
						new ExpectedCellValue(sheetName, 35, 7, null),
						new ExpectedCellValue(sheetName, 36, 7, null),
						new ExpectedCellValue(sheetName, 37, 7, null),
						new ExpectedCellValue(sheetName, 38, 7, null),
						new ExpectedCellValue(sheetName, 39, 7, null),
						new ExpectedCellValue(sheetName, 40, 7, null),
						new ExpectedCellValue(sheetName, 41, 7, null),
						new ExpectedCellValue(sheetName, 42, 7, null),
						new ExpectedCellValue(sheetName, 43, 7, null),
						new ExpectedCellValue(sheetName, 44, 7, null),
						new ExpectedCellValue(sheetName, 45, 7, null),
						new ExpectedCellValue(sheetName, 46, 7, null),
						new ExpectedCellValue(sheetName, 47, 7, null),
						new ExpectedCellValue(sheetName, 48, 7, null),
						new ExpectedCellValue(sheetName, 49, 7, null),
						new ExpectedCellValue(sheetName, 50, 7, null),
						new ExpectedCellValue(sheetName, 51, 7, null),
						new ExpectedCellValue(sheetName, 52, 7, null),
						new ExpectedCellValue(sheetName, 53, 7, null),
						new ExpectedCellValue(sheetName, 54, 7, null),
						new ExpectedCellValue(sheetName, 55, 7, null),
						new ExpectedCellValue(sheetName, 56, 7, null),
						new ExpectedCellValue(sheetName, 57, 7, null),
						new ExpectedCellValue(sheetName, 58, 7, null),
						new ExpectedCellValue(sheetName, 59, 7, null),
						new ExpectedCellValue(sheetName, 60, 7, null),
						new ExpectedCellValue(sheetName, 61, 7, null),
						new ExpectedCellValue(sheetName, 62, 7, null),
						new ExpectedCellValue(sheetName, 63, 7, null),
						new ExpectedCellValue(sheetName, 64, 7, null),
						new ExpectedCellValue(sheetName, 65, 7, null),
						new ExpectedCellValue(sheetName, 66, 7, null),
						new ExpectedCellValue(sheetName, 67, 7, null),
						new ExpectedCellValue(sheetName, 68, 7, null),
						new ExpectedCellValue(sheetName, 69, 7, null),
						new ExpectedCellValue(sheetName, 70, 7, null),
						new ExpectedCellValue(sheetName, 71, 7, null),
						new ExpectedCellValue(sheetName, 72, 7, null),
						new ExpectedCellValue(sheetName, 73, 7, 415.75),
						new ExpectedCellValue(sheetName, 74, 7, 415.75),
						new ExpectedCellValue(sheetName, 75, 7, 415.75),
						new ExpectedCellValue(sheetName, 76, 7, null),
						new ExpectedCellValue(sheetName, 77, 7, 2d),
						new ExpectedCellValue(sheetName, 78, 7, 2d),
						new ExpectedCellValue(sheetName, 79, 7, 2d),
						new ExpectedCellValue(sheetName, 80, 7, null),
						new ExpectedCellValue(sheetName, 81, 7, 831.5),
						new ExpectedCellValue(sheetName, 82, 7, 831.5),
						new ExpectedCellValue(sheetName, 83, 7, 831.5),
						new ExpectedCellValue(sheetName, 84, 7, 415.75),
						new ExpectedCellValue(sheetName, 85, 7, 2d),
						new ExpectedCellValue(sheetName, 86, 7, 831.5),
						new ExpectedCellValue(sheetName, 87, 7, null),
						new ExpectedCellValue(sheetName, 88, 7, null),
						new ExpectedCellValue(sheetName, 89, 7, null),
						new ExpectedCellValue(sheetName, 90, 7, null),
						new ExpectedCellValue(sheetName, 91, 7, null),
						new ExpectedCellValue(sheetName, 92, 7, null),
						new ExpectedCellValue(sheetName, 93, 7, null),
						new ExpectedCellValue(sheetName, 94, 7, null),
						new ExpectedCellValue(sheetName, 95, 7, null),
						new ExpectedCellValue(sheetName, 96, 7, null),
						new ExpectedCellValue(sheetName, 97, 7, null),
						new ExpectedCellValue(sheetName, 98, 7, null),
						new ExpectedCellValue(sheetName, 99, 7, null),
						new ExpectedCellValue(sheetName, 100, 7, null),
						new ExpectedCellValue(sheetName, 101, 7, null),
						new ExpectedCellValue(sheetName, 102, 7, null),
						new ExpectedCellValue(sheetName, 103, 7, 415.75),
						new ExpectedCellValue(sheetName, 104, 7, 2d),
						new ExpectedCellValue(sheetName, 105, 7, 831.5),
						new ExpectedCellValue(sheetName, 106, 7, null),
						new ExpectedCellValue(sheetName, 107, 7, null),
						new ExpectedCellValue(sheetName, 108, 7, null),
						new ExpectedCellValue(sheetName, 109, 7, null),
						new ExpectedCellValue(sheetName, 110, 7, null),
						new ExpectedCellValue(sheetName, 111, 7, null),
						new ExpectedCellValue(sheetName, 112, 7, null),
						new ExpectedCellValue(sheetName, 113, 7, null),
						new ExpectedCellValue(sheetName, 114, 7, null),
						new ExpectedCellValue(sheetName, 115, 7, null),
						new ExpectedCellValue(sheetName, 116, 7, null),
						new ExpectedCellValue(sheetName, 117, 7, null),
						new ExpectedCellValue(sheetName, 118, 7, null),
						new ExpectedCellValue(sheetName, 119, 7, null),
						new ExpectedCellValue(sheetName, 120, 7, null),
						new ExpectedCellValue(sheetName, 121, 7, null),
						new ExpectedCellValue(sheetName, 122, 7, null),
						new ExpectedCellValue(sheetName, 123, 7, null),
						new ExpectedCellValue(sheetName, 124, 7, null),
						new ExpectedCellValue(sheetName, 125, 7, null),
						new ExpectedCellValue(sheetName, 126, 7, null),
						new ExpectedCellValue(sheetName, 127, 7, null),
						new ExpectedCellValue(sheetName, 128, 7, null),
						new ExpectedCellValue(sheetName, 129, 7, null),
						new ExpectedCellValue(sheetName, 130, 7, null),
						new ExpectedCellValue(sheetName, 131, 7, null),
						new ExpectedCellValue(sheetName, 132, 7, null),
						new ExpectedCellValue(sheetName, 133, 7, null),
						new ExpectedCellValue(sheetName, 134, 7, null),
						new ExpectedCellValue(sheetName, 135, 7, null),
						new ExpectedCellValue(sheetName, 136, 7, null),
						new ExpectedCellValue(sheetName, 137, 7, null),
						new ExpectedCellValue(sheetName, 138, 7, null),
						new ExpectedCellValue(sheetName, 139, 7, null),
						new ExpectedCellValue(sheetName, 140, 7, null),
						new ExpectedCellValue(sheetName, 141, 7, null),
						new ExpectedCellValue(sheetName, 142, 7, null),
						new ExpectedCellValue(sheetName, 143, 7, null),
						new ExpectedCellValue(sheetName, 144, 7, null),
						new ExpectedCellValue(sheetName, 145, 7, null),
						new ExpectedCellValue(sheetName, 146, 7, 415.75),
						new ExpectedCellValue(sheetName, 147, 7, 2d),
						new ExpectedCellValue(sheetName, 148, 7, 831.5),
						new ExpectedCellValue(sheetName, 149, 7, 415.75),
						new ExpectedCellValue(sheetName, 150, 7, 2d),
						new ExpectedCellValue(sheetName, 151, 7, 831.5),
						new ExpectedCellValue(sheetName, 2, 8, null),
						new ExpectedCellValue(sheetName, 3, 8, null),
						new ExpectedCellValue(sheetName, 4, 8, "Oct"),
						new ExpectedCellValue(sheetName, 5, 8, null),
						new ExpectedCellValue(sheetName, 6, 8, null),
						new ExpectedCellValue(sheetName, 7, 8, null),
						new ExpectedCellValue(sheetName, 8, 8, null),
						new ExpectedCellValue(sheetName, 9, 8, null),
						new ExpectedCellValue(sheetName, 10, 8, null),
						new ExpectedCellValue(sheetName, 11, 8, null),
						new ExpectedCellValue(sheetName, 12, 8, null),
						new ExpectedCellValue(sheetName, 13, 8, null),
						new ExpectedCellValue(sheetName, 14, 8, null),
						new ExpectedCellValue(sheetName, 15, 8, null),
						new ExpectedCellValue(sheetName, 16, 8, null),
						new ExpectedCellValue(sheetName, 17, 8, null),
						new ExpectedCellValue(sheetName, 18, 8, null),
						new ExpectedCellValue(sheetName, 19, 8, null),
						new ExpectedCellValue(sheetName, 20, 8, null),
						new ExpectedCellValue(sheetName, 21, 8, null),
						new ExpectedCellValue(sheetName, 22, 8, null),
						new ExpectedCellValue(sheetName, 23, 8, null),
						new ExpectedCellValue(sheetName, 24, 8, null),
						new ExpectedCellValue(sheetName, 25, 8, null),
						new ExpectedCellValue(sheetName, 26, 8, null),
						new ExpectedCellValue(sheetName, 27, 8, null),
						new ExpectedCellValue(sheetName, 28, 8, null),
						new ExpectedCellValue(sheetName, 29, 8, null),
						new ExpectedCellValue(sheetName, 30, 8, null),
						new ExpectedCellValue(sheetName, 31, 8, null),
						new ExpectedCellValue(sheetName, 32, 8, null),
						new ExpectedCellValue(sheetName, 33, 8, 415.75),
						new ExpectedCellValue(sheetName, 34, 8, 415.75),
						new ExpectedCellValue(sheetName, 35, 8, 415.75),
						new ExpectedCellValue(sheetName, 36, 8, null),
						new ExpectedCellValue(sheetName, 37, 8, 2d),
						new ExpectedCellValue(sheetName, 38, 8, 2d),
						new ExpectedCellValue(sheetName, 39, 8, 2d),
						new ExpectedCellValue(sheetName, 40, 8, null),
						new ExpectedCellValue(sheetName, 41, 8, 831.5),
						new ExpectedCellValue(sheetName, 42, 8, 831.5),
						new ExpectedCellValue(sheetName, 43, 8, 831.5),
						new ExpectedCellValue(sheetName, 44, 8, 415.75),
						new ExpectedCellValue(sheetName, 45, 8, 2d),
						new ExpectedCellValue(sheetName, 46, 8, 831.5),
						new ExpectedCellValue(sheetName, 47, 8, null),
						new ExpectedCellValue(sheetName, 48, 8, null),
						new ExpectedCellValue(sheetName, 49, 8, null),
						new ExpectedCellValue(sheetName, 50, 8, null),
						new ExpectedCellValue(sheetName, 51, 8, null),
						new ExpectedCellValue(sheetName, 52, 8, null),
						new ExpectedCellValue(sheetName, 53, 8, null),
						new ExpectedCellValue(sheetName, 54, 8, null),
						new ExpectedCellValue(sheetName, 55, 8, null),
						new ExpectedCellValue(sheetName, 56, 8, null),
						new ExpectedCellValue(sheetName, 57, 8, null),
						new ExpectedCellValue(sheetName, 58, 8, null),
						new ExpectedCellValue(sheetName, 59, 8, null),
						new ExpectedCellValue(sheetName, 60, 8, null),
						new ExpectedCellValue(sheetName, 61, 8, null),
						new ExpectedCellValue(sheetName, 62, 8, null),
						new ExpectedCellValue(sheetName, 63, 8, 415.75),
						new ExpectedCellValue(sheetName, 64, 8, 2d),
						new ExpectedCellValue(sheetName, 65, 8, 831.5),
						new ExpectedCellValue(sheetName, 66, 8, 415.75),
						new ExpectedCellValue(sheetName, 67, 8, 2d),
						new ExpectedCellValue(sheetName, 68, 8, 831.5),
						new ExpectedCellValue(sheetName, 69, 8, null),
						new ExpectedCellValue(sheetName, 70, 8, null),
						new ExpectedCellValue(sheetName, 71, 8, null),
						new ExpectedCellValue(sheetName, 72, 8, null),
						new ExpectedCellValue(sheetName, 73, 8, null),
						new ExpectedCellValue(sheetName, 74, 8, null),
						new ExpectedCellValue(sheetName, 75, 8, null),
						new ExpectedCellValue(sheetName, 76, 8, null),
						new ExpectedCellValue(sheetName, 77, 8, null),
						new ExpectedCellValue(sheetName, 78, 8, null),
						new ExpectedCellValue(sheetName, 79, 8, null),
						new ExpectedCellValue(sheetName, 80, 8, null),
						new ExpectedCellValue(sheetName, 81, 8, null),
						new ExpectedCellValue(sheetName, 82, 8, null),
						new ExpectedCellValue(sheetName, 83, 8, null),
						new ExpectedCellValue(sheetName, 84, 8, null),
						new ExpectedCellValue(sheetName, 85, 8, null),
						new ExpectedCellValue(sheetName, 86, 8, null),
						new ExpectedCellValue(sheetName, 87, 8, null),
						new ExpectedCellValue(sheetName, 88, 8, null),
						new ExpectedCellValue(sheetName, 89, 8, null),
						new ExpectedCellValue(sheetName, 90, 8, null),
						new ExpectedCellValue(sheetName, 91, 8, null),
						new ExpectedCellValue(sheetName, 92, 8, null),
						new ExpectedCellValue(sheetName, 93, 8, null),
						new ExpectedCellValue(sheetName, 94, 8, null),
						new ExpectedCellValue(sheetName, 95, 8, null),
						new ExpectedCellValue(sheetName, 96, 8, null),
						new ExpectedCellValue(sheetName, 97, 8, null),
						new ExpectedCellValue(sheetName, 98, 8, null),
						new ExpectedCellValue(sheetName, 99, 8, null),
						new ExpectedCellValue(sheetName, 100, 8, null),
						new ExpectedCellValue(sheetName, 101, 8, null),
						new ExpectedCellValue(sheetName, 102, 8, null),
						new ExpectedCellValue(sheetName, 103, 8, null),
						new ExpectedCellValue(sheetName, 104, 8, null),
						new ExpectedCellValue(sheetName, 105, 8, null),
						new ExpectedCellValue(sheetName, 106, 8, null),
						new ExpectedCellValue(sheetName, 107, 8, null),
						new ExpectedCellValue(sheetName, 108, 8, null),
						new ExpectedCellValue(sheetName, 109, 8, null),
						new ExpectedCellValue(sheetName, 110, 8, null),
						new ExpectedCellValue(sheetName, 111, 8, null),
						new ExpectedCellValue(sheetName, 112, 8, null),
						new ExpectedCellValue(sheetName, 113, 8, null),
						new ExpectedCellValue(sheetName, 114, 8, null),
						new ExpectedCellValue(sheetName, 115, 8, null),
						new ExpectedCellValue(sheetName, 116, 8, null),
						new ExpectedCellValue(sheetName, 117, 8, null),
						new ExpectedCellValue(sheetName, 118, 8, null),
						new ExpectedCellValue(sheetName, 119, 8, null),
						new ExpectedCellValue(sheetName, 120, 8, null),
						new ExpectedCellValue(sheetName, 121, 8, null),
						new ExpectedCellValue(sheetName, 122, 8, null),
						new ExpectedCellValue(sheetName, 123, 8, null),
						new ExpectedCellValue(sheetName, 124, 8, null),
						new ExpectedCellValue(sheetName, 125, 8, null),
						new ExpectedCellValue(sheetName, 126, 8, null),
						new ExpectedCellValue(sheetName, 127, 8, null),
						new ExpectedCellValue(sheetName, 128, 8, null),
						new ExpectedCellValue(sheetName, 129, 8, null),
						new ExpectedCellValue(sheetName, 130, 8, null),
						new ExpectedCellValue(sheetName, 131, 8, null),
						new ExpectedCellValue(sheetName, 132, 8, null),
						new ExpectedCellValue(sheetName, 133, 8, null),
						new ExpectedCellValue(sheetName, 134, 8, null),
						new ExpectedCellValue(sheetName, 135, 8, null),
						new ExpectedCellValue(sheetName, 136, 8, null),
						new ExpectedCellValue(sheetName, 137, 8, null),
						new ExpectedCellValue(sheetName, 138, 8, null),
						new ExpectedCellValue(sheetName, 139, 8, null),
						new ExpectedCellValue(sheetName, 140, 8, null),
						new ExpectedCellValue(sheetName, 141, 8, null),
						new ExpectedCellValue(sheetName, 142, 8, null),
						new ExpectedCellValue(sheetName, 143, 8, null),
						new ExpectedCellValue(sheetName, 144, 8, null),
						new ExpectedCellValue(sheetName, 145, 8, null),
						new ExpectedCellValue(sheetName, 146, 8, null),
						new ExpectedCellValue(sheetName, 147, 8, null),
						new ExpectedCellValue(sheetName, 148, 8, null),
						new ExpectedCellValue(sheetName, 149, 8, 415.75),
						new ExpectedCellValue(sheetName, 150, 8, 2d),
						new ExpectedCellValue(sheetName, 151, 8, 831.5),
						new ExpectedCellValue(sheetName, 2, 9, null),
						new ExpectedCellValue(sheetName, 3, 9, "Car Rack Total"),
						new ExpectedCellValue(sheetName, 4, 9, null),
						new ExpectedCellValue(sheetName, 5, 9, null),
						new ExpectedCellValue(sheetName, 6, 9, null),
						new ExpectedCellValue(sheetName, 7, 9, null),
						new ExpectedCellValue(sheetName, 8, 9, null),
						new ExpectedCellValue(sheetName, 9, 9, null),
						new ExpectedCellValue(sheetName, 10, 9, null),
						new ExpectedCellValue(sheetName, 11, 9, null),
						new ExpectedCellValue(sheetName, 12, 9, null),
						new ExpectedCellValue(sheetName, 13, 9, null),
						new ExpectedCellValue(sheetName, 14, 9, null),
						new ExpectedCellValue(sheetName, 15, 9, null),
						new ExpectedCellValue(sheetName, 16, 9, null),
						new ExpectedCellValue(sheetName, 17, 9, null),
						new ExpectedCellValue(sheetName, 18, 9, null),
						new ExpectedCellValue(sheetName, 19, 9, null),
						new ExpectedCellValue(sheetName, 20, 9, null),
						new ExpectedCellValue(sheetName, 21, 9, null),
						new ExpectedCellValue(sheetName, 22, 9, null),
						new ExpectedCellValue(sheetName, 23, 9, null),
						new ExpectedCellValue(sheetName, 24, 9, null),
						new ExpectedCellValue(sheetName, 25, 9, null),
						new ExpectedCellValue(sheetName, 26, 9, null),
						new ExpectedCellValue(sheetName, 27, 9, null),
						new ExpectedCellValue(sheetName, 28, 9, null),
						new ExpectedCellValue(sheetName, 29, 9, null),
						new ExpectedCellValue(sheetName, 30, 9, null),
						new ExpectedCellValue(sheetName, 31, 9, null),
						new ExpectedCellValue(sheetName, 32, 9, null),
						new ExpectedCellValue(sheetName, 33, 9, 415.75),
						new ExpectedCellValue(sheetName, 34, 9, 415.75),
						new ExpectedCellValue(sheetName, 35, 9, 415.75),
						new ExpectedCellValue(sheetName, 36, 9, null),
						new ExpectedCellValue(sheetName, 37, 9, 2d),
						new ExpectedCellValue(sheetName, 38, 9, 2d),
						new ExpectedCellValue(sheetName, 39, 9, 2d),
						new ExpectedCellValue(sheetName, 40, 9, null),
						new ExpectedCellValue(sheetName, 41, 9, 831.5),
						new ExpectedCellValue(sheetName, 42, 9, 831.5),
						new ExpectedCellValue(sheetName, 43, 9, 831.5),
						new ExpectedCellValue(sheetName, 44, 9, 415.75),
						new ExpectedCellValue(sheetName, 45, 9, 2d),
						new ExpectedCellValue(sheetName, 46, 9, 831.5),
						new ExpectedCellValue(sheetName, 47, 9, null),
						new ExpectedCellValue(sheetName, 48, 9, null),
						new ExpectedCellValue(sheetName, 49, 9, null),
						new ExpectedCellValue(sheetName, 50, 9, null),
						new ExpectedCellValue(sheetName, 51, 9, null),
						new ExpectedCellValue(sheetName, 52, 9, null),
						new ExpectedCellValue(sheetName, 53, 9, null),
						new ExpectedCellValue(sheetName, 54, 9, null),
						new ExpectedCellValue(sheetName, 55, 9, null),
						new ExpectedCellValue(sheetName, 56, 9, null),
						new ExpectedCellValue(sheetName, 57, 9, null),
						new ExpectedCellValue(sheetName, 58, 9, null),
						new ExpectedCellValue(sheetName, 59, 9, null),
						new ExpectedCellValue(sheetName, 60, 9, null),
						new ExpectedCellValue(sheetName, 61, 9, null),
						new ExpectedCellValue(sheetName, 62, 9, null),
						new ExpectedCellValue(sheetName, 63, 9, 415.75),
						new ExpectedCellValue(sheetName, 64, 9, 2d),
						new ExpectedCellValue(sheetName, 65, 9, 831.5),
						new ExpectedCellValue(sheetName, 66, 9, 415.75),
						new ExpectedCellValue(sheetName, 67, 9, 2d),
						new ExpectedCellValue(sheetName, 68, 9, 831.5),
						new ExpectedCellValue(sheetName, 69, 9, null),
						new ExpectedCellValue(sheetName, 70, 9, null),
						new ExpectedCellValue(sheetName, 71, 9, null),
						new ExpectedCellValue(sheetName, 72, 9, null),
						new ExpectedCellValue(sheetName, 73, 9, 415.75),
						new ExpectedCellValue(sheetName, 74, 9, 415.75),
						new ExpectedCellValue(sheetName, 75, 9, 415.75),
						new ExpectedCellValue(sheetName, 76, 9, null),
						new ExpectedCellValue(sheetName, 77, 9, 2d),
						new ExpectedCellValue(sheetName, 78, 9, 2d),
						new ExpectedCellValue(sheetName, 79, 9, 2d),
						new ExpectedCellValue(sheetName, 80, 9, null),
						new ExpectedCellValue(sheetName, 81, 9, 831.5),
						new ExpectedCellValue(sheetName, 82, 9, 831.5),
						new ExpectedCellValue(sheetName, 83, 9, 831.5),
						new ExpectedCellValue(sheetName, 84, 9, 415.75),
						new ExpectedCellValue(sheetName, 85, 9, 2d),
						new ExpectedCellValue(sheetName, 86, 9, 831.5),
						new ExpectedCellValue(sheetName, 87, 9, null),
						new ExpectedCellValue(sheetName, 88, 9, null),
						new ExpectedCellValue(sheetName, 89, 9, null),
						new ExpectedCellValue(sheetName, 90, 9, null),
						new ExpectedCellValue(sheetName, 91, 9, null),
						new ExpectedCellValue(sheetName, 92, 9, null),
						new ExpectedCellValue(sheetName, 93, 9, null),
						new ExpectedCellValue(sheetName, 94, 9, null),
						new ExpectedCellValue(sheetName, 95, 9, null),
						new ExpectedCellValue(sheetName, 96, 9, null),
						new ExpectedCellValue(sheetName, 97, 9, null),
						new ExpectedCellValue(sheetName, 98, 9, null),
						new ExpectedCellValue(sheetName, 99, 9, null),
						new ExpectedCellValue(sheetName, 100, 9, null),
						new ExpectedCellValue(sheetName, 101, 9, null),
						new ExpectedCellValue(sheetName, 102, 9, null),
						new ExpectedCellValue(sheetName, 103, 9, 415.75),
						new ExpectedCellValue(sheetName, 104, 9, 2d),
						new ExpectedCellValue(sheetName, 105, 9, 831.5),
						new ExpectedCellValue(sheetName, 106, 9, null),
						new ExpectedCellValue(sheetName, 107, 9, null),
						new ExpectedCellValue(sheetName, 108, 9, null),
						new ExpectedCellValue(sheetName, 109, 9, null),
						new ExpectedCellValue(sheetName, 110, 9, null),
						new ExpectedCellValue(sheetName, 111, 9, null),
						new ExpectedCellValue(sheetName, 112, 9, null),
						new ExpectedCellValue(sheetName, 113, 9, null),
						new ExpectedCellValue(sheetName, 114, 9, null),
						new ExpectedCellValue(sheetName, 115, 9, null),
						new ExpectedCellValue(sheetName, 116, 9, null),
						new ExpectedCellValue(sheetName, 117, 9, null),
						new ExpectedCellValue(sheetName, 118, 9, null),
						new ExpectedCellValue(sheetName, 119, 9, null),
						new ExpectedCellValue(sheetName, 120, 9, null),
						new ExpectedCellValue(sheetName, 121, 9, null),
						new ExpectedCellValue(sheetName, 122, 9, null),
						new ExpectedCellValue(sheetName, 123, 9, null),
						new ExpectedCellValue(sheetName, 124, 9, null),
						new ExpectedCellValue(sheetName, 125, 9, null),
						new ExpectedCellValue(sheetName, 126, 9, null),
						new ExpectedCellValue(sheetName, 127, 9, null),
						new ExpectedCellValue(sheetName, 128, 9, null),
						new ExpectedCellValue(sheetName, 129, 9, null),
						new ExpectedCellValue(sheetName, 130, 9, null),
						new ExpectedCellValue(sheetName, 131, 9, null),
						new ExpectedCellValue(sheetName, 132, 9, null),
						new ExpectedCellValue(sheetName, 133, 9, null),
						new ExpectedCellValue(sheetName, 134, 9, null),
						new ExpectedCellValue(sheetName, 135, 9, null),
						new ExpectedCellValue(sheetName, 136, 9, null),
						new ExpectedCellValue(sheetName, 137, 9, null),
						new ExpectedCellValue(sheetName, 138, 9, null),
						new ExpectedCellValue(sheetName, 139, 9, null),
						new ExpectedCellValue(sheetName, 140, 9, null),
						new ExpectedCellValue(sheetName, 141, 9, null),
						new ExpectedCellValue(sheetName, 142, 9, null),
						new ExpectedCellValue(sheetName, 143, 9, null),
						new ExpectedCellValue(sheetName, 144, 9, null),
						new ExpectedCellValue(sheetName, 145, 9, null),
						new ExpectedCellValue(sheetName, 146, 9, 415.75),
						new ExpectedCellValue(sheetName, 147, 9, 2d),
						new ExpectedCellValue(sheetName, 148, 9, 831.5),
						new ExpectedCellValue(sheetName, 149, 9, 831.5),
						new ExpectedCellValue(sheetName, 150, 9, 4d),
						new ExpectedCellValue(sheetName, 151, 9, 1663d),
						new ExpectedCellValue(sheetName, 2, 10, null),
						new ExpectedCellValue(sheetName, 3, 10, "Tent"),
						new ExpectedCellValue(sheetName, 4, 10, "May"),
						new ExpectedCellValue(sheetName, 5, 10, null),
						new ExpectedCellValue(sheetName, 6, 10, null),
						new ExpectedCellValue(sheetName, 7, 10, null),
						new ExpectedCellValue(sheetName, 8, 10, null),
						new ExpectedCellValue(sheetName, 9, 10, null),
						new ExpectedCellValue(sheetName, 10, 10, null),
						new ExpectedCellValue(sheetName, 11, 10, null),
						new ExpectedCellValue(sheetName, 12, 10, null),
						new ExpectedCellValue(sheetName, 13, 10, null),
						new ExpectedCellValue(sheetName, 14, 10, null),
						new ExpectedCellValue(sheetName, 15, 10, null),
						new ExpectedCellValue(sheetName, 16, 10, null),
						new ExpectedCellValue(sheetName, 17, 10, null),
						new ExpectedCellValue(sheetName, 18, 10, null),
						new ExpectedCellValue(sheetName, 19, 10, null),
						new ExpectedCellValue(sheetName, 20, 10, null),
						new ExpectedCellValue(sheetName, 21, 10, null),
						new ExpectedCellValue(sheetName, 22, 10, null),
						new ExpectedCellValue(sheetName, 23, 10, null),
						new ExpectedCellValue(sheetName, 24, 10, null),
						new ExpectedCellValue(sheetName, 25, 10, null),
						new ExpectedCellValue(sheetName, 26, 10, null),
						new ExpectedCellValue(sheetName, 27, 10, null),
						new ExpectedCellValue(sheetName, 28, 10, null),
						new ExpectedCellValue(sheetName, 29, 10, null),
						new ExpectedCellValue(sheetName, 30, 10, null),
						new ExpectedCellValue(sheetName, 31, 10, null),
						new ExpectedCellValue(sheetName, 32, 10, null),
						new ExpectedCellValue(sheetName, 33, 10, null),
						new ExpectedCellValue(sheetName, 34, 10, null),
						new ExpectedCellValue(sheetName, 35, 10, null),
						new ExpectedCellValue(sheetName, 36, 10, null),
						new ExpectedCellValue(sheetName, 37, 10, null),
						new ExpectedCellValue(sheetName, 38, 10, null),
						new ExpectedCellValue(sheetName, 39, 10, null),
						new ExpectedCellValue(sheetName, 40, 10, null),
						new ExpectedCellValue(sheetName, 41, 10, null),
						new ExpectedCellValue(sheetName, 42, 10, null),
						new ExpectedCellValue(sheetName, 43, 10, null),
						new ExpectedCellValue(sheetName, 44, 10, null),
						new ExpectedCellValue(sheetName, 45, 10, null),
						new ExpectedCellValue(sheetName, 46, 10, null),
						new ExpectedCellValue(sheetName, 47, 10, null),
						new ExpectedCellValue(sheetName, 48, 10, null),
						new ExpectedCellValue(sheetName, 49, 10, null),
						new ExpectedCellValue(sheetName, 50, 10, null),
						new ExpectedCellValue(sheetName, 51, 10, null),
						new ExpectedCellValue(sheetName, 52, 10, null),
						new ExpectedCellValue(sheetName, 53, 10, null),
						new ExpectedCellValue(sheetName, 54, 10, null),
						new ExpectedCellValue(sheetName, 55, 10, null),
						new ExpectedCellValue(sheetName, 56, 10, null),
						new ExpectedCellValue(sheetName, 57, 10, null),
						new ExpectedCellValue(sheetName, 58, 10, null),
						new ExpectedCellValue(sheetName, 59, 10, null),
						new ExpectedCellValue(sheetName, 60, 10, null),
						new ExpectedCellValue(sheetName, 61, 10, null),
						new ExpectedCellValue(sheetName, 62, 10, null),
						new ExpectedCellValue(sheetName, 63, 10, null),
						new ExpectedCellValue(sheetName, 64, 10, null),
						new ExpectedCellValue(sheetName, 65, 10, null),
						new ExpectedCellValue(sheetName, 66, 10, null),
						new ExpectedCellValue(sheetName, 67, 10, null),
						new ExpectedCellValue(sheetName, 68, 10, null),
						new ExpectedCellValue(sheetName, 69, 10, null),
						new ExpectedCellValue(sheetName, 70, 10, null),
						new ExpectedCellValue(sheetName, 71, 10, null),
						new ExpectedCellValue(sheetName, 72, 10, null),
						new ExpectedCellValue(sheetName, 73, 10, null),
						new ExpectedCellValue(sheetName, 74, 10, null),
						new ExpectedCellValue(sheetName, 75, 10, null),
						new ExpectedCellValue(sheetName, 76, 10, null),
						new ExpectedCellValue(sheetName, 77, 10, null),
						new ExpectedCellValue(sheetName, 78, 10, null),
						new ExpectedCellValue(sheetName, 79, 10, null),
						new ExpectedCellValue(sheetName, 80, 10, null),
						new ExpectedCellValue(sheetName, 81, 10, null),
						new ExpectedCellValue(sheetName, 82, 10, null),
						new ExpectedCellValue(sheetName, 83, 10, null),
						new ExpectedCellValue(sheetName, 84, 10, null),
						new ExpectedCellValue(sheetName, 85, 10, null),
						new ExpectedCellValue(sheetName, 86, 10, null),
						new ExpectedCellValue(sheetName, 87, 10, null),
						new ExpectedCellValue(sheetName, 88, 10, null),
						new ExpectedCellValue(sheetName, 89, 10, null),
						new ExpectedCellValue(sheetName, 90, 10, null),
						new ExpectedCellValue(sheetName, 91, 10, null),
						new ExpectedCellValue(sheetName, 92, 10, null),
						new ExpectedCellValue(sheetName, 93, 10, null),
						new ExpectedCellValue(sheetName, 94, 10, null),
						new ExpectedCellValue(sheetName, 95, 10, null),
						new ExpectedCellValue(sheetName, 96, 10, null),
						new ExpectedCellValue(sheetName, 97, 10, null),
						new ExpectedCellValue(sheetName, 98, 10, null),
						new ExpectedCellValue(sheetName, 99, 10, null),
						new ExpectedCellValue(sheetName, 100, 10, null),
						new ExpectedCellValue(sheetName, 101, 10, null),
						new ExpectedCellValue(sheetName, 102, 10, null),
						new ExpectedCellValue(sheetName, 103, 10, null),
						new ExpectedCellValue(sheetName, 104, 10, null),
						new ExpectedCellValue(sheetName, 105, 10, null),
						new ExpectedCellValue(sheetName, 106, 10, null),
						new ExpectedCellValue(sheetName, 107, 10, null),
						new ExpectedCellValue(sheetName, 108, 10, null),
						new ExpectedCellValue(sheetName, 109, 10, 199d),
						new ExpectedCellValue(sheetName, 110, 10, 199d),
						new ExpectedCellValue(sheetName, 111, 10, 199d),
						new ExpectedCellValue(sheetName, 112, 10, null),
						new ExpectedCellValue(sheetName, 113, 10, 6d),
						new ExpectedCellValue(sheetName, 114, 10, 6d),
						new ExpectedCellValue(sheetName, 115, 10, 6d),
						new ExpectedCellValue(sheetName, 116, 10, null),
						new ExpectedCellValue(sheetName, 117, 10, 1194d),
						new ExpectedCellValue(sheetName, 118, 10, 1194d),
						new ExpectedCellValue(sheetName, 119, 10, 1194d),
						new ExpectedCellValue(sheetName, 120, 10, 199d),
						new ExpectedCellValue(sheetName, 121, 10, 6d),
						new ExpectedCellValue(sheetName, 122, 10, 1194d),
						new ExpectedCellValue(sheetName, 123, 10, 199d),
						new ExpectedCellValue(sheetName, 124, 10, 6d),
						new ExpectedCellValue(sheetName, 125, 10, 1194d),
						new ExpectedCellValue(sheetName, 126, 10, null),
						new ExpectedCellValue(sheetName, 127, 10, null),
						new ExpectedCellValue(sheetName, 128, 10, null),
						new ExpectedCellValue(sheetName, 129, 10, null),
						new ExpectedCellValue(sheetName, 130, 10, null),
						new ExpectedCellValue(sheetName, 131, 10, null),
						new ExpectedCellValue(sheetName, 132, 10, null),
						new ExpectedCellValue(sheetName, 133, 10, null),
						new ExpectedCellValue(sheetName, 134, 10, null),
						new ExpectedCellValue(sheetName, 135, 10, null),
						new ExpectedCellValue(sheetName, 136, 10, null),
						new ExpectedCellValue(sheetName, 137, 10, null),
						new ExpectedCellValue(sheetName, 138, 10, null),
						new ExpectedCellValue(sheetName, 139, 10, null),
						new ExpectedCellValue(sheetName, 140, 10, null),
						new ExpectedCellValue(sheetName, 141, 10, null),
						new ExpectedCellValue(sheetName, 142, 10, null),
						new ExpectedCellValue(sheetName, 143, 10, null),
						new ExpectedCellValue(sheetName, 144, 10, null),
						new ExpectedCellValue(sheetName, 145, 10, null),
						new ExpectedCellValue(sheetName, 146, 10, 199d),
						new ExpectedCellValue(sheetName, 147, 10, 6d),
						new ExpectedCellValue(sheetName, 148, 10, 1194),
						new ExpectedCellValue(sheetName, 149, 10, 199d),
						new ExpectedCellValue(sheetName, 150, 10, 6d),
						new ExpectedCellValue(sheetName, 151, 10, 1194d),
						new ExpectedCellValue(sheetName, 2, 11, null),
						new ExpectedCellValue(sheetName, 3, 11, "Tent Total"),
						new ExpectedCellValue(sheetName, 4, 11, null),
						new ExpectedCellValue(sheetName, 5, 11, null),
						new ExpectedCellValue(sheetName, 6, 11, null),
						new ExpectedCellValue(sheetName, 7, 11, null),
						new ExpectedCellValue(sheetName, 8, 11, null),
						new ExpectedCellValue(sheetName, 9, 11, null),
						new ExpectedCellValue(sheetName, 10, 11, null),
						new ExpectedCellValue(sheetName, 11, 11, null),
						new ExpectedCellValue(sheetName, 12, 11, null),
						new ExpectedCellValue(sheetName, 13, 11, null),
						new ExpectedCellValue(sheetName, 14, 11, null),
						new ExpectedCellValue(sheetName, 15, 11, null),
						new ExpectedCellValue(sheetName, 16, 11, null),
						new ExpectedCellValue(sheetName, 17, 11, null),
						new ExpectedCellValue(sheetName, 18, 11, null),
						new ExpectedCellValue(sheetName, 19, 11, null),
						new ExpectedCellValue(sheetName, 20, 11, null),
						new ExpectedCellValue(sheetName, 21, 11, null),
						new ExpectedCellValue(sheetName, 22, 11, null),
						new ExpectedCellValue(sheetName, 23, 11, null),
						new ExpectedCellValue(sheetName, 24, 11, null),
						new ExpectedCellValue(sheetName, 25, 11, null),
						new ExpectedCellValue(sheetName, 26, 11, null),
						new ExpectedCellValue(sheetName, 27, 11, null),
						new ExpectedCellValue(sheetName, 28, 11, null),
						new ExpectedCellValue(sheetName, 29, 11, null),
						new ExpectedCellValue(sheetName, 30, 11, null),
						new ExpectedCellValue(sheetName, 31, 11, null),
						new ExpectedCellValue(sheetName, 32, 11, null),
						new ExpectedCellValue(sheetName, 33, 11, null),
						new ExpectedCellValue(sheetName, 34, 11, null),
						new ExpectedCellValue(sheetName, 35, 11, null),
						new ExpectedCellValue(sheetName, 36, 11, null),
						new ExpectedCellValue(sheetName, 37, 11, null),
						new ExpectedCellValue(sheetName, 38, 11, null),
						new ExpectedCellValue(sheetName, 39, 11, null),
						new ExpectedCellValue(sheetName, 40, 11, null),
						new ExpectedCellValue(sheetName, 41, 11, null),
						new ExpectedCellValue(sheetName, 42, 11, null),
						new ExpectedCellValue(sheetName, 43, 11, null),
						new ExpectedCellValue(sheetName, 44, 11, null),
						new ExpectedCellValue(sheetName, 45, 11, null),
						new ExpectedCellValue(sheetName, 46, 11, null),
						new ExpectedCellValue(sheetName, 47, 11, null),
						new ExpectedCellValue(sheetName, 48, 11, null),
						new ExpectedCellValue(sheetName, 49, 11, null),
						new ExpectedCellValue(sheetName, 50, 11, null),
						new ExpectedCellValue(sheetName, 51, 11, null),
						new ExpectedCellValue(sheetName, 52, 11, null),
						new ExpectedCellValue(sheetName, 53, 11, null),
						new ExpectedCellValue(sheetName, 54, 11, null),
						new ExpectedCellValue(sheetName, 55, 11, null),
						new ExpectedCellValue(sheetName, 56, 11, null),
						new ExpectedCellValue(sheetName, 57, 11, null),
						new ExpectedCellValue(sheetName, 58, 11, null),
						new ExpectedCellValue(sheetName, 59, 11, null),
						new ExpectedCellValue(sheetName, 60, 11, null),
						new ExpectedCellValue(sheetName, 61, 11, null),
						new ExpectedCellValue(sheetName, 62, 11, null),
						new ExpectedCellValue(sheetName, 63, 11, null),
						new ExpectedCellValue(sheetName, 64, 11, null),
						new ExpectedCellValue(sheetName, 65, 11, null),
						new ExpectedCellValue(sheetName, 66, 11, null),
						new ExpectedCellValue(sheetName, 67, 11, null),
						new ExpectedCellValue(sheetName, 68, 11, null),
						new ExpectedCellValue(sheetName, 69, 11, null),
						new ExpectedCellValue(sheetName, 70, 11, null),
						new ExpectedCellValue(sheetName, 71, 11, null),
						new ExpectedCellValue(sheetName, 72, 11, null),
						new ExpectedCellValue(sheetName, 73, 11, null),
						new ExpectedCellValue(sheetName, 74, 11, null),
						new ExpectedCellValue(sheetName, 75, 11, null),
						new ExpectedCellValue(sheetName, 76, 11, null),
						new ExpectedCellValue(sheetName, 77, 11, null),
						new ExpectedCellValue(sheetName, 78, 11, null),
						new ExpectedCellValue(sheetName, 79, 11, null),
						new ExpectedCellValue(sheetName, 80, 11, null),
						new ExpectedCellValue(sheetName, 81, 11, null),
						new ExpectedCellValue(sheetName, 82, 11, null),
						new ExpectedCellValue(sheetName, 83, 11, null),
						new ExpectedCellValue(sheetName, 84, 11, null),
						new ExpectedCellValue(sheetName, 85, 11, null),
						new ExpectedCellValue(sheetName, 86, 11, null),
						new ExpectedCellValue(sheetName, 87, 11, null),
						new ExpectedCellValue(sheetName, 88, 11, null),
						new ExpectedCellValue(sheetName, 89, 11, null),
						new ExpectedCellValue(sheetName, 90, 11, null),
						new ExpectedCellValue(sheetName, 91, 11, null),
						new ExpectedCellValue(sheetName, 92, 11, null),
						new ExpectedCellValue(sheetName, 93, 11, null),
						new ExpectedCellValue(sheetName, 94, 11, null),
						new ExpectedCellValue(sheetName, 95, 11, null),
						new ExpectedCellValue(sheetName, 96, 11, null),
						new ExpectedCellValue(sheetName, 97, 11, null),
						new ExpectedCellValue(sheetName, 98, 11, null),
						new ExpectedCellValue(sheetName, 99, 11, null),
						new ExpectedCellValue(sheetName, 100, 11, null),
						new ExpectedCellValue(sheetName, 101, 11, null),
						new ExpectedCellValue(sheetName, 102, 11, null),
						new ExpectedCellValue(sheetName, 103, 11, null),
						new ExpectedCellValue(sheetName, 104, 11, null),
						new ExpectedCellValue(sheetName, 105, 11, null),
						new ExpectedCellValue(sheetName, 106, 11, null),
						new ExpectedCellValue(sheetName, 107, 11, null),
						new ExpectedCellValue(sheetName, 108, 11, null),
						new ExpectedCellValue(sheetName, 109, 11, 199d),
						new ExpectedCellValue(sheetName, 110, 11, 199d),
						new ExpectedCellValue(sheetName, 111, 11, 199d),
						new ExpectedCellValue(sheetName, 112, 11, null),
						new ExpectedCellValue(sheetName, 113, 11, 6d),
						new ExpectedCellValue(sheetName, 114, 11, 6d),
						new ExpectedCellValue(sheetName, 115, 11, 6d),
						new ExpectedCellValue(sheetName, 116, 11, null),
						new ExpectedCellValue(sheetName, 117, 11, 1194d),
						new ExpectedCellValue(sheetName, 118, 11, 1194d),
						new ExpectedCellValue(sheetName, 119, 11, 1194d),
						new ExpectedCellValue(sheetName, 120, 11, 199d),
						new ExpectedCellValue(sheetName, 121, 11, 6d),
						new ExpectedCellValue(sheetName, 122, 11, 1194d),
						new ExpectedCellValue(sheetName, 123, 11, 199d),
						new ExpectedCellValue(sheetName, 124, 11, 6d),
						new ExpectedCellValue(sheetName, 125, 11, 1194d),
						new ExpectedCellValue(sheetName, 126, 11, null),
						new ExpectedCellValue(sheetName, 127, 11, null),
						new ExpectedCellValue(sheetName, 128, 11, null),
						new ExpectedCellValue(sheetName, 129, 11, null),
						new ExpectedCellValue(sheetName, 130, 11, null),
						new ExpectedCellValue(sheetName, 131, 11, null),
						new ExpectedCellValue(sheetName, 132, 11, null),
						new ExpectedCellValue(sheetName, 133, 11, null),
						new ExpectedCellValue(sheetName, 134, 11, null),
						new ExpectedCellValue(sheetName, 135, 11, null),
						new ExpectedCellValue(sheetName, 136, 11, null),
						new ExpectedCellValue(sheetName, 137, 11, null),
						new ExpectedCellValue(sheetName, 138, 11, null),
						new ExpectedCellValue(sheetName, 139, 11, null),
						new ExpectedCellValue(sheetName, 140, 11, null),
						new ExpectedCellValue(sheetName, 141, 11, null),
						new ExpectedCellValue(sheetName, 142, 11, null),
						new ExpectedCellValue(sheetName, 143, 11, null),
						new ExpectedCellValue(sheetName, 144, 11, null),
						new ExpectedCellValue(sheetName, 145, 11, null),
						new ExpectedCellValue(sheetName, 146, 11, 199d),
						new ExpectedCellValue(sheetName, 147, 11, 6d),
						new ExpectedCellValue(sheetName, 148, 11, 1194),
						new ExpectedCellValue(sheetName, 149, 11, 199d),
						new ExpectedCellValue(sheetName, 150, 11, 6d),
						new ExpectedCellValue(sheetName, 151, 11, 1194d),
						new ExpectedCellValue(sheetName, 2, 12, "Nashville Total"),
						new ExpectedCellValue(sheetName, 3, 12, null),
						new ExpectedCellValue(sheetName, 4, 12, null),
						new ExpectedCellValue(sheetName, 5, 12, null),
						new ExpectedCellValue(sheetName, 6, 12, null),
						new ExpectedCellValue(sheetName, 7, 12, null),
						new ExpectedCellValue(sheetName, 8, 12, null),
						new ExpectedCellValue(sheetName, 9, 12, null),
						new ExpectedCellValue(sheetName, 10, 12, null),
						new ExpectedCellValue(sheetName, 11, 12, null),
						new ExpectedCellValue(sheetName, 12, 12, null),
						new ExpectedCellValue(sheetName, 13, 12, null),
						new ExpectedCellValue(sheetName, 14, 12, null),
						new ExpectedCellValue(sheetName, 15, 12, null),
						new ExpectedCellValue(sheetName, 16, 12, null),
						new ExpectedCellValue(sheetName, 17, 12, null),
						new ExpectedCellValue(sheetName, 18, 12, null),
						new ExpectedCellValue(sheetName, 19, 12, null),
						new ExpectedCellValue(sheetName, 20, 12, null),
						new ExpectedCellValue(sheetName, 21, 12, null),
						new ExpectedCellValue(sheetName, 22, 12, null),
						new ExpectedCellValue(sheetName, 23, 12, null),
						new ExpectedCellValue(sheetName, 24, 12, null),
						new ExpectedCellValue(sheetName, 25, 12, null),
						new ExpectedCellValue(sheetName, 26, 12, null),
						new ExpectedCellValue(sheetName, 27, 12, null),
						new ExpectedCellValue(sheetName, 28, 12, null),
						new ExpectedCellValue(sheetName, 29, 12, null),
						new ExpectedCellValue(sheetName, 30, 12, null),
						new ExpectedCellValue(sheetName, 31, 12, null),
						new ExpectedCellValue(sheetName, 32, 12, null),
						new ExpectedCellValue(sheetName, 33, 12, 415.75),
						new ExpectedCellValue(sheetName, 34, 12, 415.75),
						new ExpectedCellValue(sheetName, 35, 12, 415.75),
						new ExpectedCellValue(sheetName, 36, 12, null),
						new ExpectedCellValue(sheetName, 37, 12, 2d),
						new ExpectedCellValue(sheetName, 38, 12, 2d),
						new ExpectedCellValue(sheetName, 39, 12, 2d),
						new ExpectedCellValue(sheetName, 40, 12, null),
						new ExpectedCellValue(sheetName, 41, 12, 831.5),
						new ExpectedCellValue(sheetName, 42, 12, 831.5),
						new ExpectedCellValue(sheetName, 43, 12, 831.5),
						new ExpectedCellValue(sheetName, 44, 12, 415.75),
						new ExpectedCellValue(sheetName, 45, 12, 2d),
						new ExpectedCellValue(sheetName, 46, 12, 831.5),
						new ExpectedCellValue(sheetName, 47, 12, null),
						new ExpectedCellValue(sheetName, 48, 12, null),
						new ExpectedCellValue(sheetName, 49, 12, null),
						new ExpectedCellValue(sheetName, 50, 12, null),
						new ExpectedCellValue(sheetName, 51, 12, null),
						new ExpectedCellValue(sheetName, 52, 12, null),
						new ExpectedCellValue(sheetName, 53, 12, null),
						new ExpectedCellValue(sheetName, 54, 12, null),
						new ExpectedCellValue(sheetName, 55, 12, null),
						new ExpectedCellValue(sheetName, 56, 12, null),
						new ExpectedCellValue(sheetName, 57, 12, null),
						new ExpectedCellValue(sheetName, 58, 12, null),
						new ExpectedCellValue(sheetName, 59, 12, null),
						new ExpectedCellValue(sheetName, 60, 12, null),
						new ExpectedCellValue(sheetName, 61, 12, null),
						new ExpectedCellValue(sheetName, 62, 12, null),
						new ExpectedCellValue(sheetName, 63, 12, 415.75),
						new ExpectedCellValue(sheetName, 64, 12, 2d),
						new ExpectedCellValue(sheetName, 65, 12, 831.5),
						new ExpectedCellValue(sheetName, 66, 12, 415.75),
						new ExpectedCellValue(sheetName, 67, 12, 2d),
						new ExpectedCellValue(sheetName, 68, 12, 831.5),
						new ExpectedCellValue(sheetName, 69, 12, null),
						new ExpectedCellValue(sheetName, 70, 12, null),
						new ExpectedCellValue(sheetName, 71, 12, null),
						new ExpectedCellValue(sheetName, 72, 12, null),
						new ExpectedCellValue(sheetName, 73, 12, 415.75),
						new ExpectedCellValue(sheetName, 74, 12, 415.75),
						new ExpectedCellValue(sheetName, 75, 12, 415.75),
						new ExpectedCellValue(sheetName, 76, 12, null),
						new ExpectedCellValue(sheetName, 77, 12, 2d),
						new ExpectedCellValue(sheetName, 78, 12, 2d),
						new ExpectedCellValue(sheetName, 79, 12, 2d),
						new ExpectedCellValue(sheetName, 80, 12, null),
						new ExpectedCellValue(sheetName, 81, 12, 831.5),
						new ExpectedCellValue(sheetName, 82, 12, 831.5),
						new ExpectedCellValue(sheetName, 83, 12, 831.5),
						new ExpectedCellValue(sheetName, 84, 12, 415.75),
						new ExpectedCellValue(sheetName, 85, 12, 2d),
						new ExpectedCellValue(sheetName, 86, 12, 831.5),
						new ExpectedCellValue(sheetName, 87, 12, null),
						new ExpectedCellValue(sheetName, 88, 12, null),
						new ExpectedCellValue(sheetName, 89, 12, null),
						new ExpectedCellValue(sheetName, 90, 12, null),
						new ExpectedCellValue(sheetName, 91, 12, null),
						new ExpectedCellValue(sheetName, 92, 12, null),
						new ExpectedCellValue(sheetName, 93, 12, null),
						new ExpectedCellValue(sheetName, 94, 12, null),
						new ExpectedCellValue(sheetName, 95, 12, null),
						new ExpectedCellValue(sheetName, 96, 12, null),
						new ExpectedCellValue(sheetName, 97, 12, null),
						new ExpectedCellValue(sheetName, 98, 12, null),
						new ExpectedCellValue(sheetName, 99, 12, null),
						new ExpectedCellValue(sheetName, 100, 12, null),
						new ExpectedCellValue(sheetName, 101, 12, null),
						new ExpectedCellValue(sheetName, 102, 12, null),
						new ExpectedCellValue(sheetName, 103, 12, 415.75),
						new ExpectedCellValue(sheetName, 104, 12, 2d),
						new ExpectedCellValue(sheetName, 105, 12, 831.5),
						new ExpectedCellValue(sheetName, 106, 12, null),
						new ExpectedCellValue(sheetName, 107, 12, null),
						new ExpectedCellValue(sheetName, 108, 12, null),
						new ExpectedCellValue(sheetName, 109, 12, 199d),
						new ExpectedCellValue(sheetName, 110, 12, 199d),
						new ExpectedCellValue(sheetName, 111, 12, 199d),
						new ExpectedCellValue(sheetName, 112, 12, null),
						new ExpectedCellValue(sheetName, 113, 12, 6d),
						new ExpectedCellValue(sheetName, 114, 12, 6d),
						new ExpectedCellValue(sheetName, 115, 12, 6d),
						new ExpectedCellValue(sheetName, 116, 12, null),
						new ExpectedCellValue(sheetName, 117, 12, 1194d),
						new ExpectedCellValue(sheetName, 118, 12, 1194d),
						new ExpectedCellValue(sheetName, 119, 12, 1194d),
						new ExpectedCellValue(sheetName, 120, 12, 199d),
						new ExpectedCellValue(sheetName, 121, 12, 6d),
						new ExpectedCellValue(sheetName, 122, 12, 1194d),
						new ExpectedCellValue(sheetName, 123, 12, 199d),
						new ExpectedCellValue(sheetName, 124, 12, 6d),
						new ExpectedCellValue(sheetName, 125, 12, 1194d),
						new ExpectedCellValue(sheetName, 126, 12, null),
						new ExpectedCellValue(sheetName, 127, 12, null),
						new ExpectedCellValue(sheetName, 128, 12, null),
						new ExpectedCellValue(sheetName, 129, 12, null),
						new ExpectedCellValue(sheetName, 130, 12, null),
						new ExpectedCellValue(sheetName, 131, 12, null),
						new ExpectedCellValue(sheetName, 132, 12, null),
						new ExpectedCellValue(sheetName, 133, 12, null),
						new ExpectedCellValue(sheetName, 134, 12, null),
						new ExpectedCellValue(sheetName, 135, 12, null),
						new ExpectedCellValue(sheetName, 136, 12, null),
						new ExpectedCellValue(sheetName, 137, 12, null),
						new ExpectedCellValue(sheetName, 138, 12, null),
						new ExpectedCellValue(sheetName, 139, 12, null),
						new ExpectedCellValue(sheetName, 140, 12, null),
						new ExpectedCellValue(sheetName, 141, 12, null),
						new ExpectedCellValue(sheetName, 142, 12, null),
						new ExpectedCellValue(sheetName, 143, 12, null),
						new ExpectedCellValue(sheetName, 144, 12, null),
						new ExpectedCellValue(sheetName, 145, 12, null),
						new ExpectedCellValue(sheetName, 146, 12, 614.75),
						new ExpectedCellValue(sheetName, 147, 12, 8d),
						new ExpectedCellValue(sheetName, 148, 12, 2025.5),
						new ExpectedCellValue(sheetName, 149, 12, 1030.5),
						new ExpectedCellValue(sheetName, 150, 12, 10d),
						new ExpectedCellValue(sheetName, 151, 12, 2857d),
						new ExpectedCellValue(sheetName, 2, 13, "San Francisco"),
						new ExpectedCellValue(sheetName, 3, 13, "Car Rack"),
						new ExpectedCellValue(sheetName, 4, 13, "Nov"),
						new ExpectedCellValue(sheetName, 5, 13, null),
						new ExpectedCellValue(sheetName, 6, 13, null),
						new ExpectedCellValue(sheetName, 7, 13, null),
						new ExpectedCellValue(sheetName, 8, 13, null),
						new ExpectedCellValue(sheetName, 9, 13, null),
						new ExpectedCellValue(sheetName, 10, 13, null),
						new ExpectedCellValue(sheetName, 11, 13, null),
						new ExpectedCellValue(sheetName, 12, 13, null),
						new ExpectedCellValue(sheetName, 13, 13, null),
						new ExpectedCellValue(sheetName, 14, 13, null),
						new ExpectedCellValue(sheetName, 15, 13, null),
						new ExpectedCellValue(sheetName, 16, 13, null),
						new ExpectedCellValue(sheetName, 17, 13, null),
						new ExpectedCellValue(sheetName, 18, 13, null),
						new ExpectedCellValue(sheetName, 19, 13, null),
						new ExpectedCellValue(sheetName, 20, 13, null),
						new ExpectedCellValue(sheetName, 21, 13, null),
						new ExpectedCellValue(sheetName, 22, 13, null),
						new ExpectedCellValue(sheetName, 23, 13, null),
						new ExpectedCellValue(sheetName, 24, 13, null),
						new ExpectedCellValue(sheetName, 25, 13, null),
						new ExpectedCellValue(sheetName, 26, 13, null),
						new ExpectedCellValue(sheetName, 27, 13, null),
						new ExpectedCellValue(sheetName, 28, 13, null),
						new ExpectedCellValue(sheetName, 29, 13, null),
						new ExpectedCellValue(sheetName, 30, 13, null),
						new ExpectedCellValue(sheetName, 31, 13, null),
						new ExpectedCellValue(sheetName, 32, 13, null),
						new ExpectedCellValue(sheetName, 33, 13, null),
						new ExpectedCellValue(sheetName, 34, 13, null),
						new ExpectedCellValue(sheetName, 35, 13, null),
						new ExpectedCellValue(sheetName, 36, 13, null),
						new ExpectedCellValue(sheetName, 37, 13, null),
						new ExpectedCellValue(sheetName, 38, 13, null),
						new ExpectedCellValue(sheetName, 39, 13, null),
						new ExpectedCellValue(sheetName, 40, 13, null),
						new ExpectedCellValue(sheetName, 41, 13, null),
						new ExpectedCellValue(sheetName, 42, 13, null),
						new ExpectedCellValue(sheetName, 43, 13, null),
						new ExpectedCellValue(sheetName, 44, 13, null),
						new ExpectedCellValue(sheetName, 45, 13, null),
						new ExpectedCellValue(sheetName, 46, 13, null),
						new ExpectedCellValue(sheetName, 47, 13, null),
						new ExpectedCellValue(sheetName, 48, 13, null),
						new ExpectedCellValue(sheetName, 49, 13, null),
						new ExpectedCellValue(sheetName, 50, 13, null),
						new ExpectedCellValue(sheetName, 51, 13, null),
						new ExpectedCellValue(sheetName, 52, 13, null),
						new ExpectedCellValue(sheetName, 53, 13, null),
						new ExpectedCellValue(sheetName, 54, 13, null),
						new ExpectedCellValue(sheetName, 55, 13, null),
						new ExpectedCellValue(sheetName, 56, 13, null),
						new ExpectedCellValue(sheetName, 57, 13, null),
						new ExpectedCellValue(sheetName, 58, 13, null),
						new ExpectedCellValue(sheetName, 59, 13, null),
						new ExpectedCellValue(sheetName, 60, 13, null),
						new ExpectedCellValue(sheetName, 61, 13, null),
						new ExpectedCellValue(sheetName, 62, 13, null),
						new ExpectedCellValue(sheetName, 63, 13, null),
						new ExpectedCellValue(sheetName, 64, 13, null),
						new ExpectedCellValue(sheetName, 65, 13, null),
						new ExpectedCellValue(sheetName, 66, 13, null),
						new ExpectedCellValue(sheetName, 67, 13, null),
						new ExpectedCellValue(sheetName, 68, 13, null),
						new ExpectedCellValue(sheetName, 69, 13, null),
						new ExpectedCellValue(sheetName, 70, 13, null),
						new ExpectedCellValue(sheetName, 71, 13, null),
						new ExpectedCellValue(sheetName, 72, 13, null),
						new ExpectedCellValue(sheetName, 73, 13, null),
						new ExpectedCellValue(sheetName, 74, 13, null),
						new ExpectedCellValue(sheetName, 75, 13, null),
						new ExpectedCellValue(sheetName, 76, 13, null),
						new ExpectedCellValue(sheetName, 77, 13, null),
						new ExpectedCellValue(sheetName, 78, 13, null),
						new ExpectedCellValue(sheetName, 79, 13, null),
						new ExpectedCellValue(sheetName, 80, 13, null),
						new ExpectedCellValue(sheetName, 81, 13, null),
						new ExpectedCellValue(sheetName, 82, 13, null),
						new ExpectedCellValue(sheetName, 83, 13, null),
						new ExpectedCellValue(sheetName, 84, 13, null),
						new ExpectedCellValue(sheetName, 85, 13, null),
						new ExpectedCellValue(sheetName, 86, 13, null),
						new ExpectedCellValue(sheetName, 87, 13, null),
						new ExpectedCellValue(sheetName, 88, 13, null),
						new ExpectedCellValue(sheetName, 89, 13, null),
						new ExpectedCellValue(sheetName, 90, 13, null),
						new ExpectedCellValue(sheetName, 91, 13, null),
						new ExpectedCellValue(sheetName, 92, 13, null),
						new ExpectedCellValue(sheetName, 93, 13, null),
						new ExpectedCellValue(sheetName, 94, 13, null),
						new ExpectedCellValue(sheetName, 95, 13, null),
						new ExpectedCellValue(sheetName, 96, 13, null),
						new ExpectedCellValue(sheetName, 97, 13, null),
						new ExpectedCellValue(sheetName, 98, 13, null),
						new ExpectedCellValue(sheetName, 99, 13, null),
						new ExpectedCellValue(sheetName, 100, 13, null),
						new ExpectedCellValue(sheetName, 101, 13, null),
						new ExpectedCellValue(sheetName, 102, 13, null),
						new ExpectedCellValue(sheetName, 103, 13, null),
						new ExpectedCellValue(sheetName, 104, 13, null),
						new ExpectedCellValue(sheetName, 105, 13, null),
						new ExpectedCellValue(sheetName, 106, 13, null),
						new ExpectedCellValue(sheetName, 107, 13, null),
						new ExpectedCellValue(sheetName, 108, 13, null),
						new ExpectedCellValue(sheetName, 109, 13, null),
						new ExpectedCellValue(sheetName, 110, 13, null),
						new ExpectedCellValue(sheetName, 111, 13, null),
						new ExpectedCellValue(sheetName, 112, 13, null),
						new ExpectedCellValue(sheetName, 113, 13, null),
						new ExpectedCellValue(sheetName, 114, 13, null),
						new ExpectedCellValue(sheetName, 115, 13, null),
						new ExpectedCellValue(sheetName, 116, 13, null),
						new ExpectedCellValue(sheetName, 117, 13, null),
						new ExpectedCellValue(sheetName, 118, 13, null),
						new ExpectedCellValue(sheetName, 119, 13, null),
						new ExpectedCellValue(sheetName, 120, 13, null),
						new ExpectedCellValue(sheetName, 121, 13, null),
						new ExpectedCellValue(sheetName, 122, 13, null),
						new ExpectedCellValue(sheetName, 123, 13, null),
						new ExpectedCellValue(sheetName, 124, 13, null),
						new ExpectedCellValue(sheetName, 125, 13, null),
						new ExpectedCellValue(sheetName, 126, 13, null),
						new ExpectedCellValue(sheetName, 127, 13, null),
						new ExpectedCellValue(sheetName, 128, 13, null),
						new ExpectedCellValue(sheetName, 129, 13, 415.75),
						new ExpectedCellValue(sheetName, 130, 13, 415.75),
						new ExpectedCellValue(sheetName, 131, 13, 415.75),
						new ExpectedCellValue(sheetName, 132, 13, null),
						new ExpectedCellValue(sheetName, 133, 13, 1d),
						new ExpectedCellValue(sheetName, 134, 13, 1d),
						new ExpectedCellValue(sheetName, 135, 13, 1d),
						new ExpectedCellValue(sheetName, 136, 13, null),
						new ExpectedCellValue(sheetName, 137, 13, 415.75),
						new ExpectedCellValue(sheetName, 138, 13, 415.75),
						new ExpectedCellValue(sheetName, 139, 13, 415.75),
						new ExpectedCellValue(sheetName, 140, 13, 415.75),
						new ExpectedCellValue(sheetName, 141, 13, 1d),
						new ExpectedCellValue(sheetName, 142, 13, 415.75),
						new ExpectedCellValue(sheetName, 143, 13, 415.75),
						new ExpectedCellValue(sheetName, 144, 13, 1d),
						new ExpectedCellValue(sheetName, 145, 13, 415.75),
						new ExpectedCellValue(sheetName, 146, 13, 415.75),
						new ExpectedCellValue(sheetName, 147, 13, 1d),
						new ExpectedCellValue(sheetName, 148, 13, 415.75),
						new ExpectedCellValue(sheetName, 149, 13, 415.75),
						new ExpectedCellValue(sheetName, 150, 13, 1d),
						new ExpectedCellValue(sheetName, 151, 13, 415.75),
						new ExpectedCellValue(sheetName, 2, 14, null),
						new ExpectedCellValue(sheetName, 3, 14, "Car Rack Total"),
						new ExpectedCellValue(sheetName, 4, 14, null),
						new ExpectedCellValue(sheetName, 5, 14, null),
						new ExpectedCellValue(sheetName, 6, 14, null),
						new ExpectedCellValue(sheetName, 7, 14, null),
						new ExpectedCellValue(sheetName, 8, 14, null),
						new ExpectedCellValue(sheetName, 9, 14, null),
						new ExpectedCellValue(sheetName, 10, 14, null),
						new ExpectedCellValue(sheetName, 11, 14, null),
						new ExpectedCellValue(sheetName, 12, 14, null),
						new ExpectedCellValue(sheetName, 13, 14, null),
						new ExpectedCellValue(sheetName, 14, 14, null),
						new ExpectedCellValue(sheetName, 15, 14, null),
						new ExpectedCellValue(sheetName, 16, 14, null),
						new ExpectedCellValue(sheetName, 17, 14, null),
						new ExpectedCellValue(sheetName, 18, 14, null),
						new ExpectedCellValue(sheetName, 19, 14, null),
						new ExpectedCellValue(sheetName, 20, 14, null),
						new ExpectedCellValue(sheetName, 21, 14, null),
						new ExpectedCellValue(sheetName, 22, 14, null),
						new ExpectedCellValue(sheetName, 23, 14, null),
						new ExpectedCellValue(sheetName, 24, 14, null),
						new ExpectedCellValue(sheetName, 25, 14, null),
						new ExpectedCellValue(sheetName, 26, 14, null),
						new ExpectedCellValue(sheetName, 27, 14, null),
						new ExpectedCellValue(sheetName, 28, 14, null),
						new ExpectedCellValue(sheetName, 29, 14, null),
						new ExpectedCellValue(sheetName, 30, 14, null),
						new ExpectedCellValue(sheetName, 31, 14, null),
						new ExpectedCellValue(sheetName, 32, 14, null),
						new ExpectedCellValue(sheetName, 33, 14, null),
						new ExpectedCellValue(sheetName, 34, 14, null),
						new ExpectedCellValue(sheetName, 35, 14, null),
						new ExpectedCellValue(sheetName, 36, 14, null),
						new ExpectedCellValue(sheetName, 37, 14, null),
						new ExpectedCellValue(sheetName, 38, 14, null),
						new ExpectedCellValue(sheetName, 39, 14, null),
						new ExpectedCellValue(sheetName, 40, 14, null),
						new ExpectedCellValue(sheetName, 41, 14, null),
						new ExpectedCellValue(sheetName, 42, 14, null),
						new ExpectedCellValue(sheetName, 43, 14, null),
						new ExpectedCellValue(sheetName, 44, 14, null),
						new ExpectedCellValue(sheetName, 45, 14, null),
						new ExpectedCellValue(sheetName, 46, 14, null),
						new ExpectedCellValue(sheetName, 47, 14, null),
						new ExpectedCellValue(sheetName, 48, 14, null),
						new ExpectedCellValue(sheetName, 49, 14, null),
						new ExpectedCellValue(sheetName, 50, 14, null),
						new ExpectedCellValue(sheetName, 51, 14, null),
						new ExpectedCellValue(sheetName, 52, 14, null),
						new ExpectedCellValue(sheetName, 53, 14, null),
						new ExpectedCellValue(sheetName, 54, 14, null),
						new ExpectedCellValue(sheetName, 55, 14, null),
						new ExpectedCellValue(sheetName, 56, 14, null),
						new ExpectedCellValue(sheetName, 57, 14, null),
						new ExpectedCellValue(sheetName, 58, 14, null),
						new ExpectedCellValue(sheetName, 59, 14, null),
						new ExpectedCellValue(sheetName, 60, 14, null),
						new ExpectedCellValue(sheetName, 61, 14, null),
						new ExpectedCellValue(sheetName, 62, 14, null),
						new ExpectedCellValue(sheetName, 63, 14, null),
						new ExpectedCellValue(sheetName, 64, 14, null),
						new ExpectedCellValue(sheetName, 65, 14, null),
						new ExpectedCellValue(sheetName, 66, 14, null),
						new ExpectedCellValue(sheetName, 67, 14, null),
						new ExpectedCellValue(sheetName, 68, 14, null),
						new ExpectedCellValue(sheetName, 69, 14, null),
						new ExpectedCellValue(sheetName, 70, 14, null),
						new ExpectedCellValue(sheetName, 71, 14, null),
						new ExpectedCellValue(sheetName, 72, 14, null),
						new ExpectedCellValue(sheetName, 73, 14, null),
						new ExpectedCellValue(sheetName, 74, 14, null),
						new ExpectedCellValue(sheetName, 75, 14, null),
						new ExpectedCellValue(sheetName, 76, 14, null),
						new ExpectedCellValue(sheetName, 77, 14, null),
						new ExpectedCellValue(sheetName, 78, 14, null),
						new ExpectedCellValue(sheetName, 79, 14, null),
						new ExpectedCellValue(sheetName, 80, 14, null),
						new ExpectedCellValue(sheetName, 81, 14, null),
						new ExpectedCellValue(sheetName, 82, 14, null),
						new ExpectedCellValue(sheetName, 83, 14, null),
						new ExpectedCellValue(sheetName, 84, 14, null),
						new ExpectedCellValue(sheetName, 85, 14, null),
						new ExpectedCellValue(sheetName, 86, 14, null),
						new ExpectedCellValue(sheetName, 87, 14, null),
						new ExpectedCellValue(sheetName, 88, 14, null),
						new ExpectedCellValue(sheetName, 89, 14, null),
						new ExpectedCellValue(sheetName, 90, 14, null),
						new ExpectedCellValue(sheetName, 91, 14, null),
						new ExpectedCellValue(sheetName, 92, 14, null),
						new ExpectedCellValue(sheetName, 93, 14, null),
						new ExpectedCellValue(sheetName, 94, 14, null),
						new ExpectedCellValue(sheetName, 95, 14, null),
						new ExpectedCellValue(sheetName, 96, 14, null),
						new ExpectedCellValue(sheetName, 97, 14, null),
						new ExpectedCellValue(sheetName, 98, 14, null),
						new ExpectedCellValue(sheetName, 99, 14, null),
						new ExpectedCellValue(sheetName, 100, 14, null),
						new ExpectedCellValue(sheetName, 101, 14, null),
						new ExpectedCellValue(sheetName, 102, 14, null),
						new ExpectedCellValue(sheetName, 103, 14, null),
						new ExpectedCellValue(sheetName, 104, 14, null),
						new ExpectedCellValue(sheetName, 105, 14, null),
						new ExpectedCellValue(sheetName, 106, 14, null),
						new ExpectedCellValue(sheetName, 107, 14, null),
						new ExpectedCellValue(sheetName, 108, 14, null),
						new ExpectedCellValue(sheetName, 109, 14, null),
						new ExpectedCellValue(sheetName, 110, 14, null),
						new ExpectedCellValue(sheetName, 111, 14, null),
						new ExpectedCellValue(sheetName, 112, 14, null),
						new ExpectedCellValue(sheetName, 113, 14, null),
						new ExpectedCellValue(sheetName, 114, 14, null),
						new ExpectedCellValue(sheetName, 115, 14, null),
						new ExpectedCellValue(sheetName, 116, 14, null),
						new ExpectedCellValue(sheetName, 117, 14, null),
						new ExpectedCellValue(sheetName, 118, 14, null),
						new ExpectedCellValue(sheetName, 119, 14, null),
						new ExpectedCellValue(sheetName, 120, 14, null),
						new ExpectedCellValue(sheetName, 121, 14, null),
						new ExpectedCellValue(sheetName, 122, 14, null),
						new ExpectedCellValue(sheetName, 123, 14, null),
						new ExpectedCellValue(sheetName, 124, 14, null),
						new ExpectedCellValue(sheetName, 125, 14, null),
						new ExpectedCellValue(sheetName, 126, 14, null),
						new ExpectedCellValue(sheetName, 127, 14, null),
						new ExpectedCellValue(sheetName, 128, 14, null),
						new ExpectedCellValue(sheetName, 129, 14, 415.75),
						new ExpectedCellValue(sheetName, 130, 14, 415.75),
						new ExpectedCellValue(sheetName, 131, 14, 415.75),
						new ExpectedCellValue(sheetName, 132, 14, null),
						new ExpectedCellValue(sheetName, 133, 14, 1d),
						new ExpectedCellValue(sheetName, 134, 14, 1d),
						new ExpectedCellValue(sheetName, 135, 14, 1d),
						new ExpectedCellValue(sheetName, 136, 14, null),
						new ExpectedCellValue(sheetName, 137, 14, 415.75),
						new ExpectedCellValue(sheetName, 138, 14, 415.75),
						new ExpectedCellValue(sheetName, 139, 14, 415.75),
						new ExpectedCellValue(sheetName, 140, 14, 415.75),
						new ExpectedCellValue(sheetName, 141, 14, 1d),
						new ExpectedCellValue(sheetName, 142, 14, 415.75),
						new ExpectedCellValue(sheetName, 143, 14, 415.75),
						new ExpectedCellValue(sheetName, 144, 14, 1d),
						new ExpectedCellValue(sheetName, 145, 14, 415.75),
						new ExpectedCellValue(sheetName, 146, 14, 415.75),
						new ExpectedCellValue(sheetName, 147, 14, 1d),
						new ExpectedCellValue(sheetName, 148, 14, 415.75),
						new ExpectedCellValue(sheetName, 149, 14, 415.75),
						new ExpectedCellValue(sheetName, 150, 14, 1d),
						new ExpectedCellValue(sheetName, 151, 14, 415.75),
						new ExpectedCellValue(sheetName, 2, 15, null),
						new ExpectedCellValue(sheetName, 3, 15, "Sleeping Bag"),
						new ExpectedCellValue(sheetName, 4, 15, "Dec"),
						new ExpectedCellValue(sheetName, 5, 15, null),
						new ExpectedCellValue(sheetName, 6, 15, null),
						new ExpectedCellValue(sheetName, 7, 15, null),
						new ExpectedCellValue(sheetName, 8, 15, null),
						new ExpectedCellValue(sheetName, 9, 15, null),
						new ExpectedCellValue(sheetName, 10, 15, null),
						new ExpectedCellValue(sheetName, 11, 15, null),
						new ExpectedCellValue(sheetName, 12, 15, null),
						new ExpectedCellValue(sheetName, 13, 15, null),
						new ExpectedCellValue(sheetName, 14, 15, null),
						new ExpectedCellValue(sheetName, 15, 15, null),
						new ExpectedCellValue(sheetName, 16, 15, null),
						new ExpectedCellValue(sheetName, 17, 15, null),
						new ExpectedCellValue(sheetName, 18, 15, null),
						new ExpectedCellValue(sheetName, 19, 15, null),
						new ExpectedCellValue(sheetName, 20, 15, null),
						new ExpectedCellValue(sheetName, 21, 15, null),
						new ExpectedCellValue(sheetName, 22, 15, null),
						new ExpectedCellValue(sheetName, 23, 15, null),
						new ExpectedCellValue(sheetName, 24, 15, null),
						new ExpectedCellValue(sheetName, 25, 15, null),
						new ExpectedCellValue(sheetName, 26, 15, null),
						new ExpectedCellValue(sheetName, 27, 15, null),
						new ExpectedCellValue(sheetName, 28, 15, null),
						new ExpectedCellValue(sheetName, 29, 15, null),
						new ExpectedCellValue(sheetName, 30, 15, null),
						new ExpectedCellValue(sheetName, 31, 15, null),
						new ExpectedCellValue(sheetName, 32, 15, null),
						new ExpectedCellValue(sheetName, 33, 15, null),
						new ExpectedCellValue(sheetName, 34, 15, null),
						new ExpectedCellValue(sheetName, 35, 15, null),
						new ExpectedCellValue(sheetName, 36, 15, null),
						new ExpectedCellValue(sheetName, 37, 15, null),
						new ExpectedCellValue(sheetName, 38, 15, null),
						new ExpectedCellValue(sheetName, 39, 15, null),
						new ExpectedCellValue(sheetName, 40, 15, null),
						new ExpectedCellValue(sheetName, 41, 15, null),
						new ExpectedCellValue(sheetName, 42, 15, null),
						new ExpectedCellValue(sheetName, 43, 15, null),
						new ExpectedCellValue(sheetName, 44, 15, null),
						new ExpectedCellValue(sheetName, 45, 15, null),
						new ExpectedCellValue(sheetName, 46, 15, null),
						new ExpectedCellValue(sheetName, 47, 15, null),
						new ExpectedCellValue(sheetName, 48, 15, null),
						new ExpectedCellValue(sheetName, 49, 15, 99d),
						new ExpectedCellValue(sheetName, 50, 15, 99d),
						new ExpectedCellValue(sheetName, 51, 15, 99d),
						new ExpectedCellValue(sheetName, 52, 15, null),
						new ExpectedCellValue(sheetName, 53, 15, 1d),
						new ExpectedCellValue(sheetName, 54, 15, 1d),
						new ExpectedCellValue(sheetName, 55, 15, 1d),
						new ExpectedCellValue(sheetName, 56, 15, null),
						new ExpectedCellValue(sheetName, 57, 15, 99d),
						new ExpectedCellValue(sheetName, 58, 15, 99d),
						new ExpectedCellValue(sheetName, 59, 15, 99d),
						new ExpectedCellValue(sheetName, 60, 15, 99d),
						new ExpectedCellValue(sheetName, 61, 15, 1d),
						new ExpectedCellValue(sheetName, 62, 15, 99d),
						new ExpectedCellValue(sheetName, 63, 15, 99d),
						new ExpectedCellValue(sheetName, 64, 15, 1d),
						new ExpectedCellValue(sheetName, 65, 15, 99d),
						new ExpectedCellValue(sheetName, 66, 15, 99d),
						new ExpectedCellValue(sheetName, 67, 15, 1d),
						new ExpectedCellValue(sheetName, 68, 15, 99d),
						new ExpectedCellValue(sheetName, 69, 15, null),
						new ExpectedCellValue(sheetName, 70, 15, null),
						new ExpectedCellValue(sheetName, 71, 15, null),
						new ExpectedCellValue(sheetName, 72, 15, null),
						new ExpectedCellValue(sheetName, 73, 15, null),
						new ExpectedCellValue(sheetName, 74, 15, null),
						new ExpectedCellValue(sheetName, 75, 15, null),
						new ExpectedCellValue(sheetName, 76, 15, null),
						new ExpectedCellValue(sheetName, 77, 15, null),
						new ExpectedCellValue(sheetName, 78, 15, null),
						new ExpectedCellValue(sheetName, 79, 15, null),
						new ExpectedCellValue(sheetName, 80, 15, null),
						new ExpectedCellValue(sheetName, 81, 15, null),
						new ExpectedCellValue(sheetName, 82, 15, null),
						new ExpectedCellValue(sheetName, 83, 15, null),
						new ExpectedCellValue(sheetName, 84, 15, null),
						new ExpectedCellValue(sheetName, 85, 15, null),
						new ExpectedCellValue(sheetName, 86, 15, null),
						new ExpectedCellValue(sheetName, 87, 15, null),
						new ExpectedCellValue(sheetName, 88, 15, null),
						new ExpectedCellValue(sheetName, 89, 15, null),
						new ExpectedCellValue(sheetName, 90, 15, null),
						new ExpectedCellValue(sheetName, 91, 15, null),
						new ExpectedCellValue(sheetName, 92, 15, null),
						new ExpectedCellValue(sheetName, 93, 15, null),
						new ExpectedCellValue(sheetName, 94, 15, null),
						new ExpectedCellValue(sheetName, 95, 15, null),
						new ExpectedCellValue(sheetName, 96, 15, null),
						new ExpectedCellValue(sheetName, 97, 15, null),
						new ExpectedCellValue(sheetName, 98, 15, null),
						new ExpectedCellValue(sheetName, 99, 15, null),
						new ExpectedCellValue(sheetName, 100, 15, null),
						new ExpectedCellValue(sheetName, 101, 15, null),
						new ExpectedCellValue(sheetName, 102, 15, null),
						new ExpectedCellValue(sheetName, 103, 15, null),
						new ExpectedCellValue(sheetName, 104, 15, null),
						new ExpectedCellValue(sheetName, 105, 15, null),
						new ExpectedCellValue(sheetName, 106, 15, null),
						new ExpectedCellValue(sheetName, 107, 15, null),
						new ExpectedCellValue(sheetName, 108, 15, null),
						new ExpectedCellValue(sheetName, 109, 15, null),
						new ExpectedCellValue(sheetName, 110, 15, null),
						new ExpectedCellValue(sheetName, 111, 15, null),
						new ExpectedCellValue(sheetName, 112, 15, null),
						new ExpectedCellValue(sheetName, 113, 15, null),
						new ExpectedCellValue(sheetName, 114, 15, null),
						new ExpectedCellValue(sheetName, 115, 15, null),
						new ExpectedCellValue(sheetName, 116, 15, null),
						new ExpectedCellValue(sheetName, 117, 15, null),
						new ExpectedCellValue(sheetName, 118, 15, null),
						new ExpectedCellValue(sheetName, 119, 15, null),
						new ExpectedCellValue(sheetName, 120, 15, null),
						new ExpectedCellValue(sheetName, 121, 15, null),
						new ExpectedCellValue(sheetName, 122, 15, null),
						new ExpectedCellValue(sheetName, 123, 15, null),
						new ExpectedCellValue(sheetName, 124, 15, null),
						new ExpectedCellValue(sheetName, 125, 15, null),
						new ExpectedCellValue(sheetName, 126, 15, null),
						new ExpectedCellValue(sheetName, 127, 15, null),
						new ExpectedCellValue(sheetName, 128, 15, null),
						new ExpectedCellValue(sheetName, 129, 15, null),
						new ExpectedCellValue(sheetName, 130, 15, null),
						new ExpectedCellValue(sheetName, 131, 15, null),
						new ExpectedCellValue(sheetName, 132, 15, null),
						new ExpectedCellValue(sheetName, 133, 15, null),
						new ExpectedCellValue(sheetName, 134, 15, null),
						new ExpectedCellValue(sheetName, 135, 15, null),
						new ExpectedCellValue(sheetName, 136, 15, null),
						new ExpectedCellValue(sheetName, 137, 15, null),
						new ExpectedCellValue(sheetName, 138, 15, null),
						new ExpectedCellValue(sheetName, 139, 15, null),
						new ExpectedCellValue(sheetName, 140, 15, null),
						new ExpectedCellValue(sheetName, 141, 15, null),
						new ExpectedCellValue(sheetName, 142, 15, null),
						new ExpectedCellValue(sheetName, 143, 15, null),
						new ExpectedCellValue(sheetName, 144, 15, null),
						new ExpectedCellValue(sheetName, 145, 15, null),
						new ExpectedCellValue(sheetName, 146, 15, null),
						new ExpectedCellValue(sheetName, 147, 15, null),
						new ExpectedCellValue(sheetName, 148, 15, null),
						new ExpectedCellValue(sheetName, 149, 15, 99d),
						new ExpectedCellValue(sheetName, 150, 15, 1d),
						new ExpectedCellValue(sheetName, 151, 15, 99d),
						new ExpectedCellValue(sheetName, 2, 16, null),
						new ExpectedCellValue(sheetName, 3, 16, "Sleeping Bag Total"),
						new ExpectedCellValue(sheetName, 4, 16, null),
						new ExpectedCellValue(sheetName, 5, 16, null),
						new ExpectedCellValue(sheetName, 6, 16, null),
						new ExpectedCellValue(sheetName, 7, 16, null),
						new ExpectedCellValue(sheetName, 8, 16, null),
						new ExpectedCellValue(sheetName, 9, 16, null),
						new ExpectedCellValue(sheetName, 10, 16, null),
						new ExpectedCellValue(sheetName, 11, 16, null),
						new ExpectedCellValue(sheetName, 12, 16, null),
						new ExpectedCellValue(sheetName, 13, 16, null),
						new ExpectedCellValue(sheetName, 14, 16, null),
						new ExpectedCellValue(sheetName, 15, 16, null),
						new ExpectedCellValue(sheetName, 16, 16, null),
						new ExpectedCellValue(sheetName, 17, 16, null),
						new ExpectedCellValue(sheetName, 18, 16, null),
						new ExpectedCellValue(sheetName, 19, 16, null),
						new ExpectedCellValue(sheetName, 20, 16, null),
						new ExpectedCellValue(sheetName, 21, 16, null),
						new ExpectedCellValue(sheetName, 22, 16, null),
						new ExpectedCellValue(sheetName, 23, 16, null),
						new ExpectedCellValue(sheetName, 24, 16, null),
						new ExpectedCellValue(sheetName, 25, 16, null),
						new ExpectedCellValue(sheetName, 26, 16, null),
						new ExpectedCellValue(sheetName, 27, 16, null),
						new ExpectedCellValue(sheetName, 28, 16, null),
						new ExpectedCellValue(sheetName, 29, 16, null),
						new ExpectedCellValue(sheetName, 30, 16, null),
						new ExpectedCellValue(sheetName, 31, 16, null),
						new ExpectedCellValue(sheetName, 32, 16, null),
						new ExpectedCellValue(sheetName, 33, 16, null),
						new ExpectedCellValue(sheetName, 34, 16, null),
						new ExpectedCellValue(sheetName, 35, 16, null),
						new ExpectedCellValue(sheetName, 36, 16, null),
						new ExpectedCellValue(sheetName, 37, 16, null),
						new ExpectedCellValue(sheetName, 38, 16, null),
						new ExpectedCellValue(sheetName, 39, 16, null),
						new ExpectedCellValue(sheetName, 40, 16, null),
						new ExpectedCellValue(sheetName, 41, 16, null),
						new ExpectedCellValue(sheetName, 42, 16, null),
						new ExpectedCellValue(sheetName, 43, 16, null),
						new ExpectedCellValue(sheetName, 44, 16, null),
						new ExpectedCellValue(sheetName, 45, 16, null),
						new ExpectedCellValue(sheetName, 46, 16, null),
						new ExpectedCellValue(sheetName, 47, 16, null),
						new ExpectedCellValue(sheetName, 48, 16, null),
						new ExpectedCellValue(sheetName, 49, 16, 99d),
						new ExpectedCellValue(sheetName, 50, 16, 99d),
						new ExpectedCellValue(sheetName, 51, 16, 99d),
						new ExpectedCellValue(sheetName, 52, 16, null),
						new ExpectedCellValue(sheetName, 53, 16, 1d),
						new ExpectedCellValue(sheetName, 54, 16, 1d),
						new ExpectedCellValue(sheetName, 55, 16, 1d),
						new ExpectedCellValue(sheetName, 56, 16, null),
						new ExpectedCellValue(sheetName, 57, 16, 99d),
						new ExpectedCellValue(sheetName, 58, 16, 99d),
						new ExpectedCellValue(sheetName, 59, 16, 99d),
						new ExpectedCellValue(sheetName, 60, 16, 99d),
						new ExpectedCellValue(sheetName, 61, 16, 1d),
						new ExpectedCellValue(sheetName, 62, 16, 99d),
						new ExpectedCellValue(sheetName, 63, 16, 99d),
						new ExpectedCellValue(sheetName, 64, 16, 1d),
						new ExpectedCellValue(sheetName, 65, 16, 99d),
						new ExpectedCellValue(sheetName, 66, 16, 99d),
						new ExpectedCellValue(sheetName, 67, 16, 1d),
						new ExpectedCellValue(sheetName, 68, 16, 99d),
						new ExpectedCellValue(sheetName, 69, 16, null),
						new ExpectedCellValue(sheetName, 70, 16, null),
						new ExpectedCellValue(sheetName, 71, 16, null),
						new ExpectedCellValue(sheetName, 72, 16, null),
						new ExpectedCellValue(sheetName, 73, 16, null),
						new ExpectedCellValue(sheetName, 74, 16, null),
						new ExpectedCellValue(sheetName, 75, 16, null),
						new ExpectedCellValue(sheetName, 76, 16, null),
						new ExpectedCellValue(sheetName, 77, 16, null),
						new ExpectedCellValue(sheetName, 78, 16, null),
						new ExpectedCellValue(sheetName, 79, 16, null),
						new ExpectedCellValue(sheetName, 80, 16, null),
						new ExpectedCellValue(sheetName, 81, 16, null),
						new ExpectedCellValue(sheetName, 82, 16, null),
						new ExpectedCellValue(sheetName, 83, 16, null),
						new ExpectedCellValue(sheetName, 84, 16, null),
						new ExpectedCellValue(sheetName, 85, 16, null),
						new ExpectedCellValue(sheetName, 86, 16, null),
						new ExpectedCellValue(sheetName, 87, 16, null),
						new ExpectedCellValue(sheetName, 88, 16, null),
						new ExpectedCellValue(sheetName, 89, 16, null),
						new ExpectedCellValue(sheetName, 90, 16, null),
						new ExpectedCellValue(sheetName, 91, 16, null),
						new ExpectedCellValue(sheetName, 92, 16, null),
						new ExpectedCellValue(sheetName, 93, 16, null),
						new ExpectedCellValue(sheetName, 94, 16, null),
						new ExpectedCellValue(sheetName, 95, 16, null),
						new ExpectedCellValue(sheetName, 96, 16, null),
						new ExpectedCellValue(sheetName, 97, 16, null),
						new ExpectedCellValue(sheetName, 98, 16, null),
						new ExpectedCellValue(sheetName, 99, 16, null),
						new ExpectedCellValue(sheetName, 100, 16, null),
						new ExpectedCellValue(sheetName, 101, 16, null),
						new ExpectedCellValue(sheetName, 102, 16, null),
						new ExpectedCellValue(sheetName, 103, 16, null),
						new ExpectedCellValue(sheetName, 104, 16, null),
						new ExpectedCellValue(sheetName, 105, 16, null),
						new ExpectedCellValue(sheetName, 106, 16, null),
						new ExpectedCellValue(sheetName, 107, 16, null),
						new ExpectedCellValue(sheetName, 108, 16, null),
						new ExpectedCellValue(sheetName, 109, 16, null),
						new ExpectedCellValue(sheetName, 110, 16, null),
						new ExpectedCellValue(sheetName, 111, 16, null),
						new ExpectedCellValue(sheetName, 112, 16, null),
						new ExpectedCellValue(sheetName, 113, 16, null),
						new ExpectedCellValue(sheetName, 114, 16, null),
						new ExpectedCellValue(sheetName, 115, 16, null),
						new ExpectedCellValue(sheetName, 116, 16, null),
						new ExpectedCellValue(sheetName, 117, 16, null),
						new ExpectedCellValue(sheetName, 118, 16, null),
						new ExpectedCellValue(sheetName, 119, 16, null),
						new ExpectedCellValue(sheetName, 120, 16, null),
						new ExpectedCellValue(sheetName, 121, 16, null),
						new ExpectedCellValue(sheetName, 122, 16, null),
						new ExpectedCellValue(sheetName, 123, 16, null),
						new ExpectedCellValue(sheetName, 124, 16, null),
						new ExpectedCellValue(sheetName, 125, 16, null),
						new ExpectedCellValue(sheetName, 126, 16, null),
						new ExpectedCellValue(sheetName, 127, 16, null),
						new ExpectedCellValue(sheetName, 128, 16, null),
						new ExpectedCellValue(sheetName, 129, 16, null),
						new ExpectedCellValue(sheetName, 130, 16, null),
						new ExpectedCellValue(sheetName, 131, 16, null),
						new ExpectedCellValue(sheetName, 132, 16, null),
						new ExpectedCellValue(sheetName, 133, 16, null),
						new ExpectedCellValue(sheetName, 134, 16, null),
						new ExpectedCellValue(sheetName, 135, 16, null),
						new ExpectedCellValue(sheetName, 136, 16, null),
						new ExpectedCellValue(sheetName, 137, 16, null),
						new ExpectedCellValue(sheetName, 138, 16, null),
						new ExpectedCellValue(sheetName, 139, 16, null),
						new ExpectedCellValue(sheetName, 140, 16, null),
						new ExpectedCellValue(sheetName, 141, 16, null),
						new ExpectedCellValue(sheetName, 142, 16, null),
						new ExpectedCellValue(sheetName, 143, 16, null),
						new ExpectedCellValue(sheetName, 144, 16, null),
						new ExpectedCellValue(sheetName, 145, 16, null),
						new ExpectedCellValue(sheetName, 146, 16, null),
						new ExpectedCellValue(sheetName, 147, 16, null),
						new ExpectedCellValue(sheetName, 148, 16, null),
						new ExpectedCellValue(sheetName, 149, 16, 99d),
						new ExpectedCellValue(sheetName, 150, 16, 1d),
						new ExpectedCellValue(sheetName, 151, 16, 99d),
						new ExpectedCellValue(sheetName, 2, 17, "San Francisco Total"),
						new ExpectedCellValue(sheetName, 3, 17, null),
						new ExpectedCellValue(sheetName, 4, 17, null),
						new ExpectedCellValue(sheetName, 5, 17, null),
						new ExpectedCellValue(sheetName, 6, 17, null),
						new ExpectedCellValue(sheetName, 7, 17, null),
						new ExpectedCellValue(sheetName, 8, 17, null),
						new ExpectedCellValue(sheetName, 9, 17, null),
						new ExpectedCellValue(sheetName, 10, 17, null),
						new ExpectedCellValue(sheetName, 11, 17, null),
						new ExpectedCellValue(sheetName, 12, 17, null),
						new ExpectedCellValue(sheetName, 13, 17, null),
						new ExpectedCellValue(sheetName, 14, 17, null),
						new ExpectedCellValue(sheetName, 15, 17, null),
						new ExpectedCellValue(sheetName, 16, 17, null),
						new ExpectedCellValue(sheetName, 17, 17, null),
						new ExpectedCellValue(sheetName, 18, 17, null),
						new ExpectedCellValue(sheetName, 19, 17, null),
						new ExpectedCellValue(sheetName, 20, 17, null),
						new ExpectedCellValue(sheetName, 21, 17, null),
						new ExpectedCellValue(sheetName, 22, 17, null),
						new ExpectedCellValue(sheetName, 23, 17, null),
						new ExpectedCellValue(sheetName, 24, 17, null),
						new ExpectedCellValue(sheetName, 25, 17, null),
						new ExpectedCellValue(sheetName, 26, 17, null),
						new ExpectedCellValue(sheetName, 27, 17, null),
						new ExpectedCellValue(sheetName, 28, 17, null),
						new ExpectedCellValue(sheetName, 29, 17, null),
						new ExpectedCellValue(sheetName, 30, 17, null),
						new ExpectedCellValue(sheetName, 31, 17, null),
						new ExpectedCellValue(sheetName, 32, 17, null),
						new ExpectedCellValue(sheetName, 33, 17, null),
						new ExpectedCellValue(sheetName, 34, 17, null),
						new ExpectedCellValue(sheetName, 35, 17, null),
						new ExpectedCellValue(sheetName, 36, 17, null),
						new ExpectedCellValue(sheetName, 37, 17, null),
						new ExpectedCellValue(sheetName, 38, 17, null),
						new ExpectedCellValue(sheetName, 39, 17, null),
						new ExpectedCellValue(sheetName, 40, 17, null),
						new ExpectedCellValue(sheetName, 41, 17, null),
						new ExpectedCellValue(sheetName, 42, 17, null),
						new ExpectedCellValue(sheetName, 43, 17, null),
						new ExpectedCellValue(sheetName, 44, 17, null),
						new ExpectedCellValue(sheetName, 45, 17, null),
						new ExpectedCellValue(sheetName, 46, 17, null),
						new ExpectedCellValue(sheetName, 47, 17, null),
						new ExpectedCellValue(sheetName, 48, 17, null),
						new ExpectedCellValue(sheetName, 49, 17, 99d),
						new ExpectedCellValue(sheetName, 50, 17, 99d),
						new ExpectedCellValue(sheetName, 51, 17, 99d),
						new ExpectedCellValue(sheetName, 52, 17, null),
						new ExpectedCellValue(sheetName, 53, 17, 1d),
						new ExpectedCellValue(sheetName, 54, 17, 1d),
						new ExpectedCellValue(sheetName, 55, 17, 1d),
						new ExpectedCellValue(sheetName, 56, 17, null),
						new ExpectedCellValue(sheetName, 57, 17, 99d),
						new ExpectedCellValue(sheetName, 58, 17, 99d),
						new ExpectedCellValue(sheetName, 59, 17, 99d),
						new ExpectedCellValue(sheetName, 60, 17, 99d),
						new ExpectedCellValue(sheetName, 61, 17, 1d),
						new ExpectedCellValue(sheetName, 62, 17, 99d),
						new ExpectedCellValue(sheetName, 63, 17, 99d),
						new ExpectedCellValue(sheetName, 64, 17, 1d),
						new ExpectedCellValue(sheetName, 65, 17, 99d),
						new ExpectedCellValue(sheetName, 66, 17, 99d),
						new ExpectedCellValue(sheetName, 67, 17, 1d),
						new ExpectedCellValue(sheetName, 68, 17, 99d),
						new ExpectedCellValue(sheetName, 69, 17, null),
						new ExpectedCellValue(sheetName, 70, 17, null),
						new ExpectedCellValue(sheetName, 71, 17, null),
						new ExpectedCellValue(sheetName, 72, 17, null),
						new ExpectedCellValue(sheetName, 73, 17, null),
						new ExpectedCellValue(sheetName, 74, 17, null),
						new ExpectedCellValue(sheetName, 75, 17, null),
						new ExpectedCellValue(sheetName, 76, 17, null),
						new ExpectedCellValue(sheetName, 77, 17, null),
						new ExpectedCellValue(sheetName, 78, 17, null),
						new ExpectedCellValue(sheetName, 79, 17, null),
						new ExpectedCellValue(sheetName, 80, 17, null),
						new ExpectedCellValue(sheetName, 81, 17, null),
						new ExpectedCellValue(sheetName, 82, 17, null),
						new ExpectedCellValue(sheetName, 83, 17, null),
						new ExpectedCellValue(sheetName, 84, 17, null),
						new ExpectedCellValue(sheetName, 85, 17, null),
						new ExpectedCellValue(sheetName, 86, 17, null),
						new ExpectedCellValue(sheetName, 87, 17, null),
						new ExpectedCellValue(sheetName, 88, 17, null),
						new ExpectedCellValue(sheetName, 89, 17, null),
						new ExpectedCellValue(sheetName, 90, 17, null),
						new ExpectedCellValue(sheetName, 91, 17, null),
						new ExpectedCellValue(sheetName, 92, 17, null),
						new ExpectedCellValue(sheetName, 93, 17, null),
						new ExpectedCellValue(sheetName, 94, 17, null),
						new ExpectedCellValue(sheetName, 95, 17, null),
						new ExpectedCellValue(sheetName, 96, 17, null),
						new ExpectedCellValue(sheetName, 97, 17, null),
						new ExpectedCellValue(sheetName, 98, 17, null),
						new ExpectedCellValue(sheetName, 99, 17, null),
						new ExpectedCellValue(sheetName, 100, 17, null),
						new ExpectedCellValue(sheetName, 101, 17, null),
						new ExpectedCellValue(sheetName, 102, 17, null),
						new ExpectedCellValue(sheetName, 103, 17, null),
						new ExpectedCellValue(sheetName, 104, 17, null),
						new ExpectedCellValue(sheetName, 105, 17, null),
						new ExpectedCellValue(sheetName, 106, 17, null),
						new ExpectedCellValue(sheetName, 107, 17, null),
						new ExpectedCellValue(sheetName, 108, 17, null),
						new ExpectedCellValue(sheetName, 109, 17, null),
						new ExpectedCellValue(sheetName, 110, 17, null),
						new ExpectedCellValue(sheetName, 111, 17, null),
						new ExpectedCellValue(sheetName, 112, 17, null),
						new ExpectedCellValue(sheetName, 113, 17, null),
						new ExpectedCellValue(sheetName, 114, 17, null),
						new ExpectedCellValue(sheetName, 115, 17, null),
						new ExpectedCellValue(sheetName, 116, 17, null),
						new ExpectedCellValue(sheetName, 117, 17, null),
						new ExpectedCellValue(sheetName, 118, 17, null),
						new ExpectedCellValue(sheetName, 119, 17, null),
						new ExpectedCellValue(sheetName, 120, 17, null),
						new ExpectedCellValue(sheetName, 121, 17, null),
						new ExpectedCellValue(sheetName, 122, 17, null),
						new ExpectedCellValue(sheetName, 123, 17, null),
						new ExpectedCellValue(sheetName, 124, 17, null),
						new ExpectedCellValue(sheetName, 125, 17, null),
						new ExpectedCellValue(sheetName, 126, 17, null),
						new ExpectedCellValue(sheetName, 127, 17, null),
						new ExpectedCellValue(sheetName, 128, 17, null),
						new ExpectedCellValue(sheetName, 129, 17, 415.75),
						new ExpectedCellValue(sheetName, 130, 17, 415.75),
						new ExpectedCellValue(sheetName, 131, 17, 415.75),
						new ExpectedCellValue(sheetName, 132, 17, null),
						new ExpectedCellValue(sheetName, 133, 17, 1d),
						new ExpectedCellValue(sheetName, 134, 17, 1d),
						new ExpectedCellValue(sheetName, 135, 17, 1d),
						new ExpectedCellValue(sheetName, 136, 17, null),
						new ExpectedCellValue(sheetName, 137, 17, 415.75),
						new ExpectedCellValue(sheetName, 138, 17, 415.75),
						new ExpectedCellValue(sheetName, 139, 17, 415.75),
						new ExpectedCellValue(sheetName, 140, 17, 415.75),
						new ExpectedCellValue(sheetName, 141, 17, 1d),
						new ExpectedCellValue(sheetName, 142, 17, 415.75),
						new ExpectedCellValue(sheetName, 143, 17, 415.75),
						new ExpectedCellValue(sheetName, 144, 17, 1d),
						new ExpectedCellValue(sheetName, 145, 17, 415.75),
						new ExpectedCellValue(sheetName, 146, 17, 415.75),
						new ExpectedCellValue(sheetName, 147, 17, 1d),
						new ExpectedCellValue(sheetName, 148, 17, 415.75),
						new ExpectedCellValue(sheetName, 149, 17, 514.75),
						new ExpectedCellValue(sheetName, 150, 17, 2d),
						new ExpectedCellValue(sheetName, 151, 17, 514.75),

						new ExpectedCellValue(sheetName, 2, 18, "Grand Total"),
						new ExpectedCellValue(sheetName, 3, 18, null),
						new ExpectedCellValue(sheetName, 4, 18, null),
						new ExpectedCellValue(sheetName, 5, 18, null),
						new ExpectedCellValue(sheetName, 6, 18, null),
						new ExpectedCellValue(sheetName, 7, 18, null),
						new ExpectedCellValue(sheetName, 8, 18, null),
						new ExpectedCellValue(sheetName, 9, 18, 415.75),
						new ExpectedCellValue(sheetName, 10, 18, 415.75),
						new ExpectedCellValue(sheetName, 11, 18, 415.75),
						new ExpectedCellValue(sheetName, 12, 18, null),
						new ExpectedCellValue(sheetName, 13, 18, 2d),
						new ExpectedCellValue(sheetName, 14, 18, 2d),
						new ExpectedCellValue(sheetName, 15, 18, 2d),
						new ExpectedCellValue(sheetName, 16, 18, null),
						new ExpectedCellValue(sheetName, 17, 18, 831.5),
						new ExpectedCellValue(sheetName, 18, 18, 831.5),
						new ExpectedCellValue(sheetName, 19, 18, 831.5),
						new ExpectedCellValue(sheetName, 20, 18, 415.75),
						new ExpectedCellValue(sheetName, 21, 18, 2d),
						new ExpectedCellValue(sheetName, 22, 18, 831.5),
						new ExpectedCellValue(sheetName, 23, 18, 415.75),
						new ExpectedCellValue(sheetName, 24, 18, 2d),
						new ExpectedCellValue(sheetName, 25, 18, 831.5),
						new ExpectedCellValue(sheetName, 26, 18, 415.75),
						new ExpectedCellValue(sheetName, 27, 18, 2d),
						new ExpectedCellValue(sheetName, 28, 18, 831.5),
						new ExpectedCellValue(sheetName, 29, 18, null),
						new ExpectedCellValue(sheetName, 30, 18, null),
						new ExpectedCellValue(sheetName, 31, 18, null),
						new ExpectedCellValue(sheetName, 32, 18, null),
						new ExpectedCellValue(sheetName, 33, 18, 415.75),
						new ExpectedCellValue(sheetName, 34, 18, 415.75),
						new ExpectedCellValue(sheetName, 35, 18, 415.75),
						new ExpectedCellValue(sheetName, 36, 18, null),
						new ExpectedCellValue(sheetName, 37, 18, 2d),
						new ExpectedCellValue(sheetName, 38, 18, 2d),
						new ExpectedCellValue(sheetName, 39, 18, 2d),
						new ExpectedCellValue(sheetName, 40, 18, null),
						new ExpectedCellValue(sheetName, 41, 18, 831.5),
						new ExpectedCellValue(sheetName, 42, 18, 831.5),
						new ExpectedCellValue(sheetName, 43, 18, 831.5),
						new ExpectedCellValue(sheetName, 44, 18, 415.75),
						new ExpectedCellValue(sheetName, 45, 18, 2d),
						new ExpectedCellValue(sheetName, 46, 18, 831.5),
						new ExpectedCellValue(sheetName, 47, 18, null),
						new ExpectedCellValue(sheetName, 48, 18, null),
						new ExpectedCellValue(sheetName, 49, 18, 99d),
						new ExpectedCellValue(sheetName, 50, 18, 99d),
						new ExpectedCellValue(sheetName, 51, 18, 99d),
						new ExpectedCellValue(sheetName, 52, 18, null),
						new ExpectedCellValue(sheetName, 53, 18, 1d),
						new ExpectedCellValue(sheetName, 54, 18, 1d),
						new ExpectedCellValue(sheetName, 55, 18, 1d),
						new ExpectedCellValue(sheetName, 56, 18, null),
						new ExpectedCellValue(sheetName, 57, 18, 99d),
						new ExpectedCellValue(sheetName, 58, 18, 99d),
						new ExpectedCellValue(sheetName, 59, 18, 99d),
						new ExpectedCellValue(sheetName, 60, 18, 99d),
						new ExpectedCellValue(sheetName, 61, 18, 1d),
						new ExpectedCellValue(sheetName, 62, 18, 99d),
						new ExpectedCellValue(sheetName, 63, 18, 514.75),
						new ExpectedCellValue(sheetName, 64, 18, 3d),
						new ExpectedCellValue(sheetName, 65, 18, 930.5),
						new ExpectedCellValue(sheetName, 66, 18, 514.75),
						new ExpectedCellValue(sheetName, 67, 18, 3d),
						new ExpectedCellValue(sheetName, 68, 18, 930.5),
						new ExpectedCellValue(sheetName, 69, 18, null),
						new ExpectedCellValue(sheetName, 70, 18, null),
						new ExpectedCellValue(sheetName, 71, 18, null),
						new ExpectedCellValue(sheetName, 72, 18, null),
						new ExpectedCellValue(sheetName, 73, 18, 415.75),
						new ExpectedCellValue(sheetName, 74, 18, 415.75),
						new ExpectedCellValue(sheetName, 75, 18, 415.75),
						new ExpectedCellValue(sheetName, 76, 18, null),
						new ExpectedCellValue(sheetName, 77, 18, 2d),
						new ExpectedCellValue(sheetName, 78, 18, 2d),
						new ExpectedCellValue(sheetName, 79, 18, 2d),
						new ExpectedCellValue(sheetName, 80, 18, null),
						new ExpectedCellValue(sheetName, 81, 18, 831.5),
						new ExpectedCellValue(sheetName, 82, 18, 831.5),
						new ExpectedCellValue(sheetName, 83, 18, 831.5),
						new ExpectedCellValue(sheetName, 84, 18, 415.75),
						new ExpectedCellValue(sheetName, 85, 18, 2d),
						new ExpectedCellValue(sheetName, 86, 18, 831.5),
						new ExpectedCellValue(sheetName, 87, 18, null),
						new ExpectedCellValue(sheetName, 88, 18, null),
						new ExpectedCellValue(sheetName, 89, 18, 24.99),
						new ExpectedCellValue(sheetName, 90, 18, 24.99),
						new ExpectedCellValue(sheetName, 91, 18, 24.99),
						new ExpectedCellValue(sheetName, 92, 18, null),
						new ExpectedCellValue(sheetName, 93, 18, 1d),
						new ExpectedCellValue(sheetName, 94, 18, 1d),
						new ExpectedCellValue(sheetName, 95, 18, 1d),
						new ExpectedCellValue(sheetName, 96, 18, null),
						new ExpectedCellValue(sheetName, 97, 18, 24.99),
						new ExpectedCellValue(sheetName, 98, 18, 24.99),
						new ExpectedCellValue(sheetName, 99, 18, 24.99),
						new ExpectedCellValue(sheetName, 100, 18, 24.99),
						new ExpectedCellValue(sheetName, 101, 18, 1d),
						new ExpectedCellValue(sheetName, 102, 18, 24.99),
						new ExpectedCellValue(sheetName, 103, 18, 440.74),
						new ExpectedCellValue(sheetName, 104, 18, 3d),
						new ExpectedCellValue(sheetName, 105, 18, 856.49),
						new ExpectedCellValue(sheetName, 106, 18, null),
						new ExpectedCellValue(sheetName, 107, 18, null),
						new ExpectedCellValue(sheetName, 108, 18, null),
						new ExpectedCellValue(sheetName, 109, 18, 199d),
						new ExpectedCellValue(sheetName, 110, 18, 199d),
						new ExpectedCellValue(sheetName, 111, 18, 199d),
						new ExpectedCellValue(sheetName, 112, 18, null),
						new ExpectedCellValue(sheetName, 113, 18, 6d),
						new ExpectedCellValue(sheetName, 114, 18, 6d),
						new ExpectedCellValue(sheetName, 115, 18, 6d),
						new ExpectedCellValue(sheetName, 116, 18, null),
						new ExpectedCellValue(sheetName, 117, 18, 1194d),
						new ExpectedCellValue(sheetName, 118, 18, 1194d),
						new ExpectedCellValue(sheetName, 119, 18, 1194d),
						new ExpectedCellValue(sheetName, 120, 18, 199d),
						new ExpectedCellValue(sheetName, 121, 18, 6d),
						new ExpectedCellValue(sheetName, 122, 18, 1194d),
						new ExpectedCellValue(sheetName, 123, 18, 199d),
						new ExpectedCellValue(sheetName, 124, 18, 6d),
						new ExpectedCellValue(sheetName, 125, 18, 1194d),
						new ExpectedCellValue(sheetName, 126, 18, null),
						new ExpectedCellValue(sheetName, 127, 18, null),
						new ExpectedCellValue(sheetName, 128, 18, null),
						new ExpectedCellValue(sheetName, 129, 18, 415.75),
						new ExpectedCellValue(sheetName, 130, 18, 415.75),
						new ExpectedCellValue(sheetName, 131, 18, 415.75),
						new ExpectedCellValue(sheetName, 132, 18, null),
						new ExpectedCellValue(sheetName, 133, 18, 1d),
						new ExpectedCellValue(sheetName, 134, 18, 1d),
						new ExpectedCellValue(sheetName, 135, 18, 1d),
						new ExpectedCellValue(sheetName, 136, 18, null),
						new ExpectedCellValue(sheetName, 137, 18, 415.75),
						new ExpectedCellValue(sheetName, 138, 18, 415.75),
						new ExpectedCellValue(sheetName, 139, 18, 415.75),
						new ExpectedCellValue(sheetName, 140, 18, 415.75),
						new ExpectedCellValue(sheetName, 141, 18, 1d),
						new ExpectedCellValue(sheetName, 142, 18, 415.75),
						new ExpectedCellValue(sheetName, 143, 18, 415.75),
						new ExpectedCellValue(sheetName, 144, 18, 1d),
						new ExpectedCellValue(sheetName, 145, 18, 415.75),
						new ExpectedCellValue(sheetName, 146, 18, 1055.49),
						new ExpectedCellValue(sheetName, 147, 18, 10d),
						new ExpectedCellValue(sheetName, 148, 18, 2466.24),
						new ExpectedCellValue(sheetName, 149, 18, 1985.99),
						new ExpectedCellValue(sheetName, 150, 18, 15d),
						new ExpectedCellValue(sheetName, 151, 18, 4228.24),
				});
			}
		}
		#endregion

		#region Helper Methods
		private void CheckPivotTableAddress(ExcelAddress expectedAddress, ExcelAddress pivotTableAddress)
		{
			Assert.AreEqual(expectedAddress.Start.Row, pivotTableAddress.Start.Row);
			Assert.AreEqual(expectedAddress.Start.Column, pivotTableAddress.Start.Column);
			Assert.AreEqual(expectedAddress.End.Row, pivotTableAddress.End.Row);
			Assert.AreEqual(expectedAddress.End.Column, pivotTableAddress.End.Column);
		}
		#endregion
	}
}