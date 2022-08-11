using System;
using System.IO;
using System.Text;

using UnityEngine;

public static class UnitySystemConsoleRedirector
{
    private class UnityTextWriter : TextWriter
    {
        public override void WriteLine(string value)
        {
            Debug.Log(value);
        }
        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }
    }

    public static void Redirect()
    {
        Console.SetOut(new UnityTextWriter());
        Console.WriteLine("Redirect Console.WriteLine to Debug.Log");
    }
}