mergeInto(LibraryManager.library, {
    Response: function (str) {
        Module['Bridge'].Response(UTF8ToString(str));
    },
});
