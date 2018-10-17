using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TelegramWS.Models
{
    public class Respuestas
    {
        public Respuestas()
        {
            STATUS = false;
            MESSAGE = "Si ve este mensaje, algo salio mal...";
        }
        public string MESSAGE { get; set; }
        public bool STATUS { get; set; }
        public object data { get; set; }
    }
}