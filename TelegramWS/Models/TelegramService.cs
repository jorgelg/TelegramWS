using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using TelegramWS.Models.Telegram;
using TeleSharp.TL;
using TeleSharp.TL.Contacts;
using TeleSharp.TL.Messages;
using TLSharp.Core;
using TLSharp.Core.Utils;

namespace TelegramWS.Models
{
    public class Informacion_sesion
    {
        public string NumeroPropietario { get; set; }
        public int ApiId { get; set; }
        public string ApiHash { get; set; }
        public string Codigo_solicitud { get; set; }
        public string Codigo_autenticacion { get; set; }
        public List<Usuarios> contactos = new List<Usuarios>();
        public string NombreUsuario { get; set; }
        public List<string> Expiracion = new List<string>();
    };

    public class TelegramService
    {
        Informacion_sesion info;
        TelegramClient contexto;

        public TelegramService()
        {
            info = new Informacion_sesion();
            GetConfiguration();
        }

        private void GetConfiguration()
        {
            info.NumeroPropietario = WebConfigurationManager.AppSettings["NumeroAsociado"];
            string apiId = WebConfigurationManager.AppSettings[nameof(info.ApiId)];
            info.ApiId = int.Parse(apiId);
            info.ApiHash = WebConfigurationManager.AppSettings[nameof(info.ApiHash)];
        }

        private TelegramClient Iniciar()
        {
            ISessionStore algo = new WebSessionStore();
            var context = new TelegramClient(info.ApiId, info.ApiHash, algo, "session"); //algo
            contexto = context;
            return contexto;
        }

        public async Task<Respuestas> SolicitarCodigo(string filepath)
        {
            try
            {
                contexto = Iniciar();
                bool conectado = false;
                await contexto.ConnectAsync();
                info.Codigo_solicitud = await contexto.SendCodeRequestAsync(info.NumeroPropietario);

                if (conectado && !string.IsNullOrEmpty(info.Codigo_solicitud))
                {

                    JObject objeto = JObject.Parse(File.ReadAllText(filepath.Replace("Registros.txt", "Sesion.txt")));
                    info.Expiracion.Add(DateTime.Now.AddMilliseconds(objeto.Value<double>("SessionExpires")).ToString("MM/dd/yy H:mm:ss"));
                    GuardarArchivo(filepath, info);
                    return new Respuestas() { MESSAGE = "Solicitud enviada correctamente a " + info.NumeroPropietario + ".", STATUS = true };
                }
                else
                {
                    return new Respuestas() { MESSAGE = "No se pudo establecer conexion o no se recibio un codigo de la solicitud...", STATUS = false };
                }

            }
            catch (Exception e)
            {
                return new Respuestas() { MESSAGE = "no se pudo enviar solicitud de codigo..." + e.InnerException.Message, STATUS = false };
            }
        }
        public async Task<Respuestas> EnviarMensaje(Mensaje nuevo, string pathArchivo)
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                contexto = Iniciar();
                TLContacts contactos = new TLContacts();
                await contexto.ConnectAsync();
                contactos = await contexto.GetContactsAsync();
                List<Usuarios> lista = new List<Usuarios>();
                foreach (var item in contactos.Users.OfType<TLUser>())
                {
                    var contacto = new Usuarios
                    {
                        Nombre = item.FirstName + " " + item.LastName,
                        Telefono = "+" + item.Phone
                    };
                    lista.Add(contacto);
                }
                info.contactos = lista.OrderBy(x => x.Nombre).ToList();
                var numeroformateado = nuevo.NumeroDestino.StartsWith("+") ?
                    nuevo.NumeroDestino.Substring(1) :
                    nuevo.NumeroDestino;
                var usuariodestino = contactos.Users.OfType<TLUser>().FirstOrDefault(x => x.Phone == numeroformateado);
                if (usuariodestino == null)
                {
                    respuesta.STATUS = false;
                    respuesta.MESSAGE = "No se pudo encontrar a usuario...";
                    return respuesta;
                }
                bool completado = await contexto.SendTypingAsync(new TLInputPeerUser() { UserId = usuariodestino.Id });
                await Task.Delay(3000);
                if (completado)
                {
                    await contexto.SendMessageAsync(new TLInputPeerUser() { UserId = usuariodestino.Id }, nuevo.TextoContenido);
                }
                else
                {
                    respuesta.STATUS = false;
                    respuesta.MESSAGE = "No se pudo sincronizar conexion con Telegram...";
                    return respuesta;
                }
                respuesta.STATUS = true;
                respuesta.MESSAGE = "Mensaje enviado a: " + usuariodestino.FirstName;
                return respuesta;
            }
            catch (Exception e)
            {
                respuesta.MESSAGE = "no se pudo enviar mensaje: " + e.Message;
                respuesta.STATUS = false;
                return respuesta;
            }
        }

        public async Task<Respuestas> EnviarImagenAGrupo(Mensaje nuevo)
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                contexto = Iniciar();
                TLDialogs conversaciones = new TLDialogs();
                await contexto.ConnectAsync();
                conversaciones = (TLDialogs)await contexto.GetUserDialogsAsync();
                var grupodestino = conversaciones.Chats.OfType<TLChat>().FirstOrDefault(x => x.Title.ToUpper() == nuevo.GrupoOCanalDestino.ToUpper());
                if (grupodestino == null)
                {
                    respuesta.STATUS = false;
                    respuesta.MESSAGE = "No se pudo encontrar a usuario...";
                    return respuesta;
                }

                MemoryStream imagen = new MemoryStream(Convert.FromBase64String(nuevo.Imagen));
                //var im = Image.FromStream(imagen);
                //Image imagen_redimensionada = ResizeImage(im, 300, 60);
                //args.Image = imagen_redimensionada;
                StreamReader imageStream = new StreamReader(imagen); //verificar encoding

                var archivo = new TLInputFile();
                archivo = (TLInputFile)await contexto.UploadFile("Image" + (new Random().Next()) + ".jpg", imageStream);

                if (archivo != null)
                {
                    await contexto.SendUploadedPhoto(new TLInputPeerChat() { ChatId = grupodestino.Id }, archivo, nuevo.DescripcionImagen);
                    respuesta.MESSAGE = "Imagen enviada a: " + grupodestino.Title;
                    respuesta.STATUS = true;
                    return respuesta;
                }
                else
                {
                    respuesta.MESSAGE = "No se pudo enviar imagen, ERROR INTERNO...";
                    respuesta.STATUS = false;
                    return respuesta;
                }
            }
            catch (Exception e)
            {
                respuesta.MESSAGE = " no se pudo enviar imagen..." + e.Message;
                respuesta.STATUS = false;
                return respuesta;
            }
        }

        public async Task<Respuestas> EnviarImagenAUsuarioN(Mensaje nuevo)
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                var numeroformateado = nuevo.NumeroDestino.StartsWith("+") ?
                       nuevo.NumeroDestino.Substring(1) :
                       nuevo.NumeroDestino;
                contexto = Iniciar();
                await contexto.ConnectAsync();
                var nvoContacto = new TLInputPhoneContact() { Phone = numeroformateado, FirstName = ((string.IsNullOrEmpty(nuevo.NombreNuevoUsuario) ? "user" + numeroformateado : nuevo.NombreNuevoUsuario)), LastName = string.Empty };
                var contactos = new List<TLInputPhoneContact>() { nvoContacto };

                var req = new TLRequestImportContacts() { Contacts = new TLVector<TLInputPhoneContact>() { nvoContacto } };
                var resultado = await contexto.SendRequestAsync<TLImportedContacts>(req);
                //aqui carga 1ro infaliblemente el user_id 1ro... y es extraible...
                if (resultado.Users.Count > 0)
                {
                    var idnuevo = resultado.Users.OfType<TLUser>().ElementAtOrDefault(0).Id;
                    var usuarioDestino = resultado.Users.OfType<TLUser>().FirstOrDefault(x => x.Id == idnuevo);
                    if (usuarioDestino == null)
                    {
                        respuesta.MESSAGE = "No se encontro usuario Destino, verifique que sea un número telefónico asociado a una cuenta Telegram...";
                        respuesta.STATUS = false;
                        return respuesta;
                    }


                    MemoryStream imagen = new MemoryStream(Convert.FromBase64String(nuevo.Imagen));
                    //var im = Image.FromStream(imagen);
                    //Image imagen_redimensionada = ResizeImage(im, 300, 60);
                    //args.Image = imagen_redimensionada;
                    StreamReader imageStream = new StreamReader(imagen); //verificar encoding

                    var archivo = new TLInputFile();
                    archivo = (TLInputFile)await contexto.UploadFile("Image" + (new Random().Next()) + ".jpg", imageStream);

                    if (archivo != null)
                    {
                        await contexto.SendUploadedPhoto(new TLInputPeerUser() { UserId = usuarioDestino.Id }, archivo, nuevo.DescripcionImagen);
                        respuesta.MESSAGE = "Imagen enviada a: " + numeroformateado;
                        respuesta.STATUS = true;
                        return respuesta;
                    }
                    else
                    {
                        respuesta.MESSAGE = "No se pudo enviar imagen, ERROR INTERNO...";
                        respuesta.STATUS = false;
                        return respuesta;
                    }
                }
                else
                {
                    respuesta.MESSAGE = "Error interno, no se pudo enviar mensaje a nuevo usuario";
                    respuesta.STATUS = false;
                    return respuesta;
                }
            }
            catch (Exception e)
            {
                respuesta.MESSAGE = "No se pudo enviar mensaje error: " + e.Message;
                respuesta.STATUS = false;
                return respuesta;
            }
        }

        public async Task<Respuestas> EnviarImagenACanal(Mensaje nuevo)
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                contexto = Iniciar();
                TLDialogs conversaciones = new TLDialogs();
                await contexto.ConnectAsync();
                conversaciones = (TLDialogs)await contexto.GetUserDialogsAsync();
                var grupodestino = conversaciones.Chats.OfType<TLChannel>().FirstOrDefault(x => x.Title.ToUpper() == nuevo.GrupoOCanalDestino.ToUpper());
                if (grupodestino == null)
                {
                    respuesta.STATUS = false;
                    respuesta.MESSAGE = "No se pudo encontrar a usuario...";
                    return respuesta;
                }


                MemoryStream imagen = new MemoryStream(Convert.FromBase64String(nuevo.Imagen));
                //var im = Image.FromStream(imagen);
                //Image imagen_redimensionada = ResizeImage(im, 300, 60);
                //args.Image = imagen_redimensionada;
                StreamReader imageStream = new StreamReader(imagen); //verificar encoding

                var archivo = new TLInputFile();
                archivo = (TLInputFile)await contexto.UploadFile("Image" + (new Random().Next()) + ".jpg", imageStream);

                if (archivo != null)
                {
                    await contexto.SendUploadedPhoto(new TLInputPeerChannel() { AccessHash = (long)grupodestino.AccessHash, ChannelId = grupodestino.Id }, archivo, nuevo.DescripcionImagen);
                    respuesta.MESSAGE = "Imagen enviada a: " + grupodestino.Title;
                    respuesta.STATUS = true;
                    return respuesta;
                }
                else
                {
                    respuesta.MESSAGE = "No se pudo enviar imagen, ERROR INTERNO...";
                    respuesta.STATUS = false;
                    return respuesta;
                }
            }
            catch (Exception e)
            {
                respuesta.MESSAGE = " no se pudo enviar imagen..." + e.Message;
                respuesta.STATUS = false;
                return respuesta;
            }
        }

        public async Task<Respuestas> EnviarMensajeAUsuarioN(Mensaje nuevo, string pathArchivo, string nombreUsuario = "")
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                var numeroformateado = nuevo.NumeroDestino.StartsWith("+") ?
                       nuevo.NumeroDestino.Substring(1) :
                       nuevo.NumeroDestino;
                contexto = Iniciar();
                await contexto.ConnectAsync();
                var nvoContacto = new TLInputPhoneContact() { Phone = numeroformateado, FirstName = ((string.IsNullOrEmpty(nuevo.NombreNuevoUsuario) ? "user" + numeroformateado : nuevo.NombreNuevoUsuario)), LastName = string.Empty };

                var req = new TLRequestImportContacts() { Contacts = new TLVector<TLInputPhoneContact>() { nvoContacto } };
                var resultado = await contexto.SendRequestAsync<TLImportedContacts>(req);
                //aqui carga 1ro infaliblemente el user_id 1ro... y es extraible...
                if (resultado.Users.Count > 0)
                {
                    var idnuevo = resultado.Users.OfType<TLUser>().ElementAtOrDefault(0).Id;
                    var usuarioDestino = resultado.Users.OfType<TLUser>().FirstOrDefault(x => x.Id == idnuevo);
                    if (usuarioDestino == null)
                    {
                        respuesta.MESSAGE = "No se encontro usuario Destino, verifique que sea un número telefónico asociado a una cuenta Telegram...";
                        respuesta.STATUS = false;
                        return respuesta;
                    }
                    bool completado = false;
                    completado = await contexto.SendTypingAsync(new TLInputPeerUser() { UserId = usuarioDestino.Id });
                    await Task.Delay(3000);
                    if (completado)
                    {
                        await contexto.SendMessageAsync(new TLInputPeerUser() { UserId = usuarioDestino.Id }, nuevo.TextoContenido);
                    }
                    else
                    {
                        respuesta.MESSAGE = "No se pudo sincronizar con Telegram, intente nuevamente o pruebe reautenticando...";
                        respuesta.STATUS = false;
                        return respuesta;
                    }
                    respuesta.MESSAGE = "Mensaje enviado exitosamente a: " + numeroformateado;
                    respuesta.STATUS = true;
                    return respuesta;
                }
                else
                {
                    respuesta.MESSAGE = "Error interno, no se pudo enviar mensaje a nuevo usuario";
                    respuesta.STATUS = false;
                    return respuesta;
                }
            }
            catch (Exception e)
            {
                respuesta.MESSAGE = "No se pudo enviar mensaje error: " + e.Message;
                respuesta.STATUS = false;
                return respuesta;
            }
        }

        public async Task<Respuestas> EnviarMensajeACanal(Mensaje nuevo, string pathArchivo)
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                contexto = Iniciar();
                TLDialogs conversaciones = new TLDialogs();
                await contexto.ConnectAsync();
                conversaciones = (TLDialogs)await contexto.GetUserDialogsAsync();
                var grupodestino = conversaciones.Chats.OfType<TLChannel>().FirstOrDefault(x => x.Title.ToUpper() == nuevo.GrupoOCanalDestino.ToUpper());
                if (grupodestino == null)
                {
                    respuesta.STATUS = false;
                    respuesta.MESSAGE = "No se pudo encontrar a usuario...";
                    return respuesta;
                }
                bool completado = await contexto.SendTypingAsync(new TLInputPeerChannel() { AccessHash = (long)grupodestino.AccessHash, ChannelId = grupodestino.Id });
                await Task.Delay(3000);
                if (completado)
                {
                    await contexto.SendMessageAsync(new TLInputPeerChannel() { AccessHash = (long)grupodestino.AccessHash, ChannelId = grupodestino.Id }, nuevo.TextoContenido);
                }
                else
                {
                    respuesta.STATUS = false;
                    respuesta.MESSAGE = "No se pudo sincronizar conexion con Telegram...";
                    return respuesta;
                }
                respuesta.STATUS = true;
                respuesta.MESSAGE = "Mensaje enviado a: " + grupodestino.Title;
                return respuesta;
            }
            catch (Exception e)
            {
                respuesta.STATUS = false;
                respuesta.MESSAGE = "No se pudo enviar mensaje: " + e.Message;
                return respuesta;
            }
        }

        public async Task<Respuestas> EnviarMensajeAGrupo(Mensaje nuevo, string pathArchivo)
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                contexto = Iniciar();
                TLDialogs conversaciones = new TLDialogs();
                await contexto.ConnectAsync();
                conversaciones = (TLDialogs)await contexto.GetUserDialogsAsync();
                var grupodestino = conversaciones.Chats.OfType<TLChat>().FirstOrDefault(x => x.Title.ToUpper() == nuevo.GrupoOCanalDestino.ToUpper());
                if (grupodestino == null)
                {
                    respuesta.STATUS = false;
                    respuesta.MESSAGE = "No se pudo encontrar a usuario...";
                    return respuesta;
                }
                bool completado = await contexto.SendTypingAsync(new TLInputPeerChat() { ChatId = grupodestino.Id });
                await Task.Delay(3000);
                if (completado)
                {
                    await contexto.SendMessageAsync(new TLInputPeerChat() { ChatId = grupodestino.Id }, nuevo.TextoContenido);
                }
                else
                {
                    respuesta.STATUS = false;
                    respuesta.MESSAGE = "No se pudo sincronizar conexion con Telegram...";
                    return respuesta;
                }
                respuesta.STATUS = true;
                respuesta.MESSAGE = "Mensaje enviado a: " + grupodestino.Title;
                return respuesta;
            }
            catch (Exception e)
            {
                respuesta.STATUS = false;
                respuesta.MESSAGE = "No se pudo enviar mensaje: " + e.Message;
                return respuesta;
            }
        }

        /// <summary>
        /// Necesarios: NumeroDestino, Imagen, DescripcionImagen si se quiere
        /// </summary>
        /// <param name="nuevo"></param>
        /// <returns></returns>
        public async Task<Respuestas> EnviarImagen(Mensaje nuevo)
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                var numeroformateado = nuevo.NumeroDestino.StartsWith("+") ?
                    nuevo.NumeroDestino.Substring(1) :
                    nuevo.NumeroDestino;
                contexto = Iniciar();
                await contexto.ConnectAsync();
                var contactos = new TLContacts();
                contactos = await contexto.GetContactsAsync();

                var usuariodestino = contactos.Users.OfType<TLUser>().FirstOrDefault(x => x.Phone == numeroformateado);
                if (usuariodestino == null)
                {
                    respuesta.MESSAGE = "El numero" + numeroformateado + " no esta en la lista de sus contactos";
                    respuesta.STATUS = false;
                    return respuesta;
                }
                MemoryStream imagen = new MemoryStream(Convert.FromBase64String(nuevo.Imagen));
                //var im = Image.FromStream(imagen);
                //Image imagen_redimensionada = ResizeImage(im, 300, 60);
                //args.Image = imagen_redimensionada;
                StreamReader imageStream = new StreamReader(imagen); //verificar encoding

                var archivo = new TLInputFile();
                archivo = (TLInputFile)await contexto.UploadFile("Image" + (new Random().Next()) + ".jpg", imageStream);

                if (archivo != null)
                {
                    await contexto.SendUploadedPhoto(new TLInputPeerUser() { UserId = usuariodestino.Id }, archivo, nuevo.DescripcionImagen);
                    respuesta.MESSAGE = "Imagen enviada a: " + numeroformateado;
                    respuesta.STATUS = true;
                    return respuesta;
                }
                else
                {
                    respuesta.MESSAGE = "No se pudo enviar imagen, ERROR INTERNO...";
                    respuesta.STATUS = false;
                    return respuesta;
                }
            }
            catch (Exception e)
            {
                respuesta.MESSAGE = " no se pudo enviar imagen..." + e.Message;
                respuesta.STATUS = false;
                return respuesta;
            }
        }

        public async Task<Respuestas> CerrarSesion(string pathArchivo)
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                TeleSharp.TL.Auth.TLRequestLogOut x = new TeleSharp.TL.Auth.TLRequestLogOut();

                contexto = Iniciar();
                await contexto.ConnectAsync();
                var cerrado = await contexto.SendRequestAsync<Boolean>(x);
                JObject objeto = JObject.Parse(File.ReadAllText(pathArchivo.Replace("Registros.txt", "Sesion.txt")));
                info.Expiracion.Add(DateTime.Now.AddMilliseconds(objeto.Value<double>("SessionExpires")).ToString("MM/dd/yy H:mm:ss"));
                GuardarArchivo(pathArchivo, info);
                if (!contexto.IsUserAuthorized())
                {
                    respuesta.MESSAGE = "sesion cerrada exitosamente...";
                    respuesta.STATUS = true;
                    contexto.Dispose();
                    File.Delete(HttpContext.Current.Server.MapPath("~/Resources/session/session.dat"));
                    File.Delete(HttpContext.Current.Server.MapPath("~/Resources/session/Registros.txt"));
                    File.Delete(HttpContext.Current.Server.MapPath("~/Resources/session/Sesion.txt"));
                }
                else
                {
                    respuesta.MESSAGE = "no se pudo cerrar sesion... probablemente ya se cerro anteriormente";
                    respuesta.STATUS = false;
                }
                return respuesta;
            }
            catch (Exception e)
            {
                TeleSharp.TL.Auth.TLRequestLogOut x = new TeleSharp.TL.Auth.TLRequestLogOut();
                var cerrado = await contexto.SendRequestAsync<Boolean>(x);
                JObject objeto = JObject.Parse(File.ReadAllText(pathArchivo.Replace("Registros.txt", "Sesion.txt")));
                info.Expiracion.Add(DateTime.Now.AddMilliseconds(objeto.Value<double>("SessionExpires")).ToString("MM/dd/yy H:mm:ss"));
                GuardarArchivo(pathArchivo, info);
                if (cerrado)
                {
                    respuesta.MESSAGE = "sesion cerrada exitosamente...";
                    respuesta.STATUS = true;
                    contexto.Dispose();
                    File.Delete(HttpContext.Current.Server.MapPath("~/Resources/session/session.dat"));
                    File.Delete(HttpContext.Current.Server.MapPath("~/Resources/session/Registros.txt"));
                    File.Delete(HttpContext.Current.Server.MapPath("~/Resources/session/Sesion.txt"));
                }
                else
                {
                    respuesta.MESSAGE = "No se pudo cerrar sesión..." + e.Message;
                    respuesta.STATUS = false;
                }
                return respuesta;
            }

        }

        private T CargarDeArchivo<T>(string filepath)
        {
            T informacion;
            if (File.Exists(filepath))
            {
                informacion = JsonConvert.DeserializeObject<T>(File.ReadAllText(filepath));
                return informacion;
            }
            else
            {
                return default(T);
            }


        }

        private void GuardarArchivo<T>(string filePath, T informacion)
        {
            string datos = JsonConvert.SerializeObject(informacion, Formatting.Indented);
            File.WriteAllText(filePath, datos);
        }
        public async Task<Respuestas> AutenticarUsuario(string codigo, string filepath)
        {
            try
            {
                Respuestas respuesta = new Respuestas();
                contexto = Iniciar();
                TLContacts contactos = new TLContacts();
                info = CargarDeArchivo<Informacion_sesion>(filepath);
                info.Codigo_autenticacion = codigo;
                TLUser usuario = null;
                await contexto.ConnectAsync();
                if (contexto.IsConnected)
                {
                    usuario = await contexto.MakeAuthAsync(info.NumeroPropietario, info.Codigo_solicitud, info.Codigo_autenticacion);
                    if (usuario != null)
                    {
                        info.NombreUsuario = usuario.FirstName;
                        contactos = await contexto.GetContactsAsync();
                        List<Usuarios> lista = new List<Usuarios>();
                        foreach (var item in contactos.Users.OfType<TLUser>())
                        {
                            var contacto = new Usuarios
                            {
                                Nombre = item.FirstName + " " + item.LastName,
                                Telefono = "+" + item.Phone
                            };
                            lista.Add(contacto);
                        }
                        info.contactos = lista.OrderBy(x => x.Nombre).ToList();
                        JObject objeto = JObject.Parse(File.ReadAllText(filepath.Replace("Registros.txt", "Sesion.txt")));
                        info.Expiracion.Add(DateTime.Now.AddMilliseconds(objeto.Value<double>("SessionExpires")).ToString("MM/dd/yy H:mm:ss"));
                        GuardarArchivo(filepath, info);
                        respuesta.STATUS = true;
                        respuesta.MESSAGE = "Autenticación exitosa...";
                        respuesta.data = info;
                        return respuesta;
                    }
                    else
                    {
                        respuesta.MESSAGE = "no se pudo obtener datos de usuario...";
                        respuesta.STATUS = false;
                        return respuesta;
                    }
                }
                else
                {
                    respuesta.MESSAGE = "ya se encuentra autenticado...";
                    respuesta.STATUS = false;
                    contexto.Dispose();
                    return respuesta;
                }

            }
            catch (Exception e)
            {
                return new Respuestas() { STATUS = false, MESSAGE = "No se pudo autenticar usuario" + e.InnerException.Message };
            }
        }

        public async Task<Respuestas> VerificarSesion(string pathArchivo)
        {
            Respuestas respuesta = new Respuestas();
            try
            {
                contexto = Iniciar();
                TLContacts contactos = new TLContacts();
                info = CargarDeArchivo<Informacion_sesion>(pathArchivo);
                if (info == null)
                {
                    info = new Informacion_sesion();
                }
                TLUser usuario = new TLUser();
                await contexto.ConnectAsync();
                if (contexto.IsUserAuthorized())
                {
                    contactos = await contexto.GetContactsAsync();
                    List<Usuarios> lista = new List<Usuarios>();
                    foreach (var item in contactos.Users.OfType<TLUser>())
                    {
                        var contacto = new Usuarios
                        {
                            Nombre = item.FirstName + item.LastName,
                            Telefono = "+" + item.Phone
                        };
                        lista.Add(contacto);
                    }
                    info.contactos = lista.OrderBy(x => x.Nombre).ToList();
                    respuesta.STATUS = true;
                    respuesta.MESSAGE = "Reconexion exitosa exitosa...";
                    respuesta.data = info;
                }
                else
                {
                    respuesta.STATUS = false;
                    respuesta.data = info;
                    respuesta.MESSAGE = "Reconexion fallida, intente autenticar de nuevo...";
                }
                JObject objeto = JObject.Parse(File.ReadAllText(pathArchivo.Replace("Registros.txt", "Sesion.txt")));
                info.Expiracion.Add(DateTime.Now.AddMilliseconds(objeto.Value<double>("SessionExpires")).ToString("MM/dd/yy H:mm:ss"));
                GuardarArchivo(pathArchivo, info);
                return respuesta;
            }
            catch (Exception e)
            {
                respuesta.MESSAGE = "No se pudo hacer la verificacion..." + ((string.IsNullOrEmpty(e.InnerException.Message)) ? e.Message : e.InnerException.Message);
                if (respuesta.MESSAGE.ToUpper().Contains("AUTH_KEY_UNREGISTERED"))
                {
                    respuesta.MESSAGE = "Debe Autenticarse solicitando su código";
                }
                respuesta.STATUS = false;
                return respuesta;
            }
        }

        #region Redimensionamiento de imagen
        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        #endregion
    }
}