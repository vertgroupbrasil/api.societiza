using Ardalis.SmartEnum;
using Newtonsoft.Json;

namespace WorkflowProcess.Domain.Enumerations;

[method: JsonConstructor]
public class ProcessType(string name, int value) : SmartEnum<ProcessType>(name, value)
{
    public static readonly ProcessType Abertura = new AberturaType();
    public static readonly ProcessType Alteracao = new AlteracaoType();
    public static readonly ProcessType Baixa = new BaixaType();

    public static implicit operator ProcessType(string name)
        => FromName(name);

    public static implicit operator ProcessType(int value)
        => FromValue(value);

    public static implicit operator string(ProcessType type)
        => type.Name;

    public static implicit operator int(ProcessType type)
        => type.Value;

    public class AberturaType() : ProcessType(nameof(Abertura), 0) { }
    public class AlteracaoType() : ProcessType(nameof(Alteracao), 1) { }
    public class BaixaType() : ProcessType(nameof(Baixa), 2) { }
}