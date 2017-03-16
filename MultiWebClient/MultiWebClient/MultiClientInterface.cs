using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MultiWeb {
	public interface IMultiWebClientController {
		bool LogIn();
		void ProcessPage(string page);
		bool LogInCheck(WebResponse response);
	}

	public class NoLoginController : IMultiWebClientController {
		public bool LogIn() {
			return true;
		}

		public bool LogInCheck(WebResponse response) {
			return true;
		}

		public void ProcessPage(string page) {
			return;
		}
	}
}
