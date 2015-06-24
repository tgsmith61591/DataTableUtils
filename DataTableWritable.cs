using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;

namespace DataTableUtilities {
	[Serializable()]
	public class DataTableWritable : DataTable {
		public enum PrintJustifier {
			RIGHT,
			LEFT
		}

		/// <summary>
		/// Used for deserialization purposes
		/// </summary>
		private static readonly Dictionary<int, PrintJustifier> PrintJustifierBitMaskDeserializer = new Dictionary<int, PrintJustifier> () {
			{0, PrintJustifier.RIGHT},
			{1, PrintJustifier.LEFT}
		};

		/// <summary>
		/// Used for serialization purposes
		/// </summary>
		private static readonly Dictionary<PrintJustifier, int> PrintJustifierBitMaskSerializer = new Dictionary<PrintJustifier, int> () {
			{PrintJustifier.RIGHT, 0},
			{PrintJustifier.LEFT,  1}
		};


		public static readonly int DEFAULT_WHITE_SPACE = 4;
		public static readonly int DEFAULT_HEAD = 6;
		public static readonly PrintJustifier DEFAULT_PRINT_JUSTIFICATION = PrintJustifier.RIGHT;
		public static readonly char DEFAULT_WHITE_SPACE_CHAR = ' ';

		/// <summary>
		/// Holds the printer justification
		/// </summary>
		/// <value>The print justification.</value>
		public PrintJustifier PrintJustification { get; set; }

		/* These variables will be serialized */
		private int PrintJustificationBitMask { get; set; }
		private int WhiteSpace = DEFAULT_WHITE_SPACE; //Set as private because must handle < 1 exception
		public char WhiteSpaceChar { get; set; }
		public bool PrintHeaders { get; set; }


		protected DataTableWritable(SerializationInfo info, StreamingContext context) : base(info, context) { init (); }
		public DataTableWritable () : base() { init (); }
		public DataTableWritable(DataTable data) : this() {
			DataTable d = data.Copy ();

			String[] headers = Headers (d);
			foreach(String header in headers)
				this.Columns.Add (new DataColumn (header, d.Columns[header].DataType));

			foreach (DataRow row in d.Rows) {
				DataRow dr = this.NewRow ();

				foreach (String header in headers)
					dr [header] = row [header];

				this.Rows.Add (dr);
			}
		}


		private void init() {
			this.PrintJustification = DEFAULT_PRINT_JUSTIFICATION;
			this.PrintJustificationBitMask = PrintJustifierBitMaskSerializer [this.PrintJustification];

			this.WhiteSpaceChar = DEFAULT_WHITE_SPACE_CHAR;
			this.PrintHeaders = true;
		}



		public new DataTableWritable Copy() {
			DataTableWritable db = (DataTableWritable) base.Copy ();
			db.PrintJustification = this.PrintJustification;
			db.WhiteSpace = this.WhiteSpace;
			db.WhiteSpaceChar = this.WhiteSpaceChar;
			db.PrintHeaders = this.PrintHeaders;

			return db;
		}

		public static String[] Headers(DataTable data) {
			return (from dc in data.Columns.Cast<DataColumn>()
				select dc.ColumnName).ToArray();
		}

		public String[] Headers() {
			return Headers (this);
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

		public static DataTableWritable ReadFromByteStream(byte[] bytes) {
			using (MemoryStream m = new MemoryStream(bytes)) {
				using (BinaryReader reader = new BinaryReader(m)) {
					int length = reader.ReadInt32 ();
					byte[] datatable = reader.ReadBytes (length);
					DataTable baseStruct = DataTableSerializer.GetBaseStructFromBytes (datatable);
					DataTableWritable result = new DataTableWritable(baseStruct);

					result.PrintJustificationBitMask = reader.ReadInt32();
					result.PrintJustification = PrintJustifierBitMaskDeserializer[result.PrintJustificationBitMask];

					result.WhiteSpace = reader.ReadInt32 ();
					result.WhiteSpaceChar = reader.ReadChar ();
					result.PrintHeaders = reader.ReadBoolean ();

					return result;
				}
			}
		}

		public static Type[] Schema(DataTable data) {
			return (from dc in data.Columns.Cast<DataColumn>()
				select dc.DataType).ToArray();
		}

		public Type[] Schema() {
			return Schema (this);
		}

		public void SetWhiteSpace(Int32 Space) {
			if (Space < 1)
				throw new ArgumentException (Space+" cannot be less than 1");
			this.WhiteSpace = Space;
		}

		public void TogglePrinterJustification() {
			PrintJustification = PrintJustification == DEFAULT_PRINT_JUSTIFICATION ? PrintJustifier.LEFT : PrintJustifier.RIGHT;
			this.PrintJustificationBitMask = PrintJustifierBitMaskSerializer [this.PrintJustification];
		}

		public byte[] WriteToByteStream() {
			using (MemoryStream m = new MemoryStream()) {
				using (BinaryWriter writer = new BinaryWriter(m)) {
					byte[] dtbytes = DataTableSerializer.GetBaseStructBytes ((DataTable)this);
					writer.Write((Int32)dtbytes.Length);
					writer.Write(dtbytes);

					writer.Write(PrintJustificationBitMask);
					writer.Write(WhiteSpace);
					writer.Write(WhiteSpaceChar);
					writer.Write(PrintHeaders);
				}
				return m.ToArray();
			}
		}




		/// <summary>
		/// Private inner class to handle all prints
		/// </summary>
		private sealed class Printer {
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
					if(RowNum == 0 && Table.PrintHeaders) {
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

			private static String WhiteSpace(Int32 Length, Char WhiteSpaceChar) {
				Char[] c = new Char[Length];
				for(int i = 0; i < Length; i++)
					c[i] = WhiteSpaceChar;
				return new String ( c );
			}

			private String WhiteSpaceFormattedString(String Raw, Int32 ColumnWidth) {
				String w = WhiteSpace (ColumnWidth + Table.WhiteSpace - Raw.Length, this.Table.WhiteSpaceChar);
				return RightJustify ? w + Raw : Raw + w;
			}
		}
	}
}

