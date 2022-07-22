using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using Newtonsoft.Json;

// Reference: http://paulbourke.net/dataformats/ppm/

// magic extension     format    color
//  P1      PBM     ASCII text blac/white

//  P2      PGM     ASCII text    grey
//  P3      PPM     ASCII text    rgb

//  P5      PGM         byte      grey
//  P6      PPM         byte      rgb

public enum SerializeFormat
{
    ASCII,
    Binary,
}

public struct PXMImageHeader
{
    public string magicIdentifier;
    public string extension;
    public SerializeFormat format;
    public bool grey;
    public int width;
    public int height;
    public int colorMaximumValue;
}

public class PGMImage
{
    private PXMImageHeader header;
    private int[,] imageData;

    public void Load(byte[] fileContent)
    {
        // MemoryStream memStr = new MemoryStream(fileContent);
        CommentFilterReader commentFilterReader = new CommentFilterReader(fileContent);
        WordReader wordReader = new WordReader(commentFilterReader, ' ', '\t', '\n');

        string magicIdentifier = wordReader.ReadWordString(Encoding.ASCII);

        header = new PXMImageHeader();

        header.magicIdentifier = magicIdentifier;
        if (magicIdentifier == "P2")
        {
            header.extension = "pgm";
            header.format = SerializeFormat.ASCII;
            header.grey = true;

        }
        else if (magicIdentifier == "P5")
        {
            header.extension = "pgm";
            header.format = SerializeFormat.Binary;
            header.grey = true;
        }
        else if (magicIdentifier == "P1" || magicIdentifier == "P3" || magicIdentifier == "P6")
        {
            throw new ArgumentException($"we don't support {magicIdentifier} yet");
        }
        else
        {
            throw new ArgumentException("Unrecognize pgm file magic identifier: " + magicIdentifier);
        }

        header.width = int.Parse(wordReader.ReadWordString(Encoding.ASCII));
        header.height = int.Parse(wordReader.ReadWordString(Encoding.ASCII));

        header.colorMaximumValue = int.Parse(wordReader.ReadWordString(Encoding.ASCII));


        int colorSize = header.grey ? 1 : 3;
        int expectDataLength = header.width * header.height * colorSize;

        Debug.Log(JsonConvert.SerializeObject(header));

        if (header.format == SerializeFormat.Binary)
        {
            byte[] binData = wordReader.ReadWord();
            if (binData.Length != expectDataLength)
                throw new ArgumentException($"Data length wrong. expect {expectDataLength} but get {binData.Length}");

            imageData = new int[header.width, header.height];

            for (int i = 0; i < header.width; i++)
                for (int j = 0; j < header.height; j++)
                {
                    imageData[i, header.height - j - 1] = (int)binData[i + j * header.width];
                    if (imageData[i, j] > header.colorMaximumValue) throw new ArgumentException($"pixel value {binData[i * header.height + j]} {i} {j} > color maximum value {header.colorMaximumValue}");
                }
        }
        else
        {
            List<int> intData = new List<int>();
            string nextWord = wordReader.ReadWordString(Encoding.ASCII);
            Debug.Log(nextWord);
            while (nextWord != null)
            {
                intData.Add(int.Parse(nextWord));
                nextWord = wordReader.ReadWordString(Encoding.ASCII);
            }

            Debug.Log(intData.Count);
            if (intData.Count != expectDataLength)
                throw new ArgumentException($"Data length wrong. expect {expectDataLength} but get {intData.Count}");
            imageData = new int[header.width, header.height];

            for (int i = 0; i < header.width; i++)
                for (int j = 0; j < header.height; j++)
                {
                    imageData[i, header.height - j - 1] = intData[i + j * header.width];
                    if (imageData[i, j] > header.colorMaximumValue) throw new ArgumentException($"pixel value {imageData[i, j]} > color maximum value {header.colorMaximumValue}");
                }
        }
    }

    public string Save()
    {
        return "";
    }

    public string magicIdentifier() => header.magicIdentifier;
    public string extension() => header.extension;
    public int width() => header.width;
    public int height() => header.height;
    public SerializeFormat format() => header.format;
    public int colorMaximumValue() => header.colorMaximumValue;

    void SetPixel(int x, int y, int grey) => imageData[x, y] = grey;
    public int GetPixel(int x, int y) => imageData[x, y];


}
class CommentFilterReader
{
    MemoryStream ms;
    int peek;
    public CommentFilterReader(byte[] bytes) : base() {
        ms = new MemoryStream(bytes);
        peek = ms.ReadByte();
    }
    public int Peek() => peek;
    public int Read()
    {
        int old_peek = peek;

        int c = ms.ReadByte();

        if (c == (int)'#')
        {
            int next = ms.ReadByte();
            while (next != (int)'\n' && next != -1)
                next = ms.ReadByte();

            if (next == (int)'\n')
                peek = ms.ReadByte();
            else
                peek = next;
        }
        else
        {
            peek = c;
        }

        return old_peek;
    }
}

class WordReader
{
    private CommentFilterReader reader;
    List<char> delimitorList;
    public WordReader(CommentFilterReader reader, params char[] delimitors)
    {
        this.reader = reader;
        delimitorList = new List<char>(delimitors);
    }

    public string ReadWordString(Encoding encoding)
    {
        var bytes = ReadWord();
        if (bytes == null) return null;
        else return encoding.GetString(bytes);
    }

    public byte[] ReadWord()
    {
        List<byte> word = new List<byte>();

        while (reader.Peek() != -1 && delimitorList.Contains((char)reader.Peek()))
            reader.Read();

        if (reader.Peek() == -1)
            return null;

        while (true)
        {
            int c = reader.Read();
            if (c == -1 || delimitorList.Contains((char)c)) break;
            word.Add((byte)c);
        }

        return word.ToArray();
    }
}