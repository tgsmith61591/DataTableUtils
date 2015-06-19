using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace DataTableUtilities {
	public class DataTableWritable : DataTable {
		public enum PrintJustifier {
			RIGHT,
			LEFT
		}

		public readonly Int32 DEFAULT_HEAD = 6;
		public readonly PrintJustifier DEFAULT_PRINT_JUSTIFICATION = PrintJustifier.RIGHT;
		public PrintJustifier PrintJustification { get; set; }
		public DataTableWritable () : base() {
			this.PrintJustification = DEFAULT_PRINT_JUSTIFICATION;
		}

		public String[] Headers() {
			return (from dc in this.Columns.Cast<DataColumn>()
				select dc.ColumnName).ToArray();;
		}

		public void Print() {
			PrintHead (this.Rows.Count);
		}

		public void PrintHead() {
			PrintHead (DEFAULT_HEAD);
		}

		public void PrintHead(Int32 RowMax) {
			Print (RowMax);
		}

		private void Print(Int32 RowMax) {
			Int32 RowCount = this.Rows.Count;
			new Printer (this).Print (Math.Min(RowCount, RowMax));
		}

		public void TogglePrinterJustification() {
			PrintJustification = PrintJustification == DEFAULT_PRINT_JUSTIFICATION ? PrintJustifier.LEFT : PrintJustifier.RIGHT;
		}



		private sealed class Printer {
			private readonly static Int32 WHITE_SPACE = 4;
			private bool RightJustify;
			private String[] Headers;
			DataTableWritable Table;

			public Printer(DataTableWritable Table) {
				this.Table = Table;
				this.RightJustify = Table.PrintJustification == DataTableWritable.PrintJustifier.RIGHT;
				Headers = Table.Headers();
			}

			private Int32 ColumnWidth(String name) {
				return Table.Rows.Cast<DataRow>().Select (row => ((DataRow)row) [name].ToString ().Length).ToArray ().Max (s => s);
			}

			private Dictionary<String, Int32> GetHeaderToWidth() {
				Dictionary<String, Int32> Result = new Dictionary<String, Int32> ();
				foreach (String s in Headers)
					Result.Add (s, Math.Max(s.Length, ColumnWidth(s)));
				return Result;
			}

			public void Print(Int32 RowMax) {
				Dictionary<String, Int32> HeaderToWidth = GetHeaderToWidth();

				int RowNum = 0;
				while(RowNum < RowMax) {
					StringBuilder sb = new StringBuilder ();
					if(RowNum == 0) {
						foreach (String header in Headers)
							sb.Append (WhiteSpaceFormattedString (header, HeaderToWidth [header]));

						Console.WriteLine (sb.ToString ());
						sb = new StringBuilder ();
					}

					int colIdx = 0;
					foreach (String header in Headers)
						sb.Append (WhiteSpaceFormattedString( Table.Rows[RowNum].ItemArray[colIdx++].ToString(), HeaderToWidth[header] ));

					Console.WriteLine (sb.ToString ());
					RowNum++;
				}
			}

			private static String WhiteSpace(Int32 Length) {
				Char[] c = new Char[Length];
				for(int i = 0; i < Length; i++)
					c[i] = ' ';
				return new String ( c );
			}

			private String WhiteSpaceFormattedString(String Raw, Int32 ColumnWidth) {
				String w = WhiteSpace (ColumnWidth + WHITE_SPACE - Raw.Length);
				return RightJustify ? w + Raw : Raw + w;
			}
		}
	}
}

