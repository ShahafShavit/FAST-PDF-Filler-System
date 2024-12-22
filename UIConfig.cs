﻿using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Windows.Forms.VisualStyles;
using Newtonsoft.Json;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using LiteDB;
using System.Text;

public class TextBoxWriter : TextWriter
{
    private readonly TextBox _textBox;

    public TextBoxWriter(TextBox textBox)
    {
        _textBox = textBox;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        if (_textBox.InvokeRequired)
        {
            _textBox.BeginInvoke(new Action(() => _textBox.AppendText(value.ToString())));
        }
        else
        {
            _textBox.AppendText(value.ToString());
        }
    }

    public override void Write(string value)
    {
        value = value.Replace("\n", "\r\n");
        if (_textBox.InvokeRequired)
        {
            _textBox.BeginInvoke(new Action(() => _textBox.AppendText(value)));
        }
        else
        {
            _textBox.AppendText(value);
        }
    }
}
public static class Utility
{
    public static string ReverseRtlString(string input)
    {
        // Reverse the entire string
        char[] reversed = input.ToCharArray();
        Array.Reverse(reversed);
        string reversedString = new string(reversed);

        // Use a regex to reverse numbers back to their original order
        string formatted = Regex.Replace(reversedString, @"\d+", match =>
        {
            char[] numArray = match.Value.ToCharArray();
            Array.Reverse(numArray);
            return new string(numArray);
        });

        return formatted;
    }
    public static bool IsDate(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        string pattern = @"^(0[1-9]|[12][0-9]|3[01])[\/.\-](0[1-9]|1[0-2])[\/.\-](\d{2})$";
        return Regex.IsMatch(input, pattern);
    }
    public static PdfFont LoadSystemFont(string fontName)
    {
        // Use InstalledFontCollection to retrieve system fonts
        InstalledFontCollection fonts = new InstalledFontCollection();
        foreach (var fontFamily in fonts.Families)
        {
            if (fontFamily.Name.Equals(fontName, StringComparison.InvariantCultureIgnoreCase))
            {
                // Get the font file path
                string fontPath = GetFontFilePath(fontFamily.Name);

                if (!string.IsNullOrEmpty(fontPath))
                {
                    // Create the font program
                    FontProgram fontProgram = FontProgramFactory.CreateFont(fontPath);
                    return PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H);
                }
            }
        }

        throw new IOException($"Font \"{fontName}\" not found.");
    }
    public static string GetFontFilePath(string fontName)
    {
        // Search in Windows Fonts directory
        string fontsFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
        string[] fontFiles = Directory.GetFiles(fontsFolder, "*.ttf");

        foreach (string fontFile in fontFiles)
        {
            if (System.IO.Path.GetFileNameWithoutExtension(fontFile).Equals(fontName, StringComparison.InvariantCultureIgnoreCase))
            {
                return fontFile;
            }
        }

        throw new FileNotFoundException("Unable to locate font file path");
    }
}
public class InputField
{
    public string Type { get; set; }
    [BsonIgnore]
    public string Text { get; set; }
    public string Name { get; set; }
    public string Label { get; set; }
    public string Placeholder { get; set; }
    public string DefaultText { get; set; }
    public PDFSettings PDFSettings { get; set; }
}
public class TabObject
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string TabName { get; set; }
    public List<FormObject> Forms { get; set; }
}
public class FormObject
{
    public string FormName { get; set; }
    public string FileName { get; set; }
    public string Path { get; set; }
    public string Checksum { get; set; }
    public List<InputField> Fields { get; set; }

    public void FillForm(string outputName, string outputPath, string inputPath)
    {

        string fileinputPath = System.IO.Path.Combine(inputPath, this.Path);  // Path to your existing PDF
        string fullOutputPath = System.IO.Path.Combine(outputPath, outputName);    // Path for the modified PDF


        using (PdfReader reader = new PdfReader(fileinputPath))
        using (PdfWriter writer = new PdfWriter(fullOutputPath))
        using (PdfDocument pdf = new PdfDocument(reader, writer))
        {

            foreach (InputField inputField in this.Fields)
            {

                foreach (Location c in inputField.PDFSettings.Location)
                {
                    var page = pdf.GetPage(c.Page);

                    var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);


                    float x = c.X;
                    float y = c.Y;

                    var font = Utility.LoadSystemFont(inputField.PDFSettings.Font);
                    var formattedText = "";
                    if (Utility.IsDate(inputField.Text))
                    {
                        formattedText = inputField.Text;
                    }
                    else
                    {
                        formattedText = Utility.ReverseRtlString(inputField.Text);
                    }
                    Rectangle textbox = new Rectangle(((int)x), ((int)y), 100,200);
                    //formattedText = formattedText.Replace(" ", "\n");
                    float fontSize = GetFontSize(formattedText.Length);

                    Paragraph paragraph = new Paragraph(formattedText)
                        .SetFont(font)
                        .SetFontSize(fontSize)
                        .SetBaseDirection(BaseDirection.RIGHT_TO_LEFT);

                    
                    var document = new iText.Layout.Document(pdf);
                    document.ShowTextAligned(paragraph, x, y, c.Page, TextAlignment.RIGHT, iText.Layout.Properties.VerticalAlignment.BOTTOM, 0);
                    //document.ShowTextAligned(paragraph, textbox.X, textbox.Y+textbox.Height, c.Page, TextAlignment.RIGHT, iText.Layout.Properties.VerticalAlignment.BOTTOM, 0);
                }

            }

        }
    }
    public static int GetFontSize(int x)
    {
        if (x < 8) return 10;
        double realvalue = (1 / (0.05 * x + 0.1)) + 8;
        return (int)Math.Ceiling(realvalue);
    }
    
}
public class UIConfig
{
    [BsonId]
    public ObjectId Id { get; set; }
    public GeneralSettings GeneralSettings { get; set; }
    public List<TabObject> Tabs { get; set; }
}



public class GeneralSettings
{
    public string SavePath { get; set; }
    public string InputPath {  get; set; }
    public bool Debug {  get; set; }
}

public class PDFSettings
{
    public string Font { get; set; }
    public int Size { get; set; }
    public bool SizeFunctionOverride {  get; set; }
    public bool Required { get; set; }
    public bool RTL { get; set; }
    public List<Location> Location { get; set; }
}

public class Location
{
    public int Page { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}
