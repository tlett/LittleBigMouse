﻿using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace LbmScreenConfig
{
    class Html
    {
        public static string GetPnpName1(string pnpcode)
        {
            string html = GetHtml("http://listing.driveragent.com/c/pnp/" + pnpcode);

            if (html == null) return "";

            Match match = Regex.Match(html, "<span class=\"title2\">(.*?)</span>", RegexOptions.Singleline);
            if (match.Success)
            {
                string result = match.Groups[1].Value;
                if (result.Contains("Drivers")) result = result.Replace("Drivers", "");

                Match match2 = Regex.Match(result, @"\((.*?)\)", RegexOptions.Singleline);

                for (int i=1; i<match2.Groups.Count; i++)
                {

                    result = result.Replace("(" + match2.Groups[i].Value + ")", "");
                }

                result = result.Trim();

                return result;
            }
            return "";
        }

        public static string GetPnpName2(string pnpcode)
        {
            string html = GetHtml("http://www.driversdownloader.com/hardware-id/monitor/" + pnpcode.ToLower());

            if (html == null) return "";

            Match match = Regex.Match(html, "<b><p>(.*?)</p></b>", RegexOptions.Singleline);
            if (match.Success)
            {
                string result = match.Groups[1].Value;
                if (result.Contains("Drivers")) result = result.Replace("Drivers", "");

                Match match2 = Regex.Match(result, @"\((.*?)\)", RegexOptions.Singleline);

                for (int i = 1; i < match2.Groups.Count; i++)
                {

                    result = result.Replace("(" + match2.Groups[i].Value + ")", "");
                }

                result = result.Trim();

                return result;
            }
            return "";
        }

        public  static string GetPnpName(string pnpcode)
        {
            string result = GetPnpName("https://driverlookup.com/hardware-id/monitor/", "<p><span><a href=.*?>(.*?)</a></span>", pnpcode);

            //if (string.IsNullOrEmpty(result))
            //    result = GetPnpName("http://listing.driveragent.com/c/pnp/", "<span class=\"title2\">(.*?)</span>", pnpcode);

            //if (string.IsNullOrEmpty(result))
            //    result = GetPnpName("http://www.driversdownloader.com/hardware-id/monitor/", "<b><p>(.*?)</p></b>", pnpcode);

            return result;
        }


        public static string GetPnpName(string url, string regex, string pnpcode)
        {
            string html = GetHtml(url + pnpcode.ToLower());

            if (html == null) return "";

            Match match = Regex.Match(html, regex, RegexOptions.Singleline);
            if (match.Success)
            {
                string result = match.Groups[1].Value;
                if (result.Contains("Drivers")) result = result.Replace("Drivers", "");

                Match match2 = Regex.Match(result, @"\((.*?)\)", RegexOptions.Singleline);

                for (int i = 1; i < match2.Groups.Count; i++)
                {

                    result = result.Replace("(" + match2.Groups[i].Value + ")", "");
                }

                result = result.Trim();

                return result;
            }
            return "";
        }

        public static string GetHtml(string url,string post="", string referer="")
        {

            try
            {
                CookieContainer cc = new CookieContainer();
                if (referer != null && referer != "")
                {
                    /*
                                        HttpWebRequest reqReferer1 = (HttpWebRequest)HttpWebRequest.Create("http://www.realtek.com/downloads/downloadsView.aspx?Langid=1&PNid=14&PFid=24&Level=4&Conn=3");
                                        reqReferer1.CookieContainer = cc;
                                        reqReferer1.Method = "GET";
                                        reqReferer1.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)";
                                        reqReferer1.GetResponse();
                    */
                    HttpWebRequest reqReferer = (HttpWebRequest)HttpWebRequest.Create(referer);
                    //                    reqReferer.Referer = reqReferer1.RequestUri.ToString();
                    reqReferer.CookieContainer = cc;
                    reqReferer.Method = "GET";
                    //reqReferer.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)";
                    reqReferer.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0";
                    HttpWebResponse responseRef = (HttpWebResponse)reqReferer.GetResponse();
                    StreamReader websrcref = new StreamReader(responseRef.GetResponseStream());
                    String srcRef = websrcref.ReadToEnd();
                    /*
                                        Match match = Regex.Match(srcRef, "__VIEWSTATE\" value=\"(.*?)\"", RegexOptions.Singleline);
                                        if (match.Success)
                                        {
                                            ViewState = match.Groups[1].ToString();
                                        }*/
                }

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.CookieContainer = cc;


                //                request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)";
                //request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:24.0) Gecko/20100101 Firefox/24.0";
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 5.01; Windows NT 5.0)";
                //SetHeader(request, "User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)");
                request.AllowAutoRedirect = true;
                //request.PreAuthenticate = true;
                //request.Credentials = CredentialCache.DefaultCredentials;

                request.KeepAlive = true;
                request.Headers["Cache-Control"] = "max-age=0";
                //request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                request.Headers["Accept-Language"] = "fr,fr-fr;q=0.8,en-us;q=0.5,en;q=0.3";
                request.AutomaticDecompression = DecompressionMethods.GZip;

                if (referer != null && referer != "")
                    request.Referer = referer;
                /*                else
                                    request.Referer = URL;
                                */
                if (post != "" && post != null)
                {
                    request.Method = "POST";
                    byte[] array = System.Text.Encoding.UTF8.GetBytes(post /*+ "&__VIEWSTATE=" + ViewState*/);
                    request.ContentLength = array.Length;
                    request.ContentType = "application/x-www-form-urlencoded";
                    Stream data = request.GetRequestStream();
                    data.Write(array, 0, array.Length);
                    data.Close();
                }
                else
                    request.Method = "GET";

                // make request for web page
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader websrc = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("iso-8859-1"));
                string html = websrc.ReadToEnd();
                response.Close();


                return html;
            }
            catch (UriFormatException)
            {
                return "URL invalide";
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    return "";
                }
                else return "";
            }
            catch (IOException)
            {
                return
                "unavailable";
            }
            //catch (Exception ex)
            //{
            //    Status = "Unknown";
            //    Complement = ex.ToString();
            //}
            return null;
        }
    }
}
