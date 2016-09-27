using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.Xml.Linq;

namespace iphoneAva
{
	class MainClass
	{
		public static Dictionary<string, string> StoreDict { get; private set; }
		public static Dictionary<string, string> CaliStoreDict { get; private set; }
		private static HashSet<string> caliNum = new HashSet<string> { "R071", "R414","R057","R014", "R240"
			,"R105","R039","R052", "R002", "R099","R231", "R029", "R033","R075","R217"};

		public static Dictionary<string, Dictionary<string, string>> allPhoneDict = new Dictionary<string, Dictionary<string, string>>();

		private static String TMOBILE = "MNAQ2LL/A";
		private static String VERIZON = "MNC62LL/A";

		private static String TEST_TMOBILE = "MN9W2LL/A";
		private static String TEST_VERIZON = "MNAQ2LL/A";

		private const String AVAILABILITY_URL = "https://reserve.cdn-apple.com/US/en_US/reserve/iPhone/availability.json";
		private const String STORE_URL = "https://reserve.cdn-apple.com/US/en_US/reserve/iPhone/stores.json";

	    private static String STATE = "California";

		private static bool isTest = false;

		public static void Main()
		{
			String storeJson = getJson(STORE_URL);
			readModelXml();
			readStoreJson(storeJson,isTest);
			//var correct = true;
			Console.WriteLine("JB in Cali?  Y/N");
			String testInput = Console.ReadLine().ToLower();
			isTest = !(testInput == "y");

			//if (correct && !isTest)
			//{
			//	Console.WriteLine("Which State?  please enter California");
			//	String state = Console.ReadLine();

			//	Console.WriteLine("iPhone7 or iPhone7 Plus ?  please enter 7/7+");
			//	String iphone = Console.ReadLine();

			//	Console.WriteLine("What color? please enter: JB/B/R/G/S");
			//	String color = Console.ReadLine();

			//	Console.WriteLine("What size? please enter 32/128/256");
			//	String size = Console.ReadLine();

			//	Console.WriteLine("What carrier? please enter TMO/VER/ATT/SPT");
			//	String carrier = Console.ReadLine();

			//	Console.WriteLine(" So you want a {1} {0} in {2} correct? Y/N", iphone, color, size);
			//	string isCorrect = Console.ReadLine();
			//	correct = (isCorrect == "Y");
			//	Console.WriteLine("Model number confirmed: " + getModel(iphone, color, size, carrier));
			//	Console.WriteLine();
			//}


			while (true)
			{
				String phoneJson = getJson(AVAILABILITY_URL);
				readIphoneJson(phoneJson, isTest);
				Thread.Sleep(5000);
			}

		}

		static string getModel(string iphone, string color, string size, string carrier)
		{
			//Plus128GBGold
			//JB/B/R/G/S
			//TMO / VER / ATT / SPT
			iphone = iphone == "7" ? "7" : "Plus";
				switch (color.ToLower())
				{
					case "jb":
						color = "Jet";
						break;
					case "b":
						color = "Black";
						break;
					case "r":
						color = "Rose";
						break;
					case "g":
						color = "Gold";
						break;
					case "s":
						color = "Silver";
						break;
			}
			carrier = carrier.ToLower();
			switch (carrier)
			{
				case "tmo":
					carrier = "TMOBILE";
					break;
				case "ver":
					carrier = "Verizon";
					break;
				case "att":
					carrier = "ATT";
					break;
				case "spt":
					carrier = "Sprint";
					break;
			}
			var key = iphone + size + "GB" + color;
			if (allPhoneDict.ContainsKey(carrier)) {
				if (allPhoneDict[carrier].ContainsKey(key)) {
					return allPhoneDict[carrier][key];
				}
			}
			return "No iphone model found :" + key;
		}

		private static string getJson(string URL)
		{
			using (WebClient wc = new WebClient())
			{
				var json = "";
				try
				{
					json = wc.DownloadString(URL);
					Console.WriteLine("Update time: " + System.DateTime.Now);
				}
				catch (WebException e)
				{
					Console.WriteLine(e);
				}
				return json;
			}
		}

		private static void readModelXml()
		{
			var document = XDocument.Load("../../Tmo.xml");
			allPhoneDict = document.Descendants("phone")
			                       .ToDictionary(d=>d.FirstAttribute.Value, d=>d.Elements()
			                       .ToDictionary(c => c.Attribute("KEY").Value, c => c.Attribute("MODEL").Value));
		}

		private static void readStoreJson(string json, bool isTest)
		{
			json = "[" + json + "]";
			var jArr = JArray.Parse(json);

			jArr.Descendants().OfType<JProperty>()
							  .Where(p => p.Name == "updatedTime" || p.Name == "supportedDomains" || p.Name == "timezone"
									 || p.Name == "updatedDate" || p.Name == "reservationURL")
							  .ToList()
							  .ForEach(att => att.Remove());
			
			var newJson = jArr.ToString().Remove(0, 2);
			newJson = newJson.Remove(newJson.Length - 2, 2);

			StoreList obj = JsonConvert.DeserializeObject<StoreList>(newJson);

			StoreDict = obj.stores.ToDictionary(c => c.storeNumber, c => c.storeName);
			CaliStoreDict = isTest? StoreDict : obj.stores.Where(c => c.storeState == STATE ).ToDictionary(c => c.storeNumber, c => c.storeName);

		}

		private static void readIphoneJson(string json, bool isTest)
		{
			
			String reg = isTest ? "(\"MNC62LL/A\" : \"ALL\")|(\"MNAQ2LL/A\" : \"ALL\")|((R)+(\\d\\d\\d))" : "(\"MN5X2LL/A\" : \"ALL\")|(\"MN5L2LL/A\" : \"ALL\")|((R)+(\\d\\d\\d))";
			String verizonModel = isTest ? "\"MNC62LL/A\" : \"ALL\"" : "\"" +VERIZON + " : \"ALL\"";
			String tmoileModel = isTest ? "\"MNAQ2LL/A\" : \"ALL\"" : "\"" + TMOBILE +" : \"ALL\"";

			var regex = new Regex(reg);
			var inCali = false;
			var count = 0;
			var storeNum = "";

			MatchCollection matches = regex.Matches(json);
			if (matches.Count > 0)
			{
				foreach (Match match in matches)
				{
					if (isStoreNum(match.Value))
					{
						//is store number
						if (!isStoreNum(match.NextMatch().Value))
						{
							var statment = isTest ? CaliStoreDict.ContainsKey(match.Value) : CaliStoreDict.ContainsKey(match.Value) && caliNum.Contains(match.Value);
							//has stock
							if (statment)
							{
								//in sfo
								Console.WriteLine();
								Console.WriteLine(StoreDict[match.Value]);
								storeNum = match.Value;
								count++;
								inCali = true;
							}
						}
						else {
							//not in sfo
							inCali = false;
						}
					}
					else {
						//is not store number
						if (inCali)
						{
							if (match.Value == verizonModel)
							{
								Console.WriteLine("     Verizon  " +linkBuilder(VERIZON, "Verizon", storeNum, isTest));
							}
							else if (match.Value == tmoileModel)
							{
								Console.WriteLine("     Tmoible  " + linkBuilder(TMOBILE, "T-Mobile", storeNum, isTest));

							}
							inCali &= !isStoreNum(match.NextMatch().Value);
						}
						else {
							continue;
						}
					}

				}
				Console.WriteLine("Total store in stock: " + count);
				if (count > 0) { Console.Beep();}
				count = 0;
				Console.WriteLine("------------------------------------------------------");
				       
			}
		}
	

		public static bool isStoreNum(string number)
		{
			return StoreDict.ContainsKey(number);
		}

		public static string linkBuilder(string model, string carrier, string storeNumber , bool isTest)
		{
			//https://reserve-us.apple.com/US/en_US/reserve/iPhone?channel=1&rv=0&path=&sourceID=email&iPP=U&appleCare=Y&iUID=&iuToken=&partNumber=MNAQ2LL/A&carrier=Sprint&store=R099
			//https://reserve-us.apple.com/US/en_US/reserve/iPhone?channel=1&rv=0&path=&sourceID=email&iPP=U&appleCare=Y&iUID=&iuToken=&carrier=Sprint&store=R099&partNumber=MNCA2LL%2FA
			string modelPart = "&partNumber=" ;
			if (carrier == "Verizon")
			{
				modelPart += isTest ? TEST_VERIZON : VERIZON;
			}
			else {
				modelPart += isTest ? TEST_TMOBILE : TMOBILE;
			}

			string carrierPart = "&carrier=" + carrier;
			string storePart = "&store=" + storeNumber;
			return  "https://reserve-us.apple.com/US/en_US/reserve/iPhone?channel=1&rv=0&path=&sourceID=email&iPP=U&appleCare=Y&iUID=&iuToken=" + modelPart + carrierPart + storePart;

		}
	}
}
