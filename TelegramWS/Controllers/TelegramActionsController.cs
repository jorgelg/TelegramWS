using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using TelegramWS.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;
using TelegramWS.Models.Telegram;

namespace TelegramWS.Controllers
{
    public class TelegramActionsController : ApiController
    {
        public string pathArchivo { get; set; }

        [HttpPost]
        public async Task<JObject> ExisteSesion()
        {
            pathArchivo = HttpContext.Current.Server.MapPath("~/Resources/session/Registros.txt");
            Respuestas nuevo = new Respuestas();
            try
            {
                TelegramService pruebas = new TelegramService();
                nuevo = await pruebas.VerificarSesion(pathArchivo);
                return JObject.FromObject(nuevo);
            }
            catch (Exception e)
            {
                nuevo.MESSAGE = nuevo.MESSAGE + " " + ((e.InnerException != null) ? e.InnerException.Message : e.Message);
                return JObject.FromObject(nuevo);
            }
        }


        [HttpPost]
        public async Task<JObject> Autenticar(JObject data)
        {
            pathArchivo = HttpContext.Current.Server.MapPath("~/Resources/session/Registros.txt");
            Respuestas nuevo = new Respuestas();
            try
            {
                string codigo = data.Value<string>("codigo");
                TelegramService pruebas = new TelegramService();
                nuevo = await pruebas.AutenticarUsuario(codigo, pathArchivo);
                return JObject.FromObject(nuevo);
            }
            catch (Exception e)
            {
                nuevo.MESSAGE = nuevo.MESSAGE + " " + ((e.InnerException != null) ? e.InnerException.Message : e.Message);
                return JObject.FromObject(nuevo);
            }
        }

        [HttpPost]
        public async Task<JObject> SolicitarCodigo()
        {
            pathArchivo = HttpContext.Current.Server.MapPath("~/Resources/session/Registros.txt");
            Respuestas nuevo = new Respuestas();
            try
            {
                TelegramService pruebas = new TelegramService();
                nuevo = await pruebas.SolicitarCodigo(pathArchivo);

                return JObject.FromObject(nuevo);
            }
            catch (Exception e)
            {
                nuevo.MESSAGE = nuevo.MESSAGE + " " + ((e.InnerException != null) ? e.InnerException.Message : e.Message);
                return JObject.FromObject(nuevo);
            }
        }

        [HttpPost]
        public async Task<JObject> EnviarImagen(JObject data)
        {
            Respuestas respuesta = new Respuestas();
            pathArchivo = HttpContext.Current.Server.MapPath("~/Resources/session/Registros.txt");
            try
            {
                Mensaje nuevo = new Mensaje();
                nuevo = data.ToObject<Mensaje>();
                TelegramService pruebas = new TelegramService();
                switch (nuevo.TipoMensaje)
                {
                    case "AContacto":
                        respuesta = await pruebas.EnviarImagen(nuevo);
                        break;
                    case "AGrupo":
                        respuesta = await pruebas.EnviarImagenAGrupo(nuevo);
                        break;
                    case "ACanal":
                        respuesta = await pruebas.EnviarImagenACanal(nuevo);
                        break;
                    //Usuario N hace referencia a un usuario que la cuenta asociada a esta aplicación no tiene entre sus contactos
                    case "AUsuarioN":
                        respuesta = await pruebas.EnviarImagenAUsuarioN(nuevo);
                        break;
                    default:
                        break;
                }

                return JObject.FromObject(respuesta);
            }
            catch (Exception e)
            {
                respuesta.MESSAGE += " error: " + e.Message;
                return JObject.FromObject(respuesta);
            }
        }

        [HttpPost]
        public async Task<JObject> EnviarMensaje(JObject data)
        {
            Respuestas respuesta = new Respuestas();
            pathArchivo = HttpContext.Current.Server.MapPath("~/Resources/session/Registros.txt");
            try
            {
                Mensaje nuevo = new Mensaje();
                nuevo = data.ToObject<Mensaje>();
                TelegramService pruebas = new TelegramService();
                switch (nuevo.TipoMensaje)
                {
                    case "AContacto":
                        respuesta = await pruebas.EnviarMensaje(nuevo, pathArchivo);
                        break;
                    case "AGrupo":
                        respuesta = await pruebas.EnviarMensajeAGrupo(nuevo, pathArchivo);
                        break;
                    case "ACanal":
                        respuesta = await pruebas.EnviarMensajeACanal(nuevo, pathArchivo);
                        break;
                    //Usuario N hace referencia a un usuario que la cuenta asociada a esta aplicación no tiene entre sus contactos
                    case "AUsuarioN":
                        respuesta = await pruebas.EnviarMensajeAUsuarioN(nuevo, pathArchivo);
                        break;
                    default:
                        break;
                }

                if (!string.IsNullOrEmpty(data.Value<string>("Imagen")))
                {
                    respuesta = (await EnviarImagen(data)).ToObject<Respuestas>();
                }

                return JObject.FromObject(respuesta);
            }
            catch (Exception e)
            {
                respuesta.MESSAGE +=" error: "+ e.Message;
                return JObject.FromObject(respuesta);
            }
        }

        [HttpPost]
        public async Task<JObject> CerrarSesion()
        {
            pathArchivo = HttpContext.Current.Server.MapPath("~/Resources/session/Registros.txt");
            Respuestas nuevo = new Respuestas();
            try
            {
                TelegramService pruebas = new TelegramService();
                nuevo = await pruebas.CerrarSesion(pathArchivo);

                return JObject.FromObject(nuevo);
            }
            catch (Exception e)
            {
                nuevo.MESSAGE =nuevo.MESSAGE+ "";
                nuevo.MESSAGE = nuevo.MESSAGE + " " + ((e.InnerException != null) ? e.InnerException.Message : e.Message);
                return JObject.FromObject(nuevo);
            }
        }
    }
}
