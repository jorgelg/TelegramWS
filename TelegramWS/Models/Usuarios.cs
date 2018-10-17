using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TelegramWS.Models
{
    public class Usuarios
    {
        public Usuarios() { Nombre = ""; Telefono = ""; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }

        public string titleorusername { get; set; }
    }
}