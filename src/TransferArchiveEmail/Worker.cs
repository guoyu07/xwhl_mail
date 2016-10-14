using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNet.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.IO;

namespace TransferArchiveEmail
{
    public class MailFolder
    {
        public string id;
        public string name;
    }

    public class Worker
    {
        private const string DOMAIN = "domain.com"; //邮件域名
        private const string PASSWORD = "ABCD1234"; //账号密码，你需要把全部要导出的账号密码都改成一样的

        private DA da = new DA();

        public void Do()
        {
            Console.WriteLine("现在开始：{0:yyyy/MM/dd HH:mm}", DateTime.Now);

            MUser user = da.GetNextUser();
            while (user != null)
            {
                Console.WriteLine("---------------用户 " + user.Username);

                da.ClearData(user.Username);
                string attachFolder = ConfigurationManager.AppSettings["AttachFolder"] + user.Username;
                if (System.IO.Directory.Exists(attachFolder))
                {
                    System.IO.Directory.Delete(attachFolder, true);
                }
                System.IO.Directory.CreateDirectory(attachFolder);

                string cookie = Login(user.Username);

                if (user.Finished == 0)
                {
                    List<MailFolder> folders = GetFolders(cookie);
                    if (folders.Count == 0)
                    {
                        user = da.GetNextUser();
                        break;
                    }
                    user.Finished = 1;
                    da.AddFolders(user.Username, folders);
                    Console.WriteLine("{0}个归档文件夹", folders.Count);
                }

                if (user.Finished == 1)
                {
                    MFolder folder = da.GetNextFolder(user.Username);

                    while (folder != null)
                    {
                        int totalPages = 1;
                        int currentPage = 1;
                        List<string> mailIds = GetMailList(cookie, folder.FolderId, currentPage, ref totalPages);
                        if (mailIds.Count > 0)
                        {
                            da.AddMailIds(user.Username, folder.FolderId, mailIds);
                            while (currentPage < totalPages)
                            {
                                currentPage++;
                                mailIds = GetMailList(cookie, folder.FolderId, currentPage, ref totalPages);
                                if (mailIds.Count > 0)
                                {
                                    da.AddMailIds(user.Username, folder.FolderId, mailIds);
                                }
                            }
                        }
                        da.UpdateFolderFinished(folder);
                        folder = da.GetNextFolder(user.Username);
                    }
                    user.Finished = 2;
                }

                if (user.Finished == 2)
                {
                    int mailCount = da.GetMailCount(user.Username);
                    Console.WriteLine("{0}封邮件待下载", mailCount);
                    MMail mail = da.GetNextMail(user.Username);
                    while (mail != null)
                    {
                        GetMailContent(cookie, mail);
                        if (mail.AttachList.Count() > 0)
                        {
                            foreach (MAttachment att in mail.AttachList)
                            {
                                DownloadMailAttach(cookie, att, user.Username);
                            }
                        }
                        da.SaveMail(mail);
                        Console.WriteLine("{2}... {0:yyyy/MM/dd HH:mm} {1}", mail.MailDate, mail.MailSubject, mailCount);
                        mail = da.GetNextMail(user.Username);
                        mailCount--;
                    }

                    user.Finished = 9;
                    da.UpdateUserFinished(user);
                }

                Sleep(20);
                user = da.GetNextUser();
            }
        }

        public string Login(string username)
        {
            HttpHelper hh = new HttpHelper();

            // Step1. Get Login Cookie
            hh.GetHtml(new HttpItem()
            {
                URL = "http://220.181.130.174/webmail/index.php",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36",
                ProxyIp = "ieproxy",
                Allowautoredirect = true
            });
            HttpItem hi = new HttpItem()
            {
                URL = "http://220.181.130.174/webmail/index.php?action=login&Cmd=login",
                Method = "post",
                ProxyIp = "ieproxy",
                Referer = "http://220.181.130.174/webmail/index.php",
                KeepAlive = true,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36",
                Postdata = "AdminId=&username=" + username + "&domain=" + DOMAIN + "&password=" + PASSWORD + "&remember=0&Submit=%E7%99%BB++%E5%BD%95&language=1&sk=1",
                Allowautoredirect = false,
                ContentType = "application/x-www-form-urlencoded"
            };
            hi.Header.Add("Origin", "http://220.181.130.174");
            hi.Header.Add("Cache-Control", "max-age=0");
            hi.Header.Add("Upgrade-Insecure-Requests", "1");
            HttpResult hr12 = hh.GetHtml(hi);
            Sleep(1);

            return hr12.Cookie;
        }

        public List<MailFolder> GetFolders(string cookie)
        {
            HttpHelper hh = new HttpHelper();
            HttpResult hr2 = hh.GetHtml(new HttpItem()
            {
                URL = "http://220.181.130.174/webmail/cgijson/archivefolderlistjson.php?checktime=" + Checktime(),
                Cookie = cookie,
                Referer = "http://220.181.130.174/webmail/index.php?action=mail",
                ProxyIp = "ieproxy",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36"
            });
            Sleep(1);
            return ReadMailFolder(hr2.Html);
        }

        public List<string> GetMailList(string cookie, string folderId, int pageNo, ref int totalPages)
        {
            HttpHelper hh = new HttpHelper();
            // Step3. Get Mail List
            HttpResult hr3 = hh.GetHtml(new HttpItem()
            {
                URL = "http://220.181.130.174/webmail/cgijson/maillist.php?fid=" + folderId + "&by=dtime&ascdesc=desc&pageno=" + pageNo + "&filter=&checktime=" + Checktime(),
                Cookie = cookie,
                Referer = "http://220.181.130.174/webmail/index.php?action=inbox&fkw=" + folderId,
                ProxyIp = "ieproxy",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36"
            });
            Sleep(1);
            return ReadMailList(hr3.Html, ref totalPages);
        }

        public void GetMailContent(string cookie, MMail mail)
        {
            HttpHelper hh = new HttpHelper();
            // Step4. Get Mail Content
            //HttpResult hr4 = hh.GetHtml(new HttpItem()
            //{
            //    URL = "http://220.181.130.174/webmail/index.php?action=readmail&fid="+ mail.FolderId +"&inputid="+ mail.MailId +"%40" + mail.FolderId,
            //    Cookie = cookie,
            //    Referer = "http://220.181.130.174/webmail/index.php?action=mail",
            //    ProxyIp = "ieproxy",
            //    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36"
            //});

            HttpResult hr42 = hh.GetHtml(new HttpItem()
            {
                URL = "http://220.181.130.174/webmail/cgijson/mailnext.php?fid=" + mail.FolderId + "&mid=" + mail.MailId + "%40" + mail.FolderId + "&optype=undefined&checktime=" + Checktime(),
                Cookie = cookie,
                Referer = "http://220.181.130.174/webmail/index.php?action=readmail&fid=" + mail.FolderId + "&inputid=" + mail.MailId + "%40" + mail.FolderId,
                ProxyIp = "ieproxy",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36"
            });
            Sleep(1);
            ReadMail(hr42.Html, mail);
        }
        public void DownloadMailAttach(string cookie, MAttachment att, string username)
        {
            HttpHelper hh = new HttpHelper();
            // Step5. Download Mail Attachment
            HttpResult hr5 = hh.GetHtml(new HttpItem()
            {
                URL = "http://220.181.130.174/webmail/cgijson/mailattach.php?file_name=" + UrlEncode(att.Filename) + "&file_size=" + att.FileSize + "&content_type=" + UrlEncode(att.FileType) + "&attinfo=" + att.AttInfo + "&mid="+ att.MailId +"%40"+ att.FolderId +"&fid=" + att.FolderId,
                Cookie = cookie,
                Referer = "http://220.181.130.174/webmail/index.php?action=readmail&fid=" + att.FolderId + "&inputid=" + att.MailId + "%40" + att.FolderId,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36",
                ProxyIp = "ieproxy",
                ResultType = ResultType.Byte
            });
            string filename = ConfigurationManager.AppSettings["AttachFolder"] + username + "\\" + att.RealFilename;
            using (System.IO.FileStream fs = System.IO.File.OpenWrite(filename))
            {
                if (hr5.ResultByte != null)
                {
                    fs.Write(hr5.ResultByte, 0, hr5.ResultByte.Length);
                }
                fs.Close();
            }
            Sleep(1);
        }

        private string UrlEncode(string s)
        {
            return System.Web.HttpUtility.UrlEncode(s);
        }


        private void Sleep(int seconds)
        {
            System.Threading.Thread.Sleep(new TimeSpan(0, 0, seconds));
        }

        private DateTime javascriptBaseTime = new DateTime(1970, 1, 1);
        private Random random = new Random();

        private string Checktime()
        {
            return string.Format("{0:0}{1:.00000000000000000}",
                DateTime.Now.Subtract(javascriptBaseTime).TotalMilliseconds,
                random.NextDouble());
        }

        public class MailFolderResult
        {
            public List<MailFolder> rs;
        }
        public List<MailFolder> ReadMailFolder(string json)
        {
            // {"rs":[{"id":".2014Q4","name":"2014\u5e74\u7b2c4\u5b63\u5ea6"},{"id":".2014Q3","name":"2014\u5e74\u7b2c3\u5b63\u5ea6"}]}
            MailFolderResult result = JsonConvert.DeserializeObject<MailFolderResult>(json);
            return result.rs;
        }
        public List<string> ReadMailList(string json, ref int totalPages)
        {
            List<string> list = new List<string>();
            JObject obj = JsonConvert.DeserializeObject(json) as JObject;
            foreach (var item in obj["mlist"]["0"])
            {
                list.Add(item.First.ToString());
            }
            totalPages = Convert.ToInt32(obj["page"]["pagecount"].ToString());
            return list;
        }

        public void ReadMail(string json, MMail mail)
        {
            JObject obj = JsonConvert.DeserializeObject(json) as JObject;
            mail.MailId = obj["mid"].ToString();
            mail.FolderId = obj["fid"].ToString();
            mail.MailFrom = obj["minfo"]["from"].ToString();
            mail.MailTo = obj["minfo"]["to"].ToString();
            mail.MailCc = obj["minfo"]["cc"].ToString();
            mail.MailBcc = obj["minfo"]["bcc"].ToString();
            mail.MailSubject = obj["minfo"]["subject"].ToString();
            mail.MailBody = obj["minfo"]["body"].ToString();
            mail.IsHtml = (byte)(obj["minfo"]["ishtml"].ToString().ToLower() == "true" ? 1 : 0);
            mail.MailDate = DateTime.ParseExact(obj["minfo"]["date"].ToString(), "yy-MM-dd HH:mm:ss", null);

            List<MAttachment> list = new List<MAttachment>();

            if (obj["minfo"]["attlist"] is JObject)
            {
                int i = 0;
                JObject atts = (JObject)obj["minfo"]["attlist"];
                while (atts[i.ToString()] != null)
                {
                    JObject att = atts[i.ToString()] as JObject;
                    MAttachment a = new MAttachment();
                    a.Filename = att["0"].ToString();
                    a.FileSize = att["1"].ToString();
                    a.FileType = att["3"].ToString();
                    a.AttInfo = string.Format("{0}-{1}-{2}-{3}", att["2"], att["4"], att["5"], att["6"]);
                    a.FolderId = mail.FolderId;
                    a.MailId = mail.MailId;
                    list.Add(a);
                    i++;
                }
            }
            mail.AttachList = list;
            StringBuilder sb = new StringBuilder();
            foreach (MAttachment a in list)
            {
                if (sb.Length > 0) sb.Append("\r\n");
                sb.Append(a.ToString());
            }
            mail.Attachments = sb.ToString();
        }



        //通过Form1界面、手工补漏
        public MMail Do(string username, string mailJson)
        {
            DA da = new DA();
            MMail mail = new MMail();
            ReadMail(mailJson, mail);
            mail.Id = da.GetMailId(username, mail.MailId);
            mail.Username = username;
            da.SaveMail(mail);
            return mail;
        }


        //public void ClearAttachments()
        //{
        //    DA da = new DA();
        //    string[] dirs = Directory.GetDirectories(ConfigurationManager.AppSettings["AttachFolder"]);
        //    foreach (string dir in dirs)
        //    {
        //        string username = Path.GetFileName(dir);
        //        Console.WriteLine(string.Format("开始检查 {0}", username));

        //        string[] files = Directory.GetFiles(dir);
        //        foreach (string file in files)
        //        {
        //            string filename = Path.GetFileName(file);
        //            bool exists = da.ExistsAttachment(username, filename);
        //            if (!exists)
        //            {
        //                Console.Write("_");
        //                //Console.WriteLine(string.Format("{0}\\{1} 不属于任何邮件附件", username, filename));
        //                File.Delete(file);
        //            }
        //            else
        //            {
        //                Console.Write("#");
        //            }
        //        }
        //        Console.WriteLine();
        //        Console.WriteLine();
        //    }

        //}
    }
}
