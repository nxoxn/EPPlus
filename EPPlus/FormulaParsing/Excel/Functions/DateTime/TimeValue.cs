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

using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.ExpressionGraph;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime
{	
	/// <summary>
	/// Implements the TIMEVALUE function.
	/// </summary>
	public class TimeValue : ExcelFunction
	{
		/// <summary>
		/// Get the value represented by the input string.
		/// </summary>
		/// <param name="arguments">String to be converted to a time value.</param>
		/// <param name="context">Unused, this is information about where the function is being executed.</param>
		/// <returns>The OADate representation of the specified string.</returns>
		public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
		{
			if (this.ArgumentsAreValid(arguments, 1, out eErrorType argumentError) == false)
				return new CompileResult(argumentError);

			var dateString = this.ArgToString(arguments, 0);

			return this.Execute(dateString);
		}

		internal CompileResult Execute(string dateString)
		{

			this.TryParseDateStringToDouble(dateString, out double resultAfterParse);

			var resultDecimalsOnly = resultAfterParse - System.Math.Truncate(resultAfterParse);
	
			return resultAfterParse != -1 ?		//The '-1' is used to throw an error if an invalid input is supplied.
				 CreateResult(resultDecimalsOnly, DataType.Decimal) :
				 CreateResult(ExcelErrorValue.Create(eErrorType.Value), DataType.ExcelError);
		}
		private bool TryParseDateStringToDouble(string dateString, out double result )
		{
			var parser = new TimeStringParser();
			if (parser.Parse(dateString) == -1)
			{
				result = -1;
				return false;
			}
			else
			{
				result = parser.Parse(dateString);
				return true;
			}
		}
	}
}