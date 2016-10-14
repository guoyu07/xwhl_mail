using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TransferArchiveEmail
{
    public class MUser
    {
        public string Username { get; set; }
        public byte Finished { get; set; }
        public int? Sort { get; set; }
    }
    [PetaPoco.PrimaryKey("id")]
    public class MFolder
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FolderId { get; set; }
        public string FolderName { get; set; }
        public byte Finished { get; set; }
    }
    [PetaPoco.PrimaryKey("id")]
    public class MMail
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FolderId { get; set; }
        public string MailId { get; set; }
        public string MailSubject { get; set; }
        public string MailFrom { get; set; }
        public string MailTo { get; set; }
        public string MailCc { get; set; }
        public string MailBcc { get; set; }
        public DateTime? MailDate { get; set; }
        public byte IsHtml { get; set; }
        public string MailBody { get; set; }
        public string Attachments { get; set; }
        public byte Status { get; set; }
        public string SearchText { get; set; }

        [PetaPoco.Ignore]
        public IEnumerable<MAttachment> AttachList { get; set; }
    }

    public class MAttachment
    {
        public MAttachment()
        {
            RealFilename = Guid.NewGuid().ToString();
        }
        public string Filename { get; set; }
        public string FileSize { get; set; }
        public string FileType { get; set; }
        public string AttInfo { get; set; }
        public string FolderId { get; set; }
        public string MailId { get; set; }
        public string RealFilename { get; set; }

        public override string ToString()
        {
            return String.Format("{0},{1},{2},{3}", RealFilename, FileSize, FileType, Filename);
        }
    }


    public class DA
    {
        PetaPoco.Database db;

        public DA()
        {
            PetaPoco.Database.Mapper = new Szac.Odr.Utils.PetaPocoMapper();
            db = new PetaPoco.Database("MainDB");
        }

        public void ClearData(string username)
        {
            db.Execute("DELETE FROM m_folder WHERE username=@00", username);
            db.Execute("DELETE FROM m_mail WHERE username=@0", username);
        }

        public MUser GetNextUser()
        {
            MUser user = db.FirstOrDefault<MUser>("WHERE finished<9 ORDER BY sort DESC");
            return user;
        }

        public void AddFolders(string username, IEnumerable<MailFolder> folders)
        {
            foreach (MailFolder f in folders)
            {
                MFolder folder = new MFolder();
                folder.Username = username;
                folder.FolderId = f.id;
                folder.FolderName = f.name;
                db.Save(folder);
            }
        }

        public void UpdateUserFinished(MUser user)
        {
            db.Execute("UPDATE m_user SET finished=@0 WHERE username=@1", user.Finished, user.Username);
        }

        public MFolder GetNextFolder(string username)
        {
            MFolder folder = db.FirstOrDefault<MFolder>("WHERE finished<9 AND username=@0", username);
            return folder;
        }

        public void AddMailIds(string username, string folderId, IEnumerable<string> mailIds)
        {
            foreach (string mid in mailIds)
            {
                MMail mail = db.FirstOrDefault<MMail>("WHERE mail_id=@0", mid);
                if (mail == null)
                {
                    mail = new MMail();
                    mail.Username = username;
                    mail.FolderId = folderId;
                    mail.MailId = mid;
                    db.Save(mail);
                }
            }
        }

        public void UpdateFolderFinished(MFolder folder)
        {
            db.Execute("UPDATE m_folder SET finished=9 WHERE id=@0", folder.Id);
        }

        public MMail GetNextMail(string username)
        {
            MMail mail = db.FirstOrDefault<MMail>("WHERE mail_date IS NULL AND username=@0 ORDER BY id DESC", username);
            return mail;
        }

        public int GetMailId(string username, string mailId)
        {
            MMail mail = db.FirstOrDefault<MMail>("WHERE username=@0 AND mail_id=@1", username, mailId);
            return (mail == null ? 0 : mail.Id);
        }

        public int GetMailCount(string username)
        {
            long a = db.ExecuteScalar<long>("SELECT COUNT(1) FROM m_mail WHERE mail_date IS NULL AND username=@0", username);
            return Convert.ToInt32(a);
        }

        public void SaveMail(MMail mail)
        {
            db.Save(mail);
        }

        public bool IsMailOnly(string username)
        {
            long a = db.ExecuteScalar<long>("SELECT COUNT(1) FROM m_folder WHERE username=@0", username);
            if (a == 0) return false;
            long b = db.ExecuteScalar<long>("SELECT COUNT(1) FROM m_folder WHERE username=@0 AND finished=9", username);
            return (a == b);
        }


        //public void CheckAttachments()
        //{
        //    IEnumerable<MMail> e = db.Fetch<MMail>("WHERE attachments IS NOT NULL");
        //    foreach (var mail in e)
        //    {
        //        string[] attachs = mail.Attachments.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //        foreach (string a in attachs)
        //        {
        //            string filename = a.Substring(0, a.IndexOf(","));
        //            filename = string.Format("D:\\tmp\\Attach\\{0}\\{1}", mail.Username, filename);
        //            if (!System.IO.File.Exists(filename))
        //            {
        //                mail.Status = 1;
        //                db.Save(mail);
        //            }
        //        }
        //    }
        //}

        public void ClearFolderFinished(string username)
        {
            db.Execute("UPDATE m_folder SET finished=0 WHERE username=@0", username);
        }

        /// <summary>
        /// 生成可供检索的合成文本，去掉HTML标签等
        /// </summary>
        public void GenerateSearchText()
        {
            StringBuilder sb = new StringBuilder();
            List<MMail> list = db.Fetch<MMail>("WHERE 1=1");
            foreach (MMail mail in list)
            {
                sb.Append(FilterHtml(mail.MailSubject));
                sb.Append(" ");
                sb.Append(FilterHtml(mail.MailFrom));
                sb.Append(" ");
                sb.Append(FilterHtml(mail.MailTo));
                sb.Append(" ");
                sb.Append(FilterHtml(mail.MailBody));
                sb.Append(" ");
                sb.Append(mail.Attachments);
                mail.SearchText = sb.ToString();
                db.Save(mail);
                sb.Remove(0, sb.Length);
            }
        }
        public static string FilterHtml(string html)  //替换HTML标记
        {
            if (string.IsNullOrEmpty(html)) return "";
            RegexOptions opts = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled;

            html = Regex.Replace(html, @"<script.*?>.*?</script>", "", opts);
            html = Regex.Replace(html, @"<style.*?>.*?</style>", "", opts);
            html = Regex.Replace(html, @"<!--.*?-->", "", opts);
            html = Regex.Replace(html, @"<[^>]*>|&(quot|amp|lt|gt|nbsp|#\d*);", "", opts);

            return html;
        }


        public bool ExistsAttachment(string username, string filename)
        {
            string a = db.ExecuteScalar<string>(
                string.Format("SELECT 'ok' FROM m_mail WHERE attachments LIKE '%{0}%' AND username='{1}'", filename, username));
            return (a == "ok");
        }
    }
}
