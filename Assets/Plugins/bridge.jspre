Module['Bridge'] = {
    response: null,
    GetResponse: function () {
        return this.response;
    },
    Response: function (str) {
        this.response = str;
    },
    SendJsonMessage(str) {
        SendMessage('JSBridge', 'SendJsonMessage', str)
    },
}
