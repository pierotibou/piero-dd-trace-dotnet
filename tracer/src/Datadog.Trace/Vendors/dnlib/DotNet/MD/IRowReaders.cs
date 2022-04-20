//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, SYSLIB0011,SYSLIB0032
// dnlib: See LICENSE.txt for more info

namespace Datadog.Trace.Vendors.dnlib.DotNet.MD {
	/// <summary>
	/// Reads metadata table columns
	/// </summary>
	internal interface IColumnReader {
		/// <summary>
		/// Reads a column
		/// </summary>
		/// <param name="table">The table to read from</param>
		/// <param name="rid">Table row id</param>
		/// <param name="column">The column to read</param>
		/// <param name="value">Result</param>
		/// <returns><c>true</c> if <paramref name="value"/> was updated, <c>false</c> if
		/// the column should be read from the original table.</returns>
		bool ReadColumn(MDTable table, uint rid, ColumnInfo column, out uint value);
	}

	/// <summary>
	/// Reads table rows
	/// </summary>
	/// <typeparam name="TRow">Raw row</typeparam>
	internal interface IRowReader<TRow> where TRow : struct {
		/// <summary>
		/// Reads a table row or returns false if the row should be read from the original table
		/// </summary>
		/// <param name="rid">Row id</param>
		/// <param name="row">The row</param>
		/// <returns></returns>
		bool TryReadRow(uint rid, out TRow row);
	}
}
