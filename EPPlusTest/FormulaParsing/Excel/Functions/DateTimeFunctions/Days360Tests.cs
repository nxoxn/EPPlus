﻿/*******************************************************************************
* You may amend and distribute as you like, but don't remove this header!
*
* EPPlus provides server-side generation of Excel 2007/2010 spreadsheets.
* See http://www.codeplex.com/EPPlus for details.
*
* Copyright (C) 2011-2017 Jan Källman, Matt Delaney, and others as noted in the source history.
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
using System;
using System.Globalization;
using System.Threading;
using EPPlusTest.Excel.Functions.DateTimeFunctions;
using EPPlusTest.FormulaParsing.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

namespace EPPlusTest.FormulaParsing.Excel.Functions.DateTimeFunctions
{
	[TestClass]
	public class Days360Tests : DateTimeFunctionsTestBase
	{
		#region Days360 Function (Execute) Tests
		[TestMethod]
		public void Days360ShouldReturnCorrectResultWithNoMethodSpecified()
		{
			var function = new Days360();
			var startDate = new DateTime(2013, 1, 1).ToOADate();
			var endDate = new DateTime(2013, 3, 31).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(startDate, endDate), this.ParsingContext);
			Assert.AreEqual(90, result.Result);
		}

		[TestMethod]
		public void Days360ShouldReturnCorrectResultWithNoMethodSpecifiedMiddleOfMonthDates()
		{
			var function = new Days360();
			var startDate = new DateTime(1982, 4, 25).ToOADate();
			var endDate = new DateTime(2016, 6, 12).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(startDate, endDate), this.ParsingContext);
			Assert.AreEqual(12287, result.Result);
		}

		[TestMethod]
		public void Days360ShouldReturnCorrectResultWithEuroMethodSpecified()
		{
			var function = new Days360();
			var startDate = new DateTime(2013, 1, 1).ToOADate();
			var endDate = new DateTime(2013, 3, 31).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(startDate, endDate, true), this.ParsingContext);
			Assert.AreEqual(89, result.Result);
		}

		[TestMethod]
		public void Days360ShouldHandleFebWithEuroMethodSpecified()
		{
			var function = new Days360();
			var startDate = new DateTime(2012, 2, 29).ToOADate();
			var endDate = new DateTime(2013, 2, 28).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(startDate, endDate, true), this.ParsingContext);
			Assert.AreEqual(359, result.Result);
		}

		[TestMethod]
		public void Days360ShouldHandleFebWithUsMethodSpecified()
		{
			var function = new Days360();
			var startDate = new DateTime(2012, 2, 29).ToOADate();
			var endDate = new DateTime(2013, 2, 28).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(startDate, endDate, false), this.ParsingContext);
			Assert.AreEqual(358, result.Result);
		}

		[TestMethod]
		public void Days360ShouldHandleFebWithUsMethodSpecifiedEndOfMonth()
		{
			var function = new Days360();
			var startDate = new DateTime(2013, 2, 28).ToOADate();
			var endDate = new DateTime(2013, 3, 31).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(startDate, endDate, false), this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360ShouldHandleNullFirstDateArgument()
		{
			var function = new Days360();
			var endDate = new DateTime(2013, 3, 15).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(null, endDate, false), this.ParsingContext);
			Assert.AreEqual(40755, result.Result);
		}

		[TestMethod]
		public void Days360ShouldHandleNullFirstDateArgumentEndOfMonth()
		{
			var function = new Days360();
			var endDate = new DateTime(2013, 3, 31).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(null, endDate, false), this.ParsingContext);
			Assert.AreEqual(40771, result.Result);
		}

		[TestMethod]
		public void Days360ShouldHandleNullSecondDateArgument()
		{
			var function = new Days360();
			var startDate = new DateTime(1992, 2, 10).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(startDate, null, false), this.ParsingContext);
			Assert.AreEqual(-33160, result.Result);
		}

		[TestMethod]
		public void Days360ShouldHandleNullSecondDateArgumentEndOfMonth()
		{
			var function = new Days360();
			var startDate = new DateTime(2013, 2, 28).ToOADate();
			var result = function.Execute(FunctionsHelper.CreateArgs(startDate, null, false), this.ParsingContext);
			Assert.AreEqual(-40740, result.Result);
		}

		[TestMethod]
		public void Days360WithInvalidArgumentReturnsPoundValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs();
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void Days306WithInputAsResultOfDateFunctionReturnsCorrectValue()
		{
			var function = new Days360();
			var dateArg1 = new DateTime(2017, 5, 31);
			var dateArg2 = new DateTime(2017, 6, 30);
			var args = FunctionsHelper.CreateArgs(dateArg1, dateArg2);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithInputAsDateStringsReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5/31/2017", "6/30/2017");
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithIntegerInputReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs(15, 20);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(5, result.Result);
		}

		[TestMethod]
		public void Days360WithDatesNotAsStringReturnsZero()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs(5 / 31 / 2017, 6 / 30 / 2017);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(0, result.Result);
		}

		[TestMethod]
		public void Days360WithGenericStringReturnsPoundValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("string", "string");
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void Days360WithEmptyStringReturnsPoundValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("", "");
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void Days360WithStartDateAfterEndDateReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("6/30/2017", "5/31/2017");
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(-30, result.Result);
		}

		[TestMethod]
		public void Days360WithDatesWrittenOutAsStringReturnCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("31 May 2017", "30 Jun 2017");
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithDashesInsteadOfSlashesInStringReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5-31-2017", "6-30-2017");
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithPeriodInsteadOfSlashesInStringReturnsCorrectValue()
		{
			// This functionality is different than that of Excel's. Excel does not support inputs of this format,
			// and instead returns a #VALUE!, however many European countries write their dates with periods instead
			//of slashes so EPPlus supports this format of entering dates.
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5.31.2017", "6.30.2017");
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		//The following test cases have true in the second parameter for the European method 

		[TestMethod]
		public void Days360WithEuropeanDatesFromDateFunctionReturnsCorrectValue()
		{
			var function = new Days360();
			var dateArg1 = new DateTime(2017, 5, 31);
			var dateArg2 = new DateTime(2017, 6, 30);
			var args = FunctionsHelper.CreateArgs(dateArg1, dateArg2, true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentsAsDateStringsReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5/31/2017", "6/30/2017", true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentAndIntegerArgumentsReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs(15, 20, true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(5, result.Result);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentndDatesNotAStringsReturnsZero()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs(5 / 31 / 2017, 6 / 30 / 2017, true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(0, result.Result);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentAndGeneralStringReturnsPoundValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("string", "string", true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentWithEmptyStringReturnsPoundValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("", "", true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentWithStartDateAfterEndDateReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("6/30/2017", "5/31/2017", true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(-30, result.Result);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentAndNullFirstDateReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs(null, "6/30/2017", true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(42300, result.Result);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentAndNullSecondDateReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5/30/2017", null, true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(-42270, result.Result);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentAndDatesWrittenOutAsStringsReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("31 May 2017", "30 Jun 2017", true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentAndDatesWrittenWithDashesInsteadOfSlashesReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5-31-2017", "6-30-2017", true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithEuropeanDateArgumentAndDatesWrittenWithPeriodsInsteadOfSlashesReturnsCorrectValue()
		{
			// This functionality is different than that of Excel's. Excel does not support inputs of this format,
			// and instead returns a #VALUE!, however many European countries write their dates with periods instead
			//of slashes so EPPlus supports this format of entering dates.
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5.31.2017", "6.30.2017", true);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithGenericStringAsMethodParameterReturnsPoundValue()
		{
			var function = new Days360();
			var dateArg1 = new DateTime(2017, 5, 31);
			var dateArg2 = new DateTime(2017, 6, 30);
			var args = FunctionsHelper.CreateArgs(dateArg1, dateArg2, "string");
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void Days360WithTrueOrFalseAsStringReturnsCorrecValue()
		{
			var function = new Days360();
			var argsWithTrue = FunctionsHelper.CreateArgs("5/31/2017", "6/30/2017", "tRuE");
			var argsWithFalse = FunctionsHelper.CreateArgs("5/31/2017", "6/30/2017", "false");
			var resultWithTrue = function.Execute(argsWithTrue, this.ParsingContext);
			var resultWithFalse = function.Execute(argsWithFalse, this.ParsingContext);
			Assert.AreEqual(30, resultWithTrue.Result);
			Assert.AreEqual(30, resultWithFalse.Result);
		}

		[TestMethod]
		public void Days360WithIntegerAsMethodParameterReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5/1/2017", "5/31/2017", 1500);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(29, result.Result);
		}

		[TestMethod]
		public void Days360WithZeroAsMethodParameterReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5/1/2017", "5/31/2017", 0);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithStringZeroAsMethodParameterReturnsCorrectValue()
		{
			var function = new Days360();
			var args = FunctionsHelper.CreateArgs("5/1/2017", "5/31/2017", "0");
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)result.Result).Type);
		}

		[TestMethod]
		public void Days360WithDateAsMethodParameterReurnsCorrectValue()
		{
			var function = new Days360();
			var dateArg = new DateTime(2017, 6, 3);
			var args = FunctionsHelper.CreateArgs("5/31/2017", "6/30/2017", dateArg);
			var result = function.Execute(args, this.ParsingContext);
			Assert.AreEqual(30, result.Result);
		}

		[TestMethod]
		public void Days360WithGermanCultureReturnsCorrectValue()
		{
			var currentCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("de-DE");
				var function = new Days360();
				var args = FunctionsHelper.CreateArgs("30.5.2017", "30.6.2017", "TRUE");
				var result = function.Execute(args, this.ParsingContext);
				Assert.AreEqual(30, result.Result);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = currentCulture;
			}
		}

		[TestMethod]
		public void Days360WithDates0And1ReturnsCorrectResult()
		{
			// Note that the Excel OADate 0 corresponds to the special date 1/0/1900 in Excel.
			// The Excel OADate 1 corresponds to 1/1/1900.
			var func = new Days360();
			var args = FunctionsHelper.CreateArgs(0, 1);
			var result = func.Execute(args, this.ParsingContext);
			Assert.AreEqual(1, result.Result);
		}

		[TestMethod]
		public void Days360WithDates0And1InStringsReturnsCorrectResult()
		{
			var func = new Days360();
			var args = FunctionsHelper.CreateArgs("0", "1");
			var result = func.Execute(args, this.ParsingContext);
			Assert.AreEqual(1, result.Result);
		}

		[TestMethod]
		public void Days360WithZeroDateAnd31January1900ReturnsCorrectResult()
		{
			// Note that the Excel OADate 0 corresponds to the special date 1/0/1900 in Excel.
			// The Excel OADate 31 corresponds to 1/31/1900.
			var func = new Days360();
			var args = FunctionsHelper.CreateArgs(0, 31);
			var result = func.Execute(args, this.ParsingContext);
			Assert.AreEqual(31, result.Result);
		}

		[TestMethod]
		public void Days360WithZeroDateAnd1February1900ReturnsCorrectResult()
		{
			// Note that the Excel OADate 0 corresponds to the special date 1/0/1900 in Excel.
			// The Excel OADate 32 corresponds to 2/1/1900.
			var func = new Days360();
			var args = FunctionsHelper.CreateArgs(0, 32);
			var result = func.Execute(args, this.ParsingContext);
			Assert.AreEqual(31, result.Result);
		}

		[TestMethod]
		public void Days360WithZeroDateAnd28February1900ReturnsCorrectResult()
		{
			// Note that the Excel OADate 0 corresponds to the special date 1/0/1900 in Excel.
			// The Excel OADate 59 corresponds to 2/28/1900.
			var func = new Days360();
			var args = FunctionsHelper.CreateArgs(0, 59);
			var result = func.Execute(args, this.ParsingContext);
			Assert.AreEqual(58, result.Result);
		}

		[TestMethod]
		public void Days360WithZeroDateAnd1March1900ReturnsCorrectResult()
		{
			// Note that the Excel OADate 0 corresponds to the special date 1/0/1900 in Excel.
			// The Excel OADate 61 corresponds to 3/1/1900.
			var func = new Days360();
			var args = FunctionsHelper.CreateArgs(0, 61);
			var result = func.Execute(args, this.ParsingContext);
			Assert.AreEqual(61, result.Result);
		}

		[TestMethod]
		public void Days360WithZeroDateAnd31March1900ReturnsCorrectResult()
		{
			// Note that the Excel OADate 0 corresponds to the special date 1/0/1900 in Excel.
			// The Excel OADate 91 corresponds to 3/31/1900.
			var func = new Days360();
			var args = FunctionsHelper.CreateArgs(0, 91);
			var result = func.Execute(args, this.ParsingContext);
			Assert.AreEqual(91, result.Result);
		}

		[TestMethod]
		public void Days360FunctionWithErrorValuesAsInputReturnsTheInputErrorValue()
		{
			var func = new Days360();
			var argNA = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.NA),5);
			var argNAME = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Name),5);
			var argVALUE = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Value),5);
			var argNUM = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Num),5);
			var argDIV0 = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Div0),5);
			var argREF = FunctionsHelper.CreateArgs(ExcelErrorValue.Create(eErrorType.Ref),5);
			var resultNA = func.Execute(argNA, this.ParsingContext);
			var resultNAME = func.Execute(argNAME, this.ParsingContext);
			var resultVALUE = func.Execute(argVALUE, this.ParsingContext);
			var resultNUM = func.Execute(argNUM, this.ParsingContext);
			var resultDIV0 = func.Execute(argDIV0, this.ParsingContext);
			var resultREF = func.Execute(argREF, this.ParsingContext);
			Assert.AreEqual(eErrorType.NA, ((ExcelErrorValue)resultNA.Result).Type);
			Assert.AreEqual(eErrorType.Name, ((ExcelErrorValue)resultNAME.Result).Type);
			Assert.AreEqual(eErrorType.Value, ((ExcelErrorValue)resultVALUE.Result).Type);
			Assert.AreEqual(eErrorType.Num, ((ExcelErrorValue)resultNUM.Result).Type);
			Assert.AreEqual(eErrorType.Div0, ((ExcelErrorValue)resultDIV0.Result).Type);
			Assert.AreEqual(eErrorType.Ref, ((ExcelErrorValue)resultREF.Result).Type);
		}
		#endregion
	}
}
