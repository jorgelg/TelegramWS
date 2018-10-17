window.fbAsyncInit = function () {
    FB.init({
        appId: '1410882755639041',
        xfbml: false,
        status:true,
        version: 'v2.8'
    });
    FB.AppEvents.logPageView();
};

(
    function (d, s, id) {
    var js, fjs = d.getElementsByTagName(s)[0];
    if (d.getElementById(id)) return;
    js = d.createElement(s); js.id = id;
    js.src = "//connect.facebook.net/es_LA/sdk.js"; //in case of debugging js.src = "//connect.facebook.net/en_US/sdk/debug.js";
    fjs.parentNode.insertBefore(js, fjs);
    }
(document, 'script', 'facebook-jssdk')

);