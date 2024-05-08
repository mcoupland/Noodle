using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportBuilder
{
    public enum EmphasisColor { None, Red, Green, Blue};

    public class Template
    {
        private string template_report;
        private string template_head;
        private string template_body;
        private string template_title;
        private string template_footer;
        private string template_sections;
        private FileInfo report_name;
        private DirectoryInfo template_path;
        private List<TemplateSection> templateSections;
        public string FileName;

        public Template(FileInfo reportName, DirectoryInfo templatePath) 
        {
            templateSections = new List<TemplateSection>();
            report_name = reportName;
            template_path = templatePath;
            FileName = reportName.FullName;
            template_report = Template.GetTemplateText(template_path, "Report");
            template_head = Template.GetTemplateText(template_path, "Head");
            template_body = Template.GetTemplateText(template_path, "Body");
            template_title = Template.GetTemplateText(template_path, "Title");
            template_footer = Template.GetTemplateText(template_path, "Footer");
        }

        public void AddTitle(string title, string subTitle, string dateTime)
        {
            template_title = Template.ReplaceTemplateToken(template_title, "title", title);
            template_title = Template.ReplaceTemplateToken(template_title, "subtitle", subTitle);
            template_title = Template.ReplaceTemplateToken(template_title, "datetime", dateTime);
        }

        public void AddSection(TemplateSection templateSection)
        {
            templateSections.Add(templateSection);
        }

        public void AddFooter(string text, string timespan)
        {
            template_footer = Template.ReplaceTemplateToken(template_footer, "text", text);
            template_footer = Template.ReplaceTemplateToken(template_footer, "timespan", timespan);
        }

        public void Write()
        {
            foreach (var section in templateSections)
            {
                template_sections = $"{template_sections}{Environment.NewLine}{section.GetSection()}";
            }
            template_body = Template.ReplaceTemplateToken(template_body, "title", template_title);
            template_body = Template.ReplaceTemplateToken(template_body, "sections", template_sections);
            template_body = Template.ReplaceTemplateToken(template_body, "footer", template_footer);
            template_report = Template.ReplaceTemplateToken(template_report, "head", template_head);
            template_report = Template.ReplaceTemplateToken(template_report, "body", template_body);

            File.WriteAllText(report_name.FullName, template_report);
        }

        public static string GetTemplateText(DirectoryInfo templatePath, string templateName)
        {
            return File.ReadAllText($"{templatePath.FullName}{Path.DirectorySeparatorChar}Template_{templateName}.txt");
        }

        public static string ReplaceTemplateToken(string template, string tokenName, string value)
        {
            return template.Replace($"[[^{tokenName.ToUpper()}^]]", value);
        }

        internal static string GetDateTimeStringLong(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd h:mm tt");
        }
    }
}
