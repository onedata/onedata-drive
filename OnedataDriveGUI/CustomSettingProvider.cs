using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedataDriveGUI
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Xml;

    public class CustomSettingsProvider : SettingsProvider
    {
        public string filePath { get; private set; }
        public string folderPath { get; private set; }
        public string fileName { get; private set; }

        public override string ApplicationName
        {
            get => AppDomain.CurrentDomain.FriendlyName;
            set { /* Do nothing */ }
        }

        public CustomSettingsProvider()
        {
            folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OnedataDrive");
            fileName = "UserSettings.config";
            filePath = Path.Combine(folderPath, fileName);
            // TODO: log path
        }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            base.Initialize(ApplicationName, config);
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties)
        {
            var values = new SettingsPropertyValueCollection();
            var xml = LoadSettingsXml();

            foreach (SettingsProperty property in properties)
            {
                var value = new SettingsPropertyValue(property)
                {
                    IsDirty = false,
                    SerializedValue = xml.SelectSingleNode($"//settings/{property.Name}")?.InnerText ?? property.DefaultValue
                };
                values.Add(value);
            }

            return values;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection values)
        {
            var xml = LoadSettingsXml();

            foreach (SettingsPropertyValue value in values)
            {
                var node = xml.SelectSingleNode($"//settings/{value.Name}");
                if (node == null)
                {
                    node = xml.CreateElement(value.Name);
                    xml.DocumentElement?.AppendChild(node);
                }
                node.InnerText = value.SerializedValue?.ToString() ?? string.Empty;
                
                if (node is XmlElement element)
                {
                    element.SetAttribute("Type", value.PropertyValue.GetType().ToString());
                }
            }

            SaveSettingsXml(xml);
        }

        private XmlDocument LoadSettingsXml()
        {
            var xml = new XmlDocument();

            if (File.Exists(filePath))
            {
                xml.Load(filePath);
            }
            else
            {
                var declaration = xml.CreateXmlDeclaration("1.0", "utf-8", null);
                xml.AppendChild(declaration);
                var root = xml.CreateElement("settings");
                xml.AppendChild(root);
            }

            return xml;
        }

        private void SaveSettingsXml(XmlDocument xml)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                xml.Save(filePath);
            }
            catch (Exception e)
            {
                // TODO: log
            }
        }
    }

}
