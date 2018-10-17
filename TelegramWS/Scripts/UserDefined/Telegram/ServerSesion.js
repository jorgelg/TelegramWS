window.onload = VerSesion;

function mostrarMensaje(mensaje) {
    document.getElementById("EspacioMensaje").innerHTML = "<div class='alert alert-info'>" + mensaje + "</div>";
}

function VerSesion() {
    var datos={};
    callAjaxJSON("/api/TelegramActions/ExisteSesion", datos, function (result) {
        if (result.STATUS) {
            document.getElementById("NoAuth").style.display = "none";
            document.getElementById("Authenticated").style.display = "block";
            document.getElementById("noAuthList").style.display = "none";
            document.getElementById("tabla").style.display = "block";
            mostrarMensaje(result.MESSAGE);
            procesarLista(result.data.contactos);
        }
        else {
            mostrarMensaje(result.MESSAGE);
        }
    });
}

function EnviarMensaje() {
    var boton = document.getElementById("btnEnviar").disabled = true;
    var nrodest ="+591"+ document.getElementById("nrotel").value;
    var texto = document.getElementById("TextoMensaje").value;
    document.getElementById("TextoMensaje").disabled = true;
    var datos = { NumeroDestino: nrodest, TextoContenido: texto, TipoMensaje:"AContacto" };
    callAjaxJSON("/api/TelegramActions/EnviarMensaje", datos, function (result) {
        document.getElementById("btnEnviar").disabled = false;
        document.getElementById("TextoMensaje").disabled = false;
        Limpiar();
        mostrarMensaje(result.MESSAGE);
    });
}

function Request() {
    this.TextoContenido = "";
    this.NumeroDestino = "";
    this.GrupoOCanalDestino = "";
    this.TipoMensaje = "";
    this.Imagen = "";
    this.NombreNuevoUsuario = "";
}

function IniciarSesion() {
    document.getElementById("btnAutenticar").disabled = true;
    var codigo = document.getElementById("txtCodigo").value;
    var datos = { codigo: codigo };
    callAjaxJSON("/api/TelegramActions/Autenticar", datos, function (result) {
        if (result.STATUS) {
            document.getElementById("NoAuth").style.display = "none";
            document.getElementById("Authenticated").style.display = "block";
            document.getElementById("noAuthList").style.display = "none";
            document.getElementById("tabla").style.display = "block";
            mostrarMensaje("Autenticación exitosa para: " + result.data.NombreUsuario);
            var listaContactos = result.data.contactos;
            procesarLista(listaContactos);
        }
        else {
            document.getElementById("btnAutenticar").disabled = false;
            mostrarMensaje(result.MESSAGE);
        }
    });
}

function CerrarSesion() {
    var datos = {};
    callAjaxJSON("/api/TelegramActions/CerrarSesion", datos, function (result) {
        if (result.STATUS) {
            document.getElementById("tabla").style.display = "none";
            document.getElementById("NoAuth").style.display = "block";
            document.getElementById("boton").disabled = false;
            mostrarMensaje(result.MESSAGE);
        }
        else {
            mostrarMensaje(result.MESSAGE);
        }
    });
}

function procesarLista(listaContactos) {
    var target = document.getElementById("contactos");
    var totalContactos = listaContactos.length;
    var html = "";
    for (var i = 0; i < totalContactos; i++) {
        html += "<tr>" +
            "<td>" + (i + 1).toString() + "</td>" +
            "<td>" + listaContactos[i].Nombre + "</td>" +
            "<td>" + listaContactos[i].Telefono + "</td>" +
            "</tr>";
    }
    target.innerHTML = html;
}



function SolicitarCodigo() {
    var datos = {};
    document.getElementById("boton").disabled = true;
    callAjaxJSON("/api/TelegramActions/SolicitarCodigo", datos, function (result) {
        if (result.STATUS) {
            //document.getElementById("tabla").style.display = "block";
            document.getElementById("btnAutenticar").disabled = false;
            //document.getElementById("tabla").style.display = "block";
            mostrarMensaje(result.MESSAGE);
        }
        else {
            document.getElementById("boton").disabled = false;
            mostrarMensaje(result.MESSAGE);
        }
    });
}