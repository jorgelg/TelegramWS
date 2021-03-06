﻿function callAjax(url, parameters, callback) {
    var xmlhttp;
    // compatible with IE7+, Firefox, Chrome, Opera, Safari
    xmlhttp = new XMLHttpRequest();
    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            callback(xmlhttp.responseText);
        }
    }
    xmlhttp.open("POST", url, true);
    //xmlhttp.timeout = 1000;
    //xmlhttp.ontimeout = function (e) {
    //    callback('{"mensaje":"tiempo agotado para solicitud de codigo"}');
    //}
    //xmlhttp.send(JSON.stringify(parameters));
    xmlhttp.send(url);
}

function callAjaxJSON(url, parameters, callback) {
    var xmlhttp;
    // compatible with IE7+, Firefox, Chrome, Opera, Safari
    xmlhttp = new XMLHttpRequest();
    
    xmlhttp.onreadystatechange = function () {
        if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
            var resultado = JSON.parse(xmlhttp.responseText); //parsing to object quack
            callback(resultado);
        }
    }
    xmlhttp.open("POST", url, true);
    //xmlhttp.timeout = 5000;
    //xmlhttp.ontimeout = function (e) {
    //    callback('{"MESSAGE":"tiempo agotado para solicitud de codigo"}');
    //}
    xmlhttp.setRequestHeader("Content-Type", "application/json");
    xmlhttp.send(JSON.stringify(parameters));
    
    //xmlhttp.send(url);
}

function callAjaxJSONX(url, parameters, callback) {
    var xmlhttp;
    // compatible with IE7+, Firefox, Chrome, Opera, Safari
    xmlhttp = new XMLHttpRequest();

    if ('withCredentials' in xmlhttp) {
        xmlhttp.open("POST", url, true);
        xmlhttp.onreadystatechange = function () {
            if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
                var resultado = JSON.parse(xmlhttp.responseText); //parsing to object quack
                callback(resultado);
            }
        };
        xmlhttp.setRequestHeader("Content-Type", "application/json");
        xmlhttp.send(JSON.stringify(parameters));
    }
    else if (XDomainRequest) {
        xmlhttp = new XDomainRequest();
        xmlhttp.open("POST", url, true);
        xmlhttp.onload = function () {
            var resultado = JSON.parse(xmlhttp.responseText); //parsing to object quack
            callback(resultado);
        };
        xmlhttp.setRequestHeader("Content-Type", "application/json");
        xmlhttp.send(JSON.stringify(parameters));
    }

    
    
    //xmlhttp.timeout = 5000;
    //xmlhttp.ontimeout = function (e) {
    //    callback('{"MESSAGE":"tiempo agotado para solicitud de codigo"}');
    //}
    
    //xmlhttp.send(url);
}