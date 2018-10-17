using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using TLSharp.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TelegramWS.Models.Telegram
{
    public class WebSessionStore : ISessionStore
    {
        public void Save(Session session)
        {
            var file = HttpContext.Current.Server.MapPath("~/Resources/session/{0}.dat");

            using (FileStream fileStream = new FileStream(string.Format(file, (object)session.SessionUserId), FileMode.OpenOrCreate))
            {
                var bytes = session.ToBytes();
                
                
                fileStream.Write(bytes, 0, bytes.Length);
            }
            string datos = JsonConvert.SerializeObject(session, Formatting.Indented);
            File.WriteAllText(HttpContext.Current.Server.MapPath("~/Resources/session/Sesion.txt"), datos);
        }

        public Session Load(string sessionUserId)
        {
            var file = HttpContext.Current.Server.MapPath("~/Resources/session/{0}.dat");

            string path = string.Format(file, (object)sessionUserId);
            if (!File.Exists(path))
                return null;

            using(var stream = new FileStream(path, FileMode.Open))
            {
                var buffer1 = new byte[2048];
                stream.Read(buffer1, 0, 2048);
                return Session.FromBytes(buffer1, this, sessionUserId);
            }
        }
    }
}