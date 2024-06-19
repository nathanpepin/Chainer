using System.Collections.Generic;
using System.Text;

namespace Chainer.SourceGen;

public class Registration(string className)
{
    public List<string> Handlers { get; } = [];

    public StringBuilder AddGenerateCode(StringBuilder? stringBuilder = null)
    {
        stringBuilder ??= new StringBuilder();

        stringBuilder.Append("services.TryAddScoped<");
        stringBuilder.Append(className);
        stringBuilder.Append(">();");
        stringBuilder.AppendLine();

        foreach (var handler in Handlers)
        {
            stringBuilder.Append("services.TryAddScoped<");
            stringBuilder.Append(handler);
            stringBuilder.Append(">();");
            stringBuilder.AppendLine();
        }

        return stringBuilder;
    }
}