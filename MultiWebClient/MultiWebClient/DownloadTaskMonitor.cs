using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiWeb {
	public class DownloadTaskItemActive {
		private DateTime request;
		private string address;

		public DownloadTaskItemActive(DateTime request, string address) {
			this.request = request;
			this.address = address;
		}

		public DateTime Request {
			get { return request; }
		}
		public string Address {
			get { return address; }
		}

		public DownloadTaskItem Finilize(DateTime response, bool success) {
			DownloadTaskItem result = new DownloadTaskItem(request, response, address, success);
			return result;
		}
	}

	public struct DownloadTaskItem {
		private DateTime request;
		private DateTime response;
		private string address;
		private bool success;

		public DownloadTaskItem(DateTime request, DateTime response, string address, bool success) {
			this.request = request;
			this.response = response;
			this.address = address;
			this.success = success;
		}

		public DateTime Request {
			get { return request; }
		}
		public DateTime Response {
			get { return response; }
		}
		public TimeSpan Duration {
			get { return response - request; }
		}
		public string Address {
			get { return address; }
		}
		public bool Success {
			get { return success; }
		}
	}

	
	public class DownloadTaskMonitor {
		private ConcurrentBag<DownloadTaskItem> archive_table;
		private DownloadTaskItemActive[] active_table;

		public DownloadTaskMonitor(int threadPoolSize) {
			archive_table = new ConcurrentBag<DownloadTaskItem>();
			active_table = new DownloadTaskItemActive[threadPoolSize];
		}

		public DownloadTaskMonitor(DownloadTaskMonitor oldMonitor, int threadPoolSize) {
			this.archive_table = oldMonitor.archive_table;
			active_table = new DownloadTaskItemActive[threadPoolSize];
		}

		public IEnumerable<DownloadTaskItem> GetDownloadedList() {
			return archive_table.AsEnumerable();
		}

		public IEnumerable<DownloadTaskItemActive> GetActiveDownloadList() {
			return active_table.AsEnumerable();
		}

		internal void DownloadStarted(int worker_id, DateTime request, string address) {
			var new_item = new DownloadTaskItemActive(request, address);
			active_table[worker_id] = new_item;
		}

		internal void DownloadFinished(int worker_id, DateTime response, bool success) {
			archive_table.Add(active_table[worker_id].Finilize(response, success));
			active_table[worker_id] = null;
		}
	}
}
