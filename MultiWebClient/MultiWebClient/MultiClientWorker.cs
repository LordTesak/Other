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

		static private void WorkerMain(MultiWebClient client, int id) {
			client.workers_client[id] = new ProxyClientWorker(client);

			DownloadTask task;
			while(true) {
				if(client.main_queue.TryDequeue(out task)) {
					client.DownloadMonitor.DownloadStarted(id, DateTime.Now, task.Address);
					WorkerProcess(client, id, task);
					bool success = (task.Exception == null);
					client.DownloadMonitor.DownloadFinished(id, DateTime.Now, success);
				} else {
					Util.LockedWait(client.MonitorWorker, 10000);
				}
			}
		}

		static private void WorkerProcess(MultiWebClient client, int id, DownloadTask task) {
			string result;
			try {

				if (task.Get) {
					result = client.workers_client[id].DownloadString(task.Address);
				} else {
					result = client.workers_client[id].UploadString(task.Address, task.Data);
				}
				task.Result = result;
				ProcessIngamePage(client, result);

			} catch(Exception ex) {
				task.Exception = ex;
			} finally {
				Interlocked.Decrement(ref client.pending_downloads);
			}
			
		}

		static private void ProcessIngamePage(MultiWebClient client, string page) {
			client.Controller.ProcessPage(page);
		}
		
	}


	class ProxyClientWorker : WebClient {

		private MultiWebClient client;
		private CookieContainer Cookies
		{
			get { return client.Cookies; }
			set { client.Cookies = value; }
		}
		private WebException exception;
		private bool web_exception;
		private bool page_redecode;

		public ProxyClientWorker(MultiWebClient client) {

			this.client = client;
			this.web_exception = false;

			this.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
			this.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate,lzma,sdch");
			this.Headers.Add(HttpRequestHeader.AcceptLanguage, "cs-CZ,cs;q=0.8");
			this.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2062.122 Safari/537.36 OPR/24.0.1558.64");
		}
		
		protected override WebResponse GetWebResponse(WebRequest request) {
			WebResponse result;
			try {
				result = base.GetWebResponse(request);
			} catch(WebException ex) {
				web_exception = true;
				exception = ex;
				return null;
			}
			if(!client.Controller.LogInCheck(result)
				 /*result.ResponseUri.AbsolutePath == "/login.php"*/) {
				client.LogIn();
				throw new NotLoggedInException(true);
			}
			var new_cookie = result.Headers.Get("Set-Cookie");
			this.Headers.Add(HttpRequestHeader.Cookie, new_cookie);
			request.ContentType = result.ContentType;

			var content_type = result.Headers.Get("Content-Type");
			page_redecode = true;
			if (content_type != null && content_type.IndexOf("charset") != -1)
				page_redecode = false;

			try {
				this.Encoding = Encoding.GetEncoding(((HttpWebResponse)result).CharacterSet);
			} catch (ArgumentException) {
				this.Encoding = Encoding.GetEncoding("ISO-8859-1");
			}

			return result;
		}

		protected override WebRequest GetWebRequest(Uri address) {
			var request = (HttpWebRequest)base.GetWebRequest(address);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.Timeout = 30 * 1000; // 30 seconds is enough in most cases
			request.Referer = address.AbsoluteUri;
			request.CookieContainer = Cookies;
			return request;
		}

		public new string DownloadString(string uri) {
			while(true) {
				try {
					client.CheckLogin();
					var result = base.DownloadString(uri);
					result = DecodeString(result);
					return result;
				} catch(NotLoggedInException ex) {
					//Záznamy.Zapiš(ex.Message, Záznamy.Závažnost.VnitřníChyba);
					continue;
				} catch(WebException ex) {
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}
			}
		}
		
		public new string UploadString(string uri, string data) {
			while(true) {
				try {
					client.CheckLogin();
					this.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
					var result = base.UploadString(uri, data);
					result = DecodeString(result);
					return result;
				} catch(NotLoggedInException ex) {
					//Záznamy.Zapiš(ex.Message, Záznamy.Závažnost.VnitřníChyba);
					continue;
				} catch(WebException ex) {
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}
			}
		}

		private string DecodeString(string page) {
			string result = page;
			result = WebUtility.HtmlDecode(result);
			
			try {
				if (page_redecode) {
					byte[] bytes = this.Encoding.GetBytes(page);

					page = page.ToLower();
					int begin = page.IndexOf("http-equiv=\"content-type\"");
					int end = begin;
					string content_type = Util.ParseString(page, ref begin, "content=\"", ref end, '\"');
					begin = content_type.IndexOf("charset=") + "charset=".Length;
					end = content_type.IndexOf(';', begin);
					if (end == -1) {
						end = content_type.Length;
					}
					string charset = content_type.Substring(begin, end - begin);
					result = Encoding.GetEncoding(charset).GetString(bytes);
				}
			} catch (Exception) {
				//Záznamy.Zapiš(ex.Message, Záznamy.Závažnost.VnějšíChyba);
			}
			return result;

		}
		
	}

}
