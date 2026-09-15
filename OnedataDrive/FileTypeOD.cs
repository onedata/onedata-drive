using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnedataDrive
{
    public enum FileTypeOD
    {
        EMPTY,
        REG,
        DIR,
        SYMLNK,
        UNKNOWN
    }

    public class FileTypeODConverter : JsonConverter<FileTypeOD>
    {
        public override FileTypeOD Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return FileTypeOD.EMPTY;
            }

            var stringValue = reader.GetString();

            if (string.IsNullOrEmpty(stringValue))
            {
                return FileTypeOD.EMPTY;
            }

            return stringValue switch
            {
                "REG" => FileTypeOD.REG,
                "DIR" => FileTypeOD.DIR,
                "SYMLNK" => FileTypeOD.SYMLNK,
                _ => FileTypeOD.UNKNOWN
            };
        }

        public override void Write(Utf8JsonWriter writer, FileTypeOD value, JsonSerializerOptions options)
        {
            var stringValue = value switch
            {
                FileTypeOD.REG => "REG",
                FileTypeOD.DIR => "DIR",
                FileTypeOD.SYMLNK => "SYMLNK",
                FileTypeOD.EMPTY => null,
                _ => "UNKNOWN"
            };

            if (stringValue == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(stringValue);
            }
        }
    }
}
