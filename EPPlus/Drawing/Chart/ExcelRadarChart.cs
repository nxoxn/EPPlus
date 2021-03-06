﻿/*******************************************************************************
 * You may amend and distribute as you like, but don't remove this header!
 *
 * EPPlus provides server-side generation of Excel 2007/2010 spreadsheets.
 * See http://www.codeplex.com/EPPlus for details.
 *
 * Copyright (C) 2011  Jan Källman
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
 * Code change notes:
 * 
 * Author							Change						Date
 * ******************************************************************************
 * Jan Källman		Initial Release		        2009-10-01
 * Jan Källman		License changed GPL-->LGPL 2011-12-16
 *******************************************************************************/
using System;
using System.Globalization;
using System.Xml;
using OfficeOpenXml.Table.PivotTable;

namespace OfficeOpenXml.Drawing.Chart
{
	/// <summary>
	/// Provides access to line chart specific properties.
	/// </summary>
	public class ExcelRadarChart : ExcelChart
	{
		#region Constants
		private const string StylePath = "c:radarStyle/@val";
		#endregion

		#region Class Variables
		private ExcelChartDataLabel myDataLabel = null;
		#endregion
		
		#region Properties
		/// <summary>
		/// The type of radarchart.
		/// </summary>
		public eRadarStyle RadarStyle
		{
			get
			{
				var value = this.ChartXmlHelper?.GetXmlNodeString(ExcelRadarChart.StylePath);
				if (string.IsNullOrEmpty(value))
					return eRadarStyle.Standard;
				else
					return (eRadarStyle)Enum.Parse(typeof(eRadarStyle), value, true);
			}
			set
			{
				this.ChartXmlHelper?.SetXmlNodeString(ExcelRadarChart.StylePath, value.ToString().ToLower(CultureInfo.InvariantCulture));
			}
		}
		
		/// <summary>
		/// Access to the DataLabel properties.
		/// </summary>
		public ExcelChartDataLabel DataLabel
		{
			get
			{
				if (this.myDataLabel == null)
					this.myDataLabel = new ExcelChartDataLabel(this.NameSpaceManager, this.ChartNode);
				return this.myDataLabel;
			}
		}
		#endregion

		#region Constructors
		internal ExcelRadarChart(ExcelDrawings drawings, XmlNode node, Uri uriChart, Packaging.ZipPackagePart part, XmlDocument chartXml, XmlNode chartNode) :
			 base(drawings, node, uriChart, part, chartXml, chartNode)
		{
			SetTypeProperties();
		}

		internal ExcelRadarChart(ExcelChart topChart, XmlNode chartNode) :
			 base(topChart, chartNode)
		{
			SetTypeProperties();
		}

		internal ExcelRadarChart(ExcelDrawings drawings, XmlNode node, eChartType type, ExcelChart topChart, ExcelPivotTable PivotTableSource) :
			 base(drawings, node, type, topChart, PivotTableSource)
		{
			SetTypeProperties();
		}
		#endregion

		#region Private Methods
		private void SetTypeProperties()
		{
			if (this.ChartType == eChartType.RadarFilled)
				this.RadarStyle = eRadarStyle.Filled;
			else if (this.ChartType == eChartType.RadarMarkers)
				this.RadarStyle = eRadarStyle.Marker;
			else
				this.RadarStyle = eRadarStyle.Standard;
		}
		#endregion
		
		#region Internal Methods
		internal override eChartType GetChartType(string name)
		{
			if (this.RadarStyle == eRadarStyle.Filled)
				return eChartType.RadarFilled;
			else if (this.RadarStyle == eRadarStyle.Marker)
				return eChartType.RadarMarkers;
			else
				return eChartType.Radar;
		}
		#endregion
	}
}
