Module['Bridge'] = {
    // message JS -> C#
    SendMessage2CSharp: function (str) {
        SendMessage('JSBridge', 'SendJsonMessage', str);
    },

    // message C# -> JS
    handleMessageFromCSharp: null,
    RegisterMessageHandler: function (handler) {
        getMessageFromCsharp = handler;
    },
    SendMessage2JS: function (str) {
        if (this.handleMessageFromCSharp != null)
            getMessageFromCsharp(str);
    },

    // Request Response JS -> C# -> JS
    requestNumberCallback: new Map(),
    Request: function (str, callback) {
        delimiter = "@@@";
        number = this.GetRandomInt(2147483647);
        this.requestNumberCallback.set(number, callback);
        SendMessage('JSBridge', 'Request', number + delimiter + str);
    },
    Response: function (number, str) {
        var callback = this.requestNumberCallback.get(number);
        this.requestNumberCallback.delete(number);
        callback(str);
    },

    // Maths
    GetRandomInt: function (max) {
        return Math.floor(Math.random() * max);
    }
}
