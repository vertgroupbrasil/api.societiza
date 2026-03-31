using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace WorkflowTemplate.Domain.Enumerations;

[method: JsonConstructor]
public class FieldType(string name, int value) : SmartEnum<FieldType>(name, value)
{
    public static readonly FieldType Text = new TextType();
    public static readonly FieldType Boolean = new BooleanType();
    public static readonly FieldType Integer = new IntegerType();
    public static readonly FieldType Select = new SelectType();
    public static readonly FieldType Date = new DateType();

    public static implicit operator FieldType(string name)
        => FromName(name);

    public static implicit operator FieldType(int value)
        => FromValue(value);

    public static implicit operator string(FieldType fieldType)
        => fieldType.Name;

    public static implicit operator int(FieldType fieldType)
        => fieldType.Value;

    public class TextType() : FieldType(nameof(Text), 0) { }
    public class BooleanType() : FieldType(nameof(Boolean), 1) { }
    public class IntegerType() : FieldType(nameof(Integer), 2) { }
    public class SelectType() : FieldType(nameof(Select), 3) { }
    public class DateType() : FieldType(nameof(Date), 4) { }
}
