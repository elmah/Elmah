var stdout = WScript.StdOut, stdin = WScript.StdIn;
var fso = new ActiveXObject("Scripting.FileSystemObject");
var TextFile = {
    read : function(path) {
        var ts = fso.OpenTextFile(path, /* ForReading */ 1);
        try {
            return ts.ReadAll();
        }
        finally {
            ts.Close();
        }
    }
};
var args = [];
for (var i = 0; i < WScript.Arguments.length; i++)
    args.push(WScript.Arguments.Item(i));
main(args);
function main(args) {
    if (!args.length) 
            throw new Error("Missing input file specification.");
    var path = args.shift();
    var text = '-' === path ? stdin.ReadAll() : TextFile.read(path);
    if (args.length) {
        var re = new RegExp(args.shift(), "g");
        if (args.length) 
            text = text.replace(re, args.shift());
    }
    stdout.Write(text);
}
