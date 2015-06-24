using System;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DataTableUtilities {
	public class DataTableSerializer {
		public static byte[] GetBaseStructBytes(DataTable dt) {
			byte[] binaryDataResult = null;
			using(MemoryStream ms = new MemoryStream()) {
				BinaryFormatter bf = new BinaryFormatter ();
				dt.RemotingFormat = SerializationFormat.Binary;
				bf.Serialize(ms, dt);
				binaryDataResult = ms.ToArray ();
			}

			return binaryDataResult;
		}

		public static DataTable GetBaseStructFromBytes(byte[] bytes) {
			BinaryFormatter bf = new BinaryFormatter ();
			MemoryStream ms = new MemoryStream (bytes);
			DataTable dt = (DataTable)bf.Deserialize (ms);
			ms.Close ();
			return dt;
		}
	}
}
