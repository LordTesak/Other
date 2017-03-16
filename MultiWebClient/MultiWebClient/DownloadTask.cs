using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiWeb {
	public struct DownloadTaskId : IEquatable<DownloadTaskId> {
		private long id;

		public DownloadTaskId(long id) {
			this.id = id;
		}

		public long Id {
			get { return id; }
		}

		public bool Equals(DownloadTaskId other) {
			return id.Equals(other.id);
		}

		public override int GetHashCode() {
			return id.GetHashCode();
		}

		public static implicit operator DownloadTaskId(long id) {
			return new DownloadTaskId(id);
		}

		public static explicit operator long(DownloadTaskId id) {
			return id.id;
		}
	}

	class DownloadTask {
		private string result;
		private string address;
		private string data;
		private bool downloaded;
		private object monitor = new object();
		private Exception exception = null;

		public DownloadTask(string address) {
			this.address = address;
			this.downloaded = false;
			this.data = null;
		}

		public DownloadTask(string address, string data) {
			this.address = address;
			this.downloaded = false;
			this.data = data;
		}

		public string Result {
			get {
				while (true) {
					if (downloaded) {
						if (exception != null)
							throw exception;
						return result;
					}
					Util.LockedWait(monitor);
				}
			}
			set {
				result = value;
				Thread.MemoryBarrier();
				downloaded = true;
				Util.LockedPulseAll(monitor);
			}
		}

		public string Address {
			get {
				return address;
			}
		}

		public string Data {
			get { return data; }
		}

		public bool Get {
			get { return (data == null); }
		}

		public Exception Exception {
			set { exception = value; }
			get { return exception; }
		}
	}
}
