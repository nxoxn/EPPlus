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
		[DeploymentItem(@"..\..\Workbooks\PivotTableDataSourceTypeWorksheet.xlsx")]
		public void PivotTableRefreshFromCacheWithChangedData()
		{
			var file = new FileInfo("PivotTableDataSourceTypeWorksheet.xlsx");
			Assert.IsTrue(file.Exists);
			using (var package = new ExcelPackage(file))
			{
				var worksheet = package.Workbook.Worksheets.First();
				var pivotTable = worksheet.PivotTables.First();
				var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
				worksheet.Cells[4, 5].Value = "Blue";
				worksheet.Cells[5, 5].Value = "Green";
				worksheet.Cells[6, 5].Value = "Purple";
				cacheDefinition.UpdateData();
				Assert.AreEqual(4, pivotTable.Fields.Count);
				Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
				Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
				Assert.AreEqual(6, pivotTable.Fields[2].Items.Count);
				Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
				foreach (var field in pivotTable.Fields)
				{
					if (field.Items.Count > 0)
						this.CheckFieldItems(field);
				}
				Assert.AreEqual(7, pivotTable.RowItems.Count);
				Assert.AreEqual("Blue", worksheet.Cells[11, 9].Value);
				Assert.AreEqual(100d, worksheet.Cells[11, 10].Value);
				Assert.AreEqual("Bike", worksheet.Cells[12, 9].Value);
				Assert.AreEqual(100d, worksheet.Cells[12, 10].Value);
				Assert.AreEqual("Green", worksheet.Cells[13, 9].Value);
				Assert.AreEqual(90000d, worksheet.Cells[13, 10].Value);
				Assert.AreEqual("Car", worksheet.Cells[14, 9].Value);
				Assert.AreEqual(90000d, worksheet.Cells[14, 10].Value);
				Assert.AreEqual("Purple", worksheet.Cells[15, 9].Value);
				Assert.AreEqual(10d, worksheet.Cells[15, 10].Value);
				Assert.AreEqual("Skateboard", worksheet.Cells[16, 9].Value);
				Assert.AreEqual(10d, worksheet.Cells[16, 10].Value);
				Assert.AreEqual("Grand Total", worksheet.Cells[17, 9].Value);
				Assert.AreEqual(90110d, worksheet.Cells[17, 10].Value);
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableDataSourceTypeWorksheet.xlsx")]
		public void PivotTableRefreshFromCacheWithAddedData()
		{
			var file = new FileInfo("PivotTableDataSourceTypeWorksheet.xlsx");
			Assert.IsTrue(file.Exists);
			using (var package = new ExcelPackage(file))
			{
				var worksheet = package.Workbook.Worksheets.First();
				var pivotTable = worksheet.PivotTables.First();
				var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
				worksheet.Cells[7, 3].Value = 4;
				worksheet.Cells[7, 4].Value = "Scooter";
				worksheet.Cells[7, 5].Value = "Purple";
				worksheet.Cells[7, 6].Value = 28;
				cacheDefinition.SourceRange = worksheet.Cells["C3:F7"];
				cacheDefinition.UpdateData();
				Assert.AreEqual(4, pivotTable.Fields.Count);
				var pivotField1 = pivotTable.Fields[0];
				Assert.AreEqual(0, pivotField1.Items.Count);
				var pivotField2 = pivotTable.Fields[1];
				Assert.AreEqual(5, pivotField2.Items.Count);
				var pivotField3 = pivotTable.Fields[2];
				Assert.AreEqual(4, pivotField3.Items.Count);
				this.CheckFieldItems(pivotField3);
				var pivotField4 = pivotTable.Fields[3];
				Assert.AreEqual(0, pivotField4.Items.Count);
				Assert.AreEqual(8, pivotTable.RowItems.Count);
				Assert.AreEqual("Black", worksheet.Cells[11, 9].Value);
				Assert.AreEqual(110d, worksheet.Cells[11, 10].Value);
				Assert.AreEqual("Bike", worksheet.Cells[12, 9].Value);
				Assert.AreEqual(100d, worksheet.Cells[12, 10].Value);
				Assert.AreEqual("Skateboard", worksheet.Cells[13, 9].Value);
				Assert.AreEqual(10d, worksheet.Cells[13, 10].Value);
				Assert.AreEqual("Red", worksheet.Cells[14, 9].Value);
				Assert.AreEqual(90000d, worksheet.Cells[14, 10].Value);
				Assert.AreEqual("Car", worksheet.Cells[15, 9].Value);
				Assert.AreEqual(90000d, worksheet.Cells[15, 10].Value);
				Assert.AreEqual("Purple", worksheet.Cells[16, 9].Value);
				Assert.AreEqual(28d, worksheet.Cells[16, 10].Value);
				Assert.AreEqual("Scooter", worksheet.Cells[17, 9].Value);
				Assert.AreEqual(28d, worksheet.Cells[17, 10].Value);
				Assert.AreEqual("Grand Total", worksheet.Cells[18, 9].Value);
				Assert.AreEqual(90138d, worksheet.Cells[18, 10].Value);
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableDataSourceTypeWorksheet.xlsx")]
		public void PivotTableRefreshFromCacheRemoveRow()
		{
			var file = new FileInfo("PivotTableDataSourceTypeWorksheet.xlsx");
			Assert.IsTrue(file.Exists);
			using (var package = new ExcelPackage(file))
			{
				var worksheet = package.Workbook.Worksheets.First();
				var pivotTable = worksheet.PivotTables.First();
				var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
				cacheDefinition.SourceRange = worksheet.Cells["C3:F5"];
				cacheDefinition.UpdateData();
				Assert.AreEqual(4, pivotTable.Fields.Count);
				var pivotField1 = pivotTable.Fields[0];
				Assert.AreEqual(0, pivotField1.Items.Count);
				var pivotField2 = pivotTable.Fields[1];
				Assert.AreEqual(4, pivotField2.Items.Count);
				var pivotField3 = pivotTable.Fields[2];
				Assert.AreEqual(3, pivotField3.Items.Count);
				this.CheckFieldItems(pivotField3);
				var pivotField4 = pivotTable.Fields[3];
				Assert.AreEqual(0, pivotField4.Items.Count);
				Assert.AreEqual(5, pivotTable.RowItems.Count);
				Assert.AreEqual("Black", worksheet.Cells[11, 9].Value);
				Assert.AreEqual(100d, worksheet.Cells[11, 10].Value);
				Assert.AreEqual("Bike", worksheet.Cells[12, 9].Value);
				Assert.AreEqual(100d, worksheet.Cells[12, 10].Value);
				Assert.AreEqual("Red", worksheet.Cells[13, 9].Value);
				Assert.AreEqual(90000d, worksheet.Cells[13, 10].Value);
				Assert.AreEqual("Car", worksheet.Cells[14, 9].Value);
				Assert.AreEqual(90000d, worksheet.Cells[14, 10].Value);
				Assert.AreEqual("Grand Total", worksheet.Cells[15, 9].Value);
				Assert.AreEqual(90100d, worksheet.Cells[15, 10].Value);
				Assert.IsNull(worksheet.Cells[16, 9].Value);
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshColumnItemsWithChangedData()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var package = new ExcelPackage(file))
			{
				var worksheet = package.Workbook.Worksheets.First();
				var pivotTable = worksheet.PivotTables.First();
				var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
				worksheet.Cells[4, 3].Value = "January";
				worksheet.Cells[7, 3].Value = "January";
				cacheDefinition.UpdateData();
				Assert.AreEqual(7, pivotTable.Fields.Count);
				Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
				Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
				Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
				Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
				Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
				Assert.AreEqual(4, pivotTable.Fields[5].Items.Count);
				Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
				foreach (var field in pivotTable.Fields)
				{
					if (field.Items.Count > 0)
						this.CheckFieldItems(field);
				}
				Assert.AreEqual("January", worksheet.Cells[13, 3].Value);
				Assert.AreEqual("Car Rack", worksheet.Cells[14, 3].Value);
				Assert.AreEqual("San Francisco", worksheet.Cells[15, 3].Value);
				Assert.AreEqual("Chicago", worksheet.Cells[15, 4].Value);
				Assert.AreEqual("Nashville", worksheet.Cells[15, 5].Value);
				Assert.AreEqual("Car Rack Total", worksheet.Cells[14, 6].Value);
				Assert.AreEqual("Headlamp", worksheet.Cells[14, 7].Value);
				Assert.AreEqual("Chicago", worksheet.Cells[15, 7].Value);
				Assert.AreEqual("Headlamp Total", worksheet.Cells[14, 8].Value);
				Assert.AreEqual("January Total", worksheet.Cells[13, 9].Value);
				Assert.AreEqual("February", worksheet.Cells[13, 10].Value);
				Assert.AreEqual("Sleeping Bag", worksheet.Cells[14, 10].Value);
				Assert.AreEqual("San Francisco", worksheet.Cells[15, 10].Value);
				Assert.AreEqual("Sleeping Bag Total", worksheet.Cells[14, 11].Value);
				Assert.AreEqual("Tent", worksheet.Cells[14, 12].Value);
				Assert.AreEqual("Nashville", worksheet.Cells[15, 12].Value);
				Assert.AreEqual("Tent Total", worksheet.Cells[14, 13].Value);
				Assert.AreEqual("February Total", worksheet.Cells[13, 14].Value);
				Assert.AreEqual("Grand Total", worksheet.Cells[13, 15].Value);
			}
		}
		
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshColumnItemsWithAddedData()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
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
				cacheDefinition.SourceRange = worksheet.Cells["A1:G9"];
				cacheDefinition.UpdateData();
				Assert.AreEqual(7, pivotTable.Fields.Count);
				Assert.AreEqual(9, pivotTable.Fields[0].Items.Count);
				Assert.AreEqual(5, pivotTable.Fields[1].Items.Count);
				Assert.AreEqual(5, pivotTable.Fields[2].Items.Count);
				Assert.AreEqual(6, pivotTable.Fields[3].Items.Count);
				Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
				Assert.AreEqual(4, pivotTable.Fields[5].Items.Count);
				Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
				foreach (var field in pivotTable.Fields)
				{
					if (field.Items.Count > 0)
						this.CheckFieldItems(field);
				}
				Assert.AreEqual("20100076", worksheet.Cells[16, 2].Value);
				Assert.AreEqual("20100085", worksheet.Cells[17, 2].Value);
				Assert.AreEqual("20100083", worksheet.Cells[18, 2].Value);
				Assert.AreEqual("20100007", worksheet.Cells[19, 2].Value);
				Assert.AreEqual("20100070", worksheet.Cells[20, 2].Value);
				Assert.AreEqual("20100017", worksheet.Cells[21, 2].Value);
				Assert.AreEqual("20100090", worksheet.Cells[22, 2].Value);
				Assert.AreEqual("20100091", worksheet.Cells[23, 2].Value);
				Assert.AreEqual("January", worksheet.Cells[13, 3].Value);
				Assert.AreEqual("Car Rack", worksheet.Cells[14, 3].Value);
				Assert.AreEqual("San Francisco", worksheet.Cells[15, 3].Value);
				Assert.AreEqual("Chicago", worksheet.Cells[15, 4].Value);
				Assert.AreEqual("Nashville", worksheet.Cells[15, 5].Value);
				Assert.AreEqual("Car Rack Total", worksheet.Cells[14, 6].Value);
				Assert.AreEqual("January Total", worksheet.Cells[13, 7].Value);
				Assert.AreEqual("February", worksheet.Cells[13, 8].Value);
				Assert.AreEqual("Sleeping Bag", worksheet.Cells[14, 8].Value);
				Assert.AreEqual("San Francisco", worksheet.Cells[15, 8].Value);
				Assert.AreEqual("Sleeping Bag Total", worksheet.Cells[14, 9].Value);
				Assert.AreEqual("Tent", worksheet.Cells[14, 10].Value);
				Assert.AreEqual("Nashville", worksheet.Cells[15, 10].Value);
				Assert.AreEqual("Tent Total", worksheet.Cells[14, 11].Value);
				Assert.AreEqual("February Total", worksheet.Cells[13, 12].Value);
				Assert.AreEqual("March", worksheet.Cells[13, 13].Value);
				Assert.AreEqual("Car Rack", worksheet.Cells[14, 13].Value);
				Assert.AreEqual("Nashville", worksheet.Cells[15, 13].Value);
				Assert.AreEqual("Car Rack Total", worksheet.Cells[14, 14].Value);
				Assert.AreEqual("Headlamp", worksheet.Cells[14, 15].Value);
				Assert.AreEqual("Chicago", worksheet.Cells[15, 15].Value);
				Assert.AreEqual("Headlamp Total", worksheet.Cells[14, 16].Value);
				Assert.AreEqual("March Total", worksheet.Cells[13, 17].Value);
				Assert.AreEqual("December", worksheet.Cells[13, 18].Value);
				Assert.AreEqual("Bike", worksheet.Cells[14, 18].Value);
				Assert.AreEqual("Texas", worksheet.Cells[15, 18].Value);
				Assert.AreEqual("Bike Total", worksheet.Cells[14, 19].Value);
				Assert.AreEqual("December Total", worksheet.Cells[13, 20].Value);
				Assert.AreEqual("Grand Total", worksheet.Cells[13, 21].Value);
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshColumnItemsWithRemoveData()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var package = new ExcelPackage(file))
			{
				var worksheet = package.Workbook.Worksheets.First();
				var pivotTable = worksheet.PivotTables["Sheet1PivotTable1"];
				var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
				cacheDefinition.SourceRange = worksheet.Cells["A1:G5"];
				cacheDefinition.UpdateData();
				Assert.AreEqual(7, pivotTable.Fields.Count);
				Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
				Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
				Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
				Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
				Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
				Assert.AreEqual(4, pivotTable.Fields[5].Items.Count);
				Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
				foreach (var field in pivotTable.Fields)
				{
					if (field.Items.Count > 0)
						this.CheckFieldItems(field);
				}
				Assert.AreEqual("January", worksheet.Cells[13, 3].Value);
				Assert.AreEqual("Car Rack", worksheet.Cells[14, 3].Value);
				Assert.AreEqual("San Francisco", worksheet.Cells[15, 3].Value);
				Assert.AreEqual("Chicago", worksheet.Cells[15, 4].Value);
				Assert.AreEqual("Car Rack Total", worksheet.Cells[14, 5].Value);
				Assert.AreEqual("January Total", worksheet.Cells[13, 6].Value);
				Assert.AreEqual("February", worksheet.Cells[13, 7].Value);
				Assert.AreEqual("Sleeping Bag", worksheet.Cells[14, 7].Value);
				Assert.AreEqual("San Francisco", worksheet.Cells[15, 7].Value);
				Assert.AreEqual("Sleeping Bag Total", worksheet.Cells[14, 8].Value);
				Assert.AreEqual("February Total", worksheet.Cells[13, 9].Value);
				Assert.AreEqual("March", worksheet.Cells[13, 10].Value);
				Assert.AreEqual("Headlamp", worksheet.Cells[14, 10].Value);
				Assert.AreEqual("Chicago", worksheet.Cells[15, 10].Value);
				Assert.AreEqual("Headlamp Total", worksheet.Cells[14, 11].Value);
				Assert.AreEqual("March Total", worksheet.Cells[13, 12].Value);
				Assert.AreEqual("Grand Total", worksheet.Cells[13, 13].Value);
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDeletingSourceRow()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets.First();
					var pivotTable = worksheet.PivotTables["Sheet1PivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					worksheet.DeleteRow(6);
					cacheDefinition.SourceRange = worksheet.Cells["A1:G7"];
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "Sheet1";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 15, 2, "20100076"),
					new ExpectedCellValue(sheetName, 16, 2, "20100085"),
					new ExpectedCellValue(sheetName, 17, 2, "20100083"),
					new ExpectedCellValue(sheetName, 18, 2, "20100007"),
					new ExpectedCellValue(sheetName, 19, 2, "20100017"),
					new ExpectedCellValue(sheetName, 20, 2, "20100090"),
					new ExpectedCellValue(sheetName, 12, 3, "January"),
					new ExpectedCellValue(sheetName, 13, 3, "Car Rack"),
					new ExpectedCellValue(sheetName, 14, 3, "San Francisco"),
					new ExpectedCellValue(sheetName, 14, 4, "Chicago"),
					new ExpectedCellValue(sheetName, 14, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 13, 6, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 12, 7, "January Total"),
					new ExpectedCellValue(sheetName, 12, 8, "February"),
					new ExpectedCellValue(sheetName, 13, 8, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 14, 8, "San Francisco"),
					new ExpectedCellValue(sheetName, 13, 9, "Sleeping Bag Total"),
					new ExpectedCellValue(sheetName, 12, 10, "February Total"),
					new ExpectedCellValue(sheetName, 12, 11, "March"),
					new ExpectedCellValue(sheetName, 13, 11, "Car Rack"),
					new ExpectedCellValue(sheetName, 14, 11, "Nashville"),
					new ExpectedCellValue(sheetName, 13, 12, "Car Rack Total"),
					new ExpectedCellValue(sheetName, 13, 13, "Headlamp"),
					new ExpectedCellValue(sheetName, 14, 13, "Chicago"),
					new ExpectedCellValue(sheetName, 13, 14, "Headlamp Total"),
					new ExpectedCellValue(sheetName, 12, 15, "March Total"),
					new ExpectedCellValue(sheetName, 12, 16, "Grand Total")
				});
			}
		}
		#endregion

		#region UpdateData Field Values Tests
		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldOneRowFieldWithTrueSubtotalTop()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowItems"];
					var pivotTable = worksheet.PivotTables["RowItemsPivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowItems";
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
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldTwoRowFieldsWithTrueSubtotalTop()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowItems"];
					var pivotTable = worksheet.PivotTables["RowItemsPivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowItems";
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
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldTwoRowFieldsWithFalseSubtotalTop()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowItems"];
					var pivotTable = worksheet.PivotTables["RowItemsPivotTable2"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowItems";
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
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldTwoRowFieldsWithNoSubtotal()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowItems"];
					var pivotTable = worksheet.PivotTables["RowItemsPivotTable4"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowItems";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 27, 1, "January"),
					new ExpectedCellValue(sheetName, 28, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 29, 1, "February"),
					new ExpectedCellValue(sheetName, 30, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 31, 1, "Tent"),
					new ExpectedCellValue(sheetName, 32, 1, "March"),
					new ExpectedCellValue(sheetName, 33, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 34, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 35, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 28, 2, 2078.75),
					new ExpectedCellValue(sheetName, 30, 2, 99d),
					new ExpectedCellValue(sheetName, 31, 2, 1194d),
					new ExpectedCellValue(sheetName, 33, 2, 831.5),
					new ExpectedCellValue(sheetName, 34, 2, 24.99),
					new ExpectedCellValue(sheetName, 35, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldThreeRowFieldsWithTrueSubtotalTop()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowItems"];
					var pivotTable = worksheet.PivotTables["RowItemsPivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowItems";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 12, "January"),
					new ExpectedCellValue(sheetName, 3, 12, "Car Rack"),
					new ExpectedCellValue(sheetName, 4, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 5, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 6, 12, "Nashville"),
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
					new ExpectedCellValue(sheetName, 4, 13, 415.75),
					new ExpectedCellValue(sheetName, 5, 13, 831.5),
					new ExpectedCellValue(sheetName, 6, 13, 831.5),
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
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldThreeRowFieldsWithFalseSubtotalTop()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowItems"];
					var pivotTable = worksheet.PivotTables["RowItemsPivotTable3"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowItems";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 12, "January"),
					new ExpectedCellValue(sheetName, 3, 12, "Car Rack"),
					new ExpectedCellValue(sheetName, 4, 12, "San Francisco"),
					new ExpectedCellValue(sheetName, 5, 12, "Chicago"),
					new ExpectedCellValue(sheetName, 6, 12, "Nashville"),
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
					new ExpectedCellValue(sheetName, 4, 13, 415.75),
					new ExpectedCellValue(sheetName, 5, 13, 831.5),
					new ExpectedCellValue(sheetName, 6, 13, 831.5),
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
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldsRowsAndColumnsWithNoSubtotal()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["NoSubtotals"];
					var pivotTable = worksheet.PivotTables["NoSubtotalsPivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(3, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "NoSubtotals";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "1"),
					new ExpectedCellValue(sheetName, 6, 1, "2"),
					new ExpectedCellValue(sheetName, 7, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 8, 1, "1"),
					new ExpectedCellValue(sheetName, 9, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 10, 1, "1"),
					new ExpectedCellValue(sheetName, 11, 1, "Tent"),
					new ExpectedCellValue(sheetName, 12, 1, "6"),
					new ExpectedCellValue(sheetName, 13, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 5, 2, 415.75),
					new ExpectedCellValue(sheetName, 13, 2, 415.75),
					new ExpectedCellValue(sheetName, 8, 3, 99d),
					new ExpectedCellValue(sheetName, 13, 3, 99d),
					new ExpectedCellValue(sheetName, 6, 4, 415.75),
					new ExpectedCellValue(sheetName, 13, 4, 415.75),
					new ExpectedCellValue(sheetName, 10, 5, 24.99),
					new ExpectedCellValue(sheetName, 13, 5, 24.99),
					new ExpectedCellValue(sheetName, 6, 6, 415.75),
					new ExpectedCellValue(sheetName, 13, 6, 415.75),
					new ExpectedCellValue(sheetName, 12, 7, 199d),
					new ExpectedCellValue(sheetName, 13, 7, 199d),
					new ExpectedCellValue(sheetName, 6, 8, 415.75),
					new ExpectedCellValue(sheetName, 13, 8, 415.75)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldsRowsAndColumnsGrandTotalOff()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["GrandTotals"];
					var pivotTable = worksheet.PivotTables["GrandTotalsPivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					pivotTable.RowGrandTotals = false;
					pivotTable.ColumnGrandTotals = false;
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "GrandTotals";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 6, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 7, 1, "Tent"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 3, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 2, 415.75),
					new ExpectedCellValue(sheetName, 3, 3, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 3, 831.5),
					new ExpectedCellValue(sheetName, 3, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 4, 831.5),
					new ExpectedCellValue(sheetName, 2, 5, "January Total"),
					new ExpectedCellValue(sheetName, 4, 5, 2078.75),
					new ExpectedCellValue(sheetName, 2, 6, "February"),
					new ExpectedCellValue(sheetName, 3, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 5, 6, 99d),
					new ExpectedCellValue(sheetName, 3, 7, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 7, 1194d),
					new ExpectedCellValue(sheetName, 2, 8, "February Total"),
					new ExpectedCellValue(sheetName, 5, 8, 99d),
					new ExpectedCellValue(sheetName, 7, 8, 1194d),
					new ExpectedCellValue(sheetName, 2, 9, "March"),
					new ExpectedCellValue(sheetName, 3, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 6, 9, 24.99),
					new ExpectedCellValue(sheetName, 3, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 10, 831.5),
					new ExpectedCellValue(sheetName, 2, 11, "March Total"),
					new ExpectedCellValue(sheetName, 4, 11, 831.5),
					new ExpectedCellValue(sheetName, 6, 11, 24.99)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldsColumnGrandTotalOff()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["GrandTotals"];
					var pivotTable = worksheet.PivotTables["GrandTotalsPivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					pivotTable.ColumnGrandTotals = false;
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "GrandTotals";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 6, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 7, 1, "Tent"),
					new ExpectedCellValue(sheetName, 8, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 3, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 2, 415.75),
					new ExpectedCellValue(sheetName, 8, 2, 415.75),
					new ExpectedCellValue(sheetName, 3, 3, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 3, 831.5),
					new ExpectedCellValue(sheetName, 8, 3, 831.5),
					new ExpectedCellValue(sheetName, 3, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 4, 831.5),
					new ExpectedCellValue(sheetName, 8, 4, 831.5),
					new ExpectedCellValue(sheetName, 2, 5, "January Total"),
					new ExpectedCellValue(sheetName, 4, 5, 2078.75),
					new ExpectedCellValue(sheetName, 8, 5, 2078.75),
					new ExpectedCellValue(sheetName, 2, 6, "February"),
					new ExpectedCellValue(sheetName, 3, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 5, 6, 99d),
					new ExpectedCellValue(sheetName, 8, 6, 99d),
					new ExpectedCellValue(sheetName, 3, 7, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 7, 1194d),
					new ExpectedCellValue(sheetName, 8, 7, 1194d),
					new ExpectedCellValue(sheetName, 2, 8, "February Total"),
					new ExpectedCellValue(sheetName, 5, 8, 99d),
					new ExpectedCellValue(sheetName, 7, 8, 1194d),
					new ExpectedCellValue(sheetName, 8, 8, 1293d),
					new ExpectedCellValue(sheetName, 2, 9, "March"),
					new ExpectedCellValue(sheetName, 3, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 6, 9, 24.99),
					new ExpectedCellValue(sheetName, 8, 9, 24.99),
					new ExpectedCellValue(sheetName, 3, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 10, 831.5),
					new ExpectedCellValue(sheetName, 8, 10, 831.5),
					new ExpectedCellValue(sheetName, 2, 11, "March Total"),
					new ExpectedCellValue(sheetName, 4, 11, 831.5),
					new ExpectedCellValue(sheetName, 6, 11, 24.99),
					new ExpectedCellValue(sheetName, 8, 11, 856.49)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshDataFieldsRowGrandTotalOff()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["GrandTotals"];
					var pivotTable = worksheet.PivotTables["GrandTotalsPivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					pivotTable.RowGrandTotals = false;
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "GrandTotals";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 4, 1, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 1, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 6, 1, "Headlamp"),
					new ExpectedCellValue(sheetName, 7, 1, "Tent"),
					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 3, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 2, 415.75),
					new ExpectedCellValue(sheetName, 3, 3, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 3, 831.5),
					new ExpectedCellValue(sheetName, 3, 4, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 4, 831.5),
					new ExpectedCellValue(sheetName, 2, 5, "January Total"),
					new ExpectedCellValue(sheetName, 4, 5, 2078.75),
					new ExpectedCellValue(sheetName, 2, 6, "February"),
					new ExpectedCellValue(sheetName, 3, 6, "San Francisco"),
					new ExpectedCellValue(sheetName, 5, 6, 99d),
					new ExpectedCellValue(sheetName, 3, 7, "Nashville"),
					new ExpectedCellValue(sheetName, 7, 7, 1194d),
					new ExpectedCellValue(sheetName, 2, 8, "February Total"),
					new ExpectedCellValue(sheetName, 5, 8, 99d),
					new ExpectedCellValue(sheetName, 7, 8, 1194d),
					new ExpectedCellValue(sheetName, 2, 9, "March"),
					new ExpectedCellValue(sheetName, 3, 9, "Chicago"),
					new ExpectedCellValue(sheetName, 6, 9, 24.99),
					new ExpectedCellValue(sheetName, 3, 10, "Nashville"),
					new ExpectedCellValue(sheetName, 4, 10, 831.5),
					new ExpectedCellValue(sheetName, 2, 11, "March Total"),
					new ExpectedCellValue(sheetName, 4, 11, 831.5),
					new ExpectedCellValue(sheetName, 6, 11, 24.99d),
					new ExpectedCellValue(sheetName, 2, 12, "Grand Total"),
					new ExpectedCellValue(sheetName, 4, 12, 2910.25),
					new ExpectedCellValue(sheetName, 5, 12, 99d),
					new ExpectedCellValue(sheetName, 6, 12, 24.99),
					new ExpectedCellValue(sheetName, 7, 12, 1194d)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleDataFields()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["MultipleDataFields"];
					var pivotTable = worksheet.PivotTables["MultipleDataFieldsPivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "MultipleDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 3, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 5, 1, "Grand Total"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 2, 2, 2),
					new ExpectedCellValue(sheetName, 3, 2, 3),
					new ExpectedCellValue(sheetName, 4, 2, 10),
					new ExpectedCellValue(sheetName, 5, 2, 15),
					new ExpectedCellValue(sheetName, 1, 3, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 3, 514.75),
					new ExpectedCellValue(sheetName, 3, 3, 856.49),
					new ExpectedCellValue(sheetName, 4, 3, 2857d),
					new ExpectedCellValue(sheetName, 5, 3, 4228.24),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleDataFieldsNoGrandTotal()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["MultipleDataFields"];
					var pivotTable = worksheet.PivotTables["MultipleDataFieldsPivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					pivotTable.RowGrandTotals = false;
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "MultipleDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 2, 1, "San Francisco"),
					new ExpectedCellValue(sheetName, 3, 1, "Chicago"),
					new ExpectedCellValue(sheetName, 4, 1, "Nashville"),
					new ExpectedCellValue(sheetName, 1, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 2, 2, 2),
					new ExpectedCellValue(sheetName, 3, 2, 3),
					new ExpectedCellValue(sheetName, 4, 2, 10),
					new ExpectedCellValue(sheetName, 1, 3, "Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 3, 514.75),
					new ExpectedCellValue(sheetName, 3, 3, 856.49),
					new ExpectedCellValue(sheetName, 4, 3, 2857d),
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleDataFieldsAndColumnHeaders()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["MultipleDataFields"];
					var pivotTable = worksheet.PivotTables["MultipleDataFieldsPivotTable2"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
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
					new ExpectedCellValue(sheetName, 12, 2, "San Francisco"),
					new ExpectedCellValue(sheetName, 13, 2, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 2, 1d),
					new ExpectedCellValue(sheetName, 17, 2, 1d),
					new ExpectedCellValue(sheetName, 13, 3, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 3, 415.75),
					new ExpectedCellValue(sheetName, 17, 3, 415.75),
					new ExpectedCellValue(sheetName, 12, 4, "Chicago"),
					new ExpectedCellValue(sheetName, 13, 4, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 4, 2d),
					new ExpectedCellValue(sheetName, 17, 4, 2d),
					new ExpectedCellValue(sheetName, 13, 5, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 5, 831.5),
					new ExpectedCellValue(sheetName, 17, 5, 831.5),
					new ExpectedCellValue(sheetName, 12, 6, "Nashville"),
					new ExpectedCellValue(sheetName, 13, 6, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 6, 2d),
					new ExpectedCellValue(sheetName, 16, 6, 2d),
					new ExpectedCellValue(sheetName, 17, 6, 4d),
					new ExpectedCellValue(sheetName, 13, 7, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 7, 831.5),
					new ExpectedCellValue(sheetName, 16, 7, 831.5),
					new ExpectedCellValue(sheetName, 17, 7, 1663d),
					new ExpectedCellValue(sheetName, 11, 8, "Car Rack Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 14, 8, 5d),
					new ExpectedCellValue(sheetName, 16, 8, 2d),
					new ExpectedCellValue(sheetName, 17, 8, 7d),
					new ExpectedCellValue(sheetName, 11, 9, "Car Rack Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 9, 2078.75),
					new ExpectedCellValue(sheetName, 16, 9, 831.5),
					new ExpectedCellValue(sheetName, 17, 9, 2910.25),
					new ExpectedCellValue(sheetName, 11, 10, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 12, 10, "San Francisco"),
					new ExpectedCellValue(sheetName, 13, 10, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 10, 1d),
					new ExpectedCellValue(sheetName, 17, 10, 1d),
					new ExpectedCellValue(sheetName, 13, 11, "Sum of Total"),
					new ExpectedCellValue(sheetName, 15, 11, 99d),
					new ExpectedCellValue(sheetName, 17, 11, 99d),
					new ExpectedCellValue(sheetName, 11, 12, "Sleeping Bag Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 12, 1d),
					new ExpectedCellValue(sheetName, 17, 12, 1d),
					new ExpectedCellValue(sheetName, 11, 13, "Sleeping Bag Sum of Total"),
					new ExpectedCellValue(sheetName, 15, 13, 99d),
					new ExpectedCellValue(sheetName, 17, 13, 99d),
					new ExpectedCellValue(sheetName, 11, 14, "Headlamp"),
					new ExpectedCellValue(sheetName, 12, 14, "Chicago"),
					new ExpectedCellValue(sheetName, 13, 14, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 14, 1d),
					new ExpectedCellValue(sheetName, 17, 14, 1d),
					new ExpectedCellValue(sheetName, 13, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 15, 24.99),
					new ExpectedCellValue(sheetName, 17, 15, 24.99),
					new ExpectedCellValue(sheetName, 11, 16, "Headlamp Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 16, 1d),
					new ExpectedCellValue(sheetName, 17, 16, 1d),
					new ExpectedCellValue(sheetName, 11, 17, "Headlamp Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 17, 24.99),
					new ExpectedCellValue(sheetName, 17, 17, 24.99),
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
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsOneRowAndOneColumn()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["RowDataFieldsPivotTable1"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
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

					new ExpectedCellValue(sheetName, 2, 2, "January"),
					new ExpectedCellValue(sheetName, 4, 2, 1d),
					new ExpectedCellValue(sheetName, 5, 2, 415.75),
					new ExpectedCellValue(sheetName, 7, 2, 2d),
					new ExpectedCellValue(sheetName, 8, 2, 831.5),
					new ExpectedCellValue(sheetName, 10, 2, 2d),
					new ExpectedCellValue(sheetName, 11, 2, 831.5),
					new ExpectedCellValue(sheetName, 12, 2, 5d),
					new ExpectedCellValue(sheetName, 13, 2, 2078.75),

					new ExpectedCellValue(sheetName, 2, 3, "February"),
					new ExpectedCellValue(sheetName, 4, 3, 1d),
					new ExpectedCellValue(sheetName, 5, 3, 99d),
					new ExpectedCellValue(sheetName, 10, 3, 6d),
					new ExpectedCellValue(sheetName, 11, 3, 1194d),
					new ExpectedCellValue(sheetName, 12, 3, 7d),
					new ExpectedCellValue(sheetName, 13, 3, 1293d),

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
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsTwoRowsAndOneColumnSubtotalsOff()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["RowDataFieldsPivotTable2"];
					foreach (var field in pivotTable.Fields)
					{
						field.DefaultSubtotal = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 8, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 6, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 7, 8, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 8, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 9, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 10, 8, "Chicago"),
					new ExpectedCellValue(sheetName, 11, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 12, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 8, "Headlamp"),
					new ExpectedCellValue(sheetName, 15, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 17, 8, "Nashville"),
					new ExpectedCellValue(sheetName, 18, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 19, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 21, 8, "Tent"),
					new ExpectedCellValue(sheetName, 22, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 23, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 24, 8, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 25, 8, "Total Sum of Total"),

					new ExpectedCellValue(sheetName, 2, 9, "January"),
					new ExpectedCellValue(sheetName, 5, 9, 1d),
					new ExpectedCellValue(sheetName, 6, 9, 415.75),
					new ExpectedCellValue(sheetName, 12, 9, 2d),
					new ExpectedCellValue(sheetName, 13, 9, 831.5),
					new ExpectedCellValue(sheetName, 19, 9, 2d),
					new ExpectedCellValue(sheetName, 20, 9, 831.5),
					new ExpectedCellValue(sheetName, 24, 9, 5d),
					new ExpectedCellValue(sheetName, 25, 9, 2078.75),

					new ExpectedCellValue(sheetName, 2, 10, "February"),
					new ExpectedCellValue(sheetName, 8, 10, 1d),
					new ExpectedCellValue(sheetName, 9, 10, 99d),
					new ExpectedCellValue(sheetName, 22, 10, 6d),
					new ExpectedCellValue(sheetName, 23, 10, 1194d),
					new ExpectedCellValue(sheetName, 24, 10, 7d),
					new ExpectedCellValue(sheetName, 25, 10, 1293d),

					new ExpectedCellValue(sheetName, 2, 11, "March"),
					new ExpectedCellValue(sheetName, 15, 11, 1d),
					new ExpectedCellValue(sheetName, 16, 11, 24.99),
					new ExpectedCellValue(sheetName, 19, 11, 2d),
					new ExpectedCellValue(sheetName, 20, 11, 831.5),
					new ExpectedCellValue(sheetName, 24, 11, 3d),
					new ExpectedCellValue(sheetName, 25, 11, 856.49),

					new ExpectedCellValue(sheetName, 2, 12, "Grand Total"),
					new ExpectedCellValue(sheetName, 5, 12, 1d),
					new ExpectedCellValue(sheetName, 6, 12, 415.75),
					new ExpectedCellValue(sheetName, 8, 12, 1d),
					new ExpectedCellValue(sheetName, 9, 12, 99d),
					new ExpectedCellValue(sheetName, 12, 12, 2d),
					new ExpectedCellValue(sheetName, 13, 12, 831.5),
					new ExpectedCellValue(sheetName, 15, 12, 1d),
					new ExpectedCellValue(sheetName, 16, 12, 24.99),
					new ExpectedCellValue(sheetName, 19, 12, 4d),
					new ExpectedCellValue(sheetName, 20, 12, 1663d),
					new ExpectedCellValue(sheetName, 22, 12, 6d),
					new ExpectedCellValue(sheetName, 23, 12, 1194d),
					new ExpectedCellValue(sheetName, 24, 12, 15d),
					new ExpectedCellValue(sheetName, 25, 12, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsTwoRowsAndOneColumnSubtotalsOn()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["RowDataFieldsPivotTable2"];
					foreach (var field in pivotTable.Fields)
					{
						field.SubtotalTop = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 8, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 6, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 7, 8, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 8, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 9, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 10, 8, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 11, 8, "San Francisco Sum of Total"),
					new ExpectedCellValue(sheetName, 12, 8, "Chicago"),
					new ExpectedCellValue(sheetName, 13, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 14, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 8, "Headlamp"),
					new ExpectedCellValue(sheetName, 17, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 18, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 19, 8, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 8, "Chicago Sum of Total"),
					new ExpectedCellValue(sheetName, 21, 8, "Nashville"),
					new ExpectedCellValue(sheetName, 22, 8, "Car Rack"),
					new ExpectedCellValue(sheetName, 23, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 24, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 25, 8, "Tent"),
					new ExpectedCellValue(sheetName, 26, 8, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 27, 8, "Sum of Total"),
					new ExpectedCellValue(sheetName, 28, 8, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 29, 8, "Nashville Sum of Total"),
					new ExpectedCellValue(sheetName, 30, 8, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 31, 8, "Total Sum of Total"),
					new ExpectedCellValue(sheetName, 2, 9, "January"),
					new ExpectedCellValue(sheetName, 5, 9, 1d),
					new ExpectedCellValue(sheetName, 6, 9, 415.75),
					new ExpectedCellValue(sheetName, 10, 9, 1d),
					new ExpectedCellValue(sheetName, 11, 9, 415.75),
					new ExpectedCellValue(sheetName, 14, 9, 2d),
					new ExpectedCellValue(sheetName, 15, 9, 831.5),
					new ExpectedCellValue(sheetName, 19, 9, 2d),
					new ExpectedCellValue(sheetName, 20, 9, 831.5),
					new ExpectedCellValue(sheetName, 23, 9, 2d),
					new ExpectedCellValue(sheetName, 24, 9, 831.5),
					new ExpectedCellValue(sheetName, 28, 9, 2d),
					new ExpectedCellValue(sheetName, 29, 9, 831.5),
					new ExpectedCellValue(sheetName, 30, 9, 5d),
					new ExpectedCellValue(sheetName, 31, 9, 2078.75),
					new ExpectedCellValue(sheetName, 2, 10, "February"),
					new ExpectedCellValue(sheetName, 8, 10, 1d),
					new ExpectedCellValue(sheetName, 9, 10, 99d),
					new ExpectedCellValue(sheetName, 10, 10, 1d),
					new ExpectedCellValue(sheetName, 11, 10, 99d),
					new ExpectedCellValue(sheetName, 26, 10, 6d),
					new ExpectedCellValue(sheetName, 27, 10, 1194d),
					new ExpectedCellValue(sheetName, 28, 10, 6d),
					new ExpectedCellValue(sheetName, 29, 10, 1194d),
					new ExpectedCellValue(sheetName, 30, 10, 7d),
					new ExpectedCellValue(sheetName, 31, 10, 1293d),
					new ExpectedCellValue(sheetName, 2, 11, "March"),
					new ExpectedCellValue(sheetName, 17, 11, 1d),
					new ExpectedCellValue(sheetName, 18, 11, 24.99),
					new ExpectedCellValue(sheetName, 19, 11, 1d),
					new ExpectedCellValue(sheetName, 20, 11, 24.99),
					new ExpectedCellValue(sheetName, 23, 11, 2d),
					new ExpectedCellValue(sheetName, 24, 11, 831.5),
					new ExpectedCellValue(sheetName, 28, 11, 2d),
					new ExpectedCellValue(sheetName, 29, 11, 831.5),
					new ExpectedCellValue(sheetName, 30, 11, 3d),
					new ExpectedCellValue(sheetName, 31, 11, 856.49),
					new ExpectedCellValue(sheetName, 2, 12, "Grand Total"),
					new ExpectedCellValue(sheetName, 5, 12, 1d),
					new ExpectedCellValue(sheetName, 6, 12, 415.75),
					new ExpectedCellValue(sheetName, 8, 12, 1d),
					new ExpectedCellValue(sheetName, 9, 12, 99d),
					new ExpectedCellValue(sheetName, 10, 12, 2d),
					new ExpectedCellValue(sheetName, 11, 12, 514.75),
					new ExpectedCellValue(sheetName, 14, 12, 2d),
					new ExpectedCellValue(sheetName, 15, 12, 831.5),
					new ExpectedCellValue(sheetName, 17, 12, 1d),
					new ExpectedCellValue(sheetName, 18, 12, 24.99),
					new ExpectedCellValue(sheetName, 19, 12, 3d),
					new ExpectedCellValue(sheetName, 20, 12, 856.49),
					new ExpectedCellValue(sheetName, 23, 12, 4d),
					new ExpectedCellValue(sheetName, 24, 12, 1663d),
					new ExpectedCellValue(sheetName, 26, 12, 6d),
					new ExpectedCellValue(sheetName, 27, 12, 1194d),
					new ExpectedCellValue(sheetName, 28, 12, 10d),
					new ExpectedCellValue(sheetName, 29, 12, 2857d),
					new ExpectedCellValue(sheetName, 30, 12, 15d),
					new ExpectedCellValue(sheetName, 31, 12, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsThreeRowsAndOneColumnSubtotalsOff()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["RowDataFieldsPivotTable3"];
					foreach (var field in pivotTable.Fields)
					{
						field.DefaultSubtotal = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 15, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 15, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 15, "20100076"),
					new ExpectedCellValue(sheetName, 6, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 7, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 8, 15, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 9, 15, "20100085"),
					new ExpectedCellValue(sheetName, 10, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 11, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 12, 15, "Chicago"),
					new ExpectedCellValue(sheetName, 13, 15, "Car Rack"),
					new ExpectedCellValue(sheetName, 14, 15, "20100007"),
					new ExpectedCellValue(sheetName, 15, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 16, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 17, 15, "Headlamp"),
					new ExpectedCellValue(sheetName, 18, 15, "20100083"),
					new ExpectedCellValue(sheetName, 19, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 20, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 21, 15, "Nashville"),
					new ExpectedCellValue(sheetName, 22, 15, "Car Rack"),
					new ExpectedCellValue(sheetName, 23, 15, "20100017"),
					new ExpectedCellValue(sheetName, 24, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 25, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 26, 15, "20100090"),
					new ExpectedCellValue(sheetName, 27, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 28, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 29, 15, "Tent"),
					new ExpectedCellValue(sheetName, 30, 15, "20100070"),
					new ExpectedCellValue(sheetName, 31, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 32, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 15, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 34, 15, "Total Sum of Total"),

					new ExpectedCellValue(sheetName, 2, 16, "January"),
					new ExpectedCellValue(sheetName, 6, 16, 1d),
					new ExpectedCellValue(sheetName, 7, 16, 415.75),
					new ExpectedCellValue(sheetName, 15, 16, 2d),
					new ExpectedCellValue(sheetName, 16, 16, 831.5),
					new ExpectedCellValue(sheetName, 27, 16, 2d),
					new ExpectedCellValue(sheetName, 28, 16, 831.5),
					new ExpectedCellValue(sheetName, 33, 16, 5d),
					new ExpectedCellValue(sheetName, 34, 16, 2078.75),

					new ExpectedCellValue(sheetName, 2, 17, "February"),
					new ExpectedCellValue(sheetName, 10, 17, 1d),
					new ExpectedCellValue(sheetName, 11, 17, 99d),
					new ExpectedCellValue(sheetName, 31, 17, 6d),
					new ExpectedCellValue(sheetName, 32, 17, 1194d),
					new ExpectedCellValue(sheetName, 33, 17, 7d),
					new ExpectedCellValue(sheetName, 34, 17, 1293d),

					new ExpectedCellValue(sheetName, 2, 18, "March"),
					new ExpectedCellValue(sheetName, 19, 18, 1d),
					new ExpectedCellValue(sheetName, 20, 18, 24.99),
					new ExpectedCellValue(sheetName, 24, 18, 2d),
					new ExpectedCellValue(sheetName, 25, 18, 831.5),
					new ExpectedCellValue(sheetName, 33, 18, 3d),
					new ExpectedCellValue(sheetName, 34, 18, 856.49),

					new ExpectedCellValue(sheetName, 2, 19, "Grand Total"),
					new ExpectedCellValue(sheetName, 6, 19, 1d),
					new ExpectedCellValue(sheetName, 7, 19, 415.75),
					new ExpectedCellValue(sheetName, 10, 19, 1d),
					new ExpectedCellValue(sheetName, 11, 19, 99d),
					new ExpectedCellValue(sheetName, 15, 19, 2d),
					new ExpectedCellValue(sheetName, 16, 19, 831.5),
					new ExpectedCellValue(sheetName, 19, 19, 1d),
					new ExpectedCellValue(sheetName, 20, 19, 24.99),
					new ExpectedCellValue(sheetName, 24, 19, 2d),
					new ExpectedCellValue(sheetName, 25, 19, 831.5),
					new ExpectedCellValue(sheetName, 27, 19, 2d),
					new ExpectedCellValue(sheetName, 28, 19, 831.5),
					new ExpectedCellValue(sheetName, 31, 19, 6d),
					new ExpectedCellValue(sheetName, 32, 19, 1194d),
					new ExpectedCellValue(sheetName, 33, 19, 15d),
					new ExpectedCellValue(sheetName, 34, 19, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsThreeRowsAndOneColumnSubtotalsOn()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["RowDataFieldsPivotTable3"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(8, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(5, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 3, 15, "San Francisco"),
					new ExpectedCellValue(sheetName, 4, 15, "Car Rack"),
					new ExpectedCellValue(sheetName, 5, 15, "20100076"),
					new ExpectedCellValue(sheetName, 6, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 7, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 8, 15, "Car Rack Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 9, 15, "Car Rack Sum of Total"),
					new ExpectedCellValue(sheetName, 10, 15, "Sleeping Bag"),
					new ExpectedCellValue(sheetName, 11, 15, "20100085"),
					new ExpectedCellValue(sheetName, 12, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 13, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 14, 15, "Sleeping Bag Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 15, 15, "Sleeping Bag Sum of Total"),
					new ExpectedCellValue(sheetName, 16, 15, "San Francisco Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 17, 15, "San Francisco Sum of Total"),
					new ExpectedCellValue(sheetName, 18, 15, "Chicago"),
					new ExpectedCellValue(sheetName, 19, 15, "Car Rack"),
					new ExpectedCellValue(sheetName, 20, 15, "20100007"),
					new ExpectedCellValue(sheetName, 21, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 22, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 23, 15, "Car Rack Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 24, 15, "Car Rack Sum of Total"),
					new ExpectedCellValue(sheetName, 25, 15, "Headlamp"),
					new ExpectedCellValue(sheetName, 26, 15, "20100083"),
					new ExpectedCellValue(sheetName, 27, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 28, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 29, 15, "Headlamp Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 30, 15, "Headlamp Sum of Total"),
					new ExpectedCellValue(sheetName, 31, 15, "Chicago Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 32, 15, "Chicago Sum of Total"),
					new ExpectedCellValue(sheetName, 33, 15, "Nashville"),
					new ExpectedCellValue(sheetName, 34, 15, "Car Rack"),
					new ExpectedCellValue(sheetName, 35, 15, "20100017"),
					new ExpectedCellValue(sheetName, 36, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 37, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 38, 15, "20100090"),
					new ExpectedCellValue(sheetName, 39, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 40, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 41, 15, "Car Rack Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 42, 15, "Car Rack Sum of Total"),
					new ExpectedCellValue(sheetName, 43, 15, "Tent"),
					new ExpectedCellValue(sheetName, 44, 15, "20100070"),
					new ExpectedCellValue(sheetName, 45, 15, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 46, 15, "Sum of Total"),
					new ExpectedCellValue(sheetName, 47, 15, "Tent Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 48, 15, "Tent Sum of Total"),
					new ExpectedCellValue(sheetName, 49, 15, "Nashville Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 50, 15, "Nashville Sum of Total"),
					new ExpectedCellValue(sheetName, 51, 15, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 52, 15, "Total Sum of Total"),

					new ExpectedCellValue(sheetName, 2, 16, "January"),
					new ExpectedCellValue(sheetName, 6, 16, 1d),
					new ExpectedCellValue(sheetName, 7, 16, 415.75),
					new ExpectedCellValue(sheetName, 8, 16, 1d),
					new ExpectedCellValue(sheetName, 9, 16, 415.75),
					new ExpectedCellValue(sheetName, 16, 16, 1d),
					new ExpectedCellValue(sheetName, 17, 16, 415.75),
					new ExpectedCellValue(sheetName, 21, 16, 2d),
					new ExpectedCellValue(sheetName, 22, 16, 831.5),
					new ExpectedCellValue(sheetName, 23, 16, 2d),
					new ExpectedCellValue(sheetName, 24, 16, 831.5),
					new ExpectedCellValue(sheetName, 31, 16, 2d),
					new ExpectedCellValue(sheetName, 32, 16, 831.5),
					new ExpectedCellValue(sheetName, 39, 16, 2d),
					new ExpectedCellValue(sheetName, 40, 16, 831.5),
					new ExpectedCellValue(sheetName, 41, 16, 2d),
					new ExpectedCellValue(sheetName, 42, 16, 831.5),
					new ExpectedCellValue(sheetName, 49, 16, 2d),
					new ExpectedCellValue(sheetName, 50, 16, 831.5),
					new ExpectedCellValue(sheetName, 51, 16, 5d),
					new ExpectedCellValue(sheetName, 52, 16, 2078.75),

					new ExpectedCellValue(sheetName, 2, 17, "February"),
					new ExpectedCellValue(sheetName, 12, 17, 1d),
					new ExpectedCellValue(sheetName, 13, 17, 99d),
					new ExpectedCellValue(sheetName, 14, 17, 1d),
					new ExpectedCellValue(sheetName, 15, 17, 99d),
					new ExpectedCellValue(sheetName, 16, 17, 1d),
					new ExpectedCellValue(sheetName, 17, 17, 99d),
					new ExpectedCellValue(sheetName, 45, 17, 6d),
					new ExpectedCellValue(sheetName, 46, 17, 1194d),
					new ExpectedCellValue(sheetName, 47, 17, 6d),
					new ExpectedCellValue(sheetName, 48, 17, 1194d),
					new ExpectedCellValue(sheetName, 49, 17, 6d),
					new ExpectedCellValue(sheetName, 50, 17, 1194d),
					new ExpectedCellValue(sheetName, 51, 17, 7d),
					new ExpectedCellValue(sheetName, 52, 17, 1293d),

					new ExpectedCellValue(sheetName, 2, 18, "March"),
					new ExpectedCellValue(sheetName, 27, 18, 1d),
					new ExpectedCellValue(sheetName, 28, 18, 24.99),
					new ExpectedCellValue(sheetName, 29, 18, 1d),
					new ExpectedCellValue(sheetName, 30, 18, 24.99),
					new ExpectedCellValue(sheetName, 31, 18, 1d),
					new ExpectedCellValue(sheetName, 32, 18, 24.99),
					new ExpectedCellValue(sheetName, 36, 18, 2d),
					new ExpectedCellValue(sheetName, 37, 18, 831.5),
					new ExpectedCellValue(sheetName, 41, 18, 2d),
					new ExpectedCellValue(sheetName, 42, 18, 831.5),
					new ExpectedCellValue(sheetName, 49, 18, 2d),
					new ExpectedCellValue(sheetName, 50, 18, 831.5),
					new ExpectedCellValue(sheetName, 51, 18, 3d),
					new ExpectedCellValue(sheetName, 52, 18, 856.49),

					new ExpectedCellValue(sheetName, 2, 19, "Grand Total"),
					new ExpectedCellValue(sheetName, 6, 19, 1d),
					new ExpectedCellValue(sheetName, 7, 19, 415.75),
					new ExpectedCellValue(sheetName, 8, 19, 1d),
					new ExpectedCellValue(sheetName, 9, 19, 415.75),
					new ExpectedCellValue(sheetName, 12, 19, 1d),
					new ExpectedCellValue(sheetName, 13, 19, 99d),
					new ExpectedCellValue(sheetName, 14, 19, 1d),
					new ExpectedCellValue(sheetName, 15, 19, 99d),
					new ExpectedCellValue(sheetName, 16, 19, 2d),
					new ExpectedCellValue(sheetName, 17, 19, 514.75),
					new ExpectedCellValue(sheetName, 21, 19, 2d),
					new ExpectedCellValue(sheetName, 22, 19, 831.5),
					new ExpectedCellValue(sheetName, 23, 19, 2d),
					new ExpectedCellValue(sheetName, 24, 19, 831.5),
					new ExpectedCellValue(sheetName, 27, 19, 1d),
					new ExpectedCellValue(sheetName, 28, 19, 24.99),
					new ExpectedCellValue(sheetName, 29, 19, 1d),
					new ExpectedCellValue(sheetName, 30, 19, 24.99),
					new ExpectedCellValue(sheetName, 31, 19, 3d),
					new ExpectedCellValue(sheetName, 32, 19, 856.49),
					new ExpectedCellValue(sheetName, 36, 19, 2d),
					new ExpectedCellValue(sheetName, 37, 19, 831.5),
					new ExpectedCellValue(sheetName, 39, 19, 2d),
					new ExpectedCellValue(sheetName, 40, 19, 831.5),
					new ExpectedCellValue(sheetName, 41, 19, 4d),
					new ExpectedCellValue(sheetName, 42, 19, 1663),
					new ExpectedCellValue(sheetName, 45, 19, 6d),
					new ExpectedCellValue(sheetName, 46, 19, 1194d),
					new ExpectedCellValue(sheetName, 47, 19, 6d),
					new ExpectedCellValue(sheetName, 48, 19, 1194d),
					new ExpectedCellValue(sheetName, 49, 19, 10d),
					new ExpectedCellValue(sheetName, 50, 19, 2857d),
					new ExpectedCellValue(sheetName, 51, 19, 15d),
					new ExpectedCellValue(sheetName, 52, 19, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsOneRowAndNoColumns()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["RowDataFieldsPivotTable4"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 38, 1, "January"),
					new ExpectedCellValue(sheetName, 39, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 40, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 41, 1, "February"),
					new ExpectedCellValue(sheetName, 42, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 43, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 44, 1, "March"),
					new ExpectedCellValue(sheetName, 45, 1, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 46, 1, "Sum of Total"),
					new ExpectedCellValue(sheetName, 47, 1, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 48, 1, "Total Sum of Total"),

					new ExpectedCellValue(sheetName, 39, 2, 5d),
					new ExpectedCellValue(sheetName, 40, 2, 2078.75),
					new ExpectedCellValue(sheetName, 42, 2, 7d),
					new ExpectedCellValue(sheetName, 43, 2, 1293d),
					new ExpectedCellValue(sheetName, 45, 2, 3d),
					new ExpectedCellValue(sheetName, 46, 2, 856.49),
					new ExpectedCellValue(sheetName, 47, 2, 15d),
					new ExpectedCellValue(sheetName, 48, 2, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsTwoRowsAndNoColumnsSubtotalsOff()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["RowDataFieldsPivotTable5"];
					foreach (var field in pivotTable.Fields)
					{
						field.DefaultSubtotal = false;
					}
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 38, 5, "January"),
					new ExpectedCellValue(sheetName, 39, 5, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 40, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 41, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 42, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 43, 5, "Sum of Total"),
					new ExpectedCellValue(sheetName, 44, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 45, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 46, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 47, 5, "February"),
					new ExpectedCellValue(sheetName, 48, 5, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 49, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 50, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 51, 5, "Sum of Total"),
					new ExpectedCellValue(sheetName, 52, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 53, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 54, 5, "March"),
					new ExpectedCellValue(sheetName, 55, 5, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 56, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 57, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 58, 5, "Sum of Total"),
					new ExpectedCellValue(sheetName, 59, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 60, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 61, 5, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 62, 5, "Total Sum of Total"),

					new ExpectedCellValue(sheetName, 40, 6, 1d),
					new ExpectedCellValue(sheetName, 41, 6, 2d),
					new ExpectedCellValue(sheetName, 42, 6, 2d),
					new ExpectedCellValue(sheetName, 44, 6, 415.75),
					new ExpectedCellValue(sheetName, 45, 6, 831.5),
					new ExpectedCellValue(sheetName, 46, 6, 831.5),
					new ExpectedCellValue(sheetName, 49, 6, 1d),
					new ExpectedCellValue(sheetName, 50, 6, 6d),
					new ExpectedCellValue(sheetName, 52, 6, 99d),
					new ExpectedCellValue(sheetName, 53, 6, 1194d),
					new ExpectedCellValue(sheetName, 56, 6, 1d),
					new ExpectedCellValue(sheetName, 57, 6, 2d),
					new ExpectedCellValue(sheetName, 59, 6, 24.99),
					new ExpectedCellValue(sheetName, 60, 6, 831.5),
					new ExpectedCellValue(sheetName, 61, 6, 15d),
					new ExpectedCellValue(sheetName, 62, 6, 4228.24)
				});
			}
		}

		[TestMethod]
		[DeploymentItem(@"..\..\Workbooks\PivotTableColumnFields.xlsx")]
		public void PivotTableRefreshMultipleRowDataFieldsTwoRowsAndNoColumnsSubtotalsOn()
		{
			var file = new FileInfo("PivotTableColumnFields.xlsx");
			Assert.IsTrue(file.Exists);
			using (var newFile = new TempTestFile())
			{
				using (var package = new ExcelPackage(file))
				{
					var worksheet = package.Workbook.Worksheets["RowDataFields"];
					var pivotTable = worksheet.PivotTables["RowDataFieldsPivotTable5"];
					var cacheDefinition = package.Workbook.PivotCacheDefinitions.Single();
					cacheDefinition.UpdateData();
					Assert.AreEqual(7, pivotTable.Fields.Count);
					Assert.AreEqual(0, pivotTable.Fields[0].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[1].Items.Count);
					Assert.AreEqual(4, pivotTable.Fields[2].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[3].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[4].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[5].Items.Count);
					Assert.AreEqual(0, pivotTable.Fields[6].Items.Count);
					foreach (var field in pivotTable.Fields)
					{
						if (field.Items.Count > 0)
							this.CheckFieldItems(field);
					}
					package.SaveAs(newFile.File);
				}
				string sheetName = "RowDataFields";
				TestHelperUtility.ValidateWorksheet(newFile.File, sheetName, new[]
				{
					new ExpectedCellValue(sheetName, 38, 5, "January"),
					new ExpectedCellValue(sheetName, 39, 5, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 40, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 41, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 42, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 43, 5, "Sum of Total"),
					new ExpectedCellValue(sheetName, 44, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 45, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 46, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 47, 5, "January Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 48, 5, "January Sum of Total"),
					new ExpectedCellValue(sheetName, 49, 5, "February"),
					new ExpectedCellValue(sheetName, 50, 5, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 51, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 52, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 53, 5, "Sum of Total"),
					new ExpectedCellValue(sheetName, 54, 5, "San Francisco"),
					new ExpectedCellValue(sheetName, 55, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 56, 5, "February Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 57, 5, "February Sum of Total"),
					new ExpectedCellValue(sheetName, 58, 5, "March"),
					new ExpectedCellValue(sheetName, 59, 5, "Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 60, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 61, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 62, 5, "Sum of Total"),
					new ExpectedCellValue(sheetName, 63, 5, "Chicago"),
					new ExpectedCellValue(sheetName, 64, 5, "Nashville"),
					new ExpectedCellValue(sheetName, 65, 5, "March Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 66, 5, "March Sum of Total"),
					new ExpectedCellValue(sheetName, 67, 5, "Total Sum of Units Sold"),
					new ExpectedCellValue(sheetName, 68, 5, "Total Sum of Total"),

					new ExpectedCellValue(sheetName, 40, 6, 1d),
					new ExpectedCellValue(sheetName, 41, 6, 2d),
					new ExpectedCellValue(sheetName, 42, 6, 2d),
					new ExpectedCellValue(sheetName, 44, 6, 415.75),
					new ExpectedCellValue(sheetName, 45, 6, 831.5),
					new ExpectedCellValue(sheetName, 46, 6, 831.5),
					new ExpectedCellValue(sheetName, 47, 6, 5d),
					new ExpectedCellValue(sheetName, 48, 6, 2078.75),
					new ExpectedCellValue(sheetName, 51, 6, 1d),
					new ExpectedCellValue(sheetName, 52, 6, 6d),
					new ExpectedCellValue(sheetName, 54, 6, 99d),
					new ExpectedCellValue(sheetName, 55, 6, 1194d),
					new ExpectedCellValue(sheetName, 56, 6, 7d),
					new ExpectedCellValue(sheetName, 57, 6, 1293d),
					new ExpectedCellValue(sheetName, 60, 6, 1d),
					new ExpectedCellValue(sheetName, 61, 6, 2d),
					new ExpectedCellValue(sheetName, 63, 6, 24.99),
					new ExpectedCellValue(sheetName, 64, 6, 831.5),
					new ExpectedCellValue(sheetName, 65, 6, 3d),
					new ExpectedCellValue(sheetName, 66, 6, 856.49),
					new ExpectedCellValue(sheetName, 67, 6, 15d),
					new ExpectedCellValue(sheetName, 68, 6, 4228.24)
				});
			}
		}
		#endregion
		
		#endregion

		#region Helper Methods
		private void CheckFieldItems(ExcelPivotTableField field)
		{
			int i = 0;
			for (; i < field.Items.Count - 1; i++)
			{
				Assert.AreEqual(i, field.Items[i].X);
			}
			var lastItem = field.Items[field.Items.Count - 1];
			if (string.IsNullOrEmpty(lastItem.T))
				Assert.AreEqual(i, lastItem.X);
		}
		#endregion
	}
}