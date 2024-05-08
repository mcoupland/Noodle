using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportBuilder
{
    public class TemplateSection
    {
        private string template_section;
        private DirectoryInfo template_path;
        private List<string> rows = new List<string>();
        private SectionTypes section_type;

        public enum SectionTypes { Dictionary, Group, Log };

        public TemplateSection(DirectoryInfo templatePath, string name, SectionTypes sectionType) 
        {
            template_path = templatePath;      
            template_section = Template.GetTemplateText(template_path, "Section");
            template_section = Template.ReplaceTemplateToken(template_section, "name", name);
            section_type = sectionType;
        }

        public void AddRow(string title, string detail, EmphasisColor emphasisColor = EmphasisColor.None)
        {
            var row_template_name = "";
            switch (section_type)
            {
                case SectionTypes.Dictionary:
                case SectionTypes.Group:
                    row_template_name = "SectionDictionaryRow";
                    break;
                case SectionTypes.Log:
                    row_template_name = "SectionLogRow";
                    break;
            }
            var emphasis_color = "";
            switch(emphasisColor)
            {
                case EmphasisColor.Blue:
                    emphasis_color = " emphasis-blue";
                    break;
                case EmphasisColor.Red:
                    emphasis_color = " emphasis-red";
                    break;
                case EmphasisColor.Green:
                    emphasis_color = " emphasis-green";
                    break;
            }
            var row = Template.GetTemplateText(template_path, row_template_name);
            row = Template.ReplaceTemplateToken(row, "title", title);
            row = Template.ReplaceTemplateToken(row, "timestamp", Template.GetDateTimeStringLong(DateTime.Now));  // wont hurt to do this since none of the other types have a title token
            row = Template.ReplaceTemplateToken(row, "emphasiscolor", emphasis_color);
            rows.Add(Template.ReplaceTemplateToken(row, "detail", detail));
            
        }

        public string GetSection()
        {
            var template_rows = "";
            foreach(var row in rows)
            {
                template_rows = $"{template_rows}{Environment.NewLine}{row}";
            }
            return Template.ReplaceTemplateToken(template_section, "rows", template_rows);
        }
    }
}
