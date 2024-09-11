using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Interop.Excel;
using OfficeOpenXml;
using PdfSharpCore;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Template.Models;
using Template.Services;
using Microsoft.AspNetCore.Rewrite;
//using Document = iTextSharp.text.Document;

namespace Template.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly PdfGenerator _pdfGenerator;
        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment, PdfGenerator pdfGenerator)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _pdfGenerator = pdfGenerator;
        }



        public enum Separator
        {
            Hyphen,
            Underscore,
            Space,
            None
        }

        
        //This code is used to get the index view of a module.
        [HttpGet]
        public IActionResult Index(string ModuleName)
        {
            //Set the ViewBag to the ModuleName
            ViewBag.TemplatePath = ModuleName;

            //Create a string to store the path of the templates
            string templatePath = Path.Combine(_webHostEnvironment.WebRootPath, "Templates");
            //Set the ViewBag to the list of files in the template path
            string[] listOfTemplates = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories).ToArray();
            ViewBag.ListOfTemplates = listOfTemplates;
            if (String.IsNullOrEmpty(ModuleName))
            {
                ViewBag.TemplatePath = Path.GetFileNameWithoutExtension(listOfTemplates[0]);
            }
            //Return the view
            return View();
        }

        [HttpPost]
        public IActionResult Index(string content, string templaneName, string moduleName)
        {

            var filePath = _webHostEnvironment.WebRootPath + $"/Templates/{templaneName}.html";
            System.IO.File.WriteAllText(filePath, content);
            ViewBag.TemplatePath = moduleName;
            return View();
        }
        [HttpPost]
        public IActionResult Excel(IFormFile file, string content)
        {
            ExcelPackage.LicenseContext = LicenseContext.Commercial;
            if (file != null && file.Length > 0)
            {
                // Read the Excel file using EPPlus
                using (var package = new ExcelPackage(file.OpenReadStream()))
                {
                    var workbook = package.Workbook;
                    var worksheetNames = new List<string>();

                    foreach (var worksheet1 in workbook.Worksheets)
                    {
                        worksheetNames.Add(worksheet1.Name);
                    }
                    var worksheet = package.Workbook.Worksheets[worksheetNames[0]]; // Replace "Sheet1" with the actual name of the worksheet

                    if (worksheet != null)
                    {
                        var start = worksheet.Dimension.Start;
                        var end = worksheet.Dimension.End;

                        // Read the header row
                        var headerRow = worksheet.Cells[start.Row, start.Column, start.Row, end.Column];
                        var headerNames = new List<string>();

                        foreach (var cell in headerRow)
                        {
                            headerNames.Add(cell.Value.ToString());
                        }

                        // Read the remaining rows
                        for (int row = start.Row + 1; row <= end.Row; row++)
                        {
                            for (int col = start.Column; col <= end.Column; col++)
                            {
                                var cell = worksheet.Cells[row, col];
                                var address = cell.Address; // Get the cell address

                                // Check if the current row is not the header row
                                if (row > start.Row)
                                {
                                    var column = new string(address.TakeWhile(char.IsLetter).ToArray()); // Extract the column letter
                                    var headerIndex = headerNames.IndexOf(column); // Get the index of the column header in the list
                                    //var headerName = headerNames[headerIndex]; // Get the name of the column header
                                    var value = cell.Value; // Get the cell value

                                }
                            }
                        }

                    }
                }
            }

            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult LetterGenerate(string Templatename)
        {
            var dTemplatename = TempData["templateName"];
            if (Templatename == null && dTemplatename != null)
            {
                Templatename = dTemplatename.ToString();
            }
            List<string> headers = new List<string>();
            string pattern = @"\${(.*?)}";
            if (Templatename != null)
            {
                var filePath = _webHostEnvironment.WebRootPath + $"/Templates/{Templatename}.html";
                string html = System.IO.File.ReadAllText(filePath);


                MatchCollection matches = Regex.Matches(html, pattern);
                foreach (Match match in matches)
                {
                    string value = match.Groups[1].Value;
                    headers.Add(value);
                }
            }
            List<string> distinctListHeaders = headers.Distinct().ToList();
            ViewBag.Headers = distinctListHeaders;
            string templatePath = Path.Combine(_webHostEnvironment.WebRootPath, "Templates");
            ViewBag.ListOfTemplates = Directory.EnumerateFiles(templatePath, "*", SearchOption.AllDirectories).ToArray();
            ViewBag.valuesList = TempData["valuesList"];
            return View();
        }
        [HttpPost]
        public JsonResult LetterGenerate(string moduleName, IFormFile file)
        {
            var dict = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> rowValues = new Dictionary<string, List<string>>();

            ExcelPackage.LicenseContext = LicenseContext.Commercial;
            if (file != null && file.Length > 0)
            {
                // Read the Excel file using EPPlus
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        // Get the first worksheet
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                        if (worksheet != null)
                        {
                            // Loop through the columns to get the headers
                            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                            {
                                var header = worksheet.Cells[1, col].Value?.ToString();
                                if (!string.IsNullOrEmpty(header))
                                {
                                    dict.Add(header, new List<string>());
                                }
                            }

                            var range = worksheet.Dimension;
                            List<string> headers = new List<string>();

                            // Extract header row values
                            for (int col = 1; col <= range.End.Column; col++)
                            {
                                var headerAddress = $"{(char)('A' + col - 1)}1";
                                var headerValue = worksheet.Cells[headerAddress].Value?.ToString();
                                headers.Add(headerValue);
                            }
                            foreach (string key in headers)
                            {
                                rowValues.Add(key, new List<string>());
                            }
                            // Iterate over the data rows
                            for (int row = 2; row <= range.End.Row; row++)
                            {

                                // Extract values for each column in the row
                                for (int col = 1; col <= range.End.Column; col++)
                                {
                                    var cellAddress = $"{(char)('A' + col - 1)}{row}";
                                    var cellValue = worksheet.Cells[cellAddress].Value?.ToString();
                                    rowValues[headers[col - 1]].Add(cellValue ?? "");
                                }
                            }
                        }
                    }
                }
            }

            ViewBag.ExcelData = rowValues;
            return Json("");
        }

        public IActionResult pdf(string templateName, string selectedIds)
        {
            string[] finalValuesfromGrid = selectedIds.Split(',');
            for (int i = 0; i < finalValuesfromGrid.Length; i++)
            {
                finalValuesfromGrid[i] = finalValuesfromGrid[i].Trim();
            }

            var filePath = _webHostEnvironment.WebRootPath + $"/Templates/{templateName}.html";
            string html = System.IO.File.ReadAllText(filePath);

            List<string> headers = new List<string>();
            string pattern = @"\${(.*?)}";
            MatchCollection matches = Regex.Matches(html, pattern);
            foreach (Match match in matches)
            {
                string value = match.Groups[1].Value;
                headers.Add("{" + value + "}");
            }
            List<string> distinctListHeaders = headers.Distinct().ToList();
            finalValuesfromGrid = finalValuesfromGrid.Where(s => !string.IsNullOrEmpty(s)).ToArray();

            for (int i = 0; i < finalValuesfromGrid.Length; i++)
            {
                html = html.Replace("$" + distinctListHeaders[i], finalValuesfromGrid[i]);
            }

            // Create a new PDF document
            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Arial", 12);

            // Render the HTML content as PDF
            XTextFormatter tf = new XTextFormatter(gfx);
            XRect rect = new XRect(40, 40, page.Width - 80, page.Height - 80);
            tf.DrawString(html, font, XBrushes.Black, rect, XStringFormats.TopLeft);

            // Save the PDF to a MemoryStream
            using (MemoryStream memoryStream = new MemoryStream())
            {
                document.Save(memoryStream, false);
                byte[] bytes = memoryStream.ToArray();

                // Return the PDF as a FileContentResult with "application/pdf" content type
                return Content(html, "text/html");
            }
        }

        [HttpPost]
        public ActionResult print(string templateName, string[] selectedIds, string pageOrientation)
        {
            MemoryStream memoryStream = new MemoryStream();
            string pdfPath = string.Empty;
            foreach (var item in selectedIds)
            {
                string[] finalValuesfromGrid = item.Split('|');
                finalValuesfromGrid = finalValuesfromGrid.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();

                Dictionary<string, string> attributesDictionary = new Dictionary<string, string>();
                foreach (string str in finalValuesfromGrid)
                {
                    string[] parts = str.Split('=');
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        attributesDictionary.Add(key, value);
                    }
                }

                if (attributesDictionary.Count != 0)
                {
                    string fileName = attributesDictionary["${Letter Date}"] + attributesDictionary["${EMP First Name}"] + attributesDictionary["${EMP Middle Name}"]
                        + attributesDictionary["${EMP Last Name}"] + attributesDictionary["${Employee ID/Code}"];

                    var filePath = _webHostEnvironment.WebRootPath + $"/Templates/{templateName}.html";
                    string html = System.IO.File.ReadAllText(filePath);
                    string filePathtemplatePassword = _webHostEnvironment.WebRootPath + $"/TemplateSavedPasswords/{templateName}.txt";
                    string passwordTemplateData = string.Empty;
                    string templateRawPwd = string.Empty;
                    if (System.IO.File.Exists(filePathtemplatePassword))
                    {
                        passwordTemplateData = System.IO.File.ReadAllText(filePathtemplatePassword);
                        templateRawPwd = passwordTemplateData;
                    }

                    foreach (var attr in attributesDictionary)
                    {
                        if (System.IO.File.Exists(filePathtemplatePassword))
                        {
                            if (passwordTemplateData.Contains(attr.Key))
                            {
                                passwordTemplateData = passwordTemplateData.Replace(attr.Key, attr.Value);
                            }
                        }
                        if (html.Contains(attr.Key))
                        {
                            html = html.Replace(attr.Key, attr.Value);
                        }
                    }

                    List<string> options = GetOptions();
                    foreach (string option in options)
                    {
                        passwordTemplateData = passwordTemplateData.Replace(option, string.Empty);
                    }

                    char[] charsToTrim = { '_', '-', ' ' };
                    string finalPwdPattern = passwordTemplateData.TrimEnd(charsToTrim);
                    string emailId = attributesDictionary["${Email Id}"];

                    // Save PDF to file
                    pdfPath = Path.Combine(_webHostEnvironment.WebRootPath, "LetterGeneratorPdfs", $"{fileName}_{templateName}.pdf.pdf");
                    _pdfGenerator.GeneratorPdf(
                                                html,
                                                Path.Combine(_webHostEnvironment.WebRootPath, "LetterGeneratorPdfs"),
                                                $"{fileName}_{templateName}.pdf",
                                                pageOrientation,
                                                finalPwdPattern,
                                                emailId
                                            );

                    if (!string.IsNullOrEmpty(finalPwdPattern))
                    {
                        _pdfGenerator.setPassword(
                                                    pdfPath,
                                                    finalPwdPattern,
                                                    emailId,
                                                    templateRawPwd
                                                );

                        //System.IO.File.Delete(pdfPath);
                    }
                    else
                    {
                        //EmailSender emailSender = new EmailSender();
                        //emailSender.SendEmail(
                        //                        emailId,
                        //                        "No Password for PDF File",
                        //                        pdfPath
                        //                     );
                    }

                    // Read the PDF into memory to return it for download
                    using (var fileStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.CopyTo(memoryStream);
                    }
                }
            }

            memoryStream.Position = 0; // Reset the stream position to the beginning before returning it
            var finalResult = File(memoryStream, "application/pdf", $"{templateName}_{DateTime.UtcNow}.pdf");

            if (System.IO.File.Exists(pdfPath))
            {
                System.IO.File.Delete(pdfPath);
            }

            return finalResult;
        }

        


        [HttpPost]
        public ActionResult editModulePrint(string templateName, string htmlContentFromEditModule, string[] selectedIds)
        {
            MemoryStream memoryStream = new MemoryStream();
            string[] finalValuesfromGrid = selectedIds[0].Split('|');
            finalValuesfromGrid = finalValuesfromGrid.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();

            Dictionary<string, string> attributesDictionary = new Dictionary<string, string>();
            foreach (string str in finalValuesfromGrid)
            {
                string[] parts = str.Split('=');
                string key = parts[0].Trim();
                string value = parts[1].Trim();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    attributesDictionary.Add(key, value);
                }

            }
            string emailId = attributesDictionary["${Email Id}"];
            string filePathtemplatePassword = _webHostEnvironment.WebRootPath + $"/TemplateSavedPasswords/{templateName}.txt";
            string passwordTemplateData = string.Empty;
            string templateRawPwd = string.Empty;
            if (System.IO.File.Exists(filePathtemplatePassword))
            {
                passwordTemplateData = System.IO.File.ReadAllText(filePathtemplatePassword);
                templateRawPwd = passwordTemplateData;
            }
            foreach (var attr in attributesDictionary)
            {
                if (System.IO.File.Exists(filePathtemplatePassword))
                {
                    if (passwordTemplateData.Contains(attr.Key))
                    {
                        passwordTemplateData = passwordTemplateData.Replace(attr.Key, attr.Value);
                    }
                }
                if (htmlContentFromEditModule.Contains(attr.Key))
                {
                    htmlContentFromEditModule = htmlContentFromEditModule.Replace(attr.Key, attr.Value);
                }
            }
            List<string> options = GetOptions();
            foreach (string option in options)
            {
                passwordTemplateData = passwordTemplateData.Replace(option, string.Empty);
            }
            char[] charsToTrim = { '_', '-', ' ' };
            string finalPwdPattern = passwordTemplateData.TrimEnd(charsToTrim);

            string fileName = attributesDictionary["${Letter Date}"] + attributesDictionary["${EMP First Name}"] + attributesDictionary["${EMP Middle Name}"]
                        + attributesDictionary["${EMP Last Name}"] + attributesDictionary["${Employee ID/Code}"];

            _pdfGenerator.GeneratorPdf(
                                        htmlContentFromEditModule,
                                        Path.Combine(_webHostEnvironment.WebRootPath, "LetterGeneratorPdfs"),
                                        $"{fileName}_{templateName}.pdf", // Ensure the file name has a .pdf extension
                                        "",
                                        finalPwdPattern,
                                        emailId
                                      );



            if (!string.IsNullOrEmpty(finalPwdPattern))
            {
                //Thread.Sleep(5000);
                _pdfGenerator.setPassword(
                                            Path.Combine(_webHostEnvironment.WebRootPath, "LetterGeneratorPdfs", $"{fileName}_{templateName}.pdf"),
                                            finalPwdPattern,
                                            emailId,
                                            templateRawPwd
                                         );

            }
            else
            {
                //EmailSender emailSender = new EmailSender();
                //emailSender.SendEmail(emailId, "No Password for PDF File", Path.Combine(_webHostEnvironment.WebRootPath,"LetterGeneratorPdfs",$"{fileName}_{templateName}.pdf"));
            }
            string pdfPath = Path.Combine(_webHostEnvironment.WebRootPath, "LetterGeneratorPdfs", $"{fileName}_{templateName}.pdf.pdf");
            using (var fileStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(memoryStream);
            }
            memoryStream.Position = 0; // Reset the stream position to the beginning before returning it
            var finalResult = File(memoryStream, "application/pdf", $"{templateName}_{DateTime.UtcNow}.pdf");

            if (System.IO.File.Exists(pdfPath))
            {
                System.IO.File.Delete(pdfPath);
            }

            return finalResult;
        }
        public ActionResult templateEditor(string templateName, string[] selectedIds)
        {
            string html = string.Empty;
            foreach (var item in selectedIds)
            {
                string[] finalValuesfromGrid = item.Split('|');
                finalValuesfromGrid = finalValuesfromGrid.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();

                Dictionary<string, string> attributesDictionary = new Dictionary<string, string>();
                foreach (string str in finalValuesfromGrid)
                {
                    string[] parts = str.Split('=');
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        attributesDictionary.Add(key, value);
                    }

                }
                if (attributesDictionary.Count != 0)
                {
                    string fileName = attributesDictionary["${Letter Date}"] + attributesDictionary["${EMP First Name}"] + attributesDictionary["${EMP Middle Name}"]
                        + attributesDictionary["${EMP Last Name}"] + attributesDictionary["${Employee ID/Code}"];

                    var filePath = _webHostEnvironment.WebRootPath + $"/Templates/{templateName}.html";
                    html = System.IO.File.ReadAllText(filePath);
                    string filePathtemplatePassword = _webHostEnvironment.WebRootPath + $"/TemplateSavedPasswords/{templateName}.txt";
                    string passwordTemplateData = string.Empty;
                    string templateRawPwd = string.Empty;
                    if (System.IO.File.Exists(filePathtemplatePassword))
                    {
                        passwordTemplateData = System.IO.File.ReadAllText(filePathtemplatePassword);
                        templateRawPwd = passwordTemplateData;
                    }

                    foreach (var attr in attributesDictionary)
                    {
                        if (System.IO.File.Exists(filePathtemplatePassword))
                        {
                            if (passwordTemplateData.Contains(attr.Key))
                            {
                                passwordTemplateData = passwordTemplateData.Replace(attr.Key, attr.Value);
                            }
                        }
                        if (html.Contains(attr.Key))
                        {
                            html = html.Replace(attr.Key, attr.Value);
                        }
                    }                   
                }

            }
            return Content(html, "text/html");
        }

        [HttpPost]
        public IActionResult printAll(string templateName, string[] selectedIds)
        {


            return View();
        }
        public IActionResult PrintPreview(string templateName)
        {
            return null;
        }

        public ActionResult SetPassword(string templateName)
        {

            var filePath = _webHostEnvironment.WebRootPath + $"/Templates/{templateName}.html";
            string html = System.IO.File.ReadAllText(filePath);
            List<string> options = GetOptions();
            return Json(options);
        }

        public List<string> GetOptions()
        {
            List<string> options = new List<string>
                     {                         
                         "${EMP First Name}",
                         "${EMP Middle Name}",
                         "${EMP Last Name}",
                         "${Employee ID/Code}",
                         "${DOJ}",
                         "${DOB}",
                         "${Pan Number}"
                     };
            return options;
        }

        public ActionResult SavePassword(string[] attributes, string templaneName, Separator separator)
        {
            string content = String.Join(GetSeparatorString(separator), attributes);
            var filePath = _webHostEnvironment.WebRootPath + $"/TemplateSavedPasswords/{templaneName}.txt";
            System.IO.File.WriteAllText(filePath, content);
            return Json(new { success = true });
        }
        public ActionResult GenerateRandomPWD(string randomPassword, string templateName)
        {
            var filePath = _webHostEnvironment.WebRootPath + $"/TemplateSavedPasswords/{templateName}.txt";
            System.IO.File.WriteAllText(filePath, randomPassword);
            return Json(new { success = true });
        }
        public ActionResult DeletePassword(string templateName)
        {
            var filePath = _webHostEnvironment.WebRootPath + $"/TemplateSavedPasswords/{templateName}.txt";
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            return Json(new { success = true });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        private string GetSeparatorString(Separator separator)
        {
            switch (separator)
            {
                case Separator.Hyphen:
                    return "-";
                case Separator.Underscore:
                    return "_";
                case Separator.Space:
                    return " ";
                default:
                    return "";
            }
        }

    }
}
