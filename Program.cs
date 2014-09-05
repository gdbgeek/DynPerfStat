using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Configuration;
using System.Threading;
using System.Linq;
using System.Text;
using Procurios.Public;

namespace DynPerfStat
{
    class Program
    {
        

        static void Main(string[] args)
        {
            string home = Directory.GetCurrentDirectory();

            //ServicePointManager.ServerCertificateValidationCallback += (sender, ICertificatePolicy, chain, sslPolicyErrors) => true;
            ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };

            string ExeFriendlyName = System.AppDomain.CurrentDomain.FriendlyName;
            string[] ExeNameBits = ExeFriendlyName.Split('.');
            string ExeName = ExeNameBits[0];

            bool debug = false;
            bool proxy = false;

            string Tmapservice = "";
            string TMSName = "";
            string Txy = "";
            string Tx = "";
            string Ty = "";
            string TImageWidth = "";
            string TImageHeight = "";
            string TScaleDefault = "500000";
            string[] TScales = TScaleDefault.Split(';');
            string TDpi = "";
            string TFormat = "";
            string TNode = "";
            string TThink = "";
            string TLayers = "";
            string TTokenURL = "";
            string TLogsURL = "";
            string TUser = "";
            string TPassword = "";

            int c = args.GetUpperBound(0);

            // Loop through arguments
            for (int n = 0; n < c; n++)
            {
                string thisKey = args[n].ToLower();
                string thisVal = args[n + 1].TrimEnd().TrimStart();

                // eval the key
                switch (thisKey)
                {
                    case "-xy":
                        Txy = thisVal;
                        string[] coord = thisVal.Split(';');
                        Tx = coord[0];
                        Ty = coord[1];
                        break;
                    case "-mapserver":
                        Tmapservice = thisVal;
                        break;
                    case "-scale":
                        TScales = thisVal.Split(';');
                        break;
                    case "-width":
                        TImageWidth = thisVal;
                        break;
                    case "-height":
                        TImageHeight = thisVal;
                        break;
                    case "-dpi":
                        TDpi = thisVal;
                        break;
                    case "-format":
                        TFormat = thisVal;
                        break;
                    case "-node":
                        TNode = thisVal;
                        break;
                    case "-layers":
                        TLayers = thisVal;
                        break;
                    case "-think":
                        TThink = thisVal;
                        break;
                    case "-debug":
                        string dbg = thisVal;
                        if (dbg.ToUpper() == "Y") debug = true;
                        break;
                    case "-proxy":
                        string prx = thisVal;
                        if (prx.ToUpper() == "Y") proxy = true;
                        break;
                    case "-user":
                        TUser = thisVal;
                        break;
                    case "-password":
                        TPassword = thisVal;
                        break;
                    case "-tokenurl":
                        TTokenURL = thisVal;
                        break;
                    case "-logsurl":
                        TLogsURL = thisVal;
                        break;
                    default:
                        break;                    
                }
            }

            if (Txy == "") return;
            if (Tmapservice == "") return;

            string [] temp_ms = Tmapservice.Split('/');
            TMSName = temp_ms[temp_ms.GetUpperBound(0) - 1];

            double x = 0;
            double y = 0;
            int dpi = 96;
            int ImageWidth = 800;
            int ImageHeight = 400;
            string Format = "png32";
            string Node = "";
            int Think = 0;
            string layer_ids = "";
            string Layers = "D";
            int LayerCount = 0;
            
            string SToken = "";
            string PToken = "";

            string mapservice = Tmapservice;

            WebClient client = new WebClient();
            client.Credentials = CredentialCache.DefaultCredentials;

            if (proxy == true)
            {
                client.Proxy = WebRequest.DefaultWebProxy;
                client.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }

            if (TTokenURL != "" && TPassword != "" && TUser != "")
            {
                if (TTokenURL.ToLower().Contains("sharing"))
                {
                    PToken = GetToken(TTokenURL, TUser, TPassword);
                }

                if (TTokenURL.ToLower().Contains("tokens"))
                {
                    SToken = GetToken(TTokenURL, TUser, TPassword);
                }

                if (PToken.Contains("Token Error:") || SToken.Contains("Token Error:"))
                {
                    Console.WriteLine(SToken);
                    Console.WriteLine(PToken);
                    Environment.Exit(-1);
                }

                if (PToken != "" && SToken == "")
                {
                    SToken = GetFederatedToken(TTokenURL, PToken);
                }

                if (debug == true) Console.WriteLine("PToken:" + PToken);
                if (debug == true) Console.WriteLine("SToken:" + SToken);
            }

            string json = "";

            try
            {
                if (SToken != "")
                {
                   json = client.DownloadString(new Uri(mapservice + "?f=json&token=" + SToken));
                }
                else
                {
                    json = client.DownloadString(new Uri(mapservice + "?f=json"));
                }
            }
            catch (WebException webEx)
            {                
                Console.WriteLine(webEx.Message);
                Environment.Exit(-1);
            }

            if (json.ToLower().Contains("error"))
            {
                Console.WriteLine(json);
                Environment.Exit(-1);
            }

            if (TLayers != "") Layers = TLayers;

            if (Layers != "D")
            {
                Hashtable root;
                ArrayList layers;

                root = (Hashtable)Procurios.Public.JSON.JsonDecode(json);
                layers = (ArrayList)root["layers"];                

                LayerCount = layers.Count;

                for (int n = 0; n < LayerCount; n++)
                {
                    if (layer_ids != "") layer_ids = layer_ids + ",";
                    layer_ids = layer_ids + n.ToString();
                }
            }

            if (layer_ids == "") layer_ids = "0";          

            if (Tx != "" && Ty != "")
            {
                x = Convert.ToDouble(Tx);
                y = Convert.ToDouble(Ty);
            }
            
            if (TImageWidth != "") ImageWidth = Convert.ToInt32(TImageWidth);
            if (TImageHeight != "") ImageHeight = Convert.ToInt32(TImageHeight);
            if (TDpi != "") dpi = Convert.ToInt32(TDpi);
            if (TFormat != "") Format = TFormat;
            if (TNode != "") Node = TNode;
            if (TThink != "") Think = Convert.ToInt32(TThink);

            Console.WriteLine(mapservice + "\t" + TMSName);
            if (debug == true) Console.WriteLine("DPI:" + dpi);
            if (debug == true) Console.WriteLine("ImageFormat: " + Format);
            if (debug == true) Console.WriteLine("ImageHeight: " + ImageWidth);
            if (debug == true) Console.WriteLine("ImageWidth: " + ImageWidth);
            if (debug == true) Console.WriteLine("");

            Console.WriteLine("Scale\tTotal Time (ms)\tDownload Time (ms)\tApprox Service Time (ms)\tActual Service Time (ms)\tFile Size (KB)");

            foreach (string TScale in TScales)
            {
                double scale = Convert.ToDouble(TScale);

                /* dividing image width by DPI to get it in Inch */
                double dblImgWidthInInch = ImageWidth / dpi;
                double dblImgHeightInInch = ImageHeight / dpi;

                /* converting Inch to meter (assume the map is in meter) */
                double dblImgWidthInMapUnit = dblImgWidthInInch * 0.0254;
                double dblImgHeightInMapUnit = dblImgHeightInInch * 0.0254;

                /* calculating half of map’s height & width at the specific scale */
                double dX = (dblImgWidthInMapUnit * scale) / 2;
                double dY = (dblImgHeightInMapUnit * scale) / 2;

                double XMin = x - dX;
                double XMax = x + dX;

                double YMin = y - dY;
                double YMax = y + dY;

                string imgurl = mapservice + "/export?";

                imgurl = imgurl + "dpi=" + dpi.ToString();
                imgurl = imgurl + "&size=" + ImageWidth.ToString() + "," + ImageHeight.ToString();
                imgurl = imgurl + "&format=" + Format;
                imgurl = imgurl + "&f=image";
                imgurl = imgurl + "&bbox=" + XMin.ToString() + "," + YMin.ToString() + "," + XMax.ToString() + "," + YMax.ToString();

                if (Layers == "A") imgurl = imgurl + "&layers=show:" + layer_ids;

                if (SToken != "") imgurl = imgurl + "&token=" + SToken;

                Stopwatch sw = Stopwatch.StartNew();

                double seconds_download_time = 0;

                Int64 bytesTotal = 0;
                Double FileSize = 0;

                try
                {
                    Stream stream = client.OpenRead(new Uri(imgurl));                    

                    using (FileStream fileStream = new FileStream(home + "\\" + ExeName + Node + scale.ToString() + "." + GetImageExtension(Format.ToUpper()), FileMode.Create))
                    {
                        var buffer = new byte[32768];
                        int bytesRead;
                        Int64 bytesReadComplete = 0;  // Use Int64 for files larger than 2 gb

                        // Get the size of the file to download
                        bytesTotal = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);

                        Stopwatch sw2 = Stopwatch.StartNew();

                        // Download file in chunks
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            bytesReadComplete += bytesRead;
                            fileStream.Write(buffer, 0, bytesRead);
                        }

                        sw2.Stop();

                        seconds_download_time = sw2.ElapsedMilliseconds;
                    } 

                    if (debug == true) Console.WriteLine(imgurl + " OK");
                }
                catch (WebException webEx)
                {
                    if (debug == true) Console.WriteLine(imgurl + " " + webEx.Message);                    
                }

                sw.Stop();

                double seconds = sw.ElapsedMilliseconds;

                try
                {
                    //Check if the file is bigger than 1K
                    FileInfo FI = new FileInfo(home + "\\" + ExeName + Node + scale.ToString() + "." + GetImageExtension(Format.ToUpper()));
                    long FL = FI.Length;
                    FileSize = FI.Length;
                    FileSize = Math.Ceiling(FileSize / 1024);

                    if (FL < 1000)
                    {
                        //Probably timeout or error
                        Console.WriteLine(scale.ToString() + "\terror\terror\terror\terror\t" + FileSize.ToString() );
                    }
                    else
                    {
                        string actual_service_time = "NA";

                        if (TLogsURL != "")
                        {
                            string filter = "{\"codes\":[100004],\"processIds\":[],\"server\": \"*\",\"services\": [\"" + TMSName + ".MapServer" + "\"],\"machines\": \"*\"}";

                            if (PToken != "") actual_service_time = GetServiceTime(TLogsURL, filter, PToken, FL); //Use Portal Token
                            if (SToken != "" && PToken == "") actual_service_time = GetServiceTime(TLogsURL, filter, SToken, FL); //Use Server Token

                            if (actual_service_time != "error")
                            {
                                double dactual_service_time = Convert.ToDouble(actual_service_time);
                                dactual_service_time = dactual_service_time * 1000;
                                actual_service_time = dactual_service_time.ToString();
                            }                            
                        }
                        
                        Console.WriteLine(scale.ToString() + "\t" + seconds.ToString() + "\t" + seconds_download_time.ToString() + "\t" + (seconds - seconds_download_time).ToString() + "\t" + actual_service_time + "\t" + FileSize.ToString());
                    }
                }
                catch
                {
                    //Nothing
                    Console.WriteLine(scale.ToString() + "\tfailure\tfailure\tfailure\tfailure\t" + FileSize.ToString());
                }

                if (debug == true) Console.WriteLine("Waiting " + Think.ToString() + " milliseconds.");

                Thread.Sleep(Think);
            }

            if (debug == true) Console.WriteLine("Done!");
        }

        public static string GetImageExtension(string imageFormat)
        {
            string result = "";

            switch (imageFormat)
            {
                case "PNG":
                case "PNG8":
                case "PNG24":
                case "PNG32":
                    result = "png";
                    break;
                case "GIF":
                    result = "gif";
                    break;
                case "JPEG":
                case "JPG":
                    result = "jpg";
                    break;
                default:
                    return "";
            }

            return result;
        }

        public static string GetToken(string tokenurl, string username, string password)
        {
            string url = "";

            if (username == "IWA" && password == "IWA")
            {
                url = tokenurl + "generateToken?request=getToken&f=json"; 
            }
            else
            {
                url = tokenurl + "generateToken?request=getToken&f=json&username=" + username + "&password=" + password;
            }

            System.Net.WebRequest request = System.Net.WebRequest.Create(url);
            request.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials; 

            string myToken = "";
            string myJSON = "";

            try
            {
                System.Net.WebResponse response = request.GetResponse();
                System.IO.Stream responseStream = response.GetResponseStream();
                System.IO.StreamReader readStream = new System.IO.StreamReader(responseStream);

                myJSON = readStream.ReadToEnd();

                Hashtable LogRecord = (Hashtable)JSON.JsonDecode(myJSON);

                myToken = (string)LogRecord["token"];                                                
            }

            catch (WebException we)
            {
                myToken = "Token Error: " + we.Message;
            }

            return myToken;
        }

        public static string GetFederatedToken(string tokenurl, string token)
        {
            string url = tokenurl + "generateToken?request=getToken&f=json&token=" + token; 

            System.Net.WebRequest request = System.Net.WebRequest.Create(url);
            request.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

            string myToken = "";
            string myJSON = "";

            try
            {
                System.Net.WebResponse response = request.GetResponse();
                System.IO.Stream responseStream = response.GetResponseStream();
                System.IO.StreamReader readStream = new System.IO.StreamReader(responseStream);

                myJSON = readStream.ReadToEnd();

                Hashtable LogRecord = (Hashtable)JSON.JsonDecode(myJSON);

                myToken = (string)LogRecord["token"];
            }

            catch (WebException we)
            {
                myToken = "Token Error: " + we.Message;
            }

            return myToken;
        }

        public static string GetServiceTime(string logsurl, string filter, string token, double filesize)
        {
            string imgurl = logsurl + "/query?";

            imgurl = imgurl + "startTime=";

            imgurl = imgurl + "&endTime=";
            imgurl = imgurl + "&level=FINE";
            imgurl = imgurl + "&filterType=json";
            imgurl = imgurl + "&filter=" + filter;

            imgurl = imgurl + "&pageSize=100";
            imgurl = imgurl + "&f=pjson";

            if (token != "")
            {
                imgurl = imgurl + "&token=" + token;
            }

            string response = "";
            WebClient client = new WebClient();
            client.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

            try
            {
                response = client.DownloadString(new Uri(imgurl));

                Hashtable root;
                ArrayList LogRecords;
                long LogRecordsCount = 0;

                root = (Hashtable)Procurios.Public.JSON.JsonDecode(response);

                LogRecords = (ArrayList)root["logMessages"];

                if (LogRecords == null)
                {
                    return "error";
                }

                LogRecordsCount = LogRecords.Count;

                int n;

                for (n = 0; n < LogRecordsCount; n++)
                {
                    Hashtable LogRecord = (Hashtable)LogRecords[n];

                    string message = (string)LogRecord["message"];
                    string elapsed = (string)LogRecord["elapsed"];
                    string methodName = (string)LogRecord["methodName"];

                    if (message.Contains("Response size is") && methodName == "/export")
                    {
                        string reported_size;

                        int pos1 = message.IndexOf(" characters");
                        int pos2 = message.IndexOf("is ");

                        reported_size = message.Substring(pos2+3, pos1 - pos2 -3);

                        if (int.Parse(reported_size) < filesize + 10 && int.Parse(reported_size) > filesize - 10) return elapsed;
                    }
                }

                return "error";
            }
            catch (WebException webEx)
            {
                return webEx.Message;
            }
        }
    }
}
