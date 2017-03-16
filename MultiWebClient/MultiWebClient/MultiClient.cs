using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;

using System.Globalization;

namespace MultiWeb {

	public partial class MultiWebClient {
		private short workerCount;
		private long nextId;
		private long pending_downloads;
		//private string login;
		//private string password;
		public IMultiWebClientController Controller;
		private CookieContainer cookies;
		private ConcurrentQueue<DownloadTask> main_queue;
		private ConcurrentDictionary<DownloadTaskId, DownloadTask> download_table;
		
		private bool logged_in;
		private bool wrong_password;
		private object monitor_cookies = new object();
		private object monitor_worker = new object();
		private object monitor_logging = new object();
		private object monitor_login = new object();
		private object monitor_add = new object();
		private object monitor_pending_downloads = new object();

		private Thread[] workers_thread;
		private ProxyClientWorker[] workers_client;

		public DownloadTaskMonitor DownloadMonitor;

		public object MonitorPendingDownloads {
			get { return monitor_pending_downloads; }
		}

		public object MonitorWorker {
			get { return monitor_worker; }
		}

		public object MonitorLogin {
			get { return monitor_login; }
		}

		public CookieContainer Cookies {
			get {
				lock (monitor_cookies) {
					return cookies;
				}
			}
			set {
				lock (monitor_cookies) {
					cookies = value;
				}
			}
		}

		public void CheckLogin() {
			if (!wrong_password && !logged_in)
				LogIn();
			while (!logged_in) {
				Util.LockedWait(monitor_login);
			}
		}

		public MultiWebClient() : this(new NoLoginController()) {
			LogIn();
		}

		public MultiWebClient(IMultiWebClientController controller) {
			this.workerCount = 16;
			this.Controller = controller;
			this.download_table = new ConcurrentDictionary<DownloadTaskId, DownloadTask>();
			this.main_queue = new ConcurrentQueue<DownloadTask>();
			this.workers_client = new ProxyClientWorker[workerCount];
			this.workers_thread = new Thread[workerCount];
			this.cookies = new CookieContainer();
			this.logged_in = false;
			this.wrong_password = true;
			this.DownloadMonitor = new DownloadTaskMonitor(workerCount);

			ReactivateDownloadingThreads();
		}

		public void AbortDownloadingThreads() {
			foreach (var thread in workers_thread) {
				if (thread != null)
					thread.Abort();
			}
		}

		public void ReactivateDownloadingThreads() {
			AbortDownloadingThreads();
			for (int i = 0; i < workerCount; i++) {
				StartWorkingThread(i);
				//int j = i;
				//workers_thread[i] = new Thread(() => WorkerMain(this, j));
				//workers_thread[i].Name = string.Format("{0}-Worker: {1}", this.GetHashCode(), i);
				//workers_thread[i].IsBackground = true;
				//workers_thread[i].Start();
			}
		}

		public void ReactivateDownloadingThreads(int threadPoolSize) {
			if(threadPoolSize <= 0 || threadPoolSize > 2048) {
				throw new ArgumentOutOfRangeException("Thread pool size must be between 1 and 2048");
			}
			AbortDownloadingThreads();
			workers_client = new ProxyClientWorker[threadPoolSize];
			workers_thread = new Thread[threadPoolSize];
			workerCount = (short)threadPoolSize;
			DownloadMonitor = new DownloadTaskMonitor(DownloadMonitor, workerCount);
			
			for (int i = 0; i < workerCount; i++) {
				StartWorkingThread(i);
				//int j = i;
				//workers_thread[i] = new Thread(() => WorkerMain(this, j));
				//workers_thread[i].Name = string.Format("{0}-Worker: {1}", this.GetHashCode(), i);
				//workers_thread[i].IsBackground = true;
				//workers_thread[i].Start();
			}
		}

		private void StartWorkingThread(int id) {
			int j = id;
			workers_thread[id] = new Thread(() => WorkerMain(this, j));
			workers_thread[id].Name = string.Format("{0}-Worker: {1}", this.GetHashCode(), id);
			workers_thread[id].IsBackground = true;
			workers_thread[id].Start();
		}

		public DownloadTaskId GetPageAsync(string url) {
			DownloadTask task = new DownloadTask(url);
			DownloadTaskId taskId;

			lock (monitor_add) {
				download_table.TryAdd(nextId, task);
				Thread.MemoryBarrier();
				main_queue.Enqueue(task);
				Interlocked.Increment(ref pending_downloads);
				Util.LockedPulseAll(MonitorWorker);
				taskId = nextId++;
			}

			return taskId;
		}

		public DownloadTaskId PostPageAsync(string url, string data) {
			DownloadTask task = new DownloadTask(url, data);
			DownloadTaskId taskId;

			lock (monitor_add) {
				download_table.TryAdd(nextId, task);
				Thread.MemoryBarrier();
				main_queue.Enqueue(task);
				Interlocked.Increment(ref pending_downloads);
				Util.LockedPulseAll(MonitorWorker);
				taskId = nextId++;
			}

			return taskId;
		}

		public string GetPage(string url) {
			return ReturnPage(GetPageAsync(url));
		}

		public string PostPage(string url, string data) {
			return ReturnPage(PostPageAsync(url, data));
		}

		public string ReturnPage(DownloadTaskId id) {
			DownloadTask result;
			download_table.TryRemove(id, out result);
			return result.Result;
		}
		
		public void WaitAllDownloads() {
			while (true) {
				if (pending_downloads == 0)
					return;
				else
					Util.LockedWait(MonitorPendingDownloads, 1000);
			}
		}

		public bool LogIn() {
			if (System.Threading.Monitor.TryEnter(monitor_logging)) {
				lock (monitor_cookies) {
					while (true) {
						try {
							bool logged = Controller.LogIn();
							if (logged) {
								logged_in = true;
								wrong_password = false;
								Util.LockedPulseAll(monitor_login);
							} else {
								logged_in = false;
								wrong_password = true;
								throw new Exception();
							}

							System.Threading.Monitor.Exit(monitor_logging);
							return logged;
						} catch (WebException ex) {
							//Záznamy.Zapiš(ex.Message, Záznamy.Závažnost.Chyby);
						}
					}
				}
			} else {
				return false;
			}
		}

	}

	class NotLoggedInException : Exception {
		public NotLoggedInException(bool disconnected) : base("Nejste přihlášen do hry.") {
			this.reconnected = !disconnected;
		}
		public bool Reconnected {
			get { return reconnected; }
		}
		private bool reconnected;
	}
}
