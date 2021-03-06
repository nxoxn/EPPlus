﻿using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;

namespace EPPlusTest.DataValidation.IntegrationTests
{
	/// <summary>
	/// Remove the Ignore attributes from the testmethods if you want to run any of these tests
	/// </summary>
	[TestClass]
	public class IntegrationTests : ValidationTestBase
	{
		[TestInitialize]
		public void Setup()
		{
			SetupTestData();
		}

		[TestCleanup]
		public void Cleanup()
		{
			CleanupTestData();
		}

		[TestMethod, Ignore]
		public void DataValidations_AddOneValidationOfTypeWhole()
		{
			_sheet.Cells["B1"].Value = 2;
			var validation = _sheet.DataValidations.AddIntegerValidation("A1");
			validation.ShowErrorMessage = true;
			validation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
			validation.ErrorTitle = "Invalid value was entered";
			validation.PromptTitle = "Enter value here";
			validation.Operator = ExcelDataValidationOperator.greaterThan;
			//validation.Value.Value = 3;
			validation.Formula.ExcelFormula = "B1";

			_package.SaveAs(new FileInfo(GetTestOutputPath("AddOneValidationOfTypeWhole.xlsx")));
		}
		[TestMethod, Ignore]
		public void DataValidations_AddOneValidationOfTypeDecimal()
		{
			var validation = _sheet.DataValidations.AddDecimalValidation("A1");
			validation.ShowErrorMessage = true;
			validation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
			validation.ErrorTitle = "Invalid value was entered";
			validation.Error = "Value must be greater than 1.4";
			validation.PromptTitle = "Enter value here";
			validation.Prompt = "Enter a value that is greater than 1.4";
			validation.ShowInputMessage = true;
			validation.Operator = ExcelDataValidationOperator.greaterThan;
			validation.Formula.Value = 1.4;

			_package.SaveAs(new FileInfo(GetTestOutputPath("AddOneValidationOfTypeDecimal.xlsx")));
		}

		[TestMethod]
		public void DataValidations_AddOneValidationOfTypeListOfTypeList()
		{
			var file = new FileInfo(GetTestOutputPath("AddOneValidationOfTypeList.xlsx"));
			try
			{
				var validation = _sheet.DataValidations.AddListValidation("A:A");
				validation.ShowErrorMessage = true;
				validation.ShowInputMessage = true;
				validation.Formula.Values.Add("1");
				validation.Formula.Values.Add("2");
				validation.Formula.Values.Add("3");
				validation.Validate();

				_package.SaveAs(file);
			}
			finally
			{
				if (file.Exists)
					file.Delete();
			}
		}

		[TestMethod]
		public void DataValidations_AddOneValidationOfTypeListOfTypeTime()
		{
			var file = new FileInfo(GetTestOutputPath("AddOneValidationOfTypeTime.xlsx"));
			try
			{
				var validation = _sheet.DataValidations.AddTimeValidation("A1");
				validation.ShowErrorMessage = true;
				validation.ShowInputMessage = true;
				validation.Formula.Value.Hour = 14;
				validation.Formula.Value.Minute = 30;
				validation.Operator = ExcelDataValidationOperator.greaterThan;
				validation.Prompt = "Enter a time greater than 14:30";
				validation.Error = "Invalid time was entered";
				validation.Validate();

				_package.SaveAs(file);
			}
			finally
			{
				if (file.Exists)
					file.Delete();
			}
		}

		[TestMethod, Ignore]
		public void DataValidations_ReadExistingWorkbookWithDataValidations()
		{
			using (var package = new ExcelPackage(new FileInfo(GetTestOutputPath("DVTest.xlsx"))))
			{
				Assert.AreEqual(3, package.Workbook.Worksheets[1].DataValidations.Count);
			}
		}

		[TestMethod, Ignore]
		public void RemoveDataValidation()
		{
			var fileInfo = new FileInfo(@"c:\Temp\DvTest.xlsx");
			if (File.Exists(fileInfo.FullName))
				File.Delete(fileInfo.FullName);
			using (var package = new ExcelPackage())
			{
				var sheet = package.Workbook.Worksheets.Add("test");
				var validation = sheet.DataValidations.AddIntegerValidation("A1");
				validation.Formula.Value = 1;
				validation.Formula2.Value = 2;
				validation.ShowErrorMessage = true;
				validation.Error = "Error!";

				//var validation2 = sheet.DataValidations.AddIntegerValidation("B1");
				//validation2.Formula.Value = 1;
				//validation2.Formula2.Value = 2;
				//validation2.ShowErrorMessage = true;
				//validation2.Error = "Error!";

				package.SaveAs(fileInfo);
			}
			using (var pck = new ExcelPackage(fileInfo))
			{
				var sheet = pck.Workbook.Worksheets.First();
				var dv = sheet.DataValidations.First();
				sheet.DataValidations.Remove(dv);
				if (File.Exists(fileInfo.FullName))
					File.Delete(fileInfo.FullName);
				pck.SaveAs(fileInfo);
			}
		}
	}
}
