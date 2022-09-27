mergeInto(LibraryManager.library, {
    // Request Response JS -> C# -> JS
    Response: function (number, str) {
        Module.Bridge.Response(number, UTF8ToString(str));
    },

    // message C# -> JS
    SendMessageToJS: function (str) {
        Module.Bridge.SendMessageToJS(UTF8ToString(str));
    }
});
