using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiWeb;

namespace ExchangeRates {
	class Program {
		class Currency : IComparable<Currency> {
			public string Code;

			public Currency(string code) {
				this.Code = code;
			}

			public int CompareTo(Currency other) {
				return Code.CompareTo(other.Code);
			}

			public static explicit operator Currency(string currency) {
				return new Currency(currency);
			}

			public override string ToString() {
				return Code;
			}
		}

		struct ExchangeRate {
			public Currency Currency;
			public decimal BaseRate;

			public override string ToString() {
				return string.Format("{0} {1}", Currency, BaseRate);
			}
		}

		static List<ExchangeRate> lineStatus = new List<ExchangeRate>();
		static SortedList<Currency, Currency> allCurrencies = new SortedList<Currency, Currency>();
		static List<Tuple<DateTime, SortedList<Currency, decimal>>> exchangeRates = new List<Tuple<DateTime, SortedList<Currency, decimal>>>();
		static IFormatProvider czechCulture = new System.Globalization.CultureInfo("cs-CZ");
		static short min_year = 1991;
		static short max_year = 2017;

		static void Main(string[] args) {

			string[] pages = DownloadExchangeRates();
			ParseDownloadedData(pages);
			var changes = CalculateGrowthRates();
			FindAndPrintBestSolution(changes);

			Console.ReadLine();
		}

		static string[] DownloadExchangeRates() {
			string cnb_url_format = "https://www.cnb.cz/cs/financni_trhy/devizovy_trh/kurzy_devizoveho_trhu/rok.txt?rok={0}";
			string[] pages = new string[max_year - min_year + 1];
			DownloadTaskId[] pages_id = new DownloadTaskId[max_year - min_year + 1];
			MultiWebClient client = new MultiWebClient();

			for (int i = 0; i < max_year - min_year + 1; i++) {
				pages_id[i] = client.GetPageAsync(string.Format(cnb_url_format, min_year + i));
			}
			for (int i = 0; i < max_year - min_year + 1; i++) {
				Console.WriteLine("Downloaded exchange rates for {0}", min_year + i);
				pages[i] = client.ReturnPage(pages_id[i]);
			}
			Console.WriteLine("Everything Downloaded Successfully");
			client.AbortDownloadingThreads();
			return pages;
		}

		static void ParseDownloadedData(string[] pages) {
			foreach (var page in pages) {
				string[] lines = page.Split('\n');
				foreach (var line in lines) {
					ProcessLine(line);
				}
			}
			Console.WriteLine("Data Processed");
		}

		static List<List<decimal>> CalculateGrowthRates() {
			List<Task> tasks = new List<Task>();
			List<List<decimal>> changes = new List<List<decimal>>();

			foreach (var currency in allCurrencies) {
				Task<List<decimal>> task = Task<List<decimal>>.Factory.StartNew(() => CalculateRateChanges(currency.Value));
				tasks.Add(task);
			}

			foreach (Task<List<decimal>> task in tasks) {
				task.Wait();
				changes.Add(task.Result);
			}

			Console.WriteLine("Data Calculated");
			return changes;
		}

		static void FindAndPrintBestSolution(List<List<decimal>> changes) {
			List<int> exchanges = new List<int>();
			decimal cumulative_increase = 1;
			int last_currency = -1;
			for (int i = 0; i < exchangeRates.Count - 1; i++) {
				var best_buy = GetMax(i, changes);

				string s_date = exchangeRates[i].Item1.ToString("dd.mm.yyyy");
				if (best_buy.Item2 <= 1) { // No exchange rate is going to increase, best is to stay at CZK
					exchanges.Add(-1);

					last_currency = -1;
					Console.WriteLine("{0} STAY AT \'CZK\' (TOTAL: {1})",
						s_date, cumulative_increase);
				} else {
					exchanges.Add(best_buy.Item1);
					cumulative_increase *= best_buy.Item2;

					if (last_currency == best_buy.Item1) // Stay at current currency
						Console.WriteLine("{0} STAY AT \'{1}\' INCREASE EXPECTED +{2:00.000}% (TOTAL: {3})",
							s_date, allCurrencies.ElementAt(best_buy.Item1).Key, (best_buy.Item2 - 1) * 100, cumulative_increase);
					else // Sell last currency and buy new one
						Console.WriteLine("{0} BUY     \'{1}\' INCREASE EXPECTED +{2:00.000}% (TOTAL: {3})",
							s_date, allCurrencies.ElementAt(best_buy.Item1).Key, (best_buy.Item2 - 1) * 100, cumulative_increase);
					last_currency = best_buy.Item1;

				}
			}
			Console.WriteLine("Exchanges processed");
			Console.WriteLine("Final earnings: {0:N}", cumulative_increase);
		}

		static List<decimal> CalculateRateChanges(Currency currency) {
			List<decimal> changes = new List<decimal>();
			decimal last_rate = -1;
			foreach (var day in exchangeRates) {
				decimal current_rate;
				if (day.Item2.TryGetValue(currency, out current_rate)) {
					if (last_rate != -1) {
						changes.Add(current_rate / last_rate);
					}
					last_rate = current_rate;
				} else {
					changes.Add(-1);
				}
			}
			return changes;
		}

		static Tuple<int, decimal> GetMax(int iteration, List<List<decimal>> data) {
			int index_max = -1;
			decimal value_max = 0;
			for (int i = 0; i < data.Count; i++) {
				if (data[i][iteration] > value_max) {
					value_max = data[i][iteration];
					index_max = i;
				}
			}
			return new Tuple<int, decimal>(index_max, value_max);
		}

		static void ProcessLine(string line) {
			if (line == "")
				return;

			string[] cells = line.Split('|');
			if (cells[0] == "Datum") {
				ChangeLineState(cells);
			} else {
				AddExchangeRateData(cells);
			}
		}

		static void ChangeLineState(string[] cells) {
			lineStatus = new List<ExchangeRate>();
			foreach (var cell in cells) {
				if (cell == "Datum")
					continue;
				string[] tmp = cell.Split(' ');
				Currency currency_code;
				if (allCurrencies.TryGetValue((Currency)tmp[1], out currency_code)) {

				} else {
					currency_code = new Currency(tmp[1]);
					allCurrencies.Add(currency_code, currency_code);
				}
				ExchangeRate rate_info = new ExchangeRate();
				rate_info.Currency = currency_code;
				rate_info.BaseRate = 1 / decimal.Parse(tmp[0]);
				lineStatus.Add(rate_info);
			}
		}

		static void AddExchangeRateData(string[] cells) {
			string[] s_date = cells[0].Split('.');
			DateTime date = new DateTime(day: int.Parse(s_date[0]), month: int.Parse(s_date[1]), year: int.Parse(s_date[2]));
			var currency_enumerator = lineStatus.GetEnumerator();
			SortedList<Currency, decimal> rates_data = new SortedList<Currency, decimal>();

			for (int i = 1; i < cells.Length; i++) {
				currency_enumerator.MoveNext();
				decimal _rate = decimal.Parse(cells[i], provider: czechCulture);
				ExchangeRate rate = currency_enumerator.Current;
				rates_data.Add(rate.Currency, rate.BaseRate * _rate);
			}

			exchangeRates.Add(new Tuple<DateTime, SortedList<Currency, decimal>>(date, rates_data));
		}
	}
}
