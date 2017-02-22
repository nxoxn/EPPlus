﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using EPPlusTest.FormulaParsing.TestHelpers;

namespace EPPlusTest.FormulaParsing.Excel.Functions.Math
{
	/// <summary>
	/// Tests for the FLOOR function, as defined at 
	/// https://support.office.com/en-us/article/FLOOR-function-14bb497c-24f2-4e04-b327-b0b4de5a8886
	/// and https://exceljet.net/excel-functions/excel-floor-function .
	/// </summary>
	[TestClass]
	public class FloorTests
	{
		private ParsingContext _parsingContext = ParsingContext.Create();

		[TestMethod]
		public void FloorOfNineIntoThreeIsNine()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(9, 3);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(9.0, result.Result);
		}

		[TestMethod]
		public void FloorWorksForDecimalsToo()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(1.57, 0.1);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(1.5, result.Result);
			args = FunctionsHelper.CreateArgs(0.234, 0.01);
			result = func.Execute(args, _parsingContext);
			Assert.AreEqual(0.23, result.Result);
		}

		[TestMethod]
		public void FloorWorksForWeirdDecimalsToo()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(1.57, 0.397);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(1.191, result.Result);
		}

		[TestMethod]
		public void FloorOfNegativeOneIntoThreeIsNegativeThree()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(-1, 3);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(-3.0, result.Result);
		}

		[TestMethod]
		public void FloorOfTenAndElevenIntoThreeIsNine()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(10, 3);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(9.0, result.Result);
			args = FunctionsHelper.CreateArgs(11, 3);
			result = func.Execute(args, _parsingContext);
			Assert.AreEqual(9.0, result.Result);
		}

		[TestMethod]
		public void FloorOfNegativeTwoPointFiveIntoNegativeTwoIsNegativeTwo()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(-2.5, -2);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(-2.0, result.Result);
		}

		[TestMethod]
		public void FloorOfNegativeFourIntoNegativeTwoIsNegativeFour()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(-4, -2);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(-4.0, result.Result);
		}

		[TestMethod]
		public void FloorOfNegativeFourIntoTwoIsNegativeFour()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(-4, 2);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(-4.0, result.Result);
		}

		[TestMethod]
		public void FloorOfNegativeIntoPositiveNumberRoundsDownTowardsNegativeInfinity()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(-2.5, 2);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(-4.0, result.Result);
		}

		[TestMethod]
		public void FloorOfPositiveIntoNegativeNumberIsPoundNum()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(2.5, -2);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(ExcelErrorValue.Create(eErrorType.Num), result.Result);
		}

		[TestMethod]
		public void FloorWithZeroSignificancePoundDivZeroes()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(9, 0);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(ExcelErrorValue.Create(eErrorType.Div0), result.Result);
		}

		[TestMethod]
		public void FloorWithOneArgumentPoundValues()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(9);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(ExcelErrorValue.Create(eErrorType.Value), result.Result);
		}

		[TestMethod]
		public void FloorOfZeroIntoAnythingIsZero()
		{
			var rng = new Random();
			var func = new Floor();
			for (int i = 0; i < 1000; i++)
			{
				var args = FunctionsHelper.CreateArgs(0, rng.Next());
				var result = func.Execute(args, _parsingContext);
				Assert.AreEqual(0.0, result.Result);
			}
			for (int i = 0; i < 1000; i++)
			{
				var args = FunctionsHelper.CreateArgs(0, -rng.Next());
				var result = func.Execute(args, _parsingContext);
				Assert.AreEqual(0.0, result.Result);
			}
			for (int i = 0; i < 1000; i++)
			{
				var args = FunctionsHelper.CreateArgs(0, rng.Next() + rng.NextDouble());
				var result = func.Execute(args, _parsingContext);
				Assert.AreEqual(0.0, result.Result);
			}
		}

		[TestMethod]
		public void FloorShouldReturnCorrectResultWhenSignificanceIsBetween0And1()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(26.75d, 0.1);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(26.7d, (double)result.Result, 0.000000001);
		}

		[TestMethod]
		public void FloorShouldReturnCorrectResultWhenSignificanceIs1()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(26.75d, 1);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(26d, result.Result);
		}

		[TestMethod]
		public void FloorShouldReturnCorrectResultWhenSignificanceIsMinus1()
		{
			var func = new Floor();
			var args = FunctionsHelper.CreateArgs(-26.75d, -1);
			var result = func.Execute(args, _parsingContext);
			Assert.AreEqual(-26d, result.Result);
		}
	}
}