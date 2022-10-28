mergeInto(LibraryManager.library, {
    DeviceUniqueIdentifier: function () {
        var deviceUniqueIdentifier = localStorage.getItem("deviceUniqueIdentifier")
        if (deviceUniqueIdentifier === null)
        {
            console.log("deviceUniqueIdentifier is null. Generate a new one")

            var date = new Date();
            var id = md5(date.getTime() + " " + Math.floor(Math.random() * 2147483647));

            localStorage.setItem("deviceUniqueIdentifier", id)
            console.log("new deviceUniqueIdentifier generated: " + id)
            deviceUniqueIdentifier = id
        }
        else
        {
            console.log("get deviceUniqueIdentifier: " + deviceUniqueIdentifier)
        }

        var bufferSize = lengthBytesUTF8(deviceUniqueIdentifier) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(deviceUniqueIdentifier, buffer, bufferSize);
        return buffer;
    }
});
