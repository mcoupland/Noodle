using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Noodle
{
    public enum ExcelDocumentRowTypes { BLANK, TITLE, HEADER, NORMAL, SUMMARY }
    public enum ExcelDocumentCellTypes { BLANK, COLUMNHEADER, TITLE, NORMAL, CURRENCY, ACCOUNTING, ERROR, RIGHTJUSTIFY }
    public enum ExcelDocumentColors { WHITE, BLACK, BLACK1, BLACK2, BLACK3, BLACK4, BLACK5, RED, RED1, RED2, RED3, RED4, RED5, GREEN, GREEN1, GREEN2, GREEN3, GREEN4, GREEN5, BLUE, BLUE1, BLUE2, BLUE3, BLUE4, BLUE5, YELLOW, YELLOW1, YELLOW2, YELLOW3, YELLOW4, YELLOW5 }

    public class ExcelDocument
    {
        private IWorkbook Workbook;
        private ISheet Sheet;

        public string Title;
        public IEnumerable<string> ColumnHeaders = new List<string>();

        public ExcelDocument(string title, IEnumerable<string> columnHeaders)
        {
            Title = title;
            Workbook = new XSSFWorkbook();
            Sheet = Workbook.CreateSheet();
            ColumnHeaders = columnHeaders;
        }

        public void AddRow(ExcelDocumentRowTypes rowType, IEnumerable<object> cellData = null)
        {            
            var row = Sheet.CreateRow(Sheet.PhysicalNumberOfRows);
            var validateMessage = "";
            try
            {
                if (rowType == ExcelDocumentRowTypes.TITLE)
                {
                    validateMessage = ValidateRowProperties<string>("TITLE", new List<string> { Title });
                    if (!string.IsNullOrWhiteSpace(validateMessage)) { throw new Exception(validateMessage); }

                    cellData = new List<string> { Title };
                    var cellRange = new NPOI.SS.Util.CellRangeAddress(0, 0, 0, ColumnHeaders.Count() - 1);
                    Sheet.AddMergedRegion(cellRange);
                }

                if (rowType == ExcelDocumentRowTypes.HEADER)
                {
                    validateMessage = ValidateRowProperties<string>("HEADER", ColumnHeaders);
                    if (!string.IsNullOrWhiteSpace(validateMessage)) { throw new Exception(validateMessage); }

                    cellData = ColumnHeaders;
                }

                if (rowType == ExcelDocumentRowTypes.BLANK)
                {
                    cellData = new List<string> { "", "", "" };
                }

                for (int i = 0; i < cellData.Count(); i++)
                {
                    GetCell(cellData.ElementAt(i), row, i, rowType);
                }
            }
            catch (Exception e)
            {
                var rowCount = Sheet.LastRowNum;
                throw new Exception(e.Message);
            }
        }

        public void AutosizeColumns(int padding)
        {
            for (int i = 0; i < ColumnHeaders.Count(); i++)
            {
                var beforeAutoSize = Sheet.GetColumnWidth(i);
                var afterAutoSize = 0;
                try
                {
                    Sheet.AutoSizeColumn(i);
                    afterAutoSize = Sheet.GetColumnWidth(i);
                    if ((afterAutoSize + padding) / 256 < 255)
                    {
                        Sheet.SetColumnWidth(i, afterAutoSize + padding);
                    }
                    else
                    {
                        Sheet.SetColumnWidth(i, 256 * 255);
                    }
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        public void SortAndFilter(int headerRow)
        {
            Sheet.SetAutoFilter(new CellRangeAddress(headerRow, Sheet.LastRowNum, 0, ColumnHeaders.Count() - 1));
        }

        public bool SaveWorkbook(string reportFileName)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportFileName));                
                using (var fs = new FileStream(reportFileName, FileMode.Create, FileAccess.Write))
                {
                    Workbook.Write(fs);
                }
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw (ex);
            }
            return false;
        }

        private string ValidateRowProperties<T>(string propertyName, IEnumerable<T> property)
        {
            var valid = "";
            if (property == null || !property.Any())
            {
                valid = $"No items in {propertyName}";
            }
            else if (property.GetType().GetGenericArguments()[0] == typeof(string) && string.IsNullOrWhiteSpace(property.FirstOrDefault().ToString()))
            {
                valid = $"Invalid argument for {propertyName}";
            }
            return valid;
        }

        private void GetCell(object value, IRow row, int cellNumber, ExcelDocumentRowTypes rowType = ExcelDocumentRowTypes.NORMAL)
        {
            var cell = row.CreateCell(cellNumber);
            var cellType = ExcelDocumentCellTypes.NORMAL;
            try
            {
                if (value == null || (value.GetType() == typeof(string) && string.IsNullOrWhiteSpace((string)value)))
                {
                    value = "";
                }

                switch (value)
                {
                    case double db:
                    case decimal dc:
                    case float fl:
                        cell.SetCellValue(Convert.ToDouble(value));
                        cellType = ExcelDocumentCellTypes.CURRENCY;
                        break;
                    case int it:
                        cell.SetCellValue(it == int.MinValue ? "" : value.ToString());
                        cellType = ExcelDocumentCellTypes.RIGHTJUSTIFY;
                        break;
                    case bool bl:
                        cell.SetCellValue(Convert.ToBoolean(value));
                        cellType = ExcelDocumentCellTypes.NORMAL;
                        break;
                    case string st:
                        if (value.ToString().Length >= 32766) { value = value.ToString().Substring(0, 5000); } //5000 chars ought to be enough
                        cell.SetCellValue(value.ToString());
                        cellType = rowType == ExcelDocumentRowTypes.NORMAL ? ExcelDocumentCellTypes.NORMAL : ExcelDocumentCellTypes.BLANK;
                        break;
                    default:
                        cell.SetCellValue(value.ToString());
                        cellType = ExcelDocumentCellTypes.ERROR;
                        break;
                }

                if (rowType == ExcelDocumentRowTypes.HEADER) { cellType = ExcelDocumentCellTypes.COLUMNHEADER; }
                else if (rowType == ExcelDocumentRowTypes.TITLE) { cellType = ExcelDocumentCellTypes.TITLE; }
                cell.CellStyle = AddCellStyle(cellType);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private XSSFCellStyle AddCellStyle(ExcelDocumentCellTypes cellType)
        {
            XSSFCellStyle style = (XSSFCellStyle)Workbook.CreateCellStyle();
            IFont font = Workbook.CreateFont();
            XSSFColor background;
            HorizontalAlignment horizontalAlignment;
            FillPattern fillPattern;
            short dataFormat;

            switch (cellType)
            {
                case ExcelDocumentCellTypes.BLANK:
                    fillPattern = FillPattern.SolidForeground;
                    background = GetXSSFColor(ExcelDocumentColors.WHITE);
                    font.IsBold = false;
                    horizontalAlignment = HorizontalAlignment.Right;
                    break;
                case ExcelDocumentCellTypes.ACCOUNTING:
                    fillPattern = FillPattern.SolidForeground;
                    background = GetXSSFColor(ExcelDocumentColors.GREEN);
                    font.IsBold = false;
                    horizontalAlignment = HorizontalAlignment.Right;
                    style.DataFormat = Workbook.CreateDataFormat().GetFormat("$#,##0.00");
                    break;
                case ExcelDocumentCellTypes.CURRENCY:
                    fillPattern = FillPattern.SolidForeground;
                    background = GetXSSFColor(ExcelDocumentColors.BLUE1);
                    font.IsBold = false;
                    horizontalAlignment = HorizontalAlignment.Right;
                    style.DataFormat = Workbook.CreateDataFormat().GetFormat("$#,##0.00");
                    break;
                case ExcelDocumentCellTypes.ERROR:
                    fillPattern = FillPattern.SolidForeground;
                    background = GetXSSFColor(ExcelDocumentColors.WHITE);
                    font.IsBold = true;
                    horizontalAlignment = HorizontalAlignment.Center;
                    break;
                case ExcelDocumentCellTypes.COLUMNHEADER:
                    fillPattern = FillPattern.SolidForeground;
                    background = GetXSSFColor(ExcelDocumentColors.WHITE);
                    font.IsBold = true;
                    horizontalAlignment = HorizontalAlignment.Center;
                    break;
                case ExcelDocumentCellTypes.TITLE:
                    fillPattern = FillPattern.SolidForeground;
                    background = GetXSSFColor(ExcelDocumentColors.WHITE);
                    font.IsBold = true;
                    horizontalAlignment = HorizontalAlignment.Center;
                    break;
                case ExcelDocumentCellTypes.RIGHTJUSTIFY:
                    fillPattern = FillPattern.SolidForeground;
                    background = GetXSSFColor(ExcelDocumentColors.BLUE1);
                    font.IsBold = false;
                    horizontalAlignment = HorizontalAlignment.Right;
                    break;
                case ExcelDocumentCellTypes.NORMAL:
                default:
                    fillPattern = FillPattern.SolidForeground;
                    background = GetXSSFColor(ExcelDocumentColors.BLUE1);
                    font.IsBold = false;
                    horizontalAlignment = HorizontalAlignment.Left;
                    break;
            }
            style.FillPattern = fillPattern;
            style.SetFillForegroundColor(background); //why do we use foreground method for background color?
            style.SetFont(font);
            style.Alignment = horizontalAlignment;

            return style;
        }

        private XSSFColor GetXSSFColor(ExcelDocumentColors documentColor)
        {
            NPOI.XSSF.UserModel.XSSFColor color = new XSSFColor(new byte[3] { 0, 0, 0 });
            switch (documentColor)
            {
                case ExcelDocumentColors.WHITE:
                    color = new XSSFColor(new byte[3] { 255, 255, 255 });
                    break;
                case ExcelDocumentColors.BLACK:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.BLACK1:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.BLACK2:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.BLACK3:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.BLACK4:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.BLACK5:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.RED:
                    color = new XSSFColor(new byte[3] { 255, 0, 0 });
                    break;
                case ExcelDocumentColors.RED1:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.RED2:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.RED3:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.RED4:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.RED5:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.GREEN:
                    color = new XSSFColor(new byte[3] { 0, 255, 0 });
                    break;
                case ExcelDocumentColors.GREEN1:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.GREEN2:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.GREEN3:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.GREEN4:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.GREEN5:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.BLUE:
                    color = new XSSFColor(new byte[3] { 0, 0, 255 });
                    break;
                case ExcelDocumentColors.BLUE1:
                    color = new XSSFColor(new byte[3] { 221, 235, 247 });
                    break;
                case ExcelDocumentColors.BLUE2:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.BLUE3:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.BLUE4:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.BLUE5:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.YELLOW:
                    color = new XSSFColor(new byte[3] { 255, 255, 0 });
                    break;
                case ExcelDocumentColors.YELLOW1:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.YELLOW2:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.YELLOW3:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.YELLOW4:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
                case ExcelDocumentColors.YELLOW5:
                    color = new XSSFColor(new byte[3] { 0, 0, 0 });
                    break;
            }
            return color;
        }
    }
}
