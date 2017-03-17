using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiWeb {
	public static class Util {

		public static void Populate<T>(ref T[] array, T value) {
			for (int i = 0; i < array.Length; i++) {
				array[i] = value;
			}
		}

		#region Interlocked
		public static void LockedWait(object o) {
			lock (o)
				System.Threading.Monitor.Wait(o);
		}
		public static void LockedWait(object o, int ms) {
			lock (o)
				System.Threading.Monitor.Wait(o, ms);
		}
		public static void LockedPulseAll(object o) {
			lock (o)
				System.Threading.Monitor.PulseAll(o);
		}
		public static void LockedPulse(object o) {
			lock (o)
				System.Threading.Monitor.Pulse(o);
		}
		#endregion

		#region String Parsing
		public static string ParseString(string text, ref int begin, string begin_tag, ref int end, string end_tag) {
			begin = text.IndexOf(begin_tag, end) + begin_tag.Length;
			end = text.IndexOf(end_tag, begin);
			return text.Substring(begin, end - begin);
		}
		public static string ParseString(string text, ref int begin, char begin_tag, ref int end, string end_tag) {
			begin = text.IndexOf(begin_tag, end) + 1;
			end = text.IndexOf(end_tag, begin);
			return text.Substring(begin, end - begin);
		}
		public static string ParseString(string text, ref int begin, string begin_tag, ref int end, char end_tag) {
			begin = text.IndexOf(begin_tag, end) + begin_tag.Length;
			end = text.IndexOf(end_tag, begin);
			return text.Substring(begin, end - begin);
		}
		public static string ParseString(string text, ref int begin, char begin_tag, ref int end, char end_tag) {
			begin = text.IndexOf(begin_tag, end) + 1;
			end = text.IndexOf(end_tag, begin);
			return text.Substring(begin, end - begin);
		}
		public static string ParseStringReverse(string text, ref int begin, string begin_tag, ref int end, string end_tag) {
			end = text.IndexOf(end_tag, end);
			begin = text.LastIndexOf(begin_tag, end) + begin_tag.Length;
			return text.Substring(begin, end - begin);
		}
		public static string ParseStringReverse(string text, ref int begin, char begin_tag, ref int end, string end_tag) {
			end = text.IndexOf(end_tag, end);
			begin = text.LastIndexOf(begin_tag, end) + 1;
			return text.Substring(begin, end - begin);
		}
		public static string ParseStringReverse(string text, ref int begin, string begin_tag, ref int end, char end_tag) {
			end = text.IndexOf(end_tag, end);
			begin = text.LastIndexOf(begin_tag, end) + begin_tag.Length;
			return text.Substring(begin, end - begin);
		}
		public static string ParseStringReverse(string text, ref int begin, char begin_tag, ref int end, char end_tag) {
			end = text.IndexOf(end_tag, end);
			begin = text.LastIndexOf(begin_tag, end) + 1;
			return text.Substring(begin, end - begin);
		}
		#endregion
	}
}
